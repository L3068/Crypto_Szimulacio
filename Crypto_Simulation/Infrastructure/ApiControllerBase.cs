using System.Security.Claims;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Infrastructure
{
    /// <summary>
    /// Shared helpers for resolving the caller from the bearer token and enforcing that a user
    /// may only reach their own data.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>The authenticated user's id, taken from the token rather than the request.</summary>
        protected int CurrentUserId
        {
            get
            {
                var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out int id)
                    ? id
                    : throw new ForbiddenException("The access token does not identify a user.");
            }
        }

        protected bool IsAdmin => User.IsInRole(UserRoles.Admin);

        /// <summary>Throws unless the caller is the owner of <paramref name="userId"/> or an admin.</summary>
        protected void EnsureCanAccess(int userId)
        {
            if (!IsAdmin && CurrentUserId != userId)
            {
                throw new ForbiddenException("You may only access your own data.");
            }
        }
    }
}
