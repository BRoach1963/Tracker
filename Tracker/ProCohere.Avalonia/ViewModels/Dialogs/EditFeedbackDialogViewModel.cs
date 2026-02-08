using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditFeedbackDialog.
/// Handles editing existing feedback.
/// </summary>
public partial class EditFeedbackDialogViewModel : ObservableObject
{
    private IDialogService? _dialogService;
    private Guid _feedbackId;
    private string _recipientName = string.Empty;
    
    // Original values for change detection
    private string _originalTitle = string.Empty;
    private string _originalContent = string.Empty;
    private int _originalFeedbackTypeIndex;
    private int _originalVisibilityIndex;
    private int? _originalRating;
    
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
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _content = string.Empty;
    
    [ObservableProperty]
    private int _feedbackTypeIndex;
    
    [ObservableProperty]
    private int _visibilityIndex;
    
    [ObservableProperty]
    private int? _rating;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    #endregion
    
    // Tag values for combo boxes (must match XAML order)
    private static readonly string[] FeedbackTypeTags = { "general", "praise", "constructive", "coaching", "recognition" };
    private static readonly string[] VisibilityTags = { "private", "shared" };
    
    /// <summary>
    /// Load existing feedback for editing.
    /// </summary>
    public async Task LoadFeedbackAsync(Guid feedbackId, string recipientName)
    {
        _feedbackId = feedbackId;
        _recipientName = recipientName;
        RecipientDisplayName = recipientName;
        
        IsLoading = true;
        ErrorMessage = null;
        
        try
        {
            var feedback = await FeedbackService.Instance.GetByIdAsync(feedbackId);
            
            if (feedback == null)
            {
                ErrorMessage = FeedbackService.Instance.LastError ?? "Feedback not found";
                return;
            }
            
            // Populate fields
            Title = feedback.Title ?? string.Empty;
            Content = feedback.Content ?? string.Empty;
            FeedbackTypeIndex = GetIndexByTag(FeedbackTypeTags, feedback.FeedbackType);
            VisibilityIndex = GetIndexByTag(VisibilityTags, feedback.Visibility);
            Rating = feedback.Rating;
            
            // Store originals for change detection
            _originalTitle = Title;
            _originalContent = Content;
            _originalFeedbackTypeIndex = FeedbackTypeIndex;
            _originalVisibilityIndex = VisibilityIndex;
            _originalRating = Rating;
            
            Debug.WriteLine($"[EditFeedbackDialog] Loaded feedback: {feedbackId}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Debug.WriteLine($"[EditFeedbackDialog] Error loading feedback: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private static int GetIndexByTag(string[] tags, string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return 0;
        }
        
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].Equals(tag, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return 0;
    }
    
    private static string? GetTagByIndex(string[] tags, int index)
    {
        if (index >= 0 && index < tags.Length)
        {
            return tags[index];
        }
        return null;
    }
    
    /// <summary>
    /// Sets the dialog service for showing confirmations.
    /// </summary>
    public void SetDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    /// <summary>
    /// Returns true if the user has made changes to the feedback.
    /// </summary>
    public bool HasUnsavedChanges =>
        Title != _originalTitle ||
        Content != _originalContent ||
        FeedbackTypeIndex != _originalFeedbackTypeIndex ||
        VisibilityIndex != _originalVisibilityIndex ||
        Rating != _originalRating;
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[EditFeedbackDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}");
        
        // Show confirmation if there are unsaved changes
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            Debug.WriteLine($"[EditFeedbackDialog] Confirmation result: {confirmed}");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Debug.WriteLine("[EditFeedbackDialog] Closing dialog via CloseRequested");
        WasSaved = false;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validate
        var content = Content?.Trim();
        if (string.IsNullOrEmpty(content))
        {
            ErrorMessage = "Feedback content is required";
            return;
        }
        
        IsLoading = true;
        ErrorMessage = null;
        
        try
        {
            Debug.WriteLine($"[EditFeedbackDialog] Updating feedback {_feedbackId}");
            
            var updated = await FeedbackService.Instance.UpdateFeedbackAsync(
                _feedbackId,
                content,
                GetTagByIndex(FeedbackTypeTags, FeedbackTypeIndex) ?? "general",
                string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
                GetTagByIndex(VisibilityTags, VisibilityIndex) ?? "private",
                Rating);
            
            if (updated != null)
            {
                Debug.WriteLine($"[EditFeedbackDialog] Feedback updated: {updated.Id}");
                WasSaved = true;
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = FeedbackService.Instance.LastError ?? "Failed to update feedback";
                Debug.WriteLine($"[EditFeedbackDialog] Failed to update feedback: {ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Debug.WriteLine($"[EditFeedbackDialog] Error updating feedback: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
