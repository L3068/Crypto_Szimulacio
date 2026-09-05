using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Crypto_Simulation.DataContext.Entities
{
    public class PriceHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("CryptoCurrency")]
        public int CryptoId { get; set; }

        [JsonIgnore]
        public CryptoCurrency CryptoCurrency { get; set; } = null!;

        [Required]
        public decimal Price { get; set; }

        /// <summary>Always stored in UTC.</summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
