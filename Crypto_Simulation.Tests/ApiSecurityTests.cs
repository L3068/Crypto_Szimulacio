using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Crypto_Simulation.DataContext.Dtos;
using Xunit;

namespace Crypto_Simulation.Tests
{
    public class ApiSecurityTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public ApiSecurityTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static UserRegisterDto Registration(string suffix) => new()
        {
            Username = $"trader-{suffix}",
            Email = $"trader-{suffix}@example.com",
            Password = "correct horse battery"
        };

        private static async Task<(int UserId, string Token)> RegisterAndLoginAsync(HttpClient client, string suffix)
        {
            var registration = Registration(suffix);

            var registerResponse = await client.PostAsJsonAsync("/api/users/register", registration);
            registerResponse.EnsureSuccessStatusCode();
            var created = await registerResponse.Content.ReadFromJsonAsync<UserResponseDto>();

            var loginResponse = await client.PostAsJsonAsync("/api/users/login", new UserLoginDto
            {
                Email = registration.Email,
                Password = registration.Password
            });
            loginResponse.EnsureSuccessStatusCode();
            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            return (created!.UserId, auth!.Token);
        }

        private static void Authenticate(HttpClient client, string token) =>
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        [Fact]
        public async Task ProtectedEndpointsRejectAnonymousCallers()
        {
            var client = await _factory.CreateSeededClientAsync();

            var response = await client.GetAsync("/api/trade/portfolio");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TradingRejectsAnonymousCallers()
        {
            var client = await _factory.CreateSeededClientAsync();

            var response = await client.PostAsJsonAsync("/api/trade/buy", new TradeRequestDto
            {
                CryptoId = 1,
                Quantity = 1M
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MarketDataStaysPublic()
        {
            var client = await _factory.CreateSeededClientAsync();

            var response = await client.GetAsync("/api/cryptos");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoResponseDto>>();
            Assert.NotEmpty(cryptos!);
        }

        [Fact]
        public async Task AUserCannotReadSomebodyElsesWallet()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, firstToken) = await RegisterAndLoginAsync(client, "wallet-a");
            client.DefaultRequestHeaders.Authorization = null;
            var (secondUserId, _) = await RegisterAndLoginAsync(client, "wallet-b");

            Authenticate(client, firstToken);
            var response = await client.GetAsync($"/api/wallet/{secondUserId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AUserCannotReadSomebodyElsesTransactions()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, firstToken) = await RegisterAndLoginAsync(client, "tx-a");
            client.DefaultRequestHeaders.Authorization = null;
            var (secondUserId, _) = await RegisterAndLoginAsync(client, "tx-b");

            Authenticate(client, firstToken);
            var response = await client.GetAsync($"/api/transactions/{secondUserId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ARegularUserCannotTopUpTheirOwnBalance()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (userId, token) = await RegisterAndLoginAsync(client, "topup");
            Authenticate(client, token);

            var response = await client.PutAsJsonAsync($"/api/wallet/{userId}", new WalletUpdateDto
            {
                Balance = 1_000_000M
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ARegularUserCannotCreateACurrency()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, token) = await RegisterAndLoginAsync(client, "mint");
            Authenticate(client, token);

            var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoCreateDto
            {
                Name = "Scamcoin",
                Symbol = "SCAM",
                CurrentPrice = 1M,
                TotalSupply = 1_000M
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AnAdminCanCreateACurrency()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (userId, _) = await RegisterAndLoginAsync(client, "admin");
            await _factory.PromoteToAdminAsync(userId);

            // Re-login so the token carries the new role.
            var loginResponse = await client.PostAsJsonAsync("/api/users/login", new UserLoginDto
            {
                Email = Registration("admin").Email,
                Password = Registration("admin").Password
            });
            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Authenticate(client, auth!.Token);

            var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoCreateDto
            {
                Name = "Testcoin",
                Symbol = "TEST",
                CurrentPrice = 1M,
                TotalSupply = 1_000M
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task RegistrationRejectsAShortPassword()
        {
            var client = await _factory.CreateSeededClientAsync();

            var response = await client.PostAsJsonAsync("/api/users/register", new UserRegisterDto
            {
                Username = "shorty",
                Email = "shorty@example.com",
                Password = "abc"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegistrationRejectsANonPositiveTradeQuantity()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, token) = await RegisterAndLoginAsync(client, "zero-qty");
            Authenticate(client, token);

            var response = await client.PostAsJsonAsync("/api/trade/buy", new TradeRequestDto
            {
                CryptoId = 1,
                Quantity = 0M
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ADuplicateRegistrationReportsAConflict()
        {
            var client = await _factory.CreateSeededClientAsync();

            await RegisterAndLoginAsync(client, "dupe");
            client.DefaultRequestHeaders.Authorization = null;

            var response = await client.PostAsJsonAsync("/api/users/register", Registration("dupe"));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task AMissingCurrencyReportsNotFoundRatherThanBadRequest()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, token) = await RegisterAndLoginAsync(client, "missing");
            Authenticate(client, token);

            var response = await client.PostAsJsonAsync("/api/trade/buy", new TradeRequestDto
            {
                CryptoId = 9999,
                Quantity = 1M
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ASignedInUserCanTradeAndSeeThePosition()
        {
            var client = await _factory.CreateSeededClientAsync();

            var (_, token) = await RegisterAndLoginAsync(client, "happy-path");
            Authenticate(client, token);

            var buyResponse = await client.PostAsJsonAsync("/api/trade/buy", new TradeRequestDto
            {
                CryptoId = 1,
                Quantity = 0.25M
            });
            buyResponse.EnsureSuccessStatusCode();

            var portfolio = await client.GetFromJsonAsync<WalletResponseDto>("/api/trade/portfolio");

            var position = Assert.Single(portfolio!.Cryptos);
            Assert.Equal("BTC", position.Symbol);
            Assert.Equal(0.25M, position.Amount);
            Assert.Equal(87_500M, portfolio.Balance);   // 100 000 - 0.25 * 50 000
        }
    }
}
