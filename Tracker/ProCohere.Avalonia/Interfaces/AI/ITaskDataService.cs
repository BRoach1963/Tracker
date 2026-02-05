using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for task data operations.
/// Provides simplified, AI-friendly methods for task management.
/// </summary>
public interface ITaskDataService
{
    /// <summary>
    /// Creates a new task with the specified details.
    /// </summary>
    /// <param name="description">Task description</param>
    /// <param name="priority">Task priority (High, Medium, Low)</param>
    /// <param name="dueDate">Optional due date</param>
    /// <param name="assignedToEmail">Optional email of person to assign to</param>
    /// <returns>Created task details or error message</returns>
    Task<string> CreateTaskAsync(string description, string priority = "Medium", string? dueDate = null, string? assignedToEmail = null);

    /// <summary>
    /// Gets tasks with optional filtering.
    /// </summary>
    /// <param name="priority">Filter by priority (High, Medium, Low) or null for all</param>
    /// <param name="status">Filter by status (open, completed, all)</param>
    /// <returns>List of matching tasks</returns>
    Task<List<TaskDetail>> GetTasksAsync(string? priority = null, string status = "open");

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>Success message or error</returns>
    Task<string> CompleteTaskAsync(Guid taskId);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="description">New description (optional)</param>
    /// <param name="priority">New priority (optional)</param>
    /// <param name="dueDate">New due date (optional)</param>
    /// <returns>Success message or error</returns>
    Task<string> UpdateTaskAsync(Guid taskId, string? description = null, string? priority = null, string? dueDate = null);
}