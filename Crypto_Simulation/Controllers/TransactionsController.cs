using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Exceptions;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ApiControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>Transaction history of a user, newest first and paged.</summary>
        [HttpGet("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<TransactionResponseDto>>> GetUserTransactions(
            int userId,
            [FromQuery][Range(0, int.MaxValue)] int skip = 0,
            [FromQuery][Range(1, 200)] int take = 50)
        {
            EnsureCanAccess(userId);
            return Ok(await _transactionService.GetUserTransactionsAsync(userId, skip, take));
        }

        /// <summary>A single transaction. Only the owner or an admin may read it.</summary>
        [HttpGet("details/{transactionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransactionResponseDto>> GetTransactionDetails(int transactionId)
        {
            var ownerId = await _transactionService.GetOwnerIdAsync(transactionId)
                ?? throw NotFoundException.For("Transaction", transactionId);

            EnsureCanAccess(ownerId);
            return Ok(await _transactionService.GetTransactionDetailsAsync(transactionId));
        }
    }
}
