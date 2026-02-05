using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the QuickMessageDialog.
/// Handles message composition and sending via configured messaging provider (Slack or Teams).
/// </summary>
public partial class QuickMessageDialogViewModel : ObservableObject
{
    private const int MaxMessageLength = 500;

    #region Observable Properties

    [ObservableProperty]
    private string _recipientName = string.Empty;

    [ObservableProperty]
    private string _recipientEmail = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string _providerName = "None";

    #endregion

    #region Computed Properties

    public int MessageCharacterCount => Message?.Length ?? 0;

    public bool CanSend => !string.IsNullOrWhiteSpace(Message) 
                          && Message.Length <= MaxMessageLength 
                          && !IsSending
                          && !string.IsNullOrEmpty(RecipientEmail);

    public string ProviderDisplayText => ProviderName == "None" 
        ? "No messaging provider configured" 
        : $"Sending via {ProviderName}";

    #endregion

    #region Dialog State

    public bool WasSent { get; private set; }
    public MessageResult? Result { get; private set; }

    /// <summary>
    /// Event fired when user cancels or completes the dialog.
    /// </summary>
    public event EventHandler? CloseRequested;

    #endregion

    public QuickMessageDialogViewModel()
    {
        // Load provider name on initialization
        _ = LoadProviderAsync();
    }

    /// <summary>
    /// Sets the recipient for this message.
    /// </summary>
    public void SetRecipient(string email, string displayName)
    {
        RecipientEmail = email;
        RecipientName = displayName;
        OnPropertyChanged(nameof(CanSend));
    }

    /// <summary>
    /// Loads the configured messaging provider.
    /// </summary>
    private async Task LoadProviderAsync()
    {
        try
        {
            var available = await MessageService.Instance.IsAvailableAsync();
            ProviderName = available ? MessageService.Instance.CurrentProvider : "None";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QuickMessageDialogViewModel] Error loading provider: {ex.Message}");
            ProviderName = "None";
        }
    }

    partial void OnMessageChanged(string value)
    {
        // Clear errors when user types
        if (HasError)
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }

        OnPropertyChanged(nameof(MessageCharacterCount));
        OnPropertyChanged(nameof(CanSend));
    }

    #region Commands

    [RelayCommand]
    private async Task SendAsync()
    {
        if (!CanSend)
            return;

        try
        {
            IsSending = true;
            HasError = false;
            ErrorMessage = string.Empty;

            // Validate provider is available
            var available = await MessageService.Instance.IsAvailableAsync();
            if (!available)
            {
                HasError = true;
                ErrorMessage = "No messaging provider is configured. Please configure Slack or Teams in settings.";
                Result = MessageResult.Failed(ErrorMessage);
                return;
            }

            // Send message
            var success = await MessageService.Instance.SendMessageAsync(RecipientEmail, Message);

            if (success)
            {
                WasSent = true;
                Result = MessageResult.Sent(RecipientEmail, Message);
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                HasError = true;
                ErrorMessage = $"Failed to send message via {ProviderName}. Please check your configuration.";
                Result = MessageResult.Failed(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error sending message: {ex.Message}";
            Result = MessageResult.Failed(ErrorMessage);
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = MessageResult.Cancelled();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
