# ProCohere Notification & System Tray Implementation Plan

> **Created**: January 26, 2026  
> **Status**: In Progress  
> **Reference**: WPF Tracker's `SystemTrayService.cs`, `NotificationManager.cs`, `TrackerToast.xaml`

## Overview

This document outlines migrating the WPF Tracker's notification and system tray functionality to ProCohere.Avalonia. There are two main features:

1. **System Tray Integration** - App minimizes to tray on "close", with styled context menu
2. **Toast Notifications** - Both in-app styled toasts and native Windows notifications

---

## Part 1: System Tray ("Always Running" Behavior)

### What the WPF Version Does

- `SystemTrayService.cs` - Singleton service using WinForms `NotifyIcon`
- Shows custom dark-themed context menu with:
  - "Open ProCohere" (bold, primary action)
  - "Reminders Enabled" (toggle checkbox)
  - "Exit" (actually closes the app)
- On window close → Cancel close, hide window, show tray icon with balloon tip
- On tray icon double-click → Restore window
- On "Exit" from menu → Force close the app

### Avalonia Approach

Avalonia has **built-in `TrayIcon` support** - no external packages needed. Key differences:
- TrayIcon is declared in `App.axaml` (not code-behind)
- Uses `NativeMenu` instead of WinForms ContextMenuStrip
- Styling is limited (native menus) - but consistent with OS

### Implementation Steps

| Step | File | Description |
|------|------|-------------|
| 1.1 | `Services/LocalSettingsService.cs` | Add `MinimizeToTray` property |
| 1.2 | `App.axaml` | Add `TrayIcon.Icons` section with `NativeMenu` |
| 1.3 | `ViewModels/TrayIconViewModel.cs` | NEW: Commands for Open/Exit, toggle for reminders |
| 1.4 | `Services/SystemTrayService.cs` | NEW: Singleton to manage tray state and events |
| 1.5 | `App.axaml.cs` | Set up DataContext for tray icon, handle events |
| 1.6 | `Views/MainWindow.axaml.cs` | Override `OnClosing()` to hide instead of close |

### TrayIconViewModel Design

```csharp
TrayIconViewModel : ObservableObject
├── OpenCommand → Raises ShowWindowRequested event
├── ExitCommand → Calls Application.Shutdown()
├── RemindersEnabled (bool) → Toggle, persisted to LocalSettings
└── ToolTipText (string) → "ProCohere - Team Management"
```

### SystemTrayService Design

```csharp
SystemTrayService (Singleton)
├── Initialize() - Called from App.axaml.cs
├── ShowWindowRequested event
├── ExitRequested event
├── Show() / Hide() - Control tray visibility
└── IsVisible (bool)
```

### MainWindow Close Interception Pattern

```csharp
protected override void OnClosing(WindowClosingEventArgs e)
{
    var settings = LocalSettingsService.Instance.CurrentSettings;
    if (settings.MinimizeToTray && !_forceClose)
    {
        e.Cancel = true;
        Hide();
        // Tray icon already visible via App.axaml
    }
    base.OnClosing(e);
}
```

---

## Part 2: Toast Notifications

### What the WPF Version Does

- `NotificationManager.cs` - Singleton with `ShowInfo()`, `ShowSuccess()`, `ShowError()`, `ShowWarning()`
- `TrackerToast.xaml/.cs` - Custom styled WPF window:
  - Semi-transparent dark background
  - Color-coded accent bar (blue/green/amber/red)
  - Icon + Title + Message
  - Progress bar showing auto-dismiss timer
  - Pause timer on hover
  - Stack multiple toasts with animation
- Native Windows toasts via `Microsoft.Toolkit.Uwp.Notifications`

### Avalonia Approach

**In-App Toasts:**
- Create custom Avalonia Window similar to WPF's `TrackerToast`
- Use Avalonia animations for slide-in/fade
- Positioning via `Screens.Primary.WorkingArea`

**Native Windows Toasts:**
- `Microsoft.Toolkit.Uwp.Notifications` works on .NET 8 desktop apps
- Optional - for background notifications when minimized

### Implementation Steps

| Step | File | Description |
|------|------|-------------|
| 2.1 | `Models/ToastType.cs` | NEW: Enum (Information, Success, Warning, Error) |
| 2.2 | `Views/Toasts/ProCohereToast.axaml` | NEW: Custom window matching WPF design |
| 2.3 | `Views/Toasts/ProCohereToast.axaml.cs` | NEW: Animation, positioning, timer logic |
| 2.4 | `Services/NotificationService.cs` | NEW: Singleton with Show methods |

### NotificationService Design

```csharp
NotificationService (Singleton)
├── ShowInfo(title, message, duration = 5)
├── ShowSuccess(title, message, duration = 5)
├── ShowWarning(title, message, duration = 5)
├── ShowError(title, message, duration = 7)
├── ShowToast(title, message, type, duration)
├── CloseAllToasts()
└── SendNativeToast(title, message) [Windows only, optional]

Internal:
├── _activeToasts list for stacking
├── _toastLock for thread safety
└── Auto-reposition when toast closes
```

### Toast Visual Design

```
┌─────────────────────────────────────────┐
│▌ [Icon]  Title                      [X] │
│▌         Message text here...           │
│▌                                        │
│═════════════════════════════════════════│ ← Progress bar
└─────────────────────────────────────────┘
  ↑
  Accent bar (4px, color by type)
```

| Type | Accent Color | Icon |
|------|--------------|------|
| Information | `#3B82F6` (blue) | ℹ circle |
| Success | `#22C55E` (green) | ✓ circle |
| Warning | `#F59E0B` (amber) | ⚠ triangle |
| Error | `#EF4444` (red) | ✕ circle |

---

## Part 3: Settings Model Updates

### LocalSettings Additions

```csharp
// Add to LocalSettings class
public bool MinimizeToTray { get; set; } = true;
public bool StartWithWindows { get; set; } = false;
public bool EnableReminders { get; set; } = true;
public bool ShowMeetingReminders { get; set; } = true;
public int MeetingReminderMinutes { get; set; } = 15;
```

---

## File Summary

### New Files to Create

| File | Purpose |
|------|---------|
| `Services/SystemTrayService.cs` | Manages tray icon visibility and events |
| `Services/NotificationService.cs` | Shows in-app toasts, manages stacking |
| `ViewModels/TrayIconViewModel.cs` | Commands/state for tray context menu |
| `Views/Toasts/ProCohereToast.axaml` | Custom toast window XAML |
| `Views/Toasts/ProCohereToast.axaml.cs` | Toast logic, animations, positioning |
| `Models/ToastType.cs` | Enum for toast types |

### Files to Modify

| File | Changes |
|------|---------|
| `App.axaml` | Add TrayIcon.Icons section |
| `App.axaml.cs` | Initialize services, wire up tray events |
| `Views/MainWindow.axaml.cs` | Override OnClosing for minimize-to-tray |
| `Services/LocalSettingsService.cs` | Add new settings properties |

---

## Implementation Phases

### Phase 1: System Tray (Core) ✅ COMPLETE
1. ✅ Update LocalSettings with `MinimizeToTray`, `EnableReminders`, `StartWithWindows` properties
2. ✅ Create `SystemTrayService.cs` - Singleton with events and tray management
3. ✅ Create `TrayIconViewModel.cs` - Commands for Open/Exit, RemindersEnabled toggle
4. ✅ Add TrayIcon to `App.axaml` with NativeMenu
5. ✅ Wire up in `App.axaml.cs` - Initialize service, handle ShowWindowRequested/ExitRequested
6. ✅ Add close interception in `MainWindow.axaml.cs` - OnClosing override with ForceClose()
7. **Ready to Test**: Close → minimizes to tray, double-click restores, Exit closes

### Phase 2: In-App Toasts ✅ COMPLETE
1. ✅ Create `ToastType.cs` enum (Information, Success, Warning, Error)
2. ✅ Create `ProCohereToast.axaml` - Dark themed window with accent colors, icons, progress bar
3. ✅ Create `ProCohereToast.axaml.cs` - Timer-based animations, pause on hover, stacking support
4. ✅ Create `NotificationService.cs` - Singleton with ShowInfo/Success/Warning/Error methods
5. ✅ Wire up in `App.axaml.cs` - Welcome toast on login, cleanup on exit
6. **Ready to Test**: Login → see "Welcome Back" toast, hover pauses timer, click X closes

### Phase 3: Integration ✅ COMPLETE
1. ✅ Add toasts to `EditMeetingDialogViewModel` - Meeting save (create/update), delete
2. ✅ Add toasts to `GoalsViewModel` - Goal create, update, delete with error handling
3. ✅ Add toasts to `TasksViewModel` - Task create, complete/uncomplete toggle, delete
4. ✅ Add toasts to `MetricsViewModel` - Metric delete, value update
5. ✅ Add toasts to `ChronicleViewModel` - Note create, update, delete
6. ✅ Add toasts to `SettingsViewModel` - Profile save success/error
7. **Integrated**: All major CRUD operations now show toast feedback

### Phase 4: Native Toasts ✅ COMPLETE
1. ✅ Added `Microsoft.Toolkit.Uwp.Notifications` NuGet package (version 7.1.3)
2. ✅ Updated `ProCohere.Avalonia.csproj` TFM to `net8.0-windows10.0.17763.0` for Windows SDK support
3. ✅ Added `SendNativeToast(title, message)` to NotificationService
4. ✅ Added `IsMainWindowVisible` check - auto-switches to native toasts when app minimized to tray
5. ✅ Added `ClearNativeToasts()` cleanup on app exit
6. **Behavior**: When main window is visible → in-app toast. When minimized to tray → native Windows toast.

---

## Patterns Followed

| Pattern | Source | Application |
|---------|--------|-------------|
| Singleton services | WPF Tracker | SystemTrayService, NotificationService |
| MVVM for tray | Standard | TrayIconViewModel with commands |
| Event-based communication | WPF MainWindow | ShowWindowRequested, ExitRequested events |
| Thread-safe toast stacking | WPF NotificationManager | Lock + list management |
| No code duplication | AppDialogService precedent | All toast code in NotificationService |

---

## Known Limitations

1. **Native Menu Styling** - Avalonia's NativeMenu uses OS styling (can't customize colors like WPF did with ModernMenuRenderer)

2. **Balloon Tips** - Avalonia's TrayIcon has limited balloon support. Will show in-app toast instead.

3. **Screen Positioning** - Avalonia uses `Screens.Primary.WorkingArea` instead of WPF's `SystemParameters.WorkArea`

4. **Windows-Only Features** - Native Windows toasts require Windows; other platforms will only get in-app toasts
