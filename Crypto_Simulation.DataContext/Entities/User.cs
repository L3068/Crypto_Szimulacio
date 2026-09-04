using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Entities
{
    /// <summary>Well-known role names. Stored as plain strings on <see cref="User.Role"/>.</summary>
    public static class UserRoles
    {
        public const string User = "User";
        public const string Admin = "Admin";
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password. Never store or accept a plaintext password here -
        /// use IPasswordHasher to produce this value.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = UserRoles.User;

        public Wallet? Wallet { get; set; }
        public List<Transaction> Transactions { get; set; } = new();

        public User() { }
    }
}
