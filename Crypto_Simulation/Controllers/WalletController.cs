using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ApiControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>Balance and holdings of a user. Only the owner or an admin may read them.</summary>
        [HttpGet("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletResponseDto>> GetWallet(int userId)
        {
            EnsureCanAccess(userId);
            return Ok(await _walletService.GetWalletByUserIdAsync(userId));
        }

        /// <summary>
        /// Overwrites a wallet balance. Admin only - letting users set their own balance would
        /// make the whole profit and loss simulation meaningless.
        /// </summary>
        [HttpPut("{userId:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletResponseDto>> UpdateWalletBalance(int userId, WalletUpdateDto walletDto)
        {
            return Ok(await _walletService.UpdateWalletBalanceAsync(userId, walletDto));
        }

        /// <summary>Deletes a wallet together with its positions. Admin only.</summary>
        [HttpDelete("{userId:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteWallet(int userId)
        {
            await _walletService.DeleteWalletAsync(userId);
            return NoContent();
        }
    }
}
