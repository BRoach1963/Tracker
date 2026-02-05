using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Interface for insight analyzers.
/// Each analyzer examines specific aspects of user data and generates insights.
/// </summary>
public interface IInsightAnalyzer
{
    /// <summary>
    /// Unique name for this analyzer (used for logging and tracking).
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The types of insights this analyzer can generate.
    /// </summary>
    IReadOnlyList<InsightType> InsightTypes { get; }
    
    /// <summary>
    /// Analyzes user data and generates insights.
    /// </summary>
    /// <param name="userId">The user to analyze.</param>
    /// <param name="organizationId">The organization context.</param>
    /// <returns>List of generated insights (may be empty).</returns>
    Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId);
}
