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
    public interface ICryptoService
    {
        Task<List<CryptoResponseDto>> GetAllCryptosAsync();
        Task<CryptoResponseDto> GetCryptoByIdAsync(int cryptoId);
        Task<CryptoResponseDto> CreateCryptoAsync(CryptoCreateDto cryptoDto);
        Task DeleteCryptoAsync(int cryptoId);
        Task<CryptoResponseDto> UpdateCryptoPriceAsync(CryptoPriceUpdateDto priceUpdateDto);
        Task<List<PriceHistory>> GetPriceHistoryAsync(int cryptoId);
    }

    public class CryptoService : ICryptoService
    {
        private readonly AppDbContext _context;

        public CryptoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CryptoResponseDto>> GetAllCryptosAsync()
        {
            var cryptos = await _context.CryptoCurrencies.ToListAsync();
            return cryptos.Select(c => new CryptoResponseDto
            {
                CryptoId = c.Id,
                Name = c.Name,
                Symbol = c.Symbol,
                CurrentPrice = c.CurrentPrice,
                TotalSupply = c.TotalSupply
            }).ToList();
        }

        public async Task<CryptoResponseDto> GetCryptoByIdAsync(int cryptoId)
        {
            var crypto = await _context.CryptoCurrencies.FindAsync(cryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            return new CryptoResponseDto
            {
                CryptoId = crypto.Id,
                Name = crypto.Name,
                Symbol = crypto.Symbol,
                CurrentPrice = crypto.CurrentPrice,
                TotalSupply = crypto.TotalSupply
            };
        }

        public async Task<CryptoResponseDto> CreateCryptoAsync(CryptoCreateDto cryptoDto)
        {
            var crypto = new CryptoCurrency
            {
                Name = cryptoDto.Name,
                Symbol = cryptoDto.Symbol,
                CurrentPrice = cryptoDto.CurrentPrice,
                TotalSupply = cryptoDto.TotalSupply
            };

            _context.CryptoCurrencies.Add(crypto);
            await _context.SaveChangesAsync();

            return new CryptoResponseDto
            {
                CryptoId = crypto.Id,
                Name = crypto.Name,
                Symbol = crypto.Symbol,
                CurrentPrice = crypto.CurrentPrice,
                TotalSupply = crypto.TotalSupply
            };
        }

        public async Task DeleteCryptoAsync(int cryptoId)
        {
            var crypto = await _context.CryptoCurrencies.FindAsync(cryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            _context.CryptoCurrencies.Remove(crypto);
            await _context.SaveChangesAsync();
        }

        public async Task<CryptoResponseDto> UpdateCryptoPriceAsync(CryptoPriceUpdateDto priceUpdateDto)
        {
            var crypto = await _context.CryptoCurrencies.FindAsync(priceUpdateDto.CryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            crypto.CurrentPrice = priceUpdateDto.NewPrice;

            var priceHistory = new PriceHistory
            {
                CryptoId = crypto.Id,
                Price = priceUpdateDto.NewPrice,
                Timestamp = DateTime.Now
            };

            _context.PriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync();

            return new CryptoResponseDto
            {
                CryptoId = crypto.Id,
                Name = crypto.Name,
                Symbol = crypto.Symbol,
                CurrentPrice = crypto.CurrentPrice,
                TotalSupply = crypto.TotalSupply
            };
        }

        public async Task<List<PriceHistory>> GetPriceHistoryAsync(int cryptoId)
        {
            var crypto = await _context.CryptoCurrencies.FindAsync(cryptoId);
            if (crypto == null)
            {
                throw new Exception("Cryptocurrency not found");
            }

            return await _context.PriceHistories
                .Where(ph => ph.CryptoId == cryptoId)
                .OrderBy(ph => ph.Timestamp)
                .ToListAsync();
        }
    }
}
