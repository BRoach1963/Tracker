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
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Google;
using Tracker.Services.MeetingPrep;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing meetings.
    /// Redesigned: Single panel layout with AutoSuggest team member picker and RichTextEditor notes.
    /// </summary>
    public class MeetingViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("OneOnOneVM");
        
        private readonly Meeting _data;
        private ObservableCollection<AgendaItem> _agendaItems = new();
        private ObservableCollection<TrackerTask> _tasks = new();
        
        // Team member search
        private ObservableCollection<TeamMember> _allTeamMembers = new();
        private ObservableCollection<TeamMember> _filteredTeamMembers = new();
        private string _teamMemberSearchText = string.Empty;

        // Available items for linking (from database)
        private ObservableCollection<TrackerTask> _availableTasks = new();
        private ObservableCollection<Goal> _availableOkrs = new();
        private ObservableCollection<Metric> _availableKpis = new();

        // Meeting templates (populated once meeting templates are migrated)
        private ObservableCollection<MeetingTemplate> _templates = new();
        private MeetingTemplate? _selectedTemplate;

        // Status options
        private readonly MeetingStatus[] _statuses = Enum.GetValues<MeetingStatus>();

        private bool _inEditMode;
        private Dictionary<string, object> _changedProperties = new();

        public bool InEditMode => _inEditMode;

        // Main commands
        private ICommand? _updateOneOnOneCommand;
        private ICommand? _addOneOnOneCommand;

        // Agenda Item commands
        private ICommand? _addAgendaItemCommand;
        private ICommand? _editAgendaItemCommand;
        private ICommand? _deleteAgendaItemCommand;
        private AgendaItem? _selectedAgendaItem;

        // Task commands
        private ICommand? _addTaskCommand;
        private ICommand? _editTaskCommand;
        private ICommand? _deleteTaskCommand;
        private TrackerTask? _selectedTask;

        // Template command
        private ICommand? _applyTemplateCommand;

        // Quick Message commands
        private ICommand? _sendMessageCommand;
        private ICommand? _sendSummaryCommand;

        // Teams Meeting
        private bool _createTeamsMeeting;
        private bool _syncToOutlook;
        private ICommand? _copyTeamsMeetingLinkCommand;

        // Google Meet
        private bool _createGoogleMeet;
        private ICommand? _copyGoogleMeetLinkCommand;

        // Meeting Prep
        private ICommand? _viewPrepCommand;
        private bool _isPrepPanelVisible;
        private MeetingPrepViewModel? _meetingPrepViewModel;

        // Time field change tracking (for calendar sync - only push time if explicitly changed)
        private DateTime _originalDate;
        private TimeSpan _originalStartTime;
        private TimeSpan _originalEndTime;
        private bool _timeFieldsChangedByUser;

        #endregion

        #region Ctor

        public MeetingViewModel(Action? callback, Meeting data, bool edit = true, TeamMember? teamMember = null) : base(callback)
        {
            _inEditMode = edit;
            _data = data ?? throw new ArgumentNullException(nameof(data));

            // For new meetings, initialize the report (direct report) from the provided team member
            if (teamMember != null && !_inEditMode)
            {
                _data.Report = teamMember;
                _data.ReportTeamMemberId = teamMember.Id;
            }

            SetLists();
            LoadTeamMembers();
            LoadTemplates();

            // Initialize search text if team member already selected
            if (_data.Report != null && _data.Report.Id != Guid.Empty)
            {
                _teamMemberSearchText = _data.Report.FullName;
            }

            // If editing an existing meeting with calendar sync, refresh time from calendar first
            if (_inEditMode && _data.Id != Guid.Empty)
            {
                RefreshTimeFromCalendar();
            }

            // Store original time values for change detection
            StoreOriginalTimeValues();
        }

        /// <summary>
        /// Stores the current time values so we can detect if user explicitly changed them.
        /// </summary>
        private void StoreOriginalTimeValues()
        {
            _originalDate = _data.ScheduledAt.Date;
            _originalStartTime = _data.ScheduledAt.TimeOfDay;
            var duration = TimeSpan.FromMinutes(_data.DurationMinutes ?? 0);
            _originalEndTime = _originalStartTime + duration;
            _timeFieldsChangedByUser = false;
        }

        /// <summary>
        /// Checks if the user has changed any time fields since opening the dialog.
        /// </summary>
        public bool TimeFieldsChangedByUser => _timeFieldsChangedByUser;

        /// <summary>
        /// Refreshes the meeting time from the connected calendar (if synced).
        /// Calendar is authoritative for time - this ensures we have the latest.
        /// </summary>
        private async void RefreshTimeFromCalendar()
        {
            // Check if meeting is synced to any calendar
            bool hasSyncedCalendar =
                !string.IsNullOrEmpty(_data.GoogleCalendarEventId) ||
                !string.IsNullOrEmpty(_data.OutlookCalendarEventId);

            if (!hasSyncedCalendar) return;

            try
            {
                var timeUpdated = await CalendarSyncManager.Instance.RefreshTimeFromCalendarAsync(_data);
                
                if (timeUpdated)
                {
                    // Update the UI with the new time values
                    RaisePropertyChanged(nameof(Date));
                    RaisePropertyChanged(nameof(StartTime));
                    RaisePropertyChanged(nameof(EndTime));
                    RaisePropertyChanged(nameof(StartTimeDateTime));
                    RaisePropertyChanged(nameof(EndTimeDateTime));
                    RaisePropertyChanged(nameof(DateDisplay));

                    // Re-store original values after calendar refresh
                    StoreOriginalTimeValues();

                    NotificationManager.Instance.ShowInfo("Calendar Updated", 
                        "Meeting time was updated from your calendar.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to refresh time from calendar");
            }
        }

        private void LoadTemplates()
        {
            // Templates will be loaded from the new data layer in a
            // future iteration of the meeting migration. For now we
            // keep the collection empty to avoid TrackerDbManager
            // dependencies on the legacy schema.
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

        // Agenda Item Commands
        public ICommand AddAgendaItemCommand =>
            _addAgendaItemCommand ??= new TrackerCommand(AddAgendaItemExecuted);

        public ICommand EditAgendaItemCommand =>
            _editAgendaItemCommand ??= new TrackerCommand(EditAgendaItemExecuted, CanEditOrDeleteAgendaItem);

        public ICommand DeleteAgendaItemCommand =>
            _deleteAgendaItemCommand ??= new TrackerCommand(DeleteAgendaItemExecuted);

        // LinkAgendaItemCommand removed with legacy OKR/KPI agenda linking.

        // Task Commands
        public ICommand AddTaskCommand =>
            _addTaskCommand ??= new TrackerCommand(AddTaskExecuted);

        public ICommand EditTaskCommand =>
            _editTaskCommand ??= new TrackerCommand(EditTaskExecuted, CanEditOrDeleteTask);

        public ICommand DeleteTaskCommand =>
            _deleteTaskCommand ??= new TrackerCommand(DeleteTaskExecuted, CanEditOrDeleteTask);

        // Rollover command (removed for new meeting model)

        #endregion

        #region Public Properties

        public Guid Id => _data.Id;

        public Meeting Data => _data;

        public ObservableCollection<AgendaItem> AgendaItems => _agendaItems;

        public ObservableCollection<TrackerTask> Tasks => _tasks;

        // Available items for linking
        public ObservableCollection<TrackerTask> AvailableTasks => _availableTasks;
        public ObservableCollection<Goal> AvailableOkrs => _availableOkrs;
        public ObservableCollection<Metric> AvailableKpis => _availableKpis;

        // Meeting templates
        public ObservableCollection<MeetingTemplate> Templates => _templates;

        public MeetingTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ApplyTemplateCommand =>
            _applyTemplateCommand ??= new TrackerCommand(ApplyTemplateExecuted, CanApplyTemplate);

        private bool CanApplyTemplate(object? obj) => SelectedTemplate != null && !_inEditMode;

        // Meeting Prep Command
        public ICommand ViewPrepCommand =>
            _viewPrepCommand ??= new TrackerCommand(ViewPrepExecuted, CanViewPrep);

        /// <summary>
        /// Whether the meeting prep panel is visible.
        /// </summary>
        public bool IsPrepPanelVisible
        {
            get => _isPrepPanelVisible;
            set
            {
                if (_isPrepPanelVisible != value)
                {
                    _isPrepPanelVisible = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// ViewModel for the meeting prep panel.
        /// </summary>
        public MeetingPrepViewModel? MeetingPrepViewModel
        {
            get => _meetingPrepViewModel;
            set
            {
                if (_meetingPrepViewModel != value)
                {
                    _meetingPrepViewModel = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool CanViewPrep(object? obj) => SelectedTeamMember != null;

        private void ViewPrepExecuted(object? parameter)
        {
            if (SelectedTeamMember == null) return;

            // Toggle the prep panel
            if (IsPrepPanelVisible)
            {
                IsPrepPanelVisible = false;
                return;
            }

            // Initialize and show the prep panel
            MeetingPrepViewModel = new MeetingPrepViewModel();
            MeetingPrepViewModel.Initialize(
                _data,
                onAgendaItemAdded: AddPrepItemToAgenda,
                onClose: () => IsPrepPanelVisible = false);
            
            IsPrepPanelVisible = true;
        }

        private void AddPrepItemToAgenda(string itemText)
        {
            if (string.IsNullOrWhiteSpace(itemText)) return;

            // Add to the Agenda property
            var currentAgenda = NotesMarkdown ?? string.Empty;
            if (!string.IsNullOrEmpty(currentAgenda) && !currentAgenda.EndsWith("\n"))
            {
                currentAgenda += "\n";
            }
            NotesMarkdown = currentAgenda + "- " + itemText + "\n";

            _logger.Info("Added prep item to agenda: {0}", itemText);
        }

        /// <summary>
        /// Whether the scheduling assistant control is visible.
        /// </summary>
        public bool IsSchedulingAssistantVisible
        {
            get => _isSchedulingAssistantVisible;
            set
            {
                if (_isSchedulingAssistantVisible != value)
                {
                    _isSchedulingAssistantVisible = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _isSchedulingAssistantVisible;

        /// <summary>
        /// Applies a selected time slot to the meeting.
        /// </summary>
        public void ApplySelectedTimeSlot(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            // Use the ScheduledAt/DurationMinutes representation from the new model
            _data.ScheduledAt = date.Date + startTime;
            _data.DurationMinutes = (int)Math.Round((endTime - startTime).TotalMinutes);

            RaisePropertyChanged(nameof(Data));
            RaisePropertyChanged(nameof(Date));
            RaisePropertyChanged(nameof(StartTime));
            RaisePropertyChanged(nameof(EndTime));
            
            _logger.Info("Applied selected time slot: {0} {1}-{2}", date.Date, startTime, endTime);
        }

        // Quick Message Commands
        public ICommand SendMessageCommand =>
            _sendMessageCommand ??= new TrackerCommand(SendMessageExecuted, CanSendMessage);

        public ICommand SendSummaryCommand =>
            _sendSummaryCommand ??= new TrackerCommand(SendSummaryExecuted, CanSendSummary);

        private bool CanSendMessage(object? obj) => SelectedTeamMember != null && 
            !string.IsNullOrEmpty(SelectedTeamMember.Email) &&
            Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        private bool CanSendSummary(object? obj) => _inEditMode && SelectedTeamMember != null && 
            !string.IsNullOrEmpty(SelectedTeamMember.Email) &&
            Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        private void SendMessageExecuted(object? parameter)
        {
            if (SelectedTeamMember == null || _data == null) return;
            Views.Dialogs.QuickMessageDialog.ShowDialog(SelectedTeamMember, _data);
        }

        private void SendSummaryExecuted(object? parameter)
        {
            if (SelectedTeamMember == null || _data == null) return;
            
            // Open Quick Message dialog with Summary template pre-selected
            var dialog = new Views.Dialogs.QuickMessageDialog();
            var vm = new QuickMessageViewModel(() => dialog.Close());
            vm.Initialize(SelectedTeamMember, _data);
            vm.SelectedTemplate = MessageTemplate.OneOnOneSummary;
            dialog.DataContext = vm;
            dialog.ShowDialog();
        }

        public bool CanShowMessaging => Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        // Teams Meeting Properties
        public bool CreateTeamsMeeting
        {
            get => _createTeamsMeeting;
            set
            {
                _createTeamsMeeting = value;
                RaisePropertyChanged();
            }
        }

        public bool SyncToOutlook
        {
            get => _syncToOutlook;
            set
            {
                _syncToOutlook = value;
                RaisePropertyChanged();
            }
        }

        public bool CanCreateTeamsMeeting => Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        public bool HasTeamsMeetingUrl => !string.IsNullOrEmpty(_data?.TeamsMeetingUrl);

        public string TeamsMeetingUrlPreview => _data?.TeamsMeetingUrl?.Length > 40 
            ? _data.TeamsMeetingUrl.Substring(0, 40) + "..." 
            : _data?.TeamsMeetingUrl ?? "";

        public ICommand CopyTeamsMeetingLinkCommand => _copyTeamsMeetingLinkCommand ??= 
            new TrackerCommand(CopyTeamsMeetingLink, _ => HasTeamsMeetingUrl);

        private void CopyTeamsMeetingLink(object? parameter)
        {
            if (!string.IsNullOrEmpty(_data?.TeamsMeetingUrl))
            {
                System.Windows.Clipboard.SetText(_data.TeamsMeetingUrl);
                NotificationManager.Instance.ShowSuccess("Copied", "Teams meeting link copied to clipboard.");
            }
        }

        // Google Meet Properties
        public bool CreateGoogleMeet
        {
            get => _createGoogleMeet;
            set
            {
                _createGoogleMeet = value;
                RaisePropertyChanged();
            }
        }

        public bool CanCreateGoogleMeet => Services.Google.GoogleAuthService.Instance.IsAuthenticated;

        public bool HasGoogleMeetUrl => !string.IsNullOrEmpty(_data?.GoogleMeetUrl);

        public string GoogleMeetUrlPreview => _data?.GoogleMeetUrl?.Length > 40 
            ? _data.GoogleMeetUrl.Substring(0, 40) + "..." 
            : _data?.GoogleMeetUrl ?? "";

        public ICommand CopyGoogleMeetLinkCommand => _copyGoogleMeetLinkCommand ??= 
            new TrackerCommand(CopyGoogleMeetLink, _ => HasGoogleMeetUrl);

        private void CopyGoogleMeetLink(object? parameter)
        {
            if (!string.IsNullOrEmpty(_data?.GoogleMeetUrl))
            {
                System.Windows.Clipboard.SetText(_data.GoogleMeetUrl);
                NotificationManager.Instance.ShowSuccess("Copied", "Google Meet link copied to clipboard.");
            }
        }

        private void ApplyTemplateExecuted(object? parameter)
        {
            // Meeting templates will be reintroduced once they are
            // aligned with the new Meeting/AgendaItem model.
            NotificationManager.Instance.ShowInfo("Templates Unavailable", "Meeting templates are being updated for the new meeting model.");
        }

        // Selected items for linking (removed with legacy OKR/KPI agenda linking)

        // Selected items for editing/deleting
        public AgendaItem? SelectedAgendaItem
        {
            get => _selectedAgendaItem;
            set { _selectedAgendaItem = value; RaisePropertyChanged(); }
        }

        public TrackerTask? SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; RaisePropertyChanged(); }
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
            get => _data.Report ?? new TeamMember();
            set
            {
                _data.Report = value;
                _data.ReportTeamMemberId = value.Id;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneTeamMemberId, value.Id);
            }
        }

        public string TeamMemberName => _data.Report?.FullName ?? string.Empty;

        // Team Member Search (for AutoSuggestBox)
        public ObservableCollection<TeamMember> AllTeamMembers => _allTeamMembers;
        public ObservableCollection<TeamMember> FilteredTeamMembers => _filteredTeamMembers;

        public string TeamMemberSearchText
        {
            get => _teamMemberSearchText;
            set
            {
                _teamMemberSearchText = value;
                RaisePropertyChanged();
                FilterTeamMembers();
            }
        }

        private void FilterTeamMembers()
        {
            _filteredTeamMembers.Clear();
            
            if (string.IsNullOrWhiteSpace(_teamMemberSearchText))
            {
                foreach (var member in _allTeamMembers)
                    _filteredTeamMembers.Add(member);
            }
            else
            {
                var searchLower = _teamMemberSearchText.ToLower();
                foreach (var member in _allTeamMembers.Where(m =>
                    m.FullName.ToLower().Contains(searchLower) ||
                    m.JobTitle.ToLower().Contains(searchLower) ||
                    m.Email.ToLower().Contains(searchLower)))
                {
                    _filteredTeamMembers.Add(member);
                }
            }
            
            RaisePropertyChanged(nameof(FilteredTeamMembers));
        }

        public TeamMember? SelectedTeamMember
        {
            get => _data.Report;
            set
            {
                if (value != null)
                {
                    _data.Report = value;
                    _data.ReportTeamMemberId = value.Id;
                    _teamMemberSearchText = value.FullName;

                    // Clear filtered items to close the popup
                    _filteredTeamMembers.Clear();

                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TeamMember));
                    RaisePropertyChanged(nameof(TeamMemberName));
                    RaisePropertyChanged(nameof(TeamMemberSearchText));
                    RaisePropertyChanged(nameof(FilteredTeamMembers));
                    UpdateChangedValues(TrackerConstants.OneOnOneTeamMemberId, value.Id);
                }
            }
        }

        // Notes as Markdown (for RichTextEditor)
        public string NotesMarkdown
        {
            get => _data?.Notes ?? string.Empty;
            set
            {
                if (_data != null)
                {
                    _data.Notes = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Notes));
                    UpdateChangedValues(TrackerConstants.OneOnOneNotes, value);
                }
            }
        }

        // AgendaMarkdown has been folded into NotesMarkdown for the new Meeting model.

        public MeetingStatus Status
        {
            get => _data.Status;
            set
            {
                _data.Status = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneStatus, value);
            }
        }

        /// <summary>
        /// Available meeting statuses for the dropdown.
        /// </summary>
        public MeetingStatus[] Statuses => _statuses;

        public bool IsRecurring
        {
            get => !string.IsNullOrEmpty(_data.RecurrenceRule);
            set
            {
                _data.RecurrenceRule = value ? "FREQ=WEEKLY" : null;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneIsRecurring, value);
            }
        }

        public DateTime Date
        {
            get => _data.ScheduledAt.Date;
            set
            {
                var time = _data.ScheduledAt.TimeOfDay;
                _data.ScheduledAt = value.Date + time;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DateDisplay));
                UpdateChangedValues(TrackerConstants.OneOnOneDate, value);

                if (value.Date != _originalDate.Date)
                {
                    _timeFieldsChangedByUser = true;
                }
            }
        }

        public TimeSpan StartTime
        {
            get => _data.ScheduledAt.TimeOfDay;
            set
            {
                var date = _data.ScheduledAt.Date;
                _data.ScheduledAt = date + value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartTimeDateTime));
                UpdateChangedValues(TrackerConstants.OneOnOneStartTime, value.ToString(@"hh\:mm\:ss"));

                if (value != _originalStartTime)
                {
                    _timeFieldsChangedByUser = true;
                }
            }
        }

        public TimeSpan EndTime
        {
            get
            {
                var duration = TimeSpan.FromMinutes(_data.DurationMinutes ?? 0);
                return _data.ScheduledAt.TimeOfDay + duration;
            }
            set
            {
                var startTime = _data.ScheduledAt.TimeOfDay;
                var duration = value - startTime;
                if (duration < TimeSpan.Zero)
                {
                    duration = TimeSpan.Zero;
                }

                _data.DurationMinutes = (int)Math.Round(duration.TotalMinutes);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EndTimeDateTime));
                Duration = duration;

                if (value != _originalEndTime)
                {
                    _timeFieldsChangedByUser = true;
                }
            }
        }

        /// <summary>
        /// DateTime wrapper for StartTime - used by TimePicker which expects DateTime?.
        /// </summary>
        public DateTime? StartTimeDateTime
        {
            get => _data.ScheduledAt;
            set
            {
                if (value.HasValue)
                {
                    StartTime = value.Value.TimeOfDay;
                }
            }
        }

        /// <summary>
        /// DateTime wrapper for EndTime - used by TimePicker which expects DateTime?.
        /// </summary>
        public DateTime? EndTimeDateTime
        {
            get => _data.ScheduledAt.Date + EndTime;
            set
            {
                if (value.HasValue)
                {
                    EndTime = value.Value.TimeOfDay;
                }
            }
        }

        public string DateDisplay
        {
            get => Date == DateTime.Now.Date ? "MM/DD/YYYY" : Date.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out var date))
                {
                    Date = date;
                }
                RaisePropertyChanged();
            }
        }

        public TimeSpan Duration
        {
            get => TimeSpan.FromMinutes(_data.DurationMinutes ?? 0);
            set
            {
                _data.DurationMinutes = (int)Math.Round(value.TotalMinutes);
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.OneOnOneDuration, value);
            }
        }

        #endregion

        #region Private Methods - Main

        private bool CanExecuteAddOneOnOne(object? obj)
        {
            if (!_data.ReportTeamMemberId.HasValue || _data.ReportTeamMemberId.Value == Guid.Empty) return false;
            return !string.IsNullOrEmpty(Description);
        }

        private async void AddOneOnOneExecuted(object? parameter)
        {
            // Attach current agenda items and tasks before persisting
            _data.AgendaItems = _agendaItems.ToList();
            _data.Tasks = _tasks.ToList();

            var result = await TrackerDataManager.Instance.AddOneOnOneMeeting(_data, _data.ReportTeamMemberId);

            if (result > 0)
            {
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

        private bool CanUpdateOneOnOne(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        private async void UpdateOneOnOneExecuted(object? parameter)
        {
            // Attach current agenda items and tasks before persisting
            _data.AgendaItems = _agendaItems.ToList();
            _data.Tasks = _tasks.ToList();

            var success = await TrackerDataManager.Instance.UpdateOneOnOneMeeting(_data);
            
            if (success)
            {
                var settings = UserSettingsManager.Instance.Settings.Calendar;
                if (settings.AutoSyncOnSave)
                {
                    await CalendarSyncManager.Instance.SyncToAllCalendarsAsync(_data!);
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
            if (_data.AgendaItems != null)
            {
                _agendaItems = new ObservableCollection<AgendaItem>(_data.AgendaItems);
            }

            if (_data.Tasks != null)
            {
                _tasks = new ObservableCollection<TrackerTask>(_data.Tasks);
            }
        }

        private async void LoadTeamMembers()
        {
            try
            {
                // Use TrackerDataManager as single source of truth
                var members = await TrackerDataManager.Instance.GetTeamData();
                _allTeamMembers.Clear();
                _filteredTeamMembers.Clear();
                
                foreach (var member in members.Where(m => !m.IsDeleted))
                {
                    _allTeamMembers.Add(member);
                    _filteredTeamMembers.Add(member);
                }
                
                RaisePropertyChanged(nameof(AllTeamMembers));
                RaisePropertyChanged(nameof(FilteredTeamMembers));
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading team members: {0}", ex.Message);
            }
        }

        // Previous meeting/uncompleted-task and OKR/KPI linking logic has been
        // removed as part of the new meeting model migration. Related data will
        // be reintroduced via dedicated analytics and reporting flows.

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

        #region Private Methods - Agenda Items

        private void AddAgendaItemExecuted(object? parameter)
        {
            var newItem = new AgendaItem
            {
                Title = "New Agenda Item",
                SortOrder = _agendaItems.Count
            };
            _agendaItems.Add(newItem);
            SelectedAgendaItem = newItem;
            UpdateChangedValues("AgendaItems", _agendaItems.Count);
        }

        private bool CanEditOrDeleteAgendaItem(object? parameter) => parameter is AgendaItem || SelectedAgendaItem != null;

        private void EditAgendaItemExecuted(object? parameter)
        {
            var item = parameter as AgendaItem ?? SelectedAgendaItem;
            if (item != null)
            {
                UpdateChangedValues("AgendaItems", _agendaItems.Count);
            }
        }

        private void DeleteAgendaItemExecuted(object? parameter)
        {
            var item = parameter as AgendaItem ?? SelectedAgendaItem;
            if (item != null && _agendaItems.Contains(item))
            {
                // Use dispatcher to ensure UI thread execution
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    _agendaItems.Remove(item);
                    if (SelectedAgendaItem == item) SelectedAgendaItem = null;
                    UpdateChangedValues("AgendaItems", _agendaItems.Count);
                });
            }
        }

        // Agenda item linking to legacy OKR/KPI entities has been removed. The
        // new model uses RelatedEntityType/RelatedEntityId on AgendaItem and
        // will be wired up via dedicated flows in a later pass.

        #endregion

        #region Private Methods - Tasks

        private void AddTaskExecuted(object? parameter)
        {
            var newItem = new TrackerTask
            {
                Title = "New Task",
                Description = "New Task",
                DueDate = DateTime.Now.AddDays(7),
                Owner = SelectedTeamMember,
                OwnerTeamMemberId = SelectedTeamMember?.Id,
                MeetingId = _data.Id != Guid.Empty ? _data.Id : (Guid?)null,
                SourceMeetingId = _data.Id != Guid.Empty ? _data.Id : (Guid?)null,
            };
            _tasks.Add(newItem);
            SelectedTask = newItem;
            UpdateChangedValues("Tasks", _tasks.Count);
        }

        private bool CanEditOrDeleteTask(object? parameter) => SelectedTask != null;

        private void EditTaskExecuted(object? parameter)
        {
            if (SelectedTask != null)
            {
                UpdateChangedValues("Tasks", _tasks.Count);
            }
        }

        private void DeleteTaskExecuted(object? parameter)
        {
            if (SelectedTask != null)
            {
                _tasks.Remove(SelectedTask);
                SelectedTask = null;
                UpdateChangedValues("Tasks", _tasks.Count);
            }
        }

        #endregion
    }
}
