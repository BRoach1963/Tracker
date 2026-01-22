using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing carry-forward lifecycle for deferred agenda items.
/// Handles deferral, expiration, and surfacing in future meetings.
/// </summary>
public class CarryForwardService
{
    #region Singleton

    private static readonly Lazy<CarryForwardService> _instance =
        new(() => new CarryForwardService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static CarryForwardService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "carry_forward_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Default expiration period for carry-forward items (30 days).
    /// </summary>
    public const int DefaultExpirationDays = 30;

    /// <summary>
    /// Maximum meeting opportunities before expiration.
    /// </summary>
    public const int MaxMeetingOpportunities = 2;

    private CarryForwardService() { }

    #region Defer Operations

    /// <summary>
    /// Defers an agenda item to a future meeting with a specific person.
    /// Creates the carry-forward tracking state.
    /// </summary>
    public async Task<bool> DeferAgendaItemAsync(
        Guid agendaItemId,
        Guid anchorTeamMemberId,
        int? expirationDays = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        var days = expirationDays ?? DefaultExpirationDays;
        var expiresAt = DateTime.UtcNow.AddDays(days);

        try
        {
            Log($"Deferring agenda item {agendaItemId} to team member {anchorTeamMemberId}, expires in {days} days");

            await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, agendaItemId.ToString())
                .Set(x => x.Status, "deferred")
                .Set(x => x.AnchorTeamMemberId!, anchorTeamMemberId)
                .Set(x => x.CarryForwardState!, CarryForwardState.Pending)
                .Set(x => x.CarryForwardExpiresAt!, expiresAt)
                .Set(x => x.CarryForwardMeetingCount, 0)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log("Agenda item deferred successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeferAgendaItem ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all pending carry-forward items for a specific team member.
    /// These are items that should be suggested for upcoming meetings with this person.
    /// </summary>
    public async Task<List<MeetingAgendaItem>> GetPendingCarryForwardsAsync(Guid anchorTeamMemberId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<MeetingAgendaItem>();
        }

        try
        {
            Log($"Getting pending carry-forwards for team member: {anchorTeamMemberId}");

            var result = await client.From<MeetingAgendaItem>()
                .Filter("anchor_team_member_id", Operator.Equals, anchorTeamMemberId.ToString())
                .Filter("carry_forward_state", Operator.In, new[] { CarryForwardState.Pending, CarryForwardState.Surfaced })
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Ascending)
                .Get();

            var items = result.Models ?? new List<MeetingAgendaItem>();

            // Filter out expired items
            var now = DateTime.UtcNow;
            var validItems = items.Where(i => 
                !i.IsExpired && 
                i.CarryForwardMeetingCount < MaxMeetingOpportunities).ToList();

            Log($"Found {validItems.Count} pending carry-forwards (filtered from {items.Count})");
            return validItems;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetPendingCarryForwards ERROR: {ex.Message}");
            return new List<MeetingAgendaItem>();
        }
    }

    /// <summary>
    /// Gets all pending carry-forward items for multiple team members (batch lookup).
    /// Useful for meeting prep when showing suggestions for all attendees.
    /// </summary>
    public async Task<Dictionary<Guid, List<MeetingAgendaItem>>> GetPendingCarryForwardsForAttendeesAsync(
        IEnumerable<Guid> teamMemberIds)
    {
        var result = new Dictionary<Guid, List<MeetingAgendaItem>>();
        foreach (var memberId in teamMemberIds)
        {
            var items = await GetPendingCarryForwardsAsync(memberId);
            if (items.Count > 0)
            {
                result[memberId] = items;
            }
        }
        return result;
    }

    #endregion

    #region Lifecycle Operations

    /// <summary>
    /// Marks a carry-forward item as surfaced (shown in meeting prep).
    /// </summary>
    public async Task<bool> MarkAsSurfacedAsync(Guid agendaItemId)
    {
        return await UpdateCarryForwardStateAsync(agendaItemId, CarryForwardState.Surfaced);
    }

    /// <summary>
    /// Marks a carry-forward item as resolved (discussed in meeting).
    /// </summary>
    public async Task<bool> MarkAsResolvedAsync(Guid agendaItemId)
    {
        return await UpdateCarryForwardStateAsync(agendaItemId, CarryForwardState.Resolved);
    }

    /// <summary>
    /// Marks a carry-forward item as converted (turned into task/action).
    /// </summary>
    public async Task<bool> MarkAsConvertedAsync(Guid agendaItemId)
    {
        return await UpdateCarryForwardStateAsync(agendaItemId, CarryForwardState.Converted);
    }

    /// <summary>
    /// Marks a carry-forward item as expired.
    /// </summary>
    public async Task<bool> MarkAsExpiredAsync(Guid agendaItemId)
    {
        return await UpdateCarryForwardStateAsync(agendaItemId, CarryForwardState.Expired);
    }

    /// <summary>
    /// Increments the meeting count for a carry-forward item.
    /// Call this when a meeting opportunity occurs with the anchored person.
    /// </summary>
    public async Task<bool> IncrementMeetingCountAsync(Guid agendaItemId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            // First get the current item
            var item = await MeetingAgendaItemService.Instance.GetAgendaItemAsync(agendaItemId);
            if (item == null)
            {
                LastError = "Agenda item not found";
                return false;
            }

            var newCount = item.CarryForwardMeetingCount + 1;
            Log($"Incrementing meeting count for {agendaItemId}: {item.CarryForwardMeetingCount} -> {newCount}");

            await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, agendaItemId.ToString())
                .Set(x => x.CarryForwardMeetingCount, newCount)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            // Auto-expire if meeting count reached
            if (newCount >= MaxMeetingOpportunities)
            {
                Log($"Meeting count reached {MaxMeetingOpportunities}, marking as expired");
                await MarkAsExpiredAsync(agendaItemId);
            }

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"IncrementMeetingCount ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the carry-forward state of an agenda item.
    /// </summary>
    private async Task<bool> UpdateCarryForwardStateAsync(Guid agendaItemId, string newState)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating carry-forward state for {agendaItemId} -> {newState}");

            await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, agendaItemId.ToString())
                .Set(x => x.CarryForwardState!, newState)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log("Carry-forward state updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateCarryForwardState ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Carry Forward Creation

    /// <summary>
    /// Creates a new agenda item in a new meeting, linked to the source deferred item.
    /// This "carries forward" the deferred item to the new meeting.
    /// </summary>
    public async Task<MeetingAgendaItem?> CarryForwardToMeetingAsync(
        Guid sourceAgendaItemId,
        Guid newMeetingId)
    {
        LastError = null;

        // Get the source item
        var sourceItem = await MeetingAgendaItemService.Instance.GetAgendaItemAsync(sourceAgendaItemId);
        if (sourceItem == null)
        {
            LastError = "Source agenda item not found";
            return null;
        }

        // Create a new agenda item in the new meeting
        var newItem = await MeetingAgendaItemService.Instance.CreateAgendaItemAsync(
            meetingId: newMeetingId,
            title: sourceItem.Title,
            description: sourceItem.Description,
            isPrivate: sourceItem.IsPrivate,
            linkedEntityType: "agenda_item",  // Link to the original
            linkedEntityId: sourceAgendaItemId
        );

        if (newItem == null)
        {
            LastError = MeetingAgendaItemService.Instance.LastError ?? "Failed to create carry-forward item";
            return null;
        }

        // Update the source item to track that it was carried forward
        await MarkAsSurfacedAsync(sourceAgendaItemId);
        await IncrementMeetingCountAsync(sourceAgendaItemId);

        // Set source reference on the new item
        var client = AuthService.Instance.GetProCohereClient();
        if (client != null)
        {
            try
            {
                await client.From<MeetingAgendaItem>()
                    .Filter("id", Operator.Equals, newItem.Id.ToString())
                    .Set(x => x.SourceAgendaItemId!, sourceAgendaItemId)
                    .Update();
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not set source reference: {ex.Message}");
            }
        }

        return newItem;
    }

    #endregion

    #region Expiration Check

    /// <summary>
    /// Checks and expires any carry-forward items that have passed their expiration date.
    /// Should be called periodically or during app startup.
    /// </summary>
    public async Task<int> ExpireOverdueItemsAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return 0;
        }

        var profile = AuthService.Instance.CurrentProfile;
        if (profile == null)
        {
            LastError = "Not authenticated - no profile";
            return 0;
        }

        var orgId = profile.OrganizationId;
        if (!orgId.HasValue)
        {
            LastError = "No organization context";
            return 0;
        }

        try
        {
            Log("Checking for overdue carry-forward items...");

            // Get all pending/surfaced items
            var result = await client.From<MeetingAgendaItem>()
                .Filter("organization_id", Operator.Equals, orgId.Value.ToString())
                .Filter("carry_forward_state", Operator.In, new[] { CarryForwardState.Pending, CarryForwardState.Surfaced })
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var items = result.Models ?? new List<MeetingAgendaItem>();
            var now = DateTime.UtcNow;
            var expiredCount = 0;

            foreach (var item in items)
            {
                var shouldExpire = false;
                
                // Check date expiration
                if (item.CarryForwardExpiresAt.HasValue && now > item.CarryForwardExpiresAt.Value)
                {
                    shouldExpire = true;
                    Log($"Item {item.Id} expired by date: {item.CarryForwardExpiresAt.Value:yyyy-MM-dd}");
                }
                
                // Check meeting count
                if (item.CarryForwardMeetingCount >= MaxMeetingOpportunities)
                {
                    shouldExpire = true;
                    Log($"Item {item.Id} expired by meeting count: {item.CarryForwardMeetingCount}");
                }

                if (shouldExpire)
                {
                    await MarkAsExpiredAsync(item.Id);
                    expiredCount++;
                }
            }

            Log($"Expired {expiredCount} overdue items");
            return expiredCount;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ExpireOverdueItems ERROR: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region Query Helpers

    /// <summary>
    /// Gets statistics about carry-forward items for the current organization.
    /// </summary>
    public async Task<CarryForwardStats> GetCarryForwardStatsAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new CarryForwardStats();
        }

        var profile = AuthService.Instance.CurrentProfile;
        if (profile == null)
        {
            LastError = "Not authenticated - no profile";
            return new CarryForwardStats();
        }

        var orgId = profile.OrganizationId;
        if (!orgId.HasValue)
        {
            LastError = "No organization context";
            return new CarryForwardStats();
        }

        try
        {
            var result = await client.From<MeetingAgendaItem>()
                .Filter("organization_id", Operator.Equals, orgId.Value.ToString())
                .Filter("status", Operator.Equals, "deferred")
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var items = result.Models ?? new List<MeetingAgendaItem>();

            return new CarryForwardStats
            {
                TotalDeferred = items.Count,
                Pending = items.Count(i => i.CarryForwardState == CarryForwardState.Pending),
                Surfaced = items.Count(i => i.CarryForwardState == CarryForwardState.Surfaced),
                Resolved = items.Count(i => i.CarryForwardState == CarryForwardState.Resolved),
                Converted = items.Count(i => i.CarryForwardState == CarryForwardState.Converted),
                Expired = items.Count(i => i.CarryForwardState == CarryForwardState.Expired),
                ExpiringWithin7Days = items.Count(i => 
                    i.CarryForwardState == CarryForwardState.Pending &&
                    i.CarryForwardExpiresAt.HasValue &&
                    i.CarryForwardExpiresAt.Value <= DateTime.UtcNow.AddDays(7))
            };
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetCarryForwardStats ERROR: {ex.Message}");
            return new CarryForwardStats();
        }
    }

    /// <summary>
    /// Gets the original source of a carry-forward chain.
    /// Follows the source_agenda_item_id chain back to the original item.
    /// </summary>
    public async Task<MeetingAgendaItem?> GetOriginalSourceAsync(Guid agendaItemId)
    {
        var current = await MeetingAgendaItemService.Instance.GetAgendaItemAsync(agendaItemId);
        if (current == null) return null;

        // Follow the chain (with a safety limit)
        var maxDepth = 10;
        var depth = 0;

        while (current.SourceAgendaItemId.HasValue && depth < maxDepth)
        {
            var source = await MeetingAgendaItemService.Instance.GetAgendaItemAsync(current.SourceAgendaItemId.Value);
            if (source == null) break;
            current = source;
            depth++;
        }

        return current;
    }

    #endregion
}

/// <summary>
/// Statistics about carry-forward items.
/// </summary>
public class CarryForwardStats
{
    public int TotalDeferred { get; set; }
    public int Pending { get; set; }
    public int Surfaced { get; set; }
    public int Resolved { get; set; }
    public int Converted { get; set; }
    public int Expired { get; set; }
    public int ExpiringWithin7Days { get; set; }

    public int ActiveCount => Pending + Surfaced;
    public int CompletedCount => Resolved + Converted;
}
