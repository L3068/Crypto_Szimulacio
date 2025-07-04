using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<List<TransactionResponseDto>>> GetUserTransactions(int userId)
        {
            try
            {
                var transactions = await _transactionService.GetUserTransactionsAsync(userId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("details/{transactionId}")]
        public async Task<ActionResult<TransactionResponseDto>> GetTransactionDetails(int transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionDetailsAsync(transactionId);
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
