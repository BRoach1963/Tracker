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
        public ObservableCollection<OneOnOne> Meetings { get; set; } = new();
        
        /// <summary>
        /// Display text for when the last meeting was held.
        /// </summary>
        public string LastMeetingDisplay
        {
            get
            {
                var lastCompleted = Meetings
                    .Where(m => m.Status == Common.Enums.MeetingStatusEnum.Completed)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();
                
                if (lastCompleted == null)
                    return "Never";
                
                var days = (DateTime.Today - lastCompleted.Date.Date).Days;
                
                return days switch
                {
                    0 => "Today",
                    1 => "Yesterday",
                    < 7 => $"{days} days ago",
                    < 14 => "1 week ago",
                    < 21 => "2 weeks ago",
                    < 30 => "3 weeks ago",
                    _ => lastCompleted.Date.ToString("MMM dd")
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
                    .Where(m => m.Status == Common.Enums.MeetingStatusEnum.Scheduled && m.Date >= DateTime.Today)
                    .OrderBy(m => m.Date)
                    .FirstOrDefault();
                
                if (nextScheduled == null)
                    return null;
                
                var days = (nextScheduled.Date.Date - DateTime.Today).Days;
                
                return days switch
                {
                    0 => "Today",
                    1 => "Tomorrow",
                    < 7 => nextScheduled.Date.ToString("dddd"),
                    _ => nextScheduled.Date.ToString("MMM dd")
                };
            }
        }
    }
}

