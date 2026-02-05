using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for task operations.
/// Wraps TaskService with AI-friendly interface.
/// </summary>
public class TaskDataService : ITaskDataService
{
    private readonly TaskService _taskService;
    private readonly TeamDataService _teamDataService;

    public TaskDataService()
    {
        _taskService = TaskService.Instance;
        _teamDataService = new TeamDataService();
    }

    public async Task<string> CreateTaskAsync(string description, string priority = "Medium", string? dueDate = null, string? assignedToEmail = null)
    {
        try
        {
            // Validate priority
            if (!IsValidPriority(priority))
            {
                return $"Invalid priority '{priority}'. Valid options are: High, Medium, Low";
            }

            // Parse due date if provided
            DateTime? parsedDueDate = null;
            if (!string.IsNullOrEmpty(dueDate))
            {
                if (!DateTime.TryParse(dueDate, out var date))
                {
                    return $"Invalid date format '{dueDate}'. Please use a standard date format like YYYY-MM-DD";
                }
                parsedDueDate = date;
            }

            // Find assignee if email provided
            Guid? assigneeId = null;
            if (!string.IsNullOrEmpty(assignedToEmail))
            {
                var teamMember = await _teamDataService.GetTeamMemberByEmailAsync(assignedToEmail);
                if (teamMember == null)
                {
                    return $"Could not find team member with email '{assignedToEmail}'";
                }
                assigneeId = teamMember.Id;
            }

            // Create task using service method
            var createdTask = await _taskService.CreateTaskAsync(
                title: description,
                description: null,
                priority: priority,
                dueDate: parsedDueDate,
                assignedTo: assigneeId);
            
            var success = createdTask != null;
            
            if (success)
            {
                var assigneeText = assigneeId.HasValue ? $" assigned to {assignedToEmail}" : "";
                var dueDateText = parsedDueDate.HasValue ? $" due {parsedDueDate:MM/dd/yyyy}" : "";
                return $"✅ Created {priority.ToLower()} priority task: '{description}'{assigneeText}{dueDateText}";
            }
            else
            {
                return $"❌ Failed to create task: {_taskService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error creating task: {ex.Message}";
        }
    }

    public async Task<List<TaskDetail>> GetTasksAsync(string? priority = null, string status = "open")
    {
        try
        {
            var tasks = await _taskService.GetTasksAsync(includeCompleted: status.ToLower() != "open");
            
            if (tasks == null)
                return new List<TaskDetail>();

            // Apply filters
            var filtered = tasks.AsEnumerable();

            if (!string.IsNullOrEmpty(priority) && IsValidPriority(priority))
            {
                filtered = filtered.Where(t => string.Equals(t.Priority, priority, StringComparison.OrdinalIgnoreCase));
            }

            if (status.ToLower() != "all")
            {
                var statusFilter = status.ToLower() == "completed" ? "completed" : "open";
                filtered = filtered.Where(t => string.Equals(t.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }
        catch (Exception)
        {
            return new List<TaskDetail>();
        }
    }

    public async Task<string> CompleteTaskAsync(Guid taskId)
    {
        try
        {
            var success = await _taskService.CompleteTaskAsync(taskId);
            
            if (success)
            {
                return "✅ Task marked as completed";
            }
            else
            {
                return $"❌ Failed to complete task: {_taskService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error completing task: {ex.Message}";
        }
    }

    public async Task<string> UpdateTaskAsync(Guid taskId, string? description = null, string? priority = null, string? dueDate = null)
    {
        try
        {
            // Get existing task
            var tasks = await _taskService.GetTasksAsync();
            var existingTask = tasks?.FirstOrDefault(t => t.Id == taskId);
            
            if (existingTask == null)
            {
                return "❌ Task not found";
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(description))
                existingTask.Description = description;

            if (!string.IsNullOrEmpty(priority))
            {
                if (!IsValidPriority(priority))
                {
                    return $"Invalid priority '{priority}'. Valid options are: High, Medium, Low";
                }
                existingTask.Priority = priority;
            }

            if (!string.IsNullOrEmpty(dueDate))
            {
                if (!DateTime.TryParse(dueDate, out var date))
                {
                    return $"Invalid date format '{dueDate}'. Please use a standard date format like YYYY-MM-DD";
                }
                existingTask.DueDate = date;
            }

            var updatedTask = await _taskService.UpdateTaskAsync(existingTask);
            
            if (updatedTask != null)
            {
                return "✅ Task updated successfully";
            }
            else
            {
                return $"❌ Failed to update task: {_taskService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error updating task: {ex.Message}";
        }
    }

    private static bool IsValidPriority(string priority)
    {
        var validPriorities = new[] { "High", "Medium", "Low" };
        return validPriorities.Any(p => string.Equals(p, priority, StringComparison.OrdinalIgnoreCase));
    }
}