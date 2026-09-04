using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Simulation.Services
{
    public interface ITransactionService
    {
        Task<List<TransactionResponseDto>> GetUserTransactionsAsync(int userId, int skip, int take);
        Task<TransactionResponseDto> GetTransactionDetailsAsync(int transactionId);

        /// <summary>Which user a transaction belongs to, so the caller can enforce ownership.</summary>
        Task<int?> GetOwnerIdAsync(int transactionId);
    }

    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<TransactionResponseDto>> GetUserTransactionsAsync(int userId, int skip, int take) =>
            _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Skip(skip)
                .Take(take)
                .Select(t => new TransactionResponseDto
                {
                    TransactionId = t.Id,
                    UserId = t.UserId,
                    Username = t.User.Username,
                    CryptoId = t.CryptoId,
                    CryptoName = t.CryptoCurrency.Name,
                    CryptoSymbol = t.CryptoCurrency.Symbol,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    PricePerUnit = t.PricePerUnit,
                    TotalPrice = t.TotalPrice,
                    TimestampUtc = t.Timestamp
                })
                .ToListAsync();

        public async Task<TransactionResponseDto> GetTransactionDetailsAsync(int transactionId)
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Id == transactionId)
                .Select(t => new TransactionResponseDto
                {
                    TransactionId = t.Id,
                    UserId = t.UserId,
                    Username = t.User.Username,
                    CryptoId = t.CryptoId,
                    CryptoName = t.CryptoCurrency.Name,
                    CryptoSymbol = t.CryptoCurrency.Symbol,
                    Type = t.Type.ToString(),
                    Quantity = t.Quantity,
                    PricePerUnit = t.PricePerUnit,
                    TotalPrice = t.TotalPrice,
                    TimestampUtc = t.Timestamp
                })
                .FirstOrDefaultAsync();

            return transaction ?? throw NotFoundException.For("Transaction", transactionId);
        }

        public async Task<int?> GetOwnerIdAsync(int transactionId)
        {
            var owner = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Id == transactionId)
                .Select(t => (int?)t.UserId)
                .FirstOrDefaultAsync();

            return owner;
        }
    }
}
