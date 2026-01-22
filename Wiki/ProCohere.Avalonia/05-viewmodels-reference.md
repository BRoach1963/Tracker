# 05 – ViewModels Reference

This document describes all **ViewModels** in ProCohere.Avalonia.

---

## Overview

ViewModels follow MVVM pattern using **CommunityToolkit.Mvvm**:
- `[ObservableProperty]` generates property change notifications
- `[RelayCommand]` generates ICommand implementations
- `[NotifyPropertyChangedFor]` chains dependent properties
- `[NotifyCanExecuteChangedFor]` re-evaluates command CanExecute

All ViewModels inherit from `ViewModelBase` → `ObservableObject`.

---

## Base Class

### ViewModelBase

```csharp
public class ViewModelBase : ObservableObject
{
    // Empty base - just establishes inheritance
}
```

Located in `ViewModels/ViewModelBase.cs`.

---

## ViewModel Index

| ViewModel | File | Lines | Purpose |
|-----------|------|-------|---------|
| `MainWindowViewModel` | MainWindowViewModel.cs | ~370 | Navigation, user info, app-level commands |
| `LoginViewModel` | LoginViewModel.cs | ~190 | Login form, validation, auth flow |
| `BriefingViewModel` | BriefingViewModel.cs | ~685 | Daily summary, Manager/IC views |
| `MeViewModel` | MeViewModel.cs | ~1207 | Personal hub (tasks, goals, meetings) |
| `CircleViewModel` | CircleViewModel.cs | ~1847 | Team management (managers only) |
| `PulseViewModel` | PulseViewModel.cs | ~209 | Goals/Metrics/Tasks coordinator |
| `GoalsViewModel` | GoalsViewModel.cs | ~558 | Goal CRUD, narrative-first philosophy |
| `MetricsViewModel` | MetricsViewModel.cs | ~682 | Metric CRUD, signals-not-targets |
| `TasksViewModel` | TasksViewModel.cs | ~471 | Task CRUD, filtering |
| `ChronicleViewModel` | ChronicleViewModel.cs | ~637 | Notes management |
| `SettingsViewModel` | SettingsViewModel.cs | ~551 | Profile editing, theme, logout |

---

## MainWindowViewModel

**Purpose**: Top-level navigation and user state.

### Key Properties

```csharp
// Navigation
[ObservableProperty] NavigationItem SelectedNavigation;
[ObservableProperty] string SelectedSubNavigation;
[ObservableProperty] bool IsNavigationExpanded;

// User info
[ObservableProperty] string DisplayName;
[ObservableProperty] string Email;
[ObservableProperty] string? AvatarUrl;
[ObservableProperty] bool HasDirectReports;  // Show Circle nav

// State
[ObservableProperty] bool IsLoadingProfile;
[ObservableProperty] bool IsDarkTheme;
```

### Key Commands

| Command | Action |
|---------|--------|
| `NavigateToCommand` | Switch main navigation |
| `ToggleNavigationCommand` | Expand/collapse sidebar |
| `ToggleThemeCommand` | Switch light/dark |
| `EditProfileCommand` | Open profile editor |
| `SignOutCommand` | Log out user |

### Events

```csharp
public event Action? SignOutRequested;
public event Action? EditProfileRequested;
```

---

## LoginViewModel

**Purpose**: Authentication form.

### Key Properties

```csharp
[ObservableProperty] string Email;
[ObservableProperty] string Password;
[ObservableProperty] bool KeepMeSignedIn;
[ObservableProperty] bool IsLoading;
[ObservableProperty] string ErrorMessage;
[ObservableProperty] bool HasError;
[ObservableProperty] bool IsPasswordVisible;
```

### Key Commands

| Command | Can Execute | Action |
|---------|-------------|--------|
| `SignInCommand` | Valid email, password not empty, not loading | Call AuthService.SignInAsync |
| `TogglePasswordVisibilityCommand` | Always | Show/hide password |

### Events

```csharp
public event Action? LoginSuccessful;
```

---

## BriefingViewModel

**Purpose**: Daily/weekly summary view.

### Philosophy
- Per spec: No percentages, no rankings, no performance scoring
- Manager view: Team activity sparkline, team-level stats
- IC view: Personal inventory distribution bar

### Key Properties

```csharp
// Role detection
[ObservableProperty] bool IsManager;
public bool IsIndividualContributor => !IsManager;

// Scope toggle
[ObservableProperty] BriefingScope CurrentScope;  // Today/Week
public bool IsTodayScope => CurrentScope == BriefingScope.Today;
public bool IsWeekScope => CurrentScope == BriefingScope.Week;
public string DateRangeText { get; }  // Computed date string

// Collections
public ObservableCollection<MeetingDetail> UpcomingMeetings { get; }
public ObservableCollection<TaskDetail> PriorityTasks { get; }
public ObservableCollection<AttentionItem> AttentionItems { get; }
```

### Key Commands

| Command | Action |
|---------|--------|
| `SetScopeCommand` | Switch Today/Week |
| `LoadDataCommand` | Refresh briefing data |

---

## MeViewModel

**Purpose**: Personal hub - user's own items only.

### Tabs
```csharp
public enum MeTab
{
    Tasks,    // My tasks (I am owner)
    Goals,    // My goals (I am owner)
    Feedback, // Received/Given feedback
    Meetings  // Meetings I'm participating in
}
```

### Flyouts
```csharp
public enum MeFlyoutType
{
    None,
    Task,
    Meeting,
    Goal,
    Feedback
}
```

### Key Properties

```csharp
// Tab state
[ObservableProperty] MeTab SelectedTab;

// My data
[ObservableProperty] ObservableCollection<TaskDetail> MyTasks;
[ObservableProperty] ObservableCollection<GoalDetail> MyGoals;
[ObservableProperty] ObservableCollection<MeetingDetail> MyMeetings;
[ObservableProperty] ObservableCollection<FeedbackDetail> ReceivedFeedback;
[ObservableProperty] ObservableCollection<FeedbackDetail> GivenFeedback;

// Flyout state
[ObservableProperty] MeFlyoutType CurrentFlyoutType;
[ObservableProperty] bool IsFlyoutOpen;
[ObservableProperty] TaskDetail? SelectedTask;
[ObservableProperty] MeetingDetail? SelectedMeeting;
// etc.
```

---

## CircleViewModel

**Purpose**: Team management (managers only).

### Tabs
```csharp
public enum CircleTab
{
    Team,     // Team members list
    Goals,    // Team goals
    Feedback, // Team feedback
    Meetings  // Team meetings
}
```

### Key Properties

```csharp
// Tab state
[ObservableProperty] CircleTab SelectedTab;

// Team stats
[ObservableProperty] int TotalMemberCount;
[ObservableProperty] int ActiveMemberCount;
[ObservableProperty] int MeetingsOnTrackCount;
[ObservableProperty] int MeetingsOverdueCount;

// Hierarchy
[ObservableProperty] TeamMemberDetail? CurrentTeamMember;
[ObservableProperty] ObservableCollection<TeamMemberDetail> DirectReports;

// Team members
public ObservableCollection<TeamMemberDetail> TeamMembers { get; }
```

### Key Commands

| Command | Action |
|---------|--------|
| `SelectTabCommand` | Switch tabs |
| `LoadTeamMembersCommand` | Refresh team |
| `AddTeamMemberCommand` | Open add dialog |
| `ViewMemberDetailCommand` | Open member flyout |

---

## PulseViewModel

**Purpose**: Coordinator for Goals/Metrics/Tasks tabs.

### Sub-Tabs
```csharp
public int SelectedSubTab { get; set; }  // 0=Goals, 1=Metrics, 2=Tasks

public bool IsSubTabGoals => SelectedSubTab == 0;
public bool IsSubTabMetrics => SelectedSubTab == 1;
public bool IsSubTabTasks => SelectedSubTab == 2;
```

### Child ViewModels
```csharp
public GoalsViewModel GoalsViewModel { get; }
public MetricsViewModel MetricsViewModel { get; }
public TasksViewModel TasksViewModel { get; }
```

### Primary Action
```csharp
public string PrimaryActionText => SelectedSubTab switch
{
    0 => "+ New Goal",
    1 => "+ New Metric",
    2 => "+ New Task",
    _ => "+ New"
};

[RelayCommand]
private void PrimaryAction()
{
    // Delegates to child ViewModel's create command
}
```

---

## GoalsViewModel

**Purpose**: Goal management within Pulse.

### Philosophy
> "Goals express intent, Metrics observe reality, Humans decide."
> NO progress bars, percentages, or red/yellow/green status indicators.

### Scope Filter
```csharp
public int SelectedScope { get; set; }  // 0=My, 1=Team, 2=Shared

public bool IsScopeMyGoals => SelectedScope == 0;
public bool IsScopeTeamGoals => SelectedScope == 1;
public bool IsScopeSharedGoals => SelectedScope == 2;
```

### Key Properties

```csharp
// Collection
public ObservableCollection<GoalDetail> Goals { get; }

// Selection
[ObservableProperty] GoalDetail? SelectedGoal;
[ObservableProperty] bool IsDetailFlyoutOpen;
[ObservableProperty] bool IsEditorFlyoutOpen;

// Health/Lifecycle dialogs
[ObservableProperty] bool IsHealthDialogOpen;
[ObservableProperty] bool IsLifecycleDialogOpen;
[ObservableProperty] GoalHealth SelectedHealth;
[ObservableProperty] GoalLifecycle SelectedLifecycle;
```

### Key Commands

| Command | Action |
|---------|--------|
| `LoadGoalsCommand` | Refresh from GoalsService |
| `CreateNewGoalCommand` | Open create dialog |
| `SetScopeCommand` | Filter by scope |
| `SelectGoalCommand` | Open detail flyout |
| `UpdateHealthCommand` | Change goal health |
| `UpdateLifecycleCommand` | Change goal lifecycle |

---

## MetricsViewModel

**Purpose**: Metric management within Pulse.

### Philosophy
> "Metrics are signals that tell a story, NOT targets to chase."
> Display DIRECTIONAL TRENDS (↗ → ↘), not numeric values by default.
> NO progress bars, percentages, or red/yellow/green status.

### Filters
```csharp
// Scope: 0=Individual, 1=Team, 2=Organization, 3=All
[ObservableProperty] int SelectedScope;

// Lifecycle: null=All, otherwise specific
[ObservableProperty] MetricLifecycle? LifecycleFilter;

// Source filter
[ObservableProperty] MetricSource? SourceFilter;
```

### Key Properties

```csharp
public ObservableCollection<MetricDetail> Metrics { get; }

[ObservableProperty] MetricDetail? SelectedMetric;
[ObservableProperty] bool IsDetailFlyoutOpen;
[ObservableProperty] bool IsEditorFlyoutOpen;
```

### Key Commands

| Command | Action |
|---------|--------|
| `LoadMetricsCommand` | Refresh from MetricsService |
| `CreateNewMetricCommand` | Open create dialog |
| `SetScopeCommand` | Filter by scope |
| `SetLifecycleFilterCommand` | Filter by lifecycle |
| `RecordValueCommand` | Add new data point |

---

## TasksViewModel

**Purpose**: Task management within Pulse.

### Filters
```csharp
public enum TaskFilter
{
    All,
    Today,
    Overdue,
    Completed
}

[ObservableProperty] TaskFilter CurrentFilter;
```

### Key Properties

```csharp
// Collections
private ObservableCollection<TaskDetail> AllTasks { get; }
public ObservableCollection<TaskDetail> FilteredTasks { get; }
public ObservableCollection<TeamMemberDetail> TeamMembers { get; }

// Stats
[ObservableProperty] int TotalCount;
[ObservableProperty] int TodayCount;
[ObservableProperty] int OverdueCount;
[ObservableProperty] int CompletedCount;

// Selection
[ObservableProperty] TaskDetail? SelectedTask;
```

### Events

```csharp
public event EventHandler? AddTaskDialogRequested;
```

### Key Commands

| Command | Action |
|---------|--------|
| `LoadTasksCommand` | Refresh from TaskService |
| `RequestAddTaskDialogCommand` | Fire AddTaskDialogRequested |
| `SetFilterCommand` | Apply filter |
| `ToggleCompleteCommand` | Toggle task completion |
| `DeleteTaskCommand` | Soft delete task |

---

## ChronicleViewModel

**Purpose**: Notes management.

### Key Properties

```csharp
public ObservableCollection<NoteDetail> Notes { get; }

// Filters
[ObservableProperty] string SearchQuery;
[ObservableProperty] NoteScope? ScopeFilter;  // Private/Team/Shared

// Selection
[ObservableProperty] NoteDetail? SelectedNote;
[ObservableProperty] bool IsDetailFlyoutOpen;
[ObservableProperty] bool IsEditorOpen;
```

### Key Commands

| Command | Action |
|---------|--------|
| `LoadNotesCommand` | Refresh from NotesService |
| `CreateNoteCommand` | Open create dialog |
| `SearchCommand` | Apply search filter |
| `SelectNoteCommand` | Open detail view |
| `DeleteNoteCommand` | Soft delete note |

---

## SettingsViewModel

**Purpose**: User settings and profile editing.

### Key Properties

```csharp
// Display (read-only display)
[ObservableProperty] string DisplayName;
[ObservableProperty] string Email;
[ObservableProperty] string? AvatarUrl;
[ObservableProperty] string Initials;

// Editing (mutable form fields)
[ObservableProperty] bool IsEditingProfile;
[ObservableProperty] bool IsSavingProfile;
[ObservableProperty] string FirstName;
[ObservableProperty] string LastName;
[ObservableProperty] string JobTitle;
[ObservableProperty] string Company;
[ObservableProperty] string Phone;
[ObservableProperty] DateTime? Birthday;
[ObservableProperty] DateTime? HireDate;

// Theme
[ObservableProperty] bool IsDarkTheme;

// Logout
[ObservableProperty] bool IsLoggingOut;
```

### Static Converters
```csharp
public static FuncValueConverter<string?, string> InitialConverter { get; }
public static FuncValueConverter<bool, string> LogoutTextConverter { get; }
public static FuncValueConverter<bool, string> SaveTextConverter { get; }
```

### Key Commands

| Command | Action |
|---------|--------|
| `StartEditingCommand` | Enter edit mode |
| `CancelEditingCommand` | Exit without save |
| `SaveProfileCommand` | Save to AuthService |
| `UploadAvatarCommand` | Open file picker, upload |
| `ToggleThemeCommand` | Switch light/dark |
| `SignOutCommand` | Log out |

---

## Common Patterns

### Loading State
All ViewModels use this pattern:
```csharp
[ObservableProperty]
private bool _isLoading;

[ObservableProperty]
private string? _errorMessage;

[ObservableProperty]
private bool _hasError;
```

### Async Command Pattern
```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        ErrorMessage = null;
        
        // Load from service
        var data = await SomeService.Instance.GetDataAsync();
        Items.Clear();
        foreach (var item in data)
            Items.Add(item);
    }
    catch (Exception ex)
    {
        ErrorMessage = ex.Message;
        HasError = true;
    }
    finally
    {
        IsLoading = false;
    }
}
```

### Flyout Pattern
```csharp
[ObservableProperty]
private ItemDetail? _selectedItem;

[ObservableProperty]
private bool _isFlyoutOpen;

[RelayCommand]
private void SelectItem(ItemDetail item)
{
    SelectedItem = item;
    IsFlyoutOpen = true;
}

[RelayCommand]
private void CloseFlyout()
{
    IsFlyoutOpen = false;
    SelectedItem = null;
}
```

### Tab Pattern
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsTabA))]
[NotifyPropertyChangedFor(nameof(IsTabB))]
private int _selectedTab = 0;

public bool IsTabA => SelectedTab == 0;
public bool IsTabB => SelectedTab == 1;

[RelayCommand]
private void SetTab(string tabIndex)
{
    if (int.TryParse(tabIndex, out var index))
        SelectedTab = index;
}
```

---

## Invariants

1. **No business logic in ViewModels** - delegate to Services
2. **Services accessed via singleton** - `ServiceName.Instance`
3. **Collections are ObservableCollection** - for UI binding
4. **Commands use RelayCommand** - async where needed
5. **Loading state always tracked** - show spinners
6. **Errors captured and displayed** - never swallowed

