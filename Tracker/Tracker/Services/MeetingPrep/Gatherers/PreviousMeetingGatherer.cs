using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Database.Repositories;
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
                var repository = CreateMeetingRepository();
                if (repository == null)
                {
                    _logger.Debug("No current user context, skipping previous meeting data");
                    return null;
                }

                // Get previous meetings with this team member
                var meetings = await repository.GetMeetingsAsync();
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
                    .Where(m => m.ScheduledAt.Date < meetingDate.Date && m.Status == MeetingStatus.Completed)
                    .OrderByDescending(m => m.ScheduledAt)
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
                var daysSince = (meetingDate.Date - lastMeeting.ScheduledAt.Date).Days;
                section.Description = $"Last meeting: {lastMeeting.ScheduledAt:MMM d, yyyy} ({daysSince} days ago)";

                // Check for open action items (MeetingTasks)
                if (lastMeeting.Tasks != null && lastMeeting.Tasks.Any())
                {
                    var openTasks = lastMeeting.Tasks.Where(t => !t.IsCompleted).ToList();
                    var completedTasks = lastMeeting.Tasks.Where(t => t.IsCompleted).ToList();

                    // Add open action items first (higher priority)
                    foreach (var task in openTasks)
                    {
                        var isOverdue = task.DueDate.HasValue && task.DueDate.Value.Date < DateTime.Today;
                        section.Items.Add(new PrepItem
                        {
                            Title = task.Description,
                            Subtext = isOverdue 
                                ? $"⚠️ Overdue since {task.DueDate.Value:MMM d}" 
                                : task.DueDate.HasValue 
                                    ? $"Due {task.DueDate.Value:MMM d}" 
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
                    // Show items that haven't been discussed yet
                    var undiscussedItems = lastMeeting.AgendaItems
                        .Where(a => !a.IsDiscussed)
                        .Take(3);

                    foreach (var item in undiscussedItems)
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

        private static MeetingRepository? CreateMeetingRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new MeetingRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
