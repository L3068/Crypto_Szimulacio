using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Dtos
{
    /// <summary>
    /// The acting user is taken from the bearer token, never from the request body -
    /// otherwise any caller could trade on somebody else's wallet.
    /// </summary>
    public class TradeRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CryptoId { get; set; }

        [Required]
        [Range(typeof(decimal), DecimalRanges.SmallestQuantity, DecimalRanges.LargestQuantity,
            ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }
    }

    public class ConvertRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CryptoId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TargetCryptoId { get; set; }

        [Required]
        [Range(typeof(decimal), DecimalRanges.SmallestQuantity, DecimalRanges.LargestQuantity,
            ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }
    }

    /// <summary>Shared bounds for [Range] on decimal properties (the attribute needs string literals).</summary>
    public static class DecimalRanges
    {
        public const string SmallestQuantity = "0.000000000000000001";
        public const string LargestQuantity = "99999999999999999999";
        public const string SmallestPrice = "0.000000000001";
        public const string LargestPrice = "99999999999999999999";
        public const string ZeroMoney = "0";
        public const string LargestMoney = "99999999999999999999";
    }
}
