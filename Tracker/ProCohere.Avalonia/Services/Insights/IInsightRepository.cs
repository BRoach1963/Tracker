using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Repository interface for insight data operations.
/// Provides abstraction over Supabase for testability.
/// </summary>
public interface IInsightRepository
{
    /// <summary>
    /// Gets all active insights for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to get insights for.</param>
    /// <returns>List of active insights, ordered by created date descending.</returns>
    Task<List<Insight>> GetActiveInsightsAsync(Guid userId);
    
    /// <summary>
    /// Gets a specific insight by ID.
    /// </summary>
    /// <param name="id">The insight ID.</param>
    /// <returns>The insight if found, null otherwise.</returns>
    Task<Insight?> GetInsightByIdAsync(Guid id);
    
    /// <summary>
    /// Creates a new insight.
    /// </summary>
    /// <param name="insight">The insight to create.</param>
    /// <returns>The ID of the created insight.</returns>
    Task<Guid> CreateInsightAsync(Insight insight);
    
    /// <summary>
    /// Updates an existing insight.
    /// </summary>
    /// <param name="insight">The insight to update.</param>
    Task UpdateInsightAsync(Insight insight);
    
    /// <summary>
    /// Dismisses an insight (sets status to dismissed).
    /// </summary>
    /// <param name="id">The insight ID.</param>
    /// <param name="userId">The user dismissing the insight.</param>
    Task DismissInsightAsync(Guid id, Guid userId);
    
    /// <summary>
    /// Marks an insight as acted upon.
    /// </summary>
    /// <param name="id">The insight ID.</param>
    Task MarkInsightActionedAsync(Guid id);
    
    /// <summary>
    /// Snoozes an insight until a specific date/time.
    /// </summary>
    /// <param name="id">The insight ID.</param>
    /// <param name="until">When to show the insight again.</param>
    Task SnoozeInsightAsync(Guid id, DateTime until);
    
    /// <summary>
    /// Gets the count of active insights for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Number of active insights.</returns>
    Task<int> GetActiveCountAsync(Guid userId);
    
    /// <summary>
    /// Soft-deletes an insight.
    /// </summary>
    /// <param name="id">The insight ID.</param>
    Task DeleteInsightAsync(Guid id);
    
    /// <summary>
    /// Gets insights by type for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="type">The insight type.</param>
    /// <returns>List of insights matching the type.</returns>
    Task<List<Insight>> GetInsightsByTypeAsync(Guid userId, InsightType type);
    
    /// <summary>
    /// Checks if an insight already exists (prevents duplicates).
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="type">The insight type.</param>
    /// <param name="entityId">The entity ID (optional).</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> InsightExistsAsync(Guid organizationId, Guid userId, InsightType type, Guid? entityId);
}
