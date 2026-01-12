using System.Text;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.MeetingPrep.Gatherers;

namespace Tracker.Services.MeetingPrep
{
    /// <summary>
    /// Service that orchestrates meeting prep generation.
    /// Coordinates multiple data gatherers to build a comprehensive prep package.
    /// </summary>
    public class MeetingPrepService
    {
        #region Fields

        private static MeetingPrepService? _instance;
        private static readonly object _lock = new();
        private readonly ILogger _logger;
        private readonly List<IMeetingPrepGatherer> _gatherers = new();
        private readonly GeminiChatService? _aiService;

        #endregion

        #region Singleton

        /// <summary>
        /// Singleton instance of MeetingPrepService.
        /// </summary>
        public static MeetingPrepService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new MeetingPrepService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constructor

        private MeetingPrepService()
        {
            _logger = LoggingManager.GetComponentLogger("MeetingPrepService");
            
            // Initialize AI service if available
            try
            {
                _aiService = new GeminiChatService();
            }
            catch (Exception ex)
            {
                _logger.Warn("AI service not available for meeting prep: {0}", ex.Message);
            }

            // Register default gatherers
            RegisterDefaultGatherers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a meeting prep package for an upcoming 1:1.
        /// </summary>
        /// <param name="meeting">The meeting to prepare for.</param>
        /// <returns>A populated MeetingPrep object.</returns>
        public async Task<DataModels.MeetingPrep> GeneratePrepAsync(Meeting meeting)
        {
            if (meeting == null)
                throw new ArgumentNullException(nameof(meeting));

            _logger.Info("Generating meeting prep for 1:1 with {0} on {1}", 
                meeting.Report?.FullName ?? "Unknown", meeting.ScheduledAt.ToShortDateString());

            var prep = new DataModels.MeetingPrep
            {
                MeetingId = meeting.Id.GetHashCode(), // Convert Guid to int for compatibility
                TeamMember = meeting.Report, // Use Report for 1:1 meetings
                MeetingDate = meeting.ScheduledAt,
                GeneratedAt = DateTime.Now
            };

            var settings = GetSettings();

            if (!settings.IsEnabled)
            {
                _logger.Debug("Meeting prep is disabled in settings");
                return prep;
            }

            // Run all gatherers in parallel
            var tasks = _gatherers
                .Where(g => g.IsEnabled)
                .Select(g => RunGathererAsync(g, meeting.Report, meeting.ScheduledAt))
                .ToList();

            var sections = await Task.WhenAll(tasks);

            // Add non-null sections to the prep
            foreach (var section in sections.Where(s => s != null))
            {
                prep.Sections.Add(section!);
            }

            // Calculate statistics
            CalculateStatistics(prep, meeting.Report);

            // Sort all items within sections
            prep.SortAllItems();

            // Limit items per section
            prep.LimitItemsPerSection(settings.MaxItemsPerSection);

            // Remove empty sections
            prep.PruneEmptySections();

            // Generate AI suggestions if enabled
            if (settings.EnableAiSuggestions && _aiService?.IsAvailable == true)
            {
                try
                {
                    prep.AiSuggestedAgenda = await GenerateAiSuggestionsAsync(prep);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Failed to generate AI suggestions: {0}", ex.Message);
                }
            }

            _logger.Info("Meeting prep generated: {0} sections, {1} total items", 
                prep.Sections.Count, prep.TotalItemCount);

            return prep;
        }

        /// <summary>
        /// Generates a meeting prep package for a team member and date.
        /// </summary>
        public async Task<DataModels.MeetingPrep> GeneratePrepAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var meeting = new Meeting
            {
                Id = Guid.Empty,
                Report = teamMember,
                ScheduledAt = meetingDate,
                Type = MeetingType.OneOnOne
            };

            return await GeneratePrepAsync(meeting);
        }

        /// <summary>
        /// Registers a custom data gatherer.
        /// </summary>
        public void RegisterGatherer(IMeetingPrepGatherer gatherer)
        {
            if (!_gatherers.Contains(gatherer))
            {
                _gatherers.Add(gatherer);
                _logger.Debug("Registered gatherer: {0}", gatherer.Name);
            }
        }

        /// <summary>
        /// Gets the registered gatherers.
        /// </summary>
        public IReadOnlyList<IMeetingPrepGatherer> Gatherers => _gatherers.AsReadOnly();

        #endregion

        #region Private Methods

        private void RegisterDefaultGatherers()
        {
            RegisterGatherer(new PreviousMeetingGatherer());
            RegisterGatherer(new TaskDataGatherer());
            RegisterGatherer(new GoalGatherer(new TrackerDbContext()));
            RegisterGatherer(new PersonalDatesGatherer());
            RegisterGatherer(new SurveyDataGatherer());
            RegisterGatherer(new FeedbackGatherer());

            _logger.Info("Registered {0} default meeting prep gatherers", _gatherers.Count);
        }

        private async Task<PrepSection?> RunGathererAsync(
            IMeetingPrepGatherer gatherer, 
            TeamMember teamMember, 
            DateTime meetingDate)
        {
            try
            {
                _logger.Debug("Running gatherer: {0}", gatherer.Name);
                return await gatherer.GatherAsync(teamMember, meetingDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Gatherer {0} failed: {1}", gatherer.Name, ex.Message);
                return null;
            }
        }

        private void CalculateStatistics(DataModels.MeetingPrep prep, TeamMember teamMember)
        {
            // Count overdue tasks
            var taskSection = prep.Sections.FirstOrDefault(s => s.Type == PrepSectionType.TaskStatus);
            if (taskSection != null)
            {
                prep.OverdueTaskCount = taskSection.Items
                    .Count(i => i.Priority >= PrepItemPriority.High && 
                               i.Subtext?.Contains("Overdue") == true);
            }

            // Count open action items from follow-ups
            var followUpSection = prep.Sections.FirstOrDefault(s => s.Type == PrepSectionType.FollowUp);
            if (followUpSection != null)
            {
                prep.OpenActionItemCount = followUpSection.Items
                    .Count(i => i.LinkType == PrepItemLinkType.MeetingTask && !i.IsAddedToAgenda);
            }

            // Count OKRs at risk
            var goalSection = prep.Sections.FirstOrDefault(s => s.Type == PrepSectionType.GoalProgress);
            if (goalSection != null)
            {
                prep.OkrsAtRiskCount = goalSection.Items
                    .Count(i => i.LinkType == PrepItemLinkType.Okr && 
                               i.Priority >= PrepItemPriority.High);
            }

            // Calculate days since last meeting
            if (teamMember.LastOneOnOneDate.HasValue)
            {
                prep.DaysSinceLastMeeting = (DateTime.Today - teamMember.LastOneOnOneDate.Value.Date).Days;
            }
        }

        private async Task<string> GenerateAiSuggestionsAsync(DataModels.MeetingPrep prep)
        {
            if (_aiService == null || !_aiService.IsAvailable)
            {
                return string.Empty;
            }

            var prompt = BuildAiPrompt(prep);
            
            var response = await _aiService.GetResponseAsync(
                $"Generate 3-5 specific, actionable agenda items for this 1:1. Be concise and practical.\n\n{prompt}",
                systemContext: "You are a management assistant helping prepare for 1:1 meetings. Keep suggestions brief and actionable."
            );

            return response ?? string.Empty;
        }

        private string BuildAiPrompt(DataModels.MeetingPrep prep)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Preparing for 1:1 with {prep.TeamMember.FullName}");
            sb.AppendLine($"Role: {prep.TeamMember.JobTitle}");
            sb.AppendLine($"Tenure: {prep.TeamMember.Tenure}");
            sb.AppendLine($"Days since last 1:1: {prep.DaysSinceLastMeeting}");
            sb.AppendLine();

            foreach (var section in prep.Sections.Where(s => s.HasItems))
            {
                sb.AppendLine($"## {section.Title}");
                foreach (var item in section.Items.Take(5))
                {
                    sb.AppendLine($"- {item.Title}");
                    if (!string.IsNullOrEmpty(item.Subtext))
                    {
                        sb.AppendLine($"  ({item.Subtext})");
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }

        #endregion
    }
}
