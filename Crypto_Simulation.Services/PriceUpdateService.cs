using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.Services
{
    public class PriceUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriceUpdateService> _logger;
        private readonly Random _random = new Random();
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public PriceUpdateService(
            IServiceProvider serviceProvider,
            ILogger<PriceUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price Update Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Price Update Service is running at: {time}", DateTimeOffset.Now);

                try
                {
                    await UpdatePricesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred updating prices.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Price Update Service is stopping.");
        }

        private async Task UpdatePricesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cryptos = await context.CryptoCurrencies.ToListAsync();

                foreach (var crypto in cryptos)
                {
                    decimal fluctuation = (decimal)(_random.NextDouble() * 0.06 - 0.03);

                    decimal newPrice = crypto.CurrentPrice * (1 + fluctuation);

                    newPrice = Math.Max(0.01M, newPrice);

                    newPrice = Math.Round(newPrice, 2);

                    crypto.CurrentPrice = newPrice;

                    var priceHistory = new PriceHistory
                    {
                        CryptoId = crypto.Id,
                        Price = newPrice,
                        Timestamp = DateTime.Now
                    };

                    context.PriceHistories.Add(priceHistory);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Updated prices for {count} cryptocurrencies", cryptos.Count);
            }
        }
    }
}
