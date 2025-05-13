using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using CommonLib.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// MongoDB implementation of ISymbolRepository
    /// </summary>
    public class SymbolRepository : ISymbolRepository
    {
        private readonly IMongoCollection<Symbol> _symbols;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Constructor for SymbolRepository
        /// </summary>
        /// <param name="dbFactory">MongoDB connection factory</param>
        /// <param name="logger">Logger service</param>
        public SymbolRepository(MongoDbConnectionFactory dbFactory, ILoggerService logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _symbols = dbFactory?.GetCollection<Symbol>() ?? throw new System.ArgumentNullException(nameof(dbFactory));
        }

        /// <inheritdoc />
        public async Task<List<Symbol>> GetAllSymbolsAsync()
        {
            return await _symbols.Find(symbol => true).ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Symbol>> GetActiveSymbolsAsync()
        {
            return await _symbols.Find(symbol => symbol.IsActive).ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Symbol> GetSymbolByNameAsync(string symbolName)
        {
            return await _symbols.Find(symbol => symbol.Name == symbolName).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Symbol> GetSymbolByIdAsync(ObjectId id)
        {
            return await _symbols.Find(symbol => symbol.Id == id).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Symbol> CreateSymbolAsync(Symbol symbol)
        {
            await _symbols.InsertOneAsync(symbol);
            return symbol;
        }

        /// <inheritdoc />
        public async Task<Symbol> UpdateSymbolAsync(Symbol symbol)
        {
            await _symbols.ReplaceOneAsync(
                s => s.Id == symbol.Id,
                symbol);

            return symbol;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteSymbolAsync(ObjectId id)
        {
            var deleteResult = await _symbols.DeleteOneAsync(s => s.Id == id);
            return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
        }
    }
}