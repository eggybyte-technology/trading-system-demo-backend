using System;
using System.Collections.Generic;
using CommonLib.Models.Trading;

namespace SimulationTest.Strategies
{
    /// <summary>
    /// Contains strategies for generating different types of orders
    /// </summary>
    public static class SimulationStrategies
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a random LIMIT order for a given symbol
        /// </summary>
        /// <param name="symbol">The trading symbol</param>
        /// <param name="priceRanges">Dictionary of price ranges for each symbol</param>
        /// <returns>A random order request</returns>
        public static CreateOrderRequest GenerateRandomOrder(string symbol, Dictionary<string, (decimal Min, decimal Max)> priceRanges)
        {
            // Determine price range for the symbol
            decimal minPrice = 90.0m;
            decimal maxPrice = 110.0m;

            if (priceRanges.TryGetValue(symbol, out var range))
            {
                minPrice = range.Min;
                maxPrice = range.Max;
            }

            // Generate a random price within the range
            var price = GenerateRandomPrice(minPrice, maxPrice);

            // Generate a random quantity (0.01 to 1.0)
            var quantity = Math.Round(0.01m + (decimal)_random.NextDouble() * 0.99m, 2);

            // Generate a random side (BUY or SELL)
            var side = _random.Next(2) == 0 ? "BUY" : "SELL";

            return new CreateOrderRequest
            {
                Symbol = symbol,
                Side = side,
                Type = "LIMIT",
                TimeInForce = "GTC",
                Quantity = quantity,
                Price = price
            };
        }

        /// <summary>
        /// Generates a random MARKET order for a given symbol
        /// </summary>
        /// <param name="symbol">The trading symbol</param>
        /// <returns>A market order request</returns>
        public static CreateOrderRequest GenerateMarketOrder(string symbol)
        {
            // Generate a random quantity (0.01 to 1.0)
            var quantity = Math.Round(0.01m + (decimal)_random.NextDouble() * 0.99m, 2);

            // Generate a random side (BUY or SELL)
            var side = _random.Next(2) == 0 ? "BUY" : "SELL";

            return new CreateOrderRequest
            {
                Symbol = symbol,
                Side = side,
                Type = "MARKET",
                TimeInForce = "IOC", // Immediate or Cancel for market orders
                Quantity = quantity,
                Price = 0 // Market orders don't need a price
            };
        }

        /// <summary>
        /// Generates a random limit price
        /// </summary>
        /// <param name="min">Minimum price</param>
        /// <param name="max">Maximum price</param>
        /// <returns>A random price</returns>
        private static decimal GenerateRandomPrice(decimal min, decimal max)
        {
            // Generate a random price within the range
            var range = max - min;
            var sample = (decimal)_random.NextDouble();
            var scaled = sample * range + min;
            return Math.Round(scaled, 2);
        }
    }
}