using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Transactions;
using System.Xml.Linq;

namespace Crypto_Simulation.DataContext.Entities
{
    public class CryptoCurrency
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [StringLength(10)]
        public string Symbol { get; set; } = string.Empty;
        [Required]
        public decimal CurrentPrice { get; set; }
        [Required]
        public decimal TotalSupply { get; set; }
        public List<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
        public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();

        public CryptoCurrency() { }
    }
}
