using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for goal operations.
/// Wraps GoalsService with AI-friendly interface.
/// </summary>
public class GoalDataService : IGoalDataService
{
    private readonly GoalsService _goalsService;

    public GoalDataService()
    {
        _goalsService = GoalsService.Instance;
    }

    public async Task<string> CreateGoalAsync(string title, string? description = null, string? targetDate = null, string category = "Professional")
    {
        try
        {
            // Parse target date if provided
            DateTime? parsedDueDate = null;
            if (!string.IsNullOrEmpty(targetDate))
            {
                if (!DateTime.TryParse(targetDate, out var date))
                {
                    return $"Invalid date format '{targetDate}'. Please use a standard date format like YYYY-MM-DD";
                }
                parsedDueDate = date;
            }

            // Create goal
            var goal = new GoalDetail
            {
                Title = title,
                Description = description,
                DueDate = parsedDueDate,
                Status = "active"  // Status drives computed Health and Lifecycle properties
            };

            var created = await _goalsService.CreateGoalAsync(goal);
            
            if (created != null)
            {
                var dateText = parsedDueDate.HasValue ? $" with due date {parsedDueDate:MM/dd/yyyy}" : "";
                return $"✅ Created goal: '{title}'{dateText}";
            }
            else
            {
                return $"❌ Failed to create goal: {_goalsService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error creating goal: {ex.Message}";
        }
    }

    public async Task<List<GoalDetail>> GetGoalsAsync(string status = "active", string? category = null)
    {
        try
        {
            var goals = await _goalsService.GetMyGoalsAsync();
            
            if (goals == null)
                return new List<GoalDetail>();

            // Apply lifecycle filter
            var filtered = goals.AsEnumerable();

            if (status.ToLower() != "all")
            {
                var lifecycleFilter = status.ToLower() == "completed" 
                    ? GoalLifecycle.Retired  // Completed goals map to Retired lifecycle
                    : GoalLifecycle.Active;
                filtered = filtered.Where(g => g.Lifecycle == lifecycleFilter);
            }

            return filtered.ToList();
        }
        catch (Exception)
        {
            return new List<GoalDetail>();
        }
    }

    public async Task<string> CompleteGoalAsync(Guid goalId)
    {
        try
        {
            var goal = await _goalsService.GetGoalByIdAsync(goalId);
            if (goal != null)
            {
                goal.Status = "completed";  // This maps to Lifecycle.Retired
                var updated = await _goalsService.UpdateGoalAsync(goal);
                
                if (updated != null)
                {
                    return "✅ Goal marked as completed";
                }
                else
                {
                    return $"❌ Failed to complete goal: {_goalsService.LastError ?? "Unknown error"}";
                }
            }
            return "❌ Goal not found";
        }
        catch (Exception ex)
        {
            return $"❌ Error completing goal: {ex.Message}";
        }
    }

    public async Task<string> UpdateGoalAsync(Guid goalId, string? title = null, string? description = null, string? targetDate = null)
    {
        try
        {
            // Get existing goal
            var existingGoal = await _goalsService.GetGoalByIdAsync(goalId);
            
            if (existingGoal == null)
            {
                return "❌ Goal not found";
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(title))
                existingGoal.Title = title;

            if (!string.IsNullOrEmpty(description))
                existingGoal.Description = description;

            if (!string.IsNullOrEmpty(targetDate))
            {
                if (!DateTime.TryParse(targetDate, out var date))
                {
                    return $"Invalid date format '{targetDate}'. Please use a standard date format like YYYY-MM-DD";
                }
                existingGoal.DueDate = date;
            }

            var updated = await _goalsService.UpdateGoalAsync(existingGoal);
            
            if (updated != null)
            {
                return "✅ Goal updated successfully";
            }
            else
            {
                return $"❌ Failed to update goal: {_goalsService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error updating goal: {ex.Message}";
        }
    }

}