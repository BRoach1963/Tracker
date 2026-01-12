using System.Collections.ObjectModel;
using Tracker.DataModels;

namespace Tracker.ViewModels
{
    /// <summary>
    /// Represents a team member and their 1:1 meetings for grouped display.
    /// </summary>
    public class TeamMemberMeetingGroup
    {
        public TeamMember TeamMember { get; set; } = null!;
        public ObservableCollection<Meeting> Meetings { get; set; } = new();
        
        /// <summary>
        /// Display text for when the last meeting was held.
        /// </summary>
        public string LastMeetingDisplay
        {
            get
            {
                var lastCompleted = Meetings
                    .Where(m => m.Status == Common.Enums.MeetingStatus.Completed)
                    .OrderByDescending(m => m.ScheduledAt)
                    .FirstOrDefault();
                
                if (lastCompleted == null)
                    return "Never";
                
                var days = (DateTime.Today - lastCompleted.ScheduledAt.Date).Days;
                
                return days switch
                {
                    0 => "Today",
                    1 => "Yesterday",
                    < 7 => $"{days} days ago",
                    < 14 => "1 week ago",
                    < 21 => "2 weeks ago",
                    < 30 => "3 weeks ago",
                    _ => lastCompleted.ScheduledAt.ToString("MMM dd")
                };
            }
        }
        
        /// <summary>
        /// Display text for the next scheduled meeting.
        /// </summary>
        public string? NextMeetingDisplay
        {
            get
            {
                var nextScheduled = Meetings
                    .Where(m => m.Status == Common.Enums.MeetingStatus.Scheduled && m.ScheduledAt >= DateTime.Today)
                    .OrderBy(m => m.ScheduledAt)
                    .FirstOrDefault();
                
                if (nextScheduled == null)
                    return null;
                
                var days = (nextScheduled.ScheduledAt.Date - DateTime.Today).Days;
                
                return days switch
                {
                    0 => "Today",
                    1 => "Tomorrow",
                    < 7 => nextScheduled.ScheduledAt.ToString("dddd"),
                    _ => nextScheduled.ScheduledAt.ToString("MMM dd")
                };
            }
        }
    }
}

