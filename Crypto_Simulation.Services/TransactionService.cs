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
    public interface ITransactionService
    {
        Task<List<TransactionResponseDto>> GetUserTransactionsAsync(int userId);
        Task<TransactionResponseDto> GetTransactionDetailsAsync(int transactionId);
    }

    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TransactionResponseDto>> GetUserTransactionsAsync(int userId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.CryptoCurrency)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            return transactions.Select(t => new TransactionResponseDto
            {
                TransactionId = t.Id,
                UserId = t.UserId,
                Username = t.User?.Username ?? string.Empty,
                CryptoId = t.CryptoId,
                CryptoName = t.CryptoCurrency?.Name ?? string.Empty,
                CryptoSymbol = t.CryptoCurrency?.Symbol ?? string.Empty,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                PricePerUnit = t.PricePerUnit,
                TotalPrice = t.TotalPrice,
                Timestamp = t.Timestamp
            }).ToList();
        }

        public async Task<TransactionResponseDto> GetTransactionDetailsAsync(int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.CryptoCurrency)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }

            return new TransactionResponseDto
            {
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Username = transaction.User?.Username ?? string.Empty,
                CryptoId = transaction.CryptoId,
                CryptoName = transaction.CryptoCurrency?.Name ?? string.Empty,
                CryptoSymbol = transaction.CryptoCurrency?.Symbol ?? string.Empty,
                Type = transaction.Type.ToString(),
                Quantity = transaction.Quantity,
                PricePerUnit = transaction.PricePerUnit,
                TotalPrice = transaction.TotalPrice,
                Timestamp = transaction.Timestamp
            };
        }
    }
}
