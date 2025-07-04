using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class TransactionResponseDto
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int CryptoId { get; set; }
        public string CryptoName { get; set; } = string.Empty;
        public string CryptoSymbol { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
