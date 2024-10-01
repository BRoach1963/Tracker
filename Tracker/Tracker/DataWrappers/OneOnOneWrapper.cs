using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.DataWrappers
{
    public class OneOnOneWrapper : BaseDataWrapper
    {
        #region Fields

        private OneOnOne? _data;
        private ObservableCollection<ActionItem> _actionItems = new();
        private ObservableCollection<ObjectiveKeyResult> _keyResults = new();
        private ObservableCollection<FollowUpItem> _followUpItems = new();
        private ObservableCollection<string> _discussionPoints = new();
        private ObservableCollection<string> _concerns = new();

        #endregion

        #region Ctor

        public OneOnOneWrapper(OneOnOne? data = null)
        {
            _data = data ?? new OneOnOne();
        }

        #endregion

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
            }
        }

        public string Agenda
        {
            get => _data.Agenda;
            set
            {
                _data.Agenda = value;
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
