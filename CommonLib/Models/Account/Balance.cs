using System;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Account balance model
    /// </summary>
    public class Balance
    {
        /// <summary>
        /// Asset type
        /// </summary>
        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Available balance
        /// </summary>
        [BsonElement("free")]
        public decimal Free { get; set; }

        /// <summary>
        /// Locked balance
        /// </summary>
        [BsonElement("locked")]
        public decimal Locked { get; set; }

        /// <summary>
        /// Total balance = Available + Locked
        /// </summary>
        [BsonIgnore]
        public decimal Total => Free + Locked;

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}