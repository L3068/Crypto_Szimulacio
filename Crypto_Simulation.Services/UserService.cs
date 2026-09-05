using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Crypto_Simulation.Services.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Crypto_Simulation.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterUserAsync(UserRegisterDto userDto);
        Task<AuthResponseDto> LoginAsync(UserLoginDto loginDto);
        Task<UserResponseDto> GetUserByIdAsync(int userId);
        Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto userDto);
        Task DeleteUserAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly SimulationOptions _simulation;
        private readonly ILogger<UserService> _logger;

        public UserService(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IOptions<SimulationOptions> simulation,
            ILogger<UserService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _simulation = simulation.Value;
            _logger = logger;
        }

        public async Task<UserResponseDto> RegisterUserAsync(UserRegisterDto userDto)
        {
            var email = userDto.Email.Trim();
            var username = userDto.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                throw new ConflictException("This email address is already registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                throw new ConflictException("This username is already taken.");
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Role = UserRoles.User
            };

            // Never persist the plaintext password.
            user.PasswordHash = _passwordHasher.HashPassword(user, userDto.Password);

            // Assigning through the navigation property inserts the user and the wallet in a
            // single SaveChanges, so a failure cannot leave a user without a wallet.
            user.Wallet = new Wallet { Balance = _simulation.StartingBalance };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registered user {UserId}", user.Id);

            return ToDto(user);
        }

        public async Task<AuthResponseDto> LoginAsync(UserLoginDto loginDto)
        {
            var email = loginDto.Email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Same message and roughly the same work either way, so the response does not
            // reveal whether the email exists.
            if (user is null)
            {
                _passwordHasher.HashPassword(new User(), loginDto.Password);
                throw new ValidationException("Invalid email address or password.");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new ValidationException("Invalid email address or password.");
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, loginDto.Password);
                await _context.SaveChangesAsync();
            }

            var (token, expiresAtUtc) = _tokenService.CreateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = ToDto(user)
            };
        }

        public async Task<UserResponseDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw NotFoundException.For("User", userId);

            return ToDto(user);
        }

        public async Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw NotFoundException.For("User", userId);

            var email = userDto.Email.Trim();
            var username = userDto.Username.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email && u.Id != userId))
            {
                throw new ConflictException("This email address is already registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == username && u.Id != userId))
            {
                throw new ConflictException("This username is already taken.");
            }

            user.Username = username;
            user.Email = email;

            // The previous version accepted a new password and silently dropped it.
            if (!string.IsNullOrWhiteSpace(userDto.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, userDto.Password);
            }

            await _context.SaveChangesAsync();

            return ToDto(user);
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw NotFoundException.For("User", userId);

            // Wallet, portfolio items and transactions are removed by the configured cascades.
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted user {UserId}", userId);
        }

        private static UserResponseDto ToDto(User user) => new()
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }
}
