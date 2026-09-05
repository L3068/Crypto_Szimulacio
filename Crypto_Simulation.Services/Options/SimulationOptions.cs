using System.ComponentModel.DataAnnotations;

namespace Crypto_Simulation.Services.Options
{
    public class SimulationOptions
    {
        public const string SectionName = "Simulation";

        /// <summary>Virtual cash every new account starts with.</summary>
        [Range(0, double.MaxValue)]
        public decimal StartingBalance { get; set; } = 10_000M;

        /// <summary>How often the background job moves prices.</summary>
        [Range(1, 3600)]
        public int PriceUpdateIntervalSeconds { get; set; } = 30;

        /// <summary>Maximum relative price move per tick (0.03 = +/-3%).</summary>
        [Range(0.0, 1.0)]
        public double MaxPriceFluctuation { get; set; } = 0.03;

        /// <summary>
        /// Price history rows older than this are pruned. At one row per currency per tick the
        /// table would otherwise grow without bound. Set to 0 to keep everything.
        /// </summary>
        [Range(0, 3650)]
        public int PriceHistoryRetentionDays { get; set; } = 30;
    }
}
