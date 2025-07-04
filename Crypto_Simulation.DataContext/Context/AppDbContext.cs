using Microsoft.EntityFrameworkCore;
using Crypto_Simulation.DataContext.Entities;

namespace Crypto_Simulation.DataContext
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<CryptoCurrency> CryptoCurrencies { get; set; }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PortfolioItem>()
                .HasKey(wc => new { wc.WalletId, wc.CryptoId});

            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId);
            modelBuilder.Entity<User>()
                .HasMany(u => u.Transactions)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Wallet>()
                .HasKey(w => w.Id);
            modelBuilder.Entity<Wallet>()
                .HasMany(w => w.PortfolioItems)
                .WithOne(pi => pi.Wallet)
                .HasForeignKey(pi => pi.WalletId);

            modelBuilder.Entity<CryptoCurrency>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<CryptoCurrency>()
                .HasMany(c => c.PortfolioItems)
                .WithOne(pi => pi.CryptoCurrency)
                .HasForeignKey(pi => pi.CryptoId);
            modelBuilder.Entity<CryptoCurrency>()
                .HasMany(c => c.PriceHistories)
                .WithOne(ph => ph.CryptoCurrency)
                .HasForeignKey(ph => ph.CryptoId);
            modelBuilder.Entity<CryptoCurrency>()
               .HasIndex(t => t.Symbol)
               .IsUnique();
        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        {

        }
    }
}
