using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CommonLib.Models.Trading;
using MatchMakingService.Repositories;

namespace MatchMakingService.Services
{
    /// <summary>
    /// Service responsible for order matching operations
    /// </summary>
    public class OrderMatchingService
    {
        private readonly ILogger<OrderMatchingService> _logger;
        private readonly OrderMatcherRepository _matcherRepository;
        private readonly MatchingJobRepository _jobRepository;
        private readonly OrderRepository _orderRepository;
        private readonly IConfiguration _configuration;
        private readonly int _lockTimeoutSeconds;

        /// <summary>
        /// Initializes a new instance of the OrderMatchingService class
        /// </summary>
        public OrderMatchingService(
            ILogger<OrderMatchingService> logger,
            OrderMatcherRepository matcherRepository,
            MatchingJobRepository jobRepository,
            OrderRepository orderRepository,
            IConfiguration configuration)
        {
            _logger = logger;
            _matcherRepository = matcherRepository;
            _jobRepository = jobRepository;
            _orderRepository = orderRepository;
            _configuration = configuration;
            _lockTimeoutSeconds = _configuration.GetValue<int>("MatchMaking:OrderLockTimeoutSeconds", 60);
        }

        /// <summary>
        /// Process all pending orders for matching
        /// </summary>
        public async Task ProcessAllPendingOrdersAsync(CancellationToken cancellationToken)
        {
            // First, unlock any orders that have timed out
            await UnlockTimedOutOrdersAsync(cancellationToken);

            // Get all active matchers
            var activeMatchers = await _matcherRepository.GetActiveMatchersAsync(cancellationToken);

            foreach (var matcher in activeMatchers)
            {
                try
                {
                    // Process each symbol
                    await ProcessSymbolOrdersAsync(matcher, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing orders for symbol {Symbol}", matcher.Symbol);
                }
            }
        }

        /// <summary>
        /// Unlock any orders that have been locked for too long
        /// </summary>
        private async Task UnlockTimedOutOrdersAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddSeconds(-_lockTimeoutSeconds);
                _logger.LogDebug("Checking for locked orders with timeout > {TimeoutSeconds}s (locked before {CutoffTime})",
                    _lockTimeoutSeconds, cutoffTime);

                var unlockedCount = await _orderRepository.UnlockTimedOutOrdersAsync(_lockTimeoutSeconds, cancellationToken);

                if (unlockedCount > 0)
                {
                    _logger.LogWarning("Found and unlocked {Count} timed-out orders that exceeded the lock timeout of {TimeoutSeconds}s",
                        unlockedCount, _lockTimeoutSeconds);
                }
                else
                {
                    _logger.LogDebug("No timed-out locked orders found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking timed-out orders");
            }
        }

        /// <summary>
        /// Process orders for a specific symbol
        /// </summary>
        private async Task ProcessSymbolOrdersAsync(OrderMatcher matcher, CancellationToken cancellationToken)
        {
            var processingStartTime = DateTime.UtcNow;
            // Create a new matching job
            var job = new MatchingJob
            {
                Symbol = matcher.Symbol,
                StartedAt = processingStartTime,
                Status = "RUNNING"
            };

            await _jobRepository.CreateJobAsync(job, cancellationToken);
            _logger.LogInformation("Starting matching job {JobId} for symbol {Symbol}", job.Id, matcher.Symbol);

            List<Order> buyOrders = new List<Order>();
            List<Order> sellOrders = new List<Order>();

            try
            {
                var queryStartTime = DateTime.UtcNow;
                // Get all active buy orders sorted by price (highest first) and time
                buyOrders = await _orderRepository.GetActiveBuyOrdersAsync(
                    matcher.Symbol,
                    matcher.BatchSize,
                    cancellationToken);

                // Get all active sell orders sorted by price (lowest first) and time
                sellOrders = await _orderRepository.GetActiveSellOrdersAsync(
                    matcher.Symbol,
                    matcher.BatchSize,
                    cancellationToken);

                var queryEndTime = DateTime.UtcNow;
                var queryTimeMs = (long)(queryEndTime - queryStartTime).TotalMilliseconds;
                _logger.LogDebug("Order query completed in {QueryTimeMs}ms - Found {BuyCount} buy orders and {SellCount} sell orders",
                    queryTimeMs, buyOrders.Count, sellOrders.Count);

                // If no orders to process, just return
                if (buyOrders.Count == 0 || sellOrders.Count == 0)
                {
                    _logger.LogInformation("No matching possible for {Symbol} - Orders: Buy={BuyCount}, Sell={SellCount}",
                        matcher.Symbol, buyOrders.Count, sellOrders.Count);

                    job.CompletedAt = DateTime.UtcNow;
                    job.Status = "COMPLETED";
                    job.OrdersProcessed = 0;
                    job.TradesGenerated = 0;
                    job.ProcessingTimeMs = 0;

                    await _jobRepository.UpdateJobAsync(job, cancellationToken);
                    return;
                }

                // Lock all orders for processing
                var allOrders = new List<Order>();
                allOrders.AddRange(buyOrders);
                allOrders.AddRange(sellOrders);

                _logger.LogInformation("Locking {OrderCount} orders for matching for symbol {Symbol}",
                    allOrders.Count, matcher.Symbol);

                var lockStartTime = DateTime.UtcNow;
                await _orderRepository.LockOrdersAsync(allOrders, job.Id, cancellationToken);
                var lockEndTime = DateTime.UtcNow;
                var lockTimeMs = (long)(lockEndTime - lockStartTime).TotalMilliseconds;
                _logger.LogDebug("Orders locked in {LockTimeMs}ms", lockTimeMs);

                // Match orders
                var matchStartTime = DateTime.UtcNow;
                var matchResults = MatchOrders(buyOrders, sellOrders);
                var matchEndTime = DateTime.UtcNow;
                var matchTimeMs = (long)(matchEndTime - matchStartTime).TotalMilliseconds;
                _logger.LogDebug("Order matching algorithm completed in {MatchTimeMs}ms", matchTimeMs);

                // Process match results
                if (matchResults.Count > 0)
                {
                    // Update orders with trade information
                    var ordersToUpdate = new List<Order>();
                    ordersToUpdate.AddRange(buyOrders.Where(o => o.ExecutedQuantity > 0));
                    ordersToUpdate.AddRange(sellOrders.Where(o => o.ExecutedQuantity > 0));

                    if (ordersToUpdate.Count > 0)
                    {
                        var updateStartTime = DateTime.UtcNow;
                        await _orderRepository.UpdateOrdersAsync(ordersToUpdate, cancellationToken);
                        var updateEndTime = DateTime.UtcNow;
                        var updateTimeMs = (long)(updateEndTime - updateStartTime).TotalMilliseconds;
                        _logger.LogDebug("Updated {OrderCount} orders in {UpdateTimeMs}ms", ordersToUpdate.Count, updateTimeMs);
                    }

                    // Insert new trades
                    var tradeStartTime = DateTime.UtcNow;
                    await _orderRepository.CreateTradesAsync(matchResults, cancellationToken);
                    var tradeEndTime = DateTime.UtcNow;
                    var tradeTimeMs = (long)(tradeEndTime - tradeStartTime).TotalMilliseconds;
                    _logger.LogDebug("Created {TradeCount} trades in {TradeTimeMs}ms", matchResults.Count, tradeTimeMs);

                    // Update job with trade IDs
                    job.TradeIds = matchResults.Select(t => t.Id).ToList();

                    // Log detailed trade information
                    var totalVolume = matchResults.Sum(t => t.Quantity * t.Price);
                    var avgPrice = matchResults.Count > 0 ? totalVolume / matchResults.Sum(t => t.Quantity) : 0;
                    _logger.LogInformation("Created {TradeCount} trades for symbol {Symbol} - Total volume: {Volume}, Avg price: {AvgPrice}",
                        matchResults.Count, matcher.Symbol, totalVolume, avgPrice);
                }
                else
                {
                    _logger.LogInformation("No matches found for symbol {Symbol} despite having both buy and sell orders", matcher.Symbol);
                }

                // Update job with results
                job.CompletedAt = DateTime.UtcNow;
                job.Status = "COMPLETED";
                job.OrdersProcessed = buyOrders.Count + sellOrders.Count;
                job.TradesGenerated = matchResults.Count;
                job.ProcessingTimeMs = (long)(job.CompletedAt - processingStartTime).Value.TotalMilliseconds;
                job.TotalVolume = matchResults.Sum(t => t.Quantity * t.Price);

                // Update the job in the database
                await _jobRepository.UpdateJobAsync(job, cancellationToken);
                _logger.LogInformation("Completed matching job {JobId} for symbol {Symbol} in {ProcessingTimeMs}ms - Generated {TradeCount} trades",
                    job.Id, matcher.Symbol, job.ProcessingTimeMs, job.TradesGenerated);

                // Update matcher statistics
                matcher.LastMatchTime = DateTime.UtcNow;
                matcher.Stats.TotalOrdersProcessed += job.OrdersProcessed;
                matcher.Stats.TotalTradesGenerated += job.TradesGenerated;
                matcher.Stats.LastMatchTimeMs = job.ProcessingTimeMs;

                // Update moving average of matching time
                if (matcher.Stats.AverageMatchTimeMs == 0)
                {
                    matcher.Stats.AverageMatchTimeMs = job.ProcessingTimeMs;
                }
                else
                {
                    matcher.Stats.AverageMatchTimeMs =
                        (matcher.Stats.AverageMatchTimeMs * 0.9) + (job.ProcessingTimeMs * 0.1);
                }

                // Update the matcher in the database
                await _matcherRepository.UpdateMatcherAsync(matcher, cancellationToken);
            }
            catch (Exception ex)
            {
                // Update job with error information
                job.Status = "FAILED";
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = ex.Message;

                await _jobRepository.UpdateJobAsync(job, cancellationToken);

                _logger.LogError(ex, "Error during order matching for symbol {Symbol} - Job {JobId}", matcher.Symbol, job.Id);
                throw;
            }
            finally
            {
                // Always unlock orders when done, regardless of success or failure
                if (buyOrders.Count > 0 || sellOrders.Count > 0)
                {
                    var allOrders = new List<Order>();
                    allOrders.AddRange(buyOrders);
                    allOrders.AddRange(sellOrders);

                    try
                    {
                        _logger.LogInformation("Unlocking {OrderCount} orders after matching for symbol {Symbol}",
                            allOrders.Count, matcher.Symbol);

                        var unlockStartTime = DateTime.UtcNow;
                        await _orderRepository.UnlockOrdersAsync(allOrders, cancellationToken);
                        var unlockTimeMs = (long)(DateTime.UtcNow - unlockStartTime).TotalMilliseconds;
                        _logger.LogDebug("Orders unlocked in {UnlockTimeMs}ms", unlockTimeMs);
                    }
                    catch (Exception unlockEx)
                    {
                        _logger.LogError(unlockEx, "Error unlocking orders for symbol {Symbol}", matcher.Symbol);
                        // Don't rethrow - we still want to log the original error if there was one
                    }
                }
            }
        }

        /// <summary>
        /// Match buy and sell orders to create trades
        /// </summary>
        private List<Trade> MatchOrders(List<Order> buyOrders, List<Order> sellOrders)
        {
            var resultingTrades = new List<Trade>();

            // Only perform matching if we have both buy and sell orders
            if (buyOrders.Count == 0 || sellOrders.Count == 0)
            {
                return resultingTrades;
            }

            // Iterate through all buy orders
            foreach (var buyOrder in buyOrders)
            {
                // Skip fully filled buy orders
                if (buyOrder.ExecutedQuantity >= buyOrder.OriginalQuantity)
                {
                    continue;
                }

                // Calculate remaining quantity to be filled
                var remainingQuantity = buyOrder.OriginalQuantity - buyOrder.ExecutedQuantity;

                // Match against sell orders
                foreach (var sellOrder in sellOrders)
                {
                    // Skip fully filled sell orders
                    if (sellOrder.ExecutedQuantity >= sellOrder.OriginalQuantity)
                    {
                        continue;
                    }

                    // Check if prices match (buy price >= sell price for a match)
                    if (buyOrder.Price < sellOrder.Price)
                    {
                        continue;
                    }

                    // Calculate the sell order's remaining quantity
                    var sellRemainingQuantity = sellOrder.OriginalQuantity - sellOrder.ExecutedQuantity;

                    // Determine the matched quantity (minimum of buy and sell remaining quantities)
                    var matchedQuantity = Math.Min(remainingQuantity, sellRemainingQuantity);

                    // Create a new trade
                    var trade = new Trade
                    {
                        Symbol = buyOrder.Symbol,
                        OrderId = sellOrder.Id, // For compatibility with existing code
                        BuyerOrderId = buyOrder.Id,
                        SellerOrderId = sellOrder.Id,
                        BuyerUserId = buyOrder.UserId,
                        SellerUserId = sellOrder.UserId,
                        Price = sellOrder.Price, // Use the sell price as the execution price
                        Quantity = matchedQuantity,
                        CreatedAt = DateTime.UtcNow,
                        IsBuyerMaker = false // The buyer is the taker in this case (arrived second)
                    };

                    // Add the trade to the results
                    resultingTrades.Add(trade);

                    // Update the executed quantities
                    buyOrder.ExecutedQuantity += matchedQuantity;
                    sellOrder.ExecutedQuantity += matchedQuantity;

                    // Update order status based on execution
                    if (buyOrder.ExecutedQuantity >= buyOrder.OriginalQuantity)
                    {
                        buyOrder.Status = "FILLED";
                        buyOrder.IsWorking = false;
                    }
                    else
                    {
                        buyOrder.Status = "PARTIALLY_FILLED";
                    }

                    if (sellOrder.ExecutedQuantity >= sellOrder.OriginalQuantity)
                    {
                        sellOrder.Status = "FILLED";
                        sellOrder.IsWorking = false;
                    }
                    else
                    {
                        sellOrder.Status = "PARTIALLY_FILLED";
                    }

                    // Add the trade to the orders' trade lists
                    buyOrder.Trades.Add(trade);

                    // Update the remaining quantity
                    remainingQuantity -= matchedQuantity;

                    // Break if the buy order is fully filled
                    if (remainingQuantity <= 0)
                    {
                        break;
                    }
                }
            }

            return resultingTrades;
        }
    }
}