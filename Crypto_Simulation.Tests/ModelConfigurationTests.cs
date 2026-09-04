using Crypto_Simulation.DataContext.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Crypto_Simulation.Tests
{
    /// <summary>
    /// Guards the column shapes. The original model left every decimal at SQL Server's
    /// decimal(18,2) default, which silently rounded fractional coin amounts to two decimals.
    /// </summary>
    public class ModelConfigurationTests
    {
        [Theory]
        [InlineData(typeof(PortfolioItem), nameof(PortfolioItem.Quantity), 38, 18)]
        [InlineData(typeof(Transaction), nameof(Transaction.Quantity), 38, 18)]
        [InlineData(typeof(CryptoCurrency), nameof(CryptoCurrency.TotalSupply), 38, 18)]
        [InlineData(typeof(CryptoCurrency), nameof(CryptoCurrency.CurrentPrice), 28, 12)]
        [InlineData(typeof(PortfolioItem), nameof(PortfolioItem.AveragePrice), 28, 12)]
        [InlineData(typeof(Transaction), nameof(Transaction.PricePerUnit), 28, 12)]
        [InlineData(typeof(PriceHistory), nameof(PriceHistory.Price), 28, 12)]
        [InlineData(typeof(Wallet), nameof(Wallet.Balance), 28, 8)]
        [InlineData(typeof(Transaction), nameof(Transaction.TotalPrice), 28, 8)]
        public void DecimalColumnsHaveAnExplicitPrecision(Type entityType, string propertyName, int precision, int scale)
        {
            using var context = TestContextFactory.CreateContext();

            var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

            Assert.Equal(precision, property.GetPrecision());
            Assert.Equal(scale, property.GetScale());
        }

        [Fact]
        public void WalletCarriesAConcurrencyToken()
        {
            using var context = TestContextFactory.CreateContext();

            var property = context.Model.FindEntityType(typeof(Wallet))!.FindProperty(nameof(Wallet.RowVersion))!;

            Assert.True(property.IsConcurrencyToken);
        }

        [Theory]
        [InlineData(nameof(User.Email))]
        [InlineData(nameof(User.Username))]
        public void UserIdentifiersAreUnique(string propertyName)
        {
            using var context = TestContextFactory.CreateContext();

            var index = context.Model.FindEntityType(typeof(User))!
                .GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);

            Assert.NotNull(index);
            Assert.True(index!.IsUnique);
        }

        [Fact]
        public void DeletingACurrencyDoesNotCascadeIntoPortfolios()
        {
            using var context = TestContextFactory.CreateContext();

            var foreignKey = context.Model.FindEntityType(typeof(PortfolioItem))!
                .GetForeignKeys()
                .Single(fk => fk.PrincipalEntityType.ClrType == typeof(CryptoCurrency));

            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }
    }
}
