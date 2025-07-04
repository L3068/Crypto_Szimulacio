using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext;
namespace Crypto_Simulation.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterUserAsync(UserRegisterDto userDto);
        Task<UserResponseDto> GetUserByIdAsync(int userId);
        Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto userDto);
        Task DeleteUserAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserResponseDto> RegisterUserAsync(UserRegisterDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                throw new Exception("Email already registered");
            }

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                Password = userDto.Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 10000M
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return new UserResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }

        public async Task<UserResponseDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return new UserResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }

        public async Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            user.Username = userDto.Username;
            user.Email = userDto.Email;

            await _context.SaveChangesAsync();

            return new UserResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            };
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet != null)
            {
                _context.Wallets.Remove(wallet);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
