# 08 – Enums Reference

This document lists **all enums** defined in Tracker.Core.

Location: `Common/Enums/`

---

## Meeting Enums

### MeetingType
**File:** `MeetingType.cs`

```csharp
public enum MeetingType
{
    OneOnOne = 0,      // 1:1 meeting
    TeamMeeting = 1,   // Team meeting
    AllHands = 2,      // All-hands
    ProjectKickoff = 3,
    Review = 4,
    Planning = 5,
    Other = 6,
    Project = 7,       // Project meeting
    Interview = 8
}
```

**Database mapping:** `meeting_type` enum → `one_on_one`, `team_meeting`, `all_hands`, `project`, `interview`, `other`

### MeetingStatus / MeetingStatusEnum
**File:** `MeetingStatus.cs`, `MeetingStatusEnum.cs`

```csharp
public enum MeetingStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
```

---

## Task Enums

### WorkItemStatus
**File:** `TaskEnums.cs`

```csharp
public enum WorkItemStatus
{
    NotStarted,
    InProgress,
    Blocked,
    Completed,
    Cancelled
}
```

### WorkItemPriority
**File:** `TaskEnums.cs`

```csharp
public enum WorkItemPriority
{
    Low,
    Medium,
    High,
    Critical
}
```

### TaskTypeEnum
**File:** `TaskTypeEnum.cs`

```csharp
public enum TaskType
{
    Standalone,
    ProjectTask,
    GoalTask,
    MeetingActionItem
}
```

---

## Goal Enums

### GoalStatus
**File:** `GoalStatusEnum.cs`

```csharp
public enum GoalStatus
{
    NotStarted,
    OnTrack,
    AtRisk,
    OffTrack,
    Completed,
    Cancelled
}
```

### GoalCategory
**File:** `GoalCategory.cs`

Categories for organizing goals.

---

## Metric Enums

### MetricStatus
**File:** `MetricStatus.cs`

```csharp
public enum MetricStatus
{
    Active,
    Archived,
    Paused
}
```

### AggregationTypeEnum
**File:** `AggregationTypeEnum.cs`

```csharp
public enum AggregationType
{
    Sum,
    Average,
    Min,
    Max,
    Count,
    Latest
}
```

### TargetDirectionEnum
**File:** `TargetDirectionEnum.cs`

```csharp
public enum TargetDirection
{
    Increase,
    Decrease,
    Maintain
}
```

### TimePeriodEnum
**File:** `TimePeriodEnum.cs`

```csharp
public enum TimePeriod
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly
}
```

---

## Feedback & Recognition

### FeedbackType
**File:** `FeedbackType.cs`

```csharp
public enum FeedbackType
{
    Praise,
    Constructive,
    Coaching,
    Performance,
    Development
}
```

### KudosEnums
**File:** `KudosEnums.cs`

Kudos categories and types.

---

## Team & Organization

### RoleEnum
**File:** `RoleEnum.cs`

```csharp
public enum Role
{
    Admin,
    Manager,
    Member,
    Guest
}
```

### EmploymentStatus
**File:** `EmploymentStatus.cs`

```csharp
public enum EmploymentStatus
{
    Active,
    OnLeave,
    Terminated,
    Contractor
}
```

### SubscriptionTier
**File:** `SubscriptionTier.cs`

```csharp
public enum SubscriptionTier
{
    Free,
    Starter,
    Professional,
    Enterprise
}
```

---

## UI Enums

### ThemeEnum
**File:** `ThemeEnum.cs`

```csharp
public enum Theme
{
    System,
    Light,
    Dark
}
```

### DialogType
**File:** `DialogType.cs`

Types of dialogs in the application.

### ToastType
**File:** `ToastType.cs`

```csharp
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
```

### ToastNotificationAction
**File:** `ToastNotificationAction.cs`

Actions that can trigger toast notifications.

### ToolBarStyleEnum
**File:** `ToolBarStyleEnum.cs`

Toolbar display options.

---

## Notes & Reminders

### NoteCategory
**File:** `NoteCategory.cs`

Categories for quick notes.

### NoteLinkedEntityType
**File:** `NoteLinkedEntityType.cs`

```csharp
public enum NoteLinkedEntityType
{
    TeamMember,
    Meeting,
    Goal,
    Task,
    Project
}
```

### ReminderType
**File:** `ReminderType.cs`

```csharp
public enum ReminderType
{
    OneTime,
    Recurring
}
```

### ReminderStatus
**File:** `ReminderStatus.cs`

```csharp
public enum ReminderStatus
{
    Pending,
    Completed,
    Dismissed,
    Snoozed
}
```

---

## Meeting Prep & Agenda

### MeetingPrepEnums
**File:** `MeetingPrepEnums.cs`

```csharp
public enum PrepItemVisibility
{
    Personal,   // Only creator sees
    Assigned,   // Creator + assignee
    Meeting     // All attendees
}

public enum PrepItemStatus
{
    NotStarted,
    InProgress,
    Completed
}
```

### AgendaItemCategory
**File:** `AgendaItemCategory.cs`

Categories for agenda items.

### LinkedItemType
**File:** `LinkedItemType.cs`

```csharp
public enum LinkedItemType
{
    Task,
    Goal,
    Metric,
    Project,
    TeamMember
}
```

---

## Insights & AI

### InsightType
**File:** `InsightType.cs`

```csharp
public enum InsightType
{
    Performance,
    Coaching,
    Recognition,
    Risk,
    Opportunity
}
```

### InsightSeverity
**File:** `InsightSeverity.cs`

```csharp
public enum InsightSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}
```

---

## Calendar & Sync

### SyncStatus
**File:** `SyncStatus.cs`

```csharp
public enum SyncStatus
{
    NotSynced,
    Syncing,
    Synced,
    Error
}
```

### VideoConferenceProvider
**File:** `VideoConferenceProvider.cs`

```csharp
public enum VideoConferenceProvider
{
    None,
    Teams,
    Zoom,
    GoogleMeet,
    Webex
}
```

### AttendeeResponse
**File:** `AttendeeResponse.cs`

```csharp
public enum AttendeeResponse
{
    Pending,
    Accepted,
    Declined,
    Tentative
}
```

---

## Surveys

### SurveyEnums
**File:** `SurveyEnums.cs`

Survey status, question types, response types.

---

## Skills & Development

### SkillLevelEnum
**File:** `SkillLevelEnum.cs`

```csharp
public enum SkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}
```

### SkillSetEnum
**File:** `SkillSetEnum.cs`

Skill categories/areas.

---

## Project & Risk

### Impact
**File:** `Impact.cs`

```csharp
public enum Impact
{
    Low,
    Medium,
    High,
    Critical
}
```

### Severity
**File:** `Severity.cs`

```csharp
public enum Severity
{
    Minor,
    Moderate,
    Major,
    Critical
}
```

### RiskLevelEnum
**File:** `RiskLevelEnum.cs`

```csharp
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

### ResolutionStatus
**File:** `ResolutionStatus.cs`

Status for issues/risks.

---

## Miscellaneous

### LogLevel
**File:** `LogLevel.cs`

```csharp
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}
```

### PropertyChangedEnum
**File:** `PropertyChangedEnum.cs`

For change tracking.

### OwnerType
**File:** `OwnerType.cs`

```csharp
public enum OwnerType
{
    User,
    Team,
    Organization
}
```

### TeamMemberFilterEnum
**File:** `TeamMemberFilterEnum.cs`

Filter options for team member lists.

---

## Enum to String Mapping

PostgreSQL stores enums as strings. Use this pattern in models:

```csharp
// Storage property (database column)
[Column("status")]
public string StatusString { get; set; } = "not_started";

// Convenience property (application use)
[NotMapped]
public GoalStatus Status
{
    get => StatusString switch
    {
        "not_started" => GoalStatus.NotStarted,
        "on_track" => GoalStatus.OnTrack,
        "at_risk" => GoalStatus.AtRisk,
        "off_track" => GoalStatus.OffTrack,
        "completed" => GoalStatus.Completed,
        _ => GoalStatus.NotStarted
    };
    set => StatusString = value switch
    {
        GoalStatus.NotStarted => "not_started",
        GoalStatus.OnTrack => "on_track",
        GoalStatus.AtRisk => "at_risk",
        GoalStatus.OffTrack => "off_track",
        GoalStatus.Completed => "completed",
        _ => "not_started"
    };
}
```

---

## File List

All enum files in `Common/Enums/`:

| File | Enums |
|------|-------|
| AgendaItemCategory.cs | AgendaItemCategory |
| AggregationTypeEnum.cs | AggregationType |
| AttendeeResponse.cs | AttendeeResponse |
| DialogType.cs | DialogType |
| EmploymentStatus.cs | EmploymentStatus |
| FeedbackType.cs | FeedbackType |
| GoalCategory.cs | GoalCategory |
| GoalStatusEnum.cs | GoalStatus |
| Impact.cs | Impact |
| InsightSeverity.cs | InsightSeverity |
| InsightType.cs | InsightType |
| KudosEnums.cs | Kudos enums |
| LinkedItemType.cs | LinkedItemType |
| LogLevel.cs | LogLevel |
| MeetingPrepEnums.cs | PrepItemVisibility, PrepItemStatus |
| MeetingStatus.cs | MeetingStatus |
| MeetingType.cs | MeetingType |
| MetricEnums.cs | Metric-related enums |
| MetricStatus.cs | MetricStatus |
| NoteCategory.cs | NoteCategory |
| NoteLinkedEntityType.cs | NoteLinkedEntityType |
| OwnerType.cs | OwnerType |
| ReminderStatus.cs | ReminderStatus |
| ReminderType.cs | ReminderType |
| ResolutionStatus.cs | ResolutionStatus |
| RiskLevelEnum.cs | RiskLevel |
| RoleEnum.cs | Role |
| Severity.cs | Severity |
| SkillLevelEnum.cs | SkillLevel |
| SkillSetEnum.cs | SkillSet |
| SubscriptionTier.cs | SubscriptionTier |
| SurveyEnums.cs | Survey-related enums |
| SyncStatus.cs | SyncStatus |
| TargetDirectionEnum.cs | TargetDirection |
| TaskEnums.cs | WorkItemStatus, WorkItemPriority |
| TeamMemberFilterEnum.cs | TeamMemberFilter |
| ThemeEnum.cs | Theme |
| TimePeriodEnum.cs | TimePeriod |
| ToastNotificationAction.cs | ToastNotificationAction |
| ToastType.cs | ToastType |
| ToolBarStyleEnum.cs | ToolBarStyle |
| VideoConferenceProvider.cs | VideoConferenceProvider |

