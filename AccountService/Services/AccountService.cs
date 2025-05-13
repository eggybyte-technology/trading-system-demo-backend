using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Models.Account;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccountService.Services
{
    /// <summary>
    /// Service for account management operations
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;
        private readonly Dictionary<string, object> _lockObjects = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the account service
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public AccountService(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Account> GetAccountBalanceAsync(string userId)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var accountCollection = _dbFactory.GetCollection<Account>();

                var account = await accountCollection
                    .Find(a => a.UserId == userObjectId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    _logger.LogWarning($"Account not found for user ID: {userId}");
                    account = await EnsureUserHasAccountAsync(userId);
                }

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting account balance: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(List<Transaction> Transactions, long Total)> GetTransactionHistoryAsync(
            string userId,
            int page,
            int pageSize,
            long? startTime = null,
            long? endTime = null,
            string? type = null)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var transactionCollection = _dbFactory.GetCollection<Transaction>();

                // Build filter
                var builder = Builders<Transaction>.Filter;
                var filter = builder.Eq(t => t.UserId, userObjectId);

                // Apply optional filters
                if (!string.IsNullOrEmpty(type))
                {
                    filter = builder.And(filter, builder.Eq(t => t.Type, type));
                }

                if (startTime.HasValue)
                {
                    var startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime.Value).UtcDateTime;
                    filter = builder.And(filter, builder.Gte(t => t.CreatedAt, startDateTime));
                }

                if (endTime.HasValue)
                {
                    var endDateTime = DateTimeOffset.FromUnixTimeMilliseconds(endTime.Value).UtcDateTime;
                    filter = builder.And(filter, builder.Lte(t => t.CreatedAt, endDateTime));
                }

                // Get total count
                var total = await transactionCollection.CountDocumentsAsync(filter);

                // Get paginated transactions
                var transactions = await transactionCollection
                    .Find(filter)
                    .Sort(Builders<Transaction>.Sort.Descending(t => t.CreatedAt))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return (transactions, total);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting transaction history: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Transaction> CreateDepositAsync(string userId, string asset, decimal amount, string? reference = null)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var account = await EnsureUserHasAccountAsync(userId);

                // Create transaction record
                var transaction = new Transaction
                {
                    AccountId = account.Id,
                    UserId = userObjectId,
                    Asset = asset,
                    Amount = amount,
                    Type = "deposit",
                    Status = "completed", // For demo purposes, deposits are immediately completed
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    Reference = reference ?? Guid.NewGuid().ToString()
                };

                // Save transaction
                var transactionCollection = _dbFactory.GetCollection<Transaction>();
                await transactionCollection.InsertOneAsync(transaction);

                // Update account balance
                await UpdateAccountBalance(account, asset, amount);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating deposit: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Withdrawal> CreateWithdrawalAsync(
            string userId,
            string asset,
            decimal amount,
            string address,
            string? memo = null)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var account = await GetAccountBalanceAsync(userId);

                // Check if user has enough balance
                var balance = account.Balances.FirstOrDefault(b => b.Asset == asset);
                if (balance == null || balance.Free < amount)
                {
                    throw new InsufficientFundsException($"Insufficient balance for withdrawal. Required: {amount}, Available: {balance?.Free ?? 0}");
                }

                // Lock funds for withdrawal (subtract from free balance and add to locked balance)
                await LockFunds(account.Id, asset, amount);

                // Create withdrawal request
                var withdrawal = new Withdrawal
                {
                    UserId = userObjectId,
                    Asset = asset,
                    Amount = amount,
                    Address = address,
                    Memo = memo,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save withdrawal request
                var withdrawalCollection = _dbFactory.GetCollection<Withdrawal>();
                await withdrawalCollection.InsertOneAsync(withdrawal);

                return withdrawal;
            }
            catch (InsufficientFundsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating withdrawal: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Withdrawal?> GetWithdrawalAsync(ObjectId withdrawalId, string userId)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var withdrawalCollection = _dbFactory.GetCollection<Withdrawal>();

                var filter = Builders<Withdrawal>.Filter.And(
                    Builders<Withdrawal>.Filter.Eq(w => w.Id, withdrawalId),
                    Builders<Withdrawal>.Filter.Eq(w => w.UserId, userObjectId)
                );

                return await withdrawalCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting withdrawal: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Withdrawal?> GetWithdrawalByIdAsync(string userId, string withdrawalId)
        {
            try
            {
                if (!ObjectId.TryParse(userId, out var userObjectId) ||
                    !ObjectId.TryParse(withdrawalId, out var withdrawalObjectId))
                {
                    return null;
                }

                return await GetWithdrawalAsync(withdrawalObjectId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting withdrawal by ID: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetAvailableAssetsAsync()
        {
            // In a real implementation, this would fetch from a configuration or database
            // For simplicity, return a predefined list of supported assets
            return await Task.FromResult(new List<string>
            {
                "BTC", "ETH", "USDT", "BNB", "XRP", "ADA", "SOL", "DOT", "USDC"
            });
        }

        /// <inheritdoc/>
        public async Task<Account> EnsureUserHasAccountAsync(string userId)
        {
            var userObjectId = ObjectId.Parse(userId);
            var accountCollection = _dbFactory.GetCollection<Account>();

            var account = await accountCollection
                .Find(a => a.UserId == userObjectId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                // For demo purposes, we'll use a generic username based on the user ID
                string username = $"user_{userId.Substring(0, 8)}";

                // Create new account for user
                account = new Account
                {
                    UserId = userObjectId,
                    Username = username,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "active",
                    Balances = new List<Balance>()
                };

                await accountCollection.InsertOneAsync(account);
                _logger.LogInformation($"Created new account for user {userId} with username {username}");
            }

            return account;
        }

        /// <inheritdoc/>
        public async Task<Account> GetAccountByUserIdAsync(ObjectId userId)
        {
            try
            {
                var accountCollection = _dbFactory.GetCollection<Account>();

                return await accountCollection
                    .Find(a => a.UserId == userId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting account by user ID: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Account> CreateAccountAsync(Account account)
        {
            try
            {
                var accountCollection = _dbFactory.GetCollection<Account>();

                // Check if account exists
                var existingAccount = await GetAccountByUserIdAsync(account.UserId);
                if (existingAccount != null)
                {
                    _logger.LogWarning($"Account already exists for user ID: {account.UserId}");
                    return existingAccount;
                }

                // Set username if not set
                if (string.IsNullOrEmpty(account.Username))
                {
                    account.Username = $"user_{account.UserId.ToString().Substring(0, 8)}";
                }

                // Set creation time if not set
                if (account.CreatedAt == default)
                {
                    account.CreatedAt = DateTime.UtcNow;
                }

                // Set update time
                account.UpdatedAt = DateTime.UtcNow;

                // Insert account
                await accountCollection.InsertOneAsync(account);
                _logger.LogInformation($"Created new account for user ID: {account.UserId} with username {account.Username}");

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LockBalanceResponse> LockBalanceAsync(LockBalanceRequest request)
        {
            try
            {
                // Generate a lock ID if one is not provided
                var lockId = string.IsNullOrEmpty(request.LockId) ? Guid.NewGuid().ToString() : request.LockId;

                if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                {
                    return new LockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid user ID format",
                        LockId = lockId
                    };
                }

                // Get user account
                var account = await GetAccountByUserIdAsync(userObjectId);
                if (account == null)
                {
                    return new LockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Account not found",
                        LockId = lockId
                    };
                }

                // Check if balance exists and has enough free funds
                var balance = account.Balances.FirstOrDefault(b => b.Asset == request.Asset);
                if (balance == null || balance.Free < request.Amount)
                {
                    return new LockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = $"Insufficient {request.Asset} balance. Required: {request.Amount}, Available: {balance?.Free ?? 0}",
                        LockId = lockId
                    };
                }

                // Create a lock object for this lock operation
                string lockKey = $"{request.UserId}_{request.Asset}_{lockId}";

                // First step: Double-check balance availability and prepare data (outside lock)
                var currentBalance = account.Balances.FirstOrDefault(b => b.Asset == request.Asset);
                if (currentBalance == null || currentBalance.Free < request.Amount)
                {
                    return new LockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = $"Insufficient {request.Asset} balance (concurrent operation). Required: {request.Amount}, Available: {currentBalance?.Free ?? 0}",
                        LockId = lockId
                    };
                }

                // Update balance in the database using a filter to ensure atomic operation
                var accountCollection = _dbFactory.GetCollection<Account>();
                var filter = Builders<Account>.Filter.And(
                    Builders<Account>.Filter.Eq(a => a.Id, account.Id),
                    Builders<Account>.Filter.ElemMatch(a => a.Balances, b => b.Asset == request.Asset && b.Free >= request.Amount)
                );

                var update = Builders<Account>.Update
                    .Inc("Balances.$.Free", -request.Amount)
                    .Inc("Balances.$.Locked", request.Amount)
                    .Set("Balances.$.UpdatedAt", DateTime.UtcNow);

                var result = await accountCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    return new LockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to update balance - concurrent modification or insufficient funds",
                        LockId = lockId
                    };
                }

                // Store lock information
                var lockCollection = _dbFactory.GetCollection<BalanceLock>("account", "balanceLocks");
                var balanceLock = new BalanceLock
                {
                    UserId = userObjectId,
                    Asset = request.Asset,
                    Amount = request.Amount,
                    LockId = lockId,
                    OrderId = request.OrderId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(request.TimeoutSeconds)
                };

                await lockCollection.InsertOneAsync(balanceLock);

                return new LockBalanceResponse
                {
                    Success = true,
                    LockId = lockId,
                    ExpirationTimestamp = new DateTimeOffset(balanceLock.ExpiresAt).ToUnixTimeMilliseconds()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error locking balance: {ex.Message}");
                return new LockBalanceResponse
                {
                    Success = false,
                    ErrorMessage = $"Error locking balance: {ex.Message}",
                    LockId = string.IsNullOrEmpty(request.LockId) ? Guid.NewGuid().ToString() : request.LockId
                };
            }
        }

        /// <inheritdoc/>
        public async Task<UnlockBalanceResponse> UnlockBalanceAsync(UnlockBalanceRequest request)
        {
            try
            {
                if (!ObjectId.TryParse(request.UserId, out var userObjectId))
                {
                    return new UnlockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid user ID format"
                    };
                }

                // Get lock information
                var lockCollection = _dbFactory.GetCollection<BalanceLock>("account", "balanceLocks");
                var lockFilter = Builders<BalanceLock>.Filter.And(
                    Builders<BalanceLock>.Filter.Eq(l => l.UserId, userObjectId),
                    Builders<BalanceLock>.Filter.Eq(l => l.Asset, request.Asset),
                    Builders<BalanceLock>.Filter.Eq(l => l.LockId, request.LockId)
                );

                var balanceLock = await lockCollection.Find(lockFilter).FirstOrDefaultAsync();
                if (balanceLock == null)
                {
                    return new UnlockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Lock not found or already released"
                    };
                }

                // Get user account (outside of lock)
                var account = await GetAccountByUserIdAsync(userObjectId);
                if (account == null)
                {
                    return new UnlockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Account not found"
                    };
                }

                // Update balance in the database using atomic operations
                var accountCollection = _dbFactory.GetCollection<Account>();
                var filter = Builders<Account>.Filter.And(
                    Builders<Account>.Filter.Eq(a => a.Id, account.Id),
                    Builders<Account>.Filter.ElemMatch(a => a.Balances, b => b.Asset == request.Asset && b.Locked >= balanceLock.Amount)
                );

                var update = Builders<Account>.Update
                    .Inc("Balances.$.Free", balanceLock.Amount)
                    .Inc("Balances.$.Locked", -balanceLock.Amount)
                    .Set("Balances.$.UpdatedAt", DateTime.UtcNow);

                var result = await accountCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    return new UnlockBalanceResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to update balance - concurrent modification or insufficient locked funds"
                    };
                }

                // Remove the lock
                await lockCollection.DeleteOneAsync(lockFilter);

                return new UnlockBalanceResponse
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unlocking balance: {ex.Message}");
                return new UnlockBalanceResponse
                {
                    Success = false,
                    ErrorMessage = $"Error unlocking balance: {ex.Message}"
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ExecuteTradeResponse> ExecuteTradeAsync(ExecuteTradeRequest request)
        {
            try
            {
                // Validate and parse user IDs
                if (!ObjectId.TryParse(request.BuyerUserId, out var buyerObjectId) ||
                    !ObjectId.TryParse(request.SellerUserId, out var sellerObjectId))
                {
                    return new ExecuteTradeResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid user ID format"
                    };
                }

                // Get buyer and seller account
                var buyerAccount = await GetAccountByUserIdAsync(buyerObjectId);
                var sellerAccount = await GetAccountByUserIdAsync(sellerObjectId);

                if (buyerAccount == null || sellerAccount == null)
                {
                    return new ExecuteTradeResponse
                    {
                        Success = false,
                        ErrorMessage = "One or both accounts not found"
                    };
                }

                // Get locks
                var lockCollection = _dbFactory.GetCollection<BalanceLock>("account", "balanceLocks");

                var buyerLockFilter = Builders<BalanceLock>.Filter.And(
                    Builders<BalanceLock>.Filter.Eq(l => l.UserId, buyerObjectId),
                    Builders<BalanceLock>.Filter.Eq(l => l.Asset, request.QuoteAsset)
                );

                var sellerLockFilter = Builders<BalanceLock>.Filter.And(
                    Builders<BalanceLock>.Filter.Eq(l => l.UserId, sellerObjectId),
                    Builders<BalanceLock>.Filter.Eq(l => l.Asset, request.BaseAsset)
                );

                var buyerLock = await lockCollection.Find(buyerLockFilter).FirstOrDefaultAsync();
                var sellerLock = await lockCollection.Find(sellerLockFilter).FirstOrDefaultAsync();

                if (buyerLock == null || sellerLock == null)
                {
                    return new ExecuteTradeResponse
                    {
                        Success = false,
                        ErrorMessage = "Required balance locks not found or expired"
                    };
                }

                // Calculate trade amounts
                decimal quoteAmount = request.Quantity * request.Price;

                // Execute the trade atomically
                using (var session = await _dbFactory.Client.StartSessionAsync())
                {
                    session.StartTransaction();

                    try
                    {
                        var accountCollection = _dbFactory.GetCollection<Account>();

                        // 1. Update buyer's account - remove locked quote asset amount, add base asset
                        var buyerFilter = Builders<Account>.Filter.Eq(a => a.Id, buyerAccount.Id);

                        // First, check if the buyer already has a balance for the base asset
                        var buyerBaseAssetBalance = buyerAccount.Balances.FirstOrDefault(b => b.Asset == request.BaseAsset);
                        var buyerUpdate = buyerBaseAssetBalance == null
                            ? Builders<Account>.Update
                                .Push(a => a.Balances, new Balance
                                {
                                    Asset = request.BaseAsset,
                                    Free = request.Quantity,
                                    Locked = 0,
                                    UpdatedAt = DateTime.UtcNow
                                })
                                .Set(a => a.UpdatedAt, DateTime.UtcNow)
                            : Builders<Account>.Update
                                .Inc($"Balances.{buyerAccount.Balances.IndexOf(buyerBaseAssetBalance)}.Free", request.Quantity)
                                .Set($"Balances.{buyerAccount.Balances.IndexOf(buyerBaseAssetBalance)}.UpdatedAt", DateTime.UtcNow)
                                .Set(a => a.UpdatedAt, DateTime.UtcNow);

                        await accountCollection.UpdateOneAsync(session, buyerFilter, buyerUpdate);

                        // 2. Update buyer's quote asset locked balance (decrease it)
                        var buyerQuoteAssetBalance = buyerAccount.Balances.FirstOrDefault(b => b.Asset == request.QuoteAsset);
                        if (buyerQuoteAssetBalance != null && buyerQuoteAssetBalance.Locked >= quoteAmount)
                        {
                            var buyerQuoteUpdate = Builders<Account>.Update
                                .Inc($"Balances.{buyerAccount.Balances.IndexOf(buyerQuoteAssetBalance)}.Locked", -quoteAmount)
                                .Set($"Balances.{buyerAccount.Balances.IndexOf(buyerQuoteAssetBalance)}.UpdatedAt", DateTime.UtcNow)
                                .Set(a => a.UpdatedAt, DateTime.UtcNow);

                            await accountCollection.UpdateOneAsync(session, buyerFilter, buyerQuoteUpdate);
                        }
                        else
                        {
                            throw new Exception("Buyer's locked balance insufficient for trade");
                        }

                        // 3. Update seller's account - remove locked base asset amount, add quote asset
                        var sellerFilter = Builders<Account>.Filter.Eq(a => a.Id, sellerAccount.Id);

                        // Check if seller already has a balance for the quote asset
                        var sellerQuoteAssetBalance = sellerAccount.Balances.FirstOrDefault(b => b.Asset == request.QuoteAsset);
                        var sellerUpdate = sellerQuoteAssetBalance == null
                            ? Builders<Account>.Update
                                .Push(a => a.Balances, new Balance
                                {
                                    Asset = request.QuoteAsset,
                                    Free = quoteAmount,
                                    Locked = 0,
                                    UpdatedAt = DateTime.UtcNow
                                })
                                .Set(a => a.UpdatedAt, DateTime.UtcNow)
                            : Builders<Account>.Update
                                .Inc($"Balances.{sellerAccount.Balances.IndexOf(sellerQuoteAssetBalance)}.Free", quoteAmount)
                                .Set($"Balances.{sellerAccount.Balances.IndexOf(sellerQuoteAssetBalance)}.UpdatedAt", DateTime.UtcNow)
                                .Set(a => a.UpdatedAt, DateTime.UtcNow);

                        await accountCollection.UpdateOneAsync(session, sellerFilter, sellerUpdate);

                        // 4. Update seller's base asset locked balance (decrease it)
                        var sellerBaseAssetBalance = sellerAccount.Balances.FirstOrDefault(b => b.Asset == request.BaseAsset);
                        if (sellerBaseAssetBalance != null && sellerBaseAssetBalance.Locked >= request.Quantity)
                        {
                            var sellerBaseUpdate = Builders<Account>.Update
                                .Inc($"Balances.{sellerAccount.Balances.IndexOf(sellerBaseAssetBalance)}.Locked", -request.Quantity)
                                .Set($"Balances.{sellerAccount.Balances.IndexOf(sellerBaseAssetBalance)}.UpdatedAt", DateTime.UtcNow)
                                .Set(a => a.UpdatedAt, DateTime.UtcNow);

                            await accountCollection.UpdateOneAsync(session, sellerFilter, sellerBaseUpdate);
                        }
                        else
                        {
                            throw new Exception("Seller's locked balance insufficient for trade");
                        }

                        // 5. Create transaction records for both parties
                        var transactionCollection = _dbFactory.GetCollection<Transaction>();

                        // Buyer's transaction (paid quote asset, received base asset)
                        var buyerTransaction = new Transaction
                        {
                            AccountId = buyerAccount.Id,
                            UserId = buyerObjectId,
                            Asset = request.BaseAsset,
                            Amount = request.Quantity,
                            Type = "trade_buy",
                            Status = "completed",
                            Reference = $"{request.MatchId}:{request.BuyOrderId}",
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        };

                        // Seller's transaction (sold base asset, received quote asset)
                        var sellerTransaction = new Transaction
                        {
                            AccountId = sellerAccount.Id,
                            UserId = sellerObjectId,
                            Asset = request.QuoteAsset,
                            Amount = quoteAmount,
                            Type = "trade_sell",
                            Status = "completed",
                            Reference = $"{request.MatchId}:{request.SellOrderId}",
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        };

                        await transactionCollection.InsertManyAsync(session, new[] { buyerTransaction, sellerTransaction });

                        // 6. Delete locks
                        await lockCollection.DeleteOneAsync(session, buyerLockFilter);
                        await lockCollection.DeleteOneAsync(session, sellerLockFilter);

                        // Commit the transaction
                        await session.CommitTransactionAsync();

                        return new ExecuteTradeResponse
                        {
                            Success = true,
                            TradeId = ObjectId.GenerateNewId().ToString()
                        };
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        _logger.LogError($"Error executing trade transaction: {ex.Message}");
                        return new ExecuteTradeResponse
                        {
                            Success = false,
                            ErrorMessage = $"Trade execution failed: {ex.Message}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing trade: {ex.Message}");
                return new ExecuteTradeResponse
                {
                    Success = false,
                    ErrorMessage = $"Error executing trade: {ex.Message}"
                };
            }
        }

        #region Helper Methods

        private object GetLockObject(string key)
        {
            lock (_lockObjects)
            {
                if (!_lockObjects.TryGetValue(key, out var lockObject))
                {
                    lockObject = new object();
                    _lockObjects[key] = lockObject;
                }
                return lockObject;
            }
        }

        /// <summary>
        /// Updates an account balance for a specific asset
        /// </summary>
        private async Task UpdateAccountBalance(Account account, string asset, decimal amount)
        {
            var accountCollection = _dbFactory.GetCollection<Account>();

            var balance = account.Balances.FirstOrDefault(b => b.Asset == asset);
            if (balance == null)
            {
                // Add new balance entry
                balance = new Balance
                {
                    Asset = asset,
                    Free = amount,
                    Locked = 0,
                    UpdatedAt = DateTime.UtcNow
                };

                account.Balances.Add(balance);

                var update = Builders<Account>.Update.Set(a => a.Balances, account.Balances);
                await accountCollection.UpdateOneAsync(a => a.Id == account.Id, update);
            }
            else
            {
                // Update existing balance
                var filter = Builders<Account>.Filter.And(
                    Builders<Account>.Filter.Eq(a => a.Id, account.Id),
                    Builders<Account>.Filter.ElemMatch(a => a.Balances, b => b.Asset == asset)
                );

                var update = Builders<Account>.Update
                    .Inc("Balances.$.Free", amount)
                    .Set("Balances.$.UpdatedAt", DateTime.UtcNow);

                await accountCollection.UpdateOneAsync(filter, update);
            }
        }

        /// <summary>
        /// Locks funds for a withdrawal by moving amount from free to locked balance
        /// </summary>
        private async Task LockFunds(ObjectId accountId, string asset, decimal amount)
        {
            var accountCollection = _dbFactory.GetCollection<Account>();

            var filter = Builders<Account>.Filter.And(
                Builders<Account>.Filter.Eq(a => a.Id, accountId),
                Builders<Account>.Filter.ElemMatch(a => a.Balances, b => b.Asset == asset && b.Free >= amount)
            );

            var update = Builders<Account>.Update
                .Inc("Balances.$.Free", -amount)
                .Inc("Balances.$.Locked", amount)
                .Set("Balances.$.UpdatedAt", DateTime.UtcNow);

            var result = await accountCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                throw new InsufficientFundsException($"Failed to lock funds of {amount} {asset} for withdrawal - insufficient funds");
            }
        }

        #endregion
    }

    /// <summary>
    /// Balance lock document for tracking locked balances
    /// </summary>
    public class BalanceLock
    {
        public ObjectId Id { get; set; }
        public ObjectId UserId { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
        public string LockId { get; set; }
        public string? OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}