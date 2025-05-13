using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Risk;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RiskService.Services
{
    /// <summary>
    /// Implementation of risk management services
    /// </summary>
    public class RiskService : IRiskService
    {
        private readonly MongoDbConnectionFactory _dbFactory;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for RiskService
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public RiskService(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("RiskService initialized");
        }

        /// <inheritdoc />
        public async Task<RiskProfile> GetRiskProfileAsync(ObjectId userId)
        {
            try
            {
                var riskProfileCollection = _dbFactory.GetCollection<RiskProfile>();
                var filter = Builders<RiskProfile>.Filter.Eq(r => r.UserId, userId);
                return await riskProfileCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting risk profile: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<TradingLimits> GetTradingLimitsAsync(ObjectId userId)
        {
            try
            {
                var riskProfileCollection = _dbFactory.GetCollection<RiskProfile>();
                var filter = Builders<RiskProfile>.Filter.Eq(r => r.UserId, userId);
                var profile = await riskProfileCollection.Find(filter).FirstOrDefaultAsync();
                return profile?.Limits;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trading limits: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<RiskAlert>> GetActiveAlertsAsync(ObjectId userId)
        {
            try
            {
                var riskAlertCollection = _dbFactory.GetCollection<RiskAlert>();
                var filter = Builders<RiskAlert>.Filter.And(
                    Builders<RiskAlert>.Filter.Eq(a => a.UserId, userId),
                    Builders<RiskAlert>.Filter.Eq(a => a.IsAcknowledged, false)
                );

                return await riskAlertCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active alerts: {ex.Message}");
                return new List<RiskAlert>();
            }
        }

        /// <inheritdoc />
        public async Task<RiskAlert?> AcknowledgeAlertAsync(ObjectId alertId, ObjectId userId, string? comment = null)
        {
            try
            {
                var riskAlertCollection = _dbFactory.GetCollection<RiskAlert>();
                // Find the alert for this user that's not acknowledged
                var filter = Builders<RiskAlert>.Filter.And(
                    Builders<RiskAlert>.Filter.Eq(a => a.Id, alertId),
                    Builders<RiskAlert>.Filter.Eq(a => a.UserId, userId),
                    Builders<RiskAlert>.Filter.Eq(a => a.IsAcknowledged, false)
                );

                var update = Builders<RiskAlert>.Update
                    .Set(a => a.IsAcknowledged, true)
                    .Set(a => a.AcknowledgedAt, DateTime.UtcNow)
                    .Set(a => a.AcknowledgmentComment, comment);

                var options = new FindOneAndUpdateOptions<RiskAlert>
                {
                    ReturnDocument = ReturnDocument.After
                };

                return await riskAlertCollection.FindOneAndUpdateAsync(filter, update, options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error acknowledging alert: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<RiskRule>> GetActiveRulesAsync()
        {
            try
            {
                var riskRuleCollection = _dbFactory.GetCollection<RiskRule>();
                var filter = Builders<RiskRule>.Filter.Eq(r => r.IsActive, true);
                return await riskRuleCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active rules: {ex.Message}");
                return new List<RiskRule>();
            }
        }
    }
}