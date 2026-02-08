using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes survey sentiment trends to detect declining or improving morale.
/// Note: Requires survey_responses table to exist with sentiment scoring.
/// </summary>
public class SurveySentimentAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    public string Name => "Survey Sentiment";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.SentimentDeclining,
        InsightType.SentimentImproving
    };

    public SurveySentimentAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[SurveySentimentAnalyzer] Analyzing for org {organizationId}");

            // Check if survey_responses table exists
            var tableExists = await CheckSurveyTableExists();
            if (!tableExists)
            {
                Console.WriteLine("[SurveySentimentAnalyzer] survey_responses table not found");
                return insights;
            }

            // Get recent survey responses (last 90 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var response = await Client
                .From<SurveyResponseDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.CreatedAt >= cutoffDate)
                .Get();

            var responses = response.Models;
            Console.WriteLine($"[SurveySentimentAnalyzer] Found {responses.Count} survey responses");

            if (responses.Count < 5)
            {
                Console.WriteLine("[SurveySentimentAnalyzer] Insufficient survey data");
                return insights;
            }

            // Analyze trends by team member
            var memberGroups = responses.GroupBy(r => r.TeamMemberId).ToList();
            
            foreach (var group in memberGroups)
            {
                if (group.Key == null) continue;

                var orderedResponses = group.OrderBy(r => r.CreatedAt).ToList();
                if (orderedResponses.Count < 2) continue;

                var trend = AnalyzeSentimentTrend(orderedResponses);
                
                if (trend == SentimentTrend.Declining)
                {
                    insights.Add(CreateDecliningInsight(group.Key.Value, orderedResponses, userId, organizationId));
                }
                else if (trend == SentimentTrend.Improving)
                {
                    insights.Add(CreateImprovingInsight(group.Key.Value, orderedResponses, userId, organizationId));
                }
            }

            Console.WriteLine($"[SurveySentimentAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SurveySentimentAnalyzer] ERROR: {ex.Message}");
            return insights;
        }
    }

    #region Private Methods

    private async Task<bool> CheckSurveyTableExists()
    {
        try
        {
            // Try a simple query to see if table exists
            await Client.From<SurveyResponseDto>().Limit(1).Get();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private enum SentimentTrend
    {
        Stable,
        Declining,
        Improving
    }

    private static SentimentTrend AnalyzeSentimentTrend(List<SurveyResponseDto> responses)
    {
        if (responses.Count < 2) return SentimentTrend.Stable;

        // Compare recent scores (last 30 days) to older scores
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var recentScores = responses.Where(r => r.CreatedAt >= cutoff).Select(r => r.SentimentScore ?? 0).ToList();
        var olderScores = responses.Where(r => r.CreatedAt < cutoff).Select(r => r.SentimentScore ?? 0).ToList();

        if (recentScores.Count == 0 || olderScores.Count == 0)
            return SentimentTrend.Stable;

        var recentAvg = recentScores.Average();
        var olderAvg = olderScores.Average();
        var change = recentAvg - olderAvg;

        // Threshold of 0.5 point change on 1-5 scale
        if (change <= -0.5m) return SentimentTrend.Declining;
        if (change >= 0.5m) return SentimentTrend.Improving;
        return SentimentTrend.Stable;
    }

    private static Insight CreateDecliningInsight(Guid teamMemberId, List<SurveyResponseDto> responses, Guid userId, Guid organizationId)
    {
        var recentScore = responses.OrderByDescending(r => r.CreatedAt).First().SentimentScore ?? 0;

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GeneratedFor = userId,
            Type = InsightType.SentimentDeclining,
            Title = "Sentiment Declining for Team Member",
            Content = $"Survey responses show declining sentiment trend. Recent score: {recentScore:F1}/5. Consider scheduling a check-in.",
            SubjectType = "team_member",
            SubjectId = teamMemberId,
            SourceType = "team_member",
            SourceId = teamMemberId,
            SeverityLevel = 4, // High for declining sentiment
            RelevanceScore = 0.75m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateImprovingInsight(Guid teamMemberId, List<SurveyResponseDto> responses, Guid userId, Guid organizationId)
    {
        var recentScore = responses.OrderByDescending(r => r.CreatedAt).First().SentimentScore ?? 0;

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GeneratedFor = userId,
            Type = InsightType.SentimentImproving,
            Title = "Sentiment Improving for Team Member",
            Content = $"Survey responses show improving sentiment trend. Recent score: {recentScore:F1}/5. Great progress!",
            SubjectType = "team_member",
            SubjectId = teamMemberId,
            SourceType = "team_member",
            SourceId = teamMemberId,
            SeverityLevel = 1, // Low for positive trends
            RelevanceScore = 0.35m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    #endregion
}

/// <summary>
/// DTO for survey_responses table (if exists).
/// </summary>
[Supabase.Postgrest.Attributes.Table("survey_responses")]
internal class SurveyResponseDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    [Supabase.Postgrest.Attributes.Column("sentiment_score")]
    public decimal? SentimentScore { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
