using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Window for editing user account/profile information.
/// Non-modal, draggable, can be moved and resized.
/// </summary>
public partial class EditAccountDialog : Window
{
    private UserProfile? _profile;
    private string? _pendingAvatarPath;
    private bool _removeAvatar;

    /// <summary>
    /// Result of the dialog - true if saved successfully, false if cancelled.
    /// </summary>
    public bool Result { get; private set; }

    /// <summary>
    /// Event raised when profile is saved successfully.
    /// </summary>
    public event Action? ProfileSaved;

    public EditAccountDialog()
    {
        InitializeComponent();
        LoadTimezones();
    }

    /// <summary>
    /// Handle header drag to move window.
    /// </summary>
    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    /// <summary>
    /// Loads the current user profile into the dialog.
    /// </summary>
    public void LoadProfile(UserProfile profile)
    {
        _profile = profile;

        // Personal info
        FirstNameTextBox.Text = profile.FirstName ?? string.Empty;
        LastNameTextBox.Text = profile.LastName ?? string.Empty;
        DisplayNameTextBox.Text = profile.DisplayName;
        EmailTextBox.Text = profile.Email;

        // Work info
        JobTitleTextBox.Text = profile.JobTitle ?? string.Empty;
        CompanyTextBox.Text = profile.Company ?? string.Empty;
        PhoneTextBox.Text = profile.Phone ?? string.Empty;

        // Dates (CalendarDatePicker uses DateTime? directly)
        BirthdayPicker.SelectedDate = profile.Birthday;
        HireDatePicker.SelectedDate = profile.HireDate;

        // Timezone
        SelectTimezone(profile.Timezone);

        // Avatar
        UpdateAvatarDisplay();
    }

    private void LoadTimezones()
    {
        var timezones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new TimezoneItem { Id = tz.Id, DisplayName = tz.DisplayName })
            .ToList();

        TimezoneComboBox.ItemsSource = timezones;
        TimezoneComboBox.DisplayMemberBinding = new global::Avalonia.Data.Binding(nameof(TimezoneItem.DisplayName));

        // Default to local timezone
        SelectTimezone(TimeZoneInfo.Local.Id);
    }

    private void SelectTimezone(string timezoneId)
    {
        if (TimezoneComboBox.ItemsSource is IEnumerable<TimezoneItem> items)
        {
            var match = items.FirstOrDefault(tz => tz.Id == timezoneId);
            if (match != null)
            {
                TimezoneComboBox.SelectedItem = match;
            }
        }
    }

    private void UpdateAvatarDisplay()
    {
        if (_profile == null) return;

        // Show initials
        AvatarInitials.Text = _profile.Initials;

        // Try to load avatar image
        if (!string.IsNullOrEmpty(_pendingAvatarPath) && File.Exists(_pendingAvatarPath))
        {
            try
            {
                using var stream = File.OpenRead(_pendingAvatarPath);
                AvatarImage.Source = new Bitmap(stream);
                AvatarInitials.IsVisible = false;
            }
            catch
            {
                AvatarImage.Source = null;
                AvatarInitials.IsVisible = true;
            }
        }
        else if (!_removeAvatar && !string.IsNullOrEmpty(_profile.AvatarUrl))
        {
            // Load from URL asynchronously
            _ = LoadAvatarFromUrlAsync(_profile.AvatarUrl);
        }
        else
        {
            AvatarImage.Source = null;
            AvatarInitials.IsVisible = true;
        }
    }

    private async Task LoadAvatarFromUrlAsync(string url)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            AvatarImage.Source = new Bitmap(stream);
            AvatarInitials.IsVisible = false;
        }
        catch
        {
            AvatarImage.Source = null;
            AvatarInitials.IsVisible = true;
        }
    }

    private async void UploadAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Profile Photo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            _pendingAvatarPath = file.Path.LocalPath;
            _removeAvatar = false;
            UpdateAvatarDisplay();
            StatusText.Text = "New photo selected. Save to apply.";
        }
    }

    private void RemoveAvatarButton_Click(object? sender, RoutedEventArgs e)
    {
        _pendingAvatarPath = null;
        _removeAvatar = true;
        AvatarImage.Source = null;
        AvatarInitials.IsVisible = true;
        StatusText.Text = "Photo will be removed. Save to apply.";
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_profile == null) return;

        SaveButton.IsEnabled = false;
        SaveButton.Content = "Saving...";
        StatusText.Text = string.Empty;

        try
        {
            var authService = AuthService.Instance;
            string? avatarError = null;

            // Upload avatar if changed (non-blocking - continue even if fails)
            if (!string.IsNullOrEmpty(_pendingAvatarPath))
            {
                StatusText.Text = "Uploading photo...";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#64748B"));
                
                var (success, avatarUrl, error) = await authService.UploadAvatarAsync(_pendingAvatarPath);
                if (!success)
                {
                    // Log the error but continue with profile save
                    avatarError = error;
                    System.Diagnostics.Debug.WriteLine($"Avatar upload failed: {error}");
                }
            }

            // Get timezone
            var timezone = (TimezoneComboBox.SelectedItem as TimezoneItem)?.Id ?? TimeZoneInfo.Local.Id;

            // Update profile
            StatusText.Text = "Saving profile...";
            var (updateSuccess, updateError) = await authService.UpdateUserProfileAsync(
                firstName: FirstNameTextBox.Text?.Trim(),
                lastName: LastNameTextBox.Text?.Trim(),
                jobTitle: JobTitleTextBox.Text?.Trim(),
                company: CompanyTextBox.Text?.Trim(),
                phone: PhoneTextBox.Text?.Trim(),
                birthday: BirthdayPicker.SelectedDate,
                hireDate: HireDatePicker.SelectedDate,
                timezone: timezone
            );

            if (!updateSuccess)
            {
                StatusText.Text = $"Failed to save: {updateError}";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Save Changes";
                return;
            }

            // If avatar failed but profile saved, show warning and close
            if (!string.IsNullOrEmpty(avatarError))
            {
                StatusText.Text = $"Profile saved. Photo upload failed (storage permission issue).";
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#D97706"));
                // Still close after a short delay
                await Task.Delay(1500);
            }

            Result = true;
            ProfileSaved?.Invoke();
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            StatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Save Changes";
        }
    }
}

/// <summary>
/// Helper class for timezone dropdown.
/// </summary>
public class TimezoneItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
