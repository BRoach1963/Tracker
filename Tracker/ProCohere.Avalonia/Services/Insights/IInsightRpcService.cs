using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Abstracts RPC calls to Supabase for insight operations.
/// Separates RPC protocol from business logic.
/// </summary>
public interface IInsightRpcService
{
    /// <summary>
    /// Creates multiple insights via RPC.
    /// </summary>
    /// <param name="generatedFor">Target user (null = current user).</param>
    /// <param name="insights">Insights to create.</param>
    /// <returns>Number of insights created.</returns>
    Task<int> CreateInsightsBatchAsync(Guid? generatedFor, IReadOnlyList<Insight> insights);
    
    /// <summary>
    /// Creates a single insight via RPC.
    /// </summary>
    /// <param name="generatedFor">Target user (null = current user).</param>
    /// <param name="insight">Insight to create.</param>
    /// <returns>ID of the created insight.</returns>
    Task<Guid> CreateInsightAsync(Guid? generatedFor, Insight insight);
    
    /// <summary>
    /// Records a user action on an insight via RPC.
    /// </summary>
    /// <param name="signatureHash">Insight signature (64 hex chars).</param>
    /// <param name="actionType">Type: viewed, dismissed, snoozed, acted.</param>
    /// <param name="expiresAt">When the action expires (for snooze/dismiss).</param>
    /// <param name="insightId">Optional insight ID for audit trail.</param>
    /// <param name="reason">Optional reason for the action.</param>
    /// <returns>ID of the created action.</returns>
    Task<Guid> CreateActionAsync(
        string signatureHash, 
        string actionType, 
        DateTime? expiresAt = null, 
        Guid? insightId = null, 
        string? reason = null);
}
