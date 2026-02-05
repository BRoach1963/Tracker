using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes meeting cadence patterns to detect overdue or upcoming meetings.
/// </summary>
public class MeetingCadenceAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    /// <summary>
    /// Days overdue before alerting about missing meeting.
    /// </summary>
    private const int OverdueThresholdDays = 7;

    /// <summary>
    /// Days ahead to alert about upcoming meetings.
    /// </summary>
    private const int UpcomingThresholdDays = 3;

    public string Name => "Meeting Cadence";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.MeetingOverdue,
        InsightType.MeetingUpcoming
    };

    public MeetingCadenceAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[MeetingCadenceAnalyzer] Analyzing for org {organizationId}");

            var today = DateTime.UtcNow.Date;
            var overdueDate = today.AddDays(-OverdueThresholdDays);
            var upcomingDate = today.AddDays(UpcomingThresholdDays);

            // Get meetings in relevant date range
            var response = await Client
                .From<MeetingDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.IsDeleted == false)
                .Get();

            var meetings = response.Models;
            Console.WriteLine($"[MeetingCadenceAnalyzer] Found {meetings.Count} meetings");

            foreach (var meeting in meetings)
            {
                if (meeting.ScheduledDate == null)
                    continue;

                var meetingDate = meeting.ScheduledDate.Value.Date;

                // Check if overdue
                if (meetingDate < today && meeting.Status != "completed")
                {
                    var daysOverdue = (today - meetingDate).Days;
                    if (daysOverdue >= OverdueThresholdDays)
                    {
                        insights.Add(CreateOverdueInsight(meeting, daysOverdue, userId));
                    }
                }
                // Check if upcoming
                else if (meetingDate >= today && meetingDate <= upcomingDate)
                {
                    var daysUntil = (meetingDate - today).Days;
                    insights.Add(CreateUpcomingInsight(meeting, daysUntil, userId));
                }
            }

            Console.WriteLine($"[MeetingCadenceAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MeetingCadenceAnalyzer] ERROR: {ex.Message}");
            return insights;
        }
    }

    #region Private Methods

    private static Insight CreateOverdueInsight(MeetingDto meeting, int daysOverdue, Guid userId)
    {
        var severity = daysOverdue >= 14 ? InsightSeverity.High : InsightSeverity.Medium;
        var meetingTitle = TruncateText(meeting.Title ?? "Untitled Meeting", 50);

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = meeting.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.MeetingOverdue,
            Title = $"Overdue Meeting: \"{meetingTitle}\"",
            Content = $"Meeting was scheduled {daysOverdue} day{(daysOverdue != 1 ? "s" : "")} ago and not marked complete. Consider rescheduling or marking as done.",
            EntityType = "meeting",
            EntityId = meeting.Id,
            RelevanceScore = daysOverdue >= 14 ? 0.75m : 0.55m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateUpcomingInsight(MeetingDto meeting, int daysUntil, Guid userId)
    {
        var meetingTitle = TruncateText(meeting.Title ?? "Untitled Meeting", 50);
        var timeframe = daysUntil == 0 ? "today" : 
                       daysUntil == 1 ? "tomorrow" : 
                       $"in {daysUntil} days";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = meeting.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.MeetingUpcoming,
            Title = $"Upcoming: \"{meetingTitle}\"",
            Content = $"Meeting scheduled {timeframe}. Consider reviewing agenda and preparing topics.",
            EntityType = "meeting",
            EntityId = meeting.Id,
            RelevanceScore = 0.35m,
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
/// DTO for meetings table (minimal fields needed for cadence analysis).
/// </summary>
[Supabase.Postgrest.Attributes.Table("meetings")]
internal class MeetingDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }

    [Supabase.Postgrest.Attributes.Column("scheduled_date")]
    public DateTime? ScheduledDate { get; set; }

    [Supabase.Postgrest.Attributes.Column("status")]
    public string? Status { get; set; }

    [Supabase.Postgrest.Attributes.Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
