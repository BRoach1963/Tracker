using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links an existing Goal (formerly OKR) to a Meeting for discussion tracking.
    /// This is a ViewModel-level helper class for the OneOnOne dialog.
    /// </summary>
    public class MeetingLinkedGoal : INotifyPropertyChanged
    {
        private Guid _meetingId;
        private Guid _goalId;
        private Goal? _goal;
        private string _discussionNotes = string.Empty;
        private bool _isDeleted;

        /// <summary>
        /// Primary key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Meeting this link belongs to.
        /// </summary>
        public Guid MeetingId
        {
            get => _meetingId;
            set { _meetingId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Goal being linked.
        /// </summary>
        public Guid GoalId
        {
            get => _goalId;
            set { _goalId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Navigation property to the goal.
        /// </summary>
        public Goal? Goal
        {
            get => _goal;
            set { _goal = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Notes from discussing this goal in the meeting.
        /// </summary>
        public string DiscussionNotes
        {
            get => _discussionNotes;
            set { _discussionNotes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted
        {
            get => _isDeleted;
            set { _isDeleted = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
