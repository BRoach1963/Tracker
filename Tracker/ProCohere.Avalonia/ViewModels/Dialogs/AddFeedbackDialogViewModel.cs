using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the AddFeedbackDialog.
/// Handles creating new feedback for a team member.
/// </summary>
public partial class AddFeedbackDialogViewModel : ObservableObject
{
    private IDialogService? _dialogService;
    private Guid _recipientTeamMemberId;
    private string _recipientName = string.Empty;
    
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
    private bool _isAnonymous;
    
    [ObservableProperty]
    private int? _rating;
    
    /// <summary>
    /// Whether the team member selector should be shown.
    /// True when dialog is opened without a pre-selected recipient.
    /// </summary>
    [ObservableProperty]
    private bool _showRecipientSelector;
    
    /// <summary>
    /// Available team members to select from when no recipient is pre-set.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TeamMemberDetail> _availableTeamMembers = new();
    
    /// <summary>
    /// The selected team member from the picker.
    /// </summary>
    [ObservableProperty]
    private TeamMemberDetail? _selectedTeamMember;
    
    /// <summary>
    /// Whether the team members are still loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingTeamMembers;
    
    #endregion
    
    // Tag values for combo boxes (must match XAML order)
    private static readonly string[] FeedbackTypeTags = { "general", "praise", "constructive", "coaching", "recognition" };
    private static readonly string[] VisibilityTags = { "private", "shared" };
    
    /// <summary>
    /// Set the recipient of the feedback.
    /// </summary>
    public void SetRecipient(Guid teamMemberId, string name)
    {
        _recipientTeamMemberId = teamMemberId;
        _recipientName = name;
        RecipientDisplayName = name;
        ShowRecipientSelector = false;
    }
    
    /// <summary>
    /// Initialize dialog for team member selection mode (no pre-selected recipient).
    /// Loads available team members and shows the selector.
    /// </summary>
    public async Task InitializeForTeamMemberSelectionAsync()
    {
        ShowRecipientSelector = true;
        RecipientDisplayName = "Select a team member";
        IsLoadingTeamMembers = true;
        
        try
        {
            var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;
            var members = await TeamService.Instance.GetVisibleTeamMembersAsync();
            
            // Filter out self - can't give feedback to yourself
            var otherMembers = members
                .Where(m => m.Id != currentUserId && m.Relation != "self")
                .OrderBy(m => m.FullName)
                .ToList();
            
            AvailableTeamMembers = new ObservableCollection<TeamMemberDetail>(otherMembers);
            Debug.WriteLine($"[AddFeedbackDialog] Loaded {otherMembers.Count} team members for selection");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddFeedbackDialog] Error loading team members: {ex.Message}");
        }
        finally
        {
            IsLoadingTeamMembers = false;
        }
    }
    
    partial void OnSelectedTeamMemberChanged(TeamMemberDetail? value)
    {
        if (value != null)
        {
            _recipientTeamMemberId = value.Id;
            _recipientName = value.FullName;
            RecipientDisplayName = value.FullName;
        }
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
    /// Returns true if the user has entered any data that would be lost on cancel.
    /// </summary>
    public bool HasUnsavedChanges =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Content);
    
    [RelayCommand]
    private async Task CancelAsync()
    {
        Debug.WriteLine($"[AddFeedbackDialog] CancelAsync called - HasUnsavedChanges: {HasUnsavedChanges}");
        
        // Show confirmation if there's unsaved data
        if (HasUnsavedChanges && _dialogService != null)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to close without saving?",
                "Discard",
                "Keep Editing");
            
            Debug.WriteLine($"[AddFeedbackDialog] Confirmation result: {confirmed}");
            
            if (!confirmed)
            {
                return;
            }
        }
        
        Debug.WriteLine("[AddFeedbackDialog] Closing dialog via CloseRequested");
        WasSaved = false;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validate recipient
        if (_recipientTeamMemberId == Guid.Empty)
        {
            Debug.WriteLine("[AddFeedbackDialog] No recipient selected");
            return;
        }
        
        // Validate content
        var content = Content?.Trim();
        if (string.IsNullOrEmpty(content))
        {
            return;
        }
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            var session = AuthService.Instance.CurrentSession_ProCohere;
            
            if (client == null || session?.TeamMember == null)
            {
                Debug.WriteLine("[AddFeedbackDialog] Not authenticated");
                return;
            }
            
            var feedback = new Models.FeedbackDetail
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                FromMemberId = session.TeamMember.Id,
                TeamMemberId = _recipientTeamMemberId,
                FeedbackType = GetTagByIndex(FeedbackTypeTags, FeedbackTypeIndex) ?? "general",
                Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
                Content = content,
                Visibility = GetTagByIndex(VisibilityTags, VisibilityIndex) ?? "private",
                IsAnonymous = IsAnonymous,
                Rating = Rating,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            Debug.WriteLine($"[AddFeedbackDialog] Creating feedback for {_recipientName}");
            
            var result = await client.From<Models.FeedbackDetail>().Insert(feedback);
            var created = result.Models?.FirstOrDefault();
            
            if (created != null)
            {
                Debug.WriteLine($"[AddFeedbackDialog] Feedback created: {created.Id}");
                WasSaved = true;
                CloseRequested?.Invoke();
            }
            else
            {
                Debug.WriteLine("[AddFeedbackDialog] Failed to create feedback - no result");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AddFeedbackDialog] Error creating feedback: {ex.Message}");
        }
    }
}
