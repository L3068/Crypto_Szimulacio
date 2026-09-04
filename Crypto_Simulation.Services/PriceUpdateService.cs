using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crypto_Simulation.Services
{
    /// <summary>
    /// Nudges every price by a small random amount on a fixed interval and records the result.
    /// </summary>
    public class PriceUpdateService : BackgroundService
    {
        private const decimal MinimumPrice = 0.000001M;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriceUpdateService> _logger;
        private readonly SimulationOptions _options;

        public PriceUpdateService(
            IServiceProvider serviceProvider,
            ILogger<PriceUpdateService> logger,
            IOptions<SimulationOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(_options.PriceUpdateIntervalSeconds);
            _logger.LogInformation("Price update service started, interval {Interval}.", interval);

            using var timer = new PeriodicTimer(interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdatePricesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Keep the loop alive; a transient database error should not kill the job.
                    _logger.LogError(ex, "Price update failed.");
                }

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Price update service stopped.");
        }

        private async Task UpdatePricesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cryptos = await context.CryptoCurrencies.ToListAsync(cancellationToken);
            if (cryptos.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var crypto in cryptos)
            {
                // Random.Shared is thread safe; the old private Random instance was not.
                double swing = (Random.Shared.NextDouble() * 2 - 1) * _options.MaxPriceFluctuation;
                decimal newPrice = crypto.CurrentPrice * (1 + (decimal)swing);

                // Round to 8 decimals rather than 2, so sub-cent coins do not collapse to zero.
                newPrice = Math.Max(MinimumPrice, Math.Round(newPrice, 8));

                crypto.CurrentPrice = newPrice;

                context.PriceHistories.Add(new PriceHistory
                {
                    CryptoId = crypto.Id,
                    Price = newPrice,
                    Timestamp = now
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            await PruneHistoryAsync(context, now, cancellationToken);

            _logger.LogDebug("Updated prices for {Count} cryptocurrencies.", cryptos.Count);
        }

        private async Task PruneHistoryAsync(AppDbContext context, DateTime now, CancellationToken cancellationToken)
        {
            if (_options.PriceHistoryRetentionDays <= 0)
            {
                return;
            }

            var cutoff = now.AddDays(-_options.PriceHistoryRetentionDays);
            int removed = await context.PriceHistories
                .Where(ph => ph.Timestamp < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (removed > 0)
            {
                _logger.LogInformation("Pruned {Count} price history rows older than {Cutoff}.", removed, cutoff);
            }
        }
    }
}
