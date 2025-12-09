using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Interfaces;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing individual tasks.
    /// 
    /// Key responsibilities:
    /// - Expose task properties for data binding
    /// - Provide team member selection for task ownership
    /// - Handle task creation and updates via commands
    /// - Track property changes for edit mode
    /// 
    /// Usage:
    /// <code>
    /// var vm = new NewTaskViewModel(callback, new IndividualTask());
    /// var dialog = new AddTaskDialog(vm);
    /// </code>
    /// </summary>
    public class NewTaskViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly IndividualTask _data;
        private readonly bool _inEditMode;

        private ICommand? _addTaskCommand;
        private ICommand? _updateTaskCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private TeamMember? _selectedOwner;

        private readonly Dictionary<string, object> _changedProperties = new();

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewTaskViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="data">The task data to edit or a new task instance.</param>
        /// <param name="edit">True if editing an existing task, false for new task.</param>
        public NewTaskViewModel(Action? callback, ITask data, bool edit = false) : base(callback)
        {
            _data = data as IndividualTask ?? new IndividualTask();
            _inEditMode = edit;

            // Set defaults for new tasks
            if (!_inEditMode)
            {
                _data.DueDate = DateTime.Now.AddDays(7); // Default due date is one week from now
            }

            LoadTeamMembers();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new task to the database.
        /// </summary>
        public ICommand AddTaskCommand => _addTaskCommand ??=
            new TrackerCommand(AddTaskExecuted, CanExecuteAddTask);

        /// <summary>
        /// Command to update an existing task in the database.
        /// </summary>
        public ICommand UpdateTaskCommand => _updateTaskCommand ??=
            new TrackerCommand(UpdateTaskExecuted, CanExecuteUpdateTask);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying task data model.
        /// </summary>
        public IndividualTask Data => _data;

        /// <summary>
        /// Gets whether the ViewModel is in edit mode (true) or add mode (false).
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of available team members for owner selection.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        /// <summary>
        /// Gets or sets the selected owner for the task.
        /// </summary>
        public TeamMember? SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                _selectedOwner = value;
                if (value != null)
                {
                    _data.Owner = value;
                    UpdateChangedValues("@OwnerId", value.Id);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the task description.
        /// </summary>
        public string Description
        {
            get => _data.Description;
            set
            {
                _data.Description = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Description", value);
            }
        }

        /// <summary>
        /// Gets or sets the task due date.
        /// </summary>
        public DateTime DueDate
        {
            get => _data.DueDate;
            set
            {
                _data.DueDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DueDateDisplay));
                UpdateChangedValues("@DueDate", value);
            }
        }

        /// <summary>
        /// Gets or sets the due date as a formatted display string.
        /// </summary>
        public string DueDateDisplay
        {
            get => _data.DueDate == DateTime.MinValue ? "MM/DD/YYYY" : _data.DueDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _data.DueDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(DueDate));
                    UpdateChangedValues("@DueDate", date);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the task is completed.
        /// </summary>
        public bool IsCompleted
        {
            get => _data.IsCompleted;
            set
            {
                _data.IsCompleted = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@IsCompleted", value);
            }
        }

        /// <summary>
        /// Gets the task status (Completed/Incomplete) - read-only, derived from IsCompleted.
        /// </summary>
        public string Status => _data.Status;

        /// <summary>
        /// Gets or sets additional notes for the task.
        /// </summary>
        public string Notes
        {
            get => _data.Notes;
            set
            {
                _data.Notes = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Notes", value);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads available team members for owner selection.
        /// </summary>
        private void LoadTeamMembers()
        {
            _teamMembers.Clear();
            var members = TrackerDataManager.Instance.TeamMembers;
            if (members != null)
            {
                foreach (var member in members.Where(m => m.IsActive))
                {
                    _teamMembers.Add(member);
                }
            }

            // Set selected owner if editing
            if (_inEditMode && _data.Owner?.Id > 0)
            {
                _selectedOwner = _teamMembers.FirstOrDefault(t => t.Id == _data.Owner.Id);
            }
        }

        /// <summary>
        /// Determines whether a new task can be added.
        /// Requires at least a description and an owner.
        /// </summary>
        private bool CanExecuteAddTask(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Description)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add task command - saves the new task to the database.
        /// </summary>
        private async void AddTaskExecuted(object? parameter)
        {
            var id = await TrackerDataManager.Instance.AddTask(_data);
            if (id > 0)
            {
                _data.Id = id;
                NotificationManager.Instance.ShowSuccess("Task Created", $"Task '{Description}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create task.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the task can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateTask(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update task command - saves changes to the database.
        /// </summary>
        private async void UpdateTaskExecuted(object? parameter)
        {
            var success = await TrackerDataManager.Instance.UpdateTask(_data);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("Task Updated", $"Task '{Description}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update task.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Tracks property changes for enabling/disabling the update command.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The new value.</param>
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
