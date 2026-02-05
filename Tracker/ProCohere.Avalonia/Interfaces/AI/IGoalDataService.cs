using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for goal data operations.
/// Provides simplified, AI-friendly methods for goal management.
/// </summary>
public interface IGoalDataService
{
    /// <summary>
    /// Creates a new goal with the specified details.
    /// </summary>
    /// <param name="title">Goal title</param>
    /// <param name="description">Goal description</param>
    /// <param name="targetDate">Target completion date</param>
    /// <param name="category">Goal category (Personal, Professional, Team, Learning, Health)</param>
    /// <returns>Created goal details or error message</returns>
    Task<string> CreateGoalAsync(string title, string? description = null, string? targetDate = null, string category = "Professional");

    /// <summary>
    /// Gets goals with optional filtering.
    /// </summary>
    /// <param name="status">Filter by status (active, completed, all)</param>
    /// <param name="category">Filter by category</param>
    /// <returns>List of matching goals</returns>
    Task<List<GoalDetail>> GetGoalsAsync(string status = "active", string? category = null);

    /// <summary>
    /// Marks a goal as completed.
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <returns>Success message or error</returns>
    Task<string> CompleteGoalAsync(Guid goalId);

    /// <summary>
    /// Updates an existing goal.
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <param name="title">New title (optional)</param>
    /// <param name="description">New description (optional)</param>
    /// <param name="targetDate">New target date (optional)</param>
    /// <returns>Success message or error</returns>
    Task<string> UpdateGoalAsync(Guid goalId, string? title = null, string? description = null, string? targetDate = null);
}