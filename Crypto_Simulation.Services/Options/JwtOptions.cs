using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.Services.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>
        /// Symmetric signing key. Keep it out of source control outside of local development -
        /// use user-secrets, an environment variable or a key vault.
        /// </summary>
        [Required]
        [MinLength(32, ErrorMessage = "The JWT signing key must be at least 32 characters long.")]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = "Crypto_Simulation";

        [Required]
        public string Audience { get; set; } = "Crypto_Simulation";

        [Range(1, 1440)]
        public int ExpiryMinutes { get; set; } = 60;
    }
}
