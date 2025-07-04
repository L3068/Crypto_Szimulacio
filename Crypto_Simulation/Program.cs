using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Crypto_Simulation.DataContext;
using Crypto_Simulation.Controllers;
using System.Text;
using System;
using Crypto_Simulation.Services;
using Crypto_Simulation.DataContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer("Server=.\\SQLEXPRESS;Database=CryptoDb;Trusted_Connection=True;TrustServerCertificate=True;");
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ICryptoService, CryptoService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IProfitService, ProfitService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddHostedService<PriceUpdateService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crypto API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto API v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
