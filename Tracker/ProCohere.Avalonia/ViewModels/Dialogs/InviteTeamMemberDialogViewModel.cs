using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Invite Team Member dialog.
/// Handles inviting a new user to join the team/organization.
/// </summary>
public partial class InviteTeamMemberDialogViewModel : ObservableObject
{
    #region Fields

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendInviteCommand))]
    [NotifyPropertyChangedFor(nameof(EmailError))]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _jobTitle;

    [ObservableProperty]
    private TeamMemberDetail? _selectedManager;

    [ObservableProperty]
    private string? _personalMessage;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Validation error for email field.
    /// </summary>
    public string? EmailError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Email))
                return null; // Don't show error for empty field
            if (!EmailRegex.IsMatch(Email))
                return "Please enter a valid email address";
            return null;
        }
    }

    /// <summary>
    /// Whether the email is valid for sending.
    /// </summary>
    public bool IsEmailValid => !string.IsNullOrWhiteSpace(Email) && EmailRegex.IsMatch(Email);

    #endregion

    #region Collections

    /// <summary>
    /// Available managers to assign.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> AvailableManagers { get; } = new();

    #endregion

    #region Result

    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public InviteTeamMemberResult? Result { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanSendInvite))]
    private void SendInvite()
    {
        if (!IsEmailValid) return;

        Result = new InviteTeamMemberResult
        {
            Email = Email.Trim(),
            JobTitle = string.IsNullOrWhiteSpace(JobTitle) ? null : JobTitle.Trim(),
            ManagerTeamMemberId = SelectedManager?.Id,
            PersonalMessage = string.IsNullOrWhiteSpace(PersonalMessage) ? null : PersonalMessage.Trim()
        };

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSendInvite() => IsEmailValid && !IsSending;

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
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

    #endregion
}
