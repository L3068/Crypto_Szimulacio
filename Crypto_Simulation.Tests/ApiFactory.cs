using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Crypto_Simulation.Tests
{
    /// <summary>
    /// Boots the real API pipeline (routing, authentication, authorization, the exception
    /// handler) against an in-memory database so the security boundary is exercised end to end.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DatabaseConnection"] = "Server=(placeholder);Database=ignored;",
                    ["Jwt:Key"] = "integration-test-signing-key-long-enough-for-hmac-sha256",
                    ["Jwt:Issuer"] = "Crypto_Simulation",
                    ["Jwt:Audience"] = "Crypto_Simulation",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["Simulation:StartingBalance"] = "100000"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Swap SQL Server for an in-memory store. Besides DbContextOptions itself, EF
                // registers option-configuration entries that would keep the SQL Server provider
                // alive, so strip anything option-shaped rather than naming each type.
                var optionDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("DbContextOptions", StringComparison.Ordinal) == true)
                    .ToList();

                foreach (var descriptor in optionDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));

                // The price ticker uses ExecuteDeleteAsync, which the in-memory provider cannot run.
                services.RemoveAll<IHostedService>();
            });
        }

        /// <summary>Seeds market data and returns a client with no credentials attached.</summary>
        public async Task<HttpClient> CreateSeededClientAsync()
        {
            var client = CreateClient();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await context.CryptoCurrencies.AnyAsync())
            {
                context.CryptoCurrencies.AddRange(
                    new CryptoCurrency { Id = 1, Name = "Bitcoin", Symbol = "BTC", CurrentPrice = 50_000M, TotalSupply = 21_000_000M },
                    new CryptoCurrency { Id = 2, Name = "Ethereum", Symbol = "ETH", CurrentPrice = 2_500M, TotalSupply = 120_000_000M });
                await context.SaveChangesAsync();
            }

            return client;
        }

        /// <summary>Promotes an existing account to admin.</summary>
        public async Task PromoteToAdminAsync(int userId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users.FindAsync(userId);
            user!.Role = UserRoles.Admin;
            await context.SaveChangesAsync();
        }
    }
}
