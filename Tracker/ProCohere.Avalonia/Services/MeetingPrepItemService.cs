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

    /// <summary>
    /// Valid linked entity types.
    /// </summary>
    public static readonly string[] ValidLinkedEntityTypes =
    {
        "task",
        "goal",
        "metric",
        "project"
    };

    /// <summary>
    /// Valid source types for provenance tracking.
    /// </summary>
    public static readonly string[] ValidSourceTypes =
    {
        "manual",
        "scaffold",
        "ai",
        "carry_forward"
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
                // Enhanced prep fields
                .Set(p => p.LinkedEntityType!, item.LinkedEntityType)
                .Set(p => p.LinkedEntityId!, item.LinkedEntityId)
                .Set(p => p.LinkedEntityTitleSnapshot!, item.LinkedEntityTitleSnapshot)
                .Set(p => p.PrepPrompt!, item.PrepPrompt)
                .Set(p => p.PrepResponse!, item.PrepResponse)
                .Set(p => p.PreparedAt!, item.PreparedAt)
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

    /// <summary>
    /// Creates a prep item linked to an entity (task, goal, metric, project).
    /// </summary>
    public async Task<MeetingPrepItem?> CreateLinkedPrepAsync(
        Guid meetingId,
        string linkedEntityType,
        Guid linkedEntityId,
        string linkedEntityTitle,
        string? prepPrompt = null,
        string visibilityScope = "personal")
    {
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = linkedEntityTitle,
            LinkedEntityType = linkedEntityType,
            LinkedEntityId = linkedEntityId,
            LinkedEntityTitleSnapshot = linkedEntityTitle,
            PrepPrompt = prepPrompt,
            VisibilityScope = visibilityScope,
            Status = "open",
            SourceType = "manual"
        };

        return await CreatePrepItemAsync(item);
    }

    /// <summary>
    /// Captures the preparation response for a prep item.
    /// </summary>
    public async Task<bool> CapturePrepResponseAsync(Guid prepItemId, string response)
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
            Log($"Capturing prep response for: {prepItemId}");

            var now = DateTime.UtcNow;
            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.PrepResponse!, response)
                .Set(p => p.PreparedAt!, now)
                .Set(p => p.UpdatedAt!, now)
                .Update();

            Log($"Prep response captured: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CapturePrepResponse ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the prep prompt for a prep item.
    /// </summary>
    public async Task<bool> UpdatePrepPromptAsync(Guid prepItemId, string prompt)
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
            Log($"Updating prep prompt for: {prepItemId}");

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.PrepPrompt!, prompt)
                .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                .Update();

            Log($"Prep prompt updated: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdatePrepPrompt ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Links an existing entity to a prep item.
    /// </summary>
    public async Task<bool> LinkEntityAsync(
        Guid prepItemId,
        string entityType,
        Guid entityId,
        string entityTitle)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        if (!ValidLinkedEntityTypes.Contains(entityType))
        {
            LastError = $"Invalid entity type: {entityType}";
            return false;
        }

        try
        {
            Log($"Linking entity {entityType}:{entityId} to prep item: {prepItemId}");

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.LinkedEntityType!, entityType)
                .Set(p => p.LinkedEntityId!, entityId)
                .Set(p => p.LinkedEntityTitleSnapshot!, entityTitle)
                .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                .Update();

            Log($"Entity linked to prep item: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"LinkEntity ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes the linked entity from a prep item.
    /// </summary>
    public async Task<bool> UnlinkEntityAsync(Guid prepItemId)
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
            Log($"Unlinking entity from prep item: {prepItemId}");

            await client.From<MeetingPrepItem>()
                .Filter("id", Operator.Equals, prepItemId.ToString())
                .Set(p => p.LinkedEntityType!, (string?)null)
                .Set(p => p.LinkedEntityId!, (Guid?)null)
                .Set(p => p.LinkedEntityTitleSnapshot!, (string?)null)
                .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                .Update();

            Log($"Entity unlinked from prep item: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UnlinkEntity ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets prep items for a specific linked entity (e.g., all prep items linked to a task).
    /// </summary>
    public async Task<List<MeetingPrepItem>> GetPrepItemsForEntityAsync(
        string entityType,
        Guid entityId)
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
            Log($"Loading prep items for entity: {entityType}:{entityId}");

            var result = await client.From<MeetingPrepItem>()
                .Filter("linked_entity_type", Operator.Equals, entityType)
                .Filter("linked_entity_id", Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Descending)
                .Get();

            var items = result.Models ?? new List<MeetingPrepItem>();
            Log($"Found {items.Count} prep items for entity");

            // Set current user ID for permission checks
            var currentUserId = session.TeamMember.Id;
            foreach (var item in items)
            {
                item.CurrentUserTeamMemberId = currentUserId;
            }

            await PopulateNamesAsync(items);
            return items;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetPrepItemsForEntity ERROR: {ex.Message}");
            return new List<MeetingPrepItem>();
        }
    }

    /// <summary>
    /// Carries forward incomplete prep items to a new meeting.
    /// </summary>
    public async Task<List<MeetingPrepItem>> CarryForwardPrepItemsAsync(
        Guid fromMeetingId,
        Guid toMeetingId)
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
            Log($"Carrying forward prep items from {fromMeetingId} to {toMeetingId}");

            // Get incomplete items marked for carry forward
            var itemsToCarry = await client.From<MeetingPrepItem>()
                .Filter("meeting_id", Operator.Equals, fromMeetingId.ToString())
                .Filter("carry_forward", Operator.Equals, "true")
                .Filter("status", Operator.In, new[] { "open", "in_progress" })
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var sourceItems = itemsToCarry.Models ?? new List<MeetingPrepItem>();
            if (sourceItems.Count == 0)
            {
                Log("No items to carry forward");
                return new List<MeetingPrepItem>();
            }

            var createdItems = new List<MeetingPrepItem>();
            var orgId = session.TeamMember.OrganizationId;

            foreach (var source in sourceItems)
            {
                var newItem = new MeetingPrepItem
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    MeetingId = toMeetingId,
                    RequestedByTeamMemberId = source.RequestedByTeamMemberId,
                    AssignedToTeamMemberId = source.AssignedToTeamMemberId,
                    Title = source.Title,
                    Body = source.Body,
                    VisibilityScope = source.VisibilityScope,
                    Status = "open",
                    DueAt = source.DueAt,
                    SortOrder = source.SortOrder,
                    CarryForward = source.CarryForward,
                    CarriedFromPrepItemId = source.Id,
                    SourceType = "carry_forward",
                    LinkedEntityType = source.LinkedEntityType,
                    LinkedEntityId = source.LinkedEntityId,
                    LinkedEntityTitleSnapshot = source.LinkedEntityTitleSnapshot,
                    PrepPrompt = source.PrepPrompt,
                    // Don't carry over the response - they need to prepare again
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await client.From<MeetingPrepItem>().Insert(newItem);
                var created = result.Models?.FirstOrDefault();
                if (created != null)
                {
                    createdItems.Add(created);
                }
            }

            Log($"Carried forward {createdItems.Count} prep items");
            return createdItems;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CarryForwardPrepItems ERROR: {ex.Message}");
            return new List<MeetingPrepItem>();
        }
    }

    #endregion
}
