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
    /// Creates a new agenda item for a meeting using the procohere.insert_meeting_agenda_item RPC.
    /// The RPC returns the new UUID and handles organization_id, added_by internally.
    /// </summary>
    public async Task<MeetingAgendaItem?> CreateAgendaItemAsync(
        Guid meetingId,
        string title,
        string? description = null,
        int sortOrder = 0,
        bool isPrivate = false,
        string? linkedEntityType = null,
        Guid? linkedEntityId = null,
        string? displayTitle = null,
        string? sharedContext = null,
        string? privateContext = null,
        string visibilityScope = "meeting",
        string? linkedEntityTitleSnapshot = null,
        List<TalkingPoint>? talkingPoints = null)
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

            // Align is_private with visibility_scope per constraint
            var isPrivateAligned = visibilityScope == "personal";
            var session = AuthService.Instance.CurrentSession_ProCohere;
            var orgId = session?.TeamMember?.OrganizationId ?? Guid.Empty;
            var addedBy = session?.TeamMember?.Id ?? Guid.Empty;

            // Serialize talking points to JSON array if provided
            string? talkingPointsJson = null;
            if (talkingPoints != null && talkingPoints.Count > 0)
            {
                talkingPointsJson = System.Text.Json.JsonSerializer.Serialize(talkingPoints);
            }

            // Use RPC to insert - it returns the new UUID
            // RPC signature: procohere.insert_meeting_agenda_item(
            //   p_meeting_id, p_title, p_description, p_display_title, p_status, p_sort_order,
            //   p_is_private, p_visibility_scope, p_shared_context, p_private_context,
            //   p_talking_points, p_linked_entity_type, p_linked_entity_id, p_linked_entity_title_snapshot)
            var rpcResult = await client.Rpc("insert_meeting_agenda_item", new
            {
                p_meeting_id = meetingId,
                p_title = title,
                p_description = description,
                p_display_title = displayTitle,
                p_status = "open",
                p_sort_order = sortOrder,
                p_is_private = isPrivateAligned,
                p_visibility_scope = visibilityScope,
                p_shared_context = sharedContext,
                p_private_context = privateContext,
                p_talking_points = talkingPointsJson,
                p_linked_entity_type = linkedEntityType,
                p_linked_entity_id = linkedEntityId,
                p_linked_entity_title_snapshot = linkedEntityTitleSnapshot
            });

            Log($"Insert agenda item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"CreateAgendaItem ERROR: {LastError}");
                return null;
            }

            // Parse the returned UUID from the RPC result
            var newId = ParseUuidFromRpcResult(rpcResult?.Content);
            if (newId == Guid.Empty)
            {
                LastError = "Failed to parse UUID from RPC result";
                Log($"CreateAgendaItem ERROR: {LastError}");
                return null;
            }

            // Return a populated item with the database-assigned ID
            var created = new MeetingAgendaItem
            {
                Id = newId,
                OrganizationId = orgId,
                MeetingId = meetingId,
                AddedBy = addedBy,
                Title = title,
                DisplayTitle = displayTitle,
                Description = description,
                SharedContext = sharedContext,
                PrivateContext = privateContext,
                VisibilityScope = visibilityScope,
                Status = "open",
                SortOrder = sortOrder,
                IsPrivate = isPrivateAligned,
                IsCompleted = false,
                LinkedEntityType = linkedEntityType,
                LinkedEntityId = linkedEntityId,
                LinkedEntityTitleSnapshot = linkedEntityTitleSnapshot,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TalkingPoints = talkingPoints ?? new List<TalkingPoint>()
            };

            Log($"Agenda item created: {created.Id}");
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
    /// Parses a UUID from the RPC result content.
    /// Expected format: "uuid-value" or just the raw UUID string.
    /// </summary>
    private Guid ParseUuidFromRpcResult(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Guid.Empty;

        // Remove quotes if present
        var cleaned = content.Trim().Trim('"');
        
        if (Guid.TryParse(cleaned, out var guid))
            return guid;

        return Guid.Empty;
    }

    /// <summary>
    /// Updates the status of an agenda item using the update RPC.
    /// Only the owner (added_by) can update status.
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

            // Use RPC to update status
            var rpcResult = await client.Rpc("update_meeting_agenda_item", new
            {
                p_id = itemId,
                p_status = newStatus.ToLower()
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateStatus ERROR: {LastError}");
                return false;
            }

            Log($"Agenda item status updated: {itemId}");
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
    /// Marks an agenda item as completed/discussed using the update RPC.
    /// Only the owner (added_by) can mark completed.
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

            var newStatus = completed ? "discussed" : "open";

            // Use RPC to update completed status
            var rpcResult = await client.Rpc("update_meeting_agenda_item", new
            {
                p_id = itemId,
                p_status = newStatus,
                p_is_completed = completed
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"MarkCompleted ERROR: {LastError}");
                return false;
            }

            Log($"Agenda item marked {(completed ? "completed" : "incomplete")}: {itemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"MarkCompleted ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates an agenda item with all fields using the procohere.update_meeting_agenda_item RPC.
    /// Only the owner (added_by) can update an agenda item.
    /// </summary>
    public async Task<bool> UpdateAgendaItemAsync(
        Guid itemId,
        string? title = null,
        string? displayTitle = null,
        string? sharedContext = null,
        string? privateContext = null,
        string? visibilityScope = null,
        List<TalkingPoint>? talkingPoints = null,
        string? outcomeType = null,
        string? outcomeSummary = null)
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
            Log($"Updating agenda item: {itemId}");

            // Serialize talking points to JSON if provided
            string? talkingPointsJson = null;
            if (talkingPoints != null)
            {
                talkingPointsJson = System.Text.Json.JsonSerializer.Serialize(talkingPoints);
            }

            // Use RPC to update - only owner can update
            // RPC handles visibility_scope → is_private sync internally
            var rpcResult = await client.Rpc("update_meeting_agenda_item", new
            {
                p_id = itemId,
                p_title = title,
                p_display_title = displayTitle,
                p_shared_context = sharedContext,
                p_private_context = privateContext,
                p_talking_points = talkingPointsJson,
                p_outcome_type = outcomeType,
                p_outcome_summary = outcomeSummary,
                p_visibility_scope = visibilityScope
            });

            Log($"Update agenda item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateAgendaItem ERROR: {LastError}");
                return false;
            }

            Log($"Agenda item updated: {itemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateAgendaItem ERROR: {ex.Message}");
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

        // Update agenda item status and create link using separate RPCs
        var client = AuthService.Instance.GetProCohereClient();
        if (client != null)
        {
            try
            {
                // First update the agenda item status
                var statusResult = await client.Rpc("update_meeting_agenda_item", new
                {
                    p_id = agendaItemId,
                    p_status = "action_created",
                    p_is_completed = true
                });
                
                if (statusResult?.Content?.Contains("error") == true)
                {
                    Log($"Warning: Failed to update agenda item status: {statusResult.Content}");
                }

                // Then create the link using dedicated link RPC
                // RPC signature: upsert_meeting_agenda_item_reference_link(
                //   p_meeting_agenda_item_id, p_entity_type, p_entity_id, p_entity_title_snapshot)
                var linkResult = await client.Rpc("upsert_meeting_agenda_item_reference_link", new
                {
                    p_meeting_agenda_item_id = agendaItemId,
                    p_entity_type = "task",
                    p_entity_id = task.Id,
                    p_entity_title_snapshot = task.Title
                });
                
                if (linkResult?.Content?.Contains("error") == true)
                {
                    Log($"Warning: Failed to link agenda item to task: {linkResult.Content}");
                }
                else
                {
                    Log($"Agenda item updated with linked task: {task.Id}");
                }
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
    /// Updates an existing agenda item using the procohere.update_meeting_agenda_item RPC.
    /// Only the owner (added_by) can update an agenda item.
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

            // Serialize talking points to JSON if provided
            string? talkingPointsJson = null;
            if (item.TalkingPoints != null && item.TalkingPoints.Count > 0)
            {
                talkingPointsJson = System.Text.Json.JsonSerializer.Serialize(item.TalkingPoints);
            }

            // Use RPC to update - only owner can update
            // RPC signature: procohere.update_meeting_agenda_item(
            //   p_id, p_title, p_description, p_display_title, p_status, p_is_completed,
            //   p_shared_context, p_private_context, p_talking_points,
            //   p_outcome_type, p_outcome_summary, p_sort_order)
            var rpcResult = await client.Rpc("update_meeting_agenda_item", new
            {
                p_id = item.Id,
                p_title = item.Title,
                p_description = item.Description,
                p_display_title = item.DisplayTitle,
                p_status = item.Status,
                p_is_completed = item.IsCompleted,
                p_shared_context = item.SharedContext,
                p_private_context = item.PrivateContext,
                p_talking_points = talkingPointsJson,
                p_outcome_type = item.OutcomeType,
                p_outcome_summary = item.OutcomeSummary,
                p_sort_order = item.SortOrder
            });

            Log($"Update agenda item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateAgendaItem ERROR: {LastError}");
                return null;
            }

            item.UpdatedAt = DateTime.UtcNow;
            Log($"Agenda item updated: {item.Id}");
            return item;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateAgendaItem ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Soft deletes an agenda item using the procohere.delete_meeting_agenda_item RPC.
    /// Only the owner (added_by) can delete an agenda item.
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

            // Use RPC to delete - only owner can delete
            // RPC signature: procohere.delete_meeting_agenda_item(p_id)
            var rpcResult = await client.Rpc("delete_meeting_agenda_item", new
            {
                p_id = itemId
            });

            Log($"Delete agenda item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"DeleteAgendaItem ERROR: {LastError}");
                return false;
            }

            Log($"Agenda item deleted: {itemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteAgendaItem ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reorders agenda items within a meeting using the update RPC.
    /// Only the owner of each item can update its sort_order.
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
                // Use RPC to update sort_order
                var rpcResult = await client.Rpc("update_meeting_agenda_item", new
                {
                    p_id = itemIdsInOrder[i],
                    p_sort_order = i
                });

                if (rpcResult?.Content?.Contains("error") == true)
                {
                    Log($"Warning: Failed to update sort order for item {itemIdsInOrder[i]}: {rpcResult.Content}");
                    // Continue with other items - partial reorder is better than none
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
    /// Links an agenda item to an entity (task, goal, metric, project) using upsert_meeting_agenda_item_reference_link RPC.
    /// Creates or replaces the reference link for the agenda item.
    /// Also updates meeting_agenda_items.linked_entity_title_snapshot to stay in sync.
    /// </summary>
    /// <param name="agendaItemId">The agenda item to link from</param>
    /// <param name="entityType">Entity type: 'task', 'goal', 'metric', 'project'</param>
    /// <param name="entityId">The ID of the entity to link to</param>
    /// <param name="entityTitle">Optional title snapshot for display</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> LinkToEntityAsync(Guid agendaItemId, string entityType, Guid entityId, string? entityTitle = null)
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

            // Use dedicated link RPC - handles upsert and keeps snapshot in sync
            // RPC signature: upsert_meeting_agenda_item_reference_link(
            //   p_meeting_agenda_item_id, p_entity_type, p_entity_id, p_entity_title_snapshot)
            var rpcResult = await client.Rpc("upsert_meeting_agenda_item_reference_link", new
            {
                p_meeting_agenda_item_id = agendaItemId,
                p_entity_type = entityType,
                p_entity_id = entityId,
                p_entity_title_snapshot = entityTitle
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"LinkToEntity ERROR: {LastError}");
                return false;
            }

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
    /// Removes the reference link from an agenda item using delete_meeting_agenda_item_reference_link RPC.
    /// Also clears meeting_agenda_items.linked_entity_title_snapshot to stay in sync.
    /// </summary>
    /// <param name="agendaItemId">The agenda item to unlink</param>
    /// <returns>True if successful (even if no link existed), false on error</returns>
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

            // Use dedicated unlink RPC - handles deletion and clears snapshot
            // RPC signature: delete_meeting_agenda_item_reference_link(p_agenda_item_id)
            var rpcResult = await client.Rpc("delete_meeting_agenda_item_reference_link", new
            {
                p_agenda_item_id = agendaItemId
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UnlinkEntity ERROR: {LastError}");
                return false;
            }

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
