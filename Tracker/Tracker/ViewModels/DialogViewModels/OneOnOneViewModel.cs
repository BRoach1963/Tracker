using System.Collections.ObjectModel;
using System.Globalization;
using Tracker.Common;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.ViewModels.DialogViewModels
{
    public class OneOnOneViewModel : BaseDialogViewModel
    {
        #region Fields

        private OneOnOne? _data;
        private ObservableCollection<ActionItem> _actionItems = new();
        private ObservableCollection<ObjectiveKeyResult> _keyResults = new();
        private ObservableCollection<FollowUpItem> _followUpItems = new();
        private ObservableCollection<string> _discussionPoints = new();
        private ObservableCollection<string> _concerns = new();

        private bool _inEditMode;

        private Dictionary<string, object> _changedProperties = new();

        #endregion

        public OneOnOneViewModel(Action? callback, OneOnOne data, bool edit = true, TeamMember? teamMember = null) : base(callback)
        {
            _inEditMode = edit;
            _data = data;
            if (teamMember != null && !_inEditMode) _data.TeamMember = teamMember;
            SetLists();
        }

        #region Public Properties

        public int Id => _data.Id;

        public OneOnOne Data => _data;

        public ObservableCollection<ActionItem> ActionItems => _actionItems;

        public ObservableCollection<ObjectiveKeyResult> KeyResults => _keyResults;

        public ObservableCollection<FollowUpItem> FollowUpItems => _followUpItems;

        public ObservableCollection<string> DiscussionPoints => _discussionPoints;

        public ObservableCollection<string> Concerns => _concerns;

        public string Description
        {
            get => _data.Description;
            set
            {
                _data.Description = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneDescription, value);
            }
        }

        public string Agenda
        {
            get => _data.Agenda;
            set
            {
                _data.Agenda = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneAgenda, value);
            }
        }

        public string Feedback
        {
            get => _data.Feedback;
            set
            {
                _data.Feedback = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneFeedback, value);
            }
        }

        public string Notes
        {
            get => _data.Notes;
            set
            {
                _data.Notes = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneNotes, value);
            }
        }

        public TeamMember TeamMember
        {
            get => _data.TeamMember;
            set
            {
                _data.TeamMember = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneTeamMemberId, _data.TeamMember.Id);
            }
        }

        public string TeamMemberName => _data.TeamMemberName;

        public MeetingStatusEnum Status
        {
            get => _data.Status;
            set
            {
                _data.Status = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneStatus, value);
            }
        }

        public bool IsRecurring
        {
            get => _data.IsRecurring;
            set
            {
                _data.IsRecurring = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneIsRecurring, value);
            }
        }

        public DateTime Date
        {
            get => _data.Date;
            set
            {
                _data.Date = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DateDisplay));
                UpdateChangedValues(TrackerConstants.OneOnOneDate, value);
            }
        }

        public TimeSpan StartTime
        {
            get => _data.StartTime;
            set
            {
                _data.StartTime = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneStartTime, _data.StartTime.ToString(@"hh\:mm\:ss"));
            }
        }

        public TimeSpan EndTime
        {
            get => _data.EndTime;
            set
            {
                _data.EndTime = value;
                RaisePropertyChanged();
                Duration = EndTime - StartTime;
            }
        }

        public string DateDisplay
        {
            get => _data.Date == new DateTime(1900, 1, 1) ? "MM/DD/YYYY" : _data.Date.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.Date = date;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Date));
                UpdateChangedValues(TrackerConstants.TeamMemberHireDate, _data.Date);
            }
        }

        public TimeSpan Duration
        {
            get => _data.Duration;
            set
            {
                _data.Duration = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneDuration, value);
                UpdateChangedValues(TrackerConstants.OneOnOneStartTime, _data.Duration.ToString(@"hh\:mm\:ss"));
            }
        }

        #endregion

        #region PrivateMethods


        private void SetLists()
        {
            if (_inEditMode)
            {
                Date = DateTime.Now.Date;
                StartTime = DateTime.Now.TimeOfDay;
                EndTime = DateTime.Now.TimeOfDay + new TimeSpan(0,1,0,0);
            }
        }

        private void UpdateChangedValues(string key, object? value)
        {
            if (value == null)
            {
                _changedProperties.Remove(key);
            }
            else
            {
                _changedProperties[key] = value;
            }
        }

        #endregion

    }
}
