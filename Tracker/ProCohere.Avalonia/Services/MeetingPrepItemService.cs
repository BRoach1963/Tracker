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
    /// Creates a new prep item using the procohere.insert_meeting_prep_item RPC.
    /// The RPC returns the new UUID and handles organization_id, requested_by internally.
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

            // Set defaults for local model
            item.Status = item.Status ?? "open";
            item.VisibilityScope = item.VisibilityScope ?? "personal";
            item.SourceType = item.SourceType ?? "manual";
            item.IsDeleted = false;
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            // Use RPC to insert - it returns the new UUID
            // RPC signature: procohere.insert_meeting_prep_item(
            //   p_meeting_id, p_title, p_body, p_visibility_scope, p_assigned_to_team_member_id,
            //   p_status, p_sort_order, p_carry_forward, p_carried_from_prep_item_id, p_source_type,
            //   p_linked_entity_type, p_linked_entity_id, p_linked_entity_title_snapshot,
            //   p_due_at, p_prep_prompt, p_prep_response)
            var rpcResult = await client.Rpc("insert_meeting_prep_item", new
            {
                p_meeting_id = item.MeetingId,
                p_title = item.Title,
                p_body = item.Body,
                p_visibility_scope = item.VisibilityScope,
                p_assigned_to_team_member_id = item.AssignedToTeamMemberId,
                p_status = item.Status,
                p_sort_order = item.SortOrder,
                p_carry_forward = item.CarryForward,
                p_carried_from_prep_item_id = item.CarriedFromPrepItemId,
                p_source_type = item.SourceType,
                p_linked_entity_type = item.LinkedEntityType,
                p_linked_entity_id = item.LinkedEntityId,
                p_linked_entity_title_snapshot = item.LinkedEntityTitleSnapshot,
                p_due_at = item.DueAt,
                p_prep_prompt = item.PrepPrompt,
                p_prep_response = item.PrepResponse
            });

            Log($"Insert prep item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"CreatePrepItem ERROR: {LastError}");
                return null;
            }

            // Parse the returned UUID from the RPC result
            var newId = ParseUuidFromRpcResult(rpcResult?.Content);
            if (newId == Guid.Empty)
            {
                LastError = "Failed to parse UUID from RPC result";
                Log($"CreatePrepItem ERROR: {LastError}");
                return null;
            }

            // Update local model with database-assigned values
            item.Id = newId;
            item.OrganizationId = orgId;
            item.RequestedByTeamMemberId = creatorId;
            item.CurrentUserTeamMemberId = creatorId;
            
            Log($"Prep item created: {item.Id}");
            return item;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreatePrepItem ERROR: {ex.Message}");
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
    /// Updates an existing prep item using the appropriate RPC based on user's role.
    /// Uses update_meeting_prep_item_as_requester if current user is the requester,
    /// or update_meeting_prep_item_as_assignee if current user is the assignee.
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
            var currentUserId = session.TeamMember.Id;
            Log($"Updating prep item: {item.Id} (requester: {item.RequestedByTeamMemberId}, assignee: {item.AssignedToTeamMemberId}, current: {currentUserId})");

            item.UpdatedAt = DateTime.UtcNow;

            // Determine which RPC to use based on current user's role
            if (item.RequestedByTeamMemberId == currentUserId)
            {
                // Current user is the requester - can update requester fields
                // RPC signature: procohere.update_meeting_prep_item_as_requester(
                //   p_id, p_title, p_body, p_visibility_scope, p_assigned_to_team_member_id,
                //   p_status, p_sort_order, p_due_at, p_prep_prompt)
                var rpcResult = await client.Rpc("update_meeting_prep_item_as_requester", new
                {
                    p_id = item.Id,
                    p_title = item.Title,
                    p_body = item.Body,
                    p_visibility_scope = item.VisibilityScope,
                    p_assigned_to_team_member_id = item.AssignedToTeamMemberId,
                    p_status = item.Status,
                    p_sort_order = item.SortOrder,
                    p_due_at = item.DueAt,
                    p_prep_prompt = item.PrepPrompt
                });

                Log($"Update prep item (as requester) RPC result: {rpcResult?.Content ?? "NULL"}");

                if (rpcResult?.Content?.Contains("error") == true)
                {
                    LastError = rpcResult.Content;
                    Log($"UpdatePrepItem ERROR: {LastError}");
                    return false;
                }
            }
            else if (item.AssignedToTeamMemberId == currentUserId)
            {
                // Current user is the assignee - can update assignee fields
                // RPC signature: procohere.update_meeting_prep_item_as_assignee(
                //   p_id, p_assignee_notes, p_status, p_prep_response)
                var rpcResult = await client.Rpc("update_meeting_prep_item_as_assignee", new
                {
                    p_id = item.Id,
                    p_assignee_notes = item.AssigneeNotes,
                    p_status = item.Status,
                    p_prep_response = item.PrepResponse
                });

                Log($"Update prep item (as assignee) RPC result: {rpcResult?.Content ?? "NULL"}");

                if (rpcResult?.Content?.Contains("error") == true)
                {
                    LastError = rpcResult.Content;
                    Log($"UpdatePrepItem ERROR: {LastError}");
                    return false;
                }
            }
            else
            {
                LastError = "Current user is neither the requester nor the assignee of this prep item";
                Log($"UpdatePrepItem ERROR: {LastError}");
                return false;
            }

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
    /// Updates an existing prep item as the requester (owner).
    /// Use this when you know the current user is the requester.
    /// </summary>
    public async Task<bool> UpdatePrepItemAsRequesterAsync(MeetingPrepItem item)
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
            Log($"Updating prep item as requester: {item.Id}");

            var rpcResult = await client.Rpc("update_meeting_prep_item_as_requester", new
            {
                p_id = item.Id,
                p_title = item.Title,
                p_body = item.Body,
                p_visibility_scope = item.VisibilityScope,
                p_assigned_to_team_member_id = item.AssignedToTeamMemberId,
                p_status = item.Status,
                p_sort_order = item.SortOrder,
                p_due_at = item.DueAt,
                p_prep_prompt = item.PrepPrompt
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdatePrepItemAsRequester ERROR: {LastError}");
                return false;
            }

            Log($"Prep item updated as requester: {item.Id}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdatePrepItemAsRequester ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates an existing prep item as the assignee.
    /// Use this when you know the current user is the assignee.
    /// Assignees can only update the prep_response field.
    /// </summary>
    public async Task<bool> UpdatePrepItemAsAssigneeAsync(Guid prepItemId, string? prepResponse)
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
            Log($"Updating prep item as assignee: {prepItemId}");

            var rpcResult = await client.Rpc("update_meeting_prep_item_as_assignee", new
            {
                p_id = prepItemId,
                p_prep_response = prepResponse
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdatePrepItemAsAssignee ERROR: {LastError}");
                return false;
            }

            Log($"Prep item updated as assignee: {prepItemId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdatePrepItemAsAssignee ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the status of a prep item using the requester RPC.
    /// Note: Only the requester can change status.
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
            Log($"Updating prep item status: {prepItemId} -> {newStatus}");

            // Use the requester RPC with just the status field
            var rpcResult = await client.Rpc("update_meeting_prep_item_as_requester", new
            {
                p_id = prepItemId,
                p_status = newStatus
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateStatus ERROR: {LastError}");
                return false;
            }

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
    /// Soft deletes a prep item using the procohere.delete_meeting_prep_item RPC.
    /// Only the requester (requested_by) can delete a prep item.
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
            Log($"Deleting prep item: {prepItemId}");

            // Use RPC to delete - only requester can delete
            // RPC signature: procohere.delete_meeting_prep_item(p_id)
            var rpcResult = await client.Rpc("delete_meeting_prep_item", new
            {
                p_id = prepItemId
            });

            Log($"Delete prep item RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"DeletePrepItem ERROR: {LastError}");
                return false;
            }

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
    /// Per constraint: personal visibility requires assigned_to_team_member_id (self-assigned).
    /// </summary>
    public async Task<MeetingPrepItem?> CreateQuickPrepAsync(Guid meetingId, string title)
    {
        var session = AuthService.Instance.CurrentSession_ProCohere;
        var currentUserId = session?.TeamMember?.Id;
        
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = title,
            VisibilityScope = "personal",
            AssignedToTeamMemberId = currentUserId, // Personal items are self-assigned per DB constraint
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
    /// Per constraint: personal/assigned visibility requires assigned_to_team_member_id.
    /// </summary>
    public async Task<MeetingPrepItem?> CreateLinkedPrepAsync(
        Guid meetingId,
        string linkedEntityType,
        Guid linkedEntityId,
        string linkedEntityTitle,
        string? prepPrompt = null,
        string visibilityScope = "personal",
        Guid? assigneeId = null)
    {
        var session = AuthService.Instance.CurrentSession_ProCohere;
        var currentUserId = session?.TeamMember?.Id;
        
        // Per DB constraint: personal/assigned require assignee, meeting does not
        Guid? effectiveAssigneeId = visibilityScope switch
        {
            "meeting" => null,
            "assigned" => assigneeId ?? currentUserId, // Use provided or default to self
            _ => currentUserId // "personal" = self-assigned
        };
        
        var item = new MeetingPrepItem
        {
            MeetingId = meetingId,
            Title = linkedEntityTitle,
            LinkedEntityType = linkedEntityType,
            LinkedEntityId = linkedEntityId,
            LinkedEntityTitleSnapshot = linkedEntityTitle,
            PrepPrompt = prepPrompt,
            VisibilityScope = visibilityScope,
            AssignedToTeamMemberId = effectiveAssigneeId,
            Status = "open",
            SourceType = "manual"
        };

        return await CreatePrepItemAsync(item);
    }

    /// <summary>
    /// Captures the preparation response for a prep item using the assignee RPC.
    /// This is typically used by the assignee to provide their response.
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

            // Use the assignee RPC to update the response
            var rpcResult = await client.Rpc("update_meeting_prep_item_as_assignee", new
            {
                p_id = prepItemId,
                p_prep_response = response
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"CapturePrepResponse ERROR: {LastError}");
                return false;
            }

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
    /// Updates the prep prompt for a prep item using the requester RPC.
    /// Only the requester can update the prep prompt.
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

            // Use the requester RPC to update the prompt
            var rpcResult = await client.Rpc("update_meeting_prep_item_as_requester", new
            {
                p_id = prepItemId,
                p_prep_prompt = prompt
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdatePrepPrompt ERROR: {LastError}");
                return false;
            }

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
    /// Links an existing entity to a prep item using the insert_meeting_prep_item_link RPC.
    /// Note: The update RPC doesn't support linked entity fields, so we use the link table directly.
    /// </summary>
    public async Task<bool> LinkEntityAsync(
        Guid prepItemId,
        string entityType,
        Guid entityId,
        string entityTitle)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
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
            var orgId = session.TeamMember.OrganizationId;
            Log($"Linking entity {entityType}:{entityId} to prep item: {prepItemId}");

            // Use the link RPC to create the association
            // RPC signature: insert_meeting_prep_item_link(p_id, p_organization_id, p_meeting_prep_item_id, p_link_kind, p_entity_type, p_entity_id)
            var rpcResult = await client.Rpc("insert_meeting_prep_item_link", new
            {
                p_id = Guid.NewGuid(),
                p_organization_id = orgId,
                p_meeting_prep_item_id = prepItemId,
                p_link_kind = "reference",
                p_entity_type = entityType,
                p_entity_id = entityId
            });

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"LinkEntity ERROR: {LastError}");
                return false;
            }

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
    /// Note: This requires direct table access or a dedicated delete RPC for prep item links.
    /// </summary>
    public async Task<bool> UnlinkEntityAsync(Guid prepItemId)
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
            Log($"Unlinking entity from prep item: {prepItemId}");

            // Delete all reference links for this prep item
            // Note: This uses direct table access - may need RPC if RLS blocks it
            await client.From<MeetingPrepItemLink>()
                .Filter("meeting_prep_item_id", Operator.Equals, prepItemId.ToString())
                .Filter("link_kind", Operator.Equals, "reference")
                .Delete();

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
                // Use RPC to insert - it returns the new UUID
                // RPC handles organization_id and requested_by_team_member_id internally
                var rpcResult = await client.Rpc("insert_meeting_prep_item", new
                {
                    p_meeting_id = toMeetingId,
                    p_title = source.Title,
                    p_body = source.Body,
                    p_visibility_scope = source.VisibilityScope ?? "personal",
                    p_assigned_to_team_member_id = source.AssignedToTeamMemberId,
                    p_status = "open",
                    p_sort_order = source.SortOrder,
                    p_carry_forward = source.CarryForward,
                    p_carried_from_prep_item_id = source.Id,
                    p_source_type = "carry_forward",
                    p_linked_entity_type = source.LinkedEntityType,
                    p_linked_entity_id = source.LinkedEntityId,
                    p_linked_entity_title_snapshot = source.LinkedEntityTitleSnapshot,
                    p_due_at = source.DueAt,
                    p_prep_prompt = source.PrepPrompt,
                    p_prep_response = (string?)null // Don't carry over response
                });

                if (rpcResult?.Content?.Contains("error") == true)
                {
                    Log($"CarryForward prep item failed: {rpcResult.Content}");
                    continue;
                }

                // Parse the returned UUID from the RPC result
                var newId = ParseUuidFromRpcResult(rpcResult?.Content);
                if (newId == Guid.Empty)
                {
                    Log($"CarryForward prep item failed: could not parse UUID");
                    continue;
                }

                // Build returned item
                var created = new MeetingPrepItem
                {
                    Id = newId,
                    OrganizationId = orgId,
                    MeetingId = toMeetingId,
                    RequestedByTeamMemberId = session.TeamMember.Id, // RPC sets this to current user
                    AssignedToTeamMemberId = source.AssignedToTeamMemberId,
                    Title = source.Title,
                    Body = source.Body,
                    VisibilityScope = source.VisibilityScope,
                    Status = "open",
                    SortOrder = source.SortOrder,
                    CarryForward = source.CarryForward,
                    CarriedFromPrepItemId = source.Id,
                    SourceType = "carry_forward",
                    LinkedEntityType = source.LinkedEntityType,
                    LinkedEntityId = source.LinkedEntityId,
                    LinkedEntityTitleSnapshot = source.LinkedEntityTitleSnapshot,
                    DueAt = source.DueAt,
                    PrepPrompt = source.PrepPrompt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                createdItems.Add(created);
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
