using System.Collections.ObjectModel;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.DataWrappers
{
    public class OneOnOneWrapper : BaseDataWrapper
    {
        #region Fields

        private OneOnOne? _data;
        private ObservableCollection<AgendaItem> _agendaItems = new();
        private ObservableCollection<MeetingTask> _tasks = new();

        #endregion

        #region Ctor

        public OneOnOneWrapper(OneOnOne? data = null)
        {
            _data = data ?? new OneOnOne();
            if (_data.AgendaItems != null)
                _agendaItems = new ObservableCollection<AgendaItem>(_data.AgendaItems);
            if (_data.Tasks != null)
                _tasks = new ObservableCollection<MeetingTask>(_data.Tasks);
        }

        #endregion

        #region Public Properties

        public int Id => _data.Id;

        public OneOnOne Data => _data;

        public ObservableCollection<AgendaItem> AgendaItems => _agendaItems;

        public ObservableCollection<MeetingTask> Tasks => _tasks;

        public string Description
        {
            get => _data.Description;
            set
            {
                _data.Description = value;
                RaisePropertyChanged();
            }
        }

        public string Feedback
        {
            get => _data.Feedback;
            set
            {
                _data.Feedback = value;
                RaisePropertyChanged();
            }
        }

        public string Notes
        {
            get => _data.Notes;
            set
            {
                _data.Notes = value;
                RaisePropertyChanged();
            }
        }

        public TeamMember TeamMember
        {
            get => _data.TeamMember;
            set
            {
                _data.TeamMember = value;
                RaisePropertyChanged();
            }
        }

        public string TeamMemberName => _data.TeamMemberName;

        public MeetingStatusEnum Status => _data.Status;

        public bool IsRecurring
        {
            get => _data.IsRecurring;
            set
            {
                _data.IsRecurring = value;
                RaisePropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _data.Date;
            set
            {
                _data.Date = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan StartTime
        {
            get => _data.StartTime;
            set
            {
                _data.StartTime = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan Duration
        {
            get => _data.Duration;
            set
            {
                _data.Duration = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
