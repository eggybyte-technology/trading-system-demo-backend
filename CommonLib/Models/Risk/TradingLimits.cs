using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Trading limits for user risk profile
    /// </summary>
    public class TradingLimits
    {
        /// <summary>
        /// Daily withdrawal limit
        /// </summary>
        [BsonElement("dailyWithdrawalLimit")]
        public decimal DailyWithdrawalLimit { get; set; }

        /// <summary>
        /// Single order maximum limit
        /// </summary>
        [BsonElement("singleOrderLimit")]
        public decimal SingleOrderLimit { get; set; }

        /// <summary>
        /// Daily trading volume limit
        /// </summary>
        [BsonElement("dailyTradingLimit")]
        public decimal DailyTradingLimit { get; set; }

        /// <summary>
        /// Asset-specific limits
        /// </summary>
        [BsonElement("assetSpecificLimits")]
        public Dictionary<string, decimal> AssetSpecificLimits { get; set; } = new();
    }
}