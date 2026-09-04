using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;

namespace Crypto_Simulation.Services
{
    /// <summary>
    /// Shared wallet projection. TradeService and WalletService both expose the portfolio and
    /// previously carried two copies of this mapping.
    /// </summary>
    internal static class PortfolioMapper
    {
        public static WalletResponseDto ToDto(Wallet wallet) => new()
        {
            WalletId = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            Cryptos = wallet.PortfolioItems.Select(item => new WalletCryptoDto
            {
                CryptoId = item.CryptoId,
                Name = item.CryptoCurrency.Name,
                Symbol = item.CryptoCurrency.Symbol,
                Amount = item.Quantity,
                AverageBuyPrice = item.AveragePrice,
                CurrentPrice = item.CryptoCurrency.CurrentPrice,
                CurrentValue = item.Quantity * item.CryptoCurrency.CurrentPrice,
                ProfitLoss = item.Quantity * (item.CryptoCurrency.CurrentPrice - item.AveragePrice),
                ProfitLossPercentage = item.AveragePrice > 0
                    ? ((item.CryptoCurrency.CurrentPrice - item.AveragePrice) / item.AveragePrice) * 100
                    : 0
            }).ToList()
        };
    }
}
