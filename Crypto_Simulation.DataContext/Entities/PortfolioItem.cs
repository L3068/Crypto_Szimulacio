using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypto_Simulation.DataContext.Entities
{
    public class PortfolioItem
    {
        [Required]
        [ForeignKey("Wallet")]
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; }
        [Required]
        [ForeignKey("CryptoCurrency")]
        public int CryptoId { get; set; }
        public CryptoCurrency CryptoCurrency { get; set; }
        [Required]
        public decimal Quantity { get; set; }
        [Required]
        public decimal AveragePrice { get; set; }
    }
}
