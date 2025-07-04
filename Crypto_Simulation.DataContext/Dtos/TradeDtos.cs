using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto_Simulation.DataContext.Dtos
{
    public class TradeRequestDto
    {
        public int CryptoId { get; set; }
        public int UserId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class ConvertRequestDto
    {
        public int CryptoId { get; set; }
        public int TargetCryptoId { get; set; }
        public int UserId { get; set; }
        public decimal Quantity { get; set; }
    }
}
