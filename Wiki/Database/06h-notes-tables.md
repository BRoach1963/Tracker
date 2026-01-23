# Notes Domain Tables

## Overview

The Notes domain handles freeform content capture with optional entity linking. Notes can be linked to entities (team members, meetings, projects, goals, tasks) via individual FK columns on the `notes` table.

---

## Tables

### `public.notes`

Main notes table for freeform content. **Already exists in production.**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | uuid | NO | gen_random_uuid() | Primary key |
| `organization_id` | uuid | NO | | FK to organizations |
| `author_team_member_id` | uuid | NO | | Team member who created the note |
| `title` | varchar | YES | | Optional title |
| `content` | text | NO | | Note content (required) |
| `content_format` | varchar | NO | 'plain' | Format: 'plain', 'markdown', 'html' |
| `category` | varchar | YES | | Optional category label |
| `tags` | jsonb | YES | | Array of tag strings |
| `linked_team_member_id` | uuid | YES | | FK to team_members (note about someone) |
| `linked_meeting_id` | uuid | YES | | FK to meetings |
| `linked_project_id` | uuid | YES | | FK to projects |
| `linked_goal_id` | uuid | YES | | FK to goals |
| `linked_task_id` | uuid | YES | | FK to tasks |
| `is_private` | boolean | NO | true | Only visible to creator |
| `is_pinned` | boolean | NO | false | Pin to top of list |
| `pinned_at` | timestamptz | YES | | When pinned |
| `is_archived` | boolean | NO | false | Archived flag |
| `archived_at` | timestamptz | YES | | When archived |
| `ai_summary` | text | YES | | AI-generated summary |
| `ai_suggested_actions` | jsonb | YES | | AI-suggested actions |
| `is_deleted` | boolean | NO | false | Soft delete flag |
| `deleted_at` | timestamptz | YES | | When deleted |
| `deleted_by` | uuid | YES | | FK to users who deleted |
| `created_at` | timestamptz | NO | now() | Creation timestamp |
| `updated_at` | timestamptz | NO | now() | Last update timestamp |
| `sync_id` | uuid | YES | gen_random_uuid() | Sync identifier |
| `sync_version` | integer | YES | 1 | Sync version |
| `sync_modified_at` | timestamptz | YES | now() | Sync timestamp |
| `sync_status` | enum | YES | 'synced' | Sync status |

**Foreign Keys:**
- `organization_id` → `public.organizations(id)`
- `author_team_member_id` → `public.team_members(id)`
- `linked_team_member_id` → `public.team_members(id)`
- `linked_meeting_id` → `public.meetings(id)`
- `linked_project_id` → `public.projects(id)`
- `linked_goal_id` → `public.goals(id)`
- `linked_task_id` → `public.tasks(id)`
- `deleted_by` → `public.users(id)`

---

### `public.note_templates`

Templates for commonly used note structures. **Already exists in production.**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | uuid | NO | gen_random_uuid() | Primary key |
| `organization_id` | uuid | NO | | FK to organizations |
| `created_by_user_id` | uuid | NO | | FK to users who created |
| `name` | varchar | NO | | Template name |
| `description` | text | YES | | Template description |
| `content_template` | text | NO | | Template content with placeholders |
| `template_type` | varchar | NO | | Type: 'meeting', 'one_on_one', 'general', etc. |
| `is_personal` | boolean | NO | true | Personal vs shared template |
| `sort_order` | integer | NO | 0 | Display order |
| `created_at` | timestamptz | NO | now() | Creation timestamp |
| `updated_at` | timestamptz | NO | now() | Last update timestamp |
| `is_deleted` | boolean | NO | false | Soft delete flag |
| `deleted_at` | timestamptz | YES | | When deleted |

**Foreign Keys:**
- `organization_id` → `public.organizations(id)`
- `created_by_user_id` → `public.users(id)`

---

## C# Models

### Note.cs (ProCohere.Avalonia)

Located at: `Tracker/ProCohere.Avalonia/Models/Note.cs`

```csharp
[Table("notes")]
public class Note : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("author_team_member_id")]
    public Guid AuthorTeamMemberId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string? Title { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("content_format")]
    public string ContentFormat { get; set; } = "plain";

    [Column("category")]
    public string? Category { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }

    #endregion

    #region Entity Links

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

    #endregion

    #region Status Flags

    [Column("is_private")]
    public bool IsPrivate { get; set; } = true;

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("pinned_at")]
    public DateTime? PinnedAt { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    #endregion

    #region AI Fields

    [Column("ai_summary")]
    public string? AiSummary { get; set; }

    [Column("ai_suggested_actions")]
    public List<string>? AiSuggestedActions { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Sync Fields

    [Column("sync_id")]
    public Guid? SyncId { get; set; }

    [Column("sync_version")]
    public int SyncVersion { get; set; } = 1;

    [Column("sync_modified_at")]
    public DateTime? SyncModifiedAt { get; set; }

    [Column("sync_status")]
    public string SyncStatus { get; set; } = "synced";

    #endregion
}
```

### LinkedEntityType.cs

Enum for valid entity link types:

```csharp
public enum LinkedEntityType
{
    None = 0,
    TeamMember,  // linked_team_member_id
    Meeting,     // linked_meeting_id
    Project,     // linked_project_id
    Goal,        // linked_goal_id
    Task,        // linked_task_id
    // Note: Metric and Target are NOT supported - no columns in DB
}
```

---

## Service Methods

### NotesService.cs

Located at: `Tracker/ProCohere.Avalonia/Services/NotesService.cs`

**Key Methods:**
- `GetAllNotesAsync()` - Gets all non-deleted notes, ordered by pinned then created_at
- `GetNoteByIdAsync(Guid noteId)` - Get single note by ID
- `GetNotesForEntityAsync(LinkedEntityType, Guid entityId)` - Get notes linked to an entity
- `GetPinnedNotesAsync()` - Get pinned notes only
- `SearchNotesAsync(string query)` - Search by title or content
- `CreateNoteAsync(Note note)` - Create new note
- `UpdateNoteAsync(Note note)` - Update existing note
- `TogglePinnedAsync(Guid noteId)` - Toggle pin status
- `DeleteNoteAsync(Guid noteId)` - Soft delete

---

## Supported Entity Links

The following entity types can be linked to notes via FK columns:

| Entity Type | Column Name | DB Status |
|-------------|-------------|-----------|
| TeamMember | `linked_team_member_id` | ✅ Exists |
| Meeting | `linked_meeting_id` | ✅ Exists |
| Project | `linked_project_id` | ✅ Exists |
| Goal | `linked_goal_id` | ✅ Exists |
| Task | `linked_task_id` | ✅ Exists |
| Metric | N/A | ❌ No column |
| Target | N/A | ❌ No column |

> **Note:** Metric and Target link columns do NOT exist in the database. The `LinkedEntityType` enum throws `NotSupportedException` for these types.

---

## Features Status

| Feature | DB Status | Model Status | Service Status |
|---------|-----------|--------------|----------------|
| Basic CRUD | ✅ Exists | ✅ Mapped | ✅ Implemented |
| Entity Links (5 types) | ✅ Exists | ✅ Mapped | ✅ Implemented |
| Pin/Unpin | ✅ Exists | ✅ Mapped | ✅ Implemented |
| Archive | ✅ Exists | ✅ Mapped | ⏳ Not yet used |
| Category | ✅ Exists | ✅ Mapped | ⏳ Not yet used |
| Tags | ✅ Exists | ✅ Mapped | ⏳ Not yet used |
| AI Summary | ✅ Exists | ✅ Mapped | ⏳ Not yet used |
| AI Actions | ✅ Exists | ✅ Mapped | ⏳ Not yet used |
| Sync columns | ✅ Exists | ✅ Mapped | ⏳ Not yet used |

---

## Important Notes

1. **Author Column:** The author is stored in `author_team_member_id` (NOT `created_by`). This links to `team_members` not `users`.

2. **Privacy Default:** Notes default to `is_private = true`. Public notes visible to organization members.

3. **Single Link Per Type:** Each FK column allows one link per entity type. A note can link to at most 5 entities (one of each type).

4. **No Metric/Target Links:** Despite the enum having Metric and Target values for UI purposes, the database has no corresponding columns. Attempts to use these will throw exceptions.

5. **Content Format:** Supports 'plain', 'markdown', 'html'. UI should render accordingly.
