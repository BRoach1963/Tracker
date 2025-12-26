using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Services;
using Tracker.Services.AI;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Help Bot chat interface.
    /// Grounded in application documentation and user's actual data.
    /// </summary>
    public class HelpBotViewModel : BaseViewModel, IDisposable
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IChatProvider _chatProvider;
        private readonly ObservableCollection<ChatMessageViewModel> _messages;
        private string _inputText = string.Empty;
        private bool _isLoading;
        private bool _isAvailable;
        private bool _isInitializing;
        private string _statusMessage = string.Empty;
        private string _systemContext = string.Empty;
        private CancellationTokenSource? _currentRequestCts;
        private bool _disposed;

        #endregion

        #region Constructor

        public HelpBotViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("HelpBot");
            _chatProvider = new GeminiChatService();
            _messages = new ObservableCollection<ChatMessageViewModel>();
            _isAvailable = _chatProvider.IsAvailable;

            // Add welcome message
            AddWelcomeMessage();

            // Update status
            UpdateStatus();

            // Initialize context in background
            _ = InitializeContextAsync();
        }

        private async Task InitializeContextAsync()
        {
            try
            {
                _isInitializing = true;
                StatusMessage = "📚 Indexing documentation...";
                
                // Initialize RAG (vector store + document indexing)
                await HelpBotContextService.Instance.InitializeAsync();
                
                // Index user data in background
                StatusMessage = "💾 Indexing your data...";
                DataIndexer.Instance.ProgressChanged += OnDataIndexProgress;
                
                var stats = await DataIndexer.Instance.IndexAllDataAsync();
                
                DataIndexer.Instance.ProgressChanged -= OnDataIndexProgress;
                
                _logger.Info("Indexed {0} entities in {1:F1}s", stats.TotalIndexed, stats.Duration.TotalSeconds);
                
                StatusMessage = "Loading context...";
                
                // Build system context (instructions + user data)
                _systemContext = await HelpBotContextService.Instance.BuildSystemContextAsync();
                
                _logger.Info("Help Bot initialized. Context: {0} chars", _systemContext.Length);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error initializing Help Bot");
                // Fall back to basic context
                _systemContext = HelpBotContextService.Instance.GetQuickContext();
                StatusMessage = string.Empty;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void OnDataIndexProgress(object? sender, IndexProgressEventArgs e)
        {
            StatusMessage = e.Message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The chat messages displayed in the UI.
        /// </summary>
        public ObservableCollection<ChatMessageViewModel> Messages => _messages;

        /// <summary>
        /// The current text in the input field.
        /// </summary>
        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether a request is currently being processed.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>
        /// Whether the chat provider is available.
        /// </summary>
        public bool IsAvailable
        {
            get => _isAvailable;
            private set
            {
                _isAvailable = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>
        /// Status message to display (e.g., "Thinking...", error messages).
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether the send button should be enabled.
        /// </summary>
        public bool CanSend => IsAvailable && !IsLoading && !string.IsNullOrWhiteSpace(InputText);

        /// <summary>
        /// Usage summary for display.
        /// </summary>
        public string UsageSummary => AIUsageTracker.Instance.GetUsageSummary();

        /// <summary>
        /// Budget usage percentage (0-100+).
        /// </summary>
        public decimal BudgetUsedPercent => AIUsageTracker.Instance.BudgetUsedPercent;

        /// <summary>
        /// Whether the budget warning threshold has been reached.
        /// </summary>
        public bool IsBudgetWarning => AIUsageTracker.Instance.IsWarningThresholdReached;

        #endregion

        #region Commands

        private ICommand? _sendCommand;
        private ICommand? _clearCommand;
        private ICommand? _cancelCommand;
        private ICommand? _refreshContextCommand;

        public ICommand SendCommand => _sendCommand ??= new TrackerCommand(SendExecuted, _ => CanSend);
        public ICommand ClearCommand => _clearCommand ??= new TrackerCommand(ClearExecuted);
        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(CancelExecuted, _ => IsLoading);
        public ICommand RefreshContextCommand => _refreshContextCommand ??= new TrackerCommand(RefreshContextExecuted, _ => !IsLoading);

        #endregion

        #region Command Handlers

        private async void SendExecuted(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            var userMessage = InputText.Trim();
            InputText = string.Empty;

            // Add user message to chat
            _messages.Add(new ChatMessageViewModel("user", userMessage));

            // Send to AI
            await SendToAIAsync(userMessage);
        }

        private void ClearExecuted(object? parameter)
        {
            _messages.Clear();
            AddWelcomeMessage();
            StatusMessage = string.Empty;
            
            // Refresh context on clear to get latest data
            _ = RefreshContextInternalAsync();
        }

        private async void RefreshContextExecuted(object? parameter)
        {
            await RefreshContextInternalAsync();
            _messages.Add(new ChatMessageViewModel("assistant", 
                "✓ I've refreshed my knowledge of your current data. Ask me anything!"));
        }

        private async Task RefreshContextInternalAsync()
        {
            try
            {
                StatusMessage = "🔄 Refreshing data...";
                _systemContext = await HelpBotContextService.Instance.BuildSystemContextAsync();
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Warn("Error refreshing context: {0}", ex.Message);
                StatusMessage = string.Empty;
            }
        }

        private void CancelExecuted(object? parameter)
        {
            _currentRequestCts?.Cancel();
            IsLoading = false;
            StatusMessage = "Request cancelled.";
        }

        #endregion

        #region Private Methods

        private void AddWelcomeMessage()
        {
            _messages.Add(new ChatMessageViewModel("assistant",
                "👋 Hi! I'm Oracle, your AI assistant with semantic search and intelligent actions!\n\n" +
                "I have indexed:\n" +
                "📚 App documentation\n" +
                "👥 Your team members (with hire dates, birthdays, contact info)\n" +
                "📅 Your 1:1 meetings (with agendas and notes)\n" +
                "✅ Your tasks, OKRs, KPIs & projects\n\n" +
                "I can answer questions like:\n" +
                "• \"When did John start?\"\n" +
                "• \"Who has meetings next week?\"\n" +
                "• \"What tasks are assigned to Sarah?\"\n" +
                "• \"Show me OKR progress\"\n" +
                "• \"What's discussed in my upcoming 1:1s?\"\n\n" +
                "What would you like to know?"));
        }

        private void UpdateStatus()
        {
            if (!_chatProvider.IsAvailable)
            {
                StatusMessage = "⚠️ API key not configured. Go to Settings → AI to set up.";
                IsAvailable = false;
            }
            else
            {
                StatusMessage = string.Empty;
                IsAvailable = true;
            }
        }

        private async Task SendToAIAsync(string userMessage)
        {
            IsLoading = true;
            StatusMessage = "🔍 Searching docs...";

            _currentRequestCts = new CancellationTokenSource();

            try
            {
                // Wait for context initialization if still in progress
                if (_isInitializing)
                {
                    StatusMessage = "📚 Initializing...";
                    while (_isInitializing)
                    {
                        await Task.Delay(100, _currentRequestCts.Token);
                    }
                }

                // Get system context (instructions + user data) if not cached
                if (string.IsNullOrEmpty(_systemContext))
                {
                    _systemContext = await HelpBotContextService.Instance.BuildSystemContextAsync();
                }

                // RAG: Search for relevant documentation AND data
                StatusMessage = "🔍 Finding relevant info...";
                var relevantDocs = await HelpBotContextService.Instance.GetRelevantDocsAsync(userMessage);
                var relevantData = await SmartContextBuilder.Instance.GetDataContextForQueryAsync(userMessage);
                
                // Combine documentation and data context
                var contextParts = new List<string>();
                if (!string.IsNullOrEmpty(relevantDocs))
                    contextParts.Add(relevantDocs);
                if (!string.IsNullOrEmpty(relevantData))
                    contextParts.Add(relevantData);
                
                var combinedContext = contextParts.Count > 0 
                    ? string.Join("\n\n", contextParts) 
                    : string.Empty;
                
                // Build the enhanced question (question + relevant docs/data if found)
                var enhancedQuestion = string.IsNullOrEmpty(combinedContext) 
                    ? userMessage 
                    : $"{combinedContext}\n\nQuestion: {userMessage}";

                _logger.Debug("Enhanced question: {0} chars (docs: {1}, data: {2})", 
                    enhancedQuestion.Length, relevantDocs?.Length ?? 0, relevantData?.Length ?? 0);

                StatusMessage = "🤔 Thinking...";

                // Build conversation history - ONLY last 2 exchanges to keep it small
                var history = _messages
                    .Where(m => m.Role != "system")
                    .TakeLast(4)  // Last 2 Q&A pairs max
                    .Select(m => new ChatMessage(m.Role, m.Content))
                    .ToList();

                // Add current question (with docs) 
                history.Add(new ChatMessage("user", enhancedQuestion));

                _logger.Info("Sending: history={0} msgs, question={1} chars, context={2} chars",
                    history.Count, enhancedQuestion.Length, _systemContext?.Length ?? 0);

                var response = await _chatProvider.GetResponseAsync(history, _systemContext, _currentRequestCts.Token);

                // Add assistant response
                _messages.Add(new ChatMessageViewModel("assistant", response));
                StatusMessage = string.Empty;

                // Update usage display
                RaisePropertyChanged(nameof(UsageSummary));
                RaisePropertyChanged(nameof(BudgetUsedPercent));
                RaisePropertyChanged(nameof(IsBudgetWarning));

                _logger.Info("Help Bot responded to: {0}", userMessage.Length > 50 ? userMessage[..50] + "..." : userMessage);
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("Help Bot request was cancelled");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Help Bot error");
                _messages.Add(new ChatMessageViewModel("assistant",
                    "I'm sorry, I encountered an error. Please try again or check your internet connection."));
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _currentRequestCts?.Dispose();
                _currentRequestCts = null;
            }
        }

        #endregion

        #region IDisposable

        public new void Dispose()
        {
            if (!_disposed)
            {
                _currentRequestCts?.Cancel();
                _currentRequestCts?.Dispose();
                (_chatProvider as IDisposable)?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// View model for a single chat message.
    /// </summary>
    public class ChatMessageViewModel
    {
        public string Role { get; }
        public string Content { get; }
        public DateTime Timestamp { get; }
        public bool IsUser => Role == "user";
        public bool IsAssistant => Role == "assistant";
        public string TimeDisplay => Timestamp.ToString("h:mm tt");

        public ChatMessageViewModel(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }
}

