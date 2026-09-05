using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Simulation.Services
{
    public interface IProfitService
    {
        Task<ProfitResponseDto> CalculateProfitAsync(int userId);
        Task<ProfitDetailResponseDto> CalculateDetailedProfitAsync(int userId);
    }

    public class ProfitService : IProfitService
    {
        private readonly AppDbContext _context;

        public ProfitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProfitResponseDto> CalculateProfitAsync(int userId)
        {
            var items = await LoadPositionsAsync(userId);

            decimal totalInvestment = items.Sum(i => i.Quantity * i.AveragePrice);
            decimal totalValue = items.Sum(i => i.Quantity * i.CryptoCurrency.CurrentPrice);
            decimal totalProfitLoss = totalValue - totalInvestment;

            return new ProfitResponseDto
            {
                UserId = userId,
                TotalInvestment = totalInvestment,
                TotalValue = totalValue,
                TotalProfitLoss = totalProfitLoss,
                TotalProfitLossPercentage = totalInvestment > 0
                    ? (totalProfitLoss / totalInvestment) * 100
                    : 0
            };
        }

        public async Task<ProfitDetailResponseDto> CalculateDetailedProfitAsync(int userId)
        {
            var items = await LoadPositionsAsync(userId);

            var details = items.Select(item =>
            {
                decimal investment = item.Quantity * item.AveragePrice;
                decimal currentValue = item.Quantity * item.CryptoCurrency.CurrentPrice;
                decimal profitLoss = currentValue - investment;

                return new CryptoProfitDto
                {
                    CryptoId = item.CryptoId,
                    Name = item.CryptoCurrency.Name,
                    Symbol = item.CryptoCurrency.Symbol,
                    Amount = item.Quantity,
                    AverageBuyPrice = item.AveragePrice,
                    CurrentPrice = item.CryptoCurrency.CurrentPrice,
                    Investment = investment,
                    CurrentValue = currentValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = investment > 0 ? (profitLoss / investment) * 100 : 0
                };
            }).ToList();

            return new ProfitDetailResponseDto
            {
                UserId = userId,
                Details = details,
                TotalProfitLoss = details.Sum(d => d.ProfitLoss)
            };
        }

        private async Task<List<PortfolioItem>> LoadPositionsAsync(int userId)
        {
            var wallet = await _context.Wallets
                .AsNoTracking()
                .Include(w => w.PortfolioItems)
                .ThenInclude(pi => pi.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw NotFoundException.For("Wallet for user", userId);

            return wallet.PortfolioItems;
        }
    }
}
