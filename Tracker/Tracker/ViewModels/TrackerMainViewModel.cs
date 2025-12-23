using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Interfaces;
using Tracker.Managers;
using Tracker.MockData;

namespace Tracker.ViewModels
{
    /// <summary>
    /// Main ViewModel for the Tracker application.
    /// Manages all entity collections and their CRUD operations.
    /// </summary>
    public class TrackerMainViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<OneOnOne> _oneOnOnes = new();
        private ObservableCollection<ITask> _tasks = new();
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private ObservableCollection<Project> _projects = new();
        private ObservableCollection<OneOnOne> _selectedTeamMemberOneOnOneCollection = new();
        private ObservableCollection<Feedback> _feedbacks = new();
        private ObservableCollection<IndividualGoal> _goals = new();

        // Selected items
        private TeamMember? _teamMember;
        private TeamMemberWrapper? _selectedTeamMemberWrapper;
        private ITask? _selectedTask;
        private Project? _selectedProject;
        private ObjectiveKeyResult? _selectedOkr;
        private KeyPerformanceIndicator? _selectedKpi;
        private OneOnOne? _selectedOneOnOne;
        private Feedback? _selectedFeedback;
        private IndividualGoal? _selectedGoal;
        private TeamMember? _selectedMemberForMeetings;
        private Controls.MeetingTimeFilterEnum _meetingTimeFilter = Controls.MeetingTimeFilterEnum.Upcoming;

        // Team Member commands
        private ICommand? _editTeamMemberCommand;
        private ICommand? _deleteTeamMemberCommand;
        private ICommand? _addTeamMemberOneOnOneCommand;

        // Task commands
        private ICommand? _editTaskCommand;
        private ICommand? _deleteTaskCommand;

        // Project commands
        private ICommand? _editProjectCommand;
        private ICommand? _deleteProjectCommand;

        // OKR commands
        private ICommand? _editOkrCommand;
        private ICommand? _deleteOkrCommand;

        // KPI commands
        private ICommand? _editKpiCommand;
        private ICommand? _deleteKpiCommand;

        // OneOnOne commands
        private ICommand? _editOneOnOneCommand;
        private ICommand? _deleteOneOnOneCommand;

        // Feedback commands
        private ICommand? _addFeedbackCommand;
        private ICommand? _editFeedbackCommand;
        private ICommand? _deleteFeedbackCommand;

        // Goal commands
        private ICommand? _addGoalCommand;
        private ICommand? _editGoalCommand;
        private ICommand? _deleteGoalCommand;

        // Global commands
        private ICommand? _openSearchCommand;
        private ICommand? _openHelpBotCommand;
        private ICommand? _newItemCommand;

        // User info
        private string _userInitials = "?";
        private bool _hasUserAvatar;
        private System.Windows.Media.ImageSource? _userAvatarSource;

        #endregion

        #region Ctor

        public TrackerMainViewModel()
        {
            // Don't load data here - wait for window to be loaded
            // Data will be loaded in MainWindow.Loaded event
            SubscribeToMessages();
            
            // Set user initials from current user
            UpdateUserInitials();
        }

        private void UpdateUserInitials()
        {
            var displayName = UserSettingsManager.Instance.Settings.CurrentUser ?? "User";
            
            // Extract initials (first letter of each word, max 2)
            var parts = displayName.Split(new[] { ' ', '.', '\\', '@' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                _userInitials = $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                _userInitials = parts[0][..2].ToUpper();
            }
            else
            {
                _userInitials = "U";
            }
            
            // Load user avatar
            LoadUserAvatar();
        }

        private void LoadUserAvatar()
        {
            try
            {
                var profile = Services.Backend.SupabaseService.Instance.CurrentProfile;
                System.Diagnostics.Debug.WriteLine($"[Avatar] LoadUserAvatar called. Profile: {(profile != null ? "exists" : "null")}, AvatarUrl: '{profile?.AvatarUrl ?? "null"}'");
                
                if (profile?.AvatarUrl != null && !string.IsNullOrEmpty(profile.AvatarUrl))
                {
                    // Build full URL from stored relative path
                    var avatarUrl = profile.AvatarUrl;
                    if (!avatarUrl.StartsWith("http"))
                    {
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        avatarUrl = $"{Services.Backend.SupabaseConfig.ProjectUrl}/storage/v1/object/public/{Services.Backend.SupabaseConfig.AvatarBucket}/{profile.AvatarUrl}?t={timestamp}";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[Avatar] Loading from URL: {avatarUrl}");

                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new System.Uri(avatarUrl);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    UserAvatarSource = bitmap;
                    HasUserAvatar = true;
                    System.Diagnostics.Debug.WriteLine($"[Avatar] Successfully loaded avatar");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Avatar] No avatar URL, showing initials");
                    HasUserAvatar = false;
                    UserAvatarSource = null;
                }
            }
            catch (Exception ex)
            {
                // If image fails to load, don't show avatar
                System.Diagnostics.Debug.WriteLine($"[Avatar] Failed to load: {ex.Message}");
                HasUserAvatar = false;
                UserAvatarSource = null;
            }
        }

        /// <summary>
        /// Public method to refresh user avatar (called after avatar upload).
        /// </summary>
        public void RefreshUserAvatar()
        {
            LoadUserAvatar();
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeToMessages();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands - Team Members

        public ICommand TeamMemberEditCommand => _editTeamMemberCommand ??=
            new TrackerCommand(EditTeamMemberExecuted, CanEditTeamMemberExecute);

        public ICommand TeamMemberDeleteCommand => _deleteTeamMemberCommand ??=
            new AsyncCommand(DeleteTeamMemberAsync, CanDeleteTeamMember, nameof(TeamMemberDeleteCommand));

        public ICommand AddTeamMemberOneOnOneCommand => _addTeamMemberOneOnOneCommand ??=
            new TrackerCommand(AddTeamMemberOneOnOneExecuted, CanExecuteAddTeamMemberOneOnOne);

        #endregion

        #region Commands - Tasks

        public ICommand EditTaskCommand => _editTaskCommand ??=
            new TrackerCommand(EditTaskExecuted, CanEditTask);

        public ICommand DeleteTaskCommand => _deleteTaskCommand ??=
            new AsyncCommand(DeleteTaskAsync, CanDeleteTask, nameof(DeleteTaskCommand));

        #endregion

        #region Commands - Projects

        public ICommand EditProjectCommand => _editProjectCommand ??=
            new TrackerCommand(EditProjectExecuted, CanEditProject);

        public ICommand DeleteProjectCommand => _deleteProjectCommand ??=
            new AsyncCommand(DeleteProjectAsync, CanDeleteProject, nameof(DeleteProjectCommand));

        #endregion

        #region Commands - OKRs

        public ICommand EditOkrCommand => _editOkrCommand ??=
            new TrackerCommand(EditOkrExecuted, CanEditOkr);

        public ICommand DeleteOkrCommand => _deleteOkrCommand ??=
            new AsyncCommand(DeleteOkrAsync, CanDeleteOkr, nameof(DeleteOkrCommand));

        #endregion

        #region Commands - KPIs

        public ICommand EditKpiCommand => _editKpiCommand ??=
            new TrackerCommand(EditKpiExecuted, CanEditKpi);

        public ICommand DeleteKpiCommand => _deleteKpiCommand ??=
            new AsyncCommand(DeleteKpiAsync, CanDeleteKpi, nameof(DeleteKpiCommand));

        #endregion

        #region Commands - OneOnOnes

        public ICommand EditOneOnOneCommand => _editOneOnOneCommand ??=
            new TrackerCommand(EditOneOnOneExecuted, CanEditOneOnOne);

        public ICommand DeleteOneOnOneCommand => _deleteOneOnOneCommand ??=
            new AsyncCommand(DeleteOneOnOneAsync, CanDeleteOneOnOne, nameof(DeleteOneOnOneCommand));

        #endregion

        #region Commands - Feedback

        public ICommand AddFeedbackCommand => _addFeedbackCommand ??=
            new TrackerCommand(AddFeedbackExecuted);

        public ICommand EditFeedbackCommand => _editFeedbackCommand ??=
            new TrackerCommand(EditFeedbackExecuted, CanEditFeedback);

        public ICommand DeleteFeedbackCommand => _deleteFeedbackCommand ??=
            new AsyncCommand(DeleteFeedbackAsync, CanDeleteFeedback, nameof(DeleteFeedbackCommand));

        private void AddFeedbackExecuted(object? parameter)
        {
            DialogManager.Instance.LaunchDialogByType(DialogType.AddFeedback, false, async () =>
            {
                await RefreshFeedbacksAsync();
                RefreshFeedbackStatistics();
            });
        }

        private bool CanEditFeedback(object? parameter) => parameter is Feedback || _selectedFeedback != null;

        private void EditFeedbackExecuted(object? parameter)
        {
            var feedback = parameter as Feedback ?? _selectedFeedback;
            if (feedback != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddFeedback, true, async () =>
                {
                    await RefreshFeedbacksAsync();
                    RefreshFeedbackStatistics();
                }, feedback);
            }
        }

        private bool CanDeleteFeedback(object? parameter) => parameter is Feedback || _selectedFeedback != null;

        private async Task DeleteFeedbackAsync(object? parameter)
        {
            var feedback = parameter as Feedback ?? _selectedFeedback;
            if (feedback == null) return;

            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this feedback?\n\n\"{feedback.Title}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await TrackerDataManager.Instance.DeleteFeedbackAsync(feedback);
                _feedbacks.Remove(feedback);
                if (_selectedFeedback == feedback) SelectedFeedback = null;
                NotificationManager.Instance.ShowSuccess("Deleted", "Feedback has been removed.");
                RefreshFeedbackStatistics();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to delete feedback: {ex.Message}");
            }
        }

        private async Task RefreshFeedbacksAsync()
        {
            var feedbacks = await TrackerDataManager.Instance.GetFeedbacks();
            _feedbacks.Clear();
            foreach (var f in feedbacks) _feedbacks.Add(f);
        }

        #endregion

        #region Commands - Goals

        public ICommand AddGoalCommand => _addGoalCommand ??=
            new TrackerCommand(AddGoalExecuted);

        public ICommand EditGoalCommand => _editGoalCommand ??=
            new TrackerCommand(EditGoalExecuted, CanEditGoal);

        public ICommand DeleteGoalCommand => _deleteGoalCommand ??=
            new AsyncCommand(DeleteGoalAsync, CanDeleteGoal, nameof(DeleteGoalCommand));

        private void AddGoalExecuted(object? parameter)
        {
            DialogManager.Instance.LaunchDialogByType(DialogType.AddGoal, false, async () =>
            {
                await RefreshGoalsAsync();
                RefreshGoalStatistics();
            });
        }

        private bool CanEditGoal(object? parameter) => parameter is IndividualGoal || _selectedGoal != null;

        private void EditGoalExecuted(object? parameter)
        {
            var goal = parameter as IndividualGoal ?? _selectedGoal;
            if (goal != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddGoal, true, async () =>
                {
                    await RefreshGoalsAsync();
                    RefreshGoalStatistics();
                }, goal);
            }
        }

        private bool CanDeleteGoal(object? parameter) => parameter is IndividualGoal || _selectedGoal != null;

        private async Task DeleteGoalAsync(object? parameter)
        {
            var goal = parameter as IndividualGoal ?? _selectedGoal;
            if (goal == null) return;

            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this goal?\n\n\"{goal.Title}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await TrackerDataManager.Instance.DeleteGoalAsync(goal);
                _goals.Remove(goal);
                if (_selectedGoal == goal) SelectedGoal = null;
                NotificationManager.Instance.ShowSuccess("Deleted", "Goal has been removed.");
                RefreshGoalStatistics();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to delete goal: {ex.Message}");
            }
        }

        private async Task RefreshGoalsAsync()
        {
            var goals = await TrackerDataManager.Instance.GetGoals();
            _goals.Clear();
            foreach (var g in goals) _goals.Add(g);
        }

        #endregion

        #region Commands - Global

        /// <summary>
        /// Opens the global search (Ctrl+K).
        /// Currently navigates to Chronicle tab - future: command palette overlay.
        /// </summary>
        public ICommand OpenSearchCommand => _openSearchCommand ??=
            new TrackerCommand(ExecuteOpenSearch);

        /// <summary>
        /// Opens the AI Help Bot assistant (F1 or Ctrl+Shift+H).
        /// </summary>
        public ICommand OpenHelpBotCommand => _openHelpBotCommand ??=
            new TrackerCommand(ExecuteOpenHelpBot);

        /// <summary>
        /// Creates a new item based on context (Ctrl+N). Shows a selection dialog.
        /// </summary>
        public ICommand NewItemCommand => _newItemCommand ??=
            new TrackerCommand(ExecuteNewItem);

        private void ExecuteOpenSearch(object? parameter)
        {
            // For now, this triggers a message that the UI can handle to navigate to search
            // Future enhancement: Open a command palette overlay
            Messenger.Publish(new PropertyChangedMessage
            {
                ChangedProperty = PropertyChangedEnum.NavigateToSearch
            });
        }

        private void ExecuteOpenHelpBot(object? parameter)
        {
            Views.HelpBotWindow.ShowHelpBot();
        }

        private void ExecuteNewItem(object? parameter)
        {
            // Show notification about Ctrl+N shortcut - user can use toolbar buttons for specific items
            // Future enhancement: Show a quick picker dialog for item type
            NotificationManager.Instance.ShowInfo(
                "Create New Item",
                "Use the toolbar buttons in each section to create new items:\n" +
                "• Team Members: Circle → Members → + Add\n" +
                "• 1:1 Meetings: Circle → 1:1s → + Schedule\n" +
                "• Tasks: Pulse → Tasks → + Add Task\n" +
                "• Projects: Pulse → Projects → + Add Project");
        }

        #endregion

        #region Public Properties - User

        /// <summary>
        /// User's initials for display in the profile badge.
        /// </summary>
        public string UserInitials
        {
            get => _userInitials;
            set { _userInitials = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Whether the user has an avatar image to display.
        /// </summary>
        public bool HasUserAvatar
        {
            get => _hasUserAvatar;
            set { _hasUserAvatar = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// The user's avatar image source.
        /// </summary>
        public System.Windows.Media.ImageSource? UserAvatarSource
        {
            get => _userAvatarSource;
            set { _userAvatarSource = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Public Properties - Collections

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;
        public ObservableCollection<OneOnOne> SelectedTeamMemberOneOnOneCollection => _selectedTeamMemberOneOnOneCollection;
        public ObservableCollection<OneOnOne> OneOnOnes => _oneOnOnes;
        public ObservableCollection<ITask> Tasks => _tasks;
        public ObservableCollection<ObjectiveKeyResult> ObjectiveKeyResults => _okrs;
        public ObservableCollection<KeyPerformanceIndicator> KeyPerformanceIndicators => _kpis;
        public ObservableCollection<Project> Projects => _projects;
        public ObservableCollection<Feedback> Feedbacks => _feedbacks;
        public ObservableCollection<IndividualGoal> Goals => _goals;

        #endregion

        #region Public Properties - Team Statistics (for Team Members page)

        /// <summary>
        /// Count of active team members.
        /// </summary>
        public int ActiveMemberCount => _teamMembers?.Count(m => m.IsActive) ?? 0;

        /// <summary>
        /// Count of inactive team members.
        /// </summary>
        public int InactiveMemberCount => _teamMembers?.Count(m => !m.IsActive) ?? 0;

        /// <summary>
        /// Average tenure of active team members.
        /// </summary>
        public string AverageTenure
        {
            get
            {
                var activeMembers = _teamMembers?.Where(m => m.IsActive && m.HireDate.Year > 1901).ToList();
                if (activeMembers == null || activeMembers.Count == 0) return "—";
                
                var avgDays = activeMembers.Average(m => (DateTime.Now - m.HireDate).Days);
                var avgYears = avgDays / 365;
                
                if (avgYears < 1) return "< 1 yr";
                return avgYears < 2 ? "~1 yr" : $"~{(int)avgYears} yrs";
            }
        }

        /// <summary>
        /// Total open tasks across all team members.
        /// </summary>
        public int TotalOpenTasks => _teamMembers?.Sum(m => m.OpenTaskCount) ?? 0;

        /// <summary>
        /// Count of team members who have open tasks assigned.
        /// </summary>
        public int MembersWithOpenTasksCount => _teamMembers?.Count(m => m.IsActive && m.OpenTaskCount > 0) ?? 0;

        /// <summary>
        /// Count of team members with on-track 1:1 cadence (within last 14 days).
        /// </summary>
        public int OnTrack1on1Count => _teamMembers?.Count(m => 
            m.IsActive && 
            m.LastOneOnOneDate.HasValue && 
            (DateTime.Now - m.LastOneOnOneDate.Value).Days <= 14) ?? 0;

        /// <summary>
        /// Count of team members due for 1:1 this week (15-21 days since last).
        /// </summary>
        public int DueSoon1on1Count => _teamMembers?.Count(m =>
            m.IsActive &&
            m.LastOneOnOneDate.HasValue &&
            (DateTime.Now - m.LastOneOnOneDate.Value).Days > 14 &&
            (DateTime.Now - m.LastOneOnOneDate.Value).Days <= 21) ?? 0;

        /// <summary>
        /// Count of team members overdue for 1:1 (more than 21 days since last).
        /// </summary>
        public int Overdue1on1Count => _teamMembers?.Count(m =>
            m.IsActive &&
            (!m.LastOneOnOneDate.HasValue || (DateTime.Now - m.LastOneOnOneDate.Value).Days > 21)) ?? 0;

        /// <summary>
        /// Team members who need attention (overdue for 1:1).
        /// </summary>
        public IEnumerable<TeamMember> MembersNeedingAttention => _teamMembers?
            .Where(m => m.IsActive && (!m.LastOneOnOneDate.HasValue || (DateTime.Now - m.LastOneOnOneDate.Value).Days > 21))
            .OrderByDescending(m => m.LastOneOnOneDate.HasValue ? (DateTime.Now - m.LastOneOnOneDate.Value).Days : 999)
            .Take(5) ?? Enumerable.Empty<TeamMember>();

        /// <summary>
        /// Whether there are any members needing attention.
        /// </summary>
        public bool HasMembersNeedingAttention => MembersNeedingAttention.Any();

        /// <summary>
        /// Search text for filtering team members.
        /// </summary>
        private string _memberSearchText = string.Empty;
        public string MemberSearchText
        {
            get => _memberSearchText;
            set
            {
                _memberSearchText = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilteredTeamMembers));
                RaisePropertyChanged(nameof(FilteredMemberCount));
            }
        }

        /// <summary>
        /// Current filter for team members.
        /// </summary>
        private TeamMemberFilterEnum _memberFilter = TeamMemberFilterEnum.All;
        public TeamMemberFilterEnum MemberFilter
        {
            get => _memberFilter;
            set
            {
                _memberFilter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilteredTeamMembers));
                RaisePropertyChanged(nameof(FilteredMemberCount));
            }
        }

        /// <summary>
        /// Filtered team members based on search text and selected filter.
        /// </summary>
        public IEnumerable<TeamMember> FilteredTeamMembers
        {
            get
            {
                if (_teamMembers == null) return Enumerable.Empty<TeamMember>();

                // Apply stat card filter first
                IEnumerable<TeamMember> filtered = _memberFilter switch
                {
                    TeamMemberFilterEnum.Active => _teamMembers.Where(m => m.IsActive),
                    TeamMemberFilterEnum.Inactive => _teamMembers.Where(m => !m.IsActive),
                    TeamMemberFilterEnum.OneOnOneOnTrack => _teamMembers.Where(m => 
                        m.IsActive && m.LastOneOnOneDate.HasValue && 
                        (DateTime.Now - m.LastOneOnOneDate.Value).Days <= 14),
                    TeamMemberFilterEnum.OneOnOneOverdue => _teamMembers.Where(m =>
                        m.IsActive && (!m.LastOneOnOneDate.HasValue || 
                        (DateTime.Now - m.LastOneOnOneDate.Value).Days > 21)),
                    TeamMemberFilterEnum.HasOpenTasks => _teamMembers.Where(m => m.OpenTaskCount > 0),
                    TeamMemberFilterEnum.NeedsAttention => _teamMembers.Where(m =>
                        m.IsActive && (!m.LastOneOnOneDate.HasValue || 
                        (DateTime.Now - m.LastOneOnOneDate.Value).Days > 21)),
                    _ => _teamMembers
                };

                // Then apply search filter
                if (!string.IsNullOrWhiteSpace(_memberSearchText))
                {
                    filtered = filtered.Where(m =>
                        m.FullName.Contains(_memberSearchText, StringComparison.OrdinalIgnoreCase) ||
                        m.JobTitle.Contains(_memberSearchText, StringComparison.OrdinalIgnoreCase) ||
                        m.Email.Contains(_memberSearchText, StringComparison.OrdinalIgnoreCase));
                }

                // Materialize the query to ensure the filter is applied
                return filtered.ToList();
            }
        }

        /// <summary>
        /// Count of filtered members based on search and filter.
        /// </summary>
        public int FilteredMemberCount => FilteredTeamMembers?.Count() ?? 0;

        /// <summary>
        /// Command to set the team member filter.
        /// </summary>
        private ICommand? _setMemberFilterCommand;
        public ICommand SetMemberFilterCommand => _setMemberFilterCommand ??=
            new TrackerCommand(SetMemberFilterExecuted);

        private void SetMemberFilterExecuted(object? parameter)
        {
            if (parameter is TeamMemberFilterEnum filter)
            {
                MemberFilter = filter;
            }
            else if (parameter is string filterString && Enum.TryParse<TeamMemberFilterEnum>(filterString, out var parsedFilter))
            {
                MemberFilter = parsedFilter;
            }
        }

        /// <summary>
        /// Clears the current filter.
        /// </summary>
        private ICommand? _clearMemberFilterCommand;
        public ICommand ClearMemberFilterCommand => _clearMemberFilterCommand ??=
            new TrackerCommand(_ => MemberFilter = TeamMemberFilterEnum.All);

        /// <summary>
        /// Refresh team statistics after data changes.
        /// </summary>
        private void RefreshTeamStatistics()
        {
            RaisePropertyChanged(nameof(ActiveMemberCount));
            RaisePropertyChanged(nameof(InactiveMemberCount));
            RaisePropertyChanged(nameof(AverageTenure));
            RaisePropertyChanged(nameof(TotalOpenTasks));
            RaisePropertyChanged(nameof(MembersWithOpenTasksCount));
            RaisePropertyChanged(nameof(OnTrack1on1Count));
            RaisePropertyChanged(nameof(DueSoon1on1Count));
            RaisePropertyChanged(nameof(Overdue1on1Count));
            RaisePropertyChanged(nameof(MembersNeedingAttention));
            RaisePropertyChanged(nameof(HasMembersNeedingAttention));
            
            // Refresh filtered members (for the Team Members page)
            RaisePropertyChanged(nameof(FilteredTeamMembers));
            RaisePropertyChanged(nameof(FilteredMemberCount));
            
            // Also refresh meeting stats
            RefreshMeetingStatistics();
        }

        #endregion

        #region Public Properties - Meeting Statistics (for 1:1s page)

        /// <summary>
        /// Completed meetings this month.
        /// </summary>
        public int CompletedMeetingsThisMonth => _oneOnOnes?
            .Count(m => m.Status == Common.Enums.MeetingStatusEnum.Completed && 
                        m.Date.Month == DateTime.Now.Month && 
                        m.Date.Year == DateTime.Now.Year) ?? 0;

        /// <summary>
        /// Scheduled meetings this month.
        /// </summary>
        public int ScheduledMeetingsThisMonth => _oneOnOnes?
            .Count(m => m.Status == Common.Enums.MeetingStatusEnum.Scheduled && 
                        m.Date.Month == DateTime.Now.Month && 
                        m.Date.Year == DateTime.Now.Year) ?? 0;

        /// <summary>
        /// Cancelled meetings this month.
        /// </summary>
        public int CancelledMeetingsThisMonth => _oneOnOnes?
            .Count(m => m.Status == Common.Enums.MeetingStatusEnum.Canceled && 
                        m.Date.Month == DateTime.Now.Month && 
                        m.Date.Year == DateTime.Now.Year) ?? 0;

        /// <summary>
        /// Open tasks from meetings.
        /// </summary>
        public int OpenMeetingTasksCount => _oneOnOnes?
            .SelectMany(m => m.Tasks ?? Enumerable.Empty<MeetingTask>())
            .Count(t => !t.IsCompleted) ?? 0;

        /// <summary>
        /// Upcoming meetings this week.
        /// </summary>
        public IEnumerable<OneOnOne> UpcomingMeetingsThisWeek => _oneOnOnes?
            .Where(m => m.Status == Common.Enums.MeetingStatusEnum.Scheduled &&
                        m.Date >= DateTime.Today &&
                        m.Date <= DateTime.Today.AddDays(7))
            .OrderBy(m => m.Date)
            .ThenBy(m => m.StartTime)
            .Take(5) ?? Enumerable.Empty<OneOnOne>();

        /// <summary>
        /// Whether there are no upcoming meetings this week.
        /// </summary>
        public bool HasNoUpcomingMeetingsThisWeek => !UpcomingMeetingsThisWeek.Any();

        /// <summary>
        /// Whether there are no 1:1s at all.
        /// </summary>
        public bool HasNoOneOnOnes => _oneOnOnes == null || _oneOnOnes.Count == 0;

        /// <summary>
        /// Gets 1:1 meetings grouped by team member.
        /// </summary>
        public ObservableCollection<TeamMemberMeetingGroup> OneOnOnesByMember
        {
            get
            {
                var groups = new ObservableCollection<TeamMemberMeetingGroup>();
                
                if (_oneOnOnes == null || _teamMembers == null)
                    return groups;
                
                // Group meetings by team member
                var grouped = _oneOnOnes
                    .Where(o => o.TeamMember != null)
                    .GroupBy(o => o.TeamMember.Id)
                    .OrderBy(g => g.First().TeamMemberName);
                
                foreach (var group in grouped)
                {
                    var member = _teamMembers.FirstOrDefault(m => m.Id == group.Key);
                    if (member == null) continue;
                    
                    groups.Add(new TeamMemberMeetingGroup
                    {
                        TeamMember = member,
                        Meetings = new ObservableCollection<OneOnOne>(
                            group.OrderByDescending(m => m.Date).ThenByDescending(m => m.StartTime))
                    });
                }
                
                return groups;
            }
        }

        /// <summary>
        /// Search text for filtering meetings.
        /// </summary>
        private string _meetingSearchText = string.Empty;
        public string MeetingSearchText
        {
            get => _meetingSearchText;
            set
            {
                _meetingSearchText = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(OneOnOnesByMember));
            }
        }

        /// <summary>
        /// Refresh meeting statistics.
        /// </summary>
        private void RefreshMeetingStatistics()
        {
            RaisePropertyChanged(nameof(CompletedMeetingsThisMonth));
            RaisePropertyChanged(nameof(ScheduledMeetingsThisMonth));
            RaisePropertyChanged(nameof(CancelledMeetingsThisMonth));
            RaisePropertyChanged(nameof(OpenMeetingTasksCount));
            RaisePropertyChanged(nameof(UpcomingMeetingsThisWeek));
            RaisePropertyChanged(nameof(HasNoUpcomingMeetingsThisWeek));
            RaisePropertyChanged(nameof(HasNoOneOnOnes));
            RaisePropertyChanged(nameof(OneOnOnesByMember));
        }

        #endregion

        #region Public Properties - Feedback Statistics (for Feedback page)

        /// <summary>
        /// Total feedback count.
        /// </summary>
        public int FeedbackCount => _feedbacks?.Count ?? 0;

        /// <summary>
        /// Feedback given this month.
        /// </summary>
        public int FeedbackThisMonth => _feedbacks?
            .Count(f => f.Date.Month == DateTime.Now.Month && f.Date.Year == DateTime.Now.Year) ?? 0;

        /// <summary>
        /// Positive feedback count.
        /// </summary>
        public int PositiveFeedbackCount => _feedbacks?
            .Count(f => f.Type == Common.Enums.FeedbackType.Positive) ?? 0;

        /// <summary>
        /// Constructive feedback count.
        /// </summary>
        public int ConstructiveFeedbackCount => _feedbacks?
            .Count(f => f.Type == Common.Enums.FeedbackType.Constructive) ?? 0;

        /// <summary>
        /// Recognition feedback count.
        /// </summary>
        public int RecognitionFeedbackCount => _feedbacks?
            .Count(f => f.Type == Common.Enums.FeedbackType.Recognition) ?? 0;

        /// <summary>
        /// Whether there is no feedback.
        /// </summary>
        public bool HasNoFeedback => _feedbacks == null || _feedbacks.Count == 0;

        /// <summary>
        /// Refresh feedback statistics.
        /// </summary>
        private void RefreshFeedbackStatistics()
        {
            RaisePropertyChanged(nameof(FeedbackCount));
            RaisePropertyChanged(nameof(FeedbackThisMonth));
            RaisePropertyChanged(nameof(PositiveFeedbackCount));
            RaisePropertyChanged(nameof(ConstructiveFeedbackCount));
            RaisePropertyChanged(nameof(RecognitionFeedbackCount));
            RaisePropertyChanged(nameof(HasNoFeedback));
        }

        #endregion

        #region Public Properties - Goal Statistics (for Goals page)

        /// <summary>
        /// Active goals count (not completed or cancelled).
        /// </summary>
        public int ActiveGoalCount => _goals?
            .Count(g => g.Status == Common.Enums.GoalStatus.InProgress || 
                        g.Status == Common.Enums.GoalStatus.NotStarted) ?? 0;

        /// <summary>
        /// On-track goals count.
        /// </summary>
        public int OnTrackGoalCount => _goals?
            .Count(g => g.Status == Common.Enums.GoalStatus.InProgress && !g.IsOverdue) ?? 0;

        /// <summary>
        /// At-risk goals count (overdue or on hold).
        /// </summary>
        public int AtRiskGoalCount => _goals?
            .Count(g => g.IsOverdue || g.Status == Common.Enums.GoalStatus.OnHold) ?? 0;

        /// <summary>
        /// Completed goals count.
        /// </summary>
        public int CompletedGoalCount => _goals?
            .Count(g => g.Status == Common.Enums.GoalStatus.Completed) ?? 0;

        /// <summary>
        /// Average goal progress percentage.
        /// </summary>
        public int AverageGoalProgress
        {
            get
            {
                var activeGoals = _goals?.Where(g => g.Status != Common.Enums.GoalStatus.Completed && 
                                                      g.Status != Common.Enums.GoalStatus.Cancelled).ToList();
                if (activeGoals == null || activeGoals.Count == 0) return 0;
                return (int)activeGoals.Average(g => g.ProgressPercent);
            }
        }

        /// <summary>
        /// Whether there are no goals.
        /// </summary>
        public bool HasNoGoals => _goals == null || _goals.Count == 0;

        /// <summary>
        /// Refresh goal statistics.
        /// </summary>
        private void RefreshGoalStatistics()
        {
            RaisePropertyChanged(nameof(ActiveGoalCount));
            RaisePropertyChanged(nameof(OnTrackGoalCount));
            RaisePropertyChanged(nameof(AtRiskGoalCount));
            RaisePropertyChanged(nameof(CompletedGoalCount));
            RaisePropertyChanged(nameof(AverageGoalProgress));
            RaisePropertyChanged(nameof(HasNoGoals));
        }

        #endregion

        #region Public Properties - Project Statistics (for Projects page)

        /// <summary>
        /// Count of active projects.
        /// </summary>
        public int ActiveProjectCount => _projects?
            .Count(p => p.Status?.ToLowerInvariant() is "active" or "in progress" or "inprogress") ?? 0;

        /// <summary>
        /// Count of completed projects.
        /// </summary>
        public int CompletedProjectCount => _projects?
            .Count(p => p.Status?.ToLowerInvariant() is "completed" or "done" or "finished") ?? 0;

        /// <summary>
        /// Count of at-risk projects.
        /// </summary>
        public int AtRiskProjectCount => _projects?
            .Count(p => p.Status?.ToLowerInvariant() is "at risk" or "atrisk" or "delayed") ?? 0;

        /// <summary>
        /// Search text for filtering projects.
        /// </summary>
        private string _projectSearchText = string.Empty;
        public string ProjectSearchText
        {
            get => _projectSearchText;
            set
            {
                _projectSearchText = value;
                RaisePropertyChanged();
                ApplyProjectFilters();
            }
        }

        /// <summary>
        /// Project status filter (null = All, "Active", "Completed", "AtRisk").
        /// </summary>
        private string? _projectStatusFilter;
        public string? ProjectStatusFilter
        {
            get => _projectStatusFilter;
            set
            {
                _projectStatusFilter = value;
                RaisePropertyChanged();
                ApplyProjectFilters();
            }
        }

        /// <summary>
        /// Filtered projects based on search and status filter.
        /// </summary>
        private ObservableCollection<Project> _filteredProjects = new();
        public ObservableCollection<Project> FilteredProjects
        {
            get => _filteredProjects;
            set
            {
                _filteredProjects = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Applies search and status filters to projects.
        /// </summary>
        private void ApplyProjectFilters()
        {
            var filtered = _projects?.AsEnumerable() ?? Enumerable.Empty<Project>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(ProjectSearchText))
            {
                var search = ProjectSearchText.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    p.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    (p.Description?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(ProjectStatusFilter))
            {
                filtered = ProjectStatusFilter.ToLowerInvariant() switch
                {
                    "active" => filtered.Where(p => p.Status?.ToLowerInvariant() is "active" or "in progress" or "inprogress"),
                    "completed" => filtered.Where(p => p.Status?.ToLowerInvariant() is "completed" or "done" or "finished"),
                    "atrisk" => filtered.Where(p => p.Status?.ToLowerInvariant() is "at risk" or "atrisk" or "delayed"),
                    _ => filtered
                };
            }

            FilteredProjects = new ObservableCollection<Project>(filtered.ToList());
            RefreshProjectStatistics();
        }

        /// <summary>
        /// Refresh project statistics.
        /// </summary>
        private void RefreshProjectStatistics()
        {
            RaisePropertyChanged(nameof(ActiveProjectCount));
            RaisePropertyChanged(nameof(CompletedProjectCount));
            RaisePropertyChanged(nameof(AtRiskProjectCount));
        }

        #endregion

        #region Public Properties - Task Statistics (for Tasks page)

        /// <summary>
        /// Count of open (incomplete) tasks.
        /// </summary>
        public int OpenTaskCount => _tasks?.Count(t => !t.IsCompleted) ?? 0;

        /// <summary>
        /// Count of overdue tasks.
        /// </summary>
        public int OverdueTaskCount => _tasks?.Count(t => !t.IsCompleted && t.DueDate < DateTime.Today) ?? 0;

        /// <summary>
        /// Count of completed tasks.
        /// </summary>
        public int CompletedTaskCount => _tasks?.Count(t => t.IsCompleted) ?? 0;

        /// <summary>
        /// Search text for filtering tasks.
        /// </summary>
        private string _taskSearchText = string.Empty;
        public string TaskSearchText
        {
            get => _taskSearchText;
            set
            {
                _taskSearchText = value;
                RaisePropertyChanged();
                ApplyTaskFilters();
            }
        }

        /// <summary>
        /// Task status filter (null = All, "Open", "Overdue", "Completed").
        /// </summary>
        private string? _taskStatusFilter;
        public string? TaskStatusFilter
        {
            get => _taskStatusFilter;
            set
            {
                _taskStatusFilter = value;
                RaisePropertyChanged();
                ApplyTaskFilters();
            }
        }

        /// <summary>
        /// Filtered tasks based on search and status filter.
        /// </summary>
        private ObservableCollection<ITask> _filteredTasks = new();
        public ObservableCollection<ITask> FilteredTasks
        {
            get => _filteredTasks;
            set
            {
                _filteredTasks = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Applies search and status filters to tasks.
        /// </summary>
        private void ApplyTaskFilters()
        {
            var filtered = _tasks?.AsEnumerable() ?? Enumerable.Empty<ITask>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(TaskSearchText))
            {
                var search = TaskSearchText.ToLowerInvariant();
                filtered = filtered.Where(t =>
                    (t.Description?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (t.Notes?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (t.OwnerName?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(TaskStatusFilter))
            {
                filtered = TaskStatusFilter.ToLowerInvariant() switch
                {
                    "open" => filtered.Where(t => !t.IsCompleted),
                    "overdue" => filtered.Where(t => !t.IsCompleted && t.DueDate < DateTime.Today),
                    "completed" => filtered.Where(t => t.IsCompleted),
                    _ => filtered
                };
            }

            FilteredTasks = new ObservableCollection<ITask>(filtered.ToList());
            RefreshTaskStatistics();
        }

        /// <summary>
        /// Refresh task statistics.
        /// </summary>
        private void RefreshTaskStatistics()
        {
            RaisePropertyChanged(nameof(OpenTaskCount));
            RaisePropertyChanged(nameof(OverdueTaskCount));
            RaisePropertyChanged(nameof(CompletedTaskCount));
        }

        #endregion

        #region Public Properties - OKR Statistics (for OKRs page)

        /// <summary>
        /// Count of on-track OKRs.
        /// </summary>
        public int OnTrackOkrCount => _okrs?
            .Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OnTrack) ?? 0;

        /// <summary>
        /// Count of at-risk OKRs.
        /// </summary>
        public int AtRiskOkrCount => _okrs?
            .Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.AtRisk) ?? 0;

        /// <summary>
        /// Count of off-track OKRs.
        /// </summary>
        public int OffTrackOkrCount => _okrs?
            .Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OffTrack) ?? 0;

        /// <summary>
        /// Search text for filtering OKRs.
        /// </summary>
        private string _okrSearchText = string.Empty;
        public string OkrSearchText
        {
            get => _okrSearchText;
            set
            {
                _okrSearchText = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Public Properties - KPI Statistics (for KPIs page)

        /// <summary>
        /// Count of KPIs meeting target (green).
        /// </summary>
        public int OnTargetKpiCount => _kpis?
            .Count(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget) ?? 0;

        /// <summary>
        /// Count of KPIs below target (red/amber).
        /// </summary>
        public int BelowTargetKpiCount => _kpis?
            .Count(k => k.Status != Common.Enums.KpiStatusEnum.OnTarget) ?? 0;

        /// <summary>
        /// Search text for filtering KPIs.
        /// </summary>
        private string _kpiSearchText = string.Empty;
        public string KpiSearchText
        {
            get => _kpiSearchText;
            set
            {
                _kpiSearchText = value;
                RaisePropertyChanged();
                ApplyKpiFilters();
            }
        }

        /// <summary>
        /// Status filter for KPIs (null = All).
        /// </summary>
        private Common.Enums.KpiStatusEnum? _kpiStatusFilter;
        public Common.Enums.KpiStatusEnum? KpiStatusFilter
        {
            get => _kpiStatusFilter;
            set
            {
                _kpiStatusFilter = value;
                RaisePropertyChanged();
                ApplyKpiFilters();
            }
        }

        /// <summary>
        /// Filtered KPIs based on search and status filter.
        /// </summary>
        private ObservableCollection<KeyPerformanceIndicator> _filteredKpis = new();
        public ObservableCollection<KeyPerformanceIndicator> FilteredKpis
        {
            get => _filteredKpis;
            set
            {
                _filteredKpis = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Applies search and status filters to KPIs.
        /// </summary>
        private void ApplyKpiFilters()
        {
            var filtered = _kpis?.AsEnumerable() ?? Enumerable.Empty<KeyPerformanceIndicator>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(KpiSearchText))
            {
                var search = KpiSearchText.ToLowerInvariant();
                filtered = filtered.Where(k =>
                    k.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    k.Description.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    k.Category.Contains(search, StringComparison.InvariantCultureIgnoreCase));
            }

            // Apply status filter
            if (KpiStatusFilter.HasValue)
            {
                if (KpiStatusFilter.Value == Common.Enums.KpiStatusEnum.OnTarget)
                {
                    filtered = filtered.Where(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget);
                }
                else
                {
                    // "Below Target" includes CloseToTarget and OffTarget
                    filtered = filtered.Where(k => k.Status != Common.Enums.KpiStatusEnum.OnTarget);
                }
            }

            FilteredKpis = new ObservableCollection<KeyPerformanceIndicator>(filtered.ToList());
            
            // Update counts
            RaisePropertyChanged(nameof(OnTargetKpiCount));
            RaisePropertyChanged(nameof(BelowTargetKpiCount));
        }

        #endregion

        #region Public Properties - Feedback Statistics (for Feedback page)

        /// <summary>
        /// Search text for filtering feedback.
        /// </summary>
        private string _feedbackSearchText = string.Empty;
        public string FeedbackSearchText
        {
            get => _feedbackSearchText;
            set
            {
                _feedbackSearchText = value;
                RaisePropertyChanged();
                ApplyFeedbackFilters();
            }
        }

        /// <summary>
        /// Selected team member for filtering feedback.
        /// </summary>
        private TeamMember? _selectedFeedbackFilterMember;
        public TeamMember? SelectedFeedbackFilterMember
        {
            get => _selectedFeedbackFilterMember;
            set
            {
                _selectedFeedbackFilterMember = value;
                RaisePropertyChanged();
                ApplyFeedbackFilters();
            }
        }

        /// <summary>
        /// Filtered feedback based on search and member filter.
        /// </summary>
        private ObservableCollection<Feedback> _filteredFeedbacks = new();
        public ObservableCollection<Feedback> FilteredFeedbacks
        {
            get => _filteredFeedbacks;
            set
            {
                _filteredFeedbacks = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Applies search and member filters to feedback.
        /// </summary>
        private void ApplyFeedbackFilters()
        {
            var filtered = _feedbacks?.AsEnumerable() ?? Enumerable.Empty<Feedback>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(FeedbackSearchText))
            {
                var search = FeedbackSearchText.ToLowerInvariant();
                filtered = filtered.Where(f =>
                    (f.Content?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (f.Context?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false));
            }

            // Apply member filter
            if (SelectedFeedbackFilterMember != null)
            {
                filtered = filtered.Where(f => f.TeamMemberId == SelectedFeedbackFilterMember.Id);
            }

            FilteredFeedbacks = new ObservableCollection<Feedback>(filtered.ToList());
        }

        #endregion

        #region Public Properties - Goals Statistics (for Goals page)

        /// <summary>
        /// Search text for filtering goals.
        /// </summary>
        private string _goalSearchText = string.Empty;
        public string GoalSearchText
        {
            get => _goalSearchText;
            set
            {
                _goalSearchText = value;
                RaisePropertyChanged();
                ApplyGoalFilters();
            }
        }

        /// <summary>
        /// Selected team member for filtering goals.
        /// </summary>
        private TeamMember? _selectedGoalFilterMember;
        public TeamMember? SelectedGoalFilterMember
        {
            get => _selectedGoalFilterMember;
            set
            {
                _selectedGoalFilterMember = value;
                RaisePropertyChanged();
                ApplyGoalFilters();
            }
        }

        /// <summary>
        /// Filtered goals based on search and member filter.
        /// </summary>
        private ObservableCollection<IndividualGoal> _filteredGoals = new();
        public ObservableCollection<IndividualGoal> FilteredGoals
        {
            get => _filteredGoals;
            set
            {
                _filteredGoals = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Applies search and member filters to goals.
        /// </summary>
        private void ApplyGoalFilters()
        {
            var filtered = _goals?.AsEnumerable() ?? Enumerable.Empty<IndividualGoal>();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(GoalSearchText))
            {
                var search = GoalSearchText.ToLowerInvariant();
                filtered = filtered.Where(g =>
                    (g.Title?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (g.Description?.Contains(search, StringComparison.InvariantCultureIgnoreCase) ?? false));
            }

            // Apply member filter
            if (SelectedGoalFilterMember != null)
            {
                filtered = filtered.Where(g => g.TeamMemberId == SelectedGoalFilterMember.Id);
            }

            FilteredGoals = new ObservableCollection<IndividualGoal>(filtered.ToList());
        }

        #endregion

        #region Public Properties - Selected Items

        public TeamMember? SelectedTeamMember
        {
            get => _teamMember;
            set
            {
                _teamMember = value;
                SelectedTeamMemberWrapper = new TeamMemberWrapper(_teamMember);
                if (_teamMember != null) SetTeamMemberOneOnOneCollection();
                RaisePropertyChanged();
            }
        }

        public TeamMemberWrapper? SelectedTeamMemberWrapper
        {
            get => _selectedTeamMemberWrapper;
            set
            {
                _selectedTeamMemberWrapper = value;
                RaisePropertyChanged();
            }
        }

        public ITask? SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                RaisePropertyChanged();
            }
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                RaisePropertyChanged();
            }
        }

        public ObjectiveKeyResult? SelectedOkr
        {
            get => _selectedOkr;
            set
            {
                _selectedOkr = value;
                RaisePropertyChanged();
            }
        }

        public KeyPerformanceIndicator? SelectedKpi
        {
            get => _selectedKpi;
            set
            {
                _selectedKpi = value;
                RaisePropertyChanged();
            }
        }

        public OneOnOne? SelectedOneOnOne
        {
            get => _selectedOneOnOne;
            set
            {
                _selectedOneOnOne = value;
                RaisePropertyChanged();
            }
        }

        public Feedback? SelectedFeedback
        {
            get => _selectedFeedback;
            set
            {
                _selectedFeedback = value;
                RaisePropertyChanged();
            }
        }

        public IndividualGoal? SelectedGoal
        {
            get => _selectedGoal;
            set
            {
                _selectedGoal = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The team member currently selected for viewing meetings (3-panel view).
        /// </summary>
        public TeamMember? SelectedMemberForMeetings
        {
            get => _selectedMemberForMeetings;
            set
            {
                _selectedMemberForMeetings = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedMemberMeetings));
            }
        }

        /// <summary>
        /// Time filter for meeting list (Upcoming/Past/All).
        /// </summary>
        public Controls.MeetingTimeFilterEnum MeetingTimeFilter
        {
            get => _meetingTimeFilter;
            set
            {
                _meetingTimeFilter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedMemberMeetings));
            }
        }

        /// <summary>
        /// Filtered meetings for the selected team member based on time filter.
        /// </summary>
        public IEnumerable<OneOnOne> SelectedMemberMeetings
        {
            get
            {
                if (_selectedMemberForMeetings == null || _oneOnOnes == null)
                    return Enumerable.Empty<OneOnOne>();

                var meetings = _oneOnOnes.Where(o => o.TeamMember?.Id == _selectedMemberForMeetings.Id);

                // Apply time filter
                var today = DateTime.Today;
                meetings = _meetingTimeFilter switch
                {
                    Controls.MeetingTimeFilterEnum.Upcoming => meetings.Where(m => 
                        m.Date >= today && 
                        m.Status == Common.Enums.MeetingStatusEnum.Scheduled),
                    Controls.MeetingTimeFilterEnum.Past => meetings.Where(m => m.Date < today),
                    _ => meetings // All
                };

                // Sort: upcoming by date ascending, past by date descending
                return _meetingTimeFilter == Controls.MeetingTimeFilterEnum.Past
                    ? meetings.OrderByDescending(m => m.Date).ThenByDescending(m => m.StartTime)
                    : meetings.OrderBy(m => m.Date).ThenBy(m => m.StartTime);
            }
        }

        #endregion

        #region Private Methods - Data Loading

        private async void GetData()
        {
            await RefreshAllDataAsync();
        }

        /// <summary>
        /// Refreshes all data from the database and updates the UI.
        /// Can be called externally to refresh the UI after data changes.
        /// </summary>
        public async Task RefreshAllDataAsync()
        {
            // Load data from database
            var team = await TrackerDataManager.Instance.GetTeamData();
            var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes();
            var tasks = await TrackerDataManager.Instance.GetTasks();
            var kpis = await TrackerDataManager.Instance.GetKPIs();
            var okrs = await TrackerDataManager.Instance.GetOKRs();
            var projects = await TrackerDataManager.Instance.GetProjects();
            var feedbacks = await TrackerDataManager.Instance.GetFeedbacks();
            var goals = await TrackerDataManager.Instance.GetGoals();

            // Update collections on UI thread - use same approach as DashboardViewModel
            if (App.Current.Dispatcher.CheckAccess())
            {
                // Already on UI thread
                _teamMembers = new ObservableCollection<TeamMember>(team);
                _oneOnOnes = new ObservableCollection<OneOnOne>(oneOnOnes);
                _tasks = new ObservableCollection<ITask>(tasks.Cast<ITask>());
                _kpis = new ObservableCollection<KeyPerformanceIndicator>(kpis);
                _okrs = new ObservableCollection<ObjectiveKeyResult>(okrs);
                _projects = new ObservableCollection<Project>(projects);
                _feedbacks = new ObservableCollection<Feedback>(feedbacks);
                _goals = new ObservableCollection<IndividualGoal>(goals);
                
                RaisePropertyChanged(nameof(TeamMembers));
                RaisePropertyChanged(nameof(OneOnOnes));
                RaisePropertyChanged(nameof(Tasks));
                RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                RaisePropertyChanged(nameof(ObjectiveKeyResults));
                RaisePropertyChanged(nameof(Projects));
                RaisePropertyChanged(nameof(Feedbacks));
                RaisePropertyChanged(nameof(Goals));
                
                // Apply filters
                ApplyKpiFilters();
                ApplyProjectFilters();
                ApplyTaskFilters();
                ApplyFeedbackFilters();
                ApplyGoalFilters();
                
                // Refresh team statistics
                RefreshTeamStatistics();
                
                // Refresh selected team member's 1:1 collection if one is selected
                if (_selectedTeamMemberWrapper != null)
                {
                    SetTeamMemberOneOnOneCollection();
                }
                
                // Load presence and photos from Microsoft 365 (fire and forget)
                _ = EnrichTeamMembersWithM365DataAsync();
            }
            else
            {
                // Need to dispatch to UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    _teamMembers = new ObservableCollection<TeamMember>(team);
                    _oneOnOnes = new ObservableCollection<OneOnOne>(oneOnOnes);
                    _tasks = new ObservableCollection<ITask>(tasks.Cast<ITask>());
                    _kpis = new ObservableCollection<KeyPerformanceIndicator>(kpis);
                    _okrs = new ObservableCollection<ObjectiveKeyResult>(okrs);
                    _projects = new ObservableCollection<Project>(projects);
                    _feedbacks = new ObservableCollection<Feedback>(feedbacks);
                    _goals = new ObservableCollection<IndividualGoal>(goals);
                    
                    RaisePropertyChanged(nameof(TeamMembers));
                    RaisePropertyChanged(nameof(OneOnOnes));
                    RaisePropertyChanged(nameof(Tasks));
                    RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                    RaisePropertyChanged(nameof(ObjectiveKeyResults));
                    RaisePropertyChanged(nameof(Projects));
                    RaisePropertyChanged(nameof(Feedbacks));
                    RaisePropertyChanged(nameof(Goals));
                    
                    // Apply filters
                    ApplyKpiFilters();
                    ApplyProjectFilters();
                    ApplyTaskFilters();
                    ApplyFeedbackFilters();
                    ApplyGoalFilters();
                    
                    // Refresh team statistics
                    RefreshTeamStatistics();
                    
                    // Refresh selected team member's 1:1 collection if one is selected
                    if (_selectedTeamMemberWrapper != null)
                    {
                        SetTeamMemberOneOnOneCollection();
                    }
                    
                    // Load presence and photos from Microsoft 365 (fire and forget)
                    _ = EnrichTeamMembersWithM365DataAsync();
                });
            }
        }

        /// <summary>
        /// Enriches team members with presence status and photos from Microsoft 365.
        /// Called asynchronously after team members are loaded.
        /// </summary>
        private async Task EnrichTeamMembersWithM365DataAsync()
        {
            // Only proceed if Microsoft 365 is connected
            if (!Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated)
                return;

            var teamMembersWithEmail = _teamMembers.Where(t => !string.IsNullOrEmpty(t.Email)).ToList();
            if (!teamMembersWithEmail.Any())
                return;

            try
            {
                var emails = teamMembersWithEmail.Select(t => t.Email).ToList();

                // Fetch presence in batch
                var presenceTask = Services.Microsoft365.Microsoft365EnhancedService.Instance.GetPresenceBatchAsync(emails);
                
                // Fetch photos in parallel
                var photoTasks = teamMembersWithEmail.Select(async t =>
                {
                    var photo = await Services.Microsoft365.Microsoft365EnhancedService.Instance.GetProfilePhotoAsync(t.Email);
                    return (t, photo);
                });

                // Wait for both
                var presenceResults = await presenceTask;
                var photoResults = await Task.WhenAll(photoTasks);

                // Update on UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Apply presence
                    foreach (var member in teamMembersWithEmail)
                    {
                        if (presenceResults.TryGetValue(member.Email, out var status))
                        {
                            member.Presence = status;
                        }
                    }

                    // Apply photos (only if no local photo exists)
                    foreach (var (member, photo) in photoResults)
                    {
                        if (photo != null && (member.ProfileImage == null || member.ProfileImage.Length == 0))
                        {
                            member.AzureAdPhoto = photo;
                        }
                    }

                    // Notify UI to refresh
                    RaisePropertyChanged(nameof(TeamMembers));
                    RaisePropertyChanged(nameof(FilteredTeamMembers));
                });
            }
            catch (Exception ex)
            {
                // Don't crash on M365 errors - just log and continue
                System.Diagnostics.Debug.WriteLine($"M365 enrichment failed: {ex.Message}");
            }
        }

        private async void RefreshData(PropertyChangedEnum changedProperty)
        {
            // If All is specified, refresh everything
            if (changedProperty == PropertyChangedEnum.All)
            {
                await RefreshAllDataAsync();
                return;
            }

            switch (changedProperty)
            {
                case PropertyChangedEnum.TeamMembers:
                    var team = await TrackerDataManager.Instance.GetTeamData().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedTeamMemberWrapper = null;
                        _teamMembers.Clear();
                        foreach (var member in team)
                        {
                            _teamMembers.Add(member);
                        }
                        // Refresh stats after team data changes
                        RefreshTeamStatistics();
                    });
                    break;
                case PropertyChangedEnum.OneOnOnes:
                    var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _oneOnOnes.Clear();
                        foreach (var meeting in oneOnOnes)
                        {
                            _oneOnOnes.Add(meeting);
                        }
                        if (_selectedTeamMemberWrapper != null)
                        {
                            SetTeamMemberOneOnOneCollection();
                        }
                    });
                    break;
                case PropertyChangedEnum.Tasks:
                    var tasks = await TrackerDataManager.Instance.GetTasks().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _tasks.Clear();
                        foreach (var task in tasks.Cast<ITask>())
                        {
                            _tasks.Add(task);
                        }
                    });
                    break;
                case PropertyChangedEnum.Projects:
                    var projects = await TrackerDataManager.Instance.GetProjects().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _projects.Clear();
                        foreach (var project in projects)
                        {
                            _projects.Add(project);
                        }
                    });
                    break;
                case PropertyChangedEnum.OKRs:
                    var okrs = await TrackerDataManager.Instance.GetOKRs().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _okrs.Clear();
                        foreach (var okr in okrs)
                        {
                            _okrs.Add(okr);
                        }
                    });
                    break;
                case PropertyChangedEnum.KPIs:
                    var kpis = await TrackerDataManager.Instance.GetKPIs().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _kpis.Clear();
                        foreach (var kpi in kpis)
                        {
                            _kpis.Add(kpi);
                        }
                    });
                    break;
            }
        }

        #endregion

        #region Private Methods - Team Members

        private bool CanEditTeamMemberExecute(object? parameter)
        {
            return parameter is TeamMember || parameter is TeamMemberWrapper || _selectedTeamMemberWrapper != null;
        }

        private void EditTeamMemberExecuted(object? parameter)
        {
            TeamMember? teamMember = null;
            
            if (parameter is TeamMemberWrapper wrapper)
                teamMember = wrapper.Data;
            else if (parameter is TeamMember tm)
                teamMember = tm;
            else if (_selectedTeamMemberWrapper != null)
                teamMember = _selectedTeamMemberWrapper.Data;
            
            if (teamMember != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.EditTeamMember, true, () =>
                {
                    RefreshData(PropertyChangedEnum.TeamMembers);
                    RefreshTeamStatistics();
                    RaisePropertyChanged(nameof(SelectedTeamMemberWrapper));
                }, teamMember);
            }
        }

        private bool CanDeleteTeamMember(object? parameter)
        {
            return parameter is TeamMember || parameter is TeamMemberWrapper || _selectedTeamMemberWrapper != null;
        }

        private async Task DeleteTeamMemberAsync(object? parameter)
        {
            TeamMember? teamMember = null;
            
            if (parameter is TeamMemberWrapper wrapper)
                teamMember = wrapper.Data;
            else if (parameter is TeamMember tm)
                teamMember = tm;
            else if (_selectedTeamMemberWrapper != null)
                teamMember = _selectedTeamMemberWrapper.Data;
            
            if (teamMember == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete {teamMember.FirstName} {teamMember.LastName}?\n\n" +
                "This will also remove all associated 1:1 meetings and related data.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDataManager.Instance.DeleteTeamMember(teamMember.Id);
            
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("Deleted", $"{teamMember.FirstName} {teamMember.LastName} has been removed.");
                RefreshData(PropertyChangedEnum.TeamMembers);
                RefreshTeamStatistics();
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to delete team member.");
            }
        }

        private void SetTeamMemberOneOnOneCollection()
        {
            _selectedTeamMemberOneOnOneCollection.Clear();
            foreach (var meeting in _oneOnOnes.Where(x => _teamMember != null && x.TeamMember.Id == _teamMember.Id))
            {
                _selectedTeamMemberOneOnOneCollection.Add(meeting);
            }
        }

        private bool CanExecuteAddTeamMemberOneOnOne(object? parameter)
        {
            return parameter is TeamMember || parameter is TeamMemberWrapper || _selectedTeamMemberWrapper != null;
        }

        private void AddTeamMemberOneOnOneExecuted(object? parameter)
        {
            TeamMember? teamMember = null;
            
            if (parameter is TeamMemberWrapper wrapper)
                teamMember = wrapper.Data;
            else if (parameter is TeamMember tm)
                teamMember = tm;
            else if (_selectedTeamMemberWrapper != null)
                teamMember = _selectedTeamMemberWrapper.Data;
            
            if (teamMember != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOneOnOne, false, () =>
                {
                    RefreshData(PropertyChangedEnum.OneOnOnes);
                    RefreshMeetingStatistics();
                    RaisePropertyChanged(nameof(SelectedTeamMemberWrapper));
                }, teamMember);
            }
        }

        #endregion

        #region Private Methods - Tasks

        private bool CanEditTask(object? parameter)
        {
            return parameter is ITask || _selectedTask != null;
        }

        private void EditTaskExecuted(object? parameter)
        {
            var task = parameter as ITask ?? _selectedTask;
            if (task != null)
            {
                // TODO: Launch edit task dialog when edit mode is supported
                DialogManager.Instance.LaunchDialogByType(DialogType.AddTask, true, () =>
                {
                    RaisePropertyChanged(nameof(Tasks));
                }, task);
            }
        }

        private bool CanDeleteTask(object? parameter)
        {
            return parameter is ITask || _selectedTask != null;
        }

        private async Task DeleteTaskAsync(object? parameter)
        {
            var task = parameter as ITask ?? _selectedTask;
            if (task == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this task?\n\n\"{task.Description}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection (database delete would go here when implemented)
            var taskToRemove = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (taskToRemove != null)
            {
                _tasks.Remove(taskToRemove);
                NotificationManager.Instance.ShowSuccess("Deleted", "Task has been removed.");
                RaisePropertyChanged(nameof(Tasks));
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods - Projects

        private bool CanEditProject(object? parameter)
        {
            return parameter is Project || _selectedProject != null;
        }

        private void EditProjectExecuted(object? parameter)
        {
            var project = parameter as Project ?? _selectedProject;
            if (project != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddProject, true, () =>
                {
                    RaisePropertyChanged(nameof(Projects));
                }, project);
            }
        }

        private bool CanDeleteProject(object? parameter)
        {
            return parameter is Project || _selectedProject != null;
        }

        private async Task DeleteProjectAsync(object? parameter)
        {
            var project = parameter as Project ?? _selectedProject;
            if (project == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete the project \"{project.Name}\"?\n\n" +
                "This will also remove all associated tasks, OKRs, and KPIs.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDataManager.Instance.DeleteProject(project.ID);
            
            if (success)
            {
                _projects.Remove(project);
                NotificationManager.Instance.ShowSuccess("Deleted", $"Project \"{project.Name}\" has been removed.");
                RaisePropertyChanged(nameof(Projects));
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to delete project.");
            }
        }

        #endregion

        #region Private Methods - OKRs

        private bool CanEditOkr(object? parameter)
        {
            return parameter is ObjectiveKeyResult || _selectedOkr != null;
        }

        private void EditOkrExecuted(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? _selectedOkr;
            if (okr != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOKR, true, () =>
                {
                    RaisePropertyChanged(nameof(ObjectiveKeyResults));
                }, okr);
            }
        }

        private bool CanDeleteOkr(object? parameter)
        {
            return parameter is ObjectiveKeyResult || _selectedOkr != null;
        }

        private async Task DeleteOkrAsync(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? _selectedOkr;
            if (okr == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this OKR?\n\n\"{okr.Title}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection
            _okrs.Remove(okr);
            NotificationManager.Instance.ShowSuccess("Deleted", "OKR has been removed.");
            RaisePropertyChanged(nameof(ObjectiveKeyResults));
            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods - KPIs

        private bool CanEditKpi(object? parameter)
        {
            return parameter is KeyPerformanceIndicator || _selectedKpi != null;
        }

        private void EditKpiExecuted(object? parameter)
        {
            var kpi = parameter as KeyPerformanceIndicator ?? _selectedKpi;
            if (kpi != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddKPI, true, () =>
                {
                    RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                }, kpi);
            }
        }

        private bool CanDeleteKpi(object? parameter)
        {
            return parameter is KeyPerformanceIndicator || _selectedKpi != null;
        }

        private async Task DeleteKpiAsync(object? parameter)
        {
            var kpi = parameter as KeyPerformanceIndicator ?? _selectedKpi;
            if (kpi == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this KPI?\n\n\"{kpi.Name}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection
            _kpis.Remove(kpi);
            NotificationManager.Instance.ShowSuccess("Deleted", "KPI has been removed.");
            RaisePropertyChanged(nameof(KeyPerformanceIndicators));
            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods - OneOnOnes

        private bool CanEditOneOnOne(object? parameter)
        {
            return parameter is OneOnOne || _selectedOneOnOne != null;
        }

        private void EditOneOnOneExecuted(object? parameter)
        {
            var oneOnOne = parameter as OneOnOne ?? _selectedOneOnOne;
            if (oneOnOne != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOneOnOne, true, () =>
                {
                    RaisePropertyChanged(nameof(OneOnOnes));
                    SetTeamMemberOneOnOneCollection();
                }, oneOnOne);
            }
        }

        private bool CanDeleteOneOnOne(object? parameter)
        {
            return parameter is OneOnOne || _selectedOneOnOne != null;
        }

        private async Task DeleteOneOnOneAsync(object? parameter)
        {
            var oneOnOne = parameter as OneOnOne ?? _selectedOneOnOne;
            if (oneOnOne == null) return;

            var owner = GetMainWindow();
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this 1:1 meeting?\n\n\"{oneOnOne.Description}\" with {oneOnOne.TeamMemberName}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDataManager.Instance.DeleteOneOnOne(oneOnOne.Id);
            
            if (success)
            {
                _oneOnOnes.Remove(oneOnOne);
                _selectedTeamMemberOneOnOneCollection.Remove(oneOnOne);
                NotificationManager.Instance.ShowSuccess("Deleted", "1:1 meeting has been removed.");
                RaisePropertyChanged(nameof(OneOnOnes));
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to delete 1:1 meeting.");
            }
        }

        #endregion

        #region Private Methods - Messaging

        private void SubscribeToMessages()
        {
            // Legacy messenger (being phased out)
            Messenger.Subscribe<PropertyChangedMessage>(HandlePropertyChangedMessage);
            
            // New CommunityToolkit.Mvvm messenger
            DataMessenger.Register(this, OnDataChanged);
        }

        private void UnsubscribeToMessages()
        {
            Messenger.Unsubscribe<PropertyChangedMessage>(HandlePropertyChangedMessage);
            DataMessenger.Unregister(this);
        }

        private async void HandlePropertyChangedMessage(PropertyChangedMessage message)
        {
            if (message.RefreshData)
            {
                await RefreshAllDataAsync();
            }
        }

        private void OnDataChanged(DataChangeInfo info)
        {
            // Handle user profile changes (avatar update, etc.)
            if (info.Includes(DataChangeType.UserProfile))
            {
                App.Current?.Dispatcher.InvokeAsync(() =>
                {
                    UpdateUserInitials();
                });
            }
            
            // Refresh all data when any relevant data changes
            if (info.RefreshAll ||
                info.Includes(DataChangeType.TeamMembers) ||
                info.Includes(DataChangeType.OneOnOnes) ||
                info.Includes(DataChangeType.Tasks) ||
                info.Includes(DataChangeType.Projects) ||
                info.Includes(DataChangeType.OKRs) ||
                info.Includes(DataChangeType.KPIs) ||
                info.Includes(DataChangeType.Goals) ||
                info.Includes(DataChangeType.Feedback))
            {
                // Refresh on UI thread
                App.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await RefreshAllDataAsync();
                });
            }
        }

        #endregion

        #region Helper Methods

        private MainWindow? GetMainWindow()
        {
            return Win32UtilHelper.GetMainWindow();
        }

        #endregion
    }
}
