using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crypto_Simulation.Services
{
    public interface ITradeService
    {
        Task<TransactionResponseDto> BuyCryptoAsync(int userId, TradeRequestDto tradeRequest);
        Task<TransactionResponseDto> SellCryptoAsync(int userId, TradeRequestDto tradeRequest);
        Task<TransactionResponseDto> ConvertCryptoAsync(int userId, ConvertRequestDto convertRequest);
        Task<WalletResponseDto> GetPortfolioAsync(int userId);
    }

    public class TradeService : ITradeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TradeService> _logger;

        public TradeService(AppDbContext context, ILogger<TradeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TransactionResponseDto> BuyCryptoAsync(int userId, TradeRequestDto tradeRequest)
        {
            EnsurePositive(tradeRequest.Quantity);

            var wallet = await LoadWalletAsync(userId);
            var crypto = await FindCryptoAsync(tradeRequest.CryptoId);

            decimal totalCost = tradeRequest.Quantity * crypto.CurrentPrice;

            if (wallet.Balance < totalCost)
            {
                throw new ValidationException(
                    $"Insufficient balance: the order costs {totalCost} but the wallet holds {wallet.Balance}.");
            }

            wallet.Balance -= totalCost;

            var item = wallet.PortfolioItems.FirstOrDefault(pi => pi.CryptoId == crypto.Id);
            if (item is null)
            {
                item = new PortfolioItem
                {
                    WalletId = wallet.Id,
                    CryptoId = crypto.Id,
                    Quantity = tradeRequest.Quantity,
                    AveragePrice = crypto.CurrentPrice
                };
                _context.PortfolioItems.Add(item);
            }
            else
            {
                AddToPosition(item, tradeRequest.Quantity, totalCost);
            }

            var transaction = NewTransaction(
                userId, crypto.Id, TransactionType.Buy,
                tradeRequest.Quantity, crypto.CurrentPrice, totalCost);

            _context.Transactions.Add(transaction);
            await SaveWithConcurrencyCheckAsync();

            _logger.LogInformation(
                "User {UserId} bought {Quantity} {Symbol} for {Total}",
                userId, tradeRequest.Quantity, crypto.Symbol, totalCost);

            return ToDto(transaction, wallet.User.Username, crypto);
        }

        public async Task<TransactionResponseDto> SellCryptoAsync(int userId, TradeRequestDto tradeRequest)
        {
            EnsurePositive(tradeRequest.Quantity);

            var wallet = await LoadWalletAsync(userId);
            var crypto = await FindCryptoAsync(tradeRequest.CryptoId);

            var item = wallet.PortfolioItems.FirstOrDefault(pi => pi.CryptoId == crypto.Id);
            if (item is null || item.Quantity < tradeRequest.Quantity)
            {
                throw new ValidationException(
                    $"Insufficient {crypto.Symbol}: tried to sell {tradeRequest.Quantity} but only {item?.Quantity ?? 0} is held.");
            }

            decimal totalSale = tradeRequest.Quantity * crypto.CurrentPrice;

            wallet.Balance += totalSale;
            RemoveFromPosition(item, tradeRequest.Quantity);

            var transaction = NewTransaction(
                userId, crypto.Id, TransactionType.Sell,
                tradeRequest.Quantity, crypto.CurrentPrice, totalSale);

            _context.Transactions.Add(transaction);
            await SaveWithConcurrencyCheckAsync();

            _logger.LogInformation(
                "User {UserId} sold {Quantity} {Symbol} for {Total}",
                userId, tradeRequest.Quantity, crypto.Symbol, totalSale);

            return ToDto(transaction, wallet.User.Username, crypto);
        }

        public async Task<TransactionResponseDto> ConvertCryptoAsync(int userId, ConvertRequestDto convertRequest)
        {
            EnsurePositive(convertRequest.Quantity);

            if (convertRequest.CryptoId == convertRequest.TargetCryptoId)
            {
                throw new ValidationException("The source and target cryptocurrency must differ.");
            }

            var wallet = await LoadWalletAsync(userId);
            var sourceCrypto = await FindCryptoAsync(convertRequest.CryptoId);
            var targetCrypto = await FindCryptoAsync(convertRequest.TargetCryptoId);

            var sourceItem = wallet.PortfolioItems.FirstOrDefault(pi => pi.CryptoId == sourceCrypto.Id);
            if (sourceItem is null || sourceItem.Quantity < convertRequest.Quantity)
            {
                throw new ValidationException(
                    $"Insufficient {sourceCrypto.Symbol}: tried to convert {convertRequest.Quantity} but only {sourceItem?.Quantity ?? 0} is held.");
            }

            decimal totalValue = convertRequest.Quantity * sourceCrypto.CurrentPrice;
            decimal targetAmount = totalValue / targetCrypto.CurrentPrice;

            RemoveFromPosition(sourceItem, convertRequest.Quantity);

            var targetItem = wallet.PortfolioItems.FirstOrDefault(pi => pi.CryptoId == targetCrypto.Id);
            if (targetItem is null)
            {
                targetItem = new PortfolioItem
                {
                    WalletId = wallet.Id,
                    CryptoId = targetCrypto.Id,
                    Quantity = targetAmount,
                    // Previously left at 0, which made the whole position look like pure profit.
                    AveragePrice = targetCrypto.CurrentPrice
                };
                _context.PortfolioItems.Add(targetItem);
            }
            else
            {
                AddToPosition(targetItem, targetAmount, totalValue);
            }

            var transaction = NewTransaction(
                userId, targetCrypto.Id, TransactionType.Convert,
                targetAmount, targetCrypto.CurrentPrice, totalValue);

            _context.Transactions.Add(transaction);
            await SaveWithConcurrencyCheckAsync();

            _logger.LogInformation(
                "User {UserId} converted {Quantity} {From} into {TargetAmount} {To}",
                userId, convertRequest.Quantity, sourceCrypto.Symbol, targetAmount, targetCrypto.Symbol);

            return ToDto(transaction, wallet.User.Username, targetCrypto);
        }

        public async Task<WalletResponseDto> GetPortfolioAsync(int userId)
        {
            var wallet = await _context.Wallets
                .AsNoTracking()
                .Include(w => w.PortfolioItems)
                .ThenInclude(pi => pi.CryptoCurrency)
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ?? throw NotFoundException.For("Wallet for user", userId);

            return PortfolioMapper.ToDto(wallet);
        }

        private async Task<Wallet> LoadWalletAsync(int userId) =>
            await _context.Wallets
                .Include(w => w.User)
                .Include(w => w.PortfolioItems)
                .FirstOrDefaultAsync(w => w.UserId == userId)
            ?? throw NotFoundException.For("Wallet for user", userId);

        private async Task<CryptoCurrency> FindCryptoAsync(int cryptoId) =>
            await _context.CryptoCurrencies.FindAsync(cryptoId)
            ?? throw NotFoundException.For("Cryptocurrency", cryptoId);

        private static void EnsurePositive(decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new ValidationException("Quantity must be greater than zero.");
            }
        }

        /// <summary>Adds units to a position and rolls the volume weighted average price forward.</summary>
        private static void AddToPosition(PortfolioItem item, decimal quantity, decimal cost)
        {
            decimal previousCost = item.Quantity * item.AveragePrice;
            item.Quantity += quantity;
            item.AveragePrice = item.Quantity > 0 ? (previousCost + cost) / item.Quantity : 0;
        }

        /// <summary>
        /// Removes units from a position. The average price is deliberately unchanged: selling part
        /// of a holding realises profit but does not alter what the remaining units cost.
        /// </summary>
        private void RemoveFromPosition(PortfolioItem item, decimal quantity)
        {
            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _context.PortfolioItems.Remove(item);
            }
        }

        private static Transaction NewTransaction(
            int userId, int cryptoId, TransactionType type,
            decimal quantity, decimal pricePerUnit, decimal totalPrice) => new()
            {
                UserId = userId,
                CryptoId = cryptoId,
                Type = type,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                TotalPrice = totalPrice,
                Timestamp = DateTime.UtcNow
            };

        private async Task SaveWithConcurrencyCheckAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The wallet was modified by another request. Please retry the operation.");
            }
        }

        private static TransactionResponseDto ToDto(Transaction transaction, string username, CryptoCurrency crypto) => new()
        {
            TransactionId = transaction.Id,
            UserId = transaction.UserId,
            Username = username,
            CryptoId = transaction.CryptoId,
            CryptoName = crypto.Name,
            CryptoSymbol = crypto.Symbol,
            Type = transaction.Type.ToString(),
            Quantity = transaction.Quantity,
            PricePerUnit = transaction.PricePerUnit,
            TotalPrice = transaction.TotalPrice,
            TimestampUtc = transaction.Timestamp
        };
    }
}
