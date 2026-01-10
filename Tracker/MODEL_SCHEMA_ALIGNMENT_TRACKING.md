# Model-Schema Alignment Tracking Document

**Purpose:** Document all data model changes made to align with Supabase schema. Track breaking changes and code impact areas for systematic cleanup during Phase 2.

**Status:** In Progress - Models being validated and fixed one at a time

**Last Updated:** 2026-01-10

---

## 🗄️ SCHEMA CHANGES REQUIRED

This section tracks schema modifications that need to be applied to Supabase during Phase 2 migration. These are new columns/properties not currently in the schema but identified as necessary during model validation.

### Tasks Table
- ✅ `notes` (TEXT, nullable) - Added to match MeetingTask model; allows storing additional notes on any task type
  - **Status:** Property added to Task.cs (pending schema migration)
  - **Rationale:** MeetingTask had Notes field; unified Task model needs this capability
  - **Migration Type:** ALTER TABLE tasks ADD COLUMN notes TEXT NULL;

### Goals Table
- ✅ **NO CHANGES NEEDED** - Schema perfectly matches Goal.cs model

### Targets Table
- ✅ **NO CHANGES NEEDED** - Schema perfectly matches Target.cs model

### Metrics Table
- ✅ **NO CHANGES NEEDED** - Schema perfectly matches Metric.cs model

### Meetings Table
- ⏳ PENDING - Full validation needed (see section 4)

### Other Tables
- ⏳ PENDING - Will be identified during remaining model validations

**Note:** We'll create ALTER TABLE scripts for all schema changes once all model validations are complete. May require data reseeding depending on complexity.

---

## 📋 OPTIONAL COMPUTED PROPERTIES (Low Priority - May Add Later)

This section tracks computed/convenience properties that are NOT in the schema but may improve DX. These are optional enhancements and should be added ONLY if needed by views/services.

### Goal.cs - Optional Properties to Consider
- `MeetingCount` (int) - Count of 1:1 meetings where this goal was discussed (would need to join meetings/discussion table)
  - **Priority:** Low - Not critical, computed property
  - **Rationale:** Useful for UI display of goal engagement
  
- `LinkedKpiCount`, `LinkedProjectCount`, `LinkedTaskCollectionCount` (int) - Count linked items across all targets
  - **Priority:** Low - Can be recomputed from Targets.Measurables
  - **Rationale:** Useful for UI summaries
  
- `TimePeriodDisplay` (string) - Formatted display of time period (e.g., "Q1 2025")
  - **Priority:** Low - Nice to have for UI
  - **Rationale:** Formatting logic for display
  
- `KeyResultCount`, `HasKeyResults` (int, bool) - Count of targets
  - **Priority:** Low - Can recompute from Targets.Count
  - **Rationale:** Convenience properties for checking state
  
- `IsActive` (bool) - Whether goal is between start and end dates
  - **Priority:** Low - Can compute from StartDate/EndDate
  - **Rationale:** Business logic check

### Metric.cs - Optional Properties to Consider
- `HasDataSources` (bool) → `DataSources?.Count > 0`
  - **Priority:** Low - Simple convenience check
  - **Rationale:** Cleaner than checking collection count
  
- `HasChildMetrics` (bool) → `ChildMetrics?.Count > 0`
  - **Priority:** Low - Simple convenience check
  - **Rationale:** Cleaner than checking collection count

---

## 1. PROJECT.CS ✅ COMPLETE

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `projects` table exactly

### Changes Made
Simplified from complex interfaces and computed properties to pure schema match:
- ❌ REMOVED: `IMeasurable` interface implementation
- ❌ REMOVED: `IKpiSource` interface implementation
- ❌ REMOVED: `Budget` property (not in schema)
- ❌ REMOVED: `ProjectDependency` navigation (not in schema)
- ❌ REMOVED: `Risk` navigation (not in schema)
- ❌ REMOVED: `DisplayValue` property (computed)
- ❌ REMOVED: Computed properties: `TotalTasks`, `CompletedTasks`, `IncompleteTasks`, `IsOverdue`, `DaysRemaining`

### Schema Columns → Properties Mapping
| Schema Column | C# Property | Type | Notes |
|---|---|---|---|
| id | Id | Guid | UUID PK |
| organization_id | OrganizationId | Guid | Non-nullable FK |
| owner_team_member_id | OwnerTeamMemberId | Guid? | Nullable FK |
| created_by_user_id | CreatedByUserId | Guid | FK to users |
| name | Name | string | VARCHAR 300 |
| description | Description | string? | TEXT |
| color | Color | string? | VARCHAR 7 hex |
| start_date | StartDate | DateTime? | DATE |
| target_end_date | TargetEndDate | DateTime? | DATE |
| actual_end_date | ActualEndDate | DateTime? | DATE |
| status | Status | WorkItemStatus enum | task_status enum |
| progress_percent | ProgressPercent | decimal | DECIMAL 5,2 |
| priority | Priority | WorkItemPriority enum | task_priority enum |
| is_team_visible | IsTeamVisible | bool | BOOLEAN |

### Code Impact Areas
**High Impact - Breaking Changes:**
- All code calling `project.Budget` - REMOVED
- All code checking `project.DisplayValue` - REMOVED
- All code referencing `project.Progress` - CHANGED to `ProgressPercent`
- All code with `project.EndDate` - CHANGED to `TargetEndDate` or `ActualEndDate`
- All code with `project.ID` (capitalized) - CHANGED to `Id`
- All code implementing IMeasurable on Project - BROKEN
- All code implementing IKpiSource on Project - BROKEN

**Files Likely Affected:**
- ReportsViewModel (EndDate references)
- NewProjectViewModel (EndDate references)
- ValidationHelper (EndDate validation)
- MeasurableService (DisplayValue)
- KpiCalculationService (ID references)
- ExcelExportService (ID, EndDate, Progress)
- PredictiveAnalyticsService (EndDate, Progress)
- SearchService (ID)
- HelpBotContextService
- ProgressSnapshotService (ID, Progress)
- GoalIndexer (ID, Progress)
- TrackerMainViewModel (Status enum type conversions)

---

## 2. MILESTONE.CS ✅ COMPLETE

### Schema Match Status
**Status:** ✅ COMPLETE - No changes needed, already matches `milestones` table

### Changes Made
NONE - Model was already correct.

### Schema Columns → Properties Mapping
| Schema Column | C# Property | Type | Notes |
|---|---|---|---|
| id | Id | Guid | UUID PK |
| project_id | ProjectId | Guid | FK to projects |
| title | Title | string | VARCHAR 200 |
| description | Description | string? | TEXT |
| target_date | TargetDate | DateTime | DATE NOT NULL |
| completed_date | CompletedDate | DateTime? | DATE |
| is_completed | IsCompleted | bool | BOOLEAN |
| sort_order | SortOrder | int | INTEGER |

### Code Impact Areas
**Status:** ✅ No breaking changes

---

## 3. TASK (formerly TrackerTask) ⚠️ UNIFIED - MAJOR BREAKING CHANGES

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `tasks` table exactly

### Models Consolidated
**BREAKING:** Three separate models consolidated into ONE:
- ❌ `IndividualTask` class - DELETED
- ❌ `MeetingTask` class - DELETED
- ✅ `TrackerTask` renamed to `Task`

### Changes Made
Complete rebuild to match schema + new architecture:

**REMOVED from IndividualTask:**
- ❌ `Notes` property (not in schema)
- ❌ Computed `Status` property (now enum column)
- ❌ Computed `OwnerName` property
- ❌ ITask interface implementation
- ❌ Old computed properties (MeetingCount, IsOverdue, DaysUntilDue, HasSubtasks, SubtaskProgress)

**REMOVED from MeetingTask:**
- ❌ Entire class DELETED - now just Task with MeetingId FK
- ❌ OneOnOneId → MeetingId (FK type standardized to Guid)

**ADDED to Task:**
- ✅ `Title` property (VARCHAR 300) - was missing entirely
- ✅ `WorkItemStatus Status` enum (instead of computed from IsCompleted)
- ✅ `WorkItemPriority Priority` enum
- ✅ `CompletedAt` field (TIMESTAMPTZ)
- ✅ `SortOrder` field (INTEGER)
- ✅ `GoalId` FK (was missing)
- ✅ `MeetingId` FK (standardized from OneOnOneId as int)
- ✅ `TaskType DerivedType` computed property (enum based on which FK is set)
- ✅ `TaskType` enum (Standalone, ProjectTask, GoalTask, MeetingActionItem)

### Schema Columns → Properties Mapping
| Schema Column | C# Property | Type | Notes |
|---|---|---|---|
| id | Id | Guid | UUID PK |
| organization_id | OrganizationId | Guid | Non-nullable FK |
| owner_team_member_id | OwnerTeamMemberId | Guid? | Nullable FK |
| created_by_user_id | CreatedByUserId | Guid | FK to users |
| parent_task_id | ParentTaskId | Guid? | FK to tasks |
| project_id | ProjectId | Guid? | FK to projects |
| goal_id | GoalId | Guid? | FK to goals |
| meeting_id | MeetingId | Guid? | FK to meetings |
| title | Title | string | VARCHAR 300 |
| description | Description | string? | TEXT |
| status | Status | WorkItemStatus enum | task_status enum |
| priority | Priority | WorkItemPriority enum | task_priority enum |
| due_date | DueDate | DateTime? | TIMESTAMPTZ |
| completed_at | CompletedAt | DateTime? | TIMESTAMPTZ |
| sort_order | SortOrder | int | INTEGER |

### Code Impact Areas
**CRITICAL - Breaking Changes:**

**Type References to Fix:**
- ❌ All `IndividualTask` → `Task`
- ❌ All `MeetingTask` → `Task`
- ❌ All `TrackerTask` → `Task`

**Property References to Fix:**
- ❌ `task.Notes` - REMOVED (store in Description if needed)
- ❌ `task.Status` (string computed) - NOW `task.Status` (enum)
- ❌ `task.OwnerName` - REMOVED (use `task.Owner.FullName`)
- ❌ `task.MeetingCount` - REMOVED
- ❌ `task.IsOverdue` - NOW uses `task.DueDate` and `task.Status` enum
- ❌ `task.DaysUntilDue` - NOW `task.DaysRemaining`
- ❌ `task.HasSubtasks` - REMOVED (check `task.Subtasks.Count > 0`)
- ❌ `task.SubtaskProgress` - REMOVED (recalculate from Subtasks)

**Type Conversions Needed:**
- ❌ `task.DueDate` (DateTime) → Compare with nullable DateTime?
- ❌ `task.Status` (now enum) - All string comparisons ("Completed", "InProgress", etc.) must use enum values
- ❌ `task.IsCompleted` (was direct property) → Now computed from `Status == WorkItemStatus.Completed`

**ITask Interface Issues:**
- ❌ `ITask` interface expects `int Id`, but `Task.Id` is now `Guid`
- ❌ `ITask` expects `DateTime DueDate`, but `Task.DueDate` is now `DateTime?`
- ❌ `ITask` expects properties like `Notes`, `Status` (string), `OwnerName`
- ⚠️ `ITask` interface likely needs to be **deprecated or redesigned**

**Files with MASSIVE Changes Needed:**
- NewTaskViewModel (entire ViewModel tied to ITask interface)
- AIFunctionService (CreateTaskAsync creates IndividualTask)
- ValidationHelper (TaskEnums.cs references)
- MeasurableService (DisplayValue references)
- SearchService (references IndividualTask)
- OneOnOne.cs (has `List<MeetingTask> Tasks` - becomes `List<Task> Tasks`)
- TrackerDbContext (EF configuration for IndividualTask and MeetingTask)
- All database seeding code (TestDataBuilder, DatabaseSeederTests)
- All service classes that work with tasks

---

## 4. MEETING.CS ✅ CONSOLIDATED IMPLEMENTATION COMPLETE

### Current Status
✅ **Implementation Complete** - Consolidated Meeting class created

### Models Consolidated
**COMPLETE:** Two separate models consolidated into ONE:
- ❌ `OneOnOne` class - **TO BE DELETED** (all properties moved to Meeting.cs)
- ✅ `Meeting` class - Enhanced to unified model (implementation complete)

### Implementation Summary

The new Meeting.cs consolidates:
1. **From OneOnOne**: Calendar sync fields, meeting execution fields, task/agenda collections
2. **From Meeting**: Basic meeting structure, type enum, participant relationships
3. **Schema alignment**: All properties now match Supabase `meetings` table exactly

**Key design decisions:**
- ✅ `Id` (Guid) - proper UUID PK
- ✅ `OrganizationId` (Guid, non-nullable) - required FK
- ✅ `CreatedByUserId` (Guid) - who created
- ✅ `Type` enum - MeetingType with correct values
- ✅ `ManagerTeamMemberId` / `ReportTeamMemberId` - 1:1 specific
- ✅ `TeamId` - team meeting context
- ✅ `ScheduledAt` / `DurationMinutes` - consolidated date/time
- ✅ `RecurrenceRule` - string-based, not bool
- ✅ `StartedAt` / `EndedAt` - actual execution times
- ✅ Calendar sync fields - preserved from OneOnOne
- ✅ `Tasks` (List<Task>) - action items with MeetingId FK
- ✅ `AgendaItems` - preserved collection
- ✅ All computed properties included

**Status:** ✅ READY FOR CODE MIGRATION

| OneOnOne Property | New Meeting Property | Type Change | Notes |
|---|---|---|---|
| Id (int) | Id (Guid) | ❌ int→Guid | Type mismatch fixed |
| OrganizationId (Guid?) | OrganizationId (Guid) | ⚠️ Add non-nullable | OneOnOne nullable, schema non-nullable |
| ManagerUserId (int?) | ManagerTeamMemberId (Guid?) | ❌ int→Guid FK | Type mismatch fixed |
| TeamMember | Report (TeamMember?) | ⚠️ Renamed | "Who the 1:1 is with" |
| Description (string) | Title (string) | ✅ Rename for clarity | Better naming |
| Date (DateTime) | ScheduledAt (DateTime) | ✅ Rename | More semantic |
| StartTime (TimeSpan) | ScheduledAt + DurationMinutes | ✅ Consolidated | Combined fields |
| EndTime (TimeSpan) | (calculated from ScheduledAt + DurationMinutes) | ✅ Consolidated | Calculated |
| Duration (TimeSpan) | DurationMinutes (int) | ❌ TimeSpan→int | Different type |
| IsRecurring (bool) | RecurrenceRule (string?) | ⚠️ Changed | Now stores actual rule |
| Status | Status (enum) | ✅ Same | Enum unchanged |
| Agenda (string) | (moved to Notes or separate) | ⚠️ Clarified | Merged with Notes |
| Notes (string) | Notes (string?) | ✅ Same | Kept |
| Feedback (string) | (removed to schema) | ❌ REMOVED | Not in schema |
| GoogleCalendarEventId | GoogleCalendarEventId | ✅ Same | Kept |
| CalendarEventId | OutlookCalendarEventId | ✅ Rename | More explicit |
| TeamsMeetingUrl | TeamsMeetingUrl | ✅ Same | Kept |
| TeamsMeetingId | TeamsMeetingId | ✅ Same | Kept |
| GoogleMeetUrl | GoogleMeetUrl | ✅ Same | Kept |
| LastSyncedAt | LastSyncedAt | ✅ Same | Kept |
| SyncStatus | SyncStatus | ✅ Same | Kept |
| IsSyncedToGoogle (bool) | (computed) | ⚠️ Computed | Check GoogleCalendarEventId != null |
| IsSyncedToOutlook (bool) | (computed) | ⚠️ Computed | Check OutlookCalendarEventId != null |
| HasTeamsMeeting (computed) | (computed) | ✅ Same | Check TeamsMeetingUrl != null |
| HasGoogleMeet (computed) | (computed) | ✅ Same | Check GoogleMeetUrl != null |
| Tasks (List<MeetingTask>) | Tasks (List<Task>) | ✅ Rename | MeetingTask → Task (consolidated) |
| AgendaItems | AgendaItems | ✅ Same | Kept |
| Computed: TaskCount | ActionItemCount | ⚠️ Rename | More semantic |
| Computed: IncompleteTaskCount | (use ActionItemCount) | ⚠️ Rename | Cleaner |
| Computed: TasksDisplay | (can add if needed) | ⚠️ Optional | Low priority |
| Computed: DateTimeDisplay | (can add if needed) | ⚠️ Optional | Low priority |

### Property Mapping (Meeting.cs → Meeting)

| Current Meeting Property | New Meeting Property | Status | Notes |
|---|---|---|---|
| Id (int) | Id (Guid) | ❌ TYPE FIX | int→Guid |
| OrganizationId (Guid?) | OrganizationId (Guid) | ⚠️ NULLABILITY FIX | Needs non-nullable |
| Type (MeetingType enum) | Type (MeetingType enum) | ✅ KEEP | But enum needs redesign (see below) |
| Title | Title | ✅ KEEP | Already correct |
| PrimaryAttendeeId (int) | ReportTeamMemberId (Guid?) | ❌ TYPE FIX + RENAME | int→Guid; rename for clarity |
| PrimaryAttendee (TeamMember) | Report (TeamMember?) | ⚠️ RENAME | More semantic for 1:1 context |
| Date (DateTime) | ScheduledAt (DateTime) | ✅ RENAME | Better semantic |
| StartTime (TimeSpan?) | (merged into ScheduledAt + DurationMinutes) | ⚠️ CONSOLIDATE | Combine fields |
| EndTime (TimeSpan?) | (calculated) | ⚠️ CONSOLIDATE | Calculate from ScheduledAt + DurationMinutes |
| Duration (int?) | DurationMinutes (int?) | ✅ RENAME | Better naming |
| Status | Status (enum) | ✅ KEEP | Same |
| IsRecurring | RecurrenceRule | ⚠️ CHANGE | bool→string |
| RecurringSeriesId | (can be stored in RecurrenceRule as metadata) | ⚠️ CHANGE | Needs rethinking |
| ProjectId (int?) | ProjectId (Guid?) | ❌ TYPE FIX | int→Guid |
| Project | Project | ✅ KEEP | FK navigation |
| Notes | Notes | ✅ KEEP | Already correct |
| Location | Location | ✅ KEEP | Already correct |
| ❌ MISSING | CreatedByUserId (Guid) | ❌ ADD | Required by schema |
| ❌ MISSING | ManagerTeamMemberId (Guid?) | ❌ ADD | For 1:1 context |
| ❌ MISSING | ReportTeamMemberId (Guid?) | ❌ ADD | For 1:1 context |
| ❌ MISSING | TeamId (Guid?) | ❌ ADD | For team meetings |
| ❌ MISSING | StartedAt (DateTime?) | ❌ ADD | When meeting actually started |
| ❌ MISSING | EndedAt (DateTime?) | ❌ ADD | When meeting actually ended |

### Enum Design: MeetingType

**Current C# MeetingType (BROKEN):**
```csharp
public enum MeetingType
{
    ActionItem,     // ❌ WRONG
    FollowUpItem    // ❌ WRONG
}
```

**Correct MeetingType (align with schema):**
```csharp
public enum MeetingType
{
    OneOnOne,       // 'one_on_one'
    TeamMeeting,    // 'team_meeting'
    AllHands,       // 'all_hands'
    Project,        // 'project'
    Interview,      // 'interview'
    Other           // 'other'
}
```

### Enum Design: MeetingStatus

Uses existing `goal_status` enum from schema:
```csharp
public enum MeetingStatus  // From 'meeting_status' enum in schema
{
    Scheduled,      // 'scheduled'
    InProgress,     // 'in_progress' (started but not ended)
    Completed,      // 'completed'
    Cancelled       // 'cancelled'
}
```

### Schema Changes Required

**Meetings Table - New Columns Needed:**
- ✅ `started_at` (TIMESTAMPTZ, nullable) - when meeting actually started
- ✅ `ended_at` (TIMESTAMPTZ, nullable) - when meeting actually ended
- Both already in schema based on 07_MEETINGS.sql review

**MeetingType Enum - Values Must Change:**
Current: `'action_item', 'follow_up_item'`
Required: `'one_on_one', 'team_meeting', 'all_hands', 'project', 'interview', 'other'`

### Code Impact Areas

**CRITICAL - Breaking Changes:**
- ❌ All `OneOnOne` → `Meeting`
- ❌ All `MeetingTask` → `Task` (already consolidated)
- ❌ All `MeetingType.ActionItem` → `MeetingType.OneOnOne` (enum values change)
- ❌ All date/time field references (Date/StartTime/EndTime → ScheduledAt/DurationMinutes/StartedAt/EndedAt)
- ❌ All `IsRecurring` bool checks → check if `RecurrenceRule` is not null

**Property References to Fix:**
- ❌ `meeting.Date` → `meeting.ScheduledAt`
- ❌ `meeting.StartTime` / `meeting.EndTime` → `meeting.ScheduledAt` + `meeting.DurationMinutes`
- ❌ `meeting.Duration` (TimeSpan) → `meeting.DurationMinutes` (int?)
- ❌ `meeting.IsRecurring` → `!string.IsNullOrEmpty(meeting.RecurrenceRule)`
- ❌ `meeting.Description` (OneOnOne only) → `meeting.Title` (standard field)
- ❌ `meeting.PrimaryAttendeeId` → `meeting.ReportTeamMemberId` (semantic change)
- ❌ `meeting.TeamMember` (OneOnOne property) → `meeting.Report` (renamed)
- ❌ `meeting.Feedback` → REMOVED (no schema equivalent)
- ❌ `meeting.Agenda` → consolidate into `meeting.Notes` or keep separate

**Computed Properties to Update:**
- ❌ `meeting.TaskCount` → `meeting.ActionItemCount`
- ❌ `meeting.IncompleteTaskCount` → recompute from Tasks
- ❌ `meeting.AgendaCount` → `meeting.AgendaItemCount`
- ❌ `meeting.DateTimeDisplay` → can add if needed
- ❌ `meeting.DescriptionPreview` → adjust to Title
- ❌ `meeting.TasksDisplay` → adjust to ActionItemCount

**Collections:**
- ❌ `meeting.Tasks` (List<MeetingTask>) → `meeting.Tasks` (List<Task>) with MeetingId FK
- ⚠️ `meeting.AgendaItems` → Verify AgendaItem still has FK to meetings
- ❌ `meeting.LinkedTasks`, `meeting.LinkedOkrs`, `meeting.LinkedKpis` → REMOVE (not in schema)

**Files with Massive Changes Needed:**
- OneOnOneViewModel → rename/consolidate with MeetingViewModel
- OneOnOneEditorViewModel → merge with MeetingEditorViewModel
- OneOnOneService → consolidate with MeetingService
- MeetingPreparationService (OneOnOne references)
- CalendarSyncService (OneOnOne sync logic)
- All OneOnOne-related views (rename to Meeting views)
- All seeding/test data builders
- Dashboard/reports using OneOnOne data
- AI/insights using meeting data
- Search service (OneOnOne references)
- TrackerDbContext (OneOnOne DbSet removal)

---

## 5. GOAL.CS ✅ PERFECT (Consolidation: Replaces ObjectiveKeyResult)

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `goals` table exactly

### Models Consolidated
**BREAKING:** `ObjectiveKeyResult` class will be DELETED
- ✅ Goal.cs is the canonical model - use this only
- ❌ ObjectiveKeyResult.cs to be deleted - all properties in Goal.cs

### Why Goal.cs is Superior to OKR
Goal.cs has **more** properties aligned with schema:
- ✅ `CreatedByUserId` - OKR missing this
- ✅ `IsTeamVisible` - OKR missing this  
- ✅ `IsOrgVisible` - OKR missing this
- ✅ `Milestones` collection - OKR missing this
- ✅ `ProgressPercent` + `ProgressOverride` - OKR has CompletionPercentage (different name)

OKR has only **extra computed properties** (can be added to Goal if needed):
- `MeetingCount` - can add if needed
- `LinkedKpiCount`, `LinkedProjectCount`, `LinkedTaskCollectionCount` - can compute from relationships
- `TimePeriodDisplay` - can add if needed
- `KeyResultCount`, `HasKeyResults` - can compute from Targets collection
- `IsActive`, `DaysRemaining` - can add if needed

### Code Impact Areas
**BREAKING:** 
- ❌ All `ObjectiveKeyResult` references → `Goal`
- ❌ All `KeyResult` references → `Target` (already consolidated in Task/Target validation)

**Files Affected:**
- OkrsViewModel (references ObjectiveKeyResult)
- OKRs-related services
- All database seeding code
- Tests referencing ObjectiveKeyResult

---

## 6. TARGET.CS ✅ PERFECT (No consolidation needed)

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `targets` table exactly

### Changes Made
NONE - Model was already correct.

---

## 7. METRIC.CS ✅ PERFECT (Consolidation: Replaces KeyPerformanceIndicator)

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `metrics` table exactly

### Models Consolidated
**BREAKING:** `KeyPerformanceIndicator` class will be DELETED
- ✅ Metric.cs is the canonical model - use this only
- ❌ KeyPerformanceIndicator.cs to be deleted - all properties in Metric.cs

### Why Metric.cs is Superior to KPI
Metric.cs has **all required properties** aligned with schema:
- ✅ `CreatedByUserId` - KPI missing this
- ✅ `IsTeamVisible` - KPI missing this
- ✅ `IsOrgVisible` - KPI missing this
- ✅ `WarningThreshold` - KPI missing this
- ✅ `CriticalThreshold` - KPI missing this
- ✅ `History` collection - KPI missing this
- ✅ Uses `decimal` for values - KPI used `double` (type mismatch)
- ✅ `CurrentValue` naming - KPI called it `Value`
- ✅ `Id` is Guid - KPI used int (type mismatch)
- ✅ `ParentMetricId` is Guid - KPI used int (type mismatch)

KPI has only **extra/different computed properties** (can be added to Metric if needed):
- `MeasurableId` (legacy) - deprecated, don't add
- `Status` (KpiStatusEnum) - Metric uses OkrStatus enum (better aligned)
- `PercentComplete` (double) - Metric has Progress (decimal, better)
- `MeetingCount` - can add if needed

### Optional Enhancements (can add to Metric.cs later)
- `HasDataSources` → `public bool HasDataSources => DataSources?.Count > 0;`
- `HasChildMetrics` → `public bool HasChildMetrics => ChildMetrics?.Count > 0;`

### Code Impact Areas
**CRITICAL - Breaking Changes:**
- ❌ All `KeyPerformanceIndicator` → `Metric`
- ❌ All `KpiDataSource` → `MetricDataSource`
- ❌ All `KpiStatusEnum` → `OkrStatus` (where used)
- ❌ All `KpiFrequencyEnum` → `MetricFrequency`
- ❌ All `TargetDirectionEnum` → `MetricTargetDirection`

**Files Affected:**
- MetricsViewModel / KPIViewModel (rename/consolidate)
- MetricService / KPIService (consolidate)
- All database seeding code
- Tests referencing KeyPerformanceIndicator
- Views/Controls related to KPIs
- Reports using KPI data
- AI/ML services if they reference KPIs

---

## 8. OTHER MODELS ⏳ PENDING

---

## 6. OBJECTIVE/OKR MODELS ⏳ PENDING

### Current Status
❓ Not yet validated

---

## 7. OTHER MODELS ⏳ PENDING

Models still to validate:
- KeyResult
- KeyPerformanceIndicator (KPI)
- ObjectiveKeyResult
- Target
- DevelopmentGoal
- OneOnOneLinkedTask
- And others...

---

## Summary of Breaking Changes

### Types Deleted
| Type | Replacement | Impact | Status |
|---|---|---|---|
| IndividualTask | Task | Massive - used throughout codebase | ✅ **DELETED** |
| MeetingTask | Task | Moderate - 1:1 meeting related | ✅ **DELETED** |
| TrackerTask | Task | None - just a rename | ✅ Code updated |
| ObjectiveKeyResult | Goal | Moderate - OKR framework | ✅ **DELETED** |
| KeyPerformanceIndicator | Metric | Moderate - KPI tracking | ✅ **DELETED** |
| KeyResult | Target | Moderate - renamed Key Results | ✅ **DELETED** |
| OneOnOne | Meeting | Moderate - 1:1 meetings | ✅ **DELETED** |

### Interface Impact
| Interface | Impact | Notes |
|---|---|---|
| ITask | ❌ BROKEN | expects int Id, DateTime DueDate, string Status - Task has Guid Id, DateTime? DueDate, WorkItemStatus enum Status |
| IMeasurable | ⚠️ PARTIALLY BROKEN | Metric implements this, but old KPI may have had different expectations |
| IKpiSource | ✅ Can be DELETED | No longer needed with Metric consolidation |

### Enum Changes
| Old | New | Impact |
|---|---|---|
| TaskTypeEnum | TaskType (on Task class) | Naming change, but TaskTypeEnum still used in ITask |
| KpiStatusEnum | OkrStatus | Status enum unified for both metrics and goals |
| KpiFrequencyEnum | MetricFrequency | Renamed for clarity |
| TargetDirectionEnum | MetricTargetDirection | Renamed for clarity |
| ObjectiveStatusEnum | OkrStatus | Status enum unified |
| N/A | WorkItemStatus | Used instead of computed Status string |
| N/A | WorkItemPriority | Standardized priority across models |

### Property Name Changes
| Model | Old Name | New Name | Impact |
|---|---|---|---|
| Project | EndDate | TargetEndDate/ActualEndDate | String replacement needed |
| Project | Progress | ProgressPercent | String replacement needed |
| Project | ID | Id | Capitalization fix |
| Task | DaysUntilDue | DaysRemaining | Naming consistency |
| Task | N/A (computed from IsCompleted) | Status (enum) | Type change |
| Metric | Value | CurrentValue | Naming clarity |
| Metric | KpiId | Id (Guid) | Type change int→Guid |

### Collections Renamed
| Model | Old Name | New Name | Impact |
|---|---|---|---|
| OKR → Goal | KeyResults | Targets | Reference update needed |
| KPI → Metric | ChildKpis | ChildMetrics | Reference update needed |
| KPI → Metric | DataSources | DataSources | Name stays same (KpiDataSource → MetricDataSource) |

---

## Code Cleanup Phases (Post-Validation)

### Phase 2A: Type References
- Search/replace all `IndividualTask` → `Task`
- Search/replace all `MeetingTask` → `Task`
- Search/replace all `TrackerTask` → `Task`
- Update DbContext DbSets

### Phase 2B: Property Name Fixes
- Search/replace `project.EndDate` → `project.TargetEndDate` or `project.ActualEndDate`
- Search/replace `project.Progress` → `project.ProgressPercent`
- Search/replace `project.ID` → `project.Id`
- Update task property references (Notes, IsOverdue, etc.)

### Phase 2C: Type Conversion Issues
- Update all `.Status` enum comparisons (Status != WorkItemStatus.Completed)
- Update all priority comparisons
- Fix ITask interface usage or redesign it
- Update EF Core configurations

### Phase 2D: Computed Property Rewrites
- Remove `task.OwnerName` - replace with `task.Owner?.FirstName + " " + task.Owner?.LastName` or similar
- Remove `task.HasSubtasks` - replace with `task.Subtasks?.Count > 0`
- Rewrite `SubtaskProgress` calculation
- Rewrite `IsOverdue` logic using new Status enum

---

## Estimated Complexity

| Model | Complexity | # of Files Affected | Est. Changes | Status |
|---|---|---|---|---|
| Project | Medium | 15-20 | Property renames, interface removal | ✅ DONE |
| Milestone | Low | 0 | No changes needed | ✅ DONE |
| Task | **CRITICAL** | **40-50** | 3 models → 1, interface broken, property removals, **+ Notes field added** | ✅ DESIGN COMPLETE |
| Goal | Low | 10-15 | Delete ObjectiveKeyResult, use Goal only | ✅ PERFECT |
| Target | Low | 5-10 | Delete KeyResult, use Target only | ✅ PERFECT |
| Metric | Low | 15-20 | Delete KeyPerformanceIndicator, consolidate to Metric | ✅ PERFECT |
| Meeting | **CRITICAL** | **20-30** | 2 models → 1, date/time consolidation, enum redesign, calendar sync reconciliation | ✅ **IMPLEMENTATION COMPLETE** |
| **AgendaItem** | **Low** | **5-10** | **Props deleted: Category, Priority, Resolution, IsCompleted, LinkedTaskId, OrganizationId; Field renames: Description→Title; Id int→Guid; OneOnOneId→MeetingId** | **✅ IMPLEMENTATION COMPLETE** |
| **CalendarLink** | **Medium** | **15-20** | **Refactored: User-provider auth model; meeting-specific sync moved to Meeting entity; Removed: OneOnOneId, ExternalEventId, ETag, SyncDirection, CalendarLinkStatus; Added: SyncToken consolidation** | **✅ IMPLEMENTATION COMPLETE** |
| **CalendarSyncToken** | **Low** | **5-10** | **Consolidated into CalendarLink.SyncToken; Deleted CalendarSyncToken.cs; Schema table to be added to Supabase later** | **✅ CONSOLIDATED** |
| **ChangeTrackingEntry** | **Low** | **0** | **Deleted - Obsolete offline sync infrastructure for old SQL Server pattern; v2 offline will use Supabase-native approaches (Realtime, PostgREST cache); No functionality to preserve** | **✅ DELETED** |
| **DailyBriefing** | **Low** | **2-3** | **Runtime DTO (not persisted); ⚠️ DEPENDS ON OneOnOne (consolidating to Meeting) - will need Phase 2 update: List<OneOnOne> → List<Meeting>** | **✅ PERFECT (Phase 2 update needed)** |
| Other Models | TBD | TBD | TBD after validation | ⏳ PENDING |

---

## 8. AGENDAITEM.CS ✅ REWRITTEN - SCHEMA ALIGNED

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `meeting_agenda_items` table exactly

### Changes Made
Complete rewrite to align with Supabase schema:

**Deleted Properties (NOT in schema):**
- ❌ `OrganizationId` (Guid?) - Not in meeting_agenda_items
- ❌ `Description` (string) - Replaced by `Title` + `Notes`
- ❌ `Category` (AgendaItemCategory enum) - Not in schema
- ❌ `Priority` (Severity enum) - Not in schema  
- ❌ `Resolution` (string) - Not in schema
- ❌ `IsCompleted` (bool) - Replaced by `IsDiscussed`
- ❌ `LinkedTaskId` (int?) - Not in schema
- ❌ `LinkedItems` collection - Not in schema

**Type Changes:**
- ❌ `Id: int` → ✅ `Id: Guid` 
- ❌ `OneOnOneId: int` → ✅ `MeetingId: Guid`

**New Properties Added (from schema):**
- ✅ `AddedByTeamMemberId: Guid?` - FK to team_members (who added this item)
- ✅ `IsDiscussed: bool` - Whether this item was discussed (replaces IsCompleted)
- ✅ `DiscussedAt: DateTime?` - When it was discussed
- ✅ `TimeEstimateMinutes: int?` - Estimated discussion time
- ✅ `ActualDurationMinutes: int?` - Actual time spent discussing
- ✅ `SortOrder: int` - Position in agenda

**Property Renames:**
- `Description` (string) → `Title` (string) - Primary topic
- `Title` now + `Notes` (string?) - Full schema mapping

### Schema Columns → Properties Mapping
| Schema Column | C# Property | Type | Notes |
|---|---|---|---|
| id | Id | Guid | UUID PK |
| meeting_id | MeetingId | Guid | FK to meetings |
| added_by_team_member_id | AddedByTeamMemberId | Guid? | FK to team_members |
| title | Title | string | VARCHAR 300, NOT NULL |
| notes | Notes | string? | TEXT, nullable |
| sort_order | SortOrder | int | Ordering within agenda |
| is_discussed | IsDiscussed | bool | Default false |
| discussed_at | DiscussedAt | DateTime? | When discussed, nullable |
| time_estimate_minutes | TimeEstimateMinutes | int? | Estimate, nullable |
| actual_duration_minutes | ActualDurationMinutes | int? | Actual time, nullable |
| created_at | CreatedAt (inherited) | DateTime | Audit field |
| updated_at | UpdatedAt (inherited) | DateTime | Audit field |

### Computed Properties Added
| Property | Type | Calculation | Use Case |
|---|---|---|---|
| `IsPending` | bool | `!IsDiscussed && !string.IsNullOrWhiteSpace(Title)` | Check if ready to discuss |
| `TimeVarianceMinutes` | int? | `ActualDurationMinutes - TimeEstimateMinutes` | Track estimation accuracy |

### Code Impact Areas

**CRITICAL - Breaking Changes:**
- ❌ All `agendaItem.Description` → `agendaItem.Title` (or use Notes for additional info)
- ❌ All `agendaItem.OneOnOneId` → `agendaItem.MeetingId`
- ❌ All `agendaItem.IsCompleted` checks → `agendaItem.IsDiscussed`
- ❌ All `agendaItem.Category` enum usage → REMOVE (not in schema)
- ❌ All `agendaItem.Priority` enum usage → REMOVE (not in schema)
- ❌ All `agendaItem.Resolution` text handling → REMOVE (not in schema)
- ❌ All `agendaItem.LinkedTaskId` references → REMOVE (not in schema)
- ❌ All `agendaItem.LinkedItems` collection usage → REMOVE (not in schema)

**Property References to Fix:**
- ❌ `item.Description` → `item.Title` (primary change)
- ❌ `item.IsCompleted = true` → `item.IsDiscussed = true; item.DiscussedAt = DateTime.Now`
- ❌ `item.Category` → REMOVE category-based filtering
- ❌ `item.Priority` → REMOVE priority-based sorting
- ❌ `item.Resolution` → Move to meeting_notes if resolution tracking needed

**Collections:**
- ❌ `agendaItem.LinkedItems` → REMOVED (not in schema)
- ✅ `meeting.AgendaItems` → Still valid, FK properly maintained

**Files with Changes Needed:**
- AgendaItemService (description → title, removal of category/priority logic)
- Meeting edit/creation views (update property bindings)
- Meeting detail view (update agenda display)
- Test data builders/seed data
- Any reports showing agenda categories/priorities
- Database migrations (if using EF Core migrations)

### Migration Strategy
1. Update all views to use `Title` instead of `Description`
2. Remove category/priority UI filters (not supported in schema)
3. Update meeting service methods to use new property names
4. Remove LinkedItems UI and logic (not in schema)
5. Update agenda item editing to include time tracking fields
6. Update meeting prep to track discussion time
7. Create data migration for any old agenda item data (category/priority data will be lost)

---

## 9. CALENDARLINK.CS ✅ REFACTORED - ARCHITECTURE REDESIGN

### Architectural Decision
**Problem:** CalendarLink confused two separate concerns:
1. User's authentication/connection to a calendar provider (belongs to Users)
2. Meeting-specific calendar event tracking (belongs to Meetings)

**Solution:** Refactored CalendarLink to ONLY handle concern #1 (user-provider auth)
- Concern #2 (meeting-specific sync) remains on Meeting.cs where it belongs
- This aligns with Supabase schema where `calendar_links` = user accounts, meetings have `calendar_link_id` FK

### Schema Match Status
**Status:** ✅ COMPLETE - Matches `calendar_links` table exactly (user auth layer only)

### Changes Made
**Complete architecture redesign - moved to user authentication focus:**

**Deleted Properties (NOT in schema - belonged in Meeting layer):**
- ❌ `int OneOnOneId` - Meeting FK (BELONGS in Meeting.cs, which has calendar_link_id instead)
- ❌ `string ExternalEventId` - Meeting-specific sync (BELONGS in Meeting.calendar_event_id)
- ❌ `string ETag` - Meeting-specific conflict detection (BELONGS in Meeting, per-event tracking)
- ❌ `DateTime LastSyncedAt` - Meeting-specific timestamp (BELONGS in Meeting.last_synced_at)
- ❌ `SyncDirection LastSyncDirection` - Meeting-specific direction (BELONGS in Meeting layer)
- ❌ `CalendarLinkStatus Status` - Meeting-specific status (BELONGS in Meeting.calendar_sync_status)
- ❌ `string LastError` - Meeting-specific error (BELONGS in Meeting layer)
- ❌ `OneOnOne Navigation` - Removed (belongs in Meeting.CalendarLink instead)
- ❌ `Guid? OrganizationId` - Not in schema

**Type Changes:**
- ❌ `int Id` → ✅ `Guid Id`
- ❌ `string ProviderId` → ✅ `CalendarProviderType Provider` enum (google/microsoft/apple/other)

**New Properties Added (from schema):**
- ✅ `Guid UserId` - FK to users (user owns this calendar account link)
- ✅ `string AccountEmail` - Which email account this link represents
- ✅ `string AccountName` - Display name for the account
- ✅ `string AccessToken` - OAuth access token (encrypted at rest)
- ✅ `string RefreshToken` - OAuth refresh token
- ✅ `DateTime? TokenExpiresAt` - When access token expires
- ✅ `bool IsActive` - Link is enabled/disabled
- ✅ `bool SyncEnabled` - Sync is enabled/disabled
- ✅ `bool SyncMeetingsToCalendar` - Preference to sync meetings
- ✅ `bool SyncTasksToCalendar` - Preference to sync tasks
- ✅ `bool CreateMeetingFromCalendar` - Preference to create meetings from calendar
- ✅ `string DefaultCalendarId` - Which calendar in provider to use
- ✅ `string DefaultCalendarName` - Name of default calendar
- ✅ `DateTime? LastSyncAt` - Last sync timestamp (provider-level, not meeting-level)
- ✅ `CalendarSyncStatusType? LastSyncStatus` - Overall provider sync status
- ✅ `string LastSyncError` - Overall provider sync error

**New Enum:**
- ✅ `CalendarProviderType` (Google, Microsoft, Apple, Other)
- ✅ `CalendarSyncStatusType` (Pending, Synced, Failed, Cancelled) - Provider-level only

**Computed Properties Added:**
- ✅ `IsTokenExpired` (bool) - Check if token needs refresh
- ✅ `IsReadyToSync` (bool) - Link is active and has valid auth
- ✅ `LastSyncSuccessful` (bool) - Quick check of last sync status

### Schema Columns → Properties Mapping
| Schema Column | C# Property | Type | Notes |
|---|---|---|---|
| id | Id | Guid | UUID PK |
| user_id | UserId | Guid | FK to users |
| provider | Provider | CalendarProviderType | Enum: google/microsoft/apple/other |
| account_email | AccountEmail | string? | Which account email |
| account_name | AccountName | string? | Display name |
| access_token | AccessToken | string? | OAuth token (encrypted) |
| refresh_token | RefreshToken | string? | OAuth refresh token |
| token_expires_at | TokenExpiresAt | DateTime? | Token expiration |
| is_active | IsActive | bool | Link enabled |
| sync_enabled | SyncEnabled | bool | Sync enabled |
| sync_meetings_to_calendar | SyncMeetingsToCalendar | bool | Sync preference |
| sync_tasks_to_calendar | SyncTasksToCalendar | bool | Sync preference |
| create_meeting_from_calendar | CreateMeetingFromCalendar | bool | Sync preference |
| default_calendar_id | DefaultCalendarId | string? | Default calendar |
| default_calendar_name | DefaultCalendarName | string? | Default calendar name |
| last_sync_at | LastSyncAt | DateTime? | Last sync (provider-level) |
| last_sync_status | LastSyncStatus | CalendarSyncStatusType? | Provider sync status |
| last_sync_error | LastSyncError | string? | Sync error message |
| created_at | CreatedAt (inherited) | DateTime | Audit |
| updated_at | UpdatedAt (inherited) | DateTime | Audit |

### Code Impact Areas

**CRITICAL - Breaking Changes:**
- ❌ All `calendarLink.OneOnOneId` → MOVE TO `meeting.CalendarLinkId` (reverse FK)
- ❌ All `calendarLink.ExternalEventId` → MOVE TO `meeting.CalendarEventId`
- ❌ All `calendarLink.ETag` → REMOVE (too meeting-specific, use meeting-level tracking)
- ❌ All `calendarLink.LastSyncedAt` → MOVE TO `meeting.LastSyncedAt`
- ❌ All `calendarLink.LastSyncDirection` → REMOVE (not in schema, use sync service logic)
- ❌ All `calendarLink.Status` (CalendarLinkStatus) → MOVE TO `meeting.CalendarSyncStatus`
- ❌ All `calendarLink.LastError` → MOVE TO `meeting.SyncError` or use provider-level `calendarLink.LastSyncError`
- ❌ All `string ProviderId` references → Use `CalendarProviderType Provider` enum

**Property References to Fix:**
- ❌ `link.OneOnOneId` → REMOVED (use meeting.CalendarLinkId instead - reverse lookup)
- ❌ `link.ExternalEventId` → `link` doesn't have this; use `meeting.CalendarEventId`
- ❌ `link.LastSyncedAt` → Use `meeting.LastSyncedAt` for meeting-specific sync
- ❌ `link.Status` → Use `meeting.CalendarSyncStatus` for meeting-specific status
- ✅ `link.LastSyncAt` → OK (provider-level last sync)
- ✅ `link.LastSyncStatus` → OK (provider-level status)
- ✅ `link.AccessToken` → OK (user auth)
- ✅ `link.RefreshToken` → OK (user auth)
- ✅ `link.IsActive` → OK (enable/disable this user's provider link)

**Enums to Update:**
- ❌ REMOVE `SyncDirection` enum (Push/Pull) - not needed
- ❌ REMOVE `CalendarLinkStatus` enum (Synced/Pending/Error/Orphaned) - use meeting-level instead
- ✅ ADD `CalendarProviderType` enum (Google/Microsoft/Apple/Other)
- ✅ ADD `CalendarSyncStatusType` enum (Pending/Synced/Failed/Cancelled) - provider-level only

**Navigation Changes:**
- ❌ `meeting.CalendarLink` (OneOnOne) → BECOMES `meeting.CalendarLinkId` (FK instead of object)
  - Lazy load via: `db.CalendarLinks.FirstAsync(c => c.Id == meeting.CalendarLinkId)`

**Files with Major Changes Needed:**
- CalendarSyncManager - uses CalendarLink to track meeting sync
- CalendarSyncService (Google, Microsoft, Outlook) - expects meeting-level sync tracking
- All calendar-related services - need to separate user auth from meeting sync logic
- Database migrations - CalendarLink FK changes
- EF Core DbContext configurations
- Any code querying CalendarLinks by OneOnOneId → NOW query by MeetingId instead

### Critical Code Pattern Changes

**OLD PATTERN (Wrong - mixed concerns):**
```csharp
var calendarLink = await db.CalendarLinks
    .FirstAsync(cl => cl.OneOnOneId == meetingId);
// Use calendarLink.ExternalEventId, LastSyncedAt, Status
```

**NEW PATTERN (Correct - separated concerns):**
```csharp
// Get user's calendar account
var calendarLink = await db.CalendarLinks
    .FirstAsync(cl => cl.Id == meeting.CalendarLinkId);
// Use calendarLink for: AccessToken, RefreshToken, AccountEmail, IsActive

// Get meeting-specific sync info from Meeting entity
var syncStatus = meeting.CalendarSyncStatus;
var eventId = meeting.CalendarEventId;
var lastSync = meeting.LastSyncedAt;
```

### Migration Strategy
1. **Phase 2a**: Update all CalendarLink queries to use UserId instead of OneOnOneId
2. **Phase 2b**: Update calendar sync services to track meeting-specific sync on Meeting entity
3. **Phase 2c**: Create data migration script to:
   - Verify all meetings have CalendarLinkId FK set (if they had sync)
   - Move meeting-specific sync data to Meeting.calendar_* columns
   - Consolidate provider sync tokens (calendar_sync_tokens table)
4. **Phase 2d**: Remove any calendar link-to-meeting relationships from queries
5. **Phase 2e**: Update all calendar service implementations to use two-layer access pattern

### Key Architectural Points
- **User Layer**: CalendarLink = user's OAuth tokens and preferences for a provider
- **Meeting Layer**: Meeting = "this meeting is synced to calendar provider X via account Y, event ID is Z"
- **Sync Service Layer**: Handles sync logic, uses CalendarLink for auth and Meeting for event tracking
- This matches Supabase schema intent: calendar_links = accounts, meetings = event instances

---

## 10. CALENDARSYNCTOKEN.CS ✅ CONSOLIDATED INTO CALENDARLINK

### Consolidation Decision
**Problem:** CalendarSyncToken was a separate 1:1 model tracking only the delta sync token
**Solution:** Consolidated into CalendarLink.SyncToken property since both represent per-user-per-provider state

### What CalendarSyncToken Did
- **Purpose:** Stored delta sync tokens for incremental calendar sync
- **Essential:** Yes - without this, every sync re-fetches ALL calendar events (performance killer)
- **Data:** Minimal - just `int Id`, `string ProviderId`, `string SyncToken`, `DateTime UpdatedAt`
- **Relationship:** One per user per provider

### Consolidation Details
**Deleted Properties (NOW in CalendarLink):**
- ❌ `CalendarSyncToken` class entirely deleted
- ❌ Its single meaningful property (`SyncToken`) moved to `CalendarLink.SyncToken`

**Added to CalendarLink:**
- ✅ `string? SyncToken` property (from CalendarSyncToken)
  - For Google Calendar: `syncToken` from Events.list response
  - For Outlook: `deltaLink` from delta query
  - Enables incremental sync instead of full refetch
  - Null if not yet synced or provider doesn't support delta sync

### Code Impact
**Files with Changes Needed:**
- GoogleCalendarService - update to use `calendarLink.SyncToken` instead of separate table
- CalendarSyncService (Outlook) - update to use `calendarLink.SyncToken`
- Any query referencing `CalendarSyncTokens` DbSet → now query CalendarLink instead
- Database migrations - consolidate sync token tracking

**Old Pattern (Wrong):**
```csharp
var syncToken = await db.CalendarSyncTokens
    .Where(c => c.UserId == userId && c.ProviderId == "google")
    .Select(c => c.SyncToken)
    .FirstOrDefaultAsync();
```

**New Pattern (Correct):**
```csharp
var calendarLink = await db.CalendarLinks
    .FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == CalendarProviderType.Google);
var syncToken = calendarLink?.SyncToken;
```

### Schema Changes Required
**For Supabase Phase 2:**
- ✅ `calendar_links` table already has capacity for sync_token field
- Add `sync_token TEXT` column to `calendar_links` if not present
- No new table needed - consolidation complete

**For Local DB (Phase 2):**
- Migrate CalendarSyncTokens data → CalendarLinks.SyncToken
- Drop CalendarSyncTokens table
- Add SyncToken column to CalendarLinks

### Notes
- This consolidation eliminates a join during sync operations
- All provider auth + sync state now in one place (CalendarLink)
- Performance improvement: one fewer table lookup per sync
- Cleaner API: `link.SyncToken` vs `db.CalendarSyncTokens.Find(...).SyncToken`

---

## 11. CHANGETRACKINGENTRY.CS ✅ DELETED - OBSOLETE PATTERN

### Deletion Decision
**Status:** Deleted - Infrastructure only, not used, design mismatch with Supabase architecture

### Why It Was Here
ChangeTrackingEntry was designed for a specific offline sync scenario:
1. User has SQL Server primary database
2. Works offline using local SQLite cache
3. On reconnect, sync journal (ChangeTrackingEntry records) replayed to server
4. Purpose: Track every insert/update/delete locally for replay

### Why It's Being Deleted
1. **Not used** - Infrastructure only, zero actual usage in codebase
2. **Design mismatch** - Built for SQL Server sync pattern, not Supabase
3. **v2 offline will be completely different:**
   - Supabase Realtime for detecting remote changes
   - PostgREST cache libraries for local persistence
   - Conflict resolution via Supabase
   - Schema already has sync metadata columns (sync_id, sync_version, sync_modified_at, sync_status)
4. **Rebuilding will be better** - When v2 needs offline, we'll understand Supabase patterns better
5. **Technical debt** - Dead code creates confusion and maintenance burden

### For v2 Offline Implementation
When implementing offline support in v2:
1. Use Supabase Realtime subscriptions for change detection
2. Use local SQLite + PostgREST wrapper for offline access
3. Leverage Supabase's native sync/conflict resolution
4. Use the existing sync metadata columns on entities (sync_id, sync_version, sync_modified_at, sync_status)
5. Build proper sync services specific to Supabase patterns

**Don't resurrect ChangeTrackingEntry** - it's the wrong pattern for cloud-first architecture.

### What Was Removed
- Entire `ChangeTrackingEntry` class (136 lines)
- `ChangeType` enum (Insert, Update, Delete)
- Audit tracking for offline changes
- DbSet reference in TrackerDbContext

### Files That Will Need Updates (Phase 2)
- Remove `DbSet<ChangeTrackingEntry>` from TrackerDbContext (if not already removed by tooling)
- Remove ChangeTrackingEntry configuration from TrackerDbContext.OnModelCreating

---

## 12. DAILYBRIEFING.CS ✅ PERFECT - BUT PHASE 2 DEPENDENCY UPDATE REQUIRED

### Status
**Status:** ✅ PERFECT - Correctly designed as runtime DTO, no schema changes needed
**⚠️ WARNING:** Has breaking dependency on OneOnOne which is being consolidated to Meeting

### What DailyBriefing Is
- **Purpose:** Runtime DTO for manager's daily dashboard display
- **Persistent:** ❌ NO - generated fresh each time, not stored in DB
- **Usage:** InsightEngine generates it, DailyBriefingDialog displays it
- **Design:** ✅ CORRECT - view model pattern, pure data collection

### Dependencies
✅ **Insight** - Need to validate separately
✅ **TeamMember** - Need to validate separately
❌ **OneOnOne** - **BREAKING** - Being consolidated to Meeting

### The Dependency Problem
**Current Code:**
```csharp
public List<OneOnOne> MeetingsToday { get; set; } = new();
```

**Phase 2 Issue:**
When OneOnOne.cs is deleted (consolidated to Meeting), this property breaks.

**Phase 2 Required Change:**
```csharp
public List<Meeting> MeetingsToday { get; set; } = new();
```

### Files That Need Phase 2 Updates
1. **DailyBriefing.cs** - Change `List<OneOnOne>` → `List<Meeting>`
2. **InsightEngine.cs** - Update `GenerateDailyBriefingAsync()` to populate from Meeting instead of OneOnOne
3. **DailyBriefingDialog.xaml/cs** - Check data binding references to OneOnOne properties

### Code Impact Areas

**InsightEngine.GenerateDailyBriefingAsync():**
```csharp
// CURRENT (will break in Phase 2):
briefing.MeetingsToday = await GetTodaysMeetingsAsync(); // Returns List<OneOnOne>

// NEEDS TO BE:
briefing.MeetingsToday = await GetTodaysMeetingsAsync(); // Returns List<Meeting>
```

**Search for usage:**
- Any code accessing `meeting.PropertyName` on items in MeetingsToday
- Check if OneOnOne-specific properties are used (e.g., Description, PrimaryAttendeeId)
- If so, migrate to Meeting equivalents

### Why Not Fixed Now
DailyBriefing is a DTO - it doesn't persist data, just carries it. We're fixing it during Phase 2 code migration when all models are already updated, so we do it all at once.

### No Functional Changes Needed Now
- Keep DailyBriefing.cs as-is during Phase 1
- Will fail compilation in Phase 2 during code migration
- Fix it then along with all other OneOnOne → Meeting migrations

---

## Notes for Phase 2 Cleanup

1. **Start with types first** - Replace all `IndividualTask`/`MeetingTask`/`TrackerTask` references before dealing with properties
2. **Fix enums next** - Status and Priority type mismatches will cause many errors
3. **ITask interface decision** - Decide whether to deprecate it, redesign it, or create adapters
4. **EF Core config** - Critical to update DbContext entity configurations for renamed/removed types
5. **Tests** - All test data builders and seed data need updating
6. **UI bindings** - Any XAML bindings to removed properties need to be redirected

