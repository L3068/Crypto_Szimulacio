using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeController : ControllerBase
    {
        private readonly ITradeService _tradeService;

        public TradeController(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        [HttpPost("buy")]
        public async Task<ActionResult<TransactionResponseDto>> BuyCrypto(TradeRequestDto tradeRequest)
        {
            try
            {
                var result = await _tradeService.BuyCryptoAsync(tradeRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("sell")]
        public async Task<ActionResult<TransactionResponseDto>> SellCrypto(TradeRequestDto tradeRequest)
        {
            try
            {
                var result = await _tradeService.SellCryptoAsync(tradeRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("convert")]
        public async Task<ActionResult<TransactionResponseDto>> ConvertCrypto(ConvertRequestDto ConvertRequest)
        {
            try
            {
                var result = await _tradeService.ConvertCryptoAsync(ConvertRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("portfolio/{userId}")]
        public async Task<ActionResult<WalletResponseDto>> GetPortfolio(int userId)
        {
            try
            {
                var portfolio = await _tradeService.GetPortfolioAsync(userId);
                return Ok(portfolio);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
