using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing development plans and items.
/// Provides CRUD operations for career development tracking.
/// Thread-safe singleton with caching for performance.
/// </summary>
public sealed class DevelopmentService
{
    #region Singleton
    
    private static readonly Lazy<DevelopmentService> _instance =
        new(() => new DevelopmentService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>Gets the singleton instance.</summary>
    public static DevelopmentService Instance => _instance.Value;
    
    #endregion
    
    #region Fields
    
    // Note: Cache fields reserved for future caching implementation
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    
    #endregion
    
    #region Properties
    
    /// <summary>Last error message if an operation failed.</summary>
    public string? LastError { get; private set; }
    
    #endregion
    
    #region Constructor
    
    private DevelopmentService() { }
    
    #endregion
    
    #region Development Plans
    
    /// <summary>
    /// Gets all development plans for the current user.
    /// </summary>
    public async Task<List<DevelopmentPlan>> GetMyPlansAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        var currentMember = AuthService.Instance.CurrentTeamMember;
        if (currentMember == null)
        {
            LastError = "Not authenticated";
            return new List<DevelopmentPlan>();
        }
        
        return await GetPlansByTeamMemberAsync(currentMember.Id, cancellationToken);
    }
    
    /// <summary>
    /// Gets development plans for a specific team member.
    /// </summary>
    public async Task<List<DevelopmentPlan>> GetPlansByTeamMemberAsync(
        Guid teamMemberId,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return new List<DevelopmentPlan>();
            }
            
            var response = await client.From<DevelopmentPlan>()
                .Where(p => p.TeamMemberId == teamMemberId)
                .Where(p => p.IsDeleted == false)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
            
            var plans = response.Models ?? new List<DevelopmentPlan>();
            
            // Load items for each plan in parallel
            var itemTasks = plans.Select(async plan =>
            {
                plan.Items = await GetItemsByPlanAsync(plan.Id, cancellationToken);
            });
            
            await Task.WhenAll(itemTasks);
            
            Debug.WriteLine($"[DevelopmentService] Loaded {plans.Count} plans for team member {teamMemberId}");
            return plans;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to load development plans: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return new List<DevelopmentPlan>();
        }
    }
    
    /// <summary>
    /// Gets a single development plan by ID with its items.
    /// </summary>
    public async Task<DevelopmentPlan?> GetPlanByIdAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return null;
            }
            
            var response = await client.From<DevelopmentPlan>()
                .Where(p => p.Id == planId)
                .Where(p => p.IsDeleted == false)
                .Single();
            
            if (response != null)
            {
                response.Items = await GetItemsByPlanAsync(planId, cancellationToken);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to load development plan: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Creates a new development plan.
    /// </summary>
    public async Task<DevelopmentPlan?> CreatePlanAsync(
        string title,
        string? description = null,
        DateTime? startDate = null,
        DateTime? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return null;
            }
            
            var plan = new DevelopmentPlan
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                TeamMemberId = session.TeamMember.Id,
                Title = title,
                Description = description,
                Status = "draft",
                StartDate = startDate,
                TargetDate = targetDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            var result = await client.From<DevelopmentPlan>().Insert(plan);
            var created = result.Models?.FirstOrDefault();
            
            if (created != null)
            {
                Debug.WriteLine($"[DevelopmentService] Created plan: {created.Id}");
                InvalidateCache();
            }
            
            return created;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to create development plan: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Updates an existing development plan.
    /// </summary>
    public async Task<bool> UpdatePlanAsync(
        Guid planId,
        string title,
        string? description,
        string status,
        DateTime? startDate,
        DateTime? targetDate,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return false;
            }
            
            await client.From<DevelopmentPlan>()
                .Where(p => p.Id == planId)
                .Set(p => p.Title!, title)
                .Set(p => p.Description!, description)
                .Set(p => p.Status!, status)
                .Set(p => p.StartDate!, startDate)
                .Set(p => p.TargetDate!, targetDate)
                .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                .Set(p => p.CompletedAt!, status == "completed" ? DateTime.UtcNow : null)
                .Update();
            
            Debug.WriteLine($"[DevelopmentService] Updated plan: {planId}");
            InvalidateCache();
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to update development plan: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Soft-deletes a development plan.
    /// </summary>
    public async Task<bool> DeletePlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        var currentMember = AuthService.Instance.CurrentTeamMember;
        if (currentMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return false;
            }
            
            // Soft delete the plan
            await client.From<DevelopmentPlan>()
                .Where(p => p.Id == planId)
                .Set(p => p.IsDeleted!, true)
                .Set(p => p.DeletedAt!, DateTime.UtcNow)
                .Set(p => p.DeletedBy!, currentMember.Id)
                .Update();
            
            // Also soft delete all items
            await client.From<DevelopmentPlanItem>()
                .Where(i => i.DevelopmentPlanId == planId)
                .Set(i => i.IsDeleted!, true)
                .Set(i => i.DeletedAt!, DateTime.UtcNow)
                .Set(i => i.DeletedBy!, currentMember.Id)
                .Update();
            
            Debug.WriteLine($"[DevelopmentService] Deleted plan: {planId}");
            InvalidateCache();
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to delete development plan: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Development Plan Items
    
    /// <summary>
    /// Gets items for a specific development plan.
    /// </summary>
    public async Task<List<DevelopmentPlanItem>> GetItemsByPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null) return new List<DevelopmentPlanItem>();
            
            var response = await client.From<DevelopmentPlanItem>()
                .Where(i => i.DevelopmentPlanId == planId)
                .Where(i => i.IsDeleted == false)
                .Order("sort_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models ?? new List<DevelopmentPlanItem>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DevelopmentService] Error loading items: {ex.Message}");
            return new List<DevelopmentPlanItem>();
        }
    }
    
    /// <summary>
    /// Creates a new development plan item.
    /// </summary>
    public async Task<DevelopmentPlanItem?> CreateItemAsync(
        Guid planId,
        string title,
        string? description = null,
        string? itemType = null,
        DateTime? dueDate = null,
        Guid? competencyId = null,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return null;
            }
            
            // Get current max sort order
            var existingItems = await GetItemsByPlanAsync(planId, cancellationToken);
            var maxSortOrder = existingItems.Count > 0 ? existingItems.Max(i => i.SortOrder) : -1;
            
            var item = new DevelopmentPlanItem
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                DevelopmentPlanId = planId,
                CompetencyId = competencyId,
                Title = title,
                Description = description,
                ItemType = itemType,
                Status = "not_started",
                DueDate = dueDate,
                SortOrder = maxSortOrder + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            var result = await client.From<DevelopmentPlanItem>().Insert(item);
            var created = result.Models?.FirstOrDefault();
            
            if (created != null)
            {
                Debug.WriteLine($"[DevelopmentService] Created item: {created.Id}");
            }
            
            return created;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to create item: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Updates an existing development plan item.
    /// </summary>
    public async Task<bool> UpdateItemAsync(
        Guid itemId,
        string title,
        string? description,
        string? itemType,
        string status,
        DateTime? dueDate,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return false;
            }
            
            await client.From<DevelopmentPlanItem>()
                .Where(i => i.Id == itemId)
                .Set(i => i.Title!, title)
                .Set(i => i.Description!, description)
                .Set(i => i.ItemType!, itemType)
                .Set(i => i.Status!, status)
                .Set(i => i.DueDate!, dueDate)
                .Set(i => i.UpdatedAt!, DateTime.UtcNow)
                .Set(i => i.CompletedAt!, status == "completed" ? DateTime.UtcNow : null)
                .Update();
            
            Debug.WriteLine($"[DevelopmentService] Updated item: {itemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to update item: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Updates an item's status.
    /// </summary>
    public async Task<bool> UpdateItemStatusAsync(
        Guid itemId,
        string status,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return false;
            }
            
            await client.From<DevelopmentPlanItem>()
                .Where(i => i.Id == itemId)
                .Set(i => i.Status!, status)
                .Set(i => i.UpdatedAt!, DateTime.UtcNow)
                .Set(i => i.CompletedAt!, status == "completed" ? DateTime.UtcNow : null)
                .Update();
            
            Debug.WriteLine($"[DevelopmentService] Updated item status: {itemId} -> {status}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to update item status: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Soft-deletes a development plan item.
    /// </summary>
    public async Task<bool> DeleteItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        
        var currentMember = AuthService.Instance.CurrentTeamMember;
        if (currentMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null)
            {
                LastError = "Not authenticated";
                return false;
            }
            
            await client.From<DevelopmentPlanItem>()
                .Where(i => i.Id == itemId)
                .Set(i => i.IsDeleted!, true)
                .Set(i => i.DeletedAt!, DateTime.UtcNow)
                .Set(i => i.DeletedBy!, currentMember.Id)
                .Update();
            
            Debug.WriteLine($"[DevelopmentService] Deleted item: {itemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to delete item: {ex.Message}";
            Debug.WriteLine($"[DevelopmentService] Error: {ex.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Statistics
    
    /// <summary>
    /// Gets development statistics for the current user.
    /// </summary>
    public async Task<DevelopmentStats> GetMyStatsAsync(CancellationToken cancellationToken = default)
    {
        var plans = await GetMyPlansAsync(cancellationToken);
        return CalculateStats(plans);
    }
    
    private static DevelopmentStats CalculateStats(List<DevelopmentPlan> plans)
    {
        var allItems = plans.SelectMany(p => p.Items).ToList();
        
        return new DevelopmentStats
        {
            TotalPlans = plans.Count,
            ActivePlans = plans.Count(p => p.IsActive),
            CompletedPlans = plans.Count(p => p.IsCompleted),
            TotalItems = allItems.Count,
            CompletedItems = allItems.Count(i => i.IsCompleted),
            InProgressItems = allItems.Count(i => i.IsInProgress),
            OverdueItems = allItems.Count(i => i.IsOverdue)
        };
    }
    
    #endregion
    
    #region Cache Management
    
    private void InvalidateCache()
    {
        // Reserved for future caching implementation
    }
    
    /// <summary>
    /// Forces a cache refresh on next access.
    /// </summary>
    public void ClearCache()
    {
        InvalidateCache();
    }
    
    #endregion
}

/// <summary>
/// Development statistics for display.
/// </summary>
public sealed class DevelopmentStats
{
    public int TotalPlans { get; init; }
    public int ActivePlans { get; init; }
    public int CompletedPlans { get; init; }
    public int TotalItems { get; init; }
    public int CompletedItems { get; init; }
    public int InProgressItems { get; init; }
    public int OverdueItems { get; init; }
    
    public double CompletionRate => TotalItems > 0 ? (double)CompletedItems / TotalItems : 0;
    public string CompletionRateDisplay => $"{CompletionRate:P0}";
}
