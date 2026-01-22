# 08 – Views Reference

This document describes all **Views** in ProCohere.Avalonia.

---

## Overview

Views are Avalonia UserControls/Windows using XAML:
- `.axaml` - XAML markup (declarative UI)
- `.axaml.cs` - Code-behind (event handlers, dialog hosting)

All views follow MVVM - business logic lives in ViewModels, not code-behind.

---

## View Hierarchy

```
App
├── SplashWindow           (startup)
├── LoginWindow            (authentication)
└── MainWindow             (main shell)
    ├── BriefingView
    │   ├── ManagerBriefingContent
    │   └── ICBriefingContent
    ├── MeView
    ├── CircleView         (managers only)
    ├── PulseView
    │   ├── GoalsTabView
    │   ├── MetricsTabView
    │   └── TasksTabView
    └── SettingsView
```

---

## Windows

### SplashWindow
**File**: `Views/SplashWindow.axaml`

Shown during app startup while checking auth.

- Shows logo and loading spinner
- No ViewModel - purely visual
- Auto-closed by App.axaml.cs after auth check

### LoginWindow
**File**: `Views/LoginWindow.axaml`

Authentication screen.

- Email/password fields
- "Remember Me" checkbox
- Sign up link
- ViewModel: `LoginViewModel`

### MainWindow
**File**: `Views/MainWindow.axaml`

Main application shell.

**Structure**:
```
Grid
├── Column 0: Sidebar (navigation)
└── Column 1: Content area
    ├── Row 0: Header (page title)
    └── Row 1: View content (visibility-switched)
```

- Navigation sidebar (collapsible)
- Page title header
- Content area with all main views
- ViewModel: `MainWindowViewModel`

**Code-behind responsibilities**:
- Wire up `SignOutRequested` event → navigate to LoginWindow
- Wire up `EditProfileRequested` event → show EditAccountDialog
- Host dialogs for various views

---

## Main Views

### BriefingView
**Files**: `Views/Briefing/BriefingView.axaml`

Daily summary dashboard.

**Sub-components**:
- `ManagerBriefingContent` - Team activity, attention items
- `ICBriefingContent` - Personal tasks, upcoming meetings

**Switching logic** (in code-behind):
```csharp
if (viewModel.IsManager)
    ManagerContent.IsVisible = true;
else
    ICContent.IsVisible = true;
```

ViewModel: `BriefingViewModel`

### MeView
**File**: `Views/MeView.axaml`

Personal hub - my tasks, goals, meetings, feedback.

**Tabs**: Tasks | Goals | Feedback | Meetings

**Flyouts** (shown in overlay):
- Task detail flyout
- Meeting detail flyout
- Goal detail flyout
- Feedback detail flyout

ViewModel: `MeViewModel`

### CircleView
**File**: `Views/CircleView.axaml`

Team management (managers only).

**Tabs**: Team | Goals | Feedback | Meetings

**Features**:
- Team member list with hierarchy
- Team member detail flyout
- Add/edit team member dialogs
- Meeting scheduling for team

ViewModel: `CircleViewModel`

### PulseView
**File**: `Views/PulseView.axaml`

Goals, Metrics, Tasks coordinator.

**Structure**:
```
Grid
├── Row 0: Tab buttons (Goals | Metrics | Tasks)
└── Row 1: Tab content
    ├── GoalsTabView
    ├── MetricsTabView
    └── TasksTabView
```

ViewModel: `PulseViewModel`

### SettingsView
**File**: `Views/SettingsView.axaml`

User settings and profile.

**Sections**:
- Profile display (avatar, name, email)
- Profile editing (first name, last name, etc.)
- Theme toggle (Light/Dark)
- Sign out button

ViewModel: `SettingsViewModel`

---

## Pulse Sub-Views

### GoalsTabView
**File**: `Views/Pulse/GoalsTabView.axaml`

Goal management within Pulse.

**Elements**:
- Scope filter (My | Team | Shared)
- Goal cards in grid
- Goal detail flyout
- Goal editor flyout
- Health/Lifecycle dialogs

ViewModel: `GoalsViewModel`

### MetricsTabView
**File**: `Views/Pulse/MetricsTabView.axaml`

Metric management within Pulse.

**Elements**:
- Scope filter (Individual | Team | Org | All)
- Lifecycle filter (Active | Dormant | Retired | All)
- Metric cards with trend arrows
- Metric detail flyout
- Update value dialog

ViewModel: `MetricsViewModel`

### TasksTabView
**File**: `Views/Pulse/TasksTabView.axaml`

Task management within Pulse.

**Elements**:
- Filter tabs (All | Today | Overdue | Completed)
- Task list
- Task detail flyout
- Add task dialog

ViewModel: `TasksViewModel`

---

## Dialogs

All dialogs are in `Views/Dialogs/`.

| Dialog | Purpose |
|--------|---------|
| `AddTaskDialog` | Create new task |
| `ApplyTemplateDialog` | Apply meeting template |
| `DeferAgendaItemDialog` | Defer agenda item to future meeting |
| `EditAccountDialog` | Edit user profile |
| `EditGoalDialog` | Create/edit goal |
| `EditMeetingDialog` | Create/edit meeting |
| `EditMetricDialog` | Create/edit metric |
| `EditTeamMemberDialog` | Edit team member details |
| `EntityPickerDialog` | Pick entity to link (generic) |
| `RecordOutcomeDialog` | Record agenda item outcome |
| `UpdateMetricValueDialog` | Record metric data point |

### Dialog Pattern

Dialogs are modal windows shown via code-behind:

```csharp
private async void ShowAddTaskDialog()
{
    var dialog = new AddTaskDialog();
    var result = await dialog.ShowDialog<TaskDetail?>(this);
    if (result != null)
    {
        await viewModel.RefreshTasks();
    }
}
```

---

## Controls (Flyouts & Cards)

All controls are in `Views/Controls/`.

### Flyouts

Flyouts are slide-in panels for detail views:

| Flyout | Purpose |
|--------|---------|
| `GoalDetailFlyout` | View goal details, linked metrics |
| `GoalEditorFlyout` | Edit goal inline |
| `MeetingDetailFlyout` | View meeting with agenda, attendees |
| `NoteDetailFlyout` | View note with links |
| `TaskDetailFlyout` | View task with provenance |
| `TeamMemberDetailFlyout` | View team member profile |

### Cards

Cards are list item displays:

| Card | Purpose |
|------|---------|
| `GoalCard` | Goal summary in list |
| `AgendaItemCard` | Agenda item in meeting view |

### Special Controls

| Control | Purpose |
|---------|---------|
| `DateTimeSelector` | Combined date/time picker |
| `CarryForwardSuggestionsPanel` | Agenda items to carry forward |
| `HealthChangeDialog` | Change goal health with reason |
| `LifecycleChangeDialog` | Change goal lifecycle with reason |

---

## Briefing Sub-Views

### ManagerBriefingContent
**File**: `Views/Briefing/ManagerBriefingContent.axaml`

Manager-specific briefing content:
- Team activity sparkline
- Attention needed items
- Upcoming team meetings
- Team-level stats

### ICBriefingContent
**File**: `Views/Briefing/ICBriefingContent.axaml`

Individual contributor briefing:
- Personal task inventory
- Upcoming meetings
- My goals status
- Recent feedback

---

## View ↔ ViewModel Binding

### DataContext Assignment

In code-behind constructor:
```csharp
public MeView()
{
    InitializeComponent();
    DataContext = new MeViewModel();
}
```

Or in XAML:
```xml
<UserControl.DataContext>
    <vm:MeViewModel />
</UserControl.DataContext>
```

### Command Binding
```xml
<Button Command="{Binding LoadDataCommand}" Content="Refresh" />
```

### Property Binding
```xml
<TextBlock Text="{Binding DisplayName}" />
<ProgressBar IsVisible="{Binding IsLoading}" />
```

### Visibility Binding
```xml
<views:MeView IsVisible="{Binding SelectedNavigation, 
    Converter={x:Static conv:NavigationConverters.IsMe}}" />
```

---

## Code-Behind Patterns

### Event Wiring
```csharp
protected override void OnDataContextChanged(EventArgs e)
{
    base.OnDataContextChanged(e);
    
    if (DataContext is MeViewModel vm)
    {
        vm.AddTaskRequested += ShowAddTaskDialog;
    }
}
```

### Dialog Hosting
```csharp
private async void OnEditMeetingClicked(object? sender, RoutedEventArgs e)
{
    if (sender is Button { Tag: MeetingDetail meeting })
    {
        var dialog = new EditMeetingDialog { Meeting = meeting };
        await dialog.ShowDialog(this);
        await _viewModel.RefreshMeetings();
    }
}
```

### Accessing Parent Window
```csharp
private Window? GetParentWindow()
{
    return VisualRoot as Window;
}

private async void ShowDialog()
{
    var parent = GetParentWindow();
    if (parent != null)
    {
        await dialog.ShowDialog(parent);
    }
}
```

---

## Styling Conventions

### Theme Resources
Use DynamicResource for theme-aware colors:
```xml
<Border Background="{DynamicResource BackgroundBrush}" />
<TextBlock Foreground="{DynamicResource TextPrimaryBrush}" />
```

### Common Style Classes
```xml
<TextBlock Classes="heading" />
<TextBlock Classes="subheading" />
<Button Classes="primary" />
<Button Classes="secondary" />
```

### Spacing
Standard margins/padding:
```xml
Margin="16"     <!-- Standard padding -->
Margin="8"      <!-- Tight padding -->
Margin="24"     <!-- Section spacing -->
```

---

## Key Files Summary

| File | Type | Purpose |
|------|------|---------|
| `MainWindow.axaml` | Window | Main app shell, navigation |
| `LoginWindow.axaml` | Window | Authentication |
| `SplashWindow.axaml` | Window | Startup splash |
| `BriefingView.axaml` | UserControl | Daily summary |
| `MeView.axaml` | UserControl | Personal hub |
| `CircleView.axaml` | UserControl | Team management |
| `PulseView.axaml` | UserControl | Goals/Metrics/Tasks |
| `SettingsView.axaml` | UserControl | User settings |

---

## Invariants

1. **No business logic in code-behind** - delegate to ViewModel
2. **Dialogs return values** - not modify state directly
3. **Views don't call services** - only ViewModels do
4. **Theme-aware styling** - use DynamicResource
5. **DataContext is ViewModel** - never model directly

