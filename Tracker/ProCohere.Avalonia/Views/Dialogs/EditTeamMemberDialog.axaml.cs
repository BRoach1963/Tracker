using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for adding or editing a team member.
/// NOTE: This dialog is a stub - implementation pending.
/// </summary>
public partial class EditTeamMemberDialog : Window
{
    private TeamMemberDetail? _editingMember;
    private bool _isEditMode;
    private string? _avatarUrl;
    private byte[]? _newAvatarData;
    private readonly ObservableCollection<Note> _notes = new();
    private List<TeamMemberDetail> _managers = new();
    
    /// <summary>
    /// The result team member after saving.
    /// </summary>
    public TeamMemberDetail? Result { get; private set; }

    public EditTeamMemberDialog()
    {
        InitializeComponent();
        NotesItemsControl.ItemsSource = _notes;
    }

    /// <summary>
    /// Initialize the dialog for adding a new team member.
    /// </summary>
    public void InitForCreate()
    {
        _isEditMode = false;
        _editingMember = null;
        DialogTitle.Text = "Add Team Member";
        SaveButton.Content = "Add Member";
        DeleteButton.IsVisible = false;
        StatusSection.IsVisible = false;
        NotesSection.IsVisible = false;
        
        LoadManagersAsync();
    }

    /// <summary>
    /// Initialize the dialog for editing an existing team member.
    /// </summary>
    public void InitForEdit(TeamMemberDetail member)
    {
        _isEditMode = true;
        _editingMember = member;
        DialogTitle.Text = "Edit Team Member";
        SaveButton.Content = "Save Changes";
        DeleteButton.IsVisible = true;
        StatusSection.IsVisible = true;
        NotesSection.IsVisible = true;
        
        // Populate fields
        FirstNameTextBox.Text = member.FirstName ?? string.Empty;
        LastNameTextBox.Text = member.LastName ?? string.Empty;
        EmailTextBox.Text = member.Email ?? string.Empty;
        JobTitleTextBox.Text = member.JobTitle ?? string.Empty;
        PhoneTextBox.Text = member.UserPhone ?? string.Empty;
        LinkedInTextBox.Text = member.LinkedInUrl ?? string.Empty;
        IsActiveCheckBox.IsChecked = member.IsActive;
        
        if (member.Birthday.HasValue)
            BirthdayPicker.SelectedDate = member.Birthday.Value;
        
        if (member.HireDate.HasValue)
            HireDatePicker.SelectedDate = member.HireDate.Value;
        
        // Avatar
        _avatarUrl = member.UserAvatarUrl;
        UpdateAvatarDisplay();
        
        // Update initials
        UpdateInitials();
        
        LoadManagersAsync();
        // Notes loading deferred - needs NoteService implementation
    }

    private async void LoadManagersAsync()
    {
        try
        {
            var members = await TeamService.Instance.GetVisibleTeamMembersAsync();
            _managers = members
                .Where(m => _editingMember == null || m.Id != _editingMember.Id)
                .OrderBy(m => m.FullName)
                .ToList();
            
            ManagerComboBox.ItemsSource = _managers;
            
            // Manager selection deferred - needs ManagerId property
        }
        catch (Exception ex)
        {
            Log($"[EditTeamMemberDialog] Failed to load managers: {ex.Message}");
        }
    }

    private void UpdateAvatarDisplay()
    {
        if (_newAvatarData != null)
        {
            using var ms = new MemoryStream(_newAvatarData);
            AvatarImage.Source = new Bitmap(ms);
            AvatarBorder.IsVisible = true;
            InitialsBorder.IsVisible = false;
            RemoveAvatarButton.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(_avatarUrl))
        {
            AvatarBorder.IsVisible = false;
            InitialsBorder.IsVisible = true;
            RemoveAvatarButton.IsVisible = true;
        }
        else
        {
            AvatarBorder.IsVisible = false;
            InitialsBorder.IsVisible = true;
            RemoveAvatarButton.IsVisible = false;
        }
    }

    private void UpdateInitials()
    {
        var first = FirstNameTextBox.Text?.Trim() ?? string.Empty;
        var last = LastNameTextBox.Text?.Trim() ?? string.Empty;
        
        var initials = string.Empty;
        if (!string.IsNullOrEmpty(first))
            initials += first[0];
        if (!string.IsNullOrEmpty(last))
            initials += last[0];
        
        InitialsText.Text = string.IsNullOrEmpty(initials) ? "?" : initials.ToUpperInvariant();
    }

    private void NameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateInitials();
    }

    private async void UploadAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage == null) return;

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Avatar Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files.Count == 1)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _newAvatarData = ms.ToArray();
                UpdateAvatarDisplay();
            }
        }
        catch (Exception ex)
        {
            Log($"[EditTeamMemberDialog] Failed to upload avatar: {ex.Message}");
        }
    }

    private void RemoveAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        _newAvatarData = null;
        _avatarUrl = null;
        UpdateAvatarDisplay();
    }

    private void AddNoteButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement note creation
        Log("[EditTeamMemberDialog] AddNoteButton_Click - Not implemented");
    }

    private void DeleteNoteButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement note deletion
        Log("[EditTeamMemberDialog] DeleteNoteButton_Click - Not implemented");
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement member deletion
        Log("[EditTeamMemberDialog] DeleteButton_Click - Not implemented");
        Result = null;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate required fields
        var firstName = FirstNameTextBox.Text?.Trim();
        var lastName = LastNameTextBox.Text?.Trim();
        var email = EmailTextBox.Text?.Trim();

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
        {
            // TODO: Show validation error
            return;
        }

        // TODO: Implement actual save via TeamService
        // For now, just populate a result object
        var member = _editingMember ?? new TeamMemberDetail();
        member.FirstName = firstName;
        member.LastName = lastName;
        member.Email = email;
        member.JobTitle = JobTitleTextBox.Text?.Trim();
        member.UserPhone = PhoneTextBox.Text?.Trim();
        member.LinkedInUrl = LinkedInTextBox.Text?.Trim();
        member.IsActive = IsActiveCheckBox.IsChecked ?? true;
        
        if (BirthdayPicker.SelectedDate.HasValue)
            member.Birthday = BirthdayPicker.SelectedDate.Value;
        else
            member.Birthday = null;
        
        if (HireDatePicker.SelectedDate.HasValue)
            member.HireDate = HireDatePicker.SelectedDate.Value;
        else
            member.HireDate = null;

        Result = member;
        Close();
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "edit_team_member_dialog.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }
}
