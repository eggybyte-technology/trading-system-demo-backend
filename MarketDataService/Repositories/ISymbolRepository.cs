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
        /// Get all trading symbols
        /// </summary>
        /// <returns>List of all symbols</returns>
        Task<List<Symbol>> GetAllSymbolsAsync();

        /// <summary>
        /// Get a symbol by name
        /// </summary>
        /// <param name="symbolName">Symbol name (e.g., BTC-USDT)</param>
        /// <returns>Symbol object if found, null otherwise</returns>
        Task<Symbol> GetSymbolByNameAsync(string symbolName);

        /// <summary>
        /// Get a symbol by ID
        /// </summary>
        /// <param name="id">Symbol ID</param>
        /// <returns>Symbol object if found, null otherwise</returns>
        Task<Symbol> GetSymbolByIdAsync(ObjectId id);

        /// <summary>
        /// Create a new symbol
        /// </summary>
        /// <param name="symbol">Symbol to create</param>
        /// <returns>Created symbol with assigned ID</returns>
        Task<Symbol> CreateSymbolAsync(Symbol symbol);

        /// <summary>
        /// Update a symbol
        /// </summary>
        /// <param name="symbol">Symbol to update</param>
        /// <returns>True if updated successfully, false otherwise</returns>
        Task<bool> UpdateSymbolAsync(Symbol symbol);

        /// <summary>
        /// Delete a symbol
        /// </summary>
        /// <param name="id">Symbol ID to delete</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteSymbolAsync(ObjectId id);
    }
}