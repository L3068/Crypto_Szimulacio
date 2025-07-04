using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.Services
{
    public interface IWalletService
    {
        Task<WalletResponseDto> GetWalletByUserIdAsync(int userId);
        Task<WalletResponseDto> UpdateWalletBalanceAsync(int userId, WalletUpdateDto walletDto);
        Task DeleteWalletAsync(int userId);
    }

    // Services/WalletService.cs
    public class WalletService : IWalletService
    {
        private readonly AppDbContext _context;

        public WalletService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WalletResponseDto> GetWalletByUserIdAsync(int userId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(pi => pi.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            var walletDto = new WalletResponseDto
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                Cryptos = wallet.PortfolioItems.Select(wc => new WalletCryptoDto
                {
                    CryptoId = wc.CryptoId,
                    Name = wc.CryptoCurrency.Name,
                    Symbol = wc.CryptoCurrency.Symbol,
                    Amount = wc.Quantity,
                    AverageBuyPrice = wc.AveragePrice,
                    CurrentPrice = wc.CryptoCurrency.CurrentPrice,
                    CurrentValue = wc.Quantity * wc.CryptoCurrency.CurrentPrice,
                    ProfitLoss = wc.Quantity * (wc.CryptoCurrency.CurrentPrice - wc.AveragePrice),
                    ProfitLossPercentage = wc.AveragePrice > 0 ?
                        ((wc.CryptoCurrency.CurrentPrice - wc.AveragePrice) / wc.AveragePrice) * 100 : 0
                }).ToList()
            };

            return walletDto;
        }

        public async Task<WalletResponseDto> UpdateWalletBalanceAsync(int userId, WalletUpdateDto walletDto)
        {
            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(wc => wc.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            wallet.Balance = walletDto.Balance;
            await _context.SaveChangesAsync();

            return await GetWalletByUserIdAsync(userId);
        }

        public async Task DeleteWalletAsync(int userId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            var walletCryptos = await _context.PortfolioItems
                .Where(wc => wc.WalletId == wallet.Id)
                .ToListAsync();

            _context.PortfolioItems.RemoveRange(walletCryptos);
            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();
        }
    }
}
