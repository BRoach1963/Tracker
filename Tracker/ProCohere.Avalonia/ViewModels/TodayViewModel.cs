using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Today/Dashboard view.
/// Displays team overview, stat cards, and upcoming tasks.
/// </summary>
public partial class TodayViewModel : ViewModelBase
{
    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Stats

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TeamMemberCountText))]
    private int _teamMemberCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TaskCompletionText))]
    [NotifyPropertyChangedFor(nameof(TaskCompletionColor))]
    private int _taskCompletionPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GoalsOnTrackText))]
    [NotifyPropertyChangedFor(nameof(GoalsOnTrackColor))]
    private int _goalsOnTrackPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveProjectCountText))]
    private int _activeProjectCount;

    #endregion

    #region Computed Display Text

    public string TeamMemberCountText => TeamMemberCount.ToString();
    public string TaskCompletionText => $"{TaskCompletionPercent}%";
    public string GoalsOnTrackText => $"{GoalsOnTrackPercent}%";
    public string ActiveProjectCountText => ActiveProjectCount.ToString();

    /// <summary>
    /// Today's date formatted nicely for the header.
    /// </summary>
    public string TodayDateText => DateTime.Now.ToString("dddd, MMMM d, yyyy");

    /// <summary>
    /// Color for task completion (green > 75%, amber 50-75%, red < 50%).
    /// </summary>
    public string TaskCompletionColor => TaskCompletionPercent switch
    {
        >= 75 => "#22C55E", // Green
        >= 50 => "#F59E0B", // Amber
        _ => "#EF4444"      // Red
    };

    /// <summary>
    /// Color for goals on track (green > 75%, amber 50-75%, red < 50%).
    /// </summary>
    public string GoalsOnTrackColor => GoalsOnTrackPercent switch
    {
        >= 75 => "#22C55E", // Green
        >= 50 => "#F59E0B", // Amber
        _ => "#EF4444"      // Red
    };

    #endregion

    #region Collections

    /// <summary>
    /// Team members managed by the current user.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();

    /// <summary>
    /// Upcoming tasks (due within 7 days or overdue).
    /// </summary>
    public ObservableCollection<TaskDetail> UpcomingTasks { get; } = new();

    /// <summary>
    /// Active goals owned by the user or their team.
    /// </summary>
    public ObservableCollection<GoalDetail> Goals { get; } = new();

    /// <summary>
    /// Upcoming meetings.
    /// </summary>
    public ObservableCollection<MeetingDetail> UpcomingMeetings { get; } = new();

    #endregion

    #region Quick Action Commands

    [RelayCommand]
    private void AddTask()
    {
        // TODO: Navigate to add task view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Task clicked");
    }

    [RelayCommand]
    private void AddGoal()
    {
        // TODO: Navigate to add goal view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Goal clicked");
    }

    [RelayCommand]
    private void AddMeeting()
    {
        // TODO: Navigate to schedule meeting view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Meeting clicked");
    }

    [RelayCommand]
    private void AddNote()
    {
        // TODO: Navigate to add note view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Note clicked");
    }

    #endregion

    #region User Info

    [ObservableProperty]
    private string _welcomeMessage = "Welcome";

    #endregion

    public TodayViewModel()
    {
        Log("[TodayViewModel] Constructor called");
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        // Load data when created (will fail if profile not loaded yet, but that's ok)
        _ = LoadDataAsync();
    }

    private void OnProfileChanged(object? sender, Models.UserProfile? profile)
    {
        Log($"[TodayViewModel] ProfileChanged event received: {(profile != null ? profile.Email : "NULL")}");
        if (profile != null)
        {
            // Profile just loaded, reload dashboard data
            _ = LoadDataAsync();
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProCohere", "dashboard.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        Console.WriteLine(message);
    }

    /// <summary>
    /// Loads all dashboard data from the database.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Log("[TodayViewModel] LoadDataAsync starting...");
        if (IsLoading)
        {
            Log("[TodayViewModel] Already loading, skipping");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            Log("[TodayViewModel] IsLoading = true");
            ErrorMessage = null;

            // Set welcome message
            var profile = AuthService.Instance.CurrentProfile;
            Log($"[TodayViewModel] Profile: {(profile != null ? profile.Email : "NULL")}");
            if (profile != null)
            {
                var firstName = profile.FirstName ?? profile.DisplayName?.Split(' ').FirstOrDefault() ?? "there";
                WelcomeMessage = $"Welcome back, {ToTitleCase(firstName)}";
            }

            // Load dashboard data
            Log("[TodayViewModel] Calling DashboardService.LoadDashboardDataAsync...");
            var data = await DashboardService.Instance.LoadDashboardDataAsync();
            Log($"[TodayViewModel] Data loaded: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks");

            // Update stats
            TeamMemberCount = data.Stats.TeamMemberCount;
            TaskCompletionPercent = data.Stats.TaskCompletionPercent;
            GoalsOnTrackPercent = data.Stats.GoalsOnTrackPercent;
            ActiveProjectCount = data.Stats.ActiveProjectCount;
            Log($"[TodayViewModel] Stats: {TeamMemberCount} members, {TaskCompletionPercent}% tasks, {GoalsOnTrackPercent}% goals, {ActiveProjectCount} projects");

            // Update collections
            TeamMembers.Clear();
            foreach (var member in data.TeamMembers)
            {
                TeamMembers.Add(member);
            }

            UpcomingTasks.Clear();
            foreach (var task in data.UpcomingTasks)
            {
                UpcomingTasks.Add(task);
            }

            // Update goals collection
            Goals.Clear();
            foreach (var goal in data.Goals.Where(g => g.Status != "completed").Take(5))
            {
                Goals.Add(goal);
            }

            // Load upcoming meetings (placeholder for now - will need MeetingDetail model)
            UpcomingMeetings.Clear();
            // TODO: Load actual meetings when meeting service is implemented

            Log($"[TodayViewModel] Collections updated: {TeamMembers.Count} members, {UpcomingTasks.Count} tasks, {Goals.Count} goals");

            // Check for errors
            if (!string.IsNullOrEmpty(DashboardService.Instance.LastError))
            {
                HasError = true;
                ErrorMessage = DashboardService.Instance.LastError;
                Log($"[TodayViewModel] Service error: {ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
            Log($"[TodayViewModel] EXCEPTION: {ex}");
        }
        finally
        {
            IsLoading = false;
            Log("[TodayViewModel] LoadDataAsync complete, IsLoading = false");
        }
    }

    /// <summary>
    /// Refreshes all dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Converts a string to Title Case.
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
}
