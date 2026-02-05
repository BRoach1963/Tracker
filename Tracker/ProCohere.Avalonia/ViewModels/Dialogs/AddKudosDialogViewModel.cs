using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the AddKudosDialog.
/// Handles creating new kudos recognition for a team member.
/// </summary>
public partial class AddKudosDialogViewModel : ObservableObject
{
    private IDialogService? _dialogService;
    private Guid _recipientTeamMemberId;
    private string _recipientName = string.Empty;
    
    /// <summary>
    /// The created kudos (if successful).
    /// </summary>
    public Kudos? CreatedKudos { get; private set; }
    
    /// <summary>
    /// The result indicating success (true if saved, false if cancelled).
    /// </summary>
    public bool WasSaved { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _recipientDisplayName = string.Empty;
    
    [ObservableProperty]
    private string _message = string.Empty;
    
    [ObservableProperty]
    private int _categoryIndex;
    
    [ObservableProperty]
    private bool _isPublic = true;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    private bool _hasError;
    
    #endregion
    
    // Category tag values (must match XAML order)
    private static readonly string[] CategoryTags = { "teamwork", "innovation", "leadership", "customer_focus", "quality", "above_and_beyond" };
    
    /// <summary>
    /// Message character count.
    /// </summary>
    public int MessageCharacterCount => Message?.Length ?? 0;
    
    /// <summary>
    /// Can submit if message is not empty.
    /// </summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(Message);
    
    /// <summary>
    /// Set the recipient of the kudos.
    /// </summary>
    public void SetRecipient(Guid teamMemberId, string name)
    {
        _recipientTeamMemberId = teamMemberId;
        _recipientName = name;
        RecipientDisplayName = name;
    }
    
    /// <summary>
    /// Sets the dialog service for showing confirmations.
    /// </summary>
    public void SetDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    /// <summary>
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    public bool HasUnsavedChanges => !string.IsNullOrWhiteSpace(Message);
    
    partial void OnMessageChanged(string value)
    {
        OnPropertyChanged(nameof(MessageCharacterCount));
        OnPropertyChanged(nameof(CanSubmit));
        
        // Clear error when user starts typing
        if (HasError)
        {
            HasError = false;
            ErrorMessage = null;
        }
    }
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[AddKudosDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}");
        
        // Show confirmation if there's unsaved data
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Kudos?",
                "Are you sure you want to discard this recognition? Your message will be lost.",
                "Discard",
                "Keep Editing");
            
            if (!confirmed)
            {
                Debug.WriteLine("[AddKudosDialog] User chose to keep editing");
                return;
            }
        }
        
        WasSaved = false;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private async Task SubmitAsync()
    {
        Debug.WriteLine("[AddKudosDialog] SubmitAsync called");
        
        // Validate
        if (string.IsNullOrWhiteSpace(Message))
        {
            ErrorMessage = "Please enter a recognition message.";
            HasError = true;
            return;
        }
        
        if (_recipientTeamMemberId == Guid.Empty)
        {
            ErrorMessage = "Invalid recipient.";
            HasError = true;
            return;
        }
        
        try
        {
            var currentUser = AuthService.Instance.CurrentTeamMember;
            if (currentUser == null)
            {
                ErrorMessage = "Could not determine current user.";
                HasError = true;
                return;
            }
            
            // Get selected category
            var category = CategoryIndex >= 0 && CategoryIndex < CategoryTags.Length 
                ? CategoryTags[CategoryIndex] 
                : "teamwork";
            
            // Create kudos
            var kudos = await KudosService.Instance.CreateKudosAsync(
                currentUser.Id,
                _recipientTeamMemberId,
                Message.Trim(),
                category,
                IsPublic);
            
            if (kudos == null)
            {
                ErrorMessage = KudosService.Instance.LastError ?? "Failed to create kudos.";
                HasError = true;
                return;
            }
            
            Debug.WriteLine($"[AddKudosDialog] Created kudos: {kudos.Id}");
            CreatedKudos = kudos;
            WasSaved = true;
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddKudosDialog] SubmitAsync error: {ex}");
            ErrorMessage = $"Error creating kudos: {ex.Message}";
            HasError = true;
        }
    }
}
