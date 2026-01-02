using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers OKR and KPI progress data to highlight goals at risk or needing attention.
    /// </summary>
    public class OkrKpiGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "OKR/KPI Gatherer";
        public PrepSectionType SectionType => PrepSectionType.GoalProgress;
        public bool IsEnabled { get; set; } = true;

        public OkrKpiGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("OkrKpiGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.GoalProgress);
            var settings = GetSettings();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping OKR/KPI data");
                    return null;
                }

                // Get OKRs - filter by Owner and active status
                var allOkrs = await dbManager.GetOkrsAsync();
                var memberOkrs = allOkrs?
                    .Where(o => o.Owner?.Id == teamMember.Id && o.IsActive)
                    .ToList() ?? new List<ObjectiveKeyResult>();

                // Get KPIs - filter by Owner
                var allKpis = await dbManager.GetKPIsAsync();
                var memberKpis = allKpis?
                    .Where(k => k.Owner?.Id == teamMember.Id)
                    .ToList() ?? new List<KeyPerformanceIndicator>();

                if (!memberOkrs.Any() && !memberKpis.Any())
                {
                    return null;
                }

                // Process OKRs
                foreach (var okr in memberOkrs.Take(settings.MaxItemsPerSection))
                {
                    var progress = okr.CompletionPercentage;
                    var status = okr.Status;
                    
                    // Determine priority based on status and progress
                    var priority = PrepItemPriority.Normal;
                    var statusText = "";
                    
                    switch (status)
                    {
                        case ObjectiveStatusEnum.AtRisk:
                            priority = PrepItemPriority.High;
                            statusText = "⚠️ At Risk";
                            break;
                        case ObjectiveStatusEnum.OffTrack:
                            priority = PrepItemPriority.Critical;
                            statusText = "Off track";
                            break;
                        case ObjectiveStatusEnum.OnTrack:
                            statusText = "On track";
                            break;
                        default:
                            statusText = status.ToString();
                            break;
                    }

                    section.Items.Add(new PrepItem
                    {
                        Title = okr.Title,
                        Subtext = $"{progress:F0}% • {statusText}",
                        Description = okr.Description,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Okr,
                        LinkId = okr.ObjectiveId,
                        Icon = status == ObjectiveStatusEnum.AtRisk || status == ObjectiveStatusEnum.OffTrack ? "Warning" : "Target"
                    });
                }

                // Process KPIs
                foreach (var kpi in memberKpis.Take(Math.Max(0, settings.MaxItemsPerSection - memberOkrs.Count)))
                {
                    var currentValue = kpi.Value;
                    var targetValue = kpi.TargetValue;
                    var achievement = kpi.PercentComplete;
                    
                    // Determine if KPI needs attention based on Status
                    var priority = PrepItemPriority.Normal;
                    var statusText = "";
                    
                    switch (kpi.Status)
                    {
                        case KpiStatusEnum.OffTarget:
                            priority = PrepItemPriority.Critical;
                            statusText = "⚠️ Off Target";
                            break;
                        case KpiStatusEnum.CloseToTarget:
                            priority = PrepItemPriority.High;
                            statusText = "Close to target";
                            break;
                        case KpiStatusEnum.OnTarget:
                            statusText = "On target";
                            break;
                        default:
                            statusText = $"{achievement:F0}% of target";
                            break;
                    }

                    section.Items.Add(new PrepItem
                    {
                        Title = kpi.Name,
                        Subtext = $"{currentValue:N0}/{targetValue:N0} • {statusText}",
                        Description = kpi.Description,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Kpi,
                        LinkId = kpi.KpiId,
                        Icon = kpi.Status == KpiStatusEnum.OffTarget ? "Warning" : "BarChart"
                    });
                }

                // Update section description
                var atRiskCount = memberOkrs.Count(o => o.Status == ObjectiveStatusEnum.AtRisk || o.Status == ObjectiveStatusEnum.OffTrack);
                var offTargetKpis = memberKpis.Count(k => k.Status == KpiStatusEnum.OffTarget);
                
                if (atRiskCount > 0 || offTargetKpis > 0)
                {
                    section.Description = $"{atRiskCount} OKRs at risk • {offTargetKpis} KPIs off target";
                    section.IsExpanded = true; // Auto-expand if there are issues
                }
                else
                {
                    section.Description = $"{memberOkrs.Count} OKRs • {memberKpis.Count} KPIs";
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering OKR/KPI data: {0}", ex.Message);
            }

            return section.HasItems ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
