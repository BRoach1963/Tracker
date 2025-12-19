using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing feedback entries.
    /// </summary>
    public class FeedbackViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly Feedback _data;
        private readonly bool _inEditMode;
        private readonly int _teamMemberId;

        private ICommand? _saveCommand;
        private FeedbackType _selectedType;

        #endregion

        #region Ctor

        public FeedbackViewModel(Action? callback, Feedback? data, int teamMemberId, bool edit = false) : base(callback)
        {
            _teamMemberId = teamMemberId;
            _inEditMode = edit;
            
            if (data != null && edit)
            {
                _data = data;
                _selectedType = data.Type;
            }
            else
            {
                _data = new Feedback
                {
                    TeamMemberId = teamMemberId,
                    Date = DateTime.Now,
                    Type = FeedbackType.Positive
                };
                _selectedType = FeedbackType.Positive;
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand => _saveCommand ??=
            new TrackerCommand(SaveExecuted, CanExecuteSave);

        #endregion

        #region Public Properties

        public Feedback Data => _data;
        public bool InEditMode => _inEditMode;

        public Array FeedbackTypes => Enum.GetValues(typeof(FeedbackType));

        public FeedbackType SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                _data.Type = value;
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

        public string Title
        {
            get => _data.Title;
            set
            {
                _data.Title = value;
                RaisePropertyChanged();
            }
        }

        public string Content
        {
            get => _data.Content;
            set
            {
                _data.Content = value;
                RaisePropertyChanged();
            }
        }

        public string Context
        {
            get => _data.Context;
            set
            {
                _data.Context = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private bool CanExecuteSave(object? obj)
        {
            return !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content);
        }

        private async void SaveExecuted(object? parameter)
        {
            bool success;
            
            if (_inEditMode)
            {
                success = await TrackerDbManager.Instance!.UpdateFeedbackAsync(_data);
                if (success)
                {
                    NotificationManager.Instance.ShowSuccess("Feedback Updated", "Feedback has been updated.");
                }
            }
            else
            {
                var id = await TrackerDbManager.Instance!.AddFeedbackAsync(_data);
                success = id > 0;
                if (success)
                {
                    _data.Id = id;
                    NotificationManager.Instance.ShowSuccess("Feedback Added", "Feedback has been recorded.");
                }
            }

            if (!success)
            {
                NotificationManager.Instance.ShowError("Error", "Failed to save feedback.");
                return;
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        #endregion
    }
}

