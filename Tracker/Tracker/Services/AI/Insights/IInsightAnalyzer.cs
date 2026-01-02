using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.Services.AI.Insights
{
    /// <summary>
    /// Interface for insight analyzers that detect patterns requiring attention.
    /// </summary>
    public interface IInsightAnalyzer
    {
        /// <summary>
        /// Display name of the analyzer for logging/debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The types of insights this analyzer can generate.
        /// </summary>
        IEnumerable<InsightType> SupportedInsightTypes { get; }

        /// <summary>
        /// Whether this analyzer is currently enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Analyzes data and generates insights.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>List of generated insights.</returns>
        Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default);
    }
}
