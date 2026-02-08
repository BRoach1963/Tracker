using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Services.AI;
using ChatProviderMessage = ProCohere.Avalonia.Interfaces.ChatMessage;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for AI chat interface.
/// MVVM Compliant: Owns all chat state, exposes commands, zero View coupling.
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    #region State Properties

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private bool _isLoadingContext;

    [ObservableProperty]
    private string _contextSummary = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Services

    private readonly Interfaces.IChatProvider _chatProvider;
    private readonly AIContextService _contextService;
    private readonly AIUsageTracker _usageTracker;
    private string? _conversationContext;

    #endregion

    #region Constructor

    public ChatViewModel()
    {
        _chatProvider = ChatProviderFactory.Instance.GetProviderAsync().GetAwaiter().GetResult();
        _contextService = AIContextService.Instance;
        _usageTracker = AIUsageTracker.Instance;

        // Initialize with welcome message
        Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = "Hi! I'm your AI assistant. I can help you create tasks, schedule meetings, manage goals, and more. What would you like to do?",
            Timestamp = DateTime.Now
        });

        // Load context
        _ = LoadContextAsync();
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userMessage = InputText.Trim();
        InputText = string.Empty; // Clear input immediately
        HasError = false;
        StatusMessage = string.Empty;

        // Add user message to chat
        Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = userMessage,
            Timestamp = DateTime.Now
        });

        IsSending = true;

        try
        {
            // Get FULL context - the AI needs comprehensive data to give useful answers
            // Token usage is acceptable; a dumb AI is not
            var fullContext = await _contextService.GetCurrentContextAsync();

            // Build conversation history - convert to provider format
            var providerMessages = Messages
                .Where(m => !m.HasError && !m.IsSystem)
                .Select(m => new ChatProviderMessage { Role = m.Role, Content = m.Content })
                .ToList();

            // Get AI response with full context
            var response = await _chatProvider.GetResponseAsync(
                providerMessages,
                fullContext
            );

            // Add assistant response
            Messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.Now
            });

            // Update usage display
            var usageSummary = _usageTracker.GetUsageSummary();
            StatusMessage = usageSummary;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Error: {ex.Message}";

            Messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = "I'm sorry, I encountered an error processing your request. Please try again.",
                Timestamp = DateTime.Now,
                ErrorMessage = ex.Message
            });
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSendMessage() => !IsSending && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand]
    private async Task RefreshContextAsync()
    {
        await LoadContextAsync();
        StatusMessage = "Context refreshed";
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        
        // Re-add welcome message
        Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = "Chat cleared. How can I help you?",
            Timestamp = DateTime.Now
        });

        HasError = false;
        StatusMessage = "Chat cleared";
    }

    #endregion

    #region Private Methods

    private async Task LoadContextAsync()
    {
        IsLoadingContext = true;
        try
        {
            _conversationContext = await _contextService.GetCurrentContextAsync();
            ContextSummary = await _contextService.GetContextSummaryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] Error loading context: {ex.Message}");
            ContextSummary = "Context unavailable";
        }
        finally
        {
            IsLoadingContext = false;
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnInputTextChanged(string value)
    {
        // Trigger CanExecute re-evaluation
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    #endregion
}
