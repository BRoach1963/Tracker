using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Eventing.Messages;
using Tracker.Eventing;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing 1:1 meetings.
    /// Manages discussion points, concerns, action items, and follow-ups.
    /// </summary>
    public class OneOnOneViewModel : BaseDialogViewModel
    {
        #region Fields

        private OneOnOne? _data;
        private ObservableCollection<ActionItem> _actionItems = new();
        private ObservableCollection<ObjectiveKeyResult> _keyResults = new();
        private ObservableCollection<FollowUpItem> _followUpItems = new();
        private ObservableCollection<DiscussionPoint> _discussionPoints = new();
        private ObservableCollection<Concern> _concerns = new();

        // Linked items (existing tasks/OKRs/KPIs discussed in this meeting)
        private ObservableCollection<OneOnOneLinkedTask> _linkedTasks = new();
        private ObservableCollection<OneOnOneLinkedOkr> _linkedOkrs = new();
        private ObservableCollection<OneOnOneLinkedKpi> _linkedKpis = new();

        // Available items for linking (from database)
        private ObservableCollection<IndividualTask> _availableTasks = new();
        private ObservableCollection<ObjectiveKeyResult> _availableOkrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _availableKpis = new();

        // Previous meeting and uncompleted items
        private OneOnOne? _previousMeeting;
        private ObservableCollection<ActionItem> _uncompletedActionItems = new();
        private ObservableCollection<FollowUpItem> _uncompletedFollowUpItems = new();

        private bool _inEditMode;

        private Dictionary<string, object> _changedProperties = new();

        public bool InEditMode => _inEditMode;

        // Main commands
        private ICommand? _updateOneOnOneCommand;
        private ICommand? _addOneOnOneCommand;

        // Discussion Point commands
        private ICommand? _addDiscussionPointCommand;
        private ICommand? _editDiscussionPointCommand;
        private ICommand? _deleteDiscussionPointCommand;
        private DiscussionPoint? _selectedDiscussionPoint;

        // Concern commands
        private ICommand? _addConcernCommand;
        private ICommand? _editConcernCommand;
        private ICommand? _deleteConcernCommand;
        private Concern? _selectedConcern;

        // Task commands (Action Items / Follow-ups)
        private ICommand? _addTaskCommand;
        private ICommand? _editTaskCommand;
        private ICommand? _deleteTaskCommand;
        private ActionItem? _selectedActionItem;
        private FollowUpItem? _selectedFollowUpItem;

        #endregion

        #region Ctor

        public OneOnOneViewModel(Action? callback, OneOnOne data, bool edit = true, TeamMember? teamMember = null) : base(callback)
        {
            _inEditMode = edit;
            _data = data;
            if (teamMember != null && !_inEditMode) _data.TeamMember = teamMember;
            SetLists();
            LoadPreviousMeetingAndUncompletedItems();
            LoadAvailableItemsForLinking();
            LoadLinkedItems();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        public ICommand UpdateOneOnOneCommand =>
            _updateOneOnOneCommand ??= new TrackerCommand(UpdateOneOnOneExecuted, CanUpdateOneOnOne);

        public ICommand AddOneOnOneCommand =>
            _addOneOnOneCommand ??= new TrackerCommand(AddOneOnOneExecuted, CanExecuteAddOneOnOne);

        // Discussion Point Commands
        public ICommand AddDiscussionPointCommand =>
            _addDiscussionPointCommand ??= new TrackerCommand(AddDiscussionPointExecuted);

        public ICommand EditDiscussionPointCommand =>
            _editDiscussionPointCommand ??= new TrackerCommand(EditDiscussionPointExecuted, CanEditOrDeleteDiscussionPoint);

        public ICommand DeleteDiscussionPointCommand =>
            _deleteDiscussionPointCommand ??= new TrackerCommand(DeleteDiscussionPointExecuted, CanEditOrDeleteDiscussionPoint);

        // Concern Commands
        public ICommand AddConcernCommand =>
            _addConcernCommand ??= new TrackerCommand(AddConcernExecuted);

        public ICommand EditConcernCommand =>
            _editConcernCommand ??= new TrackerCommand(EditConcernExecuted, CanEditOrDeleteConcern);

        public ICommand DeleteConcernCommand =>
            _deleteConcernCommand ??= new TrackerCommand(DeleteConcernExecuted, CanEditOrDeleteConcern);

        // Task Commands
        public ICommand AddTaskCommand =>
            _addTaskCommand ??= new TrackerCommand(AddTaskExecuted);

        public ICommand EditTaskCommand =>
            _editTaskCommand ??= new TrackerCommand(EditTaskExecuted, CanEditOrDeleteTask);

        public ICommand DeleteTaskCommand =>
            _deleteTaskCommand ??= new TrackerCommand(DeleteTaskExecuted, CanEditOrDeleteTask);

        // Linking commands
        private ICommand? _linkTaskCommand;
        private ICommand? _linkOkrCommand;
        private ICommand? _linkKpiCommand;
        private ICommand? _unlinkTaskCommand;
        private ICommand? _unlinkOkrCommand;
        private ICommand? _unlinkKpiCommand;
        private ICommand? _rolloverUncompletedItemsCommand;

        public ICommand LinkTaskCommand =>
            _linkTaskCommand ??= new TrackerCommand(LinkTaskExecuted, CanLinkTask);

        public ICommand LinkOkrCommand =>
            _linkOkrCommand ??= new TrackerCommand(LinkOkrExecuted, CanLinkOkr);

        public ICommand LinkKpiCommand =>
            _linkKpiCommand ??= new TrackerCommand(LinkKpiExecuted, CanLinkKpi);

        public ICommand UnlinkTaskCommand =>
            _unlinkTaskCommand ??= new TrackerCommand(UnlinkTaskExecuted, CanUnlinkTask);

        public ICommand UnlinkOkrCommand =>
            _unlinkOkrCommand ??= new TrackerCommand(UnlinkOkrExecuted, CanUnlinkOkr);

        public ICommand UnlinkKpiCommand =>
            _unlinkKpiCommand ??= new TrackerCommand(UnlinkKpiExecuted, CanUnlinkKpi);

        public ICommand RolloverUncompletedItemsCommand =>
            _rolloverUncompletedItemsCommand ??= new TrackerCommand(RolloverUncompletedItemsExecuted);

        #endregion

        #region Public Properties

        public int Id => _data.Id;

        public OneOnOne Data => _data;

        public ObservableCollection<ActionItem> ActionItems => _actionItems;

        public ObservableCollection<ObjectiveKeyResult> KeyResults => _keyResults;

        public ObservableCollection<FollowUpItem> FollowUpItems => _followUpItems;

        public ObservableCollection<DiscussionPoint> DiscussionPoints => _discussionPoints;

        public ObservableCollection<Concern> Concerns => _concerns;

        // Linked items (existing tasks/OKRs/KPIs discussed in this meeting)
        public ObservableCollection<OneOnOneLinkedTask> LinkedTasks => _linkedTasks;
        public ObservableCollection<OneOnOneLinkedOkr> LinkedOkrs => _linkedOkrs;
        public ObservableCollection<OneOnOneLinkedKpi> LinkedKpis => _linkedKpis;

        // Available items for linking
        public ObservableCollection<IndividualTask> AvailableTasks => _availableTasks;
        public ObservableCollection<ObjectiveKeyResult> AvailableOkrs => _availableOkrs;
        public ObservableCollection<KeyPerformanceIndicator> AvailableKpis => _availableKpis;

        // Previous meeting summary
        public OneOnOne? PreviousMeeting
        {
            get => _previousMeeting;
            private set
            {
                _previousMeeting = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasPreviousMeeting));
                RaisePropertyChanged(nameof(PreviousMeetingSummary));
            }
        }

        public bool HasPreviousMeeting => _previousMeeting != null;

        public string PreviousMeetingSummary
        {
            get
            {
                if (_previousMeeting == null) return "No previous meeting";
                return $"Last meeting: {_previousMeeting.Date:MM/dd/yyyy}\n" +
                       $"Action Items: {_previousMeeting.ActionItems.Count}\n" +
                       $"Follow-ups: {_previousMeeting.FollowUpItems.Count}\n" +
                       $"Discussion Points: {_previousMeeting.DiscussionPoints.Count}";
            }
        }

        // Uncompleted items from previous meetings
        public ObservableCollection<ActionItem> UncompletedActionItems => _uncompletedActionItems;
        public ObservableCollection<FollowUpItem> UncompletedFollowUpItems => _uncompletedFollowUpItems;

        // Selected items for linking
        private IndividualTask? _selectedAvailableTask;
        private ObjectiveKeyResult? _selectedAvailableOkr;
        private KeyPerformanceIndicator? _selectedAvailableKpi;
        private OneOnOneLinkedTask? _selectedLinkedTask;
        private OneOnOneLinkedOkr? _selectedLinkedOkr;
        private OneOnOneLinkedKpi? _selectedLinkedKpi;

        public IndividualTask? SelectedAvailableTask
        {
            get => _selectedAvailableTask;
            set
            {
                _selectedAvailableTask = value;
                RaisePropertyChanged();
            }
        }

        public ObjectiveKeyResult? SelectedAvailableOkr
        {
            get => _selectedAvailableOkr;
            set
            {
                _selectedAvailableOkr = value;
                RaisePropertyChanged();
            }
        }

        public KeyPerformanceIndicator? SelectedAvailableKpi
        {
            get => _selectedAvailableKpi;
            set
            {
                _selectedAvailableKpi = value;
                RaisePropertyChanged();
            }
        }

        public OneOnOneLinkedTask? SelectedLinkedTask
        {
            get => _selectedLinkedTask;
            set
            {
                _selectedLinkedTask = value;
                RaisePropertyChanged();
            }
        }

        public OneOnOneLinkedOkr? SelectedLinkedOkr
        {
            get => _selectedLinkedOkr;
            set
            {
                _selectedLinkedOkr = value;
                RaisePropertyChanged();
            }
        }

        public OneOnOneLinkedKpi? SelectedLinkedKpi
        {
            get => _selectedLinkedKpi;
            set
            {
                _selectedLinkedKpi = value;
                RaisePropertyChanged();
            }
        }

        // Selected items for editing/deleting
        public DiscussionPoint? SelectedDiscussionPoint
        {
            get => _selectedDiscussionPoint;
            set
            {
                _selectedDiscussionPoint = value;
                RaisePropertyChanged();
            }
        }

        public Concern? SelectedConcern
        {
            get => _selectedConcern;
            set
            {
                _selectedConcern = value;
                RaisePropertyChanged();
            }
        }

        public ActionItem? SelectedActionItem
        {
            get => _selectedActionItem;
            set
            {
                _selectedActionItem = value;
                RaisePropertyChanged();
            }
        }

        public FollowUpItem? SelectedFollowUpItem
        {
            get => _selectedFollowUpItem;
            set
            {
                _selectedFollowUpItem = value;
                RaisePropertyChanged();
            }
        }

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
            get => _data.Date == DateTime.Now.Date ? "MM/DD/YYYY" : _data.Date.ToString("MM/dd/yyyy");
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

        #region Private Methods - Main

        private bool CanExecuteAddOneOnOne(object? obj)
        {
            if (_data.TeamMember.Id == 0) return false;
            return !string.IsNullOrEmpty(Description);
        }

        private async void AddOneOnOneExecuted(object? parameter)
        {
            // First add and get the id for the base one on one.
            var id = await TrackerDataManager.Instance.AddOneOnOne(_data!);

            if (id > 0)
            {
                _data.Id = id;

                // Save all related items
                await SaveRelatedItems(id);

                // Save linked items
                await SaveLinkedItems(id);

                // Sync to calendars if enabled
                var settings = UserSettingsManager.Instance.Settings.Calendar;
                if (settings.AutoSyncOnSave)
                {
                    await CalendarSyncManager.Instance.SyncToAllCalendarsAsync(_data);
                }

                NotificationManager.Instance.ShowSuccess("1:1 Created", $"Meeting with {TeamMemberName} has been saved.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create 1:1 meeting.");
            }

            // Close dialog
            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Saves all related items (discussion points, concerns, action items, follow-ups, OKRs).
        /// These are saved as part of the OneOnOne entity through navigation properties.
        /// </summary>
        private async Task SaveRelatedItems(int oneOnOneId)
        {
            // Ensure all items are in the OneOnOne's collections
            _data.DiscussionPoints = _discussionPoints.ToList();
            _data.Concerns = _concerns.ToList();
            _data.ActionItems = _actionItems.ToList();
            _data.FollowUpItems = _followUpItems.ToList();
            _data.ObjectiveKeyResults = _keyResults.ToList();

            // Update the OneOnOne to save all related items
            await TrackerDataManager.Instance.UpdateOneOnOne(_data);
        }

        /// <summary>
        /// Saves linked items (existing tasks/OKRs/KPIs discussed in this meeting).
        /// </summary>
        private async Task SaveLinkedItems(int oneOnOneId)
        {
            // Update OneOnOneId for all linked items
            foreach (var linkedTask in _linkedTasks)
            {
                linkedTask.OneOnOneId = oneOnOneId;
                await TrackerDbManager.Instance.LinkTaskToMeetingAsync(oneOnOneId, linkedTask.TaskId, linkedTask.DiscussionNotes);
            }

            foreach (var linkedOkr in _linkedOkrs)
            {
                linkedOkr.OneOnOneId = oneOnOneId;
                await TrackerDbManager.Instance.LinkOkrToMeetingAsync(oneOnOneId, linkedOkr.OkrId, linkedOkr.DiscussionNotes);
            }

            foreach (var linkedKpi in _linkedKpis)
            {
                linkedKpi.OneOnOneId = oneOnOneId;
                await TrackerDbManager.Instance.LinkKpiToMeetingAsync(oneOnOneId, linkedKpi.KpiId, linkedKpi.DiscussionNotes);
            }
        }

        private bool CanUpdateOneOnOne(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        private async void UpdateOneOnOneExecuted(object? parameter)
        {
            var success = await TrackerDataManager.Instance.UpdateOneOnOne(_data!);
            
            if (success)
            {
                // Sync to calendars if enabled
                var settings = UserSettingsManager.Instance.Settings.Calendar;
                if (settings.AutoSyncOnSave)
                {
                    await CalendarSyncManager.Instance.SyncToAllCalendarsAsync(_data);
                }

                NotificationManager.Instance.ShowSuccess("1:1 Updated", $"Meeting with {TeamMemberName} has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update 1:1 meeting.");
            }
            
            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        private void SetLists()
        {
            if (_inEditMode)
            {
                Date = DateTime.Now.Date;
                StartTime = DateTime.Now.TimeOfDay;
                EndTime = DateTime.Now.TimeOfDay + new TimeSpan(0, 1, 0, 0);
            }
            else
            {
                // Load existing collections from data
                if (_data.ActionItems != null) _actionItems = new ObservableCollection<ActionItem>(_data.ActionItems);
                if (_data.FollowUpItems != null) _followUpItems = new ObservableCollection<FollowUpItem>(_data.FollowUpItems);
                if (_data.DiscussionPoints != null) _discussionPoints = new ObservableCollection<DiscussionPoint>(_data.DiscussionPoints);
                if (_data.Concerns != null) _concerns = new ObservableCollection<Concern>(_data.Concerns);
                if (_data.ObjectiveKeyResults != null) _keyResults = new ObservableCollection<ObjectiveKeyResult>(_data.ObjectiveKeyResults);
            }
        }

        /// <summary>
        /// Loads the previous meeting and uncompleted items for rollover.
        /// </summary>
        private async void LoadPreviousMeetingAndUncompletedItems()
        {
            if (_data?.TeamMember?.Id == null || _data.TeamMember.Id == 0) return;

            try
            {
                var excludeId = _inEditMode ? _data.Id : (int?)null;
                PreviousMeeting = await TrackerDbManager.Instance.GetPreviousOneOnOneAsync(_data.TeamMember.Id, excludeId);

                var (actionItems, followUpItems) = await TrackerDbManager.Instance.GetUncompletedItemsAsync(_data.TeamMember.Id);
                _uncompletedActionItems = new ObservableCollection<ActionItem>(actionItems);
                _uncompletedFollowUpItems = new ObservableCollection<FollowUpItem>(followUpItems);
                RaisePropertyChanged(nameof(UncompletedActionItems));
                RaisePropertyChanged(nameof(UncompletedFollowUpItems));
            }
            catch (Exception ex)
            {
                // Log error but don't crash the dialog
                System.Diagnostics.Debug.WriteLine($"Error loading previous meeting: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads available tasks, OKRs, and KPIs that can be linked to this meeting.
        /// </summary>
        private async void LoadAvailableItemsForLinking()
        {
            try
            {
                var tasks = await TrackerDataManager.Instance.GetTasks();
                _availableTasks = new ObservableCollection<IndividualTask>(tasks);
                RaisePropertyChanged(nameof(AvailableTasks));

                var okrs = await TrackerDataManager.Instance.GetOKRs();
                _availableOkrs = new ObservableCollection<ObjectiveKeyResult>(okrs);
                RaisePropertyChanged(nameof(AvailableOkrs));

                var kpis = await TrackerDataManager.Instance.GetKPIs();
                _availableKpis = new ObservableCollection<KeyPerformanceIndicator>(kpis);
                RaisePropertyChanged(nameof(AvailableKpis));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available items: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads already linked items if editing an existing meeting.
        /// </summary>
        private void LoadLinkedItems()
        {
            if (_data?.LinkedTasks != null)
            {
                _linkedTasks = new ObservableCollection<OneOnOneLinkedTask>(_data.LinkedTasks.Where(lt => !lt.IsDeleted));
                RaisePropertyChanged(nameof(LinkedTasks));
            }

            if (_data?.LinkedOkrs != null)
            {
                _linkedOkrs = new ObservableCollection<OneOnOneLinkedOkr>(_data.LinkedOkrs.Where(lo => !lo.IsDeleted));
                RaisePropertyChanged(nameof(LinkedOkrs));
            }

            if (_data?.LinkedKpis != null)
            {
                _linkedKpis = new ObservableCollection<OneOnOneLinkedKpi>(_data.LinkedKpis.Where(lk => !lk.IsDeleted));
                RaisePropertyChanged(nameof(LinkedKpis));
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

        #region Private Methods - Discussion Points

        private void AddDiscussionPointExecuted(object? parameter)
        {
            var newPoint = new DiscussionPoint
            {
                Description = "New Discussion Point",
                Type = DiscussionType.Other,
                DateRaised = DateTime.Now,
                TeamMemberId = _data?.TeamMember?.Id ?? 0
            };
            _discussionPoints.Add(newPoint);
            SelectedDiscussionPoint = newPoint;
            UpdateChangedValues("DiscussionPoints", _discussionPoints.Count);
        }

        private bool CanEditOrDeleteDiscussionPoint(object? parameter)
        {
            return SelectedDiscussionPoint != null;
        }

        private void EditDiscussionPointExecuted(object? parameter)
        {
            // For now, selection allows inline editing in the DataGrid
            // Could open a dialog for more complex editing in the future
            if (SelectedDiscussionPoint != null)
            {
                UpdateChangedValues("DiscussionPoints", _discussionPoints.Count);
            }
        }

        private void DeleteDiscussionPointExecuted(object? parameter)
        {
            if (SelectedDiscussionPoint != null)
            {
                _discussionPoints.Remove(SelectedDiscussionPoint);
                SelectedDiscussionPoint = null;
                UpdateChangedValues("DiscussionPoints", _discussionPoints.Count);
            }
        }

        #endregion

        #region Private Methods - Concerns

        private void AddConcernExecuted(object? parameter)
        {
            var newConcern = new Concern
            {
                Description = "New Concern",
                DateRaised = DateTime.Now,
                TeamMemberId = _data?.TeamMember?.Id ?? 0
            };
            _concerns.Add(newConcern);
            SelectedConcern = newConcern;
            UpdateChangedValues("Concerns", _concerns.Count);
        }

        private bool CanEditOrDeleteConcern(object? parameter)
        {
            return SelectedConcern != null;
        }

        private void EditConcernExecuted(object? parameter)
        {
            // For now, selection allows inline editing in the DataGrid
            if (SelectedConcern != null)
            {
                UpdateChangedValues("Concerns", _concerns.Count);
            }
        }

        private void DeleteConcernExecuted(object? parameter)
        {
            if (SelectedConcern != null)
            {
                _concerns.Remove(SelectedConcern);
                SelectedConcern = null;
                UpdateChangedValues("Concerns", _concerns.Count);
            }
        }

        #endregion

        #region Private Methods - Tasks (Action Items)

        private void AddTaskExecuted(object? parameter)
        {
            var newItem = new ActionItem
            {
                Description = "New Action Item",
                DueDate = DateTime.Now.AddDays(7),
                Owner = _data?.TeamMember ?? new TeamMember()
            };
            _actionItems.Add(newItem);
            SelectedActionItem = newItem;
            UpdateChangedValues("ActionItems", _actionItems.Count);
        }

        private bool CanEditOrDeleteTask(object? parameter)
        {
            return SelectedActionItem != null;
        }

        private void EditTaskExecuted(object? parameter)
        {
            // For now, selection allows inline editing in the DataGrid
            if (SelectedActionItem != null)
            {
                UpdateChangedValues("ActionItems", _actionItems.Count);
            }
        }

        private void DeleteTaskExecuted(object? parameter)
        {
            if (SelectedActionItem != null)
            {
                _actionItems.Remove(SelectedActionItem);
                SelectedActionItem = null;
                UpdateChangedValues("ActionItems", _actionItems.Count);
            }
        }

        #endregion

        #region Private Methods - Linking Items

        private bool CanLinkTask(object? obj) => SelectedAvailableTask != null;
        private bool CanLinkOkr(object? obj) => SelectedAvailableOkr != null;
        private bool CanLinkKpi(object? obj) => SelectedAvailableKpi != null;
        private bool CanUnlinkTask(object? obj) => SelectedLinkedTask != null;
        private bool CanUnlinkOkr(object? obj) => SelectedLinkedOkr != null;
        private bool CanUnlinkKpi(object? obj) => SelectedLinkedKpi != null;

        private void LinkTaskExecuted(object? parameter)
        {
            if (SelectedAvailableTask == null || _data == null) return;

            // Check if already linked
            if (_linkedTasks.Any(lt => lt.TaskId == SelectedAvailableTask.Id))
            {
                NotificationManager.Instance.ShowWarning("Already Linked", "This task is already linked to this meeting.");
                return;
            }

            var link = new OneOnOneLinkedTask
            {
                OneOnOneId = _data.Id > 0 ? _data.Id : 0, // Will be set when meeting is saved
                TaskId = SelectedAvailableTask.Id,
                Task = SelectedAvailableTask,
                DiscussionNotes = string.Empty
            };

            _linkedTasks.Add(link);
            SelectedAvailableTask = null;
            RaisePropertyChanged(nameof(LinkedTasks));
        }

        private void LinkOkrExecuted(object? parameter)
        {
            if (SelectedAvailableOkr == null || _data == null) return;

            if (_linkedOkrs.Any(lo => lo.OkrId == SelectedAvailableOkr.ObjectiveId))
            {
                NotificationManager.Instance.ShowWarning("Already Linked", "This OKR is already linked to this meeting.");
                return;
            }

            var link = new OneOnOneLinkedOkr
            {
                OneOnOneId = _data.Id > 0 ? _data.Id : 0,
                OkrId = SelectedAvailableOkr.ObjectiveId,
                Okr = SelectedAvailableOkr,
                DiscussionNotes = string.Empty
            };

            _linkedOkrs.Add(link);
            SelectedAvailableOkr = null;
            RaisePropertyChanged(nameof(LinkedOkrs));
        }

        private void LinkKpiExecuted(object? parameter)
        {
            if (SelectedAvailableKpi == null || _data == null) return;

            if (_linkedKpis.Any(lk => lk.KpiId == SelectedAvailableKpi.KpiId))
            {
                NotificationManager.Instance.ShowWarning("Already Linked", "This KPI is already linked to this meeting.");
                return;
            }

            var link = new OneOnOneLinkedKpi
            {
                OneOnOneId = _data.Id > 0 ? _data.Id : 0,
                KpiId = SelectedAvailableKpi.KpiId,
                Kpi = SelectedAvailableKpi,
                DiscussionNotes = string.Empty
            };

            _linkedKpis.Add(link);
            SelectedAvailableKpi = null;
            RaisePropertyChanged(nameof(LinkedKpis));
        }

        private async void UnlinkTaskExecuted(object? parameter)
        {
            if (SelectedLinkedTask == null || _data?.Id == null) return;

            if (_data.Id > 0)
            {
                // Meeting already saved - remove from database
                await TrackerDbManager.Instance.UnlinkTaskFromMeetingAsync(_data.Id, SelectedLinkedTask.TaskId);
            }

            _linkedTasks.Remove(SelectedLinkedTask);
            SelectedLinkedTask = null;
            RaisePropertyChanged(nameof(LinkedTasks));
        }

        private async void UnlinkOkrExecuted(object? parameter)
        {
            if (SelectedLinkedOkr == null || _data?.Id == null) return;

            if (_data.Id > 0)
            {
                await TrackerDbManager.Instance.UnlinkOkrFromMeetingAsync(_data.Id, SelectedLinkedOkr.OkrId);
            }

            _linkedOkrs.Remove(SelectedLinkedOkr);
            SelectedLinkedOkr = null;
            RaisePropertyChanged(nameof(LinkedOkrs));
        }

        private async void UnlinkKpiExecuted(object? parameter)
        {
            if (SelectedLinkedKpi == null || _data?.Id == null) return;

            if (_data.Id > 0)
            {
                await TrackerDbManager.Instance.UnlinkKpiFromMeetingAsync(_data.Id, SelectedLinkedKpi.KpiId);
            }

            _linkedKpis.Remove(SelectedLinkedKpi);
            SelectedLinkedKpi = null;
            RaisePropertyChanged(nameof(LinkedKpis));
        }

        /// <summary>
        /// Rolls over uncompleted action items and follow-ups from previous meetings into this meeting.
        /// </summary>
        private void RolloverUncompletedItemsExecuted(object? parameter)
        {
            int addedCount = 0;

            // Add uncompleted action items
            foreach (var item in _uncompletedActionItems)
            {
                if (!_actionItems.Any(ai => ai.Id == item.Id))
                {
                    _actionItems.Add(item);
                    addedCount++;
                }
            }

            // Add uncompleted follow-up items
            foreach (var item in _uncompletedFollowUpItems)
            {
                if (!_followUpItems.Any(fi => fi.Id == item.Id))
                {
                    _followUpItems.Add(item);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                NotificationManager.Instance.ShowSuccess("Items Rolled Over", $"Added {addedCount} uncompleted item(s) from previous meetings.");
                RaisePropertyChanged(nameof(ActionItems));
                RaisePropertyChanged(nameof(FollowUpItems));
            }
            else
            {
                NotificationManager.Instance.ShowInfo("No Items", "No uncompleted items to roll over.");
            }
        }

        #endregion
    }
}
