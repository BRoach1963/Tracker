using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes goal progress and trajectory.
/// Generates insights for goals that are off-track or making exceptional progress.
/// </summary>
public class GoalTrajectoryAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    public string Name => "Goal Trajectory";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.GoalOffTrack,
        InsightType.GoalOnTrack
    };

    public GoalTrajectoryAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[GoalTrajectoryAnalyzer] Analyzing for user {userId}");

            var today = DateTime.UtcNow.Date;

            // Get active goals with due dates
            var response = await Client
                .From<GoalDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Status != "completed")
                .Where(x => x.Status != "retired")
                .Get();

            var goals = response.Models.Where(g => g.DueDate.HasValue).ToList();
            Console.WriteLine($"[GoalTrajectoryAnalyzer] Found {goals.Count} goals");

            foreach (var goal in goals)
            {
                // Check if goal is at risk (due soon or past due)
                if (goal.DueDate.HasValue)
                {
                    var daysUntilDue = (goal.DueDate.Value.Date - today).Days;
                    
                    // Goal is at risk if status indicates it or it's due soon with low progress
                    if (IsGoalAtRisk(goal, daysUntilDue))
                    {
                        insights.Add(CreateOffTrackInsight(goal, daysUntilDue, userId));
                    }
                    // Goal is on track and worth celebrating
                    else if (IsGoalProgressGood(goal, daysUntilDue))
                    {
                        insights.Add(CreateOnTrackInsight(goal, userId));
                    }
                }
            }

            Console.WriteLine($"[GoalTrajectoryAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoalTrajectoryAnalyzer] ERROR: {ex.Message}");
            return insights;
        }
    }

    #region Private Methods

    private static bool IsGoalAtRisk(GoalDto goal, int daysUntilDue)
    {
        // Explicit at_risk or needs_attention status
        if (goal.Status == "at_risk" || goal.Status == "needs_attention")
            return true;

        // Past due and not completed
        if (daysUntilDue < 0)
            return true;

        // Due soon with low progress
        if (daysUntilDue <= 7 && goal.ProgressPercent < 70)
            return true;

        // Due within 2 weeks with very low progress
        if (daysUntilDue <= 14 && goal.ProgressPercent < 30)
            return true;

        return false;
    }

    private static bool IsGoalProgressGood(GoalDto goal, int daysUntilDue)
    {
        // Explicitly on track with good progress
        if (goal.Status == "on_track" && goal.ProgressPercent >= 75)
            return true;

        // Making exceptional progress (completed early)
        if (goal.Status == "completed" && daysUntilDue > 7)
            return true;

        return false;
    }

    private static Insight CreateOffTrackInsight(GoalDto goal, int daysUntilDue, Guid userId)
    {
        var severity = CalculateOffTrackSeverity(daysUntilDue, goal.ProgressPercent);
        var goalTitle = TruncateText(goal.Title ?? "Untitled Goal", 50);
        
        string description;
        if (daysUntilDue < 0)
        {
            var daysOverdue = Math.Abs(daysUntilDue);
            description = $"Goal is {daysOverdue} day{(daysOverdue != 1 ? "s" : "")} overdue. Current progress: {goal.ProgressPercent}%. Consider adjusting timeline or escalating.";
        }
        else
        {
            description = $"Goal is due in {daysUntilDue} day{(daysUntilDue != 1 ? "s" : "")} but only {goal.ProgressPercent}% complete. Consider check-in or resource allocation.";
        }

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = goal.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.GoalOffTrack,
            Title = $"At Risk: \"{goalTitle}\"",
            Content = description,
            SubjectType = "goal",
            SubjectId = goal.Id,
            SourceType = "goal",
            SourceId = goal.Id,
            SeverityLevel = SeverityToLevel(severity),
            RelevanceScore = 0.90m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateOnTrackInsight(GoalDto goal, Guid userId)
    {
        var goalTitle = TruncateText(goal.Title ?? "Untitled Goal", 50);
        var description = $"Goal is {goal.ProgressPercent}% complete and on track. Great progress!";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = goal.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.GoalOnTrack,
            Title = $"On Track: \"{goalTitle}\"",
            Content = description,
            SubjectType = "goal",
            SubjectId = goal.Id,
            SourceType = "goal",
            SourceId = goal.Id,
            SeverityLevel = 1, // Low for positive insights
            RelevanceScore = 0.30m,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static InsightSeverity CalculateOffTrackSeverity(int daysUntilDue, int progressPercent)
    {
        // Past due = critical
        if (daysUntilDue < 0)
            return InsightSeverity.Critical;

        // Due very soon with low progress = high
        if (daysUntilDue <= 3 && progressPercent < 50)
            return InsightSeverity.High;

        // Due soon with moderate progress = medium
        if (daysUntilDue <= 7 && progressPercent < 70)
            return InsightSeverity.High;

        return InsightSeverity.Medium;
    }
    
    /// <summary>
    /// Converts InsightSeverity enum to database severity level (1-5).
    /// </summary>
    private static int SeverityToLevel(InsightSeverity severity) => severity switch
    {
        InsightSeverity.Critical => 5,
        InsightSeverity.High => 4,
        InsightSeverity.Medium => 3,
        InsightSeverity.Low => 2,
        _ => 1
    };

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
/// DTO for goals table (minimal fields needed for trajectory analysis).
/// </summary>
[Supabase.Postgrest.Attributes.Table("goals")]
internal class GoalDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("owner_id")]
    public Guid OwnerTeamMemberId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }

    [Supabase.Postgrest.Attributes.Column("status")]
    public string Status { get; set; } = "not_started";

    [Supabase.Postgrest.Attributes.Column("progress_percent")]
    public int ProgressPercent { get; set; }

    [Supabase.Postgrest.Attributes.Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Supabase.Postgrest.Attributes.Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
