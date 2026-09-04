using Crypto_Simulation.DataContext.Dtos;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Crypto_Simulation.Tests
{
    public class UserServiceTests
    {
        private static UserRegisterDto NewRegistration() => new()
        {
            Username = "trader",
            Email = "trader@example.com",
            Password = "correct horse battery"
        };

        [Fact]
        public async Task Register_StoresAHashRatherThanThePlaintextPassword()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var dto = NewRegistration();
            await users.RegisterUserAsync(dto);

            var stored = await context.Users.SingleAsync();
            Assert.NotEqual(dto.Password, stored.PasswordHash);
            Assert.DoesNotContain(dto.Password, stored.PasswordHash);
            Assert.NotEmpty(stored.PasswordHash);
        }

        [Fact]
        public async Task Register_CreatesAWalletWithTheConfiguredStartingBalance()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context, startingBalance: 2_500M);

            var result = await users.RegisterUserAsync(NewRegistration());

            var wallet = await context.Wallets.SingleAsync();
            Assert.Equal(result.UserId, wallet.UserId);
            Assert.Equal(2_500M, wallet.Balance);
        }

        [Fact]
        public async Task Register_DefaultsToTheNonAdminRole()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var result = await users.RegisterUserAsync(NewRegistration());

            Assert.Equal(UserRoles.User, result.Role);
        }

        [Fact]
        public async Task Register_RejectsADuplicateEmail()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            await users.RegisterUserAsync(NewRegistration());

            var duplicate = NewRegistration();
            duplicate.Username = "someone-else";

            await Assert.ThrowsAsync<ConflictException>(() => users.RegisterUserAsync(duplicate));
        }

        [Fact]
        public async Task Register_RejectsADuplicateUsername()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            await users.RegisterUserAsync(NewRegistration());

            var duplicate = NewRegistration();
            duplicate.Email = "other@example.com";

            await Assert.ThrowsAsync<ConflictException>(() => users.RegisterUserAsync(duplicate));
        }

        [Fact]
        public async Task Login_ReturnsATokenForTheCorrectPassword()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var dto = NewRegistration();
            await users.RegisterUserAsync(dto);

            var auth = await users.LoginAsync(new UserLoginDto { Email = dto.Email, Password = dto.Password });

            Assert.NotEmpty(auth.Token);
            Assert.True(auth.ExpiresAtUtc > DateTime.UtcNow);
            Assert.Equal(dto.Email, auth.User.Email);
        }

        [Fact]
        public async Task Login_RejectsAWrongPassword()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var dto = NewRegistration();
            await users.RegisterUserAsync(dto);

            await Assert.ThrowsAsync<ValidationException>(() =>
                users.LoginAsync(new UserLoginDto { Email = dto.Email, Password = "wrong" }));
        }

        [Fact]
        public async Task Login_RejectsAnUnknownEmail()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            await Assert.ThrowsAsync<ValidationException>(() =>
                users.LoginAsync(new UserLoginDto { Email = "nobody@example.com", Password = "whatever" }));
        }

        [Fact]
        public async Task Update_ActuallyChangesThePassword()
        {
            // Regression: UserUpdateDto carried a password that the service silently ignored.
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var dto = NewRegistration();
            var created = await users.RegisterUserAsync(dto);

            await users.UpdateUserAsync(created.UserId, new UserUpdateDto
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = "a brand new password"
            });

            await Assert.ThrowsAsync<ValidationException>(() =>
                users.LoginAsync(new UserLoginDto { Email = dto.Email, Password = dto.Password }));

            var auth = await users.LoginAsync(new UserLoginDto { Email = dto.Email, Password = "a brand new password" });
            Assert.NotEmpty(auth.Token);
        }

        [Fact]
        public async Task Update_LeavesThePasswordAloneWhenNoneIsSupplied()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var dto = NewRegistration();
            var created = await users.RegisterUserAsync(dto);

            await users.UpdateUserAsync(created.UserId, new UserUpdateDto
            {
                Username = "renamed",
                Email = dto.Email,
                Password = null
            });

            var auth = await users.LoginAsync(new UserLoginDto { Email = dto.Email, Password = dto.Password });
            Assert.Equal("renamed", auth.User.Username);
        }

        [Fact]
        public async Task Update_RejectsAnEmailOwnedBySomebodyElse()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var first = await users.RegisterUserAsync(NewRegistration());
            await users.RegisterUserAsync(new UserRegisterDto
            {
                Username = "second",
                Email = "second@example.com",
                Password = "another password"
            });

            await Assert.ThrowsAsync<ConflictException>(() =>
                users.UpdateUserAsync(first.UserId, new UserUpdateDto
                {
                    Username = "trader",
                    Email = "second@example.com"
                }));
        }

        [Fact]
        public async Task Delete_RemovesTheUserAndTheWallet()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            var created = await users.RegisterUserAsync(NewRegistration());
            await users.DeleteUserAsync(created.UserId);

            Assert.Empty(await context.Users.ToListAsync());
        }

        [Fact]
        public async Task Delete_ReportsAMissingUser()
        {
            await using var context = TestContextFactory.CreateContext();
            var users = TestContextFactory.CreateUserService(context);

            await Assert.ThrowsAsync<NotFoundException>(() => users.DeleteUserAsync(404));
        }
    }
}
