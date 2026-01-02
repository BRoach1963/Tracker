using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.MeetingPrep.Gatherers
{
    /// <summary>
    /// Gathers current task status including overdue tasks, upcoming deadlines, and blockers.
    /// </summary>
    public class TaskDataGatherer : IMeetingPrepGatherer
    {
        private readonly ILogger _logger;

        public string Name => "Task Data Gatherer";
        public PrepSectionType SectionType => PrepSectionType.TaskStatus;
        public bool IsEnabled { get; set; } = true;

        public TaskDataGatherer()
        {
            _logger = LoggingManager.GetComponentLogger("TaskDataGatherer");
        }

        public async Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate)
        {
            var section = PrepSection.Create(PrepSectionType.TaskStatus);
            var urgentSection = PrepSection.Create(PrepSectionType.Urgent);
            var settings = GetSettings();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping task data");
                    return null;
                }

                // Get all tasks
                var allTasks = await dbManager.GetTasksAsync();
                if (allTasks == null || allTasks.Count == 0)
                {
                    return null;
                }

                // Filter tasks assigned to this team member
                var memberTasks = allTasks
                    .Where(t => t.Owner?.Id == teamMember.Id && !t.IsCompleted)
                    .ToList();

                if (!memberTasks.Any())
                {
                    section.Items.Add(new PrepItem
                    {
                        Title = "No open tasks",
                        Subtext = "All tasks completed",
                        Priority = PrepItemPriority.Low
                    });
                    return section;
                }

                var today = DateTime.Today;
                var cutoffDate = today.AddDays(-settings.ShowOverdueTasksMaxDays);
                var weekAhead = today.AddDays(7);

                // Categorize tasks - DueDate is not nullable in IndividualTask
                var overdueTasks = memberTasks
                    .Where(t => t.DueDate.Date < today && t.DueDate.Date >= cutoffDate)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                var dueThisWeek = memberTasks
                    .Where(t => t.DueDate.Date >= today && t.DueDate.Date <= weekAhead)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                // Tasks with MinValue DueDate are considered "no due date"
                var noDueDate = memberTasks
                    .Where(t => t.DueDate.Date == DateTime.MinValue.Date)
                    .Take(3)
                    .ToList();

                // Add overdue tasks (high priority)
                foreach (var task in overdueTasks.Take(settings.MaxItemsPerSection))
                {
                    var daysOverdue = (today - task.DueDate.Date).Days;
                    var priority = daysOverdue > 7 ? PrepItemPriority.Critical : PrepItemPriority.High;
                    
                    section.Items.Add(new PrepItem
                    {
                        Title = task.Description,
                        Subtext = $"⚠️ Overdue by {daysOverdue} day{(daysOverdue != 1 ? "s" : "")}",
                        Description = task.Notes,
                        Priority = priority,
                        LinkType = PrepItemLinkType.Task,
                        LinkId = task.Id,
                        Icon = "Warning"
                    });
                }

                // Add tasks due this week
                foreach (var task in dueThisWeek.Take(settings.MaxItemsPerSection - overdueTasks.Count()))
                {
                    var daysUntil = (task.DueDate.Date - today).Days;
                    var subtext = daysUntil == 0 
                        ? "Due TODAY" 
                        : daysUntil == 1 
                            ? "Due tomorrow" 
                            : $"Due in {daysUntil} days";
                    
                    section.Items.Add(new PrepItem
                    {
                        Title = task.Description,
                        Subtext = subtext,
                        Description = task.Notes,
                        Priority = daysUntil <= 2 ? PrepItemPriority.High : PrepItemPriority.Normal,
                        LinkType = PrepItemLinkType.Task,
                        LinkId = task.Id,
                        Icon = "Clock"
                    });
                }

                // Add tasks without due dates (if room)
                if (section.Items.Count < settings.MaxItemsPerSection && noDueDate.Any())
                {
                    foreach (var task in noDueDate.Take(settings.MaxItemsPerSection - section.Items.Count))
                    {
                        section.Items.Add(new PrepItem
                        {
                            Title = task.Description,
                            Subtext = "No due date set",
                            Description = task.Notes,
                            Priority = PrepItemPriority.Low,
                            LinkType = PrepItemLinkType.Task,
                            LinkId = task.Id,
                            Icon = "Task"
                        });
                    }
                }

                // Update section description with summary
                var totalOpen = memberTasks.Count;
                var overdueCount = overdueTasks.Count();
                section.Description = overdueCount > 0
                    ? $"{totalOpen} open tasks • {overdueCount} overdue"
                    : $"{totalOpen} open tasks";
            }
            catch (Exception ex)
            {
                _logger.Error("Error gathering task data: {0}", ex.Message);
            }

            return section.HasItems ? section : null;
        }

        private MeetingPrepSettings GetSettings()
        {
            return UserSettingsManager.Instance?.Settings?.MeetingPrep ?? new MeetingPrepSettings();
        }
    }
}
