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
/// Service for managing meeting agenda items in Supabase.
/// Handles CRUD operations, status updates, and task creation from agenda items.
/// </summary>
public class MeetingAgendaItemService
{
    #region Singleton

    private static readonly Lazy<MeetingAgendaItemService> _instance =
        new(() => new MeetingAgendaItemService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MeetingAgendaItemService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "agenda_service.log");

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
    /// Valid status values for agenda items.
    /// </summary>
    public static readonly string[] ValidStatuses = 
    { 
        "open", 
        "discussed", 
        "action_created", 
        "deferred", 
        "dropped" 
    };

    private MeetingAgendaItemService() { }

    /// <summary>
    /// Gets a single agenda item by ID.
    /// </summary>
    public async Task<MeetingAgendaItem?> GetAgendaItemAsync(Guid itemId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Getting agenda item: {itemId}");
            var result = await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, itemId.ToString())
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAgendaItem ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all agenda items for a specific meeting.
    /// </summary>
    public async Task<List<MeetingAgendaItem>> GetAgendaItemsForMeetingAsync(Guid meetingId)
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
            Log($"Loading agenda items for meeting: {meetingId}");
            
            var result = await client.From<MeetingAgendaItem>()
                .Filter("meeting_id", Operator.Equals, meetingId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("sort_order", Ordering.Ascending)
                .Get();

            Log($"Agenda items returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<MeetingAgendaItem>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAgendaItemsForMeeting ERROR: {ex.Message}");
            return new List<MeetingAgendaItem>();
        }
    }

    /// <summary>
    /// Creates a new agenda item for a meeting.
    /// </summary>
    public async Task<MeetingAgendaItem?> CreateAgendaItemAsync(
        Guid meetingId,
        string title,
        string? description = null,
        int sortOrder = 0,
        bool isPrivate = false,
        string? linkedEntityType = null,
        Guid? linkedEntityId = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating agenda item: {title} for meeting {meetingId}");

            var item = new MeetingAgendaItem
            {
                Id = Guid.NewGuid(),
                OrganizationId = profile.OrganizationId ?? Guid.Empty,
                MeetingId = meetingId,
                AddedBy = profile.Id,
                Title = title,
                Description = description,
                Status = "open",
                SortOrder = sortOrder,
                IsPrivate = isPrivate,
                IsCompleted = false,
                LinkedEntityType = linkedEntityType,
                LinkedEntityId = linkedEntityId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<MeetingAgendaItem>()
                .Insert(item);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Agenda item created: {created.Id}");
            }

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateAgendaItem ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates the status of an agenda item.
    /// </summary>
    public async Task<bool> UpdateStatusAsync(Guid itemId, string newStatus)
    {
        LastError = null;

        if (!ValidStatuses.Contains(newStatus.ToLower()))
        {
            LastError = $"Invalid status: {newStatus}. Valid values: {string.Join(", ", ValidStatuses)}";
            return false;
        }

        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating agenda item status: {itemId} -> {newStatus}");

            var item = await GetAgendaItemAsync(itemId);
            if (item == null)
            {
                LastError = "Agenda item not found";
                return false;
            }

            item.Status = newStatus.ToLower();
            item.UpdatedAt = DateTime.UtcNow;

            // If status is action_created and item is not completed, mark it
            if (newStatus.ToLower() == "action_created" && !item.IsCompleted)
            {
                item.IsCompleted = true;
                item.CompletedAt = DateTime.UtcNow;
            }

            var result = await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, itemId.ToString())
                .Update(item);

            var success = result.Models?.Count > 0;
            Log($"Agenda item status updated: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateStatus ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Marks an agenda item as completed/discussed.
    /// </summary>
    public async Task<bool> MarkCompletedAsync(Guid itemId, bool completed = true)
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
            Log($"Marking agenda item {(completed ? "completed" : "incomplete")}: {itemId}");

            var item = await GetAgendaItemAsync(itemId);
            if (item == null)
            {
                LastError = "Agenda item not found";
                return false;
            }

            item.IsCompleted = completed;
            item.CompletedAt = completed ? DateTime.UtcNow : null;
            item.Status = completed ? "discussed" : "open";
            item.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, itemId.ToString())
                .Update(item);

            var success = result.Models?.Count > 0;
            Log($"Agenda item marked {(completed ? "completed" : "incomplete")}: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"MarkCompleted ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a task from an agenda item and updates the item's status.
    /// This is the main workflow for converting agenda items to tasks.
    /// </summary>
    public async Task<TaskDetail?> CreateTaskFromAgendaItemAsync(
        Guid agendaItemId,
        string? description = null,
        string? priority = null,
        DateTime? dueDate = null,
        Guid? assignedTo = null)
    {
        LastError = null;

        // Get the agenda item
        var item = await GetAgendaItemAsync(agendaItemId);
        if (item == null)
        {
            LastError = "Agenda item not found";
            return null;
        }

        Log($"Creating task from agenda item: {item.Title}");

        // Create the task with provenance
        var task = await TaskService.Instance.CreateTaskFromAgendaItemAsync(
            item,
            description,
            priority,
            dueDate,
            assignedTo
        );

        if (task == null)
        {
            LastError = TaskService.Instance.LastError ?? "Failed to create task";
            return null;
        }

        // Update agenda item status and link
        item.Status = "action_created";
        item.IsCompleted = true;
        item.CompletedAt = DateTime.UtcNow;
        item.LinkedEntityType = "task";
        item.LinkedEntityId = task.Id;
        item.UpdatedAt = DateTime.UtcNow;

        var client = AuthService.Instance.GetProCohereClient();
        if (client != null)
        {
            try
            {
                await client.From<MeetingAgendaItem>()
                    .Filter("id", Operator.Equals, agendaItemId.ToString())
                    .Update(item);
                
                Log($"Agenda item updated with linked task: {task.Id}");
            }
            catch (Exception ex)
            {
                Log($"Warning: Failed to update agenda item link: {ex.Message}");
                // Don't fail the whole operation - task was created successfully
            }
        }

        return task;
    }

    /// <summary>
    /// Updates an existing agenda item.
    /// </summary>
    public async Task<MeetingAgendaItem?> UpdateAgendaItemAsync(MeetingAgendaItem item)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating agenda item: {item.Id}");

            item.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, item.Id.ToString())
                .Update(item);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Agenda item updated: {updated.Id}");
            }

            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateAgendaItem ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Soft deletes an agenda item.
    /// </summary>
    public async Task<bool> DeleteAgendaItemAsync(Guid itemId)
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
            Log($"Deleting agenda item: {itemId}");

            var item = await GetAgendaItemAsync(itemId);
            if (item == null)
            {
                LastError = "Agenda item not found";
                return false;
            }

            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, itemId.ToString())
                .Update(item);

            var success = result.Models?.Count > 0;
            Log($"Agenda item deleted: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteAgendaItem ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reorders agenda items within a meeting.
    /// </summary>
    public async Task<bool> ReorderAgendaItemsAsync(Guid meetingId, List<Guid> itemIdsInOrder)
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
            Log($"Reordering {itemIdsInOrder.Count} agenda items for meeting: {meetingId}");

            for (int i = 0; i < itemIdsInOrder.Count; i++)
            {
                var item = await GetAgendaItemAsync(itemIdsInOrder[i]);
                if (item != null && item.MeetingId == meetingId)
                {
                    item.SortOrder = i;
                    item.UpdatedAt = DateTime.UtcNow;

                    await client.From<MeetingAgendaItem>()
                        .Filter("id", Operator.Equals, item.Id.ToString())
                        .Update(item);
                }
            }

            Log("Agenda items reordered successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ReorderAgendaItems ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Links an agenda item to an entity (task, goal, metric, etc.).
    /// </summary>
    public async Task<bool> LinkToEntityAsync(Guid agendaItemId, string entityType, Guid entityId)
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
            Log($"Linking agenda item {agendaItemId} to {entityType}:{entityId}");

            await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, agendaItemId.ToString())
                .Set(x => x.LinkedEntityType!, entityType)
                .Set(x => x.LinkedEntityId!, entityId)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log($"Agenda item linked to {entityType} successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"LinkToEntity ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes the linked entity from an agenda item.
    /// </summary>
    public async Task<bool> UnlinkEntityAsync(Guid agendaItemId)
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
            Log($"Unlinking entity from agenda item: {agendaItemId}");

            // Get the item first
            var item = await GetAgendaItemAsync(agendaItemId);
            if (item == null)
            {
                LastError = "Agenda item not found";
                return false;
            }

            item.LinkedEntityType = null;
            item.LinkedEntityId = null;
            item.UpdatedAt = DateTime.UtcNow;

            await client.From<MeetingAgendaItem>()
                .Filter("id", Operator.Equals, agendaItemId.ToString())
                .Update(item);

            Log("Entity unlinked from agenda item successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UnlinkEntity ERROR: {ex.Message}");
            return false;
        }
    }
}
