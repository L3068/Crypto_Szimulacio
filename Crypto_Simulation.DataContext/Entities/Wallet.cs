using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypto_Simulation.DataContext.Entities
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public List<PortfolioItem> PortfolioItems { get; set; } = new();

        [Required]
        public decimal Balance { get; set; }

        /// <summary>
        /// Optimistic concurrency token. Two requests trading against the same wallet at the
        /// same time would otherwise silently overwrite each other's balance (last write wins).
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
