# 05 – Data Models (Entity Reference)

This document is the **authoritative data model dictionary** for Tracker.Core.

All entities inherit from `AuditableEntity` and map to Supabase PostgreSQL tables.

---

## Base Class: AuditableEntity

All entities inherit these fields:

| Property | Column | Type | Purpose |
|----------|--------|------|---------|
| CreatedAt | created_at | DateTime | Record creation time (UTC) |
| UpdatedAt | updated_at | DateTime | Last modification time (UTC) |
| IsDeleted | is_deleted | bool | Soft delete flag |
| DeletedAt | deleted_at | DateTime? | Deletion time (null if not deleted) |
| DeletedBy | deleted_by | Guid? | User who deleted (null if not deleted) |

---

## Core Entities

### TeamMember
**Table:** `team_members`  
**Purpose:** People in an organization (employees, contractors, etc.)

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK → organizations |
| ManagerUserId | manager_user_id | Guid? | FK → users (current manager) |
| LinkedUserId | linked_user_id | Guid? | FK → users (if has login) |
| FirstName | first_name | string | Required |
| LastName | last_name | string | Required |
| Nickname | nickname | string? | |
| Email | email | string? | |
| Phone | phone | string? | |
| Birthday | birthday | DateTime? | |
| Location | location | string? | |
| Bio | bio | string? | |
| AvatarUrl | avatar_url | string? | |
| JobTitle | job_title | string? | |
| Department | department | string? | |
| HireDate | hire_date | DateTime? | |

**Visibility:** Self + direct/indirect reports

---

### Meeting
**Table:** `meetings`  
**Purpose:** All meeting types (1:1, team, all-hands, project, interview)

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| CreatedByUserId | created_by_user_id | Guid | FK |
| TypeString | meeting_type | string | Enum: one_on_one, team_meeting, etc. |
| Title | title | string | Required |
| Description | description | string? | |
| ManagerTeamMemberId | manager_team_member_id | Guid? | For 1:1s |
| ReportTeamMemberId | report_team_member_id | Guid? | For 1:1s |
| TeamId | team_id | Guid? | For team meetings |
| ProjectId | project_id | Guid? | For project meetings |
| ScheduledAt | scheduled_at | DateTime | |
| DurationMinutes | duration_minutes | int | |
| Status | status | string | Enum: scheduled, completed, cancelled |

**Meeting Types:**
- `OneOnOne` - 1:1 between manager and report
- `TeamMeeting` - Team-level meeting
- `AllHands` - Organization-wide
- `Project` - Project-related
- `Interview` - Interview/assessment
- `Other` - Uncategorized

**Visibility:** Owner, attendees, management chain

---

### Goal
**Table:** `goals`  
**Purpose:** Objectives/OKRs - what we want to achieve

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| OwnerTeamMemberId | owner_team_member_id | Guid? | FK |
| CreatedByUserId | created_by_user_id | Guid | FK |
| Title | title | string | What we want to achieve |
| Description | description | string? | |
| TypeString | type | string | organizational, team, personal |
| Status | status | string | not_started, in_progress, completed, etc. |
| StartDate | start_date | DateTime? | |
| EndDate | end_date | DateTime? | |
| ParentGoalId | parent_goal_id | Guid? | For goal hierarchy |
| Progress | progress | decimal | 0-100% |

**Goal Types:**
- `Organizational` - Company-wide strategic goals
- `Team` - Team objectives
- `Personal` - Individual development goals

**Visibility:** Owner + management chain

---

### Target
**Table:** `targets`  
**Purpose:** Key results/measures for goals

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| GoalId | goal_id | Guid | FK → goals |
| Title | title | string | Measurable outcome |
| CurrentValue | current_value | decimal | |
| TargetValue | target_value | decimal | |
| Unit | unit | string? | e.g., "%", "$", "count" |
| Direction | direction | string | increase, decrease, maintain |

**Visibility:** Inherited from parent goal

---

### Metric
**Table:** `metrics`  
**Purpose:** KPIs and performance measures

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| OwnerTeamMemberId | owner_team_member_id | Guid? | FK |
| Title | title | string | |
| CurrentValue | current_value | decimal | |
| TargetValue | target_value | decimal? | |
| Unit | unit | string? | |
| Frequency | frequency | string | daily, weekly, monthly, etc. |

**Visibility:** Owner OR visible team member

---

### TrackerTask
**Table:** `tasks`  
**Purpose:** Work items (standalone, project tasks, action items)

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| OwnerTeamMemberId | owner_team_member_id | Guid? | Assigned to |
| CreatedByUserId | created_by_user_id | Guid | FK |
| Title | title | string | |
| Description | description | string? | |
| Status | status | string | not_started, in_progress, completed |
| Priority | priority | string | low, medium, high, urgent |
| DueDate | due_date | DateTime? | |
| ProjectId | project_id | Guid? | FK → projects |
| GoalId | goal_id | Guid? | FK → goals |
| MeetingId | meeting_id | Guid? | FK → meetings (action item) |
| ParentTaskId | parent_task_id | Guid? | For subtasks |

**Task Context:**
- ProjectId set → Project task
- GoalId set → Goal-linked task
- MeetingId set → Meeting action item
- None set → Standalone task

**Visibility:** Owner OR assignee visible

---

## Meeting Sub-Entities

### MeetingAgendaItem
**Table:** `meeting_agenda_items`

Agenda items are **conversation containers** – not simple checklist items. Each can include shared/private context, structured talking points, and tracked outcomes.

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| MeetingId | meeting_id | Guid | FK |
| AddedBy | added_by | Guid? | FK to team_members |
| Title | title | string | Original raw title |
| DisplayTitle | display_title | string? | Optional styled title for UI |
| Description | description | string? | Legacy notes field |
| SharedContext | shared_context | string? | Context visible to all attendees |
| PrivateContext | private_context | string? | Context visible only to creator |
| TalkingPointsJson | talking_points | string? | JSONB array of talking points |
| OutcomeType | outcome_type | string? | decision, action_item, deferred, etc. |
| OutcomeSummary | outcome_summary | string? | Freeform outcome text |
| VisibilityScope | visibility_scope | string | meeting, personal, assigned |
| LinkedEntityType | linked_entity_type | string? | task, goal, metric |
| LinkedEntityId | linked_entity_id | Guid? | FK to linked entity |
| LinkedEntityTitleSnapshot | linked_entity_title_snapshot | string? | Denormalized title |
| SortOrder | sort_order | int | |
| Status | status | string | pending, in_progress, completed, deferred |
| IsCompleted | is_completed | bool | |
| CompletedAt | completed_at | DateTime? | |
| DiscussedAt | discussed_at | DateTime? | When item was actually discussed |
| IsPrivate | is_private | bool | Legacy; prefer visibility_scope |

**Computed Properties:**
- `EffectiveTitle` – Returns display_title if set, otherwise title
- `TalkingPoints` – Parsed List<TalkingPoint> from JSON
- `HasTalkingPoints` – True if any talking points exist
- `IsPersonalAgenda` – True if visibility_scope = 'personal'

### MeetingPrepItem
**Table:** `meeting_prep_items`

Prep items support AI-assisted preparation with linked entity context and carry-forward between meetings.

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| MeetingId | meeting_id | Guid | FK |
| RequestedByTeamMemberId | requested_by_team_member_id | Guid | FK |
| AssignedToTeamMemberId | assigned_to_team_member_id | Guid? | FK |
| Title | title | string | |
| Body | body | string? | Detailed description |
| SourceType | source_type | string? | manual, ai_suggested, carried_forward |
| SourceSnapshot | source_snapshot | string? | Context at creation |
| LinkedEntityType | linked_entity_type | string? | goal, metric, task, contact |
| LinkedEntityId | linked_entity_id | Guid? | FK to linked entity |
| LinkedEntityTitleSnapshot | linked_entity_title_snapshot | string? | Denormalized title |
| PrepPrompt | prep_prompt | string? | AI prompt for preparation |
| PrepResponse | prep_response | string? | AI-generated prep content |
| PreparedAt | prepared_at | DateTime? | When AI prep was generated |
| VisibilityScope | visibility_scope | string | meeting, personal, assigned |
| Status | status | string | pending, in_progress, completed |
| OverriddenStatus | overridden_status | string? | Manual override |
| DueAt | due_at | DateTime? | When prep should be ready |
| SortOrder | sort_order | int | |
| AssigneeNotes | assignee_notes | string? | Notes from assigned person |
| CarryForward | carry_forward | bool | Carry to next meeting |
| CarriedFromPrepItemId | carried_from_prep_item_id | Guid? | Lineage tracking |
| CompletedAt | completed_at | DateTime? | |
| CompletedByTeamMemberId | completed_by_team_member_id | Guid? | |

**Computed Properties:**
- `HasLinkedEntity` – True if linked_entity_type and linked_entity_id are set
- `IsPrepared` – True if prep_response exists
- `LinkedEntityTypeDisplay` – Human-readable entity type

### MeetingNote
**Table:** `meeting_notes`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| MeetingId | meeting_id | Guid | FK |
| Content | content | string | |
| NoteType | note_type | string | general, action_item, decision, etc. |

### MeetingAttendee
**Table:** `meeting_attendees`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| MeetingId | meeting_id | Guid | Composite PK |
| TeamMemberId | team_member_id | Guid | Composite PK |
| Response | response | string | pending, accepted, declined |

---

## Supporting Entities

### Project
**Table:** `projects`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OrganizationId | organization_id | Guid | FK |
| Title | title | string | |
| Status | status | string | planning, active, completed, on_hold |

### QuickNote
**Table:** `quick_notes`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| TeamMemberId | team_member_id | Guid | FK (about whom) |
| CreatedByUserId | created_by_user_id | Guid | FK (by whom) |
| Content | content | string | |
| Category | category | string? | |

### Reminder
**Table:** `reminders`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| OwnerId | owner_id | Guid | FK |
| Title | title | string | |
| DueAt | due_at | DateTime | |
| Status | status | string | pending, dismissed, completed |

### Kudos
**Table:** `kudos`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| FromMemberId | from_member_id | Guid | FK → team_members |
| ToMemberId | to_member_id | Guid | FK → team_members |
| Message | message | string | |
| Category | category | string | |
| IsPublic | is_public | bool | |

### Feedback
**Table:** `feedback`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Id | id | Guid | PK |
| FromMemberId | from_member_id | Guid | FK |
| ToMemberId | to_member_id | Guid | FK |
| Content | content | string | |
| FeedbackType | feedback_type | string | praise, constructive, etc. |

---

## Enum to String Mapping Pattern

PostgreSQL uses string enums. C# models use this pattern:

```csharp
[Column("meeting_type")]
public string TypeString { get; set; } = "one_on_one";

[NotMapped]
public MeetingType Type
{
    get => TypeString switch
    {
        "one_on_one" => MeetingType.OneOnOne,
        "team_meeting" => MeetingType.TeamMeeting,
        _ => MeetingType.Other
    };
    set => TypeString = value switch
    {
        MeetingType.OneOnOne => "one_on_one",
        MeetingType.TeamMeeting => "team_meeting",
        _ => "other"
    };
}
```

**Use `TypeString` for database operations, `Type` for application logic.**

---

## Navigation Properties

Navigation properties are marked `[NotMapped]` - Dapper doesn't populate them automatically.

```csharp
[NotMapped]
public TeamMember? Manager { get; set; }
```

Populate manually via JOINs or separate queries in repository.

---

## File Locations

All models in: `Tracker.Core/DataModels/`

| File | Entity |
|------|--------|
| TeamMember.cs | TeamMember |
| Meeting.cs | Meeting |
| Goal.cs | Goal |
| Target.cs | Target |
| Metric.cs | Metric |
| TrackerTask.cs | TrackerTask |
| Project.cs | Project |
| QuickNote.cs | QuickNote |
| Reminder.cs | Reminder |
| Kudos.cs | Kudos |
| Feedback.cs | Feedback |
| MeetingAgendaItem.cs | MeetingAgendaItem |
| MeetingNote.cs | MeetingNote |
| MeetingAttendee.cs | MeetingAttendee |

---

## Invariants

1. All entities inherit from `AuditableEntity`
2. All IDs are `Guid`
3. All FK relationships use `Guid?` (nullable) unless required
4. Enum columns stored as strings
5. Navigation properties are `[NotMapped]`
6. Column attributes match snake_case database columns

