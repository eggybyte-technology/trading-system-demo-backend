using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Models.Market;
using MongoDB.Bson;

namespace MarketDataService.Repositories
{
    /// <summary>
    /// Repository interface for Symbol entity
    /// </summary>
    public interface ISymbolRepository
    {
        /// <summary>
        /// Get all symbols
        /// </summary>
        /// <returns>List of all symbols</returns>
        Task<List<Symbol>> GetAllSymbolsAsync();

        /// <summary>
        /// Get all active symbols where IsActive is true
        /// </summary>
        /// <returns>List of active symbols</returns>
        Task<List<Symbol>> GetActiveSymbolsAsync();

        /// <summary>
        /// Get symbol by name
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Symbol if found, null otherwise</returns>
        Task<Symbol> GetSymbolByNameAsync(string symbolName);

        /// <summary>
        /// Get symbol by ID
        /// </summary>
        /// <param name="id">Symbol ID</param>
        /// <returns>Symbol if found, null otherwise</returns>
        Task<Symbol> GetSymbolByIdAsync(ObjectId id);

        /// <summary>
        /// Create a new symbol
        /// </summary>
        /// <param name="symbol">Symbol to create</param>
        /// <returns>Created symbol</returns>
        Task<Symbol> CreateSymbolAsync(Symbol symbol);

        /// <summary>
        /// Update an existing symbol
        /// </summary>
        /// <param name="symbol">Symbol to update</param>
        /// <returns>Updated symbol</returns>
        Task<Symbol> UpdateSymbolAsync(Symbol symbol);

        /// <summary>
        /// Delete a symbol
        /// </summary>
        /// <param name="id">Symbol ID to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteSymbolAsync(ObjectId id);
    }
}