using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes meeting cadence and generates insights when team members
    /// haven't had a 1:1 in too long.
    /// </summary>
    public class MeetingCadenceAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "Meeting Cadence Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] { InsightType.MeetingGap };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Days without a 1:1 before generating a warning.
        /// </summary>
        public int WarningThresholdDays { get; set; } = 14;

        /// <summary>
        /// Days without a 1:1 before generating a critical alert.
        /// </summary>
        public int CriticalThresholdDays { get; set; } = 21;

        public MeetingCadenceAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("MeetingCadenceAnalyzer");

            // Load thresholds from settings if available
            var settings = UserSettingsManager.Instance?.Settings?.Insights;
            if (settings != null)
            {
                WarningThresholdDays = settings.MeetingGapWarningDays;
                CriticalThresholdDays = settings.MeetingGapCriticalDays;
            }
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var teamMemberRepository = CreateTeamMemberRepository();
                if (teamMemberRepository == null)
                {
                    _logger.Debug("No current user or database context available, skipping meeting cadence analysis");
                    return insights;
                }

                // Get all active team members
                var teamMembers = await teamMemberRepository.GetTeamMembersAsync();
                if (teamMembers == null || teamMembers.Count == 0)
                {
                    _logger.Debug("No team members found");
                    return insights;
                }

                var today = DateTime.Now.Date;

                foreach (var member in teamMembers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Skip inactive team members
                    if (!member.IsActive)
                        continue;

                    // Check if they have a last 1:1 date
                    DateTime? lastMeetingDate = member.LastOneOnOneDate;

                    // If never had a 1:1, that's critical
                    if (!lastMeetingDate.HasValue)
                    {
                        insights.Add(CreateInsight(
                            member,
                            InsightSeverity.Critical,
                            $"Schedule first 1:1 with {member.FullName}",
                            $"You haven't had any 1:1 meetings recorded with {member.FullName}. Regular check-ins help build rapport and catch issues early.",
                            "Schedule 1:1",
                            null));
                        continue;
                    }

                    // Calculate days since last 1:1
                    var daysSinceLastMeeting = (today - lastMeetingDate.Value.Date).Days;

                    if (daysSinceLastMeeting >= CriticalThresholdDays)
                    {
                        insights.Add(CreateInsight(
                            member,
                            InsightSeverity.Critical,
                            $"{member.FullName} hasn't had a 1:1 in {daysSinceLastMeeting} days",
                            $"It's been {daysSinceLastMeeting} days since your last 1:1 with {member.FullName}. This exceeds the recommended cadence. Consider reaching out soon.",
                            "Schedule 1:1",
                            lastMeetingDate.Value));
                    }
                    else if (daysSinceLastMeeting >= WarningThresholdDays)
                    {
                        insights.Add(CreateInsight(
                            member,
                            InsightSeverity.Warning,
                            $"Check in with {member.FullName}",
                            $"It's been {daysSinceLastMeeting} days since your last 1:1 with {member.FullName}. Consider scheduling a check-in.",
                            "Schedule 1:1",
                            lastMeetingDate.Value));
                    }
                }

                _logger.Info("Meeting cadence analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during meeting cadence analysis");
            }

            return insights;
        }

        private static TeamMemberRepository? CreateTeamMemberRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new TeamMemberRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        private static Insight CreateInsight(TeamMember member, InsightSeverity severity, string title, string description, string action, DateTime? lastMeetingDate)
        {
            // Create unique key based on member and current month (so it resurfaces monthly if still an issue)
            var monthKey = DateTime.Now.ToString("yyyy-MM");
            var uniqueKey = $"meeting_gap_{member.Id}_{monthKey}";

            return new Insight
            {
                UniqueKey = uniqueKey,
                Type = InsightType.MeetingGap,
                Severity = severity,
                Title = title,
                Description = description,
                ActionSuggestion = action,
                EntityType = "TeamMember",
                TargetTeamMemberId = member.Id,
                GeneratedAt = DateTime.Now
            };
        }
    }
}
