using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Account;
using CommonLib.Models.MatchMaking;
using CommonLib.Models.Trading;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using CommonLib.Api;

namespace MatchMakingService.Services
{
    public class MatchMakingService : IMatchMakingService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;
        private readonly CommonLib.Api.TradingService _tradingService;
        private readonly CommonLib.Api.AccountService _accountService;
        private readonly Dictionary<string, object> _lockObjects = new Dictionary<string, object>();
        private bool _isMatchingRunning = false;
        private DateTime _lastRunTime = DateTime.MinValue;
        private DateTime _nextScheduledRun = DateTime.MinValue;
        private int _processedBatchesCount = 0;
        private readonly MatchingSettingsResponse _settings;
        private readonly string _serviceAuthToken;

        public MatchMakingService(
            MongoDbConnectionFactory dbFactory,
            ILoggerService logger,
            CommonLib.Api.TradingService tradingService,
            CommonLib.Api.AccountService accountService,
            IConfiguration configuration)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _tradingService = tradingService;
            _accountService = accountService;

            // Get system token for inter-service communication
            var jwtSection = configuration.GetSection("JwtSettings");
            _serviceAuthToken = jwtSection["ServiceToken"] ?? "default-service-token";

            // Initialize settings with defaults or from configuration
            var matchingSettings = configuration.GetSection("MatchingSettings");
            _settings = new MatchingSettingsResponse
            {
                MatchingIntervalSeconds = matchingSettings.GetValue<int>("MatchingIntervalSeconds", 10),
                OrderLockTimeoutSeconds = matchingSettings.GetValue<int>("OrderLockTimeoutSeconds", 5),
                EnabledSymbols = matchingSettings.GetSection("EnabledSymbols").Get<string[]>() ??
                    new[] { "BTC-USDT", "ETH-USDT", "BNB-USDT" },
                MaxOrdersPerBatch = matchingSettings.GetValue<int>("MaxOrdersPerBatch", 500),
                MaxTradesPerBatch = matchingSettings.GetValue<int>("MaxTradesPerBatch", 200),
                IsMatchingEnabled = matchingSettings.GetValue<bool>("IsMatchingEnabled", true)
            };

            // Start the periodic matching task
            _ = StartPeriodicMatchingAsync();
        }

        /// <summary>
        /// Starts the periodic matching task
        /// </summary>
        private async Task StartPeriodicMatchingAsync()
        {
            while (true)
            {
                try
                {
                    if (_settings.IsMatchingEnabled)
                    {
                        await RunMatchingCycleAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in matching cycle: {ex.Message}");
                }

                // Wait for the next interval
                await Task.Delay(TimeSpan.FromSeconds(_settings.MatchingIntervalSeconds));
            }
        }

        /// <summary>
        /// Runs a single matching cycle
        /// </summary>
        private async Task RunMatchingCycleAsync()
        {
            if (_isMatchingRunning)
            {
                _logger.LogWarning("Matching cycle already running, skipping this cycle");
                return;
            }

            _isMatchingRunning = true;
            _lastRunTime = DateTime.UtcNow;
            _nextScheduledRun = _lastRunTime.AddSeconds(_settings.MatchingIntervalSeconds);

            try
            {
                var batchId = ObjectId.GenerateNewId().ToString();
                _logger.LogInformation($"Starting matching cycle with batch ID {batchId}");

                // Process each enabled symbol
                foreach (var symbol in _settings.EnabledSymbols)
                {
                    await ProcessSymbolOrdersAsync(symbol, batchId);
                }

                _processedBatchesCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during matching cycle: {ex.Message}");
            }
            finally
            {
                _isMatchingRunning = false;
            }
        }

        /// <summary>
        /// Process orders for a specific symbol
        /// </summary>
        private async Task ProcessSymbolOrdersAsync(string symbol, string batchId)
        {
            _logger.LogInformation($"Processing orders for symbol {symbol}");

            try
            {
                // 1. Get open orders from TradingService
                var openOrders = await GetOpenOrdersAsync(symbol);
                if (openOrders == null || !openOrders.Any())
                {
                    _logger.LogInformation($"No open orders found for symbol {symbol}");
                    return;
                }

                // 2. Sort and group orders by side
                var buyOrders = openOrders
                    .Where(o => o.Side.ToUpper() == "BUY")
                    .OrderByDescending(o => o.Price)
                    .ThenBy(o => o.Time)
                    .ToList();

                var sellOrders = openOrders
                    .Where(o => o.Side.ToUpper() == "SELL")
                    .OrderBy(o => o.Price)
                    .ThenBy(o => o.Time)
                    .ToList();

                if (!buyOrders.Any() || !sellOrders.Any())
                {
                    _logger.LogInformation($"Not enough orders to match for symbol {symbol}");
                    return;
                }

                // 3. Match orders
                await MatchOrdersAsync(buyOrders, sellOrders, symbol, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing orders for symbol {symbol}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get open orders from Trading Service
        /// </summary>
        private async Task<List<OrderResponse>> GetOpenOrdersAsync(string symbol)
        {
            try
            {
                var response = await _tradingService.GetOpenOrdersAsync(_serviceAuthToken, symbol);
                return response?.Orders ?? new List<OrderResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting open orders: {ex.Message}");
                return new List<OrderResponse>();
            }
        }

        /// <summary>
        /// Match buy and sell orders
        /// </summary>
        private async Task MatchOrdersAsync(List<OrderResponse> buyOrders, List<OrderResponse> sellOrders, string symbol, string batchId)
        {
            var matchingResults = new MatchingResultResponse
            {
                BatchId = batchId,
                Success = true,
                ProcessedOrdersCount = buyOrders.Count + sellOrders.Count,
                MatchedOrdersCount = 0,
                CreatedTradesCount = 0,
                TradesPerSymbol = new Dictionary<string, int> { { symbol, 0 } },
                VolumePerSymbol = new Dictionary<string, decimal> { { symbol, 0 } },
                ProcessingTimeMs = 0
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Get symbol information to determine base and quote assets
                var parts = symbol.Split('-');
                if (parts.Length != 2)
                {
                    _logger.LogError($"Invalid symbol format: {symbol}");
                    return;
                }

                string baseAsset = parts[0];
                string quoteAsset = parts[1];

                int buyIndex = 0;
                int sellIndex = 0;

                while (buyIndex < buyOrders.Count && sellIndex < sellOrders.Count)
                {
                    var buyOrder = buyOrders[buyIndex];
                    var sellOrder = sellOrders[sellIndex];

                    // Check if orders can match based on price
                    if (buyOrder.Price >= sellOrder.Price)
                    {
                        // Try to lock both orders and their balances
                        var (buyOrderLocked, buyLockId) = await LockOrderAsync(buyOrder.OrderId);
                        if (!buyOrderLocked)
                        {
                            buyIndex++;
                            continue;
                        }

                        var (sellOrderLocked, sellLockId) = await LockOrderAsync(sellOrder.OrderId);
                        if (!sellOrderLocked)
                        {
                            await UnlockOrderAsync(buyOrder.OrderId, buyLockId);
                            sellIndex++;
                            continue;
                        }

                        // Try to lock buyer's quote asset balance
                        var buyerBalanceLockRequest = new LockBalanceRequest
                        {
                            UserId = buyOrder.UserId,
                            Asset = quoteAsset,
                            Amount = sellOrder.Price * Math.Min(buyOrder.OrigQty - buyOrder.ExecutedQty, sellOrder.OrigQty - sellOrder.ExecutedQty),
                            OrderId = buyOrder.OrderId,
                            TimeoutSeconds = _settings.OrderLockTimeoutSeconds
                        };

                        var buyerBalanceLocked = await LockBalanceAsync(buyerBalanceLockRequest);
                        if (!buyerBalanceLocked.Success)
                        {
                            await UnlockOrderAsync(buyOrder.OrderId, buyLockId);
                            await UnlockOrderAsync(sellOrder.OrderId, sellLockId);
                            buyIndex++;
                            continue;
                        }

                        // Try to lock seller's base asset balance
                        var sellerBalanceLockRequest = new LockBalanceRequest
                        {
                            UserId = sellOrder.UserId,
                            Asset = baseAsset,
                            Amount = Math.Min(buyOrder.OrigQty - buyOrder.ExecutedQty, sellOrder.OrigQty - sellOrder.ExecutedQty),
                            OrderId = sellOrder.OrderId,
                            TimeoutSeconds = _settings.OrderLockTimeoutSeconds
                        };

                        var sellerBalanceLocked = await LockBalanceAsync(sellerBalanceLockRequest);
                        if (!sellerBalanceLocked.Success)
                        {
                            await UnlockOrderAsync(buyOrder.OrderId, buyLockId);
                            await UnlockOrderAsync(sellOrder.OrderId, sellLockId);
                            await UnlockBalanceAsync(new UnlockBalanceRequest
                            {
                                UserId = buyOrder.UserId,
                                Asset = quoteAsset,
                            });
                            sellIndex++;
                            continue;
                        }

                        // Calculate trade quantity and execute trade
                        decimal matchQuantity = Math.Min(
                            buyOrder.OrigQty - buyOrder.ExecutedQty,
                            sellOrder.OrigQty - sellOrder.ExecutedQty
                        );

                        // Use the price of the order that was placed first (maker's price)
                        decimal matchPrice = buyOrder.Time < sellOrder.Time ? buyOrder.Price : sellOrder.Price;

                        // Create the trade
                        var tradeResult = await ExecuteTradeAsync(new ExecuteTradeRequest
                        {
                            BuyOrderId = buyOrder.OrderId,
                            SellOrderId = sellOrder.OrderId,
                            BuyerUserId = buyOrder.UserId,
                            SellerUserId = sellOrder.UserId,
                            BaseAsset = baseAsset,
                            QuoteAsset = quoteAsset,
                            Quantity = matchQuantity,
                            Price = matchPrice,
                            MatchId = Guid.NewGuid().ToString()
                        });

                        if (tradeResult.Success)
                        {
                            // Update order status
                            await UpdateOrderStatusAsync(buyOrder.OrderId, buyOrder.ExecutedQty + matchQuantity, buyLockId);
                            await UpdateOrderStatusAsync(sellOrder.OrderId, sellOrder.ExecutedQty + matchQuantity, sellLockId);

                            // Update matching results
                            matchingResults.MatchedOrdersCount += 2;
                            matchingResults.CreatedTradesCount++;
                            matchingResults.TradesPerSymbol[symbol]++;
                            matchingResults.VolumePerSymbol[symbol] += matchQuantity * matchPrice;

                            _logger.LogInformation($"Created trade: {matchQuantity} {baseAsset} at {matchPrice} {quoteAsset}");
                        }
                        else
                        {
                            // Unlock orders as the trade failed
                            await UnlockOrderAsync(buyOrder.OrderId, buyLockId);
                            await UnlockOrderAsync(sellOrder.OrderId, sellLockId);
                            _logger.LogError($"Failed to execute trade: {tradeResult.ErrorMessage}");
                        }

                        // Move to next order if filled completely
                        if (buyOrder.OrigQty <= buyOrder.ExecutedQty + matchQuantity)
                        {
                            buyIndex++;
                        }

                        if (sellOrder.OrigQty <= sellOrder.ExecutedQty + matchQuantity)
                        {
                            sellIndex++;
                        }
                    }
                    else
                    {
                        // Orders cannot match by price, move on
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during order matching: {ex.Message}");
                matchingResults.Success = false;
                matchingResults.Errors = new[] { ex.Message };
            }
            finally
            {
                matchingResults.ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Store matching results in database for history
                await SaveMatchingResultsAsync(matchingResults);
            }
        }

        /// <summary>
        /// Lock an order for processing
        /// </summary>
        private async Task<(bool success, string lockId)> LockOrderAsync(string orderId)
        {
            try
            {
                var lockId = Guid.NewGuid().ToString();
                var lockRequest = new LockOrderRequest
                {
                    OrderId = orderId,
                    LockId = lockId,
                    TimeoutSeconds = _settings.OrderLockTimeoutSeconds
                };

                var response = await _tradingService.LockOrderAsync(_serviceAuthToken, lockRequest);
                return (response?.Success ?? false, lockId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking order {orderId}: {ex.Message}");
                return (false, string.Empty);
            }
        }

        /// <summary>
        /// Unlock an order after processing
        /// </summary>
        private async Task<bool> UnlockOrderAsync(string orderId, string lockId)
        {
            try
            {
                var unlockRequest = new UnlockOrderRequest
                {
                    OrderId = orderId,
                    LockId = lockId
                };

                var response = await _tradingService.UnlockOrderAsync(_serviceAuthToken, unlockRequest);
                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking order {orderId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lock balance for a trade
        /// </summary>
        private async Task<LockBalanceResponse> LockBalanceAsync(LockBalanceRequest request)
        {
            try
            {
                var response = await _accountService.LockBalanceAsync(_serviceAuthToken, request);
                return response ?? new LockBalanceResponse { Success = false, ErrorMessage = "Null response from service" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking balance for user {request.UserId}, asset {request.Asset}: {ex.Message}");
                return new LockBalanceResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Unlock balance after processing
        /// </summary>
        private async Task<bool> UnlockBalanceAsync(UnlockBalanceRequest request)
        {
            try
            {
                var response = await _accountService.UnlockBalanceAsync(_serviceAuthToken, request);
                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking balance for user {request.UserId}, asset {request.Asset}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute a trade with locked balances
        /// </summary>
        private async Task<ExecuteTradeResponse> ExecuteTradeAsync(ExecuteTradeRequest request)
        {
            try
            {
                var response = await _accountService.ExecuteTradeAsync(_serviceAuthToken, request);
                return response ?? new ExecuteTradeResponse { Success = false, ErrorMessage = "Null response from service" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing trade between {request.BuyerUserId} and {request.SellerUserId}: {ex.Message}");
                return new ExecuteTradeResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Update order status after matching
        /// </summary>
        private async Task<bool> UpdateOrderStatusAsync(string orderId, decimal executedQty, string lockId)
        {
            try
            {
                var updateRequest = new UpdateOrderStatusRequest
                {
                    OrderId = orderId,
                    Status = executedQty > 0 ? "PARTIALLY_FILLED" : "FILLED",
                    ExecutedQuantity = executedQty,
                    CumulativeQuoteQuantity = 0, // This should be calculated properly
                    LockId = lockId
                };

                var response = await _tradingService.UpdateOrderStatusAsync(_serviceAuthToken, updateRequest);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status for {orderId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save matching results to database
        /// </summary>
        private async Task SaveMatchingResultsAsync(MatchingResultResponse result)
        {
            try
            {
                var collection = _dbFactory.GetCollection<MatchingResultDocument>("matchmaking", "matchResults");
                var document = new MatchingResultDocument
                {
                    BatchId = result.BatchId,
                    Timestamp = DateTime.UtcNow,
                    ProcessedOrdersCount = result.ProcessedOrdersCount,
                    MatchedOrdersCount = result.MatchedOrdersCount,
                    CreatedTradesCount = result.CreatedTradesCount,
                    TradesPerSymbol = result.TradesPerSymbol,
                    TotalTradedVolume = result.VolumePerSymbol.Values.Sum(),
                    VolumePerSymbol = result.VolumePerSymbol,
                    ProcessingTimeMs = result.ProcessingTimeMs,
                    Success = result.Success,
                    Errors = result.Errors
                };

                await collection.InsertOneAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving matching results: {ex.Message}");
            }
        }

        #region Interface Implementation

        /// <summary>
        /// Get the current status of the matching engine
        /// </summary>
        public async Task<MatchingStatusResponse> GetStatusAsync()
        {
            // Get count of open orders by symbol
            var ordersPerSymbol = new Dictionary<string, int>();
            foreach (var symbol in _settings.EnabledSymbols)
            {
                var orders = await GetOpenOrdersAsync(symbol);
                ordersPerSymbol[symbol] = orders.Count;
            }

            return new MatchingStatusResponse
            {
                IsActive = _settings.IsMatchingEnabled,
                Status = _isMatchingRunning ? "RUNNING" : "IDLE",
                ProcessedBatchesCount = _processedBatchesCount,
                LastRunTimestamp = new DateTimeOffset(_lastRunTime).ToUnixTimeMilliseconds(),
                NextScheduledRunTimestamp = new DateTimeOffset(_nextScheduledRun).ToUnixTimeMilliseconds(),
                EnabledSymbols = _settings.EnabledSymbols,
                QueuedOrdersCount = ordersPerSymbol.Values.Sum(),
                OrdersPerSymbol = ordersPerSymbol
            };
        }

        /// <summary>
        /// Manually trigger a matching cycle
        /// </summary>
        public async Task<MatchingResultResponse> TriggerMatchingAsync(TriggerMatchingRequest request)
        {
            var batchId = ObjectId.GenerateNewId().ToString();
            var result = new MatchingResultResponse
            {
                BatchId = batchId,
                Success = true,
                ProcessedOrdersCount = 0,
                MatchedOrdersCount = 0,
                CreatedTradesCount = 0,
                TradesPerSymbol = new Dictionary<string, int>(),
                VolumePerSymbol = new Dictionary<string, decimal>(),
                ProcessingTimeMs = 0,
                Errors = Array.Empty<string>()
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Force run even if matching is disabled
                if (!_settings.IsMatchingEnabled && !request.ForceRun)
                {
                    result.Success = false;
                    result.Errors = new[] { "Matching is disabled. Use ForceRun=true to override." };
                    return result;
                }

                // If we're already running a matching cycle
                if (_isMatchingRunning && !request.ForceRun)
                {
                    result.Success = false;
                    result.Errors = new[] { "Matching cycle already in progress. Use ForceRun=true to override." };
                    return result;
                }

                // Set flag to indicate we're running
                _isMatchingRunning = true;
                _lastRunTime = DateTime.UtcNow;

                // Process specific symbols or all enabled symbols
                var symbolsToProcess = request.Symbols?.Length > 0
                    ? request.Symbols.Intersect(_settings.EnabledSymbols).ToArray()
                    : _settings.EnabledSymbols;

                // If no valid symbols to process
                if (symbolsToProcess.Length == 0)
                {
                    result.Success = false;
                    result.Errors = new[] { "No valid symbols to process." };
                    return result;
                }

                // Process each symbol
                foreach (var symbol in symbolsToProcess)
                {
                    await ProcessSymbolOrdersAsync(symbol, batchId);
                }

                _processedBatchesCount++;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error triggering matching cycle: {ex.Message}");
                result.Success = false;
                result.Errors = new[] { ex.Message };
                return result;
            }
            finally
            {
                _isMatchingRunning = false;
                result.ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                await SaveMatchingResultsAsync(result);
            }
        }

        /// <summary>
        /// Get matching history with pagination
        /// </summary>
        public async Task<MatchHistoryResponse> GetHistoryAsync(MatchHistoryRequest request)
        {
            try
            {
                var collection = _dbFactory.GetCollection<MatchingResultDocument>("matchmaking", "matchResults");
                var builder = Builders<MatchingResultDocument>.Filter;
                var filter = builder.Empty;

                // Apply time range filters if provided
                if (request.StartTime.HasValue)
                {
                    var startDate = DateTimeOffset.FromUnixTimeMilliseconds(request.StartTime.Value).UtcDateTime;
                    filter = filter & builder.Gte(x => x.Timestamp, startDate);
                }

                if (request.EndTime.HasValue)
                {
                    var endDate = DateTimeOffset.FromUnixTimeMilliseconds(request.EndTime.Value).UtcDateTime;
                    filter = filter & builder.Lte(x => x.Timestamp, endDate);
                }

                // Apply symbol filter if provided
                if (!string.IsNullOrEmpty(request.Symbol))
                {
                    filter = filter & builder.ElemMatch(x => x.TradesPerSymbol,
                        Builders<KeyValuePair<string, int>>.Filter.Eq(p => p.Key, request.Symbol));
                }

                // Get total count for pagination
                var totalCount = await collection.CountDocumentsAsync(filter);

                // Apply pagination
                var skip = (request.Page - 1) * request.PageSize;
                var results = await collection
                    .Find(filter)
                    .Sort(Builders<MatchingResultDocument>.Sort.Descending(x => x.Timestamp))
                    .Skip(skip)
                    .Limit(request.PageSize)
                    .ToListAsync();

                // Map to response objects
                var items = results.Select(r => new MatchBatchInfo
                {
                    BatchId = r.BatchId,
                    Timestamp = new DateTimeOffset(r.Timestamp).ToUnixTimeMilliseconds(),
                    ProcessedOrdersCount = r.ProcessedOrdersCount,
                    MatchedOrdersCount = r.MatchedOrdersCount,
                    CreatedTradesCount = r.CreatedTradesCount,
                    TradesPerSymbol = r.TradesPerSymbol,
                    TotalTradedVolume = r.TotalTradedVolume,
                    ProcessingTimeMs = r.ProcessingTimeMs
                }).ToList();

                return new MatchHistoryResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = (int)totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching history: {ex.Message}");
                return new MatchHistoryResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                    Items = new List<MatchBatchInfo>()
                };
            }
        }

        /// <summary>
        /// Get current matching engine settings
        /// </summary>
        public Task<MatchingSettingsResponse> GetSettingsAsync()
        {
            return Task.FromResult(_settings);
        }

        /// <summary>
        /// Update matching engine settings
        /// </summary>
        public Task<MatchingSettingsResponse> UpdateSettingsAsync(UpdateMatchingSettingsRequest request)
        {
            // Update settings if values are provided
            if (request.MatchingIntervalSeconds.HasValue)
            {
                _settings.MatchingIntervalSeconds = request.MatchingIntervalSeconds.Value;
            }

            if (request.OrderLockTimeoutSeconds.HasValue)
            {
                _settings.OrderLockTimeoutSeconds = request.OrderLockTimeoutSeconds.Value;
            }

            if (request.EnabledSymbols != null)
            {
                _settings.EnabledSymbols = request.EnabledSymbols;
            }

            if (request.MaxOrdersPerBatch.HasValue)
            {
                _settings.MaxOrdersPerBatch = request.MaxOrdersPerBatch.Value;
            }

            if (request.MaxTradesPerBatch.HasValue)
            {
                _settings.MaxTradesPerBatch = request.MaxTradesPerBatch.Value;
            }

            if (request.IsMatchingEnabled.HasValue)
            {
                _settings.IsMatchingEnabled = request.IsMatchingEnabled.Value;
            }

            return Task.FromResult(_settings);
        }

        /// <summary>
        /// Get matching statistics
        /// </summary>
        public async Task<MatchingStatsResponse> GetStatsAsync()
        {
            try
            {
                var collection = _dbFactory.GetCollection<MatchingResultDocument>("matchmaking", "matchResults");

                // Get total batches processed
                var totalBatches = await collection.CountDocumentsAsync(FilterDefinition<MatchingResultDocument>.Empty);

                // Get total trades created
                var pipeline = new BsonDocument[]
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", null },
                        { "totalTrades", new BsonDocument("$sum", "$CreatedTradesCount") },
                        { "totalVolume", new BsonDocument("$sum", "$TotalTradedVolume") },
                        { "avgTime", new BsonDocument("$avg", "$ProcessingTimeMs") }
                    })
                };

                var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                decimal totalTrades = 0;
                decimal totalVolume = 0;
                decimal avgTime = 0;

                if (results.Count > 0)
                {
                    var result = results[0];
                    totalTrades = result["totalTrades"].AsInt32;
                    totalVolume = (decimal)result["totalVolume"].AsDecimal128;
                    avgTime = (decimal)result["avgTime"].AsDecimal128;
                }

                // Get last day statistics
                var oneDayAgo = DateTime.UtcNow.AddDays(-1);
                var lastDayFilter = Builders<MatchingResultDocument>.Filter.Gte(x => x.Timestamp, oneDayAgo);

                var lastDayPipeline = new BsonDocument[]
                {
                    new BsonDocument("$match", new BsonDocument("Timestamp", new BsonDocument("$gte", oneDayAgo))),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", null },
                        { "trades", new BsonDocument("$sum", "$CreatedTradesCount") },
                        { "volume", new BsonDocument("$sum", "$TotalTradedVolume") }
                    })
                };

                var lastDayResults = await collection.Aggregate<BsonDocument>(lastDayPipeline).ToListAsync();

                long lastDayTrades = 0;
                decimal lastDayVolume = 0;

                if (lastDayResults.Count > 0)
                {
                    var lastDayResult = lastDayResults[0];
                    lastDayTrades = lastDayResult["trades"].AsInt64;
                    lastDayVolume = (decimal)lastDayResult["volume"].AsDecimal128;
                }

                // Get volume per symbol statistics
                var volumePipeline = new BsonDocument[]
                {
                    new BsonDocument("$unwind", new BsonDocument("path", "$VolumePerSymbol")),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$VolumePerSymbol.Key" },
                        { "volume", new BsonDocument("$sum", "$VolumePerSymbol.Value") }
                    })
                };

                var volumeResults = await collection.Aggregate<BsonDocument>(volumePipeline).ToListAsync();
                var volumePerSymbol = new Dictionary<string, decimal>();

                foreach (var doc in volumeResults)
                {
                    var symbol = doc["_id"].AsString;
                    var volume = (decimal)doc["volume"].AsDecimal128;
                    volumePerSymbol[symbol] = volume;
                }

                // Build the response
                return new MatchingStatsResponse
                {
                    TotalBatchesProcessed = (int)totalBatches,
                    TotalTradesCreated = (int)totalTrades,
                    TotalVolumeTraded = totalVolume,
                    VolumePerSymbol = volumePerSymbol,
                    AverageMatchingTimeMs = avgTime,
                    LastDayTradesCount = lastDayTrades,
                    LastDayVolume = lastDayVolume,
                    SymbolStatistics = new Dictionary<string, SymbolStats>() // Would need more complex aggregation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving matching stats: {ex.Message}");
                return new MatchingStatsResponse
                {
                    TotalBatchesProcessed = 0,
                    TotalTradesCreated = 0,
                    TotalVolumeTraded = 0,
                    VolumePerSymbol = new Dictionary<string, decimal>(),
                    AverageMatchingTimeMs = 0,
                    LastDayTradesCount = 0,
                    LastDayVolume = 0,
                    SymbolStatistics = new Dictionary<string, SymbolStats>()
                };
            }
        }

        /// <summary>
        /// Test the matching engine with simulated orders
        /// </summary>
        public Task<TestMatchingResponse> TestMatchingAsync(TestMatchingRequest request)
        {
            // Implementation for the test matching algorithm
            // This would simulate the matching without actually executing trades

            var response = new TestMatchingResponse
            {
                Success = true,
                Matches = new List<TestMatch>(),
                UnmatchedOrders = new List<TestOrder>(),
                ProcessingTimeMs = 0
            };

            // Implementation details would be similar to the real matching algorithm
            // but without saving to database or calling other services

            return Task.FromResult(response);
        }

        #endregion
    }

    /// <summary>
    /// Document model for storing matching results in MongoDB
    /// </summary>
    public class MatchingResultDocument
    {
        public ObjectId Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int ProcessedOrdersCount { get; set; }
        public int MatchedOrdersCount { get; set; }
        public int CreatedTradesCount { get; set; }
        public Dictionary<string, int> TradesPerSymbol { get; set; } = new Dictionary<string, int>();
        public decimal TotalTradedVolume { get; set; }
        public Dictionary<string, decimal> VolumePerSymbol { get; set; } = new Dictionary<string, decimal>();
        public long ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}