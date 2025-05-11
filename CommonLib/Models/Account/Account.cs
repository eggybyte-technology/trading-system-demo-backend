using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Account model
    /// </summary>
    public class Account : IndexedModel<Account>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "AccountDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Accounts";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

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
        /// Account status
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "active";

        /// <summary>
        /// List of account balances
        /// </summary>
        [BsonElement("balances")]
        public List<Balance> Balances { get; set; } = new();

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Account>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Account>, bool>>
            {
                new Tuple<IndexKeysDefinition<Account>, bool>(
                    Builders<Account>.IndexKeys.Ascending(a => a.UserId),
                    true
                )
            };
        }
    }
}