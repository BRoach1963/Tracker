using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Team Member Details dialog.
/// Displays team member info (read-only personal data) and allows editing org-specific fields.
/// </summary>
public partial class TeamMemberDetailsDialogViewModel : ObservableObject
{
    #region Fields

    private TeamMemberDetail? _teamMember;

    #endregion

    #region Observable Properties - Read Only (Personal Info from User)

    [ObservableProperty]
    private Guid _teamMemberId;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private DateTime? _birthday;

    [ObservableProperty]
    private string? _linkedInUrl;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private string _initials = "?";

    #endregion

    #region Observable Properties - Editable (Org-Specific)

    [ObservableProperty]
    private string? _jobTitle;

    [ObservableProperty]
    private TeamMemberDetail? _selectedManager;

    [ObservableProperty]
    private DateTime? _hireDate;

    [ObservableProperty]
    private bool _isActive = true;

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Formatted birthday for display.
    /// </summary>
    public string BirthdayDisplay => Birthday?.ToString("MMMM d") ?? "Not set";

    /// <summary>
    /// Whether we have a LinkedIn URL to display.
    /// </summary>
    public bool HasLinkedIn => !string.IsNullOrEmpty(LinkedInUrl);

    /// <summary>
    /// Whether we have a phone to display.
    /// </summary>
    public bool HasPhone => !string.IsNullOrEmpty(Phone);

    #endregion

    #region Collections

    /// <summary>
    /// Available managers to assign.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> AvailableManagers { get; } = new();

    /// <summary>
    /// Manager's private notes about this team member.
    /// </summary>
    public ObservableCollection<Note> Notes { get; } = new();

    #endregion

    #region Result

    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public TeamMemberDetailsResult? Result { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close, with the result.
    /// </summary>
    public event EventHandler<TeamMemberDetailsResult?>? CloseRequested;

    #endregion

    #region Commands

    [RelayCommand]
    private void Save()
    {
        Result = new TeamMemberDetailsResult
        {
            TeamMemberId = TeamMemberId,
            JobTitle = string.IsNullOrWhiteSpace(JobTitle) ? null : JobTitle.Trim(),
            ManagerTeamMemberId = SelectedManager?.Id,
            HireDate = HireDate,
            IsActive = IsActive,
            IsDeactivated = false
        };

        CloseRequested?.Invoke(this, Result);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void Deactivate()
    {
        Result = new TeamMemberDetailsResult
        {
            TeamMemberId = TeamMemberId,
            IsDeactivated = true
        };

        CloseRequested?.Invoke(this, Result);
    }

    [RelayCommand]
    private void AddNote()
    {
        // This is handled via event - View shows AddNoteDialog
        // and calls AddNoteFromDialogAsync with the result
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(Guid noteId)
    {
        try
        {
            await NotesService.Instance.DeleteNoteAsync(noteId);
            
            var noteToRemove = Notes.FirstOrDefault(n => n.Id == noteId);
            if (noteToRemove != null)
            {
                Notes.Remove(noteToRemove);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TeamMemberDetailsDialogViewModel] Failed to delete note: {ex.Message}");
            ErrorMessage = "Failed to delete note";
        }
    }

    [RelayCommand]
    private void OpenLinkedIn()
    {
        if (!string.IsNullOrEmpty(LinkedInUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LinkedInUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TeamMemberDetailsDialogViewModel] Failed to open LinkedIn: {ex.Message}");
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initialize with available managers.
    /// </summary>
    public void Initialize(IEnumerable<TeamMemberDetail> managers)
    {
        AvailableManagers.Clear();
        foreach (var manager in managers.OrderBy(m => m.FullName))
        {
            AvailableManagers.Add(manager);
        }
    }

    /// <summary>
    /// Load a team member's details.
    /// </summary>
    public async Task LoadTeamMemberAsync(TeamMemberDetail member)
    {
        _teamMember = member;
        IsLoading = true;

        try
        {
            // Set read-only personal info
            TeamMemberId = member.Id;
            FullName = member.FullName;
            Email = member.Email;
            Phone = member.UserPhone;
            Birthday = member.Birthday;
            LinkedInUrl = member.LinkedInUrl;
            AvatarUrl = member.UserAvatarUrl;
            Initials = member.Initials;

            // Set editable org-specific fields
            JobTitle = member.JobTitle;
            HireDate = member.HireDate;
            IsActive = member.IsActive;

            // Set selected manager
            if (member.ManagerTeamMemberId.HasValue)
            {
                SelectedManager = AvailableManagers.FirstOrDefault(m => m.Id == member.ManagerTeamMemberId.Value);
            }

            // Notify computed properties
            OnPropertyChanged(nameof(BirthdayDisplay));
            OnPropertyChanged(nameof(HasLinkedIn));
            OnPropertyChanged(nameof(HasPhone));

            // Load notes
            await LoadNotesAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Add a note from the dialog result.
    /// Called by View after showing AddNoteDialog.
    /// </summary>
    public async Task AddNoteFromDialogAsync(string? title, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        try
        {
            var note = new Note
            {
                Title = title,
                Content = content,
                LinkedTeamMemberId = TeamMemberId
            };

            await NotesService.Instance.CreateNoteAsync(note);
            await LoadNotesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TeamMemberDetailsDialogViewModel] Failed to create note: {ex.Message}");
            ErrorMessage = "Failed to create note";
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadNotesAsync()
    {
        try
        {
            var notes = await NotesService.Instance.GetNotesForEntityAsync(
                LinkedEntityType.TeamMember, TeamMemberId);

            Notes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.CreatedAt))
            {
                Notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TeamMemberDetailsDialogViewModel] Failed to load notes: {ex.Message}");
        }
    }

    #endregion
}
