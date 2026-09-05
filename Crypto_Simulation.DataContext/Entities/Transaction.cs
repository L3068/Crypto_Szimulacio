using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypto_Simulation.DataContext.Entities
{
    public enum TransactionType
    {
        Buy = 0,
        Sell = 1,
        Convert = 2
    }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [ForeignKey("CryptoCurrency")]
        public int CryptoId { get; set; }
        public CryptoCurrency CryptoCurrency { get; set; } = null!;

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public decimal PricePerUnit { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        /// <summary>Always stored in UTC.</summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
