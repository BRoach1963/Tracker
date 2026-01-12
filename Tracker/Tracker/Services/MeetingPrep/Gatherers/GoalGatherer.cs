using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Microsoft.EntityFrameworkCore;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers Goal progress data to highlight goals at risk or needing attention.
    /// Goals represent organizational, team, and personal objectives with associated targets.
    /// </summary>
    public class GoalGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;
        private readonly TrackerDbContext _context;

        public string Name => "Goal Progress Gatherer";
        public PrepSectionType SectionType => PrepSectionType.GoalProgress;
        public bool IsEnabled { get; set; } = true;

        public GoalGatherer(TrackerDbContext context)
        {
            _context = context;
            _logger = LoggingManager.GetComponentLogger("GoalGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.GoalProgress);
            var settings = GetSettings();

            try
            {
                if (_context == null)
                {
                    _logger.Debug("Database context not available, skipping goal data");
                    return null;
                }

                // Get goals for this team member, ordered by status (at risk first)
                var userGoals = await _context.Goals
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Where(g => !g.IsDeleted && g.CreatedByUserId == teamMember.Id)
                    .OrderByDescending(g => g.Status == OkrStatus.OffTrack)
                    .ThenByDescending(g => g.Status == OkrStatus.AtRisk)
                    .ToListAsync();

                if (!userGoals.Any())
                {
                    return null;
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
                        case OkrStatus.AtRisk:
                            priority = PrepItemPriority.High;
                            statusText = "⚠️ At Risk";
                            break;
                        case OkrStatus.OffTrack:
                            priority = PrepItemPriority.Critical;
                            statusText = "Off track";
                            break;
                        case OkrStatus.OnTrack:
                            statusText = "On track";
                            break;
                        default:
                            statusText = status.ToString();
                            break;
                    }

                    // Count active targets
                    var activeTargets = goal.Targets?.Where(t => !t.IsDeleted).Count() ?? 0;
                    var atRiskTargets = goal.Targets?.Where(t => !t.IsDeleted && (t.Status == OkrStatus.OffTrack || t.Status == OkrStatus.AtRisk)).Count() ?? 0;

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
                        LinkId = goal.Id.GetHashCode(), // Convert Guid to int for compatibility
                        Icon = status == OkrStatus.AtRisk || status == OkrStatus.OffTrack ? "Warning" : "Target"
                    });
                }

                // Update section description
                var atRiskCount = userGoals.Count(g => g.Status == OkrStatus.AtRisk || g.Status == OkrStatus.OffTrack);
                
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

            return section.HasItems ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
