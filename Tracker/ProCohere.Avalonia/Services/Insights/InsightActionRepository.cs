using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dtos;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Repository for insight action operations.
/// Reads use direct queries; writes use RPC service.
/// </summary>
public class InsightActionRepository : IInsightActionRepository
{
    private static readonly TimeSpan DefaultDismissDuration = TimeSpan.FromDays(30);
    
    private readonly IInsightRpcService _rpcService;
    
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;
    
    public InsightActionRepository(IInsightRpcService rpcService)
    {
        _rpcService = rpcService;
    }
    
    public async Task<List<InsightAction>> GetActiveActionsAsync(Guid teamMemberId)
    {
        var response = await Client
            .From<InsightActionDto>()
            .Where(x => x.TeamMemberId == teamMemberId)
            .Where(x => x.IsDeleted == false)
            .Order("created_at", Ordering.Descending)
            .Get();
        
        return response.Models
            .Select(MapToModel)
            .Where(a => a.IsActive) // Filter expired in-memory
            .ToList();
    }
    
    public async Task<List<InsightAction>> GetActiveActionsForSignaturesAsync(
        Guid teamMemberId, 
        IEnumerable<string> signatureHashes)
    {
        var signatures = signatureHashes.ToList();
        if (signatures.Count == 0)
            return new List<InsightAction>();
        
        // Supabase doesn't support IN queries easily, so fetch all and filter
        // For large datasets, consider a custom RPC
        var allActions = await GetActiveActionsAsync(teamMemberId);
        
        return allActions
            .Where(a => signatures.Contains(a.SignatureHash))
            .ToList();
    }
    
    public async Task<bool> HasActiveActionAsync(Guid teamMemberId, string signatureHash, string actionType)
    {
        var actions = await GetActiveActionsForSignaturesAsync(teamMemberId, new[] { signatureHash });
        return actions.Any(a => a.ActionType == actionType && a.IsActive);
    }
    
    public async Task<Guid> DismissAsync(string signatureHash, TimeSpan? duration = null, Guid? insightId = null)
    {
        var expiresAt = DateTime.UtcNow.Add(duration ?? DefaultDismissDuration);
        
        Debug.WriteLine($"[InsightActionRepository] Dismissing {signatureHash[..8]}... until {expiresAt:yyyy-MM-dd}");
        
        return await _rpcService.CreateActionAsync(
            signatureHash,
            InsightActionType.Dismissed,
            expiresAt,
            insightId);
    }
    
    public async Task<Guid> SnoozeAsync(string signatureHash, TimeSpan duration, Guid? insightId = null)
    {
        var expiresAt = DateTime.UtcNow.Add(duration);
        
        Debug.WriteLine($"[InsightActionRepository] Snoozing {signatureHash[..8]}... for {duration.TotalHours}h");
        
        return await _rpcService.CreateActionAsync(
            signatureHash,
            InsightActionType.Snoozed,
            expiresAt,
            insightId);
    }
    
    public async Task<Guid> MarkActedAsync(string signatureHash, Guid? insightId = null)
    {
        Debug.WriteLine($"[InsightActionRepository] Marking acted: {signatureHash[..8]}...");
        
        return await _rpcService.CreateActionAsync(
            signatureHash,
            InsightActionType.Acted,
            expiresAt: null, // Acted doesn't expire
            insightId);
    }
    
    public async Task<Guid> MarkViewedAsync(string signatureHash, Guid? insightId = null)
    {
        Debug.WriteLine($"[InsightActionRepository] Marking viewed: {signatureHash[..8]}...");
        
        return await _rpcService.CreateActionAsync(
            signatureHash,
            InsightActionType.Viewed,
            expiresAt: null, // Viewed doesn't expire
            insightId);
    }
    
    private static InsightAction MapToModel(InsightActionDto dto)
    {
        return new InsightAction
        {
            Id = dto.Id,
            OrganizationId = dto.OrganizationId,
            TeamMemberId = dto.TeamMemberId,
            InsightId = dto.InsightId,
            SignatureHash = dto.SignatureHash,
            ActionType = dto.ActionType,
            ActionReason = dto.ActionReason,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt,
            DeletedBy = dto.DeletedBy
        };
    }
}
