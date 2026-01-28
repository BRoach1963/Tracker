# 06 – Services Reference

This document describes all **Services** in ProCohere.Avalonia.

---

## Overview

Services follow the **Singleton pattern** and wrap Supabase client operations:
- Access via `ServiceName.Instance`
- All use `AuthService.Instance.GetProCohereClient()` for data queries
- All have `LastError` property for error tracking
- All write to log files in `%LocalAppData%\ProCohere\`

---

## Service Index

| Service | File | Lines | Purpose |
|---------|------|-------|---------|
| `AppDialogService` | AppDialogService.cs | ~230 | Centralized dialog management (static) |
| `AuthService` | AuthService.cs | ~1068 | Authentication, session management |
| `GoalsService` | GoalsService.cs | ~783 | Goal CRUD, health/lifecycle |
| `MetricsService` | MetricsService.cs | ~764 | Metric CRUD, data points, trends |
| `TaskService` | TaskService.cs | ~560 | Task CRUD, completion |
| `TeamService` | TeamService.cs | ~285 | Team members, hierarchy |
| `MeetingService` | MeetingService.cs | ~626 | Meeting CRUD, attendees |
| `NotesService` | NotesService.cs | ~705 | Note CRUD, search, linking |
| `ProjectService` | ProjectService.cs | ~430 | Project CRUD, members, links |
| `MeetingAgendaItemService` | MeetingAgendaItemService.cs | - | Agenda items CRUD |
| `MeetingPrepItemService` | MeetingPrepItemService.cs | - | Prep items CRUD |
| `MeetingNoteService` | MeetingNoteService.cs | - | Meeting notes CRUD |
| `MeetingTemplateService` | MeetingTemplateService.cs | - | Meeting templates |
| `AgendaItemOutcomeService` | AgendaItemOutcomeService.cs | - | Agenda outcomes |
| `CarryForwardService` | CarryForwardService.cs | - | Carry forward logic |
| `DashboardService` | DashboardService.cs | - | Dashboard data |
| `ThemeService` | ThemeService.cs | ~108 | Light/Dark theme switching |
| `LocalSettingsService` | LocalSettingsService.cs | ~178 | Local app settings |
| `WindowsCredentialService` | WindowsCredentialService.cs | ~160 | DPAPI session storage |
| `DialogService` | DialogService.cs | ~90 | Internal dialog service for EditMeetingDialog |
| `IDialogService` | IDialogService.cs | ~111 | Interface for internal dialog operations |
| `NotificationService` | NotificationService.cs | ~300 | Toast notifications (in-app & native) |
| `ReminderDataService` | ReminderDataService.cs | ~652 | Reminder CRUD operations |
| `ReminderSchedulerService` | ReminderSchedulerService.cs | ~420 | Background reminder scheduler |
| `ToastActivationHandler` | ToastActivationHandler.cs | ~180 | Native toast button handlers (static) |
| `ConfirmationService` | ConfirmationService.cs | - | Confirmation dialogs |
| `SystemTrayService` | SystemTrayService.cs | - | System tray icon management |

> **Note:** For detailed Notifications & Reminders documentation, see [13-notifications-reminders-reference](13-notifications-reminders-reference.md).

---

## Common Pattern

All data services follow this structure:

```csharp
public class SomeService
{
    #region Singleton
    private static readonly Lazy<SomeService> _instance =
        new(() => new SomeService(), LazyThreadSafetyMode.ExecutionAndPublication);
    public static SomeService Instance => _instance.Value;
    #endregion

    #region Logging
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "service_name.log");

    private static void Log(string message) { /* ... */ }
    #endregion

    public string? LastError { get; private set; }

    private SomeService() { }

    // CRUD methods...
}
```

---

## AppDialogService

**Purpose**: Centralized dialog management for the application.

This is a **static service** (not a singleton) that encapsulates all dialog creation and display logic, eliminating code duplication across views. ViewModels raise events, Views subscribe and call this service with the parent Window reference.

### Design Pattern

```
ViewModel                    View                         AppDialogService
    |                         |                                 |
    |-- CreateMeetingCommand -|                                 |
    |   (raises event)        |                                 |
    |                         |-- subscribes to event --------->|
    |                         |                                 |
    |                         |-- ShowCreateMeetingAsync(window, preSelected)
    |                         |                                 |
    |                         |<-- MeetingDialogResult ---------|
    |                         |                                 |
    |<-- OnMeetingSaved(mtg) -|                                 |
```

### Usage Pattern

**In ViewModel:**
```csharp
// Define event
public event EventHandler<TeamMemberDetail?>? CreateMeetingDialogRequested;

// Raise event from command
[RelayCommand]
private void ScheduleMeeting(TeamMemberDetail? member)
{
    CreateMeetingDialogRequested?.Invoke(this, member);
}

// Define callback for result handling
public void OnMeetingSaved(MeetingDetail meeting) { /* update collections */ }
```

**In View code-behind:**
```csharp
// Subscribe in constructor
_viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;

// Handle event
private async void OnCreateMeetingDialogRequested(object? sender, TeamMemberDetail? preSelected)
{
    var window = TopLevel.GetTopLevel(this) as Window;
    if (window == null) return;

    var result = await AppDialogService.ShowCreateMeetingAsync(window, preSelected);
    
    if (result.Success && result.Meeting != null)
    {
        _viewModel.OnMeetingSaved(result.Meeting);
    }
}
```

### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ShowCreateMeetingAsync(window, preSelectedAttendee?)` | Task\<MeetingDialogResult\> | Show create meeting dialog |
| `ShowEditMeetingAsync(window, meeting)` | Task\<MeetingDialogResult\> | Show edit meeting dialog |

### MeetingDialogResult

| Property | Type | Description |
|----------|------|-------------|
| `Meeting` | MeetingDetail? | Created/updated meeting (null if cancelled/deleted) |
| `DeletedMeetingId` | Guid? | ID of deleted meeting |
| `Error` | string? | Error message if operation failed |
| `WasCancelled` | bool | True if user cancelled |
| `WasCreated` | bool | True if meeting was created |
| `WasUpdated` | bool | True if meeting was updated |
| `WasDeleted` | bool | True if meeting was deleted |
| `Success` | bool | True if operation succeeded (created/updated/deleted) |

### Factory Methods

```csharp
MeetingDialogResult.Created(meeting)   // Meeting was created
MeetingDialogResult.Updated(meeting)   // Meeting was updated
MeetingDialogResult.Deleted(meetingId) // Meeting was deleted
MeetingDialogResult.Cancelled()        // User cancelled
MeetingDialogResult.Failed(error)      // Operation failed
```

### Views Using AppDialogService

| View | Events Subscribed |
|------|-------------------|
| `MeView` | `CreateMeetingDialogRequested`, `EditMeetingDialogRequested` |
| `CircleView` | `CreateMeetingDialogRequested` (with pre-selection) |
| `BriefingView` | `CreateMeetingDialogRequested` |

### Future Extensions

The service is designed to be extended with additional dialog types:

```csharp
// Goals (future)
Task<GoalDialogResult> ShowCreateGoalAsync(Window parent, TeamMemberDetail? owner = null);
Task<GoalDialogResult> ShowEditGoalAsync(Window parent, GoalDetail goal);

// Tasks (future)
Task<TaskDialogResult> ShowCreateTaskAsync(Window parent, MeetingDetail? relatedMeeting = null);
Task<TaskDialogResult> ShowEditTaskAsync(Window parent, TaskDetail task);
```

---

## AuthService

**Purpose**: Authentication and user session management.

See [04-authentication-flow.md](04-authentication-flow.md) for detailed documentation.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `InitializeAsync()` | Task | Initialize Supabase clients |
| `TryAutoLoginAsync()` | Task\<bool\> | Attempt auto-login from stored session |
| `SignInAsync(email, password, persist)` | (bool, string?) | Sign in with credentials |
| `SignUpAsync(email, password, displayName)` | (bool, string?) | Create new account |
| `SignOutAsync()` | Task | Sign out, clear credentials |
| `LoadUserProfileAsync()` | Task\<UserProfile?\> | Load from public.users |
| `UpdateUserProfileAsync(...)` | (bool, string?) | Update profile fields |
| `UploadAvatarAsync(filePath)` | (bool, string?, string?) | Upload avatar image |
| `GetUserSessionAsync(productKey)` | Task\<ProCohereUserSessionDto\> | Get full session with team/role |
| `HasProductAccessAsync(productCode)` | Task\<bool\> | Check license access |

---

## GoalsService

**Purpose**: Goal management.

### Philosophy
> "Goals express intent, Metrics observe reality, Humans decide."
> NO automatic goal creation or updates.
> Health and lifecycle changes require explicit user reflection.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetMyGoalsAsync()` | Task\<List\<GoalDetail\>\> | Goals where I am owner |
| `GetTeamGoalsAsync()` | Task\<List\<GoalDetail\>\> | Goals for visible team members |
| `GetSharedGoalsAsync()` | Task\<List\<GoalDetail\>\> | Goals shared with me |
| `GetGoalByIdAsync(id)` | Task\<GoalDetail?\> | Single goal with details |
| `CreateGoalAsync(goal)` | Task\<GoalDetail?\> | Create new goal |
| `UpdateGoalAsync(goal)` | Task\<bool\> | Update goal fields |
| `UpdateHealthAsync(goalId, health, reason)` | Task\<bool\> | Change health with reason |
| `UpdateLifecycleAsync(goalId, lifecycle, reason)` | Task\<bool\> | Change lifecycle with reason |
| `DeleteGoalAsync(goalId)` | Task\<bool\> | Soft delete |

### Health Values
```csharp
public enum GoalHealth
{
    OnTrack,      // Everything good
    AtRisk,       // Needs attention
    Blocked,      // Can't progress
    Undefined     // Not yet assessed
}
```

### Lifecycle Values
```csharp
public enum GoalLifecycle
{
    Draft,        // Not started
    Active,       // In progress
    Paused,       // Temporarily stopped
    Achieved,     // Successfully completed
    Abandoned     // No longer pursuing
}
```

---

## MetricsService

**Purpose**: Metric management.

### Philosophy
> "Metrics are signals that tell a story, NOT targets to chase."
> Display DIRECTIONAL TRENDS (↗ → ↘), not numeric values.
> Metrics inform but never determine goal health.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAllMetricsAsync()` | Task\<List\<MetricDetail\>\> | All metrics (RLS filtered) |
| `GetMetricsByOwnerAsync(ownerId)` | Task\<List\<MetricDetail\>\> | Metrics for specific owner |
| `GetMetricsByScopeAsync(scope)` | Task\<List\<MetricDetail\>\> | Filter by scope |
| `GetMetricByIdAsync(id)` | Task\<MetricDetail?\> | Single metric |
| `CreateMetricAsync(metric)` | Task\<MetricDetail?\> | Create new metric |
| `UpdateMetricAsync(metric)` | Task\<bool\> | Update metric fields |
| `RecordDataPointAsync(metricId, value, notes)` | Task\<bool\> | Add data point |
| `GetDataPointsAsync(metricId, limit)` | Task\<List\<MetricDataPoint\>\> | Historical data |
| `CalculateTrendAsync(metricId)` | Task\<MetricTrend\> | Calculate trend |
| `DeleteMetricAsync(metricId)` | Task\<bool\> | Soft delete |

### Trend Values
```csharp
public enum MetricTrend
{
    Improving,    // ↗ Getting better
    Stable,       // → No change
    Declining,    // ↘ Getting worse
    Insufficient  // Not enough data
}
```

---

## TaskService

**Purpose**: Task management.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetTaskAsync(taskId)` | Task\<TaskDetail?\> | Single task |
| `GetTasksAsync(includeCompleted)` | Task\<List\<TaskDetail\>\> | All tasks |
| `GetTasksForOwnerAsync(ownerId)` | Task\<List\<TaskDetail\>\> | Tasks for specific owner |
| `GetOverdueTasksAsync()` | Task\<List\<TaskDetail\>\> | Overdue tasks |
| `CreateTaskAsync(task)` | Task\<TaskDetail?\> | Create new task |
| `UpdateTaskAsync(task)` | Task\<bool\> | Update task fields |
| `CompleteTaskAsync(taskId)` | Task\<bool\> | Mark as completed |
| `UncompleteTaskAsync(taskId)` | Task\<bool\> | Mark as not completed |
| `DeleteTaskAsync(taskId)` | Task\<bool\> | Soft delete |

### Task Statuses
```csharp
public static readonly string[] ValidStatuses =
{
    "pending",
    "in_progress",
    "completed",
    "cancelled"
};
```

---

## TeamService

**Purpose**: Team member management with hierarchy awareness.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetVisibleTeamMembersAsync(forceRefresh)` | Task\<List\<TeamMemberDetail\>\> | Members visible to current user |
| `GetTeamMemberAsync(id)` | Task\<TeamMemberDetail?\> | Single team member |
| `GetDirectReportsAsync(managerId)` | Task\<List\<TeamMemberDetail\>\> | Direct reports of manager |
| `ClearCache()` | void | Clear cached members |

### Visibility Rules
Uses RPC `get_visible_team_member_ids()`:
- **Admin**: Sees everyone
- **Manager**: Sees self + direct/indirect reports
- **User**: Sees self + manager chain

### Caching
```csharp
private List<TeamMemberDetail>? _cachedMembers;
private DateTime _cacheExpiry = DateTime.MinValue;
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
```

---

## MeetingService

**Purpose**: Meeting and attendee management.

### Critical Rule
> When creating a meeting, the creator MUST be inserted as an attendee
> with role='organizer' immediately after, or RLS will prevent them from seeing it.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetMeetingAsync(meetingId)` | Task\<MeetingDetail?\> | Single meeting with attendees |
| `GetMeetingsAsync(includeCompleted)` | Task\<List\<MeetingDetail\>\> | All meetings |
| `GetUpcomingMeetingsAsync(days)` | Task\<List\<MeetingDetail\>\> | Meetings in next N days |
| `CreateMeetingAsync(meeting, attendeeIds)` | Task\<MeetingDetail?\> | Create with attendees |
| `UpdateMeetingAsync(meeting)` | Task\<bool\> | Update meeting fields |
| `AddAttendeeAsync(meetingId, memberId, role)` | Task\<bool\> | Add attendee |
| `RemoveAttendeeAsync(meetingId, memberId)` | Task\<bool\> | Remove attendee |
| `DeleteMeetingAsync(meetingId)` | Task\<bool\> | Soft delete |

### Meeting Types
```csharp
public static readonly string[] ValidMeetingTypes =
{
    "one_on_one",
    "team",
    "project",
    "standup",
    "retrospective",
    "planning",
    "review",
    "other"
};
```

### Attendee Roles
```csharp
public static readonly string[] ValidAttendeeRoles =
{
    "organizer",
    "attendee",
    "optional"
};
```

---

## NotesService

**Purpose**: Note management with entity linking.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAllNotesAsync()` | Task\<List\<Note\>\> | All notes (excluding archived) |
| `GetNoteByIdAsync(id)` | Task\<Note?\> | Single note |
| `GetNotesByEntityAsync(entityType, entityId)` | Task\<List\<Note\>\> | Notes linked to entity |
| `SearchNotesAsync(query)` | Task\<List\<Note\>\> | Full-text search |
| `CreateNoteAsync(note)` | Task\<Note?\> | Create new note |
| `UpdateNoteAsync(note)` | Task\<bool\> | Update note |
| `ArchiveNoteAsync(noteId)` | Task\<bool\> | Archive (soft hide) |
| `PinNoteAsync(noteId, isPinned)` | Task\<bool\> | Toggle pinned |
| `DeleteNoteAsync(noteId)` | Task\<bool\> | Soft delete |
| `LinkNoteToEntityAsync(noteId, entityType, entityId)` | Task\<bool\> | Create link |
| `UnlinkNoteFromEntityAsync(noteId, entityType, entityId)` | Task\<bool\> | Remove link |

---

## ProjectService

**Purpose**: Project CRUD with members and entity links via RPCs.

### Key Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAllProjectsAsync()` | Task\<List\<Project\>\> | All visible projects |
| `GetProjectByIdAsync(id)` | Task\<Project?\> | Single project with members/links |
| `CreateProjectAsync(name, description, status, dueDate)` | Task\<Project?\> | Create via RPC |
| `UpdateProjectAsync(id, name, description, status, dueDate)` | Task\<bool\> | Update via RPC |
| `DeleteProjectAsync(id)` | Task\<bool\> | Soft delete via RPC |
| `AddProjectMemberAsync(projectId, teamMemberId, role)` | Task\<bool\> | Add member via RPC |
| `RemoveProjectMemberAsync(projectMemberId)` | Task\<bool\> | Remove member via RPC |
| `AddProjectLinkAsync(projectId, entityType, entityId, titleSnapshot)` | Task\<bool\> | Link entity via RPC |
| `RemoveProjectLinkAsync(projectLinkId)` | Task\<bool\> | Unlink entity via RPC |

### RPC Functions Used

| RPC | Purpose |
|-----|---------|
| `rpc_create_project` | Create with proper RLS context |
| `rpc_update_project` | Update with ownership check |
| `rpc_delete_project` | Soft delete |
| `rpc_add_project_member` | Add membership |
| `rpc_remove_project_member` | Remove membership |
| `rpc_add_project_link` | Create polymorphic link |
| `rpc_remove_project_link` | Remove link |

### Example Usage

```csharp
// Create a project
var project = await ProjectService.Instance.CreateProjectAsync(
    "Q1 Initiatives",
    "Key projects for Q1",
    ProjectStatus.Active,
    new DateTime(2026, 3, 31)
);

// Add a member
await ProjectService.Instance.AddProjectMemberAsync(
    project.Id,
    teamMemberId,
    ProjectMemberRole.Lead
);

// Link a goal
await ProjectService.Instance.AddProjectLinkAsync(
    project.Id,
    ProjectLinkEntityType.Goal,
    goalId,
    "Increase customer retention"
);
```

---

## ThemeService

**Purpose**: Light/Dark theme management.

### Properties
```csharp
public bool IsDarkTheme { get; set; }  // Get/set current theme
```

### Events
```csharp
public event Action<bool>? ThemeChanged;  // Fired on theme change
```

### Methods
```csharp
public void ApplyTheme(bool isDark)  // Apply to Avalonia RequestedThemeVariant
```

### Implementation
```csharp
public void ApplyTheme(bool isDark)
{
    if (Application.Current != null)
    {
        Application.Current.RequestedThemeVariant = 
            isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
```

---

## LocalSettingsService

**Purpose**: Local app settings storage.

### File Location
```
%LocalAppData%\ProCohere\settings.json
```

### Properties
```csharp
public bool IsDarkTheme { get; set; }
public string? RememberedEmail { get; set; }
public bool RememberEmail { get; set; }
```

### Settings Class
```csharp
private class LocalSettings
{
    public bool IsDarkTheme { get; set; } = false;
    public string? RememberedEmail { get; set; }
    public bool RememberEmail { get; set; } = true;
}
```

---

## Meeting Sub-Services

### MeetingAgendaItemService

Uses `procohere.` schema RPC functions for all CRUD operations.

**RPCs Used:**
- `insert_meeting_agenda_item` - Create with optional entity link (also writes to link table)
- `update_meeting_agenda_item` - Update item fields + handles linking internally
- `delete_meeting_agenda_item` - Soft delete
- `upsert_meeting_agenda_item_reference_link` - Create/update the reference link (called by update RPC)
- `delete_meeting_agenda_item_reference_link` - Remove the reference link (called by update RPC)

**Read View:**
- `v_meeting_agenda_items_with_links` - Agenda items with flattened reference link + `links_json`

**Methods:**
| Method | RPC/View | Description |
|--------|----------|-------------|
| `GetAgendaItemsAsync(meetingId)` | `v_meeting_agenda_items_with_links` | Get agenda items with links |
| `CreateAgendaItemAsync(item)` | `insert_meeting_agenda_item` | Create new item |
| `UpdateAgendaItemAsync(item)` | `update_meeting_agenda_item` | Update item (handles links internally) |
| `DeleteAgendaItemAsync(itemId)` | `delete_meeting_agenda_item` | Soft delete |
| `LinkToEntityAsync(...)` | `upsert_meeting_agenda_item_reference_link` | Link to entity |
| `UnlinkEntityAsync(...)` | `delete_meeting_agenda_item_reference_link` | Remove reference link |
| `CreateTaskFromAgendaItemAsync(...)` | Task create + `upsert_meeting_agenda_item_reference_link` | Create task and link it |
| `ReorderAgendaItemsAsync(meetingId, orderedIds)` | Multiple updates | Change order |

**Link Management (Reference Link Only):**

The link table is constrained to one `'reference'` link per agenda item. The `update_meeting_agenda_item` RPC handles linking/unlinking internally:
- If `p_linked_entity_type` AND `p_linked_entity_id` are provided → upsert link
- If both are NULL → unlink
- If only one is provided → throws exception

```csharp
// Link via update RPC (recommended - atomic)
await UpdateAgendaItemAsync(item); // If item has linked entity fields set

// Or call link RPC directly
await LinkToEntityAsync(agendaItemId, "task", taskId, taskTitle);

// Unlink
await UnlinkEntityAsync(agendaItemId);
```

**Allowed Entity Types:** 
- Defined in `procohere.allowed_entity_types` lookup table
- Currently: `task`, `goal`, `metric`, `project`
- RPC validates `is_active = true` before allowing links
- Add new types via INSERT (no schema change required)

**Entity Type Picklist:**
```csharp
// Fetch allowed types for UI
var types = await client.From<AllowedEntityType>()
    .Filter("is_active", Operator.Equals, true)
    .Order("sort_order", Ordering.Ascending)
    .Get();
```

### MeetingPrepItemService

Uses role-based RPCs for updates (requester vs assignee).

**RPCs Used:**
- `insert_meeting_prep_item` - Create (supports linked entity at insert time)
- `update_meeting_prep_item_as_requester` - Update by requester (title, body, assignment, etc.)
- `update_meeting_prep_item_as_assignee` - Update by assignee (notes, response, status)
- `delete_meeting_prep_item` - Soft delete
- `insert_meeting_prep_item_link` - Add entity link

**Methods:**
| Method | RPC | Description |
|--------|-----|-------------|
| `GetPrepItemsForMeetingAsync(meetingId)` | SELECT | Get prep items visible to user |
| `CreatePrepItemAsync(item)` | `insert_meeting_prep_item` | Create new prep item |
| `UpdatePrepItemAsync(item)` | `update_meeting_prep_item_as_*` | Update (dispatches by role) |
| `UpdateStatusAsync(prepItemId, status)` | `update_meeting_prep_item_as_*` | Update status |
| `DeletePrepItemAsync(prepItemId)` | `delete_meeting_prep_item` | Soft delete |
| `CreateQuickPrepAsync(meetingId, title)` | `insert_meeting_prep_item` | Personal prep item |
| `CreateAssignedPrepAsync(...)` | `insert_meeting_prep_item` | Assigned prep |
| `CreateTeamPrepAsync(meetingId, title, body?)` | `insert_meeting_prep_item` | Team/meeting-scoped prep |
| `CreateLinkedPrepAsync(...)` | `insert_meeting_prep_item` | Linked entity prep |
| `CapturePrepResponseAsync(prepItemId, response)` | `update_meeting_prep_item_as_assignee` | Capture prep response |
| `UpdatePrepPromptAsync(prepItemId, prompt)` | `update_meeting_prep_item_as_requester` | Update prep prompt |
| `LinkEntityAsync(prepItemId, ...)` | `insert_meeting_prep_item_link` | Link entity |
| `UnlinkEntityAsync(prepItemId)` | Direct table delete | Remove linked entity |
| `GetPrepItemsForEntityAsync(entityType, entityId)` | SELECT | Get prep items for entity |
| `CarryForwardPrepItemsAsync(...)` | `insert_meeting_prep_item` | Carry forward incomplete items |

**Note:** Linked entity fields are only supported at insert time. To update links, delete and recreate the link.

### MeetingNoteService

**RPCs Used:**
- `insert_meeting_note(p_meeting_id, p_content, p_is_shared, p_tags)` - Create note with tags
- `update_meeting_note(p_id, p_content, p_is_shared, p_tags)` - Update note and tags
- `delete_meeting_note(p_id)` - Soft delete

**Important:** Uses `p_is_shared` (NOT `p_is_private`). Invert the app's `isPrivate` flag:
```csharp
new KeyValuePair<string, object>("p_is_shared", !isPrivate)
```

**Methods:**
| Method | RPC | Description |
|--------|-----|-------------|
| `GetMeetingNotesAsync(meetingId)` | SELECT | Get notes for meeting (includes tags) |
| `CreateNoteAsync(meetingId, content, isPrivate)` | `insert_meeting_note` | Create new note |
| `UpdateNoteAsync(MeetingNote)` | `update_meeting_note` | Update note (content, visibility, tags) |
| `DeleteNoteAsync(noteId)` | `delete_meeting_note` | Soft delete |
| `CreateQuickNoteAsync(meetingId, content)` | `insert_meeting_note` | Create private note |
| `CreateSharedNoteAsync(meetingId, content)` | `insert_meeting_note` | Create shared note |

**Tags Parameter:**
Tags are passed as a JSONB array of category strings:
```csharp
p_tags = note.Tags ?? []  // e.g., ["action", "decision"]
```

### MeetingTemplateService
- `GetTemplatesAsync()` - Get all templates
- `GetTemplateAsync(id)` - Get single template
- `CreateTemplateAsync(template)` - Create new template
- `ApplyTemplateToMeetingAsync(meetingId, templateId)` - Apply template

### AgendaItemOutcomeService
- `GetOutcomesAsync(agendaItemId)` - Get outcomes for item
- `CreateOutcomeAsync(outcome)` - Create new outcome
- `LinkOutcomeToTaskAsync(outcomeId, taskId)` - Link to task

### CarryForwardService
- `GetCarryForwardItemsAsync(meetingId)` - Items to carry forward
- `CarryForwardToMeetingAsync(items, targetMeetingId)` - Execute carry forward

---

## Log Files

All services log to `%LocalAppData%\ProCohere\`:

| Service | Log File |
|---------|----------|
| AuthService | auth.log |
| GoalsService | goals_service.log |
| MetricsService | metrics_service.log |
| TaskService | task_service.log |
| TeamService | team.log |
| MeetingService | meeting_service.log |
| NotesService | notes_service.log |

---

## Invariants

1. **All services are singletons** - access via `.Instance`
2. **All queries go through procohere client** - `GetProCohereClient()`
3. **RLS is enforced** - user only sees permitted data
4. **LastError captures failures** - check after operations
5. **Soft delete only** - set `is_deleted = true`
6. **Logging enabled** - all operations logged locally

