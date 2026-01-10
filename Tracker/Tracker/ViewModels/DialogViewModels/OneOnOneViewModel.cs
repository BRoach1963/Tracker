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
    /// ViewModel for creating and editing 1:1 meetings.
    /// Redesigned: Single panel layout with AutoSuggest team member picker and RichTextEditor notes.
    /// </summary>
    public class OneOnOneViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("OneOnOneVM");
        
        private OneOnOne? _data;
        private ObservableCollection<AgendaItem> _agendaItems = new();
        private ObservableCollection<MeetingTask> _tasks = new();
        
        // Team member search
        private ObservableCollection<TeamMember> _allTeamMembers = new();
        private ObservableCollection<TeamMember> _filteredTeamMembers = new();
        private string _teamMemberSearchText = string.Empty;

        // Linked items (existing tasks/OKRs/KPIs discussed in this meeting)
        private ObservableCollection<OneOnOneLinkedTask> _linkedTasks = new();
        private ObservableCollection<OneOnOneLinkedOkr> _linkedOkrs = new();
        private ObservableCollection<OneOnOneLinkedKpi> _linkedKpis = new();

        // Available items for linking (from database)
        private ObservableCollection<IndividualTask> _availableTasks = new();
        private ObservableCollection<ObjectiveKeyResult> _availableOkrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _availableKpis = new();

        // Previous meeting and uncompleted tasks
        private OneOnOne? _previousMeeting;
        private ObservableCollection<MeetingTask> _uncompletedTasks = new();

        // Meeting templates
        private ObservableCollection<MeetingTemplate> _templates = new();
        private MeetingTemplate? _selectedTemplate;

        // Status options
        private readonly MeetingStatusEnum[] _statuses = Enum.GetValues<MeetingStatusEnum>();

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
        private MeetingTask? _selectedTask;

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

        public OneOnOneViewModel(Action? callback, OneOnOne data, bool edit = true, TeamMember? teamMember = null) : base(callback)
        {
            _inEditMode = edit;
            _data = data;
            if (teamMember != null && !_inEditMode) _data.TeamMember = teamMember;
            SetLists();
            LoadTeamMembers();
            LoadPreviousMeetingAndUncompletedTasks();
            LoadAvailableItemsForLinking();
            LoadLinkedItems();
            LoadTemplates();
            
            // Initialize search text if team member already selected
            if (_data?.TeamMember != null && _data.TeamMember.Id != Guid.Empty)
            {
                _teamMemberSearchText = _data.TeamMember.FullName;
            }

            // If editing an existing meeting with calendar sync, refresh time from calendar first
            if (!_inEditMode && _data?.Id > 0)
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
            if (_data != null)
            {
                _originalDate = _data.Date;
                _originalStartTime = _data.StartTime;
                _originalEndTime = _data.EndTime;
                _timeFieldsChangedByUser = false;
            }
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
            if (_data == null) return;

            // Check if meeting is synced to any calendar
            bool hasSyncedCalendar = !string.IsNullOrEmpty(_data.CalendarEventId) || 
                                     !string.IsNullOrEmpty(_data.GoogleCalendarEventId);

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

        private async void LoadTemplates()
        {
            try
            {
                var templates = await TrackerDbManager.Instance.GetMeetingTemplatesAsync().ConfigureAwait(false);

                // If no templates exist, create defaults
                if (templates.Count == 0)
                {
                    await TrackerDbManager.Instance.CreateDefaultTemplatesAsync().ConfigureAwait(false);
                    templates = await TrackerDbManager.Instance.GetMeetingTemplatesAsync().ConfigureAwait(false);
                }

                // Replace entire collection at once instead of Clear() + Add() loop
                _templates = new ObservableCollection<MeetingTemplate>(templates);
                RaisePropertyChanged(nameof(Templates));
            }
            catch
            {
                // Ignore errors loading templates
            }
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

        private ICommand? _linkAgendaItemCommand;
        public ICommand LinkAgendaItemCommand =>
            _linkAgendaItemCommand ??= new TrackerCommand(LinkAgendaItemExecuted);

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
        private ICommand? _rolloverUncompletedTasksCommand;

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

        public ICommand RolloverUncompletedTasksCommand =>
            _rolloverUncompletedTasksCommand ??= new TrackerCommand(RolloverUncompletedTasksExecuted);

        #endregion

        #region Public Properties

        public int Id => _data.Id;

        public OneOnOne Data => _data;

        public ObservableCollection<AgendaItem> AgendaItems => _agendaItems;

        public ObservableCollection<MeetingTask> Tasks => _tasks;

        // Linked items
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
                       $"Tasks: {_previousMeeting.Tasks.Count}\n" +
                       $"Agenda Items: {_previousMeeting.AgendaItems.Count}";
            }
        }

        // Uncompleted tasks from previous meetings
        public ObservableCollection<MeetingTask> UncompletedTasks => _uncompletedTasks;

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

        private bool CanViewPrep(object? obj) => SelectedTeamMember != null && _data != null;

        private void ViewPrepExecuted(object? parameter)
        {
            if (SelectedTeamMember == null || _data == null) return;

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
            var currentAgenda = AgendaMarkdown ?? string.Empty;
            if (!string.IsNullOrEmpty(currentAgenda) && !currentAgenda.EndsWith("\n"))
            {
                currentAgenda += "\n";
            }
            AgendaMarkdown = currentAgenda + "- " + itemText + "\n";

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
            if (_data == null) return;

            _data.Date = date;
            _data.StartTime = startTime;
            _data.EndTime = endTime;
            _data.Duration = endTime - startTime;

            RaisePropertyChanged(nameof(Meeting));
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
            if (SelectedTemplate == null) return;

            // Apply template duration
            if (_data != null)
            {
                _data.Duration = TimeSpan.FromMinutes(SelectedTemplate.SuggestedDurationMinutes);
                _data.EndTime = _data.StartTime.Add(_data.Duration);
            }

            // Apply template items to agenda
            foreach (var templateItem in SelectedTemplate.Items.OrderBy(i => i.SortOrder))
            {
                var agendaItem = new AgendaItem
                {
                    Description = templateItem.Description,
                    Category = templateItem.Category,
                    Priority = templateItem.Priority
                };
                _agendaItems.Add(agendaItem);
            }

            RaisePropertyChanged(nameof(AgendaItems));
            NotificationManager.Instance.ShowSuccess("Template Applied", $"Added {SelectedTemplate.Items.Count} agenda items from '{SelectedTemplate.Name}'");
        }

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
            set { _selectedAvailableTask = value; RaisePropertyChanged(); }
        }

        public ObjectiveKeyResult? SelectedAvailableOkr
        {
            get => _selectedAvailableOkr;
            set { _selectedAvailableOkr = value; RaisePropertyChanged(); }
        }

        public KeyPerformanceIndicator? SelectedAvailableKpi
        {
            get => _selectedAvailableKpi;
            set { _selectedAvailableKpi = value; RaisePropertyChanged(); }
        }

        public OneOnOneLinkedTask? SelectedLinkedTask
        {
            get => _selectedLinkedTask;
            set { _selectedLinkedTask = value; RaisePropertyChanged(); }
        }

        public OneOnOneLinkedOkr? SelectedLinkedOkr
        {
            get => _selectedLinkedOkr;
            set { _selectedLinkedOkr = value; RaisePropertyChanged(); }
        }

        public OneOnOneLinkedKpi? SelectedLinkedKpi
        {
            get => _selectedLinkedKpi;
            set { _selectedLinkedKpi = value; RaisePropertyChanged(); }
        }

        // Selected items for editing/deleting
        public AgendaItem? SelectedAgendaItem
        {
            get => _selectedAgendaItem;
            set { _selectedAgendaItem = value; RaisePropertyChanged(); }
        }

        public MeetingTask? SelectedTask
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
            get => _data?.TeamMember;
            set
            {
                if (value != null && _data != null)
                {
                    _data.TeamMember = value;
                    _teamMemberSearchText = value.FullName;
                    
                    // Clear filtered items to close the popup
                    _filteredTeamMembers.Clear();
                    
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TeamMember));
                    RaisePropertyChanged(nameof(TeamMemberName));
                    RaisePropertyChanged(nameof(TeamMemberSearchText));
                    RaisePropertyChanged(nameof(FilteredTeamMembers));
                    UpdateChangedValues(TrackerConstants.OneOnOneTeamMemberId, value.Id);
                    
                    // Load previous meeting for newly selected member
                    LoadPreviousMeetingAndUncompletedTasks();
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

        // Agenda (free-form) as Markdown
        public string AgendaMarkdown
        {
            get => _data?.Agenda ?? string.Empty;
            set
            {
                if (_data != null)
                {
                    _data.Agenda = value;
                    RaisePropertyChanged();
                    UpdateChangedValues("Agenda", value);
                }
            }
        }

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

        /// <summary>
        /// Available meeting statuses for the dropdown.
        /// </summary>
        public MeetingStatusEnum[] Statuses => _statuses;

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
                
                // Track if user changed time fields
                if (value != _originalDate)
                {
                    _timeFieldsChangedByUser = true;
                }
            }
        }

        public TimeSpan StartTime
        {
            get => _data.StartTime;
            set
            {
                _data.StartTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartTimeDateTime));
                UpdateChangedValues(TrackerConstants.OneOnOneStartTime, _data.StartTime.ToString(@"hh\:mm\:ss"));
                
                // Track if user changed time fields
                if (value != _originalStartTime)
                {
                    _timeFieldsChangedByUser = true;
                }
            }
        }

        public TimeSpan EndTime
        {
            get => _data.EndTime;
            set
            {
                _data.EndTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EndTimeDateTime));
                Duration = EndTime - StartTime;
                
                // Track if user changed time fields
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
            get => DateTime.Today.Add(_data.StartTime);
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
            get => DateTime.Today.Add(_data.EndTime);
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
            }
        }

        #endregion

        #region Private Methods - Main

        private bool CanExecuteAddOneOnOne(object? obj)
        {
            if (_data.TeamMember == null || _data.TeamMember.Id == Guid.Empty) return false;
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

        private async Task SaveRelatedItems(int oneOnOneId)
        {
            _data.AgendaItems = _agendaItems.ToList();
            _data.Tasks = _tasks.ToList();
            await TrackerDataManager.Instance.UpdateOneOnOne(_data);
        }

        private async Task SaveLinkedItems(int oneOnOneId)
        {
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
            if (_inEditMode)
            {
                Date = DateTime.Now.Date;
                StartTime = DateTime.Now.TimeOfDay;
                EndTime = DateTime.Now.TimeOfDay + new TimeSpan(0, 1, 0, 0);
            }
            else
            {
                if (_data.AgendaItems != null) _agendaItems = new ObservableCollection<AgendaItem>(_data.AgendaItems);
                if (_data.Tasks != null) _tasks = new ObservableCollection<MeetingTask>(_data.Tasks);
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

        private async void LoadPreviousMeetingAndUncompletedTasks()
        {
            if (_data?.TeamMember?.Id == null || _data.TeamMember.Id == Guid.Empty) return;

            try
            {
                var excludeId = _inEditMode ? _data.Id : (int?)null;
                PreviousMeeting = await TrackerDbManager.Instance.GetPreviousOneOnOneAsync(_data.TeamMember.Id, excludeId);

                var uncompletedTasks = await TrackerDbManager.Instance.GetUncompletedMeetingTasksAsync(_data.TeamMember.Id);
                _uncompletedTasks = new ObservableCollection<MeetingTask>(uncompletedTasks);
                RaisePropertyChanged(nameof(UncompletedTasks));
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading previous meeting: {0}", ex.Message);
            }
        }

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
                _logger.Warn("Error loading available items: {0}", ex.Message);
            }
        }

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

        #region Private Methods - Agenda Items

        private void AddAgendaItemExecuted(object? parameter)
        {
            var newItem = new AgendaItem
            {
                Description = "New Agenda Item",
                Category = AgendaItemCategory.Topic,
                Priority = Severity.Medium
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

        private void LinkAgendaItemExecuted(object? parameter)
        {
            if (parameter is not AgendaItem agendaItem) return;

            // Show a dialog to select what to link this agenda item to
            var linkItems = new List<(string Title, LinkedItemType Type, int Id)>();
            
            // Add available tasks
            foreach (var task in _availableTasks.Where(t => !t.IsDeleted))
            {
                linkItems.Add((task.Description, LinkedItemType.Task, task.Id));
            }
            
            // Add available OKRs
            foreach (var okr in _availableOkrs.Where(o => !o.IsDeleted))
            {
                linkItems.Add((okr.Title, LinkedItemType.OKR, okr.ObjectiveId));
            }
            
            // Add available KPIs
            foreach (var kpi in _availableKpis.Where(k => !k.IsDeleted))
            {
                linkItems.Add((kpi.Name, LinkedItemType.KPI, kpi.KpiId));
            }

            if (linkItems.Count > 0)
            {
                var dialog = new Views.Dialogs.LinkAgendaItemDialog(linkItems, agendaItem)
                {
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) 
                            ?? System.Windows.Application.Current.MainWindow
                };
                if (dialog.ShowDialog() == true && dialog.SelectedItem.HasValue)
                {
                    var selected = dialog.SelectedItem.Value;
                    
                    // Check if already linked
                    if (agendaItem.LinkedItems.Any(li => li.Type == selected.Type && li.ItemId == selected.Id))
                    {
                        NotificationManager.Instance.ShowInfo("Already Linked", "This item is already linked.");
                        return;
                    }
                    
                    // Add to the LinkedItems collection (supports multiple links)
                    agendaItem.AddLinkedItem(selected.Type, selected.Id, selected.Title);
                    UpdateChangedValues("AgendaItems", _agendaItems.Count);
                }
            }
            else
            {
                NotificationManager.Instance.ShowInfo("No Items", "No tasks, OKRs, or KPIs available to link. Create some first!");
            }
        }

        #endregion

        #region Private Methods - Tasks

        private void AddTaskExecuted(object? parameter)
        {
            var newItem = new MeetingTask
            {
                Description = "New Task",
                DueDate = DateTime.Now.AddDays(7),
                Owner = _data?.TeamMember ?? new TeamMember()
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

            if (_linkedTasks.Any(lt => lt.TaskId == SelectedAvailableTask.Id))
            {
                NotificationManager.Instance.ShowWarning("Already Linked", "This task is already linked to this meeting.");
                return;
            }

            var link = new OneOnOneLinkedTask
            {
                OneOnOneId = _data.Id > 0 ? _data.Id : 0,
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

        private void RolloverUncompletedTasksExecuted(object? parameter)
        {
            int addedCount = 0;

            foreach (var item in _uncompletedTasks)
            {
                if (!_tasks.Any(t => t.Id == item.Id))
                {
                    _tasks.Add(item);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                NotificationManager.Instance.ShowSuccess("Tasks Rolled Over", $"Added {addedCount} uncompleted task(s) from previous meetings.");
                RaisePropertyChanged(nameof(Tasks));
            }
            else
            {
                NotificationManager.Instance.ShowInfo("No Tasks", "No uncompleted tasks to roll over.");
            }
        }

        #endregion
    }
}
