using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class WalletResponseDto
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public List<WalletCryptoDto> Cryptos { get; set; } = new List<WalletCryptoDto>();
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
        public decimal Balance { get; set; }
    }
}
