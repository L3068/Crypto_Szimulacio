using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Creates an account and its starting wallet.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserResponseDto>> RegisterUser(UserRegisterDto userDto)
        {
            var result = await _userService.RegisterUserAsync(userDto);
            return CreatedAtAction(nameof(GetUser), new { userId = result.UserId }, result);
        }

        /// <summary>Exchanges email and password for a bearer token.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> Login(UserLoginDto loginDto)
        {
            return Ok(await _userService.LoginAsync(loginDto));
        }

        /// <summary>Returns the profile of the signed-in user (admins may read any profile).</summary>
        [HttpGet("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponseDto>> GetUser(int userId)
        {
            EnsureCanAccess(userId);
            return Ok(await _userService.GetUserByIdAsync(userId));
        }

        /// <summary>Returns the profile of the caller.</summary>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
        {
            return Ok(await _userService.GetUserByIdAsync(CurrentUserId));
        }

        /// <summary>Updates username, email and optionally the password.</summary>
        [HttpPut("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int userId, UserUpdateDto userDto)
        {
            EnsureCanAccess(userId);
            return Ok(await _userService.UpdateUserAsync(userId, userDto));
        }

        /// <summary>Deletes the account together with its wallet and history.</summary>
        [HttpDelete("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            EnsureCanAccess(userId);
            await _userService.DeleteUserAsync(userId);
            return NoContent();
        }
    }
}
