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
                    throw new InvalidOperationException("Insufficient balance for withdrawal");
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
            catch (Exception ex)
            {
                _logger.LogError($"Error creating withdrawal: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Withdrawal?> GetWithdrawalByIdAsync(string userId, string withdrawalId)
        {
            try
            {
                var userObjectId = ObjectId.Parse(userId);
                var withdrawalObjectId = ObjectId.Parse(withdrawalId);

                var withdrawalCollection = _dbFactory.GetCollection<Withdrawal>();

                var filter = Builders<Withdrawal>.Filter.And(
                    Builders<Withdrawal>.Filter.Eq(w => w.Id, withdrawalObjectId),
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
                // Create new account for user
                account = new Account
                {
                    UserId = userObjectId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "active",
                    Balances = new List<Balance>()
                };

                await accountCollection.InsertOneAsync(account);
                _logger.LogInformation($"Created new account for user {userId}");
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

                // Set creation time if not set
                if (account.CreatedAt == default)
                {
                    account.CreatedAt = DateTime.UtcNow;
                }

                // Set update time
                account.UpdatedAt = DateTime.UtcNow;

                // Insert account
                await accountCollection.InsertOneAsync(account);
                _logger.LogInformation($"Created new account for user ID: {account.UserId}");

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating account: {ex.Message}");
                throw;
            }
        }

        #region Helper Methods

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
                throw new InvalidOperationException("Failed to lock funds for withdrawal");
            }
        }

        #endregion
    }
}