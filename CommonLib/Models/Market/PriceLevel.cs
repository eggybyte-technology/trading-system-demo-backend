using MongoDB.Bson.Serialization.Attributes;

namespace CommonLib.Models.Market
{
    /// <summary>
    /// Represents a price level in an order book
    /// </summary>
    public class PriceLevel
    {
        /// <summary>
        /// Price of the level
        /// </summary>
        [BsonElement("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Total quantity at this price level
        /// </summary>
        [BsonElement("quantity")]
        public decimal Quantity { get; set; }
    }
}