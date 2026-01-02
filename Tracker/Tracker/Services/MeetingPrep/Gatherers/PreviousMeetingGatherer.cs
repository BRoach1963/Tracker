using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers data from previous 1:1 meetings including action items and follow-ups.
    /// </summary>
    public class PreviousMeetingGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Previous Meeting Gatherer";
        public PrepSectionType SectionType => PrepSectionType.FollowUp;
        public bool IsEnabled { get; set; } = true;

        public PreviousMeetingGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("PreviousMeetingGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.FollowUp);
            var settings = GetSettings();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping previous meeting data");
                    return null;
                }

                // Get previous meetings with this team member
                var meetings = await dbManager.GetMeetingsForTeamMemberAsync(teamMember.Id);
                if (meetings == null || meetings.Count == 0)
                {
                    section.Items.Add(new PrepItem
                    {
                        Title = "No previous meetings found",
                        Subtext = "This will be the first recorded 1:1",
                        Priority = PrepItemPriority.Low
                    });
                    return section;
                }

                // Get the most recent completed meeting before the upcoming one
                var lastMeeting = meetings
                    .Where(m => m.Date.Date < meetingDate.Date && m.Status == MeetingStatusEnum.Completed)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                if (lastMeeting == null)
                {
                    section.Items.Add(new PrepItem
                    {
                        Title = "No completed previous meetings",
                        Priority = PrepItemPriority.Low
                    });
                    return section;
                }

                // Add meeting context
                var daysSince = (meetingDate.Date - lastMeeting.Date.Date).Days;
                section.Description = $"Last meeting: {lastMeeting.Date:MMM d, yyyy} ({daysSince} days ago)";

                // Check for open action items (MeetingTasks)
                if (lastMeeting.Tasks != null && lastMeeting.Tasks.Any())
                {
                    var openTasks = lastMeeting.Tasks.Where(t => !t.IsCompleted).ToList();
                    var completedTasks = lastMeeting.Tasks.Where(t => t.IsCompleted).ToList();

                    // Add open action items first (higher priority)
                    foreach (var task in openTasks)
                    {
                        var isOverdue = task.DueDate != DateTime.MinValue && task.DueDate.Date < DateTime.Today;
                        section.Items.Add(new PrepItem
                        {
                            Title = task.Description,
                            Subtext = isOverdue 
                                ? $"⚠️ Overdue since {task.DueDate:MMM d}" 
                                : task.DueDate != DateTime.MinValue 
                                    ? $"Due {task.DueDate:MMM d}" 
                                    : "No due date",
                            Priority = isOverdue ? PrepItemPriority.Critical : PrepItemPriority.High,
                            LinkType = PrepItemLinkType.MeetingTask,
                            LinkId = task.Id,
                            Icon = isOverdue ? "Warning" : "Clock"
                        });
                    }

                    // Optionally show completed items
                    if (settings.ShowCompletedActionItems && completedTasks.Any())
                    {
                        foreach (var task in completedTasks.Take(3))
                        {
                            section.Items.Add(new PrepItem
                            {
                                Title = $"✅ {task.Description}",
                                Subtext = "Completed",
                                Priority = PrepItemPriority.Low,
                                LinkType = PrepItemLinkType.MeetingTask,
                                LinkId = task.Id
                            });
                        }
                    }
                }

                // Check agenda items that might need follow-up
                if (lastMeeting.AgendaItems != null && lastMeeting.AgendaItems.Any())
                {
                    var discussedItems = lastMeeting.AgendaItems
                        .Where(a => a.Category == AgendaItemCategory.Blocker || 
                                   a.Category == AgendaItemCategory.Concern)
                        .Take(3);

                    foreach (var item in discussedItems)
                    {
                        section.Items.Add(new PrepItem
                        {
                            Title = $"Follow up: {item.Description}",
                            Subtext = $"{item.Category} from last meeting",
                            Priority = PrepItemPriority.Normal,
                            Icon = "Comment"
                        });
                    }
                }

                // Add notes context if any
                if (!string.IsNullOrWhiteSpace(lastMeeting.Notes))
                {
                    var notesSummary = lastMeeting.Notes.Length > 100 
                        ? lastMeeting.Notes.Substring(0, 100) + "..." 
                        : lastMeeting.Notes;
                    
                    section.Items.Add(new PrepItem
                    {
                        Title = "Notes from last meeting",
                        Description = notesSummary,
                        Priority = PrepItemPriority.Low,
                        Icon = "Document",
                        LinkType = PrepItemLinkType.Meeting,
                        LinkId = lastMeeting.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering previous meeting data: {0}", ex.Message);
            }

            return section.HasItems ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
