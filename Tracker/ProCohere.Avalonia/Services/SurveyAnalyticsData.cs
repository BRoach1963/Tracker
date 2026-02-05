using System.Collections.Generic;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Data container for survey analytics.
/// Returned by SurveyService.GetSurveyAnalyticsAsync.
/// </summary>
public class SurveyAnalyticsData
{
    public Survey Survey { get; set; } = null!;
    public List<SurveyQuestion> Questions { get; set; } = new();
    public List<SurveyResponse> Responses { get; set; } = new();
    public List<SurveyAnswer> Answers { get; set; } = new();
    public int TotalResponses { get; set; }
    public int CompletedResponses { get; set; }
}
