using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Services;
using Crypto_Simulation.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Crypto_Simulation.Tests
{
    /// <summary>Builds an isolated in-memory context plus seeded market data for each test.</summary>
    internal static class TestContextFactory
    {
        public const int BitcoinId = 1;
        public const int EthereumId = 2;

        public static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"crypto-tests-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        public static async Task<AppDbContext> CreateSeededContextAsync(decimal startingBalance = 10_000M)
        {
            var context = CreateContext();

            context.CryptoCurrencies.AddRange(
                new CryptoCurrency { Id = BitcoinId, Name = "Bitcoin", Symbol = "BTC", CurrentPrice = 50_000M, TotalSupply = 21_000_000M },
                new CryptoCurrency { Id = EthereumId, Name = "Ethereum", Symbol = "ETH", CurrentPrice = 2_500M, TotalSupply = 120_000_000M });

            var user = new User
            {
                Id = 1,
                Username = "trader",
                Email = "trader@example.com",
                PasswordHash = "not-used-in-these-tests",
                Role = UserRoles.User,
                Wallet = new Wallet { Id = 1, Balance = startingBalance }
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return context;
        }

        public static TradeService CreateTradeService(AppDbContext context) =>
            new(context, NullLogger<TradeService>.Instance);

        public static ProfitService CreateProfitService(AppDbContext context) => new(context);

        public static UserService CreateUserService(AppDbContext context, decimal startingBalance = 10_000M) =>
            new(context,
                new PasswordHasher<User>(),
                new TokenService(Options.Create(new JwtOptions
                {
                    Key = "test-signing-key-that-is-long-enough-for-hmac-sha256",
                    Issuer = "tests",
                    Audience = "tests",
                    ExpiryMinutes = 60
                })),
                Options.Create(new SimulationOptions { StartingBalance = startingBalance }),
                NullLogger<UserService>.Instance);
    }
}
