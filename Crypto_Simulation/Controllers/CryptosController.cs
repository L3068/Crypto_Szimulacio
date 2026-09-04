using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    /// <summary>
    /// Market data. Reads are public; changing the traded instruments is an admin operation.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class CryptosController : ApiControllerBase
    {
        private readonly ICryptoService _cryptoService;

        public CryptosController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        /// <summary>Lists every tradable cryptocurrency with its current price.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CryptoResponseDto>>> GetAllCryptos()
        {
            return Ok(await _cryptoService.GetAllCryptosAsync());
        }

        /// <summary>Returns a single cryptocurrency.</summary>
        [HttpGet("{cryptoId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoResponseDto>> GetCrypto(int cryptoId)
        {
            return Ok(await _cryptoService.GetCryptoByIdAsync(cryptoId));
        }

        /// <summary>Recorded prices, newest first internally but returned oldest-first for charting.</summary>
        [HttpGet("price/history/{cryptoId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<PriceHistoryDto>>> GetPriceHistory(
            int cryptoId, [FromQuery] PriceHistoryQueryDto query)
        {
            return Ok(await _cryptoService.GetPriceHistoryAsync(cryptoId, query));
        }

        /// <summary>Adds a new tradable cryptocurrency. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CryptoResponseDto>> CreateCrypto(CryptoCreateDto cryptoDto)
        {
            var result = await _cryptoService.CreateCryptoAsync(cryptoDto);
            return CreatedAtAction(nameof(GetCrypto), new { cryptoId = result.CryptoId }, result);
        }

        /// <summary>Sets a price manually and records it in the history. Admin only.</summary>
        [HttpPut("price")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoResponseDto>> UpdatePrice(CryptoPriceUpdateDto priceUpdateDto)
        {
            return Ok(await _cryptoService.UpdateCryptoPriceAsync(priceUpdateDto));
        }

        /// <summary>
        /// Removes a cryptocurrency. Refused while anyone still holds it or has traded it,
        /// so deleting cannot silently wipe portfolios. Admin only.
        /// </summary>
        [HttpDelete("{cryptoId:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteCrypto(int cryptoId)
        {
            await _cryptoService.DeleteCryptoAsync(cryptoId);
            return NoContent();
        }
    }
}
