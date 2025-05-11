using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Account
{
    /// <summary>
    /// Withdrawal request
    /// </summary>
    public class WithdrawalRequest : IndexedModel<WithdrawalRequest>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "AccountDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "WithdrawalRequests";

        /// <summary>
        /// User ID
        /// </summary>
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Asset type
        /// </summary>
        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;

        /// <summary>
        /// Withdrawal amount
        /// </summary>
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Destination address
        /// </summary>
        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Memo/tag (for assets that require it)
        /// </summary>
        [BsonElement("memo")]
        public string? Memo { get; set; }

        /// <summary>
        /// Withdrawal status (pending, processing, completed, rejected)
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Request creation time
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Request completion time
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Rejection reason if rejected
        /// </summary>
        [BsonElement("rejectionReason")]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Associated transaction ID
        /// </summary>
        [BsonElement("transactionId")]
        public ObjectId? TransactionId { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<WithdrawalRequest>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<WithdrawalRequest>, bool>>
            {
                new Tuple<IndexKeysDefinition<WithdrawalRequest>, bool>(
                    Builders<WithdrawalRequest>.IndexKeys.Ascending(w => w.UserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<WithdrawalRequest>, bool>(
                    Builders<WithdrawalRequest>.IndexKeys.Ascending(w => w.Status),
                    false
                )
            };
        }
    }
}