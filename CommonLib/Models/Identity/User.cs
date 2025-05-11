using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Identity
{
    /// <summary>
    /// User model
    /// </summary>
    public class User : IndexedModel<User>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "IdentityDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Users";

        /// <summary>
        /// Username
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password
        /// </summary>
        [BsonElement("hashedPassword")]
        public string HashedPassword { get; set; } = string.Empty;

        /// <summary>
        /// Phone number
        /// </summary>
        [BsonElement("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Update time
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether user is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether email is verified
        /// </summary>
        [BsonElement("isEmailVerified")]
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// 是否启用双因素认证
        /// </summary>
        [BsonElement("isTwoFactorEnabled")]
        public bool IsTwoFactorEnabled { get; set; } = false;

        /// <summary>
        /// Whether two-factor authentication is enabled
        /// </summary>
        [BsonElement("twoFactorSecret")]
        public string? TwoFactorSecret { get; set; }

        /// <summary>
        /// User roles
        /// </summary>
        [BsonElement("roles")]
        public List<Role> Roles { get; set; } = new();

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<User>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<User>, bool>>
            {
                new Tuple<IndexKeysDefinition<User>, bool>(
                    Builders<User>.IndexKeys.Ascending(u => u.Username),
                    true
                ),
                new Tuple<IndexKeysDefinition<User>, bool>(
                    Builders<User>.IndexKeys.Ascending(u => u.Email),
                    true
                )
            };
        }
    }
}