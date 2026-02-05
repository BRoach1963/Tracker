using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes personal dates (birthdays, anniversaries, work milestones).
/// Generates insights to remind managers of important personal events.
/// </summary>
public class PersonalDateAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    /// <summary>
    /// Days ahead to alert about upcoming personal dates.
    /// </summary>
    private const int LookaheadDays = 7;

    public string Name => "Personal Date";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.PersonalDate
    };

    public PersonalDateAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[PersonalDateAnalyzer] Analyzing for org {organizationId}");

            var today = DateTime.UtcNow.Date;
            var lookaheadDate = today.AddDays(LookaheadDays);

            // Get team members
            var response = await Client
                .From<TeamMemberDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.IsDeleted == false)
                .Get();

            var members = response.Models;
            Console.WriteLine($"[PersonalDateAnalyzer] Found {members.Count} members");

            foreach (var member in members)
            {
                // Check birthday
                if (member.BirthDate.HasValue)
                {
                    var birthdayThisYear = new DateTime(today.Year, member.BirthDate.Value.Month, member.BirthDate.Value.Day);
                    if (birthdayThisYear >= today && birthdayThisYear <= lookaheadDate)
                    {
                        var daysUntil = (birthdayThisYear - today).Days;
                        insights.Add(CreateBirthdayInsight(member, daysUntil, userId));
                    }
                }

                // Check start date anniversary
                if (member.StartDate.HasValue)
                {
                    var anniversaryThisYear = new DateTime(today.Year, member.StartDate.Value.Month, member.StartDate.Value.Day);
                    var yearsOfService = today.Year - member.StartDate.Value.Year;
                    
                    // Only significant anniversaries (1, 2, 3, 5, 10, 15, 20, 25...)
                    if (IsSignificantAnniversary(yearsOfService) && 
                        anniversaryThisYear >= today && 
                        anniversaryThisYear <= lookaheadDate)
                    {
                        var daysUntil = (anniversaryThisYear - today).Days;
                        insights.Add(CreateAnniversaryInsight(member, daysUntil, yearsOfService, userId));
                    }
                }
            }

            Console.WriteLine($"[PersonalDateAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PersonalDateAnalyzer] ERROR: {ex.Message}");
            return insights;
        }
    }

    #region Private Methods

    private static bool IsSignificantAnniversary(int years)
    {
        if (years <= 3) return true; // First 3 years
        if (years == 5 || years == 10 || years == 15 || years == 20 || years == 25) return true;
        if (years % 10 == 0) return true; // Every 10 years after 20
        return false;
    }

    private static Insight CreateBirthdayInsight(TeamMemberDto member, int daysUntil, Guid userId)
    {
        var memberName = member.FullName ?? "Team Member";
        var timeframe = daysUntil == 0 ? "today" : 
                       daysUntil == 1 ? "tomorrow" : 
                       $"in {daysUntil} days";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = member.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.PersonalDate,
            Title = $"🎂 Birthday: {memberName}",
            Content = $"{memberName}'s birthday is {timeframe}. Consider sending a message or acknowledgment.",
            EntityType = "team_member",
            EntityId = member.Id,
            RelevanceScore = 0.25m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateAnniversaryInsight(TeamMemberDto member, int daysUntil, int years, Guid userId)
    {
        var memberName = member.FullName ?? "Team Member";
        var timeframe = daysUntil == 0 ? "today" : 
                       daysUntil == 1 ? "tomorrow" : 
                       $"in {daysUntil} days";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = member.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.PersonalDate,
            Title = $"🎉 Work Anniversary: {memberName} ({years} years)",
            Content = $"{memberName}'s {years}-year work anniversary is {timeframe}. Consider recognition or celebration.",
            EntityType = "team_member",
            EntityId = member.Id,
            RelevanceScore = 0.30m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    #endregion
}

/// <summary>
/// DTO for team_members table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("team_members")]
internal class TeamMemberDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("full_name")]
    public string? FullName { get; set; }

    [Supabase.Postgrest.Attributes.Column("birth_date")]
    public DateTime? BirthDate { get; set; }

    [Supabase.Postgrest.Attributes.Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Supabase.Postgrest.Attributes.Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
