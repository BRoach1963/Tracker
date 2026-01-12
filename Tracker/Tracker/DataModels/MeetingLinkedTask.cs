using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links an existing task to a Meeting for discussion tracking.
    /// This is a ViewModel-level helper class for the OneOnOne dialog.
    /// </summary>
    public class MeetingLinkedTask : INotifyPropertyChanged
    {
        private Guid _meetingId;
        private Guid _taskId;
        private TrackerTask? _task;
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
        /// Task being linked.
        /// </summary>
        public Guid TaskId
        {
            get => _taskId;
            set { _taskId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Navigation property to the task.
        /// </summary>
        public TrackerTask? Task
        {
            get => _task;
            set { _task = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Notes from discussing this task in the meeting.
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
