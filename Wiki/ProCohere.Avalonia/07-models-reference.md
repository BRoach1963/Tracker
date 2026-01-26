# 07 – Models Reference

This document describes all **Models (DTOs)** in ProCohere.Avalonia.

---

## Overview

Models map to Supabase tables/views using **Supabase.Postgrest attributes**:
- `[Table("table_name")]` - specifies the database table/view
- `[PrimaryKey("id", false)]` - marks the primary key (false = not auto-generated)
- `[Column("column_name")]` - maps to database column

All models inherit from `Supabase.Postgrest.Models.BaseModel`.

---

## Model Index

### Core Entities

| Model | File | Table/View | Description |
|-------|------|------------|-------------|
| `UserProfile` | UserProfile.cs | `public.users` | User profile from public schema |
| `TeamMemberDetail` | TeamMemberDetail.cs | `v_team_members` | Team member with computed props |
| `MeetingDetail` | MeetingDetail.cs | `meetings` | Meeting with attendees |
| `GoalDetail` | GoalDetail.cs | `goals` | Goal with health/lifecycle |
| `MetricDetail` | MetricDetail.cs | `metrics` | Metric with trend |
| `TaskDetail` | TaskDetail.cs | `tasks` | Task with provenance |
| `Note` | Note.cs | `notes` | Note with entity links |
| `FeedbackDetail` | FeedbackDetail.cs | `feedback` | Feedback entry |

### Supporting Models

| Model | File | Table | Description |
|-------|------|-------|-------------|
| `MeetingAttendee` | (in MeetingDetail.cs) | `meeting_attendees` | Meeting participant |
| `MeetingAgendaItem` | (in MeetingDetail.cs) | `meeting_agenda_items` | Conversation container with context, talking points, outcomes |
| `TalkingPoint` | (in MeetingDetail.cs) | JSONB in agenda items | Structured discussion point |
| `MeetingPrepItem` | MeetingPrepItem.cs | `meeting_prep_items` | AI-assisted prep with linked entities |
| `MeetingNote` | MeetingNote.cs | `meeting_notes` | Note attached to meeting |
| `MeetingTemplateDetail` | MeetingTemplateDetail.cs | `meeting_templates` | Reusable template |
| `TargetDetail` | TargetDetail.cs | `targets` | Goal target |
| `GoalMetricAssociation` | GoalMetricAssociation.cs | `goal_metrics` | Goal-metric link |
| `AgendaItemOutcome` | AgendaItemOutcome.cs | `agenda_item_outcomes` | Outcome/action from agenda |
| `MetricHistoryEntry` | MetricHistoryEntry.cs | `metric_history` | Metric data point |

### Dialog Models

| Model | File | Table | Description |
|-------|------|-------|-------------|
| `DialogMeetingNote` | DialogMeetingNote.cs | N/A (wraps MeetingNote) | Meeting note with inline editing and tagging UI state |
| `NoteTag` | DialogMeetingNote.cs | N/A (categories stored in meeting_notes.tags) | Tag category for meeting notes |
| `DialogAgendaItem` | DialogAgendaItem.cs | N/A (wraps MeetingAgendaItem) | Agenda item with edit state |

### Session DTOs

| Model | File | Source | Description |
|-------|------|--------|-------------|
| `ProCohereUserSessionDto` | SessionDtos.cs | RPC | User session after login |
| `PublicUserDto` | SessionDtos.cs | RPC | Safe user info |
| `TeamMemberDto` | SessionDtos.cs | RPC | Team member from session |
| `RoleDto` | SessionDtos.cs | RPC | User's role |

### Enums

| Enum | File | Description |
|------|------|-------------|
| `GoalHealth` | GoalHealth.cs | on_track, needs_attention, at_risk, reframing_needed |
| `GoalLifecycle` | GoalLifecycle.cs | active, evolving, paused, superseded, retired |
| `GoalScope` | GoalScope.cs | individual, team, shared |
| `GoalType` | GoalType.cs | growth, execution, operational, directional |
| `GoalVisibility` | GoalVisibility.cs | private, team, shared |
| `MetricTrend` | MetricTrend.cs | trending_up, stable, trending_down, variable, unknown |
| `MetricLifecycle` | MetricLifecycle.cs | active, dormant, retired |
| `MetricScope` | MetricScope.cs | individual, team, organization |
| `MetricSource` | MetricSource.cs | system, survey, manual |
| `LinkedEntityType` | LinkedEntityType.cs | team_member, meeting, project, goal, task, metric, target |
| `NoteCategory` | NoteCategory.cs | general, meeting, project, feedback, other |

---

## UserProfile

**Table**: `public.users`

```csharp
[Table("users")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }  // Same as auth.users.id

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; }

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("job_title")]
    public string? JobTitle { get; set; }

    [Column("company")]
    public string? Company { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    [Column("timezone")]
    public string Timezone { get; set; } = "UTC";

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("preferences")]
    public JsonElement? Preferences { get; set; }

    [Column("notification_settings")]
    public JsonElement? NotificationSettings { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

---

## TeamMemberDetail

**View**: `v_team_members` (joins team_members with users)

```csharp
[Table("v_team_members")]
public class TeamMemberDetail : BaseModel, INotifyPropertyChanged
{
    // Identity
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    // Basic info
    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("job_title")]
    public string? JobTitle { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    // From users table via view
    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    // Hierarchy (from DB)
    [Column("manager_user_id")]
    public Guid? ManagerUserId { get; set; }

    [Column("manager_team_member_id")]
    public Guid? ManagerTeamMemberId { get; set; }

    // Hierarchy (computed by service from RPC)
    public int HierarchyDepth { get; set; }      // 0=self, 1=direct, 2+=skip
    public int DisplayDepth { get; set; }        // For tree indentation
    public string Relation { get; set; }         // self, manager, peer, direct, descendant
    public int DirectReportCount { get; set; }   // Visible direct reports
    public int TotalDescendantCount { get; set; }
    public string ManagerName { get; set; }

    // Computed
    public bool IsManager => DirectReportCount > 0;
    public string FullName => $"{FirstName} {LastName}";
    public string Initials => GetInitials(FirstName, LastName);
}
```

---

## MeetingDetail

**Table**: `meetings`

```csharp
[Table("meetings")]
public class MeetingDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("meeting_type")]
    public string MeetingType { get; set; }  // one_on_one, team, project, etc.

    [Column("status")]
    public string Status { get; set; }  // scheduled, in_progress, completed, cancelled

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Column("video_link")]
    public string? VideoLink { get; set; }

    [Column("recurrence_rule")]
    public string? RecurrenceRule { get; set; }

    [Column("parent_meeting_id")]
    public Guid? ParentMeetingId { get; set; }

    [Column("meeting_series_id")]
    public Guid? MeetingSeriesId { get; set; }

    [Column("created_by")]
    public Guid CreatedByTeamMemberId { get; set; }

    // Non-DB properties (populated by service)
    public List<MeetingAttendee> Attendees { get; set; }
    public List<MeetingAgendaItem> AgendaItems { get; set; }
    public List<MeetingPrepItem> PrepItems { get; set; }
    public Guid? CurrentUserTeamMemberId { get; set; }

    // Computed
    public bool IsOwnedByCurrentUser => CurrentUserTeamMemberId == CreatedByTeamMemberId;
    public bool HasAgendaItems => AgendaItems.Count > 0;
}
```

---

## MeetingAgendaItem

**Table**: `meeting_agenda_items`  
**File**: `MeetingDetail.cs` (nested class)

Agenda items are **conversation containers** – not simple checklist items. Each can include shared/private context, structured talking points, and tracked outcomes.

```csharp
[Table("meeting_agenda_items")]
public class MeetingAgendaItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("added_by")]
    public Guid? AddedBy { get; set; }

    // Title System
    [Column("title")]
    public string Title { get; set; }  // Original raw title

    [Column("display_title")]
    public string? DisplayTitle { get; set; }  // Optional styled title

    [Column("description")]
    public string? Description { get; set; }  // Legacy notes

    // Context Fields
    [Column("shared_context")]
    public string? SharedContext { get; set; }  // Visible to all attendees

    [Column("private_context")]
    public string? PrivateContext { get; set; }  // Visible only to creator

    // Talking Points (JSONB)
    [Column("talking_points")]
    public string? TalkingPointsJson { get; set; }  // Array of TalkingPoint

    // Outcomes
    [Column("outcome_type")]
    public string? OutcomeType { get; set; }  // decision, action_item, deferred, etc.

    [Column("outcome_summary")]
    public string? OutcomeSummary { get; set; }

    // Visibility
    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = "meeting";  // meeting, personal, assigned

    // Linked Entity
    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    [Column("linked_entity_title_snapshot")]
    public string? LinkedEntityTitleSnapshot { get; set; }

    // Status & Sort
    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("is_completed")]
    public bool IsCompleted { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("discussed_at")]
    public DateTime? DiscussedAt { get; set; }

    [Column("is_private")]
    public bool IsPrivate { get; set; }  // Legacy; prefer visibility_scope

    // Computed Properties
    public string EffectiveTitle => !string.IsNullOrWhiteSpace(DisplayTitle) ? DisplayTitle : Title;
    public List<TalkingPoint> TalkingPoints { get; }  // Parsed from JSON
    public bool HasTalkingPoints => TalkingPoints?.Any() == true;
    public bool IsPersonalAgenda => VisibilityScope == "personal";
    public string OutcomeTypeDisplay { get; }  // Human-readable outcome
}
```

### Outcome Types
| Value | Display | Meaning |
|-------|---------|---------|
| `decision` | Decision | A decision was made |
| `action_item` | Action Item | Follow-up task created |
| `deferred` | Deferred | Moved to future meeting |
| `information_shared` | Info Shared | FYI, no action needed |
| `no_action_needed` | No Action | Discussed, concluded |

---

## TalkingPoint

**Storage**: JSONB array in `meeting_agenda_items.talking_points`  
**File**: `MeetingDetail.cs`

```csharp
public class TalkingPoint
{
    [JsonPropertyName("id")]
    public string Id { get; set; }  // UUID string

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("discussed")]
    public bool Discussed { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}
```

### JSON Example
```json
[
  {"id": "550e8400-...", "text": "Review Q1 goals", "discussed": true, "order": 0},
  {"id": "6ba7b810-...", "text": "Discuss blockers", "discussed": false, "order": 1}
]
```

---

## MeetingPrepItem

**Table**: `meeting_prep_items`  
**File**: `MeetingPrepItem.cs`

Prep items support AI-assisted preparation with linked entity context and carry-forward between meetings.

```csharp
[Table("meeting_prep_items")]
public class MeetingPrepItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("requested_by_team_member_id")]
    public Guid RequestedByTeamMemberId { get; set; }

    [Column("assigned_to_team_member_id")]
    public Guid? AssignedToTeamMemberId { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("body")]
    public string? Body { get; set; }

    // Source Tracking
    [Column("source_type")]
    public string? SourceType { get; set; }  // manual, ai_suggested, carried_forward

    [Column("source_snapshot")]
    public string? SourceSnapshot { get; set; }

    // Linked Entity
    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }  // goal, metric, task, contact

    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    [Column("linked_entity_title_snapshot")]
    public string? LinkedEntityTitleSnapshot { get; set; }

    // AI Preparation
    [Column("prep_prompt")]
    public string? PrepPrompt { get; set; }

    [Column("prep_response")]
    public string? PrepResponse { get; set; }

    [Column("prepared_at")]
    public DateTime? PreparedAt { get; set; }

    // Visibility & Status
    [Column("visibility_scope")]
    public string VisibilityScope { get; set; } = "meeting";

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("overridden_status")]
    public string? OverriddenStatus { get; set; }

    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("assignee_notes")]
    public string? AssigneeNotes { get; set; }

    // Carry Forward
    [Column("carry_forward")]
    public bool CarryForward { get; set; }

    [Column("carried_from_prep_item_id")]
    public Guid? CarriedFromPrepItemId { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("completed_by_team_member_id")]
    public Guid? CompletedByTeamMemberId { get; set; }

    // Computed Properties
    public bool HasLinkedEntity => !string.IsNullOrEmpty(LinkedEntityType) && LinkedEntityId.HasValue;
    public bool IsPrepared => !string.IsNullOrEmpty(PrepResponse);
    public string LinkedEntityTypeDisplay { get; }  // Human-readable type
    public string LinkedEntityIcon { get; }  // Icon character
    public string PreparedStatusDisplay { get; }  // Prepared/Not Prepared
}
```

---

## MeetingNote

**Table**: `meeting_notes`

**File**: `MeetingNote.cs`

```csharp
[Table("meeting_notes")]
public class MeetingNote : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("author_id")]
    public Guid AuthorId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }  // Tag categories: ["action", "decision", etc.]

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Computed Properties
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);
    public string ContentPreview { get; }  // First 100 chars
    public string LastUpdatedDisplay { get; }  // Formatted timestamp
}
```

---

## DialogMeetingNote

**File**: `Models/Dialogs/DialogMeetingNote.cs`

UI wrapper model for meeting notes with inline editing support and tagging.

```csharp
public partial class DialogMeetingNote : ObservableObject
{
    // Identity
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid AuthorId { get; set; }
    
    // State
    public bool IsDirty { get; set; }      // Has unsaved changes
    public bool IsEditing { get; set; }    // Currently being edited
    
    // Content
    public string Content { get; set; }     // Actual content
    public string EditContent { get; set; } // Temp content while editing
    
    // Visibility
    public bool IsShared { get; set; }      // false = private to author
    
    // Tags
    public List<NoteTag> Tags { get; set; } // Assigned tags
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? AuthorName { get; set; }
    
    // Computed Properties
    public bool HasContent { get; }
    public bool HasTags { get; }
    public string TimestampDisplay { get; }
    public string VisibilityIcon { get; }      // Lock/Unlock icon
    public string VisibilityTooltip { get; }   // "Private"/"Shared"
    
    // Factory Methods
    public static DialogMeetingNote FromMeetingNote(MeetingNote note, string? authorName = null);
    public static DialogMeetingNote CreateNew(Guid meetingId, Guid authorId, ...);
    
    // Conversion
    public List<string> GetTagCategories();  // Convert Tags to category strings for DB
}
```

### Factory Methods

**FromMeetingNote**: Convert database model to UI model
```csharp
public static DialogMeetingNote FromMeetingNote(MeetingNote note, string? authorName = null)
{
    return new DialogMeetingNote
    {
        Id = note.Id,
        MeetingId = note.MeetingId,
        Content = note.Content,
        Tags = TagsFromCategories(note.Tags),  // Convert ["action"] → [NoteTag]
        IsEditing = false,
        IsDirty = false
    };
}
```

**CreateNew**: Create a new note in edit mode
```csharp
public static DialogMeetingNote CreateNew(Guid meetingId, Guid authorId, ...)
{
    return new DialogMeetingNote
    {
        Id = Guid.Empty,  // New note
        IsEditing = true,
        IsDirty = true
    };
}
```

---

## NoteTag

**File**: `Models/Dialogs/DialogMeetingNote.cs` (nested class)

Tag category for meeting notes. Uses predefined standard tags.

```csharp
public class NoteTag
{
    public Guid Id { get; set; }
    public string Name { get; set; }      // Display name: "Action Item"
    public string Category { get; set; }  // DB key: "action"
    public string Color { get; set; }     // Hex color: "#EF4444"
    public string Icon { get; set; }      // SVG path data
    
    // Standard predefined tags
    public static readonly List<NoteTag> StandardTags = new()
    {
        new NoteTag { Name = "Action Item", Category = "action", Color = "#EF4444" },
        new NoteTag { Name = "Decision", Category = "decision", Color = "#10B981" },
        new NoteTag { Name = "Question", Category = "question", Color = "#F59E0B" },
        new NoteTag { Name = "Follow-up", Category = "followup", Color = "#8B5CF6" },
        new NoteTag { Name = "Blocker", Category = "blocker", Color = "#DC2626" },
        new NoteTag { Name = "Idea", Category = "idea", Color = "#3B82F6" },
        new NoteTag { Name = "Risk", Category = "risk", Color = "#F97316" }
    };
}
```

### Tag Color Reference

| Category | Display Name | Color | Hex |
|----------|-------------|-------|-----|
| `action` | Action Item | 🔴 Red | #EF4444 |
| `decision` | Decision | 🟢 Green | #10B981 |
| `question` | Question | 🟡 Amber | #F59E0B |
| `followup` | Follow-up | 🟣 Purple | #8B5CF6 |
| `blocker` | Blocker | 🔴 Dark Red | #DC2626 |
| `idea` | Idea | 🔵 Blue | #3B82F6 |
| `risk` | Risk | 🟠 Orange | #F97316 |

---

## GoalDetail

**Table**: `goals`

### Philosophy
> "Goals express intent, Metrics observe reality, Humans decide."
> NO progress bars, percentages, or red/yellow/green status indicators.

```csharp
[Table("goals")]
public class GoalDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("owner_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // Goal Type
    [Column("goal_type")]
    public string? GoalTypeValue { get; set; }
    public GoalType GoalType { get; set; }  // growth, execution, operational, directional

    // Time Period
    [Column("time_period")]
    public string? TimePeriod { get; set; }

    [Column("year")]
    public int? Year { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    // Health System (replaces status)
    [Column("health")]
    public string? HealthValue { get; set; }
    public GoalHealth Health { get; set; }

    [Column("health_reason")]
    public string? HealthReason { get; set; }  // "What has changed?"

    // Lifecycle
    [Column("lifecycle")]
    public string? LifecycleValue { get; set; }
    public GoalLifecycle Lifecycle { get; set; }

    [Column("lifecycle_reason")]
    public string? LifecycleReason { get; set; }

    [Column("superseded_by_id")]
    public Guid? SupersededById { get; set; }

    // Legacy
    [Column("status")]
    public string Status { get; set; }  // Kept for backward compatibility
}
```

### Health Values
| Value | Display | Meaning |
|-------|---------|---------|
| `on_track` | On Track | Making expected progress |
| `needs_attention` | Needs Attention | Requires focus |
| `at_risk` | At Risk | May not succeed |
| `reframing_needed` | Reframing Needed | Needs rethinking |

### Lifecycle Values
| Value | Display | Meaning |
|-------|---------|---------|
| `active` | Active | Currently being pursued |
| `evolving` | Evolving | Being refined |
| `paused` | Paused | Temporarily on hold |
| `superseded` | Superseded | Replaced by another goal |
| `retired` | Retired | No longer relevant |

---

## MetricDetail

**Table**: `metrics`

### Philosophy
> "Metrics are signals that tell a story, NOT targets to chase."
> Display DIRECTIONAL TRENDS (↗ → ↘), not numeric values.

```csharp
[Table("metrics")]
public class MetricDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("owner_team_member_id")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    // Values (hidden by default in UI)
    [Column("current_value")]
    public decimal CurrentValue { get; set; }

    [Column("target_value")]
    public decimal? TargetValue { get; set; }

    [Column("baseline_value")]
    public decimal? BaselineValue { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    // Direction & Trend
    [Column("target_direction")]
    public string? TargetDirection { get; set; }  // higher_is_better, lower_is_better, neutral

    public MetricTrend Trend { get; set; }  // Computed, not stored

    // Source & Scope
    [Column("source")]
    public string? Source { get; set; }  // system, survey, manual

    [Column("scope")]
    public string? Scope { get; set; }  // individual, team, organization

    // Lifecycle
    [Column("lifecycle")]
    public string Lifecycle { get; set; }  // active, dormant, retired
}
```

### Trend Values
| Value | Arrow | Meaning |
|-------|-------|---------|
| `trending_up` | ↗ | Improving |
| `stable` | → | No change |
| `trending_down` | ↘ | Declining |
| `variable` | ↔ | Fluctuating |
| `unknown` | ? | Insufficient data |

---

## TaskDetail

**Table**: `tasks`

```csharp
[Table("tasks")]
public class TaskDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; }  // not_started, in_progress, completed, blocked

    [Column("priority")]
    public string? Priority { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("assigned_to")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("created_by")]
    public Guid? CreatedByTeamMemberId { get; set; }

    // Provenance
    [Column("source_type")]
    public string? SourceType { get; set; }  // meeting, agenda_item, goal, feedback, note

    [Column("source_id")]
    public Guid? SourceId { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Computed
    public bool IsOverdue => DueDate < DateTime.UtcNow && Status != "completed";
    public bool IsCompleted => Status == "completed";
    public bool HasSource => !string.IsNullOrEmpty(SourceType) && SourceId.HasValue;
}
```

---

## Note

**Table**: `notes`

```csharp
[Table("notes")]
public class Note : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("author_team_member_id")]
    public Guid AuthorTeamMemberId { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("content")]
    public string Content { get; set; }

    [Column("content_format")]
    public string ContentFormat { get; set; }  // plain, markdown

    // Entity Links (all nullable)
    [Column("linked_team_member_id")]
    public Guid? LinkedTeamMemberId { get; set; }

    [Column("linked_meeting_id")]
    public Guid? LinkedMeetingId { get; set; }

    [Column("linked_project_id")]
    public Guid? LinkedProjectId { get; set; }

    [Column("linked_goal_id")]
    public Guid? LinkedGoalId { get; set; }

    [Column("linked_task_id")]
    public Guid? LinkedTaskId { get; set; }

    [Column("linked_metric_id")]
    public Guid? LinkedMetricId { get; set; }

    // Organization
    [Column("category")]
    public string? Category { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }  // JSONB array

    // Status
    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; }
}
```

---

## Session DTOs

### ProCohereUserSessionDto
Returned by `get_user_session` RPC after login:

```csharp
public sealed class ProCohereUserSessionDto
{
    public bool HasAccess { get; set; }
    public string? Error { get; set; }
    public PublicUserDto? User { get; set; }
    public TeamMemberDto? TeamMember { get; set; }
    public RoleDto? Role { get; set; }
}
```

### TeamMemberDto
```csharp
public sealed class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Email { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public Guid? RoleId { get; set; }
    public Guid? ManagerTeamMemberId { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
```

### RoleDto
```csharp
public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // admin, manager, user
}
```

---

## Key Patterns

### DB Column ↔ C# Property
```csharp
[Column("snake_case_column")]
public string PascalCaseProperty { get; set; }
```

### Nullable Foreign Keys
All foreign keys are nullable Guid:
```csharp
[Column("owner_id")]
public Guid? OwnerTeamMemberId { get; set; }
```

### Computed Properties
Non-DB properties computed in C#:
```csharp
// Not [Column] - computed
public bool IsOverdue => DueDate < DateTime.UtcNow && Status != "completed";
```

### Enum Parsing
String columns with enum helpers:
```csharp
[Column("health")]
public string? HealthValue { get; set; }

public GoalHealth Health
{
    get => GoalHealthExtensions.ParseGoalHealth(HealthValue);
    set => HealthValue = value.ToDbString();
}
```

---

## Invariants

1. **All IDs are GUIDs** - never int
2. **Soft delete only** - `is_deleted`, `deleted_at`, `deleted_by`
3. **Audit columns** - `created_at`, `updated_at` on all tables
4. **Organization scoped** - all data has `organization_id`
5. **RLS enforced** - models only return permitted data

