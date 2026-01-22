# 03 – Navigation System

This document describes the **navigation architecture** in ProCohere.Avalonia.

---

## Overview

Navigation is managed by `MainWindowViewModel` and rendered by `MainWindow.axaml`.

The app uses a **sidebar navigation** pattern with:
- Main navigation items (Briefing, Me, Circle, Pulse, Chronicle, Settings)
- Sub-navigation within some sections (e.g., Pulse has Goals/Metrics/Tasks tabs)

---

## Navigation Items

```csharp
public enum NavigationItem
{
    Briefing,   // Daily summary
    Me,         // Personal hub
    Circle,     // Team view (managers only)
    Pulse,      // Goals, Metrics, Tasks
    Chronicle,  // Activity timeline (planned)
    Settings    // App settings
}
```

| Item | View | Who Sees It | Description |
|------|------|-------------|-------------|
| Briefing | BriefingView | Everyone | Daily/weekly summary |
| Me | MeView | Everyone | Personal profile, my items |
| Circle | CircleView | Managers only | Direct reports, team activity |
| Pulse | PulseView | Everyone | Goals, Metrics, Tasks |
| Chronicle | (planned) | Everyone | Activity timeline |
| Settings | SettingsView | Everyone | App settings, logout |

---

## MainWindowViewModel Navigation

### Properties

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(PageTitle))]
private NavigationItem _selectedNavigation = NavigationItem.Briefing;

[ObservableProperty]
private string _selectedSubNavigation = string.Empty;

[ObservableProperty]
private bool _isNavigationExpanded = true;
```

### Commands

```csharp
[RelayCommand]
private void NavigateTo(NavigationItem item)
{
    SelectedNavigation = item;
    SelectedSubNavigation = string.Empty;
}

[RelayCommand]
private void ToggleNavigation()
{
    IsNavigationExpanded = !IsNavigationExpanded;
}
```

### Page Title

```csharp
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
```

---

## MainWindow.axaml Structure

```xml
<Window>
    <Grid ColumnDefinitions="Auto,*">
        <!-- Sidebar (column 0) -->
        <Border>
            <StackPanel>
                <!-- Logo -->
                <!-- Navigation buttons -->
                <!-- User menu -->
            </StackPanel>
        </Border>
        
        <!-- Content area (column 1) -->
        <Grid RowDefinitions="Auto,*">
            <!-- Header row -->
            <Border>
                <TextBlock Text="{Binding PageTitle}"/>
            </Border>
            
            <!-- Content row - view switching -->
            <ContentControl>
                <!-- Visibility-based view switching -->
                <views:BriefingView IsVisible="{Binding SelectedNavigation, Converter=...}"/>
                <views:MeView IsVisible="..."/>
                <views:CircleView IsVisible="..."/>
                <views:PulseView IsVisible="..."/>
                <views:SettingsView IsVisible="..."/>
            </ContentControl>
        </Grid>
    </Grid>
</Window>
```

---

## View Switching

Views are switched using `IsVisible` bindings with converters:

```xml
<views:BriefingView 
    IsVisible="{Binding SelectedNavigation, 
               Converter={x:Static conv:NavigationConverters.IsBriefing}}"/>
```

### NavigationConverters.cs

```csharp
public static class NavigationConverters
{
    public static readonly IValueConverter IsBriefing =
        new FuncValueConverter<NavigationItem, bool>(n => n == NavigationItem.Briefing);
    
    public static readonly IValueConverter IsMe =
        new FuncValueConverter<NavigationItem, bool>(n => n == NavigationItem.Me);
    
    public static readonly IValueConverter IsCircle =
        new FuncValueConverter<NavigationItem, bool>(n => n == NavigationItem.Circle);
    
    public static readonly IValueConverter IsPulse =
        new FuncValueConverter<NavigationItem, bool>(n => n == NavigationItem.Pulse);
    
    public static readonly IValueConverter IsSettings =
        new FuncValueConverter<NavigationItem, bool>(n => n == NavigationItem.Settings);
}
```

---

## Manager-Only Navigation

Circle is only visible to managers:

```csharp
// In MainWindowViewModel constructor
var currentRole = AuthService.Instance.CurrentRole;
var roleName = currentRole?.Name?.ToLowerInvariant() ?? "";
HasDirectReports = roleName == "admin" || roleName == "manager";
```

```xml
<!-- In MainWindow.axaml -->
<Button Command="{Binding NavigateToCommand}"
        CommandParameter="{x:Static vm:NavigationItem.Circle}"
        IsVisible="{Binding HasDirectReports}">
    Circle
</Button>
```

---

## Sub-Navigation (Pulse Example)

Pulse has tabs for Goals, Metrics, Tasks.

### PulseViewModel

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsSubTabGoals))]
[NotifyPropertyChangedFor(nameof(IsSubTabMetrics))]
[NotifyPropertyChangedFor(nameof(IsSubTabTasks))]
private int _selectedSubTab = 0;

public bool IsSubTabGoals => SelectedSubTab == 0;
public bool IsSubTabMetrics => SelectedSubTab == 1;
public bool IsSubTabTasks => SelectedSubTab == 2;

[RelayCommand]
private void SetSubTab(string tabIndex)
{
    if (int.TryParse(tabIndex, out var index))
    {
        SelectedSubTab = index;
    }
}
```

### PulseView.axaml

```xml
<Grid RowDefinitions="Auto,*">
    <!-- Tab buttons -->
    <StackPanel Orientation="Horizontal">
        <Button Command="{Binding SetSubTabCommand}" CommandParameter="0">Goals</Button>
        <Button Command="{Binding SetSubTabCommand}" CommandParameter="1">Metrics</Button>
        <Button Command="{Binding SetSubTabCommand}" CommandParameter="2">Tasks</Button>
    </StackPanel>
    
    <!-- Tab content -->
    <Grid Grid.Row="1">
        <views:GoalsTabView IsVisible="{Binding IsSubTabGoals}"/>
        <views:MetricsTabView IsVisible="{Binding IsSubTabMetrics}"/>
        <views:TasksTabView IsVisible="{Binding IsSubTabTasks}"/>
    </Grid>
</Grid>
```

---

## Briefing Sub-Navigation

Briefing switches between Manager and IC (Individual Contributor) views:

| Role | View | Content |
|------|------|---------|
| Manager | ManagerBriefingContent | Team activity, attention needed |
| IC | ICBriefingContent | Personal tasks, upcoming meetings |

Detection is based on role:
```csharp
public bool IsManager => HasDirectReports;
```

---

## Navigation State

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| SelectedNavigation | NavigationItem | Briefing | Current main section |
| SelectedSubNavigation | string | "" | Sub-section (future use) |
| IsNavigationExpanded | bool | true | Sidebar collapsed/expanded |
| HasDirectReports | bool | false | Show Circle nav item |

---

## Navigation Events

### SignOutRequested
Fired when user signs out:
```csharp
public event Action? SignOutRequested;

[RelayCommand]
private async Task SignOutAsync()
{
    await AuthService.Instance.SignOutAsync();
    SignOutRequested?.Invoke();
}
```

MainWindow handles this to navigate to LoginWindow.

### EditProfileRequested
Fired when user wants to edit profile:
```csharp
public event Action? EditProfileRequested;

[RelayCommand]
private void EditProfile()
{
    EditProfileRequested?.Invoke();
}
```

MainWindow handles this to show EditAccountDialog.

---

## View Hierarchy

```
MainWindow
├── BriefingView
│   ├── ManagerBriefingContent (managers)
│   └── ICBriefingContent (ICs)
├── MeView
├── CircleView (managers only)
├── PulseView
│   ├── GoalsTabView
│   ├── MetricsTabView
│   └── TasksTabView
└── SettingsView
```

---

## Key Files

| File | Purpose |
|------|---------|
| `ViewModels/MainWindowViewModel.cs` | Navigation state, commands |
| `Views/MainWindow.axaml` | Navigation UI, view switching |
| `Converters/NavigationConverters.cs` | NavigationItem → bool converters |
| `ViewModels/PulseViewModel.cs` | Sub-tab navigation |

---

## Invariants

1. Default navigation is Briefing
2. Circle is only visible to managers (HasDirectReports = true)
3. Navigation changes clear sub-navigation
4. All views exist in the visual tree (visibility-based switching)
5. Navigation state is in ViewModel, not View

