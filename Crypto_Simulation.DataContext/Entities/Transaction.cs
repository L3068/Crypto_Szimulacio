using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Entities
{
    public enum TransactionType
    {
        Buy,
        Sell,
        Convert
    }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [ForeignKey("CryptoCurrency")]
        public int CryptoId { get; set; }
        public virtual CryptoCurrency CryptoCurrency { get; set; }

        [Required]
        public TransactionType Type { get; set; } 

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public decimal PricePerUnit { get; set; }
        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
