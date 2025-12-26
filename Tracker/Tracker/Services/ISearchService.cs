using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tracker.Services
{
    /// <summary>
    /// Interface for search service.
    /// Enables unit testing by allowing mock implementations.
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Searches across all entities for the given query.
        /// </summary>
        /// <param name="query">Search query (minimum 2 characters)</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of search results ordered by relevance</returns>
        Task<List<SearchResult>> SearchAsync(string query, int maxResults = 50);
    }
}
