using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Service that gathers contextual information about the user's current work state
/// to provide relevant context to AI conversations.
/// MVVM Compliant: Service layer owns context gathering logic.
/// </summary>
public class AIContextService
{
    #region Singleton

    private static readonly Lazy<AIContextService> _instance =
        new(() => new AIContextService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static AIContextService Instance => _instance.Value;

    #endregion

    private readonly DashboardService _dashboardService;
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly GoalsService _goalsService;

    private AIContextService()
    {
        _dashboardService = DashboardService.Instance;
        _projectService = ProjectService.Instance;
        _taskService = TaskService.Instance;
        _goalsService = GoalsService.Instance;
    }

    /// <summary>
    /// Gets the current user context for AI conversations.
    /// </summary>
    /// <returns>Formatted context string for AI system message</returns>
    public async Task<string> GetCurrentContextAsync()
    {
        var context = new StringBuilder();
        
        // Current user info
        var currentUser = AuthService.Instance.CurrentTeamMember;
        if (currentUser != null)
        {
            context.AppendLine($"Current User: {currentUser.FirstName} {currentUser.LastName}");
            if (!string.IsNullOrEmpty(currentUser.JobTitle))
                context.AppendLine($"Role: {currentUser.JobTitle}");
            context.AppendLine();
        }

        // Active projects
        var projects = await _projectService.GetAllProjectsAsync();
        var activeProjects = projects?.Where(p => p.Status == ProjectStatus.Active).ToList();
        if (activeProjects?.Any() == true)
        {
            context.AppendLine("Active Projects:");
            foreach (var project in activeProjects.Take(5))
            {
                context.AppendLine($"  - {project.Name}" + 
                    (project.DueDate.HasValue ? $" (due {project.DueDate:MMM dd})" : ""));
            }
            context.AppendLine();
        }

        // Recent tasks
        var tasks = await _taskService.GetTasksAsync(includeCompleted: false);
        if (tasks?.Any() == true)
        {
            context.AppendLine($"Open Tasks: {tasks.Count}");
            var urgentTasks = tasks.Where(t => t.Priority == "High" || t.Priority == "Critical").ToList();
            if (urgentTasks.Any())
            {
                context.AppendLine($"  High Priority: {urgentTasks.Count}");
            }
            context.AppendLine();
        }

        // Active goals
        var goals = await _goalsService.GetMyGoalsAsync();
        var activeGoals = goals?.Where(g => g.Lifecycle == GoalLifecycle.Active).ToList();
        if (activeGoals?.Any() == true)
        {
            context.AppendLine($"Active Goals: {activeGoals.Count}");
            foreach (var goal in activeGoals.Take(3))
            {
                context.AppendLine($"  - {goal.Title}");
            }
            context.AppendLine();
        }

        // Current date/time
        context.AppendLine($"Current Date: {DateTime.Now:dddd, MMMM dd, yyyy}");
        context.AppendLine($"Current Time: {DateTime.Now:h:mm tt}");

        return context.ToString();
    }

    /// <summary>
    /// Gets a brief context summary for display in UI.
    /// </summary>
    public async Task<string> GetContextSummaryAsync()
    {
        var currentUser = AuthService.Instance.CurrentTeamMember;
        if (currentUser == null)
            return "Not logged in";

        var projects = await _projectService.GetAllProjectsAsync();
        var activeProjects = projects?.Count(p => p.Status == ProjectStatus.Active) ?? 0;

        var tasks = await _taskService.GetTasksAsync(includeCompleted: false);
        var openTasks = tasks?.Count ?? 0;

        return $"{currentUser.FirstName} • {activeProjects} projects • {openTasks} tasks";
    }
}
