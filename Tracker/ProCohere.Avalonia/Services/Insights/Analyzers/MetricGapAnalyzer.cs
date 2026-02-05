using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights.Analyzers;

/// <summary>
/// Analyzes metrics for missing data or declining trends.
/// </summary>
public class MetricGapAnalyzer : IInsightAnalyzer
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    public string Name => "Metric Gap";

    public IReadOnlyList<InsightType> InsightTypes => new[]
    {
        InsightType.MetricMissing,
        InsightType.MetricDeclining
    };

    public MetricGapAnalyzer()
    {
    }

    public async Task<List<Insight>> AnalyzeAsync(Guid userId, Guid organizationId)
    {
        var insights = new List<Insight>();

        try
        {
            Console.WriteLine($"[MetricGapAnalyzer] Analyzing for org {organizationId}");

            // Get all active metrics
            var response = await Client
                .From<MetricDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.IsDeleted == false)
                .Get();

            var metrics = response.Models;
            Console.WriteLine($"[MetricGapAnalyzer] Found {metrics.Count} metrics");

            foreach (var metric in metrics)
            {
                // Check for missing current value
                if (!metric.CurrentValue.HasValue)
                {
                    insights.Add(CreateMissingDataInsight(metric, userId));
                }
                // Check for declining metric (when higher is better)
                else if (metric.TargetValue.HasValue && metric.CurrentValue.HasValue)
                {
                    if (IsMetricDeclining(metric))
                    {
                        insights.Add(CreateDecliningInsight(metric, userId));
                    }
                }
            }

            Console.WriteLine($"[MetricGapAnalyzer] Generated {insights.Count} insights");
            return insights;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MetricGapAnalyzer] ERROR: {ex.Message}");
            return insights;
        }
    }

    #region Private Methods

    private static bool IsMetricDeclining(MetricDto metric)
    {
        if (!metric.CurrentValue.HasValue || !metric.TargetValue.HasValue)
            return false;

        // If higher is better and current is significantly below target
        if (metric.Direction == "higher_is_better")
        {
            var percentOfTarget = (decimal)metric.CurrentValue.Value / metric.TargetValue.Value;
            return percentOfTarget < 0.7m; // Less than 70% of target
        }

        // If lower is better and current is significantly above target
        if (metric.Direction == "lower_is_better")
        {
            var percentOfTarget = (decimal)metric.CurrentValue.Value / metric.TargetValue.Value;
            return percentOfTarget > 1.3m; // More than 130% of target
        }

        return false;
    }

    private static Insight CreateMissingDataInsight(MetricDto metric, Guid userId)
    {
        var metricName = TruncateText(metric.Name ?? "Untitled Metric", 50);

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = metric.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.MetricMissing,
            Title = $"Missing Data: \"{metricName}\"",
            Content = "Metric has no current value recorded. Consider updating to track progress.",
            EntityType = "metric",
            EntityId = metric.Id,
            RelevanceScore = 0.60m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static Insight CreateDecliningInsight(MetricDto metric, Guid userId)
    {
        var metricName = TruncateText(metric.Name ?? "Untitled Metric", 50);
        var current = metric.CurrentValue!.Value;
        var target = metric.TargetValue!.Value;
        var unit = string.IsNullOrWhiteSpace(metric.Unit) ? "" : $" {metric.Unit}";

        var description = metric.Direction == "higher_is_better"
            ? $"Current value ({current:F1}{unit}) is below target ({target:F1}{unit}). Consider reviewing strategy or adjusting target."
            : $"Current value ({current:F1}{unit}) is above target ({target:F1}{unit}). Consider interventions to bring back on track.";

        return new Insight
        {
            Id = Guid.NewGuid(),
            OrganizationId = metric.OrganizationId,
            GeneratedFor = userId,
            Type = InsightType.MetricDeclining,
            Title = $"Off Target: \"{metricName}\"",
            Content = description,
            EntityType = "metric",
            EntityId = metric.Id,
            RelevanceScore = 0.80m,
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
/// DTO for metrics table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("metrics")]
internal class MetricDto : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Supabase.Postgrest.Attributes.Column("name")]
    public string? Name { get; set; }

    [Supabase.Postgrest.Attributes.Column("current_value")]
    public decimal? CurrentValue { get; set; }

    [Supabase.Postgrest.Attributes.Column("target_value")]
    public decimal? TargetValue { get; set; }

    [Supabase.Postgrest.Attributes.Column("unit")]
    public string? Unit { get; set; }

    [Supabase.Postgrest.Attributes.Column("direction")]
    public string? Direction { get; set; }

    [Supabase.Postgrest.Attributes.Column("frequency")]
    public string? Frequency { get; set; }

    [Supabase.Postgrest.Attributes.Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
