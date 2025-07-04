using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfitController : ControllerBase
    {
        private readonly IProfitService _profitService;

        public ProfitController(IProfitService profitService)
        {
            _profitService = profitService;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<ProfitResponseDto>> GetProfit(int userId)
        {
            try
            {
                var profit = await _profitService.CalculateProfitAsync(userId);
                return Ok(profit);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("details/{userId}")]
        public async Task<ActionResult<ProfitDetailResponseDto>> GetDetailedProfit(int userId)
        {
            try
            {
                var profit = await _profitService.CalculateDetailedProfitAsync(userId);
                return Ok(profit);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
