using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Services;
using Tracker.Services.Kudos;
using Tracker.Services.Recognition;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the Send Kudos dialog.
    /// Allows managers to compose and send kudos to team members.
    /// </summary>
    public class SendKudosViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly KudosService _kudosService;

        private TeamMember? _selectedTeamMember;
        private string _title = string.Empty;
        private string _message = string.Empty;
        private KudosCategory _selectedCategory = KudosCategory.TeamWork;
        private DeliveryChannel _selectedChannel = DeliveryChannel.InternalOnly;
        private bool _isPublic;
        private bool _isSending;
        private string _statusMessage = string.Empty;
        private bool _hasError;
        private bool _showWebhookSetup;
        private string _teamsWebhookUrl = string.Empty;

        private ICommand? _sendCommand;
        private ICommand? _cancelCommand;
        private ICommand? _testTeamsCommand;
        private ICommand? _testSlackCommand;
        private ICommand? _saveWebhookCommand;

        #endregion

        #region Constructor

        public SendKudosViewModel(Action? callback) : this(callback, null)
        {
        }

        public SendKudosViewModel(Action? callback, TeamMember? preselectedMember) : base(callback)
        {
            _logger = LoggingManager.GetComponentLogger("SendKudos");
            _kudosService = KudosService.Instance;

            // Load team members and optionally preselect
            _ = LoadTeamMembersAsync(preselectedMember);

            // Load saved webhook URL
            _teamsWebhookUrl = TeamsWebhookConfig.WebhookUrl ?? string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Available team members to send kudos to.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers { get; } = new();

        /// <summary>
        /// Selected team member.
        /// </summary>
        public TeamMember? SelectedTeamMember
        {
            get => _selectedTeamMember;
            set
            {
                _selectedTeamMember = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>
        /// Optional kudos title/headline.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The kudos message.
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>
        /// Available kudos categories.
        /// </summary>
        public IEnumerable<KudosCategory> Categories => Enum.GetValues<KudosCategory>();

        /// <summary>
        /// Selected category.
        /// </summary>
        public KudosCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether to send via Teams.
        /// </summary>
        public bool SendViaTeams
        {
            get => _selectedChannel == DeliveryChannel.MicrosoftTeams;
            set
            {
                if (value)
                {
                    _selectedChannel = DeliveryChannel.MicrosoftTeams;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SendViaSlack));
                    RaisePropertyChanged(nameof(SendInternalOnly));
                }
            }
        }

        /// <summary>
        /// Whether to send via Slack.
        /// </summary>
        public bool SendViaSlack
        {
            get => _selectedChannel == DeliveryChannel.Slack;
            set
            {
                if (value)
                {
                    _selectedChannel = DeliveryChannel.Slack;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SendViaTeams));
                    RaisePropertyChanged(nameof(SendInternalOnly));
                }
            }
        }

        /// <summary>
        /// Whether to log internally only (no delivery).
        /// </summary>
        public bool SendInternalOnly
        {
            get => _selectedChannel == DeliveryChannel.InternalOnly;
            set
            {
                if (value)
                {
                    _selectedChannel = DeliveryChannel.InternalOnly;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SendViaTeams));
                    RaisePropertyChanged(nameof(SendViaSlack));
                }
            }
        }

        /// <summary>
        /// Whether Teams is available.
        /// </summary>
        public bool TeamsAvailable => TeamsDeliveryProvider.Instance.IsAvailable;

        /// <summary>
        /// Whether Slack is available.
        /// </summary>
        public bool SlackAvailable => SlackDeliveryProvider.Instance.IsAvailable;

        /// <summary>
        /// Whether to also post publicly to a channel.
        /// </summary>
        public bool IsPublic
        {
            get => _isPublic;
            set
            {
                _isPublic = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether a send operation is in progress.
        /// </summary>
        public bool IsSending
        {
            get => _isSending;
            set
            {
                _isSending = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>
        /// Status message to display.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

        /// <summary>
        /// Whether the current status is an error.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether to show the webhook setup section.
        /// </summary>
        public bool ShowWebhookSetup
        {
            get => _showWebhookSetup;
            set
            {
                _showWebhookSetup = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Teams webhook URL.
        /// </summary>
        public string TeamsWebhookUrl
        {
            get => _teamsWebhookUrl;
            set
            {
                _teamsWebhookUrl = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Instructions for setting up Teams webhook.
        /// </summary>
        public string TeamsSetupInstructions => TeamsDeliveryProvider.Instance.GetSetupInstructions();

        /// <summary>
        /// Instructions for setting up Slack.
        /// </summary>
        public string SlackSetupInstructions => SlackDeliveryProvider.Instance.GetSetupInstructions();

        /// <summary>
        /// Whether the send button should be enabled.
        /// </summary>
        public bool CanSend => !IsSending &&
                               SelectedTeamMember != null &&
                               !string.IsNullOrWhiteSpace(Message);

        #endregion

        #region Commands

        public ICommand SendCommand => _sendCommand ??= new TrackerCommand(async _ => await SendKudosAsync());

        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(_ =>
        {
            DialogResult.Cancelled = true;
            Callback?.Invoke();
        });

        public ICommand TestTeamsCommand => _testTeamsCommand ??= new TrackerCommand(async _ => await TestTeamsAsync());

        public ICommand TestSlackCommand => _testSlackCommand ??= new TrackerCommand(async _ => await TestSlackAsync());

        public ICommand SaveWebhookCommand => _saveWebhookCommand ??= new TrackerCommand(_ => SaveWebhookUrl());

        #endregion

        #region Private Methods

        private async Task LoadTeamMembersAsync(TeamMember? preselectedMember = null)
        {
            try
            {
                var members = await Managers.TrackerDataManager.Instance.GetTeamData();
                foreach (var member in members.Where(m => m.IsActive).OrderBy(m => m.FullName))
                {
                    TeamMembers.Add(member);
                }

                // Pre-select the team member if specified
                if (preselectedMember != null)
                {
                    SelectedTeamMember = TeamMembers.FirstOrDefault(m => m.Id == preselectedMember.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading team members");
            }
        }

        private async Task SendKudosAsync()
        {
            if (!CanSend || SelectedTeamMember == null) return;

            IsSending = true;
            HasError = false;
            StatusMessage = "Saving kudos...";

            try
            {
                // Get the current user's team member ID (sender)
                var fromTeamMemberId = OrganizationContext.Current.UserIdOrNull;
                if (!fromTeamMemberId.HasValue)
                {
                    StatusMessage = "❌ Error: User context not available";
                    HasError = true;
                    return;
                }

                var options = new KudosOptions
                {
                    Title = string.IsNullOrWhiteSpace(Title) ? null : Title,
                    BadgeType = MapCategoryToBadgeType(SelectedCategory),
                    IsPublic = IsPublic
                };

                var kudos = await _kudosService.CreateKudosAsync(
                    fromTeamMemberId.Value,
                    SelectedTeamMember.Id,
                    Message,
                    options);

                if (kudos != null)
                {
                    StatusMessage = $"✅ Kudos sent to {SelectedTeamMember.FullName}!";
                    HasError = false;
                    _logger.Info("Kudos created successfully for {0}", SelectedTeamMember.FullName);

                    // Close after a short delay
                    await Task.Delay(1500);
                    Callback?.Invoke();
                }
                else
                {
                    StatusMessage = "❌ Failed to save kudos";
                    HasError = true;
                    _logger.Warn("Failed to create kudos");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                HasError = true;
                _logger.Exception(ex, "Error sending kudos");
            }
            finally
            {
                IsSending = false;
            }
        }

        /// <summary>
        /// Maps the legacy KudosCategory enum to the new BadgeType string.
        /// </summary>
        private static string MapCategoryToBadgeType(KudosCategory category)
        {
            return category switch
            {
                KudosCategory.TeamWork => "team_player",
                KudosCategory.Innovation => "innovator",
                KudosCategory.Leadership => "leader",
                KudosCategory.CustomerFocus => "customer_focus",
                KudosCategory.GoingAboveBeyond => "above_and_beyond",
                KudosCategory.ProblemSolving => "problem_solver",
                KudosCategory.LearningGrowth => "learner",
                KudosCategory.Reliability => "reliable",
                KudosCategory.Communication => "communicator",
                KudosCategory.Other => "other",
                _ => "other"
            };
        }

        private async Task TestTeamsAsync()
        {
            IsSending = true;
            StatusMessage = "Testing Teams connection...";
            HasError = false;

            try
            {
                var success = await TeamsDeliveryProvider.Instance.TestConnectionAsync();
                if (success)
                {
                    StatusMessage = "✅ Teams connection successful! Check your channel.";
                    RaisePropertyChanged(nameof(TeamsAvailable));
                }
                else
                {
                    StatusMessage = "❌ Teams connection failed. Check your webhook URL.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsSending = false;
            }
        }

        private async Task TestSlackAsync()
        {
            IsSending = true;
            StatusMessage = "Testing Slack connection...";
            HasError = false;

            try
            {
                var success = await SlackDeliveryProvider.Instance.TestConnectionAsync();
                if (success)
                {
                    StatusMessage = "✅ Slack connection successful!";
                }
                else
                {
                    StatusMessage = "❌ Slack connection failed. Make sure you're connected to Slack.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsSending = false;
            }
        }

        private void SaveWebhookUrl()
        {
            TeamsWebhookConfig.SetWebhookUrl(TeamsWebhookUrl);
            StatusMessage = "✅ Teams webhook URL saved!";
            HasError = false;
            RaisePropertyChanged(nameof(TeamsAvailable));
        }

        #endregion
    }
}
