using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing development goals.
    /// </summary>
    public class GoalViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("GoalVM");
        private readonly DevelopmentGoal _data;
        private readonly bool _inEditMode;
        private readonly Guid _teamMemberId;

        private ICommand? _saveCommand;
        private ICommand? _addMilestoneCommand;
        private ICommand? _removeMilestoneCommand;

        private DevelopmentGoalCategory _selectedCategory;
        private DevelopmentGoalStatus _selectedStatus;
        private ObservableCollection<DevelopmentGoalMilestone> _milestones = new();

        private string _newMilestoneDescription = string.Empty;

        #endregion

        #region Ctor

        public GoalViewModel(Action? callback, DevelopmentGoal? data, Guid teamMemberId, bool edit = false) : base(callback)
        {
            _teamMemberId = teamMemberId;
            _inEditMode = edit;
            
            if (data != null && edit)
            {
                _data = data;
                _selectedCategory = data.Category;
                _selectedStatus = data.Status;
                
                // Load existing milestones
                foreach (var m in data.Milestones)
                {
                    _milestones.Add(m);
                }
            }
            else
            {
                _data = new DevelopmentGoal
                {
                    TeamMemberId = teamMemberId,
                    Status = DevelopmentGoalStatus.Draft,
                    Category = DevelopmentGoalCategory.SkillDevelopment,
                    TargetDate = DateTime.Now.AddMonths(3)
                };
                _selectedCategory = DevelopmentGoalCategory.SkillDevelopment;
                _selectedStatus = DevelopmentGoalStatus.Draft;
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand => _saveCommand ??=
            new TrackerCommand(SaveExecuted, CanExecuteSave);

        public ICommand AddMilestoneCommand => _addMilestoneCommand ??=
            new TrackerCommand(AddMilestoneExecuted, CanAddMilestone);

        public ICommand RemoveMilestoneCommand => _removeMilestoneCommand ??=
            new TrackerCommand(RemoveMilestoneExecuted);

        #endregion

        #region Public Properties

        public DevelopmentGoal Data => _data;
        public bool InEditMode => _inEditMode;

        public Array GoalCategories => Enum.GetValues(typeof(DevelopmentGoalCategory));
        public Array GoalStatuses => Enum.GetValues(typeof(DevelopmentGoalStatus));

        public DevelopmentGoalCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                _data.Category = value;
                RaisePropertyChanged();
            }
        }

        public DevelopmentGoalStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                _data.Status = value;
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

        public string? Description
        {
            get => _data.Description;
            set
            {
                _data.Description = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? TargetDate
        {
            get => _data.TargetDate;
            set
            {
                _data.TargetDate = value;
                RaisePropertyChanged();
            }
        }

        public int ProgressPercent
        {
            get => _data.ProgressPercent;
            set
            {
                _data.ProgressPercent = Math.Clamp(value, 0, 100);
                RaisePropertyChanged();
            }
        }

        public string? WhyImportant
        {
            get => _data.WhyImportant;
            set
            {
                _data.WhyImportant = value;
                RaisePropertyChanged();
            }
        }

        public string? SuccessCriteria
        {
            get => _data.SuccessCriteria;
            set
            {
                _data.SuccessCriteria = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<DevelopmentGoalMilestone> Milestones => _milestones;

        public string NewMilestoneDescription
        {
            get => _newMilestoneDescription;
            set
            {
                _newMilestoneDescription = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private bool CanExecuteSave(object? obj)
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private async void SaveExecuted(object? parameter)
        {
            // Sync milestones to data
            _data.Milestones = _milestones.ToList();
            
            bool success;
            
            if (_inEditMode)
            {
                success = await TrackerDbManager.Instance!.UpdateDevelopmentGoalAsync(_data);
                if (success)
                {
                    NotificationManager.Instance.ShowSuccess("Goal Updated", "Goal has been updated.");
                }
            }
            else
            {
                var id = await TrackerDbManager.Instance!.AddDevelopmentGoalAsync(_data);
                success = id != Guid.Empty;
                if (success)
                {
                    _data.Id = id;
                    NotificationManager.Instance.ShowSuccess("Goal Created", "Goal has been created.");
                }
            }

            if (!success)
            {
                NotificationManager.Instance.ShowError("Error", "Failed to save goal.");
                return;
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        private bool CanAddMilestone(object? obj)
        {
            return !string.IsNullOrWhiteSpace(NewMilestoneDescription);
        }

        private void AddMilestoneExecuted(object? parameter)
        {
            var milestone = new DevelopmentGoalMilestone
            {
                Title = NewMilestoneDescription,
                Status = MilestoneStatus.NotStarted,
                GoalId = _data.Id,
                SortOrder = _milestones.Count
            };
            
            _milestones.Add(milestone);
            
            // Reset input
            NewMilestoneDescription = string.Empty;
        }

        private void RemoveMilestoneExecuted(object? parameter)
        {
            if (parameter is DevelopmentGoalMilestone milestone)
            {
                _milestones.Remove(milestone);
            }
        }

        #endregion
    }
}

