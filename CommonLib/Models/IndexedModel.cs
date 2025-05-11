using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace CommonLib.Models
{
    /// <summary>
    /// Abstract class for models that require indexes, inheriting from BaseModel
    /// </summary>
    public abstract class IndexedModel<T> : BaseModel
    {
        /// <summary>
        /// Gets the list of indexes that should be created for this model
        /// Each tuple contains the index definition and whether it should be unique
        /// </summary>
        /// <returns>A list of index definitions with their uniqueness flag</returns>
        public abstract List<Tuple<IndexKeysDefinition<T>, bool>> GetIndexes();
    }
}