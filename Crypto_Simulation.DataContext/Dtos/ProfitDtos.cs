using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class ProfitResponseDto
    {
        public int UserId { get; set; }
        public decimal TotalInvestment { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal TotalProfitLossPercentage { get; set; }
    }

    public class ProfitDetailResponseDto
    {
        public int UserId { get; set; }
        public List<CryptoProfitDto> Details { get; set; } = new List<CryptoProfitDto>();
        public decimal TotalProfitLoss { get; set; }
    }

    public class CryptoProfitDto
    {
        public int CryptoId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Investment { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }
}
