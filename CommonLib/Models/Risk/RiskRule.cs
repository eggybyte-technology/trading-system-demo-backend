using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Risk
{
    /// <summary>
    /// Risk rule
    /// </summary>
    public class RiskRule : IndexedModel<RiskRule>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "RiskDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "RiskRules";

        /// <summary>
        /// Rule name
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rule description
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Rule type (transaction, order, user)
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Rule parameters in JSON format
        /// </summary>
        [BsonElement("parameters")]
        public string Parameters { get; set; } = "{}";

        /// <summary>
        /// Whether the rule is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

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
        public override List<Tuple<IndexKeysDefinition<RiskRule>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<RiskRule>, bool>>
            {
                new Tuple<IndexKeysDefinition<RiskRule>, bool>(
                    Builders<RiskRule>.IndexKeys.Ascending(r => r.Name),
                    true
                )
            };
        }
    }
}