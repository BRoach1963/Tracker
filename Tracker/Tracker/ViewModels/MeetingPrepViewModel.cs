using System.Windows.Input;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.DTOs;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.MeetingPrep;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the MeetingPrepPanel control.
    /// Manages meeting prep generation and agenda item additions.
    /// </summary>
    public class MeetingPrepViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("MeetingPrepVM");
        private readonly MeetingPrepService _prepService;
        
        private DTOs.MeetingPrep? _meetingPrep;
        private Meeting? _meeting;
        private bool _isLoading;
        private Action<string>? _onAgendaItemAdded;
        private Action? _onClose;

        #endregion

        #region Properties

        /// <summary>
        /// The generated meeting prep.
        /// </summary>
        public DTOs.MeetingPrep? MeetingPrep
        {
            get => _meetingPrep;
            set
            {
                if (_meetingPrep != value)
                {
                    _meetingPrep = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsEmpty));
                    RaisePropertyChanged(nameof(HasContent));
                }
            }
        }

        /// <summary>
        /// The meeting being prepared for.
        /// </summary>
        public Meeting? Meeting
        {
            get => _meeting;
            set
            {
                if (_meeting != value)
                {
                    _meeting = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether the prep is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsEmpty));
                    RaisePropertyChanged(nameof(HasContent));
                }
            }
        }

        /// <summary>
        /// Whether the prep is empty (no content).
        /// </summary>
        public bool IsEmpty => !IsLoading && (MeetingPrep?.TotalItemCount ?? 0) == 0;

        /// <summary>
        /// Whether there is content to display.
        /// </summary>
        public bool HasContent => !IsLoading && (MeetingPrep?.TotalItemCount ?? 0) > 0;

        #endregion

        #region Commands

        private ICommand? _addToAgendaCommand;
        private ICommand? _refreshCommand;
        private ICommand? _closeCommand;

        /// <summary>
        /// Command to add a prep item to the meeting agenda.
        /// </summary>
        public ICommand AddToAgendaCommand => _addToAgendaCommand ??= new TrackerCommand(
            param => AddToAgenda(param as PrepItem),
            param => param is PrepItem item && !item.IsAddedToAgenda);

        /// <summary>
        /// Command to refresh/regenerate the meeting prep.
        /// </summary>
        public ICommand RefreshCommand => _refreshCommand ??= new TrackerCommand(
            _ => _ = GeneratePrepAsync(),
            _ => !IsLoading && Meeting != null);

        /// <summary>
        /// Command to close the prep panel.
        /// </summary>
        public ICommand CloseCommand => _closeCommand ??= new TrackerCommand(
            _ => _onClose?.Invoke());

        #endregion

        #region Constructor

        public MeetingPrepViewModel()
        {
            _prepService = MeetingPrepService.Instance;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel with a meeting and optional callbacks.
        /// </summary>
        /// <param name="meeting">The meeting to prepare for.</param>
        /// <param name="onAgendaItemAdded">Callback when an item is added to the agenda.</param>
        /// <param name="onClose">Callback when the panel should close.</param>
        public void Initialize(Meeting meeting, Action<string>? onAgendaItemAdded = null, Action? onClose = null)
        {
            Meeting = meeting;
            _onAgendaItemAdded = onAgendaItemAdded;
            _onClose = onClose;

            _ = GeneratePrepAsync();
        }

        /// <summary>
        /// Initializes the ViewModel with a team member and date.
        /// </summary>
        public void Initialize(TeamMember teamMember, DateTime meetingDate, Action<string>? onAgendaItemAdded = null, Action? onClose = null)
        {
            Meeting = new Meeting
            {
                Id = Guid.Empty,
                ReportTeamMemberId = teamMember.Id,
                Report = teamMember,
                ScheduledAt = meetingDate
            };
            _onAgendaItemAdded = onAgendaItemAdded;
            _onClose = onClose;

            _ = GeneratePrepAsync();
        }

        /// <summary>
        /// Generates or refreshes the meeting prep.
        /// </summary>
        public async Task GeneratePrepAsync()
        {
            if (Meeting == null)
            {
                _logger.Warn("Cannot generate prep: no meeting set");
                return;
            }

            IsLoading = true;
            MeetingPrep = null;

            try
            {
                _logger.Info("Generating meeting prep for {0}", Meeting.Report?.FullName ?? "Unknown");
                var prep = await _prepService.GeneratePrepAsync(Meeting);
                MeetingPrep = prep;
                _logger.Info("Meeting prep generated: {0} items", prep.TotalItemCount);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate meeting prep: {0}", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void AddToAgenda(PrepItem? item)
        {
            if (item == null || item.IsAddedToAgenda)
                return;

            var agendaText = item.ToAgendaText();
            
            // Mark as added
            item.IsAddedToAgenda = true;
            
            // Notify the callback
            _onAgendaItemAdded?.Invoke(agendaText);
            
            _logger.Info("Added to agenda: {0}", agendaText);
            
            // Refresh the UI
            RaisePropertyChanged(nameof(MeetingPrep));
        }

        #endregion
    }
}
