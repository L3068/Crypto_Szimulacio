using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CryptosController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;

        public CryptosController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CryptoResponseDto>>> GetAllCryptos()
        {
            var cryptos = await _cryptoService.GetAllCryptosAsync();
            return Ok(cryptos);
        }

        [HttpGet("{cryptoId}")]
        public async Task<ActionResult<CryptoResponseDto>> GetCrypto(int cryptoId)
        {
            try
            {
                var crypto = await _cryptoService.GetCryptoByIdAsync(cryptoId);
                return Ok(crypto);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<CryptoResponseDto>> CreateCrypto(CryptoCreateDto cryptoDto)
        {
            try
            {
                var result = await _cryptoService.CreateCryptoAsync(cryptoDto);
                return CreatedAtAction(nameof(GetCrypto), new { cryptoId = result.CryptoId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{cryptoId}")]
        public async Task<ActionResult> DeleteCrypto(int cryptoId)
        {
            try
            {
                await _cryptoService.DeleteCryptoAsync(cryptoId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("price")]
        public async Task<ActionResult<CryptoResponseDto>> UpdatePrice(CryptoPriceUpdateDto priceUpdateDto)
        {
            try
            {
                var result = await _cryptoService.UpdateCryptoPriceAsync(priceUpdateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("price/history/{cryptoId}")]
        public async Task<ActionResult<List<PriceHistory>>> GetPriceHistory(int cryptoId)
        {
            try
            {
                var history = await _cryptoService.GetPriceHistoryAsync(cryptoId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
