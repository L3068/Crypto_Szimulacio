using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    /// <summary>
    /// Trading endpoints. The acting user always comes from the bearer token, so a caller
    /// cannot trade on somebody else's wallet.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class TradeController : ApiControllerBase
    {
        private readonly ITradeService _tradeService;

        public TradeController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        /// <summary>Buys a quantity of a cryptocurrency at its current price.</summary>
        [HttpPost("buy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TransactionResponseDto>> BuyCrypto(TradeRequestDto tradeRequest)
        {
            return Ok(await _tradeService.BuyCryptoAsync(CurrentUserId, tradeRequest));
        }

        /// <summary>Sells a quantity of a held cryptocurrency at its current price.</summary>
        [HttpPost("sell")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TransactionResponseDto>> SellCrypto(TradeRequestDto tradeRequest)
        {
            return Ok(await _tradeService.SellCryptoAsync(CurrentUserId, tradeRequest));
        }

        /// <summary>Swaps one held cryptocurrency for another at current prices.</summary>
        [HttpPost("convert")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TransactionResponseDto>> ConvertCrypto(ConvertRequestDto convertRequest)
        {
            return Ok(await _tradeService.ConvertCryptoAsync(CurrentUserId, convertRequest));
        }

        /// <summary>Current holdings with per-position profit and loss.</summary>
        [HttpGet("portfolio")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<WalletResponseDto>> GetOwnPortfolio()
        {
            return Ok(await _tradeService.GetPortfolioAsync(CurrentUserId));
        }

        /// <summary>Holdings of a given user. Only the owner or an admin may read them.</summary>
        [HttpGet("portfolio/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletResponseDto>> GetPortfolio(int userId)
        {
            EnsureCanAccess(userId);
            return Ok(await _tradeService.GetPortfolioAsync(userId));
        }
    }
}
