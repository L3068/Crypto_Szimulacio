using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class CryptoResponseDto
    {
        public int CryptoId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal TotalSupply { get; set; }
    }

    public class CryptoCreateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), DecimalRanges.SmallestPrice, DecimalRanges.LargestPrice,
            ErrorMessage = "The price must be greater than zero.")]
        public decimal CurrentPrice { get; set; }

        [Required]
        [Range(typeof(decimal), DecimalRanges.SmallestQuantity, DecimalRanges.LargestQuantity,
            ErrorMessage = "The total supply must be greater than zero.")]
        public decimal TotalSupply { get; set; }
    }

    public class CryptoPriceUpdateDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CryptoId { get; set; }

        [Required]
        [Range(typeof(decimal), DecimalRanges.SmallestPrice, DecimalRanges.LargestPrice,
            ErrorMessage = "The price must be greater than zero.")]
        public decimal NewPrice { get; set; }
    }

    public class PriceHistoryDto
    {
        public int CryptoId { get; set; }
        public decimal Price { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    /// <summary>Paging/filtering for the price history endpoint, which is otherwise unbounded.</summary>
    public class PriceHistoryQueryDto
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }

        [Range(1, 1000)]
        public int Limit { get; set; } = 200;
    }
}
