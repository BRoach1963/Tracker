using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// Main window ViewModel - manages navigation and application state.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    #region Navigation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private NavigationItem _selectedNavigation = NavigationItem.Briefing;

    [ObservableProperty]
    private string _selectedSubNavigation = string.Empty;

    [ObservableProperty]
    private bool _isNavigationExpanded = true;

    #endregion

    #region Page Title

    /// <summary>
    /// Gets the page title based on current navigation.
    /// </summary>
    public string PageTitle => SelectedNavigation switch
    {
        NavigationItem.Briefing => "Briefing",
        NavigationItem.Me => "Me",
        NavigationItem.Circle => "Circle",
        NavigationItem.Pulse => "Pulse",
        NavigationItem.Chronicle => "Chronicle",
        NavigationItem.Settings => "Settings",
        _ => SelectedNavigation.ToString()
    };

    #endregion

    #region Search

    [ObservableProperty]
    private string _searchText = string.Empty;

    #endregion

    #region User Info

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private string _userDisplayName = string.Empty;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _userInitials = string.Empty;

    [ObservableProperty]
    private string? _userAvatarUrl;

    [ObservableProperty]
    private bool _isUserMenuOpen;

    /// <summary>
    /// Raised when the user signs out.
    /// </summary>
    public event Action? SignOutRequested;

    /// <summary>
    /// Raised when the user wants to edit their profile. 
    /// The view should show the EditAccountDialog.
    /// </summary>
    public event Action? EditProfileRequested;

    #endregion

    #region Theme

    [ObservableProperty]
    private bool _isDarkTheme = false;  // Start in light mode (Pro Cohere default)

    #endregion

    #region Manager Status

    /// <summary>
    /// True if current user has direct reports (is a manager).
    /// Used to control visibility of Circle navigation.
    /// </summary>
    [ObservableProperty]
    private bool _hasDirectReports = false;

    #endregion

    #region Status

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Database proof - shows profile data loaded from Supabase.
    /// </summary>
    [ObservableProperty]
    private string _databaseProof = "Loading from database...";

    #endregion

    public MainWindowViewModel()
    {
        LoadUserInfo();
        // Load profile from database async
        _ = LoadProfileFromDatabaseAsync();
    }

    /// <summary>
    /// Converts a string to Title Case (first letter of each word capitalized).
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        
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

    /// <summary>
    /// Loads profile from the Supabase database to prove connectivity.
    /// </summary>
    private async Task LoadProfileFromDatabaseAsync()
    {
        try
        {
            DatabaseProof = "🔄 Loading from Supabase...";
            
            // Get the auth user ID we're searching for
            var authUser = AuthService.Instance.CurrentUser;
            var authUserId = authUser?.Id ?? "null";
            
            DatabaseProof = $"🔄 Querying for auth ID:\n{authUserId}";
            
            var profile = await AuthService.Instance.LoadUserProfileAsync();
            
            if (profile != null)
            {
                // Update user info from database
                if (!string.IsNullOrEmpty(profile.DisplayName))
                {
                    UserDisplayName = ToTitleCase(profile.DisplayName);
                }
                if (!string.IsNullOrEmpty(profile.FirstName) || !string.IsNullOrEmpty(profile.LastName))
                {
                    UserDisplayName = ToTitleCase($"{profile.FirstName} {profile.LastName}".Trim());
                }
                UserInitials = profile.Initials;
                UserAvatarUrl = profile.AvatarUrl;
                
                // Determine if user is a manager (has direct reports) based on role
                // Admin and Manager roles can see Circle, others cannot
                var currentRole = AuthService.Instance.CurrentRole;
                var roleName = currentRole?.Name?.ToLowerInvariant() ?? "";
                HasDirectReports = roleName == "admin" || roleName == "manager";
                
                // Get data counts
                DatabaseProof = $"✅ Profile loaded! Getting data counts...";
                var counts = await AuthService.Instance.GetDataCountsAsync();
                var dataError = AuthService.Instance.LastDataCountError;
                
                // Build the proof string with data counts
                var proofBuilder = new System.Text.StringBuilder();
                proofBuilder.AppendLine("✅ DATABASE CONNECTED!");
                proofBuilder.AppendLine();
                proofBuilder.AppendLine($"👤 Profile: {profile.FirstName} {profile.LastName}");
                proofBuilder.AppendLine($"📧 Email: {profile.Email}");
                proofBuilder.AppendLine($"🆔 User ID: {profile.Id}");
                proofBuilder.AppendLine($"💼 {profile.JobTitle ?? "(no title)"} @ {profile.Company ?? "(no company)"}");
                proofBuilder.AppendLine();
                proofBuilder.AppendLine("📊 YOUR DATA:");
                proofBuilder.AppendLine("━━━━━━━━━━━━━━━━━━");
                proofBuilder.AppendLine($"👥 Team Members: {counts.TeamMembers}");
                proofBuilder.AppendLine($"📅 Meetings: {counts.Meetings}");
                proofBuilder.AppendLine($"🎯 Goals: {counts.Goals}");
                proofBuilder.AppendLine($"✅ Tasks: {counts.Tasks}");
                proofBuilder.AppendLine($"📁 Projects: {counts.Projects}");
                proofBuilder.AppendLine("━━━━━━━━━━━━━━━━━━");
                
                if (!string.IsNullOrEmpty(dataError))
                {
                    proofBuilder.AppendLine();
                    proofBuilder.AppendLine($"⚠️ Data errors: {dataError}");
                }
                
                DatabaseProof = proofBuilder.ToString();
            }
            else
            {
                var authId = authUser?.Id ?? "null";
                var error = AuthService.Instance.LastProfileError ?? "No error info";
                DatabaseProof = $"⚠️ No profile found in database.\n\n" +
                               $"Auth User ID: {authId}\n" +
                               $"Auth Email: {authUser?.Email ?? "null"}\n\n" +
                               $"Error: {error}\n\n" +
                               $"Check Supabase Table Editor:\n" +
                               $"SELECT * FROM users\n" +
                               $"WHERE supabase_auth_id = '{authId}'";
            }
        }
        catch (Exception ex)
        {
            DatabaseProof = $"❌ DATABASE ERROR:\n{ex.Message}\n\n{ex.StackTrace?.Split('\n').FirstOrDefault()}";
        }
    }

    private void LoadUserInfo()
    {
        var session = AuthService.Instance.CurrentSession;
        if (session?.User != null)
        {
            UserEmail = session.User.Email ?? string.Empty;
            
            // Try to get display name from user metadata
            var metadata = session.User.UserMetadata;
            string rawName;
            if (metadata != null && metadata.TryGetValue("full_name", out var fullName))
            {
                rawName = fullName?.ToString() ?? string.Empty;
            }
            else
            {
                // Fallback to email prefix
                rawName = UserEmail.Split('@')[0];
            }
            
            // Properly capitalize the name
            UserDisplayName = ToTitleCase(rawName);

            // Generate initials
            var parts = UserDisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                UserInitials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                UserInitials = parts[0][..2].ToUpper();
            }
            else
            {
                UserInitials = "PC";
            }
        }
    }

    #region Commands

    [RelayCommand]
    private void NavigateTo(NavigationItem item)
    {
        SelectedNavigation = item;
        SelectedSubNavigation = string.Empty;
    }

    [RelayCommand]
    private void NavigateToSub(string subItem)
    {
        SelectedSubNavigation = subItem;
    }

    [RelayCommand]
    private void ToggleNavigation()
    {
        IsNavigationExpanded = !IsNavigationExpanded;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // Theme switching will be handled by the view
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        try
        {
            // Clear stored credentials
            var credentialService = new WindowsCredentialService();
            credentialService.ClearSession();
            
            // Sign out from Supabase
            await AuthService.Instance.SignOutAsync();
            
            // Notify the view to navigate to login
            SignOutRequested?.Invoke();
        }
        catch (Exception)
        {
            // Still navigate to login even if sign out fails
            SignOutRequested?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenSearch()
    {
        // TODO: Implement command palette / search
        StatusMessage = "Search coming soon...";
    }

    [RelayCommand]
    private void OpenHelp()
    {
        // TODO: Open help / AI assistant
        StatusMessage = "Help coming soon...";
    }

    [RelayCommand]
    private void EditProfile()
    {
        // Request the view to show the EditAccountDialog
        EditProfileRequested?.Invoke();
    }

    /// <summary>
    /// Refreshes user display info after profile edit.
    /// </summary>
    public async Task RefreshUserInfoAsync()
    {
        await LoadProfileFromDatabaseAsync();
    }

    #endregion
}

/// <summary>
/// Main navigation items.
/// </summary>
public enum NavigationItem
{
    Briefing,   // Dashboard - what's relevant now (today/week view)
    Me,         // Personal hub - my tasks, goals, meetings, feedback (ALL users)
    Circle,     // Team view (MANAGERS ONLY) - team activity, attention needed
    Pulse,      // Goals, Metrics, Projects, Tasks
    Chronicle,  // Notes, Reports
    Settings    // App settings
}
