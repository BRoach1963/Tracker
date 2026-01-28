# 13 – Notifications & Reminders Reference

This document describes the **Toast Notification System** and **Reminders System** in ProCohere.Avalonia.

---

## Table of Contents

1. [Overview](#overview)
2. [Toast Notification System](#toast-notification-system)
   - [Architecture](#toast-architecture)
   - [Toast Types](#toast-types)
   - [In-App Toasts](#in-app-toasts)
   - [Native Windows Toasts](#native-windows-toasts)
   - [How to Show a Toast](#how-to-show-a-toast)
3. [Reminders System](#reminders-system)
   - [Architecture](#reminder-architecture)
   - [Reminder Model](#reminder-model)
   - [ReminderDataService](#reminderdataservice)
   - [ReminderSchedulerService](#reminderschedulerservice)
   - [Reminder Settings](#reminder-settings)
   - [How to Create a Reminder](#how-to-create-a-reminder)
4. [Service Integration](#service-integration)
   - [Automatic Reminder Creation](#automatic-reminder-creation)
   - [Toast Actions (Snooze/Dismiss)](#toast-actions-snoozedismiss)
5. [Settings UI](#settings-ui)
6. [Troubleshooting](#troubleshooting)
7. [File Reference](#file-reference)

---

## Overview

ProCohere has two complementary notification systems:

| System | Purpose | Storage |
|--------|---------|---------|
| **Toasts** | Immediate user feedback | In-memory only |
| **Reminders** | Scheduled future notifications | Supabase `reminders` table |

**Flow Diagram:**
```
User Action (create meeting)
    │
    ├── Immediate feedback → Toast ("Meeting created!")
    │
    └── Future notification → Reminder (created in DB, triggers later)
                                    │
                                    └── When due → Toast (with Snooze/Dismiss)
```

---

## Toast Notification System

### Toast Architecture

```
NotificationService (Singleton)
    │
    ├── In-App Toasts ─────► ProCohereToast (Avalonia Window)
    │   (window visible)         └── Animated, stacked, auto-dismiss
    │
    └── Native Toasts ─────► Windows Toast Notifications
        (window hidden)          └── Via Microsoft.Toolkit.Uwp.Notifications
                                 └── Appear in Windows Action Center
```

**Key Files:**
- `Services/NotificationService.cs` - Main service
- `Views/Toasts/ProCohereToast.axaml(.cs)` - In-app toast window
- `Services/ToastActivationHandler.cs` - Native toast button handlers

### Toast Types

ProCohere supports four toast types, each with distinct styling:

| Type | Icon | Use Case |
|------|------|----------|
| `Information` | ℹ️ (blue) | Neutral information |
| `Success` | ✅ (green) | Successful operations |
| `Warning` | ⚠️ (amber) | Potential issues |
| `Error` | ❌ (red) | Failures, errors |

```csharp
public enum ToastType
{
    Information,
    Success,
    Warning,
    Error
}
```

### In-App Toasts

In-app toasts appear as floating windows in the bottom-right corner of the screen.

**Features:**
- Stacked display (multiple toasts at once)
- Smooth slide-in animation
- Auto-dismiss after configurable duration
- Close button for manual dismissal
- Color-coded by type

**Behavior:**
```
Toast 1 appears at bottom-right
    │
Toast 2 appears → Toast 1 slides up
    │
Toast 1 dismissed → Toast 2 slides down
```

### Native Windows Toasts

When the main window is hidden (minimized to system tray), toasts appear as Windows notifications.

**Features:**
- Appear in Windows notification area
- Persist in Action Center
- Support action buttons (Snooze/Dismiss for reminders)
- Use Windows default notification sound

**Requirements:**
- Windows 10 version 1809+ 
- App identity configured (for toast persistence)
- `Microsoft.Toolkit.Uwp.Notifications` NuGet package

### How to Show a Toast

#### Basic Usage

```csharp
// Information toast (5 second default)
NotificationService.Instance.ShowInfo("Title", "Message");

// Success toast (5 second default)
NotificationService.Instance.ShowSuccess("Saved", "Meeting has been saved.");

// Warning toast (5 second default)
NotificationService.Instance.ShowWarning("Warning", "Connection unstable.");

// Error toast (7 second default for visibility)
NotificationService.Instance.ShowError("Error", "Failed to save meeting.");
```

#### Custom Duration

```csharp
// Show for 10 seconds
NotificationService.Instance.ShowInfo("Title", "Message", durationSeconds: 10);

// Show for 3 seconds
NotificationService.Instance.ShowSuccess("Quick!", "Done.", durationSeconds: 3);
```

#### Generic Toast Method

```csharp
// Full control over toast type and duration
NotificationService.Instance.ShowToast(
    title: "Custom Toast",
    message: "This is a custom toast.",
    type: ToastType.Warning,
    durationSeconds: 8
);
```

#### Native Toast Only

```csharp
// Force native Windows toast (ignores window visibility)
NotificationService.Instance.SendNativeToast("Title", "Message");
```

#### Reminder Toast with Actions

```csharp
// Native toast with Snooze and Dismiss buttons
NotificationService.Instance.SendReminderToast(
    title: "📅 Meeting in 15 minutes",
    message: "Weekly Standup",
    reminderId: reminder.Id,
    snoozeMinutes: 10
);
```

#### Closing All Toasts

```csharp
// Close all active in-app toasts (used during shutdown)
NotificationService.Instance.CloseAllToasts();
```

---

## Reminders System

### Reminder Architecture

```
                                    ┌─────────────────────────┐
                                    │   Supabase Database     │
                                    │   (reminders table)     │
                                    └───────────┬─────────────┘
                                                │
                    ┌───────────────────────────┼───────────────────────────┐
                    │                           │                           │
                    ▼                           ▼                           ▼
        ┌───────────────────┐       ┌───────────────────┐       ┌───────────────────┐
        │ ReminderDataService│       │ReminderScheduler  │       │ MeetingService    │
        │  (CRUD operations) │◄─────►│ Service           │       │ TaskService       │
        │                   │       │ (background timer) │       │ GoalsService      │
        └───────────────────┘       └─────────┬─────────┘       └─────────┬─────────┘
                                              │                           │
                                              │ Check every 60s           │ Auto-create
                                              │ for due reminders         │ reminders
                                              ▼                           │
                                    ┌───────────────────┐                 │
                                    │NotificationService│◄────────────────┘
                                    │   (show toast)    │
                                    └───────────────────┘
```

### Reminder Model

**File:** `Models/Reminder.cs`

```csharp
public class Reminder : BaseModel
{
    // Identity
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }           // Supabase auth user
    public Guid? TeamMemberId { get; set; }     // Team member reference
    
    // Type & Entity Linking
    public ReminderType Type { get; set; }      // Meeting, Task, Goal, etc.
    public string? EntityType { get; set; }     // "meeting", "task", "goal"
    public Guid? EntityId { get; set; }         // FK to entity
    
    // Content
    public string Title { get; set; }
    public string? Message { get; set; }
    
    // Scheduling
    public DateTime RemindAt { get; set; }      // When to trigger
    public int? MinutesBefore { get; set; }     // For reference
    
    // Status
    public ReminderStatus Status { get; set; }  // Pending, Sent, Dismissed, etc.
    public DateTime? SentAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? SnoozedUntil { get; set; }
    
    // Notification Channels
    public bool SendPush { get; set; }
    public bool SendEmail { get; set; }
    public bool SendInApp { get; set; }
    
    // Recurrence (future)
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    
    // Computed Properties
    public bool IsDue => Status == ReminderStatus.Pending && RemindAt <= DateTime.UtcNow;
    public bool IsActivelySnoozed => Status == ReminderStatus.Snoozed && SnoozedUntil > DateTime.UtcNow;
    public bool IsSnoozeDue => Status == ReminderStatus.Snoozed && SnoozedUntil <= DateTime.UtcNow;
}
```

#### Reminder Types

```csharp
public enum ReminderType
{
    Meeting,      // Meeting reminder
    Task,         // Task deadline
    Goal,         // Goal deadline
    Engagement,   // Engagement reminder (future)
    Custom        // User-created custom reminder
}
```

#### Reminder Status

```csharp
public enum ReminderStatus
{
    Pending,      // Waiting to trigger
    Sent,         // Notification sent (terminal for non-snoozed)
    Triggered,    // Alias for Sent
    Snoozed,      // User snoozed, will re-trigger
    Dismissed,    // User dismissed
    Cancelled     // Entity was deleted
}
```

### ReminderDataService

**File:** `Services/ReminderDataService.cs`

Handles all CRUD operations for reminders in Supabase.

#### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetPendingRemindersAsync()` | `Task<List<Reminder>>` | All pending reminders for user |
| `GetDueRemindersAsync()` | `Task<List<Reminder>>` | Reminders where `RemindAt <= now` |
| `GetRemindersForEntityAsync(type, id)` | `Task<List<Reminder>>` | Reminders for specific entity |
| `CreateReminderAsync(reminder)` | `Task<Reminder?>` | Create new reminder |
| `CreateMeetingReminderAsync(meeting, mins)` | `Task<Reminder?>` | Create meeting reminder |
| `CreateTaskReminderAsync(task, days)` | `Task<Reminder?>` | Create task reminder |
| `CreateGoalReminderAsync(goal, days)` | `Task<Reminder?>` | Create goal reminder |
| `CreateCustomReminderAsync(title, msg, time)` | `Task<Reminder?>` | Create custom reminder |
| `MarkReminderSentAsync(id)` | `Task<bool>` | Mark as sent |
| `DismissReminderAsync(id)` | `Task<bool>` | Mark as dismissed |
| `SnoozeReminderAsync(id, mins)` | `Task<bool>` | Snooze for X minutes |
| `CancelRemindersForEntityAsync(type, id)` | `Task<int>` | Cancel all for entity |
| `ReminderExistsAsync(type, id, reminderType)` | `Task<bool>` | Check for duplicates |

#### Creating Specific Reminder Types

**Meeting Reminder:**
```csharp
// Create reminder for meeting starting in 15 minutes
var meeting = await MeetingService.Instance.GetMeetingAsync(meetingId);
var reminder = await ReminderDataService.Instance.CreateMeetingReminderAsync(
    meeting, 
    minutesBefore: 15
);
// → Title: "Upcoming: Weekly Standup"
// → Message: "Meeting starts in 15 minutes"
// → RemindAt: meeting.ScheduledAt - 15 minutes
```

**Task Reminder:**
```csharp
// Create reminder for task due in 2 days
var task = await TaskService.Instance.GetTaskAsync(taskId);
var reminder = await ReminderDataService.Instance.CreateTaskReminderAsync(
    task, 
    daysBefore: 2
);
// → Title: "Task Due Soon: Review proposal"
// → Message: "Due in 2 days"
// → RemindAt: task.DueDate - 2 days
```

**Goal Reminder:**
```csharp
// Create reminder for goal deadline in 7 days
var goal = await GoalsService.Instance.GetGoalByIdAsync(goalId);
var reminder = await ReminderDataService.Instance.CreateGoalReminderAsync(
    goal, 
    daysBefore: 7
);
// → Title: "Goal Deadline: Q1 Revenue Target"
// → Message: "Due date in 7 days"
// → RemindAt: goal.DueDate - 7 days
```

**Custom Reminder:**
```csharp
// Create a custom reminder
var reminder = await ReminderDataService.Instance.CreateCustomReminderAsync(
    title: "Follow up with client",
    message: "Call John about the proposal",
    remindAt: DateTime.UtcNow.AddHours(3)
);
```

### ReminderSchedulerService

**File:** `Services/ReminderSchedulerService.cs`

Background service that monitors and triggers reminders.

#### Lifecycle

```
App Startup
    │
    ▼
User Authenticates
    │
    ▼
ReminderSchedulerService.Instance.Start()
    │
    ├── Load settings
    ├── Check if enabled
    └── Start timer (60-second interval)
            │
            ▼ (every 60 seconds)
        CheckRemindersAsync()
            │
            ├── Get due reminders from DB
            ├── Mark as sent
            ├── Show notification
            └── Fire ReminderTriggered event
    
User Signs Out / App Exits
    │
    ▼
ReminderSchedulerService.Instance.Stop()
    │
    └── Dispose timer
```

#### Key Methods

| Method | Description |
|--------|-------------|
| `Start()` | Start the scheduler (call after auth) |
| `Stop()` | Stop the scheduler (call on logout/exit) |
| `ReloadSettings(settings?)` | Update settings, restart if running |
| `CheckNowAsync()` | Force immediate check (for testing) |
| `SnoozeReminderAsync(id, mins?)` | Snooze a reminder |
| `DismissReminderAsync(id)` | Dismiss a reminder |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Settings` | `ReminderSettings` | Current settings |
| `ReminderTriggered` | `event` | Fired when reminder triggers |

#### Usage in App.axaml.cs

```csharp
// On successful login:
ReminderSchedulerService.Instance.Start();

// On app exit:
ReminderSchedulerService.Instance.Stop();
```

### Reminder Settings

**File:** `Models/ReminderSettings.cs`

```csharp
public class ReminderSettings
{
    // Master toggle
    public bool EnableReminders { get; set; } = true;
    
    // Meeting reminders
    public bool ShowMeetingReminders { get; set; } = true;
    public int MeetingReminderMinutes { get; set; } = 15;
    
    // Task reminders
    public bool ShowTaskReminders { get; set; } = true;
    public int TaskReminderDays { get; set; } = 1;
    
    // Goal reminders
    public bool ShowGoalReminders { get; set; } = true;
    public int GoalReminderDays { get; set; } = 7;
    
    // General
    public bool PlaySound { get; set; } = true;
    public int DefaultSnoozeDurationMinutes { get; set; } = 10;
    
    // Static default
    public static ReminderSettings Default => new();
}
```

### How to Create a Reminder

#### Method 1: Automatic (via Service Integration)

Reminders are automatically created when you create entities through their services:

```csharp
// Creating a meeting automatically creates a reminder
var meeting = await MeetingService.Instance.CreateMeetingAsync(meetingDetail);
// → If settings.ShowMeetingReminders == true, reminder is created

// Creating a task with due date automatically creates a reminder
var task = await TaskService.Instance.CreateTaskAsync(
    title: "Review proposal",
    dueDate: DateTime.UtcNow.AddDays(3)
);
// → If settings.ShowTaskReminders == true, reminder is created

// Creating a goal with due date automatically creates a reminder
var goal = await GoalsService.Instance.CreateGoalAsync(goalDetail);
// → If settings.ShowGoalReminders == true, reminder is created
```

#### Method 2: Manual (via ReminderDataService)

```csharp
// Create a meeting reminder manually
await ReminderDataService.Instance.CreateMeetingReminderAsync(meeting, minutesBefore: 30);

// Create a task reminder manually
await ReminderDataService.Instance.CreateTaskReminderAsync(task, daysBefore: 3);

// Create a goal reminder manually
await ReminderDataService.Instance.CreateGoalReminderAsync(goal, daysBefore: 14);

// Create a custom reminder
await ReminderDataService.Instance.CreateCustomReminderAsync(
    title: "Check-in with Sarah",
    message: "Discuss Q2 planning",
    remindAt: DateTime.UtcNow.AddHours(4)
);
```

#### Method 3: Low-Level (Full Control)

```csharp
var reminder = new Reminder
{
    Type = ReminderType.Custom,
    EntityType = "custom",
    EntityId = Guid.NewGuid(),
    Title = "My Custom Reminder",
    Message = "Don't forget!",
    RemindAt = DateTime.UtcNow.AddMinutes(30),
    SendInApp = true,
    SendPush = true,
    SendEmail = false
};

var created = await ReminderDataService.Instance.CreateReminderAsync(reminder);
```

---

## Service Integration

### Automatic Reminder Creation

Each entity service is integrated with the reminder system:

#### MeetingService

```csharp
// In CreateMeetingAsync:
// After meeting is created successfully...
await CreateMeetingReminderIfEnabledAsync(meeting);

// In DeleteMeetingAsync:
// After meeting is deleted...
await CancelMeetingRemindersAsync(meetingId);

// Public method for schedule changes:
await MeetingService.Instance.UpdateMeetingReminderAsync(meeting);
```

#### TaskService

```csharp
// In CreateTaskAsync:
// After task is created successfully (if has due date)...
await CreateTaskReminderIfEnabledAsync(task);

// In DeleteTaskAsync:
// After task is deleted...
await CancelTaskRemindersAsync(taskId);

// Public method for due date changes:
await TaskService.Instance.UpdateTaskReminderAsync(task);
```

#### GoalsService

```csharp
// In CreateGoalAsync:
// After goal is created successfully (if has due date)...
await CreateGoalReminderIfEnabledAsync(goal);

// In DeleteGoalAsync:
// After goal is deleted...
await CancelGoalRemindersAsync(goalId);

// Public method for due date changes:
await GoalsService.Instance.UpdateGoalReminderAsync(goal);
```

### Toast Actions (Snooze/Dismiss)

When a reminder triggers while the window is hidden, a native Windows toast appears with action buttons.

**File:** `Services/ToastActivationHandler.cs`

```
User clicks Snooze
    │
    ▼
ToastActivationHandler.OnToastActivated()
    │
    ▼
HandleSnoozeAction(args)
    │
    ├── Parse reminderId from args
    ├── ReminderSchedulerService.Instance.SnoozeReminderAsync(id, mins)
    └── NotificationService.Instance.RemoveReminderToast(id)
```

```
User clicks Dismiss
    │
    ▼
ToastActivationHandler.OnToastActivated()
    │
    ▼
HandleDismissAction(args)
    │
    ├── Parse reminderId from args
    ├── ReminderSchedulerService.Instance.DismissReminderAsync(id)
    └── NotificationService.Instance.RemoveReminderToast(id)
```

**Initialization (App.axaml.cs):**
```csharp
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
    DataContext = TrayViewModel;
    
    // Must be early, before any toasts
    ToastActivationHandler.Initialize();
}
```

---

## Settings UI

The Settings view includes a "Notifications & Reminders" card.

**ViewModel Properties** (`SettingsViewModel.cs`):
```csharp
[ObservableProperty] private bool _enableReminders = true;
[ObservableProperty] private bool _showMeetingReminders = true;
[ObservableProperty] private int _meetingReminderMinutes = 15;
[ObservableProperty] private bool _showTaskReminders = true;
[ObservableProperty] private int _taskReminderDays = 1;
[ObservableProperty] private bool _showGoalReminders = true;
[ObservableProperty] private int _goalReminderDays = 7;
[ObservableProperty] private int _snoozeDurationMinutes = 10;
[ObservableProperty] private bool _playReminderSound = true;
```

**Auto-Save Pattern:**
```csharp
partial void OnEnableRemindersChanged(bool value)
{
    SaveReminderSettings();
}

private void SaveReminderSettings()
{
    var settings = new ReminderSettings
    {
        EnableReminders = EnableReminders,
        ShowMeetingReminders = ShowMeetingReminders,
        // ... all properties
    };
    
    ReminderSchedulerService.Instance.ReloadSettings(settings);
}
```

---

## Troubleshooting

### Toast Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| No toasts appear | Window visible check failing | Check `IsMainWindowVisible` delegate |
| Toasts stack incorrectly | Animation timing | Check `SetStackOffset` calls |
| Native toasts don't appear | Not Windows / Missing package | Verify platform and NuGet |
| Toast buttons don't work | Handler not initialized | Ensure `ToastActivationHandler.Initialize()` called early |

### Reminder Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Reminders not triggering | Scheduler not started | Verify `Start()` called after auth |
| Duplicate reminders | Missing existence check | Use `ReminderExistsAsync` |
| Reminders for deleted items | Missing cancellation | Check service integration |
| Wrong reminder time | Timezone issue | All times should be UTC |

### Debug Logging

All services log to files in `%LocalAppData%\ProCohere\`:

| File | Service |
|------|---------|
| `notification_service.log` | NotificationService (if added) |
| `reminder_service.log` | ReminderDataService |
| `reminder_scheduler.log` | ReminderSchedulerService |
| `meeting_service.log` | MeetingService |
| `task_service.log` | TaskService |
| `goals_service.log` | GoalsService |

---

## File Reference

### Core Files

| File | Purpose |
|------|---------|
| `Models/Reminder.cs` | Reminder entity model |
| `Models/ReminderType.cs` | Reminder type enum |
| `Models/ReminderStatus.cs` | Reminder status enum |
| `Models/ReminderSettings.cs` | Settings model |
| `Services/NotificationService.cs` | Toast notification service |
| `Services/ReminderDataService.cs` | Reminder CRUD operations |
| `Services/ReminderSchedulerService.cs` | Background scheduler |
| `Services/ToastActivationHandler.cs` | Native toast button handlers |
| `Views/Toasts/ProCohereToast.axaml` | In-app toast window |
| `Views/Toasts/ProCohereToast.axaml.cs` | Toast code-behind |

### Integration Points

| File | Integration |
|------|-------------|
| `Services/MeetingService.cs` | Auto-create/cancel meeting reminders |
| `Services/TaskService.cs` | Auto-create/cancel task reminders |
| `Services/GoalsService.cs` | Auto-create/cancel goal reminders |
| `ViewModels/SettingsViewModel.cs` | Reminder settings UI binding |
| `Views/SettingsView.axaml` | Reminder settings UI |
| `App.axaml.cs` | Scheduler start/stop, handler init |

### Database

| Table | Purpose |
|-------|---------|
| `reminders` | Reminder storage (Supabase) |

---

## Quick Reference

### Show Toast (One-Liner)

```csharp
NotificationService.Instance.ShowSuccess("Title", "Message");
```

### Create Reminder (One-Liner)

```csharp
await ReminderDataService.Instance.CreateCustomReminderAsync("Title", "Message", remindAt);
```

### Start/Stop Scheduler

```csharp
ReminderSchedulerService.Instance.Start();   // After auth
ReminderSchedulerService.Instance.Stop();    // On exit
```

### Check Settings

```csharp
var settings = ReminderSchedulerService.Instance.Settings;
if (settings.EnableReminders && settings.ShowMeetingReminders) { ... }
```
