using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DTOs;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers Goal progress data to highlight goals at risk or needing attention.
    /// Goals represent organizational, team, and personal objectives with associated targets.
    /// </summary>
    public class GoalGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Goal Progress Gatherer";
        public PrepSectionType SectionType => PrepSectionType.GoalProgress;
        public bool IsEnabled { get; set; } = true;

        public GoalGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("GoalGatherer");
        }

        public Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.GoalProgress);
            var settings = GetSettings();

            try
            {
                // Get goals for this team member from TrackerDataManager
                var allGoals = TrackerDataManager.Instance.Goals.ToList();
                var allTargets = TrackerDataManager.Instance.Targets.ToList();
                
                var userGoals = allGoals
                    .Where(g => !g.IsDeleted && g.CreatedByUserId == teamMember.Id)
                    .OrderByDescending(g => g.Status == GoalStatus.OffTrack)
                    .ThenByDescending(g => g.Status == GoalStatus.AtRisk)
                    .ToList();

                // Associate targets with goals
                foreach (var goal in userGoals)
                {
                    goal.Targets = allTargets.Where(t => t.GoalId == goal.Id && !t.IsDeleted).ToList();
                }

                if (!userGoals.Any())
                {
                    return Task.FromResult<PrepSection?>(null);
                }

                // Process goals
                foreach (var goal in userGoals.Take(settings.MaxItemsPerSection))
                {
                    var progress = goal.ProgressPercent;
                    var status = goal.Status;
                    
                    // Determine priority based on status and progress
                    var priority = PrepItemPriority.Normal;
                    var statusText = "";
                    
                    switch (status)
                    {
                        case GoalStatus.AtRisk:
                            priority = PrepItemPriority.High;
                            statusText = "⚠️ At Risk";
                            break;
                        case GoalStatus.OffTrack:
                            priority = PrepItemPriority.Critical;
                            statusText = "Off track";
                            break;
                        case GoalStatus.OnTrack:
                            statusText = "On track";
                            break;
                        default:
                            statusText = status.ToString();
                            break;
                    }

                    // Count active targets
                    var activeTargets = goal.Targets?.Where(t => !t.IsDeleted).Count() ?? 0;
                    var atRiskTargets = goal.Targets?.Where(t => !t.IsDeleted && (t.Status == GoalStatus.OffTrack || t.Status == GoalStatus.AtRisk)).Count() ?? 0;

                    var subtext = $"{progress:F0}% • {statusText}";
                    if (activeTargets > 0)
                    {
                        subtext += $" • {activeTargets} targets";
                    }

                    section.Items.Add(new PrepItem
                    {
                        Title = goal.Title,
                        Subtext = subtext,
                        Description = goal.Description,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Okr,
                        LinkId = goal.Id,
                        Icon = status == GoalStatus.AtRisk || status == GoalStatus.OffTrack ? "Warning" : "Target"
                    });
                }

                // Update section description
                var atRiskCount = userGoals.Count(g => g.Status == GoalStatus.AtRisk || g.Status == GoalStatus.OffTrack);
                
                if (atRiskCount > 0)
                {
                    section.Description = $"{atRiskCount} goals at risk";
                    section.IsExpanded = true; // Auto-expand if there are issues
                }
                else
                {
                    section.Description = $"{userGoals.Count} active goals";
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering goal data: {0}", ex.Message);
            }

            return Task.FromResult<PrepSection?>(section.HasItems ? section : null);
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
