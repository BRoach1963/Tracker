using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing projects.
    /// 
    /// Key responsibilities:
    /// - Expose project properties for data binding
    /// - Provide team member selection for project ownership
    /// - Handle project creation and updates via commands
    /// - Track property changes for edit mode
    /// 
    /// Usage:
    /// <code>
    /// var vm = new NewProjectViewModel(callback, new Project());
    /// var dialog = new AddProjectDialog(vm);
    /// </code>
    /// </summary>
    public class NewProjectViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly Project _data;
        private readonly bool _inEditMode;

        private ICommand? _addProjectCommand;
        private ICommand? _updateProjectCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<TeamMember> _selectedTeamMembers = new();
        private TeamMember? _selectedOwner;

        private readonly Dictionary<string, object> _changedProperties = new();

        // Status options for the project
        private ObservableCollection<string> _statusOptions = new()
        {
            "Not Started",
            "Planning",
            "In Progress",
            "On Hold",
            "Completed",
            "Cancelled"
        };
        private string _selectedStatus = "Not Started";

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewProjectViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="data">The project data to edit or a new project instance.</param>
        /// <param name="edit">True if editing an existing project, false for new project.</param>
        public NewProjectViewModel(Action? callback, Project data, bool edit = false) : base(callback)
        {
            _data = data;
            _inEditMode = edit;

            // Set defaults for new projects
            if (!_inEditMode)
            {
                _data.StartDate = DateTime.Now;
                _data.EndDate = DateTime.Now.AddMonths(3); // Default 3-month duration
                _data.Status = "Not Started";
            }
            else
            {
                _selectedStatus = _data.Status;
            }

            LoadTeamMembers();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            _selectedTeamMembers.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new project to the database.
        /// </summary>
        public ICommand AddProjectCommand => _addProjectCommand ??=
            new TrackerCommand(AddProjectExecuted, CanExecuteAddProject);

        /// <summary>
        /// Command to update an existing project in the database.
        /// </summary>
        public ICommand UpdateProjectCommand => _updateProjectCommand ??=
            new TrackerCommand(UpdateProjectExecuted, CanExecuteUpdateProject);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying project data model.
        /// </summary>
        public Project Data => _data;

        /// <summary>
        /// Gets whether the ViewModel is in edit mode (true) or add mode (false).
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of available team members for owner/team selection.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        /// <summary>
        /// Gets the collection of team members assigned to this project.
        /// </summary>
        public ObservableCollection<TeamMember> SelectedTeamMembers => _selectedTeamMembers;

        /// <summary>
        /// Gets the available status options for the project.
        /// </summary>
        public ObservableCollection<string> StatusOptions => _statusOptions;

        /// <summary>
        /// Gets or sets the selected project status.
        /// </summary>
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                _data.Status = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Status", value);
            }
        }

        /// <summary>
        /// Gets or sets the selected owner for the project.
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
        /// Gets or sets the project name.
        /// </summary>
        public string Name
        {
            get => _data.Name;
            set
            {
                _data.Name = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Name", value);
            }
        }

        /// <summary>
        /// Gets or sets the project description.
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
        /// Gets or sets the project start date.
        /// </summary>
        public DateTime StartDate
        {
            get => _data.StartDate;
            set
            {
                _data.StartDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartDateDisplay));
                UpdateChangedValues("@StartDate", value);
            }
        }

        /// <summary>
        /// Gets or sets the start date as a formatted display string.
        /// </summary>
        public string StartDateDisplay
        {
            get => _data.StartDate == DateTime.MinValue ? "MM/DD/YYYY" : _data.StartDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _data.StartDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(StartDate));
                    UpdateChangedValues("@StartDate", date);
                }
            }
        }

        /// <summary>
        /// Gets or sets the project end date.
        /// </summary>
        public DateTime? EndDate
        {
            get => _data.EndDate;
            set
            {
                _data.EndDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EndDateDisplay));
                UpdateChangedValues("@EndDate", value);
            }
        }

        /// <summary>
        /// Gets or sets the end date as a formatted display string.
        /// </summary>
        public string EndDateDisplay
        {
            get => _data.EndDate == null || _data.EndDate == DateTime.MinValue 
                ? "MM/DD/YYYY" 
                : _data.EndDate.Value.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _data.EndDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(EndDate));
                    UpdateChangedValues("@EndDate", date);
                }
            }
        }

        /// <summary>
        /// Gets or sets the project budget.
        /// </summary>
        public decimal Budget
        {
            get => _data.Budget == decimal.MinValue ? 0 : _data.Budget;
            set
            {
                _data.Budget = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Budget", value);
            }
        }

        /// <summary>
        /// Gets or sets the project status.
        /// </summary>
        public string Status
        {
            get => _data.Status;
            set
            {
                _data.Status = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Status", value);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads available team members for owner and team member selection.
        /// </summary>
        private void LoadTeamMembers()
        {
            _teamMembers.Clear();
            _selectedTeamMembers.Clear();

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

            // Load existing team members if editing
            if (_inEditMode && _data.TeamMembers != null)
            {
                foreach (var member in _data.TeamMembers)
                {
                    var existing = _teamMembers.FirstOrDefault(t => t.Id == member.Id);
                    if (existing != null)
                    {
                        _selectedTeamMembers.Add(existing);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a new project can be added.
        /// Requires at least a name and an owner.
        /// </summary>
        private bool CanExecuteAddProject(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add project command - saves the new project to the database.
        /// </summary>
        private async void AddProjectExecuted(object? parameter)
        {
            // Copy selected team members to the project
            _data.TeamMembers = _selectedTeamMembers.ToList();

            var id = await TrackerDataManager.Instance.AddProject(_data);
            if (id > 0)
            {
                _data.ID = id;
                NotificationManager.Instance.ShowSuccess("Project Created", $"Project '{Name}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create project.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the project can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateProject(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update project command - saves changes to the database.
        /// </summary>
        private async void UpdateProjectExecuted(object? parameter)
        {
            // Copy selected team members to the project
            _data.TeamMembers = _selectedTeamMembers.ToList();

            var success = await TrackerDataManager.Instance.UpdateProject(_data);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("Project Updated", $"Project '{Name}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update project.");
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
