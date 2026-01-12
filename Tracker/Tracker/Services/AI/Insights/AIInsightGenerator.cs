using System.Text;
using System.Text.Json;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights
{
    /// <summary>
    /// Uses AI providers to generate rich, contextual insights from team data.
    /// Enhances rule-based insights with AI-powered analysis.
    /// </summary>
    public class AIInsightGenerator
    {
        #region Singleton

        private static readonly Lazy<AIInsightGenerator> _instance =
            new(() => new AIInsightGenerator(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AIInsightGenerator Instance => _instance.Value;

        #endregion

        #region Constants

        private const string SystemPrompt = @"You are an expert management coach assistant helping a manager be more effective with their team.
Your role is to analyze team data and provide actionable insights.

When given data about team members, meetings, tasks, OKRs, and other context:
1. Identify patterns that need attention
2. Suggest specific, actionable recommendations
3. Prioritize by business impact
4. Be direct and concise
5. Focus on what the manager can do TODAY

Format your response as JSON with this structure:
{
  ""insights"": [
    {
      ""type"": ""meeting_gap|task_overdue|okr_risk|kpi_gap|recognition_needed|followup_needed"",
      ""severity"": ""critical|warning|info"",
      ""title"": ""Short actionable title"",
      ""description"": ""Detailed explanation with context"",
      ""suggestedAction"": ""Specific action to take"",
      ""relatedEntityType"": ""TeamMember|Task|OKR|KPI|Project"",
      ""relatedEntityId"": ""optional-id""
    }
  ]
}";

        #endregion

        #region Fields

        private readonly ILogger _logger;

        #endregion

        #region Constructor

        private AIInsightGenerator()
        {
            _logger = LoggingManager.GetComponentLogger("AIInsightGenerator");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates AI-powered insights from team data.
        /// </summary>
        public async Task<List<Insight>> GenerateInsightsAsync(
            TeamDataContext dataContext,
            CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                _logger.Info("Generating AI insights for {0} team members...", dataContext.TeamMembers?.Count ?? 0);

                // Get the current AI provider
                var provider = await ChatProviderFactory.Instance.GetProviderAsync();
                if (!provider.IsAvailable)
                {
                    _logger.Warn("AI provider not available, skipping AI insights");
                    return insights;
                }

                // Build the data prompt
                var dataPrompt = BuildDataPrompt(dataContext);
                if (string.IsNullOrEmpty(dataPrompt))
                {
                    _logger.Debug("No data to analyze");
                    return insights;
                }

                // Call the AI
                var response = await provider.GetResponseAsync(dataPrompt, SystemPrompt, cancellationToken);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.Warn("Empty response from AI provider");
                    return insights;
                }

                // Parse the response
                insights = ParseAIResponse(response);
                _logger.Info("AI generated {0} insights", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to generate AI insights");
            }

            return insights;
        }

        /// <summary>
        /// Generates an AI-powered daily briefing summary.
        /// </summary>
        public async Task<string> GenerateBriefingSummaryAsync(
            DailyBriefing briefing,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await ChatProviderFactory.Instance.GetProviderAsync();
                if (!provider.IsAvailable)
                {
                    return "Good morning! Here's your daily summary.";
                }

                var prompt = BuildBriefingPrompt(briefing);
                var systemContext = @"You are a friendly management assistant. 
Provide a brief, conversational daily summary for a manager.
Be encouraging but also highlight urgent items.
Keep it under 3 paragraphs.";

                return await provider.GetResponseAsync(prompt, systemContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to generate briefing summary");
                return briefing.Greeting;
            }
        }

        /// <summary>
        /// Generates AI-powered meeting prep notes.
        /// </summary>
        public async Task<string> GenerateMeetingPrepAsync(
            TeamMember teamMember,
            IEnumerable<Meeting> recentMeetings,
            IEnumerable<TrackerTask> openTasks,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var provider = await ChatProviderFactory.Instance.GetProviderAsync();
                if (!provider.IsAvailable)
                {
                    return $"Meeting prep for {teamMember.FullName}:\n• Review recent action items\n• Check on current projects";
                }

                var prompt = BuildMeetingPrepPrompt(teamMember, recentMeetings, openTasks);
                var systemContext = @"You are a management coach helping prepare for a 1:1 meeting.
Provide specific talking points based on the data.
Include: conversation starters, items to follow up on, potential concerns to address.
Be concise and actionable.";

                return await provider.GetResponseAsync(prompt, systemContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to generate meeting prep");
                return $"Meeting prep for {teamMember.FullName}:\n• Review recent action items\n• Check on current projects";
            }
        }

        #endregion

        #region Private Methods

        private string BuildDataPrompt(TeamDataContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Analyze this team data and provide insights:");
            sb.AppendLine();

            // Team members summary
            if (ctx.TeamMembers?.Any() == true)
            {
                sb.AppendLine("## Team Members");
                foreach (var member in ctx.TeamMembers)
                {
                    var daysSinceLastMeeting = member.LastOneOnOneDate.HasValue
                        ? (DateTime.Now - member.LastOneOnOneDate.Value).Days
                        : -1;

                    sb.AppendLine($"- {member.FullName} ({member.JobTitle ?? "Unknown title"})");
                    if (daysSinceLastMeeting >= 0)
                        sb.AppendLine($"  Last 1:1: {daysSinceLastMeeting} days ago");
                    else
                        sb.AppendLine($"  Last 1:1: Never");
                }
                sb.AppendLine();
            }

            // Overdue tasks
            if (ctx.OverdueTasks?.Any() == true)
            {
                sb.AppendLine("## Overdue Tasks");
                foreach (var task in ctx.OverdueTasks.Take(10))
                {
                    var daysPastDue = task.DueDate.HasValue 
                        ? (DateTime.Now - task.DueDate.Value).Days 
                        : 0;
                    sb.AppendLine($"- {task.Title ?? task.Description ?? "Untitled"} (owned by {task.Owner?.FullName ?? "Unassigned"}, {daysPastDue} days overdue)");
                }
                sb.AppendLine();
            }

            // At-risk Goals
            if (ctx.AtRiskGoals?.Any() == true)
            {
                sb.AppendLine("## At-Risk Goals");
                foreach (var goal in ctx.AtRiskGoals.Take(5))
                {
                    sb.AppendLine($"- {goal.Title}: {goal.ProgressPercent}% complete");
                }
                sb.AppendLine();
            }

            // Recent feedback sentiment (if available)
            if (ctx.RecentFeedbackSummary != null)
            {
                sb.AppendLine("## Recent Feedback");
                sb.AppendLine(ctx.RecentFeedbackSummary);
                sb.AppendLine();
            }

            sb.AppendLine("Provide insights focusing on what actions the manager should take today.");

            return sb.ToString();
        }

        private string BuildBriefingPrompt(DailyBriefing briefing)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Generate a morning briefing for a manager.");
            sb.AppendLine();
            sb.AppendLine($"Critical items: {briefing.CriticalInsights.Count}");
            foreach (var insight in briefing.CriticalInsights.Take(3))
            {
                sb.AppendLine($"- {insight.Title}");
            }
            sb.AppendLine($"Warnings: {briefing.WarningInsights.Count}");
            sb.AppendLine($"Info items: {briefing.InfoInsights.Count}");
            sb.AppendLine();
            sb.AppendLine("Write a friendly, brief summary highlighting what needs attention today.");
            return sb.ToString();
        }

        private string BuildMeetingPrepPrompt(
            TeamMember member,
            IEnumerable<Meeting> recentMeetings,
            IEnumerable<TrackerTask> openTasks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Prepare for 1:1 meeting with {member.FullName} ({member.JobTitle ?? "Team Member"})");
            sb.AppendLine();

            // Recent meeting notes
            var meetings = recentMeetings?.Take(3).ToList();
            if (meetings?.Any() == true)
            {
                sb.AppendLine("## Recent 1:1 Notes");
                foreach (var meeting in meetings)
                {
                    sb.AppendLine($"- {meeting.ScheduledAt:MMM d}: {meeting.Notes?.Substring(0, Math.Min(200, meeting.Notes?.Length ?? 0))}...");
                }
                sb.AppendLine();
            }

            // Open tasks
            var tasks = openTasks?.Take(5).ToList();
            if (tasks?.Any() == true)
            {
                sb.AppendLine("## Open Tasks");
                foreach (var task in tasks)
                {
                    var status = task.DueDate < DateTime.Now ? " (OVERDUE)" : "";
                    sb.AppendLine($"- {task.Description}{status}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Generate talking points for this 1:1.");
            return sb.ToString();
        }

        private List<Insight> ParseAIResponse(string response)
        {
            var insights = new List<Insight>();

            try
            {
                // Try to extract JSON from the response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = JsonSerializer.Deserialize<AIInsightResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed?.Insights != null)
                    {
                        foreach (var aiInsight in parsed.Insights)
                        {
                            insights.Add(new Insight
                            {
                                UniqueKey = $"ai_{Guid.NewGuid():N}",
                                Type = ParseInsightType(aiInsight.Type),
                                Severity = ParseSeverity(aiInsight.Severity),
                                Title = aiInsight.Title ?? "AI Insight",
                                Description = aiInsight.Description ?? "",
                                ActionSuggestion = aiInsight.SuggestedAction ?? "",
                                EntityType = aiInsight.RelatedEntityType,
                                GeneratedAt = DateTime.Now,
                                IsRead = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to parse AI response JSON");
            }

            return insights;
        }

        private InsightType ParseInsightType(string? type) => type?.ToLower() switch
        {
            "meeting_gap" => InsightType.MeetingGap,
            "task_overdue" => InsightType.TaskOverdue,
            "goal_risk" or "goal_at_risk" or "okr_risk" or "okr_at_risk" => InsightType.GoalAtRisk,
            "metric_gap" or "metric_off_target" or "kpi_gap" or "kpi_off_target" => InsightType.MetricOffTarget,
            "recognition_needed" or "kudos" => InsightType.Recommendation,
            "followup_needed" or "stale_action" => InsightType.StaleActionItem,
            "birthday" => InsightType.UpcomingBirthday,
            "anniversary" => InsightType.UpcomingAnniversary,
            "survey" => InsightType.SurveyAlert,
            _ => InsightType.MeetingGap // Default
        };

        private InsightSeverity ParseSeverity(string? severity) => severity?.ToLower() switch
        {
            "critical" => InsightSeverity.Critical,
            "warning" => InsightSeverity.Medium,
            "high" => InsightSeverity.High,
            "low" => InsightSeverity.Low,
            "info" => InsightSeverity.Info,
            _ => InsightSeverity.Info
        };

        #endregion

        #region Inner Classes

        private class AIInsightResponse
        {
            public List<AIInsightItem>? Insights { get; set; }
        }

        private class AIInsightItem
        {
            public string? Type { get; set; }
            public string? Severity { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? SuggestedAction { get; set; }
            public string? RelatedEntityType { get; set; }
            public string? RelatedEntityId { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// Context data for AI insight generation.
    /// </summary>
    public class TeamDataContext
    {
        public List<TeamMember>? TeamMembers { get; set; }
        public List<TrackerTask>? OverdueTasks { get; set; }
        public List<Goal>? AtRiskGoals { get; set; }
        public string? RecentFeedbackSummary { get; set; }
    }
}
