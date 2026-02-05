using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dtos;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes action items (tasks) for staleness and overdue status.
/// Generates insights to help managers follow up on pending tasks.
/// </summary>
public class ActionItemStalenessAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    /// <summary>
    /// Days after which an uncompleted task with no due date is considered stale.
    /// </summary>
    private const int StaleThresholdDays = 14;

    public string Name => "Action Item Staleness";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.StaleActionItem,
        InsightType.TaskOverdue
    };

    public ActionItemStalenessAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[ActionItemStalenessAnalyzer] Analyzing for user {userId}");

            var today = DateTime.UtcNow.Date;
            var staleDate = today.AddDays(-StaleThresholdDays);

            // Get all uncompleted tasks for user's organization
            var response = await Client
                .From<TaskDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Status != "completed")
                .Get();

            var tasks = response.Models;
            Console.WriteLine($"[ActionItemStalenessAnalyzer] Found {tasks.Count} tasks");

            foreach (var task in tasks)
            {
                // Check if overdue
                if (task.DueDate.HasValue && task.DueDate.Value.Date < today)
                {
                    var daysOverdue = (today - task.DueDate.Value.Date).Days;
                    var severity = CalculateOverdueSeverity(daysOverdue);
                    insights.Add(CreateOverdueInsight(task, daysOverdue, severity, userId));
                }
                // Check if stale (no due date or future due date, but task is old)
                else if (task.CreatedAt.Date <= staleDate && !task.DueDate.HasValue)
                {
                    var daysOld = (today - task.CreatedAt.Date).Days;
                    insights.Add(CreateStaleInsight(task, daysOld, userId));
                }
            }

            Console.WriteLine($"[ActionItemStalenessAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ActionItemStalenessAnalyzer] ERROR: {ex.Message}");
            return insights; // Return what we have, don't throw
        }
    }

    #region Private Methods

    private static InsightSeverity CalculateOverdueSeverity(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= 14 => InsightSeverity.Critical,
            >= 7 => InsightSeverity.High,
            >= 3 => InsightSeverity.Medium,
            _ => InsightSeverity.Low
        };
    }

    private static Insight CreateOverdueInsight(TaskDto task, int daysOverdue, InsightSeverity severity, Guid userId)
    {
        var taskTitle = string.IsNullOrWhiteSpace(task.Title) 
            ? TruncateText(task.Description ?? "Untitled Task", 50) 
            : TruncateText(task.Title, 50);

        var dueDateStr = task.DueDate!.Value.ToString("MMM d");
        var plural = daysOverdue != 1 ? "s" : "";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.TaskOverdue,
            Title = $"Overdue: \"{taskTitle}\"",
            Content = $"Task was due {daysOverdue} day{plural} ago ({dueDateStr}). Consider following up or rescheduling.",
            EntityType = "task",
            EntityId = task.Id,
            RelevanceScore = 0.95m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateStaleInsight(TaskDto task, int daysOld, Guid userId)
    {
        var taskTitle = string.IsNullOrWhiteSpace(task.Title) 
            ? TruncateText(task.Description ?? "Untitled Task", 50) 
            : TruncateText(task.Title, 50);

        var plural = daysOld != 1 ? "s" : "";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.StaleActionItem,
            Title = $"Stale: \"{taskTitle}\"",
            Content = $"Task has been open for {daysOld} day{plural} with no due date set. Consider adding a deadline or marking complete.",
            EntityType = "task",
            EntityId = task.Id,
            RelevanceScore = 0.65m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return text.Length <= maxLength 
            ? text 
            : text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}

/// <summary>
/// DTO for tasks table (minimal fields needed for analysis).
/// </summary>
[Supabase.Postgrest.Attributes.Table("tasks")]
internal class TaskDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }

    [Supabase.Postgrest.Attributes.Column("description")]
    public string? Description { get; set; }

    [Supabase.Postgrest.Attributes.Column("status")]
    public string Status { get; set; } = "not_started";

    [Supabase.Postgrest.Attributes.Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Supabase.Postgrest.Attributes.Column("assigned_to")]
    public Guid? AssignedTo { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Supabase.Postgrest.Attributes.Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
