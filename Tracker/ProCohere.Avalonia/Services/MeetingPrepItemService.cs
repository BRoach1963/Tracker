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
/// Service for managing meeting prep items in Supabase.
/// Handles CRUD operations for prep items with proper visibility scoping.
/// 
/// Visibility scopes:
/// - personal: Only visible to the requester
/// - assigned: Visible to requester AND assignee
/// - meeting: Visible to all meeting attendees (team prep)
/// </summary>
public class MeetingPrepItemService
{
    #region Singleton

    private static readonly Lazy<MeetingPrepItemService> _instance =
        new(() => new MeetingPrepItemService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MeetingPrepItemService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "meeting_prep_service.log");

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
    /// Valid prep item statuses.
    /// </summary>
    public static readonly string[] ValidStatuses =
    {
        "open",
        "in_progress",
        "done",
        "dismissed"
    };

    /// <summary>
    /// Valid visibility scopes.
    /// </summary>
    public static readonly string[] ValidVisibilityScopes =
    {
        "personal",
        "assigned",
        "meeting"
    };

    private MeetingPrepItemService() { }

    #region Prep Item Loading

    /// <summary>
    /// Loads all prep items for a meeting that the current user can see.
    /// RLS will filter based on visibility scope and attendee status.
    /// </summary>
    public async Task<List<MeetingPrepItem>> GetPrepItemsForMeetingAsync(Guid meetingId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return new List<MeetingPrepItem>();
        }

        try
        {
            var currentUserId = session.TeamMember.Id;
            Log($"Loading prep items for meeting: {meetingId}, user: {currentUserId}");

            var result = await client.From<MeetingPrepItem>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("sort_order", Ordering.Ascending)
                .Get();

            var prepItems = result.Models ?? new List<MeetingPrepItem>();
            Log($"Loaded {prepItems.Count} prep items");

            // Set the current user ID on each item for permission checks
            foreach (var item in prepItems)
            {
                item.CurrentUserTeamMemberId = currentUserId;
            }

            // Load team member names for display
            await PopulateNamesAsync(prepItems);

            return prepItems;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetPrepItemsForMeeting ERROR: {ex.Message}");
            return new List<MeetingPrepItem>();
        }
    }

    /// <summary>
    /// Populates RequestedByName and AssignedToName for display.
    /// </summary>
    private async Task PopulateNamesAsync(List<MeetingPrepItem> items)
    {
        if (items.Count == 0) return;

        try
        {
            // Get all unique team member IDs
            var memberIds = items
                .SelectMany(i => new[] { i.RequestedByTeamMemberId, i.AssignedToTeamMemberId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Concat(items.Select(i => i.RequestedByTeamMemberId))
                .Distinct()
                .ToList();

            if (memberIds.Count == 0) return;

            // Load team members
            var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            var memberDict = teamMembers.ToDictionary(m => m.Id, m => m.FullName);

            // Populate names
            foreach (var item in items)
            {
                if (memberDict.TryGetValue(item.RequestedByTeamMemberId, out var requesterName))
                {
                    item.RequestedByName = requesterName;
                }

                if (item.AssignedToTeamMemberId.HasValue && 
                    memberDict.TryGetValue(item.AssignedToTeamMemberId.Value, out var assigneeName))
                {
                    item.AssignedToName = assigneeName;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"PopulateNames warning: {ex.Message}");
            // Non-fatal, names will just be empty
        }
    }

    #endregion

    #region Prep Item CRUD

    /// <summary>
    /// Creates a new prep item.
    /// </summary>
    public async Task<MeetingPrepItem?> CreatePrepItemAsync(MeetingPrepItem item)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var orgId = session.TeamMember.OrganizationId;
            var creatorId = session.TeamMember.Id;

            Log($"Creating prep item: {item.Title} for meeting: {item.MeetingId}");

            // Set required fields
            item.Id = Guid.NewGuid();
            item.OrganizationId = orgId;
            item.RequestedByTeamMemberId = creatorId;
            item.Status = item.Status ?? "open";
            item.VisibilityScope = item.VisibilityScope ?? "personal";
            item.IsDeleted = false;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<MeetingPrepItem>().Insert(item);
            var created = result.Models?.FirstOrDefault();

            if (created != null)
            {
                created.CurrentUserTeamMemberId = creatorId;
                Log($"Prep item created: {created.Id}");
            }
            else
            {
                LastError = "Failed to create prep item";
                Log("CreatePrepItem ERROR: Insert returned no model");
            }

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreatePrepItem ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing prep item.
    /// Note: Only certain fields can be updated based on user's role (requester vs assignee).
    /// </summary>
    public async Task<bool> UpdatePrepItemAsync(MeetingPrepItem item)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating prep item: {item.Id}");

            item.UpdatedAt = DateTime.UtcNow;

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, item.Id.ToString())
                .Set(p => p.Title, item.Title)
                .Set(p => p.Body!, item.Body)
                .Set(p => p.AssignedToTeamMemberId!, item.AssignedToTeamMemberId)
                .Set(p => p.VisibilityScope!, item.VisibilityScope)
                .Set(p => p.Status!, item.Status)
                .Set(p => p.AssigneeNotes!, item.AssigneeNotes)
                .Set(p => p.DueAt!, item.DueAt)
                .Set(p => p.SortOrder!, item.SortOrder)
                .Set(p => p.CarryForward, item.CarryForward)
                .Set(p => p.UpdatedAt!, item.UpdatedAt)
                .Update();

            Log($"Prep item updated: {item.Id}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdatePrepItem ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the status of a prep item.
    /// </summary>
    public async Task<bool> UpdateStatusAsync(Guid prepItemId, string newStatus)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        if (!ValidStatuses.Contains(newStatus))
        {
            LastError = $"Invalid status: {newStatus}";
            return false;
        }

        try
        {
            var currentUserId = session.TeamMember.Id;
            Log($"Updating prep item status: {prepItemId} -> {newStatus}");

            var now = DateTime.UtcNow;
            DateTime? completedAt = (newStatus == "done") ? now : null;
            Guid? completedBy = (newStatus == "done") ? currentUserId : null;

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.Status!, newStatus)
                .Set(p => p.StatusUpdatedAt!, now)
                .Set(p => p.StatusUpdatedByTeamMemberId!, currentUserId)
                .Set(p => p.CompletedAt!, completedAt)
                .Set(p => p.CompletedByTeamMemberId!, completedBy)
                .Set(p => p.UpdatedAt!, now)
                .Update();

            Log($"Prep item status updated: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateStatus ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Soft deletes a prep item.
    /// </summary>
    public async Task<bool> DeletePrepItemAsync(Guid prepItemId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var deletedBy = session.TeamMember.Id;
            Log($"Deleting prep item: {prepItemId}");

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.IsDeleted, true)
                .Set(p => p.DeletedAt!, DateTime.UtcNow)
                .Set(p => p.DeletedBy!, deletedBy)
                .Update();

            Log($"Prep item deleted: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeletePrepItem ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a quick personal prep item with minimal info.
    /// </summary>
    public async Task<MeetingPrepItem?> CreateQuickPrepAsync(Guid meetingId, string title)
    {
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = title,
            VisibilityScope = "personal",
            Status = "open"
        };

        return await CreatePrepItemAsync(item);
    }

    /// <summary>
    /// Creates an assigned prep item (visible to requester and assignee).
    /// </summary>
    public async Task<MeetingPrepItem?> CreateAssignedPrepAsync(
        Guid meetingId, 
        string title, 
        Guid assigneeId, 
        string? body = null,
        DateTime? dueAt = null)
    {
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = title,
            Body = body,
            AssignedToTeamMemberId = assigneeId,
            VisibilityScope = "assigned",
            Status = "open",
            DueAt = dueAt
        };

        return await CreatePrepItemAsync(item);
    }

    /// <summary>
    /// Creates a team/meeting prep item (visible to all attendees).
    /// </summary>
    public async Task<MeetingPrepItem?> CreateTeamPrepAsync(
        Guid meetingId, 
        string title, 
        string? body = null)
    {
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = title,
            Body = body,
            VisibilityScope = "meeting",
            Status = "open"
        };

        return await CreatePrepItemAsync(item);
    }

    #endregion
}
