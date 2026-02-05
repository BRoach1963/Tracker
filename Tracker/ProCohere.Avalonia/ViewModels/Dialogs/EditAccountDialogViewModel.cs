using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Edit Account dialog.
/// Handles user profile editing including avatar upload.
/// </summary>
public partial class EditAccountDialogViewModel : ObservableObject
{
    #region Fields

    private UserProfile? _profile;
    private string? _pendingAvatarPath;
    private bool _removeAvatar;

    #endregion

    #region Observable Properties - Personal Info

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    #endregion

    #region Observable Properties - Work Info

    [ObservableProperty]
    private string _jobTitle = string.Empty;

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    #endregion

    #region Observable Properties - Dates

    [ObservableProperty]
    private DateTime? _birthday;

    [ObservableProperty]
    private DateTime? _hireDate;

    #endregion

    #region Observable Properties - Timezone

    [ObservableProperty]
    private TimezoneItem? _selectedTimezone;

    #endregion

    #region Observable Properties - Avatar

    [ObservableProperty]
    private string _initials = "?";

    [ObservableProperty]
    private Bitmap? _avatarImage;

    [ObservableProperty]
    private bool _showInitials = true;

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isUploadingAvatar;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _statusColor = "#64748B"; // Default gray

    #endregion

    #region Collections

    /// <summary>
    /// Available timezones.
    /// </summary>
    public ObservableCollection<TimezoneItem> Timezones { get; } = new();

    #endregion

    #region Result

    /// <summary>
    /// Whether changes were saved successfully.
    /// </summary>
    public bool Result { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Raised when profile is saved successfully.
    /// </summary>
    public event Action? ProfileSaved;

    /// <summary>
    /// Raised when the View should show a file picker for avatar selection.
    /// View handles the picker and calls SetPendingAvatarPath with result.
    /// </summary>
    public event EventHandler? AvatarPickerRequested;

    #endregion

    #region Constructor

    public EditAccountDialogViewModel()
    {
        LoadTimezones();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_profile == null) return;

        IsSaving = true;
        StatusText = string.Empty;

        try
        {
            var authService = AuthService.Instance;
            string? avatarError = null;

            // Upload avatar if changed
            if (!string.IsNullOrEmpty(_pendingAvatarPath))
            {
                IsUploadingAvatar = true;
                StatusText = "Uploading photo...";
                StatusColor = "#3B82F6"; // Blue

                var (success, avatarUrl, error) = await authService.UploadAvatarAsync(_pendingAvatarPath);
                
                IsUploadingAvatar = false;
                
                if (!success)
                {
                    avatarError = error;
                    Debug.WriteLine($"Avatar upload failed: {error}");
                    
                    // Show specific error to user
                    StatusText = $"Photo upload failed: {error}";
                    StatusColor = "#DC2626"; // Red
                }
                else
                {
                    Debug.WriteLine($"Avatar uploaded successfully: {avatarUrl}");
                }
            }

            // Get timezone
            var timezone = SelectedTimezone?.Id ?? TimeZoneInfo.Local.Id;

            // Update profile
            StatusText = "Saving profile...";
            var (updateSuccess, updateError) = await authService.UpdateUserProfileAsync(
                firstName: string.IsNullOrWhiteSpace(FirstName) ? null : FirstName.Trim(),
                lastName: string.IsNullOrWhiteSpace(LastName) ? null : LastName.Trim(),
                jobTitle: string.IsNullOrWhiteSpace(JobTitle) ? null : JobTitle.Trim(),
                company: string.IsNullOrWhiteSpace(Company) ? null : Company.Trim(),
                phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                birthday: Birthday,
                hireDate: HireDate,
                timezone: timezone
            );

            if (!updateSuccess)
            {
                StatusText = $"Failed to save: {updateError}";
                StatusColor = "#DC2626"; // Red
                IsSaving = false;
                return;
            }

            // If avatar failed but profile saved, show warning
            if (!string.IsNullOrEmpty(avatarError))
            {
                StatusText = "Profile saved. Photo upload failed (storage permission issue).";
                StatusColor = "#D97706"; // Orange
                await Task.Delay(1500);
            }

            Result = true;
            ProfileSaved?.Invoke();
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            StatusColor = "#DC2626"; // Red
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = false;
        CloseRequested?.Invoke(this, false);
    }

    [RelayCommand]
    private void UploadAvatar()
    {
        // Request View to show file picker
        AvatarPickerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveAvatar()
    {
        _pendingAvatarPath = null;
        _removeAvatar = true;
        AvatarImage = null;
        ShowInitials = true;
        StatusText = "Photo will be removed. Save to apply.";
        StatusColor = "#64748B";
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Load the user profile into the form.
    /// </summary>
    public void LoadProfile(UserProfile profile)
    {
        _profile = profile;

        // Personal info
        FirstName = profile.FirstName ?? string.Empty;
        LastName = profile.LastName ?? string.Empty;
        DisplayName = profile.DisplayName;
        Email = profile.Email;

        // Work info
        JobTitle = profile.JobTitle ?? string.Empty;
        Company = profile.Company ?? string.Empty;
        Phone = profile.Phone ?? string.Empty;

        // Dates
        Birthday = profile.Birthday;
        HireDate = profile.HireDate;

        // Timezone
        SelectTimezone(profile.Timezone);

        // Avatar
        Initials = profile.Initials;
        UpdateAvatarDisplay();
    }

    /// <summary>
    /// Called by View after file picker returns a path.
    /// </summary>
    public void SetPendingAvatarPath(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var fileInfo = new FileInfo(path);
                
                // Validate file size (max 5MB)
                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    StatusText = "File too large. Maximum size is 5MB.";
                    StatusColor = "#DC2626"; // Red
                    return;
                }

                // Validate file extension
                var extension = fileInfo.Extension.ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!Array.Exists(allowedExtensions, e => e == extension))
                {
                    StatusText = "Invalid file type. Allowed: JPG, PNG, GIF, WebP.";
                    StatusColor = "#DC2626"; // Red
                    return;
                }

                _pendingAvatarPath = path;
                _removeAvatar = false;
                UpdateAvatarDisplay();
                
                // Show file size for user confirmation
                var sizeInKb = fileInfo.Length / 1024;
                StatusText = $"New photo selected ({sizeInKb:N0} KB). Save to upload.";
                StatusColor = "#10B981"; // Green
            }
            catch (Exception ex)
            {
                StatusText = $"Error reading file: {ex.Message}";
                StatusColor = "#DC2626"; // Red
            }
        }
    }

    #endregion

    #region Private Methods

    private void LoadTimezones()
    {
        var timezones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new TimezoneItem { Id = tz.Id, DisplayName = tz.DisplayName })
            .ToList();

        foreach (var tz in timezones)
        {
            Timezones.Add(tz);
        }

        // Default to local timezone
        SelectTimezone(TimeZoneInfo.Local.Id);
    }

    private void SelectTimezone(string timezoneId)
    {
        var match = Timezones.FirstOrDefault(tz => tz.Id == timezoneId);
        if (match != null)
        {
            SelectedTimezone = match;
        }
    }

    private void UpdateAvatarDisplay()
    {
        if (_profile == null) return;

        // Try to load from pending path first
        if (!string.IsNullOrEmpty(_pendingAvatarPath) && File.Exists(_pendingAvatarPath))
        {
            try
            {
                using var stream = File.OpenRead(_pendingAvatarPath);
                AvatarImage = new Bitmap(stream);
                ShowInitials = false;
                return;
            }
            catch
            {
                AvatarImage = null;
                ShowInitials = true;
            }
        }
        else if (!_removeAvatar && !string.IsNullOrEmpty(_profile.AvatarUrl))
        {
            // Load from URL asynchronously
            _ = LoadAvatarFromUrlAsync(_profile.AvatarUrl);
        }
        else
        {
            AvatarImage = null;
            ShowInitials = true;
        }
    }

    private async Task LoadAvatarFromUrlAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            AvatarImage = new Bitmap(stream);
            ShowInitials = false;
        }
        catch
        {
            AvatarImage = null;
            ShowInitials = true;
        }
    }

    #endregion
}
