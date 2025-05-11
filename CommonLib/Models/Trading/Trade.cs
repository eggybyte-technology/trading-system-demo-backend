using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CommonLib.Models.Trading
{
    /// <summary>
    /// Represents a trade/execution between two orders
    /// </summary>
    public class Trade : IndexedModel<Trade>
    {
        /// <summary>
        /// Database name
        /// </summary>
        public override string Database => "TradingDb";

        /// <summary>
        /// Collection name
        /// </summary>
        public override string Collection => "Trades";

        /// <summary>
        /// Symbol that was traded
        /// </summary>
        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// ID of the order that was executed
        /// </summary>
        [BsonElement("orderId")]
        public ObjectId OrderId { get; set; }

        /// <summary>
        /// ID of the buyer's order
        /// </summary>
        [BsonElement("buyerOrderId")]
        public ObjectId BuyerOrderId { get; set; }

        /// <summary>
        /// ID of the seller's order
        /// </summary>
        [BsonElement("sellerOrderId")]
        public ObjectId SellerOrderId { get; set; }

        /// <summary>
        /// ID of the buyer
        /// </summary>
        [BsonElement("buyerUserId")]
        public ObjectId BuyerUserId { get; set; }

        /// <summary>
        /// ID of the seller
        /// </summary>
        [BsonElement("sellerUserId")]
        public ObjectId SellerUserId { get; set; }

        /// <summary>
        /// User ID (for query convenience, not stored in database)
        /// </summary>
        [BsonIgnore]
        public ObjectId UserId { get; set; }

        /// <summary>
        /// Price at which the trade executed
        /// </summary>
        [BsonElement("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity that was traded
        /// </summary>
        [BsonElement("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Fee paid by the buyer
        /// </summary>
        [BsonElement("buyerFee")]
        public decimal BuyerFee { get; set; }

        /// <summary>
        /// Fee paid by the seller
        /// </summary>
        [BsonElement("sellerFee")]
        public decimal SellerFee { get; set; }

        /// <summary>
        /// Asset in which the buyer's fee was charged
        /// </summary>
        [BsonElement("buyerFeeAsset")]
        public string BuyerFeeAsset { get; set; } = string.Empty;

        /// <summary>
        /// Asset in which the seller's fee was charged
        /// </summary>
        [BsonElement("sellerFeeAsset")]
        public string SellerFeeAsset { get; set; } = string.Empty;

        /// <summary>
        /// When the trade was executed
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the buyer was the maker in this trade
        /// </summary>
        [BsonElement("isBuyerMaker")]
        public bool IsBuyerMaker { get; set; }

        /// <summary>
        /// Gets the list of indexes for this model
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public override List<Tuple<IndexKeysDefinition<Trade>, bool>> GetIndexes()
        {
            return new List<Tuple<IndexKeysDefinition<Trade>, bool>>
            {
                new Tuple<IndexKeysDefinition<Trade>, bool>(
                    Builders<Trade>.IndexKeys.Ascending(t => t.Symbol),
                    false
                ),
                new Tuple<IndexKeysDefinition<Trade>, bool>(
                    Builders<Trade>.IndexKeys.Ascending(t => t.BuyerUserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Trade>, bool>(
                    Builders<Trade>.IndexKeys.Ascending(t => t.SellerUserId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Trade>, bool>(
                    Builders<Trade>.IndexKeys.Ascending(t => t.OrderId),
                    false
                ),
                new Tuple<IndexKeysDefinition<Trade>, bool>(
                    Builders<Trade>.IndexKeys.Descending(t => t.CreatedAt),
                    false
                )
            };
        }
    }
}