using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for managing Pulse Surveys.
    /// Includes support for external survey links and response syncing.
    /// </summary>
    public class PulseSurveysViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("PulseSurveysVM");
        private readonly IPulseSurveyRepository _pulseSurveyRepository;

        private ObservableCollection<PulseSurvey> _surveys = new();
        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<SurveyTokenInfo> _generatedTokens = new();
        private ObservableCollection<SurveyResponse> _surveyResponses = new();

        private PulseSurvey? _selectedSurvey;
        private SurveyQuestion? _selectedQuestion;
        private SurveyTokenInfo? _selectedToken;
        private SurveyResponse? _selectedResponse;

        private bool _isEditing;
        private bool _isNewSurvey;
        private bool _isLoading;
        private bool _isSyncing;
        private bool _isGeneratingLinks;
        private bool _showResultsView;
        private bool _showLinksView;

        private string _statusMessage = string.Empty;
        private string? _supabaseSurveyId;
        private int _cloudResponseCount;
        private int _unsyncedResponseCount;

        // Edit fields
        private string _editTitle = string.Empty;
        private string _editDescription = string.Empty;
        private bool _editIsAnonymous = true;
        private DateTime? _editDueDate;

        #endregion

        #region Constructor

        public PulseSurveysViewModel(IPulseSurveyRepository pulseSurveyRepository)
        {
            _pulseSurveyRepository = pulseSurveyRepository ?? throw new ArgumentNullException(nameof(pulseSurveyRepository));
            // Don't load data in constructor - wait for Loaded event
            // Data will be loaded asynchronously to avoid blocking UI
            DataMessenger.Register(this, OnDataChanged);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DataMessenger.Unregister(this);
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Message Handlers

        private void OnDataChanged(DataChangeInfo info)
        {
            if (info.RefreshAll)
            {
                _logger.Info("Refreshing surveys due to data change");
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        #endregion

        #region Properties - Collections

        public ObservableCollection<PulseSurvey> Surveys
        {
            get => _surveys;
            private set
            {
                _surveys = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        public ObservableCollection<SurveyTokenInfo> GeneratedTokens
        {
            get => _generatedTokens;
            private set
            {
                _generatedTokens = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<SurveyResponse> SurveyResponses
        {
            get => _surveyResponses;
            private set
            {
                _surveyResponses = value;
                RaisePropertyChanged();
            }
        }

        public Array SurveyStatuses => Enum.GetValues(typeof(SurveyStatus));
        public Array QuestionTypes => Enum.GetValues(typeof(SurveyQuestionType));

        #endregion

        #region Properties - Selection & State

        public PulseSurvey? SelectedSurvey
        {
            get => _selectedSurvey;
            set
            {
                _selectedSurvey = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedSurvey));
                RaisePropertyChanged(nameof(SelectedSurveyQuestions));
                RaisePropertyChanged(nameof(CanSendSurvey));
                RaisePropertyChanged(nameof(CanCloseSurvey));
                RaisePropertyChanged(nameof(CanGenerateLinks));
                RaisePropertyChanged(nameof(CanSyncResponses));

                if (_selectedSurvey != null && !IsNewSurvey)
                {
                    LoadSurveyForEditing(_selectedSurvey);
                    _ = LoadSurveyCloudStatusAsync();
                }
            }
        }

        public SurveyQuestion? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                _selectedQuestion = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedQuestion));
            }
        }

        public SurveyTokenInfo? SelectedToken
        {
            get => _selectedToken;
            set
            {
                _selectedToken = value;
                RaisePropertyChanged();
            }
        }

        public SurveyResponse? SelectedResponse
        {
            get => _selectedResponse;
            set
            {
                _selectedResponse = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedResponse));
                RaisePropertyChanged(nameof(SelectedResponseAnswers));
            }
        }

        public ObservableCollection<SurveyQuestion>? SelectedSurveyQuestions =>
            _selectedSurvey?.Questions != null
                ? new ObservableCollection<SurveyQuestion>(_selectedSurvey.Questions.OrderBy(q => q.SortOrder))
                : null;

        public ObservableCollection<SurveyAnswer>? SelectedResponseAnswers =>
            _selectedResponse?.Answers != null
                ? new ObservableCollection<SurveyAnswer>(_selectedResponse.Answers)
                : null;

        public bool HasSelectedSurvey => _selectedSurvey != null;
        public bool HasSelectedQuestion => _selectedQuestion != null;
        public bool HasSelectedResponse => _selectedResponse != null;

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsReadOnly));
            }
        }

        public bool IsReadOnly => !_isEditing;

        public bool IsNewSurvey
        {
            get => _isNewSurvey;
            set
            {
                _isNewSurvey = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                _isSyncing = value;
                RaisePropertyChanged();
            }
        }

        public bool IsGeneratingLinks
        {
            get => _isGeneratingLinks;
            set
            {
                _isGeneratingLinks = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowResultsView
        {
            get => _showResultsView;
            set
            {
                _showResultsView = value;
                _showLinksView = false;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowLinksView));
                RaisePropertyChanged(nameof(ShowQuestionsView));
            }
        }

        public bool ShowLinksView
        {
            get => _showLinksView;
            set
            {
                _showLinksView = value;
                _showResultsView = false;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowResultsView));
                RaisePropertyChanged(nameof(ShowQuestionsView));
            }
        }

        public bool ShowQuestionsView => !_showResultsView && !_showLinksView;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged();
            }
        }

        public int CloudResponseCount
        {
            get => _cloudResponseCount;
            set
            {
                _cloudResponseCount = value;
                RaisePropertyChanged();
            }
        }

        public int UnsyncedResponseCount
        {
            get => _unsyncedResponseCount;
            set
            {
                _unsyncedResponseCount = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasUnsyncedResponses));
            }
        }

        public bool HasUnsyncedResponses => _unsyncedResponseCount > 0;

        public bool CanSendSurvey => _selectedSurvey?.Status == SurveyStatus.Draft &&
                                     _selectedSurvey?.Questions?.Any() == true;

        public bool CanCloseSurvey => _selectedSurvey?.Status == SurveyStatus.Active;

        public bool CanGenerateLinks => _selectedSurvey?.Status == SurveyStatus.Active &&
                                        !string.IsNullOrEmpty(_supabaseSurveyId) &&
                                        SupabaseService.Instance.IsSignedIn;

        public bool CanSyncResponses => !string.IsNullOrEmpty(_supabaseSurveyId) &&
                                        SupabaseService.Instance.IsSignedIn;

        #endregion

        #region Properties - Edit Fields

        public string EditTitle
        {
            get => _editTitle;
            set
            {
                _editTitle = value;
                RaisePropertyChanged();
            }
        }

        public string EditDescription
        {
            get => _editDescription;
            set
            {
                _editDescription = value;
                RaisePropertyChanged();
            }
        }

        public bool EditIsAnonymous
        {
            get => _editIsAnonymous;
            set
            {
                _editIsAnonymous = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EditDueDate
        {
            get => _editDueDate;
            set
            {
                _editDueDate = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands - Survey CRUD

        private ICommand? _newSurveyCommand;
        public ICommand NewSurveyCommand => _newSurveyCommand ??= new TrackerCommand(
            _ => CreateNewSurvey(),
            _ => !IsEditing);

        private ICommand? _editSurveyCommand;
        public ICommand EditSurveyCommand => _editSurveyCommand ??= new TrackerCommand(
            _ => StartEditing(),
            _ => HasSelectedSurvey && !IsEditing && _selectedSurvey?.Status == SurveyStatus.Draft);

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new TrackerCommand(
            async _ => await SaveSurveyAsync(),
            _ => IsEditing && !string.IsNullOrWhiteSpace(EditTitle));

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(
            _ => CancelEditing(),
            _ => IsEditing);

        private ICommand? _deleteSurveyCommand;
        public ICommand DeleteSurveyCommand => _deleteSurveyCommand ??= new TrackerCommand(
            async _ => await DeleteSurveyAsync(),
            _ => HasSelectedSurvey && !IsEditing);

        private ICommand? _sendSurveyCommand;
        public ICommand SendSurveyCommand => _sendSurveyCommand ??= new TrackerCommand(
            async _ => await SendSurveyAsync(),
            _ => CanSendSurvey && !IsEditing);

        private ICommand? _closeSurveyCommand;
        public ICommand CloseSurveyCommand => _closeSurveyCommand ??= new TrackerCommand(
            async _ => await CloseSurveyAsync(),
            _ => CanCloseSurvey && !IsEditing);

        #endregion

        #region Commands - Questions

        private ICommand? _addQuestionCommand;
        public ICommand AddQuestionCommand => _addQuestionCommand ??= new TrackerCommand(
            _ => AddQuestion(),
            _ => HasSelectedSurvey && IsEditing);

        private ICommand? _removeQuestionCommand;
        public ICommand RemoveQuestionCommand => _removeQuestionCommand ??= new TrackerCommand(
            _ => RemoveQuestion(),
            _ => HasSelectedQuestion && IsEditing);

        #endregion

        #region Commands - External Links & Sync

        private ICommand? _generateLinksCommand;
        public ICommand GenerateLinksCommand => _generateLinksCommand ??= new TrackerCommand(
            async _ => await GenerateLinksAsync(),
            _ => CanGenerateLinks && !IsGeneratingLinks);

        private ICommand? _syncResponsesCommand;
        public ICommand SyncResponsesCommand => _syncResponsesCommand ??= new TrackerCommand(
            async _ => await SyncResponsesAsync(),
            _ => CanSyncResponses && !IsSyncing);

        private ICommand? _copyLinkCommand;
        public ICommand CopyLinkCommand => _copyLinkCommand ??= new TrackerCommand(
            param => CopyLinkToClipboard(param as SurveyTokenInfo),
            _ => true);

        private ICommand? _copyAllLinksCommand;
        public ICommand CopyAllLinksCommand => _copyAllLinksCommand ??= new TrackerCommand(
            _ => CopyAllLinksToClipboard(),
            _ => GeneratedTokens.Any());

        #endregion

        #region Commands - View Switching

        private ICommand? _viewResultsCommand;
        public ICommand ViewResultsCommand => _viewResultsCommand ??= new TrackerCommand(
            _ => { ShowResultsView = true; _ = LoadResponsesAsync(); },
            _ => HasSelectedSurvey);

        private ICommand? _viewLinksCommand;
        public ICommand ViewLinksCommand => _viewLinksCommand ??= new TrackerCommand(
            _ => { ShowLinksView = true; _ = LoadExistingTokensAsync(); },
            _ => HasSelectedSurvey && _selectedSurvey?.Status == SurveyStatus.Active);

        private ICommand? _viewQuestionsCommand;
        public ICommand ViewQuestionsCommand => _viewQuestionsCommand ??= new TrackerCommand(
            _ => { ShowResultsView = false; ShowLinksView = false; RaisePropertyChanged(nameof(ShowQuestionsView)); },
            _ => HasSelectedSurvey);

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                // Initialize the survey sync service
                if (SupabaseService.Instance.IsSignedIn)
                {
                    await SurveySyncService.Instance.InitializeAsync();
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error initializing PulseSurveysViewModel");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Use TrackerDataManager as single source of truth for all data
                var surveys = await TrackerDataManager.Instance.GetPulseSurveys();
                _surveys = new ObservableCollection<PulseSurvey>(surveys);
                RaisePropertyChanged(nameof(Surveys));

                var members = await TrackerDataManager.Instance.GetTeamData();
                _teamMembers = new ObservableCollection<TeamMember>(members);
                RaisePropertyChanged(nameof(TeamMembers));

                _logger.Info("Loaded {0} surveys", surveys.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading pulse surveys");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadSurveyForEditing(PulseSurvey survey)
        {
            EditTitle = survey.Title;
            EditDescription = survey.Description;
            EditIsAnonymous = survey.IsAnonymous;
            EditDueDate = survey.EndDate;
        }

        private async Task LoadSurveyCloudStatusAsync()
        {
            if (_selectedSurvey == null || !SupabaseService.Instance.IsSignedIn)
            {
                _supabaseSurveyId = null;
                CloudResponseCount = 0;
                UnsyncedResponseCount = 0;
                return;
            }

            try
            {
                // Check if survey exists in cloud (would need to store mapping)
                // For now, we'll use a simple approach - store the Supabase ID in the survey's Tag or similar
                // TODO: Add SupabaseSurveyId property to PulseSurvey model

                if (!string.IsNullOrEmpty(_supabaseSurveyId))
                {
                    var (success, total, unsynced) = await SurveySyncService.Instance.GetResponseCountAsync(_supabaseSurveyId);
                    if (success)
                    {
                        CloudResponseCount = total;
                        UnsyncedResponseCount = unsynced;
                    }
                }

                RaisePropertyChanged(nameof(CanGenerateLinks));
                RaisePropertyChanged(nameof(CanSyncResponses));
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading cloud status: {0}", ex.Message);
            }
        }

        private async Task LoadExistingTokensAsync()
        {
            if (string.IsNullOrEmpty(_supabaseSurveyId)) return;

            try
            {
                var (success, error, tokens) = await SurveySyncService.Instance.GetTokensForSurveyAsync(_supabaseSurveyId);
                if (success && tokens != null)
                {
                    GeneratedTokens = new ObservableCollection<SurveyTokenInfo>(
                        tokens.Select(t => new SurveyTokenInfo
                        {
                            Token = t.Token,
                            Url = $"{SurveySyncService.ExternalSurveyBaseUrl}?token={t.Token}",
                            ExpiresAt = t.ExpiresAt ?? DateTime.MaxValue,
                            TeamMember = _teamMembers.FirstOrDefault(m => m.Id == t.TeamMemberId) 
                                ?? new TeamMember { FirstName = t.TeamMemberName ?? "Unknown" }
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading tokens: {0}", ex.Message);
            }
        }

        private async Task LoadResponsesAsync()
        {
            if (_selectedSurvey == null) return;

            try
            {
                var survey = await _pulseSurveyRepository.GetPulseSurveyByIdAsync(_selectedSurvey.Id);
                if (survey?.Responses != null)
                {
                    SurveyResponses = new ObservableCollection<SurveyResponse>(
                        survey.Responses.OrderByDescending(r => r.SubmittedAt));
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading responses: {0}", ex.Message);
            }
        }

        #endregion

        #region Private Methods - Editing

        private void CreateNewSurvey()
        {
            _selectedSurvey = new PulseSurvey
            {
                Title = "New Survey",
                IsAnonymous = true,
                Status = SurveyStatus.Draft
            };

            LoadSurveyForEditing(_selectedSurvey);
            IsNewSurvey = true;
            IsEditing = true;
            _supabaseSurveyId = null;

            RaisePropertyChanged(nameof(SelectedSurvey));
            RaisePropertyChanged(nameof(HasSelectedSurvey));
            RaisePropertyChanged(nameof(SelectedSurveyQuestions));
        }

        private void StartEditing()
        {
            if (_selectedSurvey != null)
            {
                LoadSurveyForEditing(_selectedSurvey);
                IsEditing = true;
            }
        }

        private void CancelEditing()
        {
            if (IsNewSurvey)
            {
                _selectedSurvey = null;
                RaisePropertyChanged(nameof(SelectedSurvey));
                RaisePropertyChanged(nameof(HasSelectedSurvey));
            }
            else if (_selectedSurvey != null)
            {
                LoadSurveyForEditing(_selectedSurvey);
            }

            IsEditing = false;
            IsNewSurvey = false;
        }

        private async Task SaveSurveyAsync()
        {
            if (_selectedSurvey == null) return;

            try
            {
                _selectedSurvey.Title = EditTitle;
                _selectedSurvey.Description = EditDescription;
                _selectedSurvey.IsAnonymous = EditIsAnonymous;
                _selectedSurvey.EndDate = EditDueDate;

                if (IsNewSurvey)
                {
                    var result = await TrackerDataManager.Instance.AddPulseSurvey(_selectedSurvey);
                    if (result > 0)
                    {
                        // Note: survey.Id is already set inside AddPulseSurvey
                        _logger.Info("Created new survey: {0}", _selectedSurvey.Title);
                        // Reload to get fresh data from cache
                        await LoadDataAsync();
                    }
                }
                else
                {
                    var success = await TrackerDataManager.Instance.UpdatePulseSurvey(_selectedSurvey);
                    if (success)
                    {
                        _logger.Info("Updated survey: {0}", _selectedSurvey.Title);
                        await LoadDataAsync();
                    }
                }

                IsEditing = false;
                IsNewSurvey = false;
                RaisePropertyChanged(nameof(Surveys));
                RaisePropertyChanged(nameof(SelectedSurveyQuestions));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving survey");
            }
        }

        private async Task DeleteSurveyAsync()
        {
            if (_selectedSurvey == null) return;

            try
            {
                var success = await TrackerDataManager.Instance.DeletePulseSurvey(_selectedSurvey.Id);
                if (success)
                {
                    _selectedSurvey = null;
                    RaisePropertyChanged(nameof(SelectedSurvey));
                    RaisePropertyChanged(nameof(HasSelectedSurvey));
                    _logger.Info("Deleted survey");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting survey");
            }
        }

        #endregion

        #region Private Methods - Survey Actions

        private async Task SendSurveyAsync()
        {
            if (_selectedSurvey == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Uploading survey to cloud...";

                // First, upload to Supabase
                if (SupabaseService.Instance.IsSignedIn)
                {
                    var (success, error, supabaseId) = await SurveySyncService.Instance.UploadSurveyAsync(_selectedSurvey);
                    if (success)
                    {
                        _supabaseSurveyId = supabaseId;
                        _logger.Info("Survey uploaded to cloud: {0}", supabaseId);
                    }
                    else
                    {
                        _logger.Warn("Failed to upload survey to cloud: {0}", error);
                        StatusMessage = $"Warning: {error}";
                    }
                }

                // Update local status
                _selectedSurvey.Status = SurveyStatus.Active;
                _selectedSurvey.StartDate = DateTime.UtcNow;

                var updateSuccess = await TrackerDataManager.Instance.UpdatePulseSurvey(_selectedSurvey);
                if (updateSuccess)
                {
                    StatusMessage = "Survey is now active!";
                    _logger.Info("Survey sent: {0}", _selectedSurvey.Title);
                    RaisePropertyChanged(nameof(SelectedSurvey));
                    RaisePropertyChanged(nameof(CanSendSurvey));
                    RaisePropertyChanged(nameof(CanCloseSurvey));
                    RaisePropertyChanged(nameof(CanGenerateLinks));
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending survey");
                StatusMessage = "Error sending survey";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CloseSurveyAsync()
        {
            if (_selectedSurvey == null) return;

            try
            {
                // Update cloud status
                if (!string.IsNullOrEmpty(_supabaseSurveyId))
                {
                    await SurveySyncService.Instance.UpdateSurveyStatusAsync(_supabaseSurveyId, SurveyStatus.Closed);
                }

                _selectedSurvey.Status = SurveyStatus.Closed;
                _selectedSurvey.UpdatedAt = DateTime.UtcNow;

                var success = await TrackerDataManager.Instance.UpdatePulseSurvey(_selectedSurvey);
                if (success)
                {
                    _logger.Info("Survey closed: {0}", _selectedSurvey.Title);
                    RaisePropertyChanged(nameof(SelectedSurvey));
                    RaisePropertyChanged(nameof(CanSendSurvey));
                    RaisePropertyChanged(nameof(CanCloseSurvey));
                    RaisePropertyChanged(nameof(CanGenerateLinks));
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error closing survey");
            }
        }

        #endregion

        #region Private Methods - Questions

        private void AddQuestion()
        {
            if (_selectedSurvey == null) return;

            var newQuestion = new SurveyQuestion
            {
                QuestionText = "New Question",
                QuestionType = SurveyQuestionType.Rating,
                SortOrder = (_selectedSurvey.Questions?.Count ?? 0) + 1,
                IsRequired = true
            };

            _selectedSurvey.Questions ??= new List<SurveyQuestion>();
            _selectedSurvey.Questions.Add(newQuestion);

            RaisePropertyChanged(nameof(SelectedSurveyQuestions));
            SelectedQuestion = newQuestion;
        }

        private void RemoveQuestion()
        {
            if (_selectedSurvey?.Questions == null || _selectedQuestion == null) return;

            _selectedSurvey.Questions.Remove(_selectedQuestion);
            _selectedQuestion = null;

            // Re-order remaining questions
            var order = 1;
            foreach (var q in _selectedSurvey.Questions.OrderBy(q => q.SortOrder))
            {
                q.SortOrder = order++;
            }

            RaisePropertyChanged(nameof(SelectedSurveyQuestions));
            RaisePropertyChanged(nameof(HasSelectedQuestion));
        }

        #endregion

        #region Private Methods - External Links & Sync

        private async Task GenerateLinksAsync()
        {
            if (_selectedSurvey == null || string.IsNullOrEmpty(_supabaseSurveyId)) return;

            try
            {
                IsGeneratingLinks = true;
                StatusMessage = "Generating survey links...";

                // Generate for all team members
                var (success, error, tokens) = await SurveySyncService.Instance.GenerateTokensAsync(
                    _supabaseSurveyId,
                    _teamMembers);

                if (success && tokens != null)
                {
                    GeneratedTokens = new ObservableCollection<SurveyTokenInfo>(tokens);
                    StatusMessage = $"Generated {tokens.Count} survey links";
                    ShowLinksView = true;
                }
                else
                {
                    StatusMessage = $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error generating links");
                StatusMessage = "Error generating links";
            }
            finally
            {
                IsGeneratingLinks = false;
            }
        }

        private async Task SyncResponsesAsync()
        {
            if (_selectedSurvey == null || string.IsNullOrEmpty(_supabaseSurveyId)) return;

            try
            {
                IsSyncing = true;
                StatusMessage = "Syncing responses from cloud...";

                var (success, error, count) = await SurveySyncService.Instance.SyncResponsesAsync(
                    _supabaseSurveyId,
                    _selectedSurvey.Id);

                if (success)
                {
                    StatusMessage = count > 0 ? $"Synced {count} new responses" : "No new responses to sync";
                    await LoadResponsesAsync();
                    await LoadSurveyCloudStatusAsync();
                }
                else
                {
                    StatusMessage = $"Sync error: {error}";
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error syncing responses");
                StatusMessage = "Error syncing responses";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void CopyLinkToClipboard(SurveyTokenInfo? token)
        {
            if (token == null) return;

            try
            {
                Clipboard.SetText(token.Url);
                StatusMessage = $"Link copied for {token.TeamMember.FullName}";
            }
            catch (Exception ex)
            {
                _logger.Warn("Error copying to clipboard: {0}", ex.Message);
            }
        }

        private void CopyAllLinksToClipboard()
        {
            if (!GeneratedTokens.Any()) return;

            try
            {
                var text = string.Join(Environment.NewLine,
                    GeneratedTokens.Select(t => $"{t.TeamMember.FullName}: {t.Url}"));

                Clipboard.SetText(text);
                StatusMessage = $"Copied {GeneratedTokens.Count} links to clipboard";
            }
            catch (Exception ex)
            {
                _logger.Warn("Error copying to clipboard: {0}", ex.Message);
            }
        }

        #endregion
    }
}
