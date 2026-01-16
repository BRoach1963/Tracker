using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Agenda item for a meeting - topics, questions, items to discuss.
    /// Maps to: meeting_agenda_items (12 columns)
    /// </summary>
    [Table("meeting_agenda_items")]
    public class AgendaItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string? _notes;
        private int _sortOrder = 0;
        private bool _isDiscussed = false;
        private DateTime? _discussedAt;
        private int? _timeEstimateMinutes;
        private int? _actualDurationMinutes;

        /// <summary>
        /// Unique identifier (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// FK to the meeting this item belongs to.
        /// Maps to: meeting_id UUID NOT NULL
        /// </summary>
        [Column("meeting_id")]
        public Guid MeetingId { get; set; }

        /// <summary>
        /// FK to the team member who added this item.
        /// Maps to: added_by_team_member_id UUID NULL
        /// </summary>
        [Column("added_by_team_member_id")]
        public Guid? AddedByTeamMemberId { get; set; }

        /// <summary>
        /// The agenda item title/topic to discuss.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Additional notes or context for this agenda item.
        /// Maps to: notes TEXT NULL
        /// </summary>
        [Column("notes")]
        public string? Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Sort order within the meeting agenda.
        /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether this item was discussed in the meeting.
        /// Maps to: is_discussed BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_discussed")]
        public bool IsDiscussed
        {
            get => _isDiscussed;
            set { _isDiscussed = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When this item was discussed.
        /// Maps to: discussed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("discussed_at")]
        public DateTime? DiscussedAt
        {
            get => _discussedAt;
            set { _discussedAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Estimated time to discuss this item (minutes).
        /// Maps to: time_estimate_minutes INT4 NULL
        /// </summary>
        [Column("time_estimate_minutes")]
        public int? TimeEstimateMinutes
        {
            get => _timeEstimateMinutes;
            set { _timeEstimateMinutes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Actual time spent discussing this item (minutes).
        /// Maps to: actual_duration_minutes INT4 NULL
        /// </summary>
        [Column("actual_duration_minutes")]
        public int? ActualDurationMinutes
        {
            get => _actualDurationMinutes;
            set { _actualDurationMinutes = value; OnPropertyChanged(); }
        }

        #region Timestamps

        /// <summary>
        /// When this record was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Navigation to the meeting.
        /// </summary>
        [NotMapped]
        public Meeting? Meeting { get; set; }

        /// <summary>
        /// Navigation to the team member who added this item.
        /// </summary>
        [NotMapped]
        public TeamMember? AddedByTeamMember { get; set; }

        #endregion

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Description/additional context for the agenda item (alias for Notes).
        /// </summary>
        [NotMapped]
        public string? Description
        {
            get => Notes;
            set => Notes = value;
        }

        /// <summary>
        /// Category of the agenda item.
        /// </summary>
        [NotMapped]
        public string? Category { get; set; }

        #region Computed Properties

        /// <summary>
        /// Whether this item is ready to be discussed (has a title and hasn't been discussed yet).
        /// </summary>
        [NotMapped]
        public bool IsPending => !IsDiscussed && !string.IsNullOrWhiteSpace(Title);

        /// <summary>
        /// Time variance - how the actual duration compares to estimate.
        /// Returns the difference in minutes, null if no estimate or not discussed.
        /// </summary>
        [NotMapped]
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
