using System.Windows.Input;
using Tracker.Command;
using Tracker.Controls;
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
        private readonly Guid _teamMemberId;

        private ICommand? _saveCommand;
        private string _selectedSentiment;

        #endregion

        #region Ctor

        public FeedbackViewModel(Action? callback, Feedback? data, Guid teamMemberId, bool edit = false) : base(callback)
        {
            _teamMemberId = teamMemberId;
            _inEditMode = edit;
            
            if (data != null && edit)
            {
                _data = data;
                _selectedSentiment = data.Sentiment;
            }
            else
            {
                _data = new Feedback
                {
                    ToTeamMemberId = teamMemberId,
                    Sentiment = "positive"
                };
                _selectedSentiment = "positive";
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

        public string[] Sentiments => new[] { "positive", "neutral", "constructive" };

        public string SelectedSentiment
        {
            get => _selectedSentiment;
            set
            {
                _selectedSentiment = value;
                _data.Sentiment = value;
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

        public string? ContextType
        {
            get => _data.ContextType;
            set
            {
                _data.ContextType = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private bool CanExecuteSave(object? obj)
        {
            return !string.IsNullOrWhiteSpace(Content);
        }

        private async void SaveExecuted(object? parameter)
        {
            bool success;
            
            if (_inEditMode)
            {
                success = await TrackerDataManager.Instance.UpdateFeedback(_data);
                if (success)
                {
                    NotificationManager.Instance.ShowSuccess("Feedback Updated", "Feedback has been updated.");
                }
            }
            else
            {
                var id = await TrackerDataManager.Instance.AddFeedback(_data);
                success = id > 0;
                if (success)
                {
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

