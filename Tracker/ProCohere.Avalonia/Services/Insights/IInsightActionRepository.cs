using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Repository interface for insight action operations.
/// Actions track user responses to insights (dismiss, snooze, etc.).
/// </summary>
public interface IInsightActionRepository
{
    /// <summary>
    /// Gets all active (non-expired) actions for a user.
    /// </summary>
    Task<List<InsightAction>> GetActiveActionsAsync(Guid teamMemberId);
    
    /// <summary>
    /// Gets active actions for specific signatures.
    /// Used to filter insights that have been dismissed/snoozed.
    /// </summary>
    Task<List<InsightAction>> GetActiveActionsForSignaturesAsync(
        Guid teamMemberId, 
        IEnumerable<string> signatureHashes);
    
    /// <summary>
    /// Checks if an active action exists for a signature.
    /// </summary>
    Task<bool> HasActiveActionAsync(Guid teamMemberId, string signatureHash, string actionType);
    
    /// <summary>
    /// Dismisses an insight for a period (default 30 days).
    /// </summary>
    Task<Guid> DismissAsync(string signatureHash, TimeSpan? duration = null, Guid? insightId = null);
    
    /// <summary>
    /// Snoozes an insight for a duration.
    /// </summary>
    Task<Guid> SnoozeAsync(string signatureHash, TimeSpan duration, Guid? insightId = null);
    
    /// <summary>
    /// Marks an insight as acted upon (user took action).
    /// </summary>
    Task<Guid> MarkActedAsync(string signatureHash, Guid? insightId = null);
    
    /// <summary>
    /// Marks an insight as viewed.
    /// </summary>
    Task<Guid> MarkViewedAsync(string signatureHash, Guid? insightId = null);
}
