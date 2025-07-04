using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(wc => wc.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            decimal totalInvestment = 0;
            decimal totalValue = 0;

            foreach (var wc in wallet.PortfolioItems)
            {
                totalInvestment += wc.Quantity * wc.AveragePrice;
                totalValue += wc.Quantity * wc.CryptoCurrency.CurrentPrice;
            }

            decimal totalProfitLoss = totalValue - totalInvestment;
            decimal totalProfitLossPercentage = totalInvestment > 0 ?
                (totalProfitLoss / totalInvestment) * 100 : 0;

            return new ProfitResponseDto
            {
                UserId = userId,
                TotalInvestment = totalInvestment,
                TotalValue = totalValue,
                TotalProfitLoss = totalProfitLoss,
                TotalProfitLossPercentage = totalProfitLossPercentage
            };
        }

        public async Task<ProfitDetailResponseDto> CalculateDetailedProfitAsync(int userId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(wc => wc.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            var details = new List<CryptoProfitDto>();
            decimal totalProfitLoss = 0;

            foreach (var wc in wallet.PortfolioItems)
            {
                decimal investment = wc.Quantity * wc.AveragePrice;
                decimal currentValue = wc.Quantity * wc.CryptoCurrency.CurrentPrice;
                decimal profitLoss = currentValue - investment;
                decimal profitLossPercentage = investment > 0 ?
                    (profitLoss / investment) * 100 : 0;

                details.Add(new CryptoProfitDto
                {
                    CryptoId = wc.CryptoId,
                    Name = wc.CryptoCurrency.Name,
                    Symbol = wc.CryptoCurrency.Symbol,
                    Amount = wc.Quantity,
                    AverageBuyPrice = wc.AveragePrice,
                    CurrentPrice = wc.CryptoCurrency.CurrentPrice,
                    Investment = investment,
                    CurrentValue = currentValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = profitLossPercentage
                });

                totalProfitLoss += profitLoss;
            }

            return new ProfitDetailResponseDto
            {
                UserId = userId,
                Details = details,
                TotalProfitLoss = totalProfitLoss
            };
        }
    }
}
