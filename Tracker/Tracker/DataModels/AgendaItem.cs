using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tracker.DataModels
{
    /// <summary>
    /// Agenda item for a meeting - topics, questions, items to discuss.
    /// Maps to meeting_agenda_items table in Supabase.
    /// </summary>
    public class AgendaItem : AuditableEntity, INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string? _notes;
        private int _sortOrder = 0;
        private bool _isDiscussed = false;
        private DateTime? _discussedAt;
        private int? _timeEstimateMinutes;
        private int? _actualDurationMinutes;
        private string? _relatedEntityType;
        private Guid? _relatedEntityId;

        /// <summary>
        /// Unique identifier for this agenda item (UUID).
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// FK to the meeting this item belongs to.
        /// </summary>
        public Guid MeetingId { get; set; }

        /// <summary>
        /// FK to the team member who added this item.
        /// Null if added by system or unknown.
        /// </summary>
        public Guid? AddedByTeamMemberId { get; set; }

        /// <summary>
        /// The agenda item title/topic to discuss.
        /// </summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Additional notes or context for this agenda item.
        /// </summary>
        public string? Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Sort order within the meeting agenda.
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this item was discussed in the meeting.
        /// </summary>
        public bool IsDiscussed
        {
            get => _isDiscussed;
            set { _isDiscussed = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When this item was discussed.
        /// Null if not yet discussed.
        /// </summary>
        public DateTime? DiscussedAt
        {
            get => _discussedAt;
            set { _discussedAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Estimated time to discuss this item (minutes).
        /// Null if no estimate.
        /// </summary>
        public int? TimeEstimateMinutes
        {
            get => _timeEstimateMinutes;
            set { _timeEstimateMinutes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Actual time spent discussing this item (minutes).
        /// Null if not yet discussed.
        /// </summary>
        public int? ActualDurationMinutes
        {
            get => _actualDurationMinutes;
            set { _actualDurationMinutes = value; OnPropertyChanged(); }
        }

        #region Related Entity (for discussing existing Tasks/Goals/Metrics)

        /// <summary>
        /// Type of related entity being discussed (Task, Goal, Metric).
        /// Null if this is a standalone agenda item.
        /// </summary>
        public string? RelatedEntityType
        {
            get => _relatedEntityType;
            set { _relatedEntityType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// ID of the related entity being discussed.
        /// Null if this is a standalone agenda item.
        /// </summary>
        public Guid? RelatedEntityId
        {
            get => _relatedEntityId;
            set { _relatedEntityId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this agenda item is linked to an existing entity.
        /// </summary>
        public bool HasRelatedEntity => RelatedEntityId.HasValue && !string.IsNullOrEmpty(RelatedEntityType);

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether this item is ready to be discussed (has a title and hasn't been discussed yet).
        /// </summary>
        public bool IsPending => !IsDiscussed && !string.IsNullOrWhiteSpace(Title);

        /// <summary>
        /// Time variance - how the actual duration compares to estimate.
        /// Returns the difference in minutes, null if no estimate or not discussed.
        /// </summary>
        public int? TimeVarianceMinutes
        {
            get
            {
                if (!TimeEstimateMinutes.HasValue || !ActualDurationMinutes.HasValue)
                    return null;
                return ActualDurationMinutes.Value - TimeEstimateMinutes.Value;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
