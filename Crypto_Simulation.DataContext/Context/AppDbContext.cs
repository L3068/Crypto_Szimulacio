using Crypto_Simulation.DataContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crypto_Simulation.DataContext
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Column shapes for the money/quantity columns. Without these EF falls back to SQL Server's
        /// decimal(18,2) default, which silently rounds sub-cent prices and fractional coin amounts
        /// (0.001 BTC would be stored as 0.00).
        /// </summary>
        private const int QuantityPrecision = 38;
        private const int QuantityScale = 18;
        private const int PricePrecision = 28;
        private const int PriceScale = 12;
        private const int MoneyPrecision = 28;
        private const int MoneyScale = 8;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
        public DbSet<CryptoCurrency> CryptoCurrencies { get; set; } = null!;
        public DbSet<PortfolioItem> PortfolioItems { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(u => u.Id);
                user.HasIndex(u => u.Email).IsUnique();
                user.HasIndex(u => u.Username).IsUnique();

                user.HasOne(u => u.Wallet)
                    .WithOne(w => w.User)
                    .HasForeignKey<Wallet>(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                user.HasMany(u => u.Transactions)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Wallet>(wallet =>
            {
                wallet.HasKey(w => w.Id);
                wallet.Property(w => w.Balance).HasPrecision(MoneyPrecision, MoneyScale);
                wallet.Property(w => w.RowVersion).IsRowVersion();

                wallet.HasMany(w => w.PortfolioItems)
                    .WithOne(pi => pi.Wallet)
                    .HasForeignKey(pi => pi.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CryptoCurrency>(crypto =>
            {
                crypto.HasKey(c => c.Id);
                crypto.HasIndex(c => c.Symbol).IsUnique();
                crypto.Property(c => c.CurrentPrice).HasPrecision(PricePrecision, PriceScale);
                crypto.Property(c => c.TotalSupply).HasPrecision(QuantityPrecision, QuantityScale);

                // Restrict, not Cascade: deleting a currency that people still hold would otherwise
                // silently wipe their positions and transaction history.
                crypto.HasMany(c => c.PortfolioItems)
                    .WithOne(pi => pi.CryptoCurrency)
                    .HasForeignKey(pi => pi.CryptoId)
                    .OnDelete(DeleteBehavior.Restrict);

                crypto.HasMany(c => c.PriceHistories)
                    .WithOne(ph => ph.CryptoCurrency)
                    .HasForeignKey(ph => ph.CryptoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PortfolioItem>(item =>
            {
                item.HasKey(pi => new { pi.WalletId, pi.CryptoId });
                item.Property(pi => pi.Quantity).HasPrecision(QuantityPrecision, QuantityScale);
                item.Property(pi => pi.AveragePrice).HasPrecision(PricePrecision, PriceScale);
            });

            modelBuilder.Entity<Transaction>(transaction =>
            {
                transaction.HasKey(t => t.Id);
                transaction.Property(t => t.Quantity).HasPrecision(QuantityPrecision, QuantityScale);
                transaction.Property(t => t.PricePerUnit).HasPrecision(PricePrecision, PriceScale);
                transaction.Property(t => t.TotalPrice).HasPrecision(MoneyPrecision, MoneyScale);
                transaction.HasIndex(t => new { t.UserId, t.Timestamp });

                transaction.HasOne(t => t.CryptoCurrency)
                    .WithMany()
                    .HasForeignKey(t => t.CryptoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PriceHistory>(history =>
            {
                history.HasKey(ph => ph.Id);
                history.Property(ph => ph.Price).HasPrecision(PricePrecision, PriceScale);
                history.HasIndex(ph => new { ph.CryptoId, ph.Timestamp });
            });
        }
    }
}
