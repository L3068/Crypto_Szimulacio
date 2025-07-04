using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Entities
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
        public List<PortfolioItem> PortfolioItems { get; set; }
        [Required]
        public decimal Balance { get; set; }
    }
}
