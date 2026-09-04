using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Entities
{
    public class CryptoCurrency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public decimal CurrentPrice { get; set; }

        [Required]
        public decimal TotalSupply { get; set; }

        public List<PortfolioItem> PortfolioItems { get; set; } = new();
        public List<PriceHistory> PriceHistories { get; set; } = new();

        public CryptoCurrency() { }
    }
}
