using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Repository interface for insight read operations.
/// Write operations go through IInsightRpcService.
/// </summary>
public interface IInsightRepository
{
    /// <summary>
    /// Gets all active insights for a specific user.
    /// Does not filter by user actions - caller should apply action filtering.
    /// </summary>
    Task<List<Insight>> GetActiveInsightsAsync(Guid teamMemberId);
    
    /// <summary>
    /// Gets a specific insight by ID.
    /// </summary>
    Task<Insight?> GetInsightByIdAsync(Guid id);
    
    /// <summary>
    /// Gets an insight by its signature hash.
    /// </summary>
    Task<Insight?> GetInsightBySignatureAsync(Guid organizationId, string signatureHash);
    
    /// <summary>
    /// Gets insights by type for a specific user.
    /// </summary>
    Task<List<Insight>> GetInsightsByTypeAsync(Guid teamMemberId, InsightType type);
    
    /// <summary>
    /// Checks if an insight with the given signature exists.
    /// </summary>
    Task<bool> SignatureExistsAsync(Guid organizationId, Guid teamMemberId, string signatureHash);
    
    /// <summary>
    /// Gets the count of active insights for a user.
    /// </summary>
    Task<int> GetActiveCountAsync(Guid teamMemberId);
    
    /// <summary>
    /// Gets top N insights by severity for a user.
    /// Used for startup popup.
    /// </summary>
    Task<List<Insight>> GetTopInsightsAsync(Guid teamMemberId, int count, int minSeverity = 4);
    
    #region Legacy Methods (deprecated - use IInsightRpcService for writes)
    
    /// <summary>
    /// Creates a new insight. DEPRECATED - use IInsightRpcService.CreateInsightAsync.
    /// </summary>
    [Obsolete("Use IInsightRpcService.CreateInsightAsync instead")]
    Task<Guid> CreateInsightAsync(Insight insight);
    
    /// <summary>
    /// Updates an existing insight.
    /// </summary>
    Task UpdateInsightAsync(Insight insight);
    
    /// <summary>
    /// Dismisses an insight. DEPRECATED - use IInsightActionRepository.DismissAsync.
    /// </summary>
    [Obsolete("Use IInsightActionRepository.DismissAsync instead")]
    Task DismissInsightAsync(Guid id, Guid userId);
    
    /// <summary>
    /// Marks an insight as acted upon. DEPRECATED - use IInsightActionRepository.
    /// </summary>
    [Obsolete("Use IInsightActionRepository.MarkActedAsync instead")]
    Task MarkInsightActionedAsync(Guid id);
    
    /// <summary>
    /// Snoozes an insight. DEPRECATED - use IInsightActionRepository.SnoozeAsync.
    /// </summary>
    [Obsolete("Use IInsightActionRepository.SnoozeAsync instead")]
    Task SnoozeInsightAsync(Guid id, DateTime until);
    
    /// <summary>
    /// Soft-deletes an insight.
    /// </summary>
    Task DeleteInsightAsync(Guid id);
    
    /// <summary>
    /// Checks if an insight exists. DEPRECATED - use SignatureExistsAsync.
    /// </summary>
    [Obsolete("Use SignatureExistsAsync instead")]
    Task<bool> InsightExistsAsync(Guid organizationId, Guid userId, InsightType type, Guid? entityId);
    
    #endregion
}
