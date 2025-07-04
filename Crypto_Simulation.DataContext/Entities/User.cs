using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public Wallet Wallet { get; set; }
        public List<Transaction> Transactions { get; set; }

        public User() { }
        public User(string username, string password, string email)
        {
            Username = username;
            Password = password;
            Email = email;
        }
    }
}
