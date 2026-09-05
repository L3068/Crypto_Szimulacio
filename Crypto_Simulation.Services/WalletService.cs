using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Simulation.Services
{
    public interface IWalletService
    {
        Task<WalletResponseDto> GetWalletByUserIdAsync(int userId);
        Task<WalletResponseDto> UpdateWalletBalanceAsync(int userId, WalletUpdateDto walletDto);
        Task DeleteWalletAsync(int userId);
    }

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
                .AsNoTracking()
                .Include(w => w.PortfolioItems)
                .ThenInclude(pi => pi.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw NotFoundException.For("Wallet for user", userId);

            return PortfolioMapper.ToDto(wallet);
        }

        public async Task<WalletResponseDto> UpdateWalletBalanceAsync(int userId, WalletUpdateDto walletDto)
        {
            if (walletDto.Balance < 0)
            {
                throw new ValidationException("The balance cannot be negative.");
            }

            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(pi => pi.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw NotFoundException.For("Wallet for user", userId);

            wallet.Balance = walletDto.Balance;
            await _context.SaveChangesAsync();

            return PortfolioMapper.ToDto(wallet);
        }

        public async Task DeleteWalletAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw NotFoundException.For("Wallet for user", userId);

            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();
        }
    }
}
