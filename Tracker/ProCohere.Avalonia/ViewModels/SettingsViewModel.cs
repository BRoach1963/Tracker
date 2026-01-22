using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Handles profile editing, theme switching, and logout functionality.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    #region Static Converters

    /// <summary>
    /// Converter that extracts the first initial from a display name.
    /// </summary>
    public static FuncValueConverter<string?, string> InitialConverter { get; } =
        new(name => string.IsNullOrEmpty(name) ? "?" : name[0].ToString().ToUpper());

    /// <summary>
    /// Converter for logout button text based on loading state.
    /// </summary>
    public static FuncValueConverter<bool, string> LogoutTextConverter { get; } =
        new(isLoggingOut => isLoggingOut ? "Signing out..." : "Sign Out");

    /// <summary>
    /// Converter for save button text based on saving state.
    /// </summary>
    public static FuncValueConverter<bool, string> SaveTextConverter { get; } =
        new(isSaving => isSaving ? "Saving..." : "Save Changes");

    #endregion

    #region Observable Properties - Profile Display

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private string _initials = "?";

    #endregion

    #region Observable Properties - Profile Editing

    [ObservableProperty]
    private bool _isEditingProfile;

    [ObservableProperty]
    private bool _isSavingProfile;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _jobTitle = string.Empty;

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BirthdayDisplay))]
    [NotifyPropertyChangedFor(nameof(BirthdayOffset))]
    private DateTime? _birthday;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HireDateDisplay))]
    [NotifyPropertyChangedFor(nameof(HireDateOffset))]
    private DateTime? _hireDate;

    /// <summary>
    /// Birthday as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? BirthdayOffset
    {
        get => Birthday.HasValue ? new DateTimeOffset(Birthday.Value) : null;
        set => Birthday = value?.DateTime;
    }

    /// <summary>
    /// HireDate as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? HireDateOffset
    {
        get => HireDate.HasValue ? new DateTimeOffset(HireDate.Value) : null;
        set => HireDate = value?.DateTime;
    }

    /// <summary>
    /// Display text for birthday (e.g., "Jan 15").
    /// </summary>
    public string BirthdayDisplay => Birthday?.ToString("MMM d") ?? "";

    /// <summary>
    /// Display text for hire date (e.g., "Jan 15, 2020").
    /// </summary>
    public string HireDateDisplay => HireDate?.ToString("MMM d, yyyy") ?? "";

    [ObservableProperty]
    private string? _profileErrorMessage;

    // Backup values for cancel
    private string _backupFirstName = string.Empty;
    private string _backupLastName = string.Empty;
    private string _backupJobTitle = string.Empty;
    private string _backupCompany = string.Empty;
    private string _backupPhone = string.Empty;
    private DateTime? _backupBirthday;
    private DateTime? _backupHireDate;

    #endregion

    #region Phone Number Formatting

    private bool _isFormattingPhone;

    /// <summary>
    /// Called when Phone property changes - formats the phone number.
    /// </summary>
    partial void OnPhoneChanged(string value)
    {
        // Prevent recursive formatting
        if (_isFormattingPhone) return;
        
        // Only auto-format if it looks like a raw number (no formatting chars)
        if (!string.IsNullOrEmpty(value) && !value.Contains('(') && !value.Contains('-') && !value.Contains(' '))
        {
            var digits = Regex.Replace(value, @"[^\d]", "");
            string? formatted = null;
            
            if (digits.Length == 10)
            {
                // Format as (XXX) XXX-XXXX
                formatted = $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
            }
            else if (digits.Length == 11 && digits.StartsWith('1'))
            {
                formatted = $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}";
            }

            if (formatted != null && formatted != value)
            {
                _isFormattingPhone = true;
                Phone = formatted;
                _isFormattingPhone = false;
            }
        }
    }

    #endregion

    #region Observable Properties - Settings

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _staySignedIn;

    [ObservableProperty]
    private bool _isLoggingOut;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    #endregion

    #region Events

    /// <summary>
    /// Raised when user logs out - App should navigate to login screen.
    /// </summary>
    public event Action? LogoutRequested;

    /// <summary>
    /// Raised when theme changes.
    /// </summary>
    public event Action<bool>? ThemeChanged;

    /// <summary>
    /// Raised when profile is updated.
    /// </summary>
    public event Action? ProfileUpdated;

    /// <summary>
    /// Raised when the user wants to open the full profile editor dialog.
    /// The view should show the EditAccountDialog.
    /// </summary>
    public event Action? OpenFullProfileEditorRequested;

    #endregion

    #region Constructor

    public SettingsViewModel()
    {
        LoadSettings();
        // Profile will be loaded asynchronously
        _ = LoadUserProfileAsync();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Loads the user profile from the database.
    /// </summary>
    public async Task LoadUserProfileAsync()
    {
        var profile = await AuthService.Instance.LoadUserProfileAsync();
        
        if (profile != null)
        {
            // Set editable fields
            FirstName = profile.FirstName ?? string.Empty;
            LastName = profile.LastName ?? string.Empty;
            JobTitle = profile.JobTitle ?? string.Empty;
            Company = profile.Company ?? string.Empty;
            Phone = profile.Phone ?? string.Empty;
            Birthday = profile.Birthday;
            HireDate = profile.HireDate;
            AvatarUrl = profile.AvatarUrl;
            
            // Set display fields
            DisplayName = profile.FullName;
            Email = profile.Email;
            Initials = profile.Initials;
        }
        else
        {
            // Fallback to auth user if no profile exists
            var user = AuthService.Instance.CurrentUser;
            if (user != null)
            {
                Email = user.Email ?? string.Empty;
                DisplayName = user.Email?.Split('@')[0] ?? "User";
                UpdateInitials();
            }
        }
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + 
                          (words[i].Length > 1 ? words[i][1..].ToLower() : string.Empty);
            }
        }
        return string.Join(" ", words);
    }

    private void UpdateInitials()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
        {
            Initials = $"{FirstName[0]}{LastName[0]}".ToUpper();
        }
        else if (!string.IsNullOrEmpty(DisplayName))
        {
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                Initials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 1)
            {
                Initials = parts[0][0].ToString().ToUpper();
            }
        }
        else
        {
            Initials = "?";
        }
    }

    private void LoadSettings()
    {
        // Load theme preference from ThemeService
        IsDarkTheme = ThemeService.Instance.IsDarkTheme;
        
        // Check if auto-login is enabled (has stored session)
        StaySignedIn = new WindowsCredentialService().HasStoredSession();
        
        // Get version
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version != null)
        {
            AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    #endregion

    #region Theme

    partial void OnIsDarkThemeChanged(bool value)
    {
        // Apply theme immediately via ThemeService
        ThemeService.Instance.IsDarkTheme = value;
        ThemeChanged?.Invoke(value);
    }

    #endregion

    #region Stay Signed In

    partial void OnStaySignedInChanged(bool value)
    {
        if (!value)
        {
            // User disabled auto-login - clear stored credentials
            var credentialService = new WindowsCredentialService();
            credentialService.ClearSession();
        }
        // Note: Credentials are stored on next login if checkbox is checked
    }

    #endregion

    #region Commands - Profile Editing

    [RelayCommand]
    private void EditProfile()
    {
        // Backup current values
        _backupFirstName = FirstName;
        _backupLastName = LastName;
        _backupJobTitle = JobTitle;
        _backupCompany = Company;
        _backupPhone = Phone;
        _backupBirthday = Birthday;
        _backupHireDate = HireDate;
        
        IsEditingProfile = true;
    }

    [RelayCommand]
    private void OpenFullProfileEditor()
    {
        // Request the view to show the EditAccountDialog for full editing (includes timezone, etc.)
        OpenFullProfileEditorRequested?.Invoke();
    }

    [RelayCommand]
    private void CancelEditProfile()
    {
        // Restore backup values
        FirstName = _backupFirstName;
        LastName = _backupLastName;
        JobTitle = _backupJobTitle;
        Company = _backupCompany;
        Phone = _backupPhone;
        Birthday = _backupBirthday;
        HireDate = _backupHireDate;
        
        IsEditingProfile = false;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsSavingProfile) return;

        IsSavingProfile = true;

        try
        {
            // Update profile in Supabase users table
            var (success, error) = await AuthService.Instance.UpdateUserProfileAsync(
                FirstName.Trim(),
                LastName.Trim(),
                JobTitle.Trim(),
                Company.Trim(),
                Phone.Trim(),
                Birthday,
                HireDate);

            if (success)
            {
                // Update display values from the profile
                DisplayName = $"{FirstName} {LastName}".Trim();
                if (string.IsNullOrEmpty(DisplayName))
                {
                    DisplayName = Email.Split('@')[0];
                }
                
                UpdateInitials();
                
                IsEditingProfile = false;
                ProfileErrorMessage = null;
                ProfileUpdated?.Invoke();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save profile: {error}");
                ProfileErrorMessage = error ?? "Failed to save profile. Please try again.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save profile: {ex.Message}");
            ProfileErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    /// <summary>
    /// Func to be set by the View to open a file picker and return the selected file path.
    /// Returns null if user cancels.
    /// </summary>
    public Func<Task<string?>>? OpenFilePickerFunc { get; set; }

    [RelayCommand]
    private async Task ChangeAvatarAsync()
    {
        try
        {
            // Use the file picker function set by the View
            if (OpenFilePickerFunc == null)
            {
                System.Diagnostics.Debug.WriteLine("OpenFilePickerFunc not set");
                return;
            }

            var filePath = await OpenFilePickerFunc();
            if (string.IsNullOrEmpty(filePath))
            {
                return; // User cancelled
            }

            // Upload the avatar
            var (success, newUrl, error) = await AuthService.Instance.UploadAvatarAsync(filePath);
            
            if (success && !string.IsNullOrEmpty(newUrl))
            {
                AvatarUrl = newUrl;
                ProfileUpdated?.Invoke();
            }
            else
            {
                ProfileErrorMessage = error ?? "Failed to upload avatar.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to change avatar: {ex.Message}");
            ProfileErrorMessage = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Commands - Account

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsLoggingOut) return;

        IsLoggingOut = true;

        try
        {
            // Clear stored credentials
            var credentialService = new WindowsCredentialService();
            credentialService.ClearSession();

            // Sign out from Supabase
            await AuthService.Instance.SignOutAsync();

            // Notify App to navigate to login
            LogoutRequested?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            // Still navigate to login even if signout fails
            LogoutRequested?.Invoke();
        }
        finally
        {
            IsLoggingOut = false;
        }
    }

    [RelayCommand]
    private void OpenPrivacyPolicy()
    {
        OpenUrl("https://procohere.com/privacy");
    }

    [RelayCommand]
    private void OpenTermsOfService()
    {
        OpenUrl("https://procohere.com/terms");
    }

    [RelayCommand]
    private void ChangePassword()
    {
        // Open password reset in browser
        OpenUrl("https://procohere.com/reset-password");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail
        }
    }

    #endregion
}
