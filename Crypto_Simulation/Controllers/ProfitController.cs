using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ProfitController : ApiControllerBase
    {
        private readonly IProfitService _profitService;

        public ProfitController(IProfitService profitService)
        {
            _profitService = profitService;
        }

        /// <summary>Aggregate unrealised profit and loss across the whole portfolio.</summary>
        [HttpGet("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfitResponseDto>> GetProfit(int userId)
        {
            EnsureCanAccess(userId);
            return Ok(await _profitService.CalculateProfitAsync(userId));
        }

        /// <summary>Per-position profit and loss breakdown.</summary>
        [HttpGet("details/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProfitDetailResponseDto>> GetDetailedProfit(int userId)
        {
            EnsureCanAccess(userId);
            return Ok(await _profitService.CalculateDetailedProfitAsync(userId));
        }
    }
}
