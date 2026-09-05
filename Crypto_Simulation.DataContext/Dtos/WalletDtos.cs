using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class WalletResponseDto
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public List<WalletCryptoDto> Cryptos { get; set; } = new();
    }

    public class WalletCryptoDto
    {
        public int CryptoId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }

    public class WalletUpdateDto
    {
        [Required]
        [Range(typeof(decimal), DecimalRanges.ZeroMoney, DecimalRanges.LargestMoney,
            ErrorMessage = "The balance cannot be negative.")]
        public decimal Balance { get; set; }
    }
}
