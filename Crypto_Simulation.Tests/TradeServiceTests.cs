using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Crypto_Simulation.Tests
{
    public class TradeServiceTests
    {
        private const int UserId = 1;

        [Fact]
        public async Task Buy_DebitsBalanceAndOpensPositionAtCurrentPrice()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            var result = await trade.BuyCryptoAsync(UserId, new TradeRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                Quantity = 0.5M
            });

            var wallet = await context.Wallets.Include(w => w.PortfolioItems).SingleAsync();
            var position = Assert.Single(wallet.PortfolioItems);

            Assert.Equal(75_000M, wallet.Balance);       // 100 000 - 0.5 * 50 000
            Assert.Equal(0.5M, position.Quantity);
            Assert.Equal(50_000M, position.AveragePrice);
            Assert.Equal("Buy", result.Type);
            Assert.Equal(25_000M, result.TotalPrice);
        }

        [Fact]
        public async Task Buy_KeepsFractionalQuantities()
        {
            // Regression: with the old decimal(18,2) columns this rounded to zero.
            await using var context = await TestContextFactory.CreateSeededContextAsync();
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                Quantity = 0.00000001M
            });

            var position = await context.PortfolioItems.SingleAsync();
            Assert.Equal(0.00000001M, position.Quantity);
        }

        [Fact]
        public async Task Buy_RollsAveragePriceForwardAcrossPurchases()
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            var bitcoin = await context.CryptoCurrencies.FindAsync(TestContextFactory.BitcoinId);
            bitcoin!.CurrentPrice = 30_000M;
            await context.SaveChangesAsync();

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            var position = await context.PortfolioItems.SingleAsync();
            Assert.Equal(2M, position.Quantity);
            Assert.Equal(40_000M, position.AveragePrice);   // (50 000 + 30 000) / 2
        }

        [Fact]
        public async Task Buy_RejectsOrderLargerThanBalance()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100M);
            var trade = TestContextFactory.CreateTradeService(context);

            await Assert.ThrowsAsync<ValidationException>(() =>
                trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M }));

            var wallet = await context.Wallets.SingleAsync();
            Assert.Equal(100M, wallet.Balance);
        }

        [Fact]
        public async Task Buy_RejectsUnknownCurrency()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync();
            var trade = TestContextFactory.CreateTradeService(context);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = 999, Quantity = 1M }));
        }

        [Fact]
        public async Task Sell_CreditsBalanceAndLeavesAveragePriceAlone()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            var bitcoin = await context.CryptoCurrencies.FindAsync(TestContextFactory.BitcoinId);
            bitcoin!.CurrentPrice = 60_000M;
            await context.SaveChangesAsync();

            await trade.SellCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 0.5M });

            var wallet = await context.Wallets.Include(w => w.PortfolioItems).SingleAsync();
            var position = Assert.Single(wallet.PortfolioItems);

            Assert.Equal(80_000M, wallet.Balance);         // 50 000 left + 0.5 * 60 000
            Assert.Equal(0.5M, position.Quantity);
            Assert.Equal(50_000M, position.AveragePrice);  // unchanged by a partial sale
        }

        [Fact]
        public async Task Sell_RemovesThePositionWhenFullyClosed()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });
            await trade.SellCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            Assert.Empty(await context.PortfolioItems.ToListAsync());
        }

        [Fact]
        public async Task Sell_RejectsMoreThanHeld()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            await Assert.ThrowsAsync<ValidationException>(() =>
                trade.SellCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 2M }));
        }

        [Fact]
        public async Task Convert_SetsAveragePriceOnTheNewPosition()
        {
            // Regression: the average price used to be left at 0, so the position looked like
            // it had been acquired for free.
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });
            await trade.ConvertCryptoAsync(UserId, new ConvertRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                TargetCryptoId = TestContextFactory.EthereumId,
                Quantity = 1M
            });

            var ethereum = await context.PortfolioItems.SingleAsync(pi => pi.CryptoId == TestContextFactory.EthereumId);

            Assert.Equal(20M, ethereum.Quantity);            // 50 000 / 2 500
            Assert.Equal(2_500M, ethereum.AveragePrice);
            Assert.NotEqual(0M, ethereum.AveragePrice);
        }

        [Fact]
        public async Task Convert_ProducesNoPhantomProfit()
        {
            // Regression: because AveragePrice was 0, the profit report treated the converted
            // position as pure profit.
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);
            var profit = TestContextFactory.CreateProfitService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });
            await trade.ConvertCryptoAsync(UserId, new ConvertRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                TargetCryptoId = TestContextFactory.EthereumId,
                Quantity = 1M
            });

            var report = await profit.CalculateProfitAsync(UserId);

            Assert.Equal(50_000M, report.TotalInvestment);
            Assert.Equal(0M, report.TotalProfitLoss);
        }

        [Fact]
        public async Task Convert_MergesIntoAnExistingPositionAtAWeightedAverage()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 200_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.EthereumId, Quantity = 20M });
            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            var ethereumCurrency = await context.CryptoCurrencies.FindAsync(TestContextFactory.EthereumId);
            ethereumCurrency!.CurrentPrice = 5_000M;
            await context.SaveChangesAsync();

            await trade.ConvertCryptoAsync(UserId, new ConvertRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                TargetCryptoId = TestContextFactory.EthereumId,
                Quantity = 1M
            });

            var ethereum = await context.PortfolioItems.SingleAsync(pi => pi.CryptoId == TestContextFactory.EthereumId);

            Assert.Equal(30M, ethereum.Quantity);            // 20 held + 50 000 / 5 000
            Assert.Equal(3_333.333333333333M, Math.Round(ethereum.AveragePrice, 12));  // (50 000 + 50 000) / 30
        }

        [Fact]
        public async Task Convert_IsRecordedAsAConvertTransaction()
        {
            // Regression: conversions used to be stored as TransactionType.Buy while the
            // response claimed "Convert".
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });
            var result = await trade.ConvertCryptoAsync(UserId, new ConvertRequestDto
            {
                CryptoId = TestContextFactory.BitcoinId,
                TargetCryptoId = TestContextFactory.EthereumId,
                Quantity = 1M
            });

            var stored = await context.Transactions.SingleAsync(t => t.Id == result.TransactionId);

            Assert.Equal(TransactionType.Convert, stored.Type);
            Assert.Equal("Convert", result.Type);
        }

        [Fact]
        public async Task Convert_RejectsTheSameCurrencyOnBothSides()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            await Assert.ThrowsAsync<ValidationException>(() =>
                trade.ConvertCryptoAsync(UserId, new ConvertRequestDto
                {
                    CryptoId = TestContextFactory.BitcoinId,
                    TargetCryptoId = TestContextFactory.BitcoinId,
                    Quantity = 1M
                }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Buy_RejectsNonPositiveQuantities(decimal quantity)
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync();
            var trade = TestContextFactory.CreateTradeService(context);

            await Assert.ThrowsAsync<ValidationException>(() =>
                trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = quantity }));
        }

        [Fact]
        public async Task GetPortfolio_ReportsUnrealisedProfitPerPosition()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync(startingBalance: 100_000M);
            var trade = TestContextFactory.CreateTradeService(context);

            await trade.BuyCryptoAsync(UserId, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M });

            var bitcoin = await context.CryptoCurrencies.FindAsync(TestContextFactory.BitcoinId);
            bitcoin!.CurrentPrice = 55_000M;
            await context.SaveChangesAsync();

            var portfolio = await trade.GetPortfolioAsync(UserId);
            var position = Assert.Single(portfolio.Cryptos);

            Assert.Equal(5_000M, position.ProfitLoss);
            Assert.Equal(10M, position.ProfitLossPercentage);
        }

        [Fact]
        public async Task Trading_RequiresAWallet()
        {
            await using var context = await TestContextFactory.CreateSeededContextAsync();
            var trade = TestContextFactory.CreateTradeService(context);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                trade.BuyCryptoAsync(userId: 42, new TradeRequestDto { CryptoId = TestContextFactory.BitcoinId, Quantity = 1M }));
        }
    }
}
