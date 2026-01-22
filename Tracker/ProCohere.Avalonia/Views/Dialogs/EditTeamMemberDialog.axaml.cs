using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit team member dialog.
/// </summary>
public class EditTeamMemberResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public Guid? ManagerTeamMemberId { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? HireDate { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? XProfileUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AvatarFilePath { get; set; } // Local path to upload
    public bool RemoveAvatar { get; set; }
}

/// <summary>
/// Dialog for creating or editing team members.
/// </summary>
public partial class EditTeamMemberDialog : Window
{
    private TeamMemberDetail? _existingMember;
    private List<TeamMemberDetail> _teamMembers = new();
    private string? _selectedAvatarPath;
    private bool _removeAvatar;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditTeamMemberResult? Result { get; private set; }
    
    public EditTeamMemberDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Load an existing team member for editing.
    /// </summary>
    public void LoadTeamMember(TeamMemberDetail member)
    {
        _existingMember = member;
        
        DialogTitle.Text = "Edit Team Member";
        SaveButton.Content = "Save Changes";
        DeleteButton.IsVisible = true;
        StatusSection.IsVisible = true;
        
        FirstNameTextBox.Text = member.FirstName;
        LastNameTextBox.Text = member.LastName;
        EmailTextBox.Text = member.Email;
        JobTitleTextBox.Text = member.JobTitle ?? "";
        PhoneTextBox.Text = member.Phone ?? "";
        
        // Set dates
        if (member.Birthday.HasValue)
            BirthdayPicker.SelectedDate = new DateTimeOffset(member.Birthday.Value);
        if (member.HireDate.HasValue)
            HireDatePicker.SelectedDate = new DateTimeOffset(member.HireDate.Value);
        
        // Social links
        LinkedInTextBox.Text = member.LinkedInUrl ?? "";
        XProfileTextBox.Text = member.XProfileUrl ?? "";
        
        // Notes
        NotesTextBox.Text = member.Notes ?? "";
        
        // Status
        IsActiveCheckBox.IsChecked = member.IsActive;
        
        // Manager is set in SetTeamMembers if called after LoadTeamMember
        if (member.ManagerTeamMemberId.HasValue && _teamMembers.Count > 0)
        {
            var manager = _teamMembers.FirstOrDefault(t => t.Id == member.ManagerTeamMemberId.Value);
            if (manager != null)
            {
                ManagerComboBox.SelectedItem = manager;
            }
        }
        
        // Update avatar
        UpdateAvatarDisplay();
        UpdateInitials();
    }
    
    /// <summary>
    /// Set the list of team members for the manager dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        // Filter out the current member (can't be their own manager)
        _teamMembers = teamMembers
            .Where(t => _existingMember == null || t.Id != _existingMember.Id)
            .ToList();
        ManagerComboBox.ItemsSource = _teamMembers;
        
        // If editing and we have a manager, select it
        if (_existingMember?.ManagerTeamMemberId.HasValue == true)
        {
            var manager = _teamMembers.FirstOrDefault(t => t.Id == _existingMember.ManagerTeamMemberId.Value);
            if (manager != null)
            {
                ManagerComboBox.SelectedItem = manager;
            }
        }
    }
    
    private void NameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateInitials();
    }
    
    private void UpdateInitials()
    {
        var first = FirstNameTextBox.Text?.Trim();
        var last = LastNameTextBox.Text?.Trim();
        
        var initials = "";
        if (!string.IsNullOrEmpty(first))
            initials += first[0].ToString().ToUpper();
        if (!string.IsNullOrEmpty(last))
            initials += last[0].ToString().ToUpper();
        
        InitialsText.Text = string.IsNullOrEmpty(initials) ? "?" : initials;
    }
    
    private void UpdateAvatarDisplay()
    {
        if (_removeAvatar)
        {
            AvatarBorder.IsVisible = false;
            InitialsBorder.IsVisible = true;
            RemoveAvatarButton.IsVisible = false;
            return;
        }
        
        if (!string.IsNullOrEmpty(_selectedAvatarPath) && File.Exists(_selectedAvatarPath))
        {
            try
            {
                using var stream = File.OpenRead(_selectedAvatarPath);
                AvatarImage.Source = new Bitmap(stream);
                AvatarBorder.IsVisible = true;
                InitialsBorder.IsVisible = false;
                RemoveAvatarButton.IsVisible = true;
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditTeamMemberDialog] Failed to load avatar: {ex.Message}");
            }
        }
        
        if (_existingMember != null && !string.IsNullOrEmpty(_existingMember.AvatarUrl))
        {
            // For now, just show initials - URL loading would need async
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
    
    private async void UploadAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Avatar Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp" }
                }
            }
        });
        
        if (files.Count > 0)
        {
            _selectedAvatarPath = files[0].Path.LocalPath;
            _removeAvatar = false;
            UpdateAvatarDisplay();
        }
    }
    
    private void RemoveAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        _selectedAvatarPath = null;
        _removeAvatar = true;
        UpdateAvatarDisplay();
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate
        var firstName = FirstNameTextBox.Text?.Trim();
        var lastName = LastNameTextBox.Text?.Trim();
        var email = EmailTextBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(firstName))
        {
            FirstNameTextBox.Focus();
            return;
        }
        
        if (string.IsNullOrEmpty(lastName))
        {
            LastNameTextBox.Focus();
            return;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            EmailTextBox.Focus();
            return;
        }
        
        // Get manager
        Guid? managerTeamMemberId = null;
        if (ManagerComboBox.SelectedItem is TeamMemberDetail manager)
        {
            managerTeamMemberId = manager.Id;
        }
        
        Result = new EditTeamMemberResult
        {
            Id = _existingMember?.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            JobTitle = string.IsNullOrWhiteSpace(JobTitleTextBox.Text) ? null : JobTitleTextBox.Text.Trim(),
            Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
            ManagerTeamMemberId = managerTeamMemberId,
            Birthday = BirthdayPicker.SelectedDate?.DateTime,
            HireDate = HireDatePicker.SelectedDate?.DateTime,
            LinkedInUrl = string.IsNullOrWhiteSpace(LinkedInTextBox.Text) ? null : LinkedInTextBox.Text.Trim(),
            XProfileUrl = string.IsNullOrWhiteSpace(XProfileTextBox.Text) ? null : XProfileTextBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            IsActive = IsActiveCheckBox.IsChecked ?? true,
            AvatarFilePath = _selectedAvatarPath,
            RemoveAvatar = _removeAvatar,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditTeamMemberDialog] Saving team member: {firstName} {lastName}");
        Close();
    }
    
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = new EditTeamMemberResult
        {
            Id = _existingMember?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditTeamMemberDialog] Deleting team member: {_existingMember?.Id}");
        Close();
    }
}
