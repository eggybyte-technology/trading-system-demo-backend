using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// User risk profile
    /// </summary>
    public class RiskProfile : IndexedModel<RiskProfile>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "RiskDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "RiskProfiles";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Risk level (low, medium, high)
        /// </summary>
        [BsonElement("riskLevel")]
        public string RiskLevel { get; set; } = "medium";

        /// <summary>
        /// Trading limits
        /// </summary>
        [BsonElement("limits")]
        public TradingLimits Limits { get; set; } = new();

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<RiskProfile>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<RiskProfile>, bool>>
            {
                new Tuple<IndexKeysDefinition<RiskProfile>, bool>(
                    Builders<RiskProfile>.IndexKeys.Ascending(r => r.UserId),
                    true
                )
            };
        }
    }
}