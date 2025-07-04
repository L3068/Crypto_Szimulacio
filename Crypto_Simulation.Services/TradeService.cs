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
    public interface ITradeService
    {
        Task<TransactionResponseDto> BuyCryptoAsync(TradeRequestDto tradeRequest);
        Task<TransactionResponseDto> SellCryptoAsync(TradeRequestDto tradeRequest);
        Task<TransactionResponseDto> ConvertCryptoAsync (ConvertRequestDto convertRequest);
        Task<WalletResponseDto> GetPortfolioAsync(int userId);
    }

    public class TradeService : ITradeService
    {
        private readonly AppDbContext _context;

        public TradeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionResponseDto> BuyCryptoAsync(TradeRequestDto tradeRequest)
        {
            if (tradeRequest.Quantity <= 0)
            {
                throw new Exception("Amount must be greater than zero");
            }

            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .FirstOrDefaultAsync(w => w.UserId == tradeRequest.UserId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            var crypto = await _context.CryptoCurrencies.FindAsync(tradeRequest.CryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            decimal totalCost = tradeRequest.Quantity * crypto.CurrentPrice;

            if (wallet.Balance < totalCost)
            {
                throw new Exception("Insufficient balance");
            }

            wallet.Balance -= totalCost;

            var walletCrypto = await _context.PortfolioItems
                .FirstOrDefaultAsync(wc => wc.WalletId == wallet.Id && wc.CryptoId == crypto.Id);

            if (walletCrypto == null)
            {
                walletCrypto = new PortfolioItem
                {
                    WalletId = wallet.Id,
                    CryptoId = crypto.Id,
                    Quantity = tradeRequest.Quantity,
                    AveragePrice = crypto.CurrentPrice
                };
                _context.PortfolioItems.Add(walletCrypto);
            }
            else
            {
                decimal totalValue = (walletCrypto.Quantity * walletCrypto.AveragePrice) + totalCost;
                walletCrypto.Quantity += tradeRequest.Quantity;
                walletCrypto.AveragePrice = totalValue / walletCrypto.Quantity;
            }

            var transaction = new Transaction
            {
                UserId = tradeRequest.UserId,
                CryptoId = tradeRequest.CryptoId,
                Type = TransactionType.Buy,
                Quantity = tradeRequest.Quantity,
                PricePerUnit = crypto.CurrentPrice,
                TotalPrice = totalCost,
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return new TransactionResponseDto
            {
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Username = (await _context.Users.FindAsync(transaction.UserId))?.Username ?? string.Empty,
                CryptoId = transaction.CryptoId,
                CryptoName = crypto.Name,
                CryptoSymbol = crypto.Symbol,
                Type = "Buy",
                Quantity = transaction.Quantity,
                PricePerUnit = transaction.PricePerUnit,
                TotalPrice = transaction.TotalPrice,
                Timestamp = transaction.Timestamp
            };
        }

        public async Task<TransactionResponseDto> SellCryptoAsync(TradeRequestDto tradeRequest)
        {
            if (tradeRequest.Quantity <= 0)
            {
                throw new Exception("Amount must be greater than zero");
            }

            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .FirstOrDefaultAsync(w => w.UserId == tradeRequest.UserId);

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            var crypto = await _context.CryptoCurrencies.FindAsync(tradeRequest.CryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            var walletCrypto = await _context.PortfolioItems
                .FirstOrDefaultAsync(wc => wc.WalletId == wallet.Id && wc.CryptoId == crypto.Id);

            if (walletCrypto == null || walletCrypto.Quantity < tradeRequest.Quantity)
            {
                throw new Exception("Insufficient cryptocurrency amount");
            }

            decimal totalSale = tradeRequest.Quantity * crypto.CurrentPrice;

            wallet.Balance += totalSale;

            walletCrypto.Quantity -= tradeRequest.Quantity;

            if (walletCrypto.Quantity == 0)
            {
                _context.PortfolioItems.Remove(walletCrypto);
            }

            var transaction = new Transaction
            {
                UserId = tradeRequest.UserId,
                CryptoId = tradeRequest.CryptoId,
                Type = TransactionType.Sell,
                Quantity = tradeRequest.Quantity,
                PricePerUnit = crypto.CurrentPrice,
                TotalPrice = totalSale,
                Timestamp = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return new TransactionResponseDto
            {
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Username = (await _context.Users.FindAsync(transaction.UserId))?.Username ?? string.Empty,
                CryptoId = transaction.CryptoId,
                CryptoName = crypto.Name,
                CryptoSymbol = crypto.Symbol,
                Type = "Sell",
                Quantity = transaction.Quantity,
                PricePerUnit = transaction.PricePerUnit,
                TotalPrice = transaction.TotalPrice,
                Timestamp = transaction.Timestamp
            };
        }

        public async Task<TransactionResponseDto> ConvertCryptoAsync(ConvertRequestDto convertRequest)
        {
            if (convertRequest.Quantity <= 0)
                throw new Exception("Amount must be greater than zero");

            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .FirstOrDefaultAsync(w => w.UserId == convertRequest.UserId);

            if (wallet == null)
                throw new Exception("Wallet not found");

            var sourceCrypto = await _context.CryptoCurrencies.FindAsync(convertRequest.CryptoId);
            var targetCrypto = await _context.CryptoCurrencies.FindAsync(convertRequest.TargetCryptoId);

            if (sourceCrypto == null || targetCrypto == null)
                throw new Exception("One or both cryptocurrencies not found");

            var sourcePortfolioItem = wallet.PortfolioItems
                .FirstOrDefault(p => p.CryptoId == sourceCrypto.Id);

            if (sourcePortfolioItem == null || sourcePortfolioItem.Quantity < convertRequest.Quantity)
                throw new Exception("Insufficient source cryptocurrency");

            decimal totalValue = convertRequest.Quantity * sourceCrypto.CurrentPrice;
            decimal targetAmount = totalValue / targetCrypto.CurrentPrice;

            sourcePortfolioItem.Quantity -= convertRequest.Quantity;
            if (sourcePortfolioItem.Quantity == 0)
                _context.PortfolioItems.Remove(sourcePortfolioItem);

            var targetPortfolioItem = wallet.PortfolioItems
                .FirstOrDefault(p => p.CryptoId == targetCrypto.Id);

            if (targetPortfolioItem == null)
            {
                targetPortfolioItem = new PortfolioItem
                {
                    WalletId = wallet.Id,
                    CryptoId = targetCrypto.Id,
                    Quantity = targetAmount
                };
                _context.PortfolioItems.Add(targetPortfolioItem);
            }
            else
            {
                targetPortfolioItem.Quantity += targetAmount;
            }

            var transaction = new Transaction
            {
                UserId = wallet.UserId,
                CryptoId = targetCrypto.Id,
                Type = TransactionType.Buy,
                Quantity = targetAmount,
                PricePerUnit = targetCrypto.CurrentPrice,
                TotalPrice = totalValue,
                Timestamp = DateTime.Now
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return new TransactionResponseDto
            {
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Username = (await _context.Users.FindAsync(transaction.UserId))?.Username ?? string.Empty,
                CryptoId = transaction.CryptoId,
                CryptoName = targetCrypto.Name,
                CryptoSymbol = targetCrypto.Symbol,
                Type = "Convert",
                Quantity = transaction.Quantity,
                PricePerUnit = transaction.PricePerUnit,
                TotalPrice = transaction.TotalPrice,
                Timestamp = transaction.Timestamp
            };
        }


        public async Task<WalletResponseDto> GetPortfolioAsync(int userId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.PortfolioItems)
                .ThenInclude(wc => wc.CryptoCurrency)
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
    }
}
