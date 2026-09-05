using System.Security.Cryptography;
using System.Text;
using Crypto_Simulation.DataContext;
using Crypto_Simulation.DataContext.Entities;
using Crypto_Simulation.Infrastructure;
using Crypto_Simulation.Services;
using Crypto_Simulation.Services.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------

// No signing key is committed to source control. During local development one is
// generated on the spot so a fresh clone runs without setup; every other environment
// must supply Jwt:Key itself, and startup fails loudly if it does not.
var jwtKeyPath = $"{JwtOptions.SectionName}:{nameof(JwtOptions.Key)}";
bool usingGeneratedJwtKey = false;

if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration[jwtKeyPath]))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        [jwtKeyPath] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
    });

    usingGeneratedJwtKey = true;
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SimulationOptions>()
    .Bind(builder.Configuration.GetSection(SimulationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---------------------------------------------------------------------------
// Persistence
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DatabaseConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DatabaseConnection' is not configured. Set it in appsettings.Development.json, " +
            "user-secrets or the ConnectionStrings__DatabaseConnection environment variable.");
    }

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

// ---------------------------------------------------------------------------
// Authentication and authorization
// ---------------------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Bound lazily from the validated JwtOptions rather than read eagerly out of configuration,
// so the key used to validate a token is always the one TokenService signed it with.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        var options = jwt.Value;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IProfitService, ProfitService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddHostedService<PriceUpdateService>();

// ---------------------------------------------------------------------------
// Web
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();

const string CorsPolicy = "DefaultCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    }));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Crypto Simulation API",
        Version = "v1",
        Description = "A cryptocurrency trading simulation with virtual balances."
    });

    // Lets Swagger UI send the bearer token returned by /api/users/login.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/users/login."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

if (usingGeneratedJwtKey)
{
    app.Logger.LogWarning(
        "No {KeyPath} was configured, so a random development key was generated. Tokens issued " +
        "now stop working when the app restarts. Set {KeyPath} with user-secrets to keep them stable.",
        jwtKeyPath, jwtKeyPath);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Simulation API v1"));
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration test host can reference the entry point assembly.</summary>
public partial class Program { }
