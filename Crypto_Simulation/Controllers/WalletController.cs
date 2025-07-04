using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<WalletResponseDto>> GetWallet(int userId)
        {
            try
            {
                var wallet = await _walletService.GetWalletByUserIdAsync(userId);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<WalletResponseDto>> UpdateWalletBalance(int userId, WalletUpdateDto walletDto)
        {
            try
            {
                var result = await _walletService.UpdateWalletBalanceAsync(userId, walletDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{userId}")]
        public async Task<ActionResult> DeleteWallet(int userId)
        {
            try
            {
                await _walletService.DeleteWalletAsync(userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
