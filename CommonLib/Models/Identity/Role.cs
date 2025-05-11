using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// User role
    /// </summary>
    public class Role : IndexedModel<Role>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "IdentityDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Roles";

        /// <summary>
        /// Role name
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Role description
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// List of permissions
        /// </summary>
        [BsonElement("permissions")]
        public List<string> Permissions { get; set; } = new();

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
        public override List<Tuple<IndexKeysDefinition<Role>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Role>, bool>>
            {
                new Tuple<IndexKeysDefinition<Role>, bool>(
                    Builders<Role>.IndexKeys.Ascending(r => r.Name),
                    true
                )
            };
        }
    }
}