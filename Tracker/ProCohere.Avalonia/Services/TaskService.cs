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
}
