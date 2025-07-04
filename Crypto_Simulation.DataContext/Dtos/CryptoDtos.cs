using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal TotalSupply { get; set; }
    }

    public class CryptoPriceUpdateDto
    {
        public int CryptoId { get; set; }
        public decimal NewPrice { get; set; }
    }
}
