using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

// Reminder integration - Phase 5

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing tasks in Supabase.
/// Handles CRUD operations and task completion.
/// </summary>
public class TaskService
{
    #region Singleton

    private static readonly Lazy<TaskService> _instance =
        new(() => new TaskService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static TaskService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "task_service.log");

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

    private TaskService() { }

    /// <summary>
    /// Gets a single task by ID.
    /// </summary>
    public async Task<TaskDetail?> GetTaskAsync(Guid taskId)
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
            Log($"Getting task: {taskId}");
            var result = await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, taskId.ToString())
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTask ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all tasks for the organization (filtered by RLS).
    /// </summary>
    public async Task<List<TaskDetail>> GetTasksAsync(bool includeCompleted = false)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log($"Loading tasks (includeCompleted: {includeCompleted})");
            
            var query = client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false");

            if (!includeCompleted)
            {
                query = query.Filter("status", Operator.NotEqual, "completed");
            }

            var result = await query
                .Order("due_date", Ordering.Ascending)
                .Get();

            Log($"Tasks returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTasks ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Gets incomplete tasks that are not linked to any project.
    /// Useful for linking existing free tasks to a new project.
    /// </summary>
    public async Task<List<TaskDetail>> GetLinkableTasksAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log("Loading linkable tasks (unlinked, incomplete)");
            
            var result = await client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("status", Operator.NotEqual, "completed")
                .Filter("project_id", Operator.Is, "null")
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Linkable tasks returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetLinkableTasks ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Creates a minimal task with just a title.
    /// Used for title-only bootstrapping during project creation.
    /// </summary>
    public async Task<TaskDetail?> CreateMinimalTaskAsync(string title, Guid? projectId = null)
    {
        return await CreateTaskAsync(
            title: title,
            description: null,
            priority: "medium",
            dueDate: null,
            assignedTo: null,
            sourceType: projectId.HasValue ? "project" : null,
            sourceId: projectId
        );
    }

    /// <summary>
    /// Gets all tasks linked to a specific project.
    /// </summary>
    public async Task<List<TaskDetail>> GetTasksByProjectAsync(Guid projectId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log($"Loading tasks for project: {projectId}");
            
            var result = await client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("project_id", Operator.Equals, projectId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Tasks for project returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTasksByProject ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Gets tasks assigned to a specific team member.
    /// </summary>
    public async Task<List<TaskDetail>> GetTasksByAssigneeAsync(Guid teamMemberId, bool includeCompleted = false)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log($"Loading tasks for assignee: {teamMemberId}");
            
            var query = client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("assigned_to", Operator.Equals, teamMemberId.ToString());

            if (!includeCompleted)
            {
                query = query.Filter("status", Operator.NotEqual, "completed");
            }

            var result = await query
                .Order("due_date", Ordering.Ascending)
                .Get();

            Log($"Tasks for assignee returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTasksByAssignee ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Gets tasks created from a specific source (e.g., agenda item).
    /// </summary>
    public async Task<List<TaskDetail>> GetTasksBySourceAsync(string sourceType, Guid sourceId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log($"Loading tasks by source: {sourceType}/{sourceId}");
            
            var result = await client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("source_type", Operator.Equals, sourceType)
                .Filter("source_id", Operator.Equals, sourceId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Tasks by source returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<TaskDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetTasksBySource ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Gets follow-up tasks for a meeting: tasks sourced from the meeting itself 
    /// or from any of its agenda items.
    /// </summary>
    /// <param name="meetingId">The meeting ID.</param>
    /// <param name="agendaItemIds">IDs of agenda items belonging to this meeting.</param>
    /// <param name="includeCompleted">Whether to include completed tasks.</param>
    /// <returns>List of follow-up tasks for the meeting.</returns>
    public async Task<List<TaskDetail>> GetMeetingFollowUpsAsync(
        Guid meetingId, 
        IEnumerable<Guid> agendaItemIds,
        bool includeCompleted = true)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TaskDetail>();
        }

        try
        {
            Log($"Loading follow-ups for meeting: {meetingId}");
            
            var allTasks = new List<TaskDetail>();

            // 1. Get tasks sourced directly from the meeting
            var meetingTasks = await client.From<TaskDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("source_type", Operator.Equals, "meeting")
                .Filter("source_id", Operator.Equals, meetingId.ToString())
                .Get();

            if (meetingTasks.Models != null)
            {
                allTasks.AddRange(meetingTasks.Models);
            }

            // 2. Get tasks sourced from each agenda item
            foreach (var agendaItemId in agendaItemIds)
            {
                var agendaTasks = await client.From<TaskDetail>()
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Filter("source_type", Operator.Equals, "agenda_item")
                    .Filter("source_id", Operator.Equals, agendaItemId.ToString())
                    .Get();

                if (agendaTasks.Models != null)
                {
                    allTasks.AddRange(agendaTasks.Models);
                }
            }

            // Filter completed if needed
            if (!includeCompleted)
            {
                allTasks = allTasks.Where(t => t.Status != "completed").ToList();
            }

            // Order by created_at descending, then by due_date
            allTasks = allTasks
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.DueDate)
                .ToList();

            Log($"Meeting follow-ups returned: {allTasks.Count}");
            return allTasks;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetMeetingFollowUps ERROR: {ex.Message}");
            return new List<TaskDetail>();
        }
    }

    /// <summary>
    /// Creates a follow-up task from a meeting.
    /// </summary>
    public async Task<TaskDetail?> CreateMeetingFollowUpAsync(
        Guid meetingId,
        string title,
        string? description = null,
        string? priority = "medium",
        DateTime? dueDate = null,
        Guid? assignedTo = null)
    {
        Log($"Creating follow-up task from meeting: {meetingId} - {title}");

        return await CreateTaskAsync(
            title: title,
            description: description,
            priority: priority,
            dueDate: dueDate,
            assignedTo: assignedTo,
            sourceType: "meeting",
            sourceId: meetingId
        );
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    public async Task<TaskDetail?> CreateTaskAsync(
        string title,
        string? description = null,
        string? priority = null,
        DateTime? dueDate = null,
        Guid? assignedTo = null,
        string? sourceType = null,
        Guid? sourceId = null)
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
            Log($"Creating task: {title}");

            var task = new TaskDetail
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Status = "not_started",
                Priority = priority,
                DueDate = dueDate,
                OwnerTeamMemberId = assignedTo,
                CreatedByTeamMemberId = profile.Id,
                SourceType = sourceType,
                SourceId = sourceId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await client.From<TaskDetail>()
                .Insert(task);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Task created: {created.Id}");
                
                // Create reminder for the task if enabled
                await CreateTaskReminderIfEnabledAsync(created);
            }

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateTask ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a task from a meeting agenda item with provenance tracking.
    /// </summary>
    public async Task<TaskDetail?> CreateTaskFromAgendaItemAsync(
        MeetingAgendaItem agendaItem,
        string? description = null,
        string? priority = null,
        DateTime? dueDate = null,
        Guid? assignedTo = null)
    {
        Log($"Creating task from agenda item: {agendaItem.Id} - {agendaItem.Title}");

        var task = await CreateTaskAsync(
            title: agendaItem.Title,
            description: description ?? agendaItem.Description,
            priority: priority,
            dueDate: dueDate,
            assignedTo: assignedTo,
            sourceType: "agenda_item",
            sourceId: agendaItem.Id
        );

        return task;
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    public async Task<TaskDetail?> UpdateTaskAsync(TaskDetail task)
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
            Log($"Updating task: {task.Id}");

            var result = await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, task.Id.ToString())
                .Update(task);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Task updated: {updated.Id}");
            }

            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateTask ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    public async Task<bool> CompleteTaskAsync(Guid taskId)
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
            Log($"Completing task: {taskId}");

            // First get the task
            var task = await GetTaskAsync(taskId);
            if (task == null)
            {
                LastError = "Task not found";
                return false;
            }

            task.Status = "completed";
            task.CompletedAt = DateTime.UtcNow;

            var result = await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, taskId.ToString())
                .Update(task);

            var success = result.Models?.Count > 0;
            Log($"Task completed: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CompleteTask ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Marks a task as not completed (reopens it).
    /// </summary>
    public async Task<bool> UncompleteTaskAsync(Guid taskId)
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
            Log($"Uncompleting task: {taskId}");

            var task = await GetTaskAsync(taskId);
            if (task == null)
            {
                LastError = "Task not found";
                return false;
            }

            task.Status = "not_started";
            task.CompletedAt = null;

            var result = await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, taskId.ToString())
                .Update(task);

            var success = result.Models?.Count > 0;
            Log($"Task uncompleted: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UncompleteTask ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Soft deletes a task.
    /// </summary>
    public async Task<bool> DeleteTaskAsync(Guid taskId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var profile = AuthService.Instance.CurrentProfile;

        if (client == null || profile == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting task: {taskId}");

            var task = await GetTaskAsync(taskId);
            if (task == null)
            {
                LastError = "Task not found";
                return false;
            }

            task.IsDeleted = true;

            var result = await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, taskId.ToString())
                .Update(task);

            var success = result.Models?.Count > 0;
            
            if (success)
            {
                // Cancel any pending reminders for this task
                await CancelTaskRemindersAsync(taskId);
            }
            
            Log($"Task deleted: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteTask ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Links an existing task to a project by setting its project_id.
    /// </summary>
    public async Task<bool> LinkTaskToProjectAsync(Guid taskId, Guid projectId)
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
            Log($"Linking task {taskId} to project {projectId}");

            // Get the task first
            var task = await GetTaskAsync(taskId);
            if (task == null)
            {
                LastError = "Task not found";
                return false;
            }

            // Update project_id
            task.ProjectId = projectId;
            task.UpdatedAt = DateTime.UtcNow;

            await client.From<TaskDetail>()
                .Filter("id", Operator.Equals, taskId.ToString())
                .Update(task);

            Log($"Task {taskId} linked to project {projectId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"LinkTaskToProject ERROR: {ex.Message}");
            return false;
        }
    }

    #region Reminder Integration

    /// <summary>
    /// Creates a reminder for the task if reminders are enabled in settings.
    /// </summary>
    private async Task CreateTaskReminderIfEnabledAsync(TaskDetail? task)
    {
        if (task == null || task.DueDate == null) return;
        
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowTaskReminders)
            {
                Log("Task reminders disabled in settings");
                return;
            }
            
            // Check if reminder already exists
            var exists = await ReminderDataService.Instance.ReminderExistsAsync(
                "task", task.Id, ReminderType.Task);
            
            if (exists)
            {
                Log($"Reminder already exists for task {task.Id}");
                return;
            }
            
            var reminder = await ReminderDataService.Instance.CreateTaskReminderAsync(
                task, settings.TaskReminderDays);
            
            if (reminder != null)
            {
                Log($"Created reminder for task {task.Id}: remind at {reminder.RemindAt:u}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the task operation if reminder creation fails
            Log($"Failed to create task reminder: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels any pending reminders for a task.
    /// </summary>
    private async Task CancelTaskRemindersAsync(Guid taskId)
    {
        try
        {
            var cancelled = await ReminderDataService.Instance.CancelRemindersForEntityAsync("task", taskId);
            if (cancelled > 0)
            {
                Log($"Cancelled {cancelled} reminder(s) for deleted task {taskId}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the delete operation if reminder cancellation fails
            Log($"Failed to cancel task reminders: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the reminder for a task if the due date changed.
    /// Cancels existing reminder and creates a new one with updated time.
    /// </summary>
    public async Task UpdateTaskReminderAsync(TaskDetail task)
    {
        try
        {
            var settings = ReminderSchedulerService.Instance.Settings;
            if (!settings.EnableReminders || !settings.ShowTaskReminders)
            {
                return;
            }
            
            // Cancel existing reminder
            await ReminderDataService.Instance.CancelRemindersForEntityAsync("task", task.Id);
            
            // Create new reminder with updated time (if task still has a due date)
            if (task.DueDate != null)
            {
                await ReminderDataService.Instance.CreateTaskReminderAsync(
                    task, settings.TaskReminderDays);
            }
            
            Log($"Updated reminder for task {task.Id}");
        }
        catch (Exception ex)
        {
            Log($"Failed to update task reminder: {ex.Message}");
        }
    }

    #endregion
}
