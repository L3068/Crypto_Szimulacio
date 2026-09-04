using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Simulation.Services
{
    public interface ICryptoService
    {
        Task<List<CryptoResponseDto>> GetAllCryptosAsync();
        Task<CryptoResponseDto> GetCryptoByIdAsync(int cryptoId);
        Task<CryptoResponseDto> CreateCryptoAsync(CryptoCreateDto cryptoDto);
        Task DeleteCryptoAsync(int cryptoId);
        Task<CryptoResponseDto> UpdateCryptoPriceAsync(CryptoPriceUpdateDto priceUpdateDto);
        Task<List<PriceHistoryDto>> GetPriceHistoryAsync(int cryptoId, PriceHistoryQueryDto query);
    }

    public class CryptoService : ICryptoService
    {
        private readonly AppDbContext _context;

        public CryptoService(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<CryptoResponseDto>> GetAllCryptosAsync() =>
            // Projected in the query rather than materialising every entity first.
            _context.CryptoCurrencies
                .AsNoTracking()
                .OrderBy(c => c.Symbol)
                .Select(c => new CryptoResponseDto
                {
                    CryptoId = c.Id,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    CurrentPrice = c.CurrentPrice,
                    TotalSupply = c.TotalSupply
                })
                .ToListAsync();

        public async Task<CryptoResponseDto> GetCryptoByIdAsync(int cryptoId)
        {
            var crypto = await _context.CryptoCurrencies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cryptoId)
                ?? throw NotFoundException.For("Cryptocurrency", cryptoId);

            return ToDto(crypto);
        }

        public async Task<CryptoResponseDto> CreateCryptoAsync(CryptoCreateDto cryptoDto)
        {
            var symbol = cryptoDto.Symbol.Trim().ToUpperInvariant();

            if (await _context.CryptoCurrencies.AnyAsync(c => c.Symbol == symbol))
            {
                throw new ConflictException($"A cryptocurrency with symbol '{symbol}' already exists.");
            }

            var crypto = new CryptoCurrency
            {
                Name = cryptoDto.Name.Trim(),
                Symbol = symbol,
                CurrentPrice = cryptoDto.CurrentPrice,
                TotalSupply = cryptoDto.TotalSupply
            };

            _context.CryptoCurrencies.Add(crypto);

            // Seed the history so charts have a starting point.
            _context.PriceHistories.Add(new PriceHistory
            {
                CryptoCurrency = crypto,
                Price = crypto.CurrentPrice,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ToDto(crypto);
        }

        public async Task DeleteCryptoAsync(int cryptoId)
        {
            var crypto = await _context.CryptoCurrencies.FindAsync(cryptoId)
                ?? throw NotFoundException.For("Cryptocurrency", cryptoId);

            // Deleting used to cascade straight through the portfolios, wiping people's holdings
            // without refunding anything. Refuse instead.
            if (await _context.PortfolioItems.AnyAsync(pi => pi.CryptoId == cryptoId))
            {
                throw new ConflictException(
                    $"'{crypto.Symbol}' is still held in at least one portfolio and cannot be deleted.");
            }

            if (await _context.Transactions.AnyAsync(t => t.CryptoId == cryptoId))
            {
                throw new ConflictException(
                    $"'{crypto.Symbol}' has transaction history and cannot be deleted.");
            }

            _context.CryptoCurrencies.Remove(crypto);
            await _context.SaveChangesAsync();
        }

        public async Task<CryptoResponseDto> UpdateCryptoPriceAsync(CryptoPriceUpdateDto priceUpdateDto)
        {
            if (priceUpdateDto.NewPrice <= 0)
            {
                throw new ValidationException("The price must be greater than zero.");
            }

            var crypto = await _context.CryptoCurrencies.FindAsync(priceUpdateDto.CryptoId)
                ?? throw NotFoundException.For("Cryptocurrency", priceUpdateDto.CryptoId);

            crypto.CurrentPrice = priceUpdateDto.NewPrice;

            _context.PriceHistories.Add(new PriceHistory
            {
                CryptoId = crypto.Id,
                Price = priceUpdateDto.NewPrice,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ToDto(crypto);
        }

        public async Task<List<PriceHistoryDto>> GetPriceHistoryAsync(int cryptoId, PriceHistoryQueryDto query)
        {
            if (!await _context.CryptoCurrencies.AnyAsync(c => c.Id == cryptoId))
            {
                throw NotFoundException.For("Cryptocurrency", cryptoId);
            }

            var history = _context.PriceHistories.AsNoTracking().Where(ph => ph.CryptoId == cryptoId);

            if (query.FromUtc.HasValue)
            {
                history = history.Where(ph => ph.Timestamp >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                history = history.Where(ph => ph.Timestamp <= query.ToUtc.Value);
            }

            // Take the most recent rows, then hand them back oldest-first for charting.
            var rows = await history
                .OrderByDescending(ph => ph.Timestamp)
                .Take(query.Limit)
                .Select(ph => new PriceHistoryDto
                {
                    CryptoId = ph.CryptoId,
                    Price = ph.Price,
                    TimestampUtc = ph.Timestamp
                })
                .ToListAsync();

            rows.Reverse();
            return rows;
        }

        private static CryptoResponseDto ToDto(CryptoCurrency crypto) => new()
        {
            CryptoId = crypto.Id,
            Name = crypto.Name,
            Symbol = crypto.Symbol,
            CurrentPrice = crypto.CurrentPrice,
            TotalSupply = crypto.TotalSupply
        };
    }
}
