# Chronicle Notes Domain

## Overview

The Chronicle tab provides a **personal journal** for capturing freeform notes with optional entity linking. Notes can be linked to multiple entities (team members, meetings, projects, goals, tasks, metrics, targets) via the `note_links` table using a polymorphic pattern.

### Design Philosophy

Chronicle Notes is designed as a **lightweight, personal capture tool** - not a full-featured notes application. The goal is quick capture with minimal friction, not advanced note management.

---

## Non-Goals (Intentional Limitations)

The following features are **explicitly out of scope** for Chronicle Notes:

| Feature | Reason |
|---------|--------|
| **Nested folders/hierarchy** | Adds complexity; use tags/search instead |
| **Full-text search with ranking** | Basic search is sufficient for personal journal |
| **Collaborative editing** | Notes are personal; use meetings for collaboration |
| **Version history** | Overkill for quick capture; not a document system |
| **Rich attachments** | Keep notes lightweight; link to external files |
| **Reminder/notification triggers** | Use Tasks or Meetings for reminders |
| **Export to external formats** | Can be added later if needed |

---

## Tables

### `public.notes`

Main notes table for freeform content. **Exists in production.**

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
| `is_private` | boolean | NO | true | Only visible to creator |
| `is_pinned` | boolean | NO | false | Pin to top of list |
| `pinned_at` | timestamptz | YES | | When pinned |
| `is_archived` | boolean | NO | false | Archived flag |
| `archived_at` | timestamptz | YES | | When archived |
| `ai_summary` | text | YES | | AI-generated summary (future) |
| `ai_suggested_actions` | jsonb | YES | | AI-suggested actions (future) |
| `is_deleted` | boolean | NO | false | Soft delete flag |
| `deleted_at` | timestamptz | YES | | When deleted |
| `deleted_by` | uuid | YES | | FK to users who deleted |
| `created_at` | timestamptz | NO | now() | Creation timestamp |
| `updated_at` | timestamptz | NO | now() | Last update timestamp |
| `sync_id` | uuid | YES | gen_random_uuid() | Sync identifier |
| `sync_version` | integer | YES | 1 | Sync version |
| `sync_modified_at` | timestamptz | YES | now() | Sync timestamp |
| `sync_status` | enum | YES | 'synced' | Sync status |

> **Note:** The `notes` table does NOT have individual FK columns for linked entities. All entity linking is done via the `note_links` table.

**Foreign Keys:**
- `organization_id` → `public.organizations(id)`
- `author_team_member_id` → `public.team_members(id)`
- `deleted_by` → `public.users(id)`

---

### `procohere.note_links`

Polymorphic junction table for note-entity relationships. **Exists in production.**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `id` | uuid | NO | gen_random_uuid() | Primary key |
| `organization_id` | uuid | NO | | FK to organizations |
| `note_id` | uuid | NO | | FK to notes |
| `entity_type` | note_link_entity_type | NO | | ENUM: meeting, team_member, goal, task, metric, target, project |
| `entity_id` | uuid | NO | | ID of the linked entity |
| `entity_title_snapshot` | varchar | YES | | Title at time of linking (for display without fetch) |
| `relationship_type` | text | YES | | Semantic type: mentioned, action_item, reference, follow_up |
| `sort_order` | smallint | YES | 0 | UI ordering (lower = first) |
| `created_by_team_member_id` | uuid | NO | | FK to team_members (not auth.users) |
| `created_at` | timestamptz | NO | now() | Creation timestamp |
| `is_deleted` | boolean | NO | false | Soft delete flag |
| `deleted_at` | timestamptz | YES | | When deleted |
| `deleted_by` | uuid | YES | | FK to team_members who deleted |

**Key Design Decisions:**
- `entity_type` is an ENUM, not TEXT + CHECK (extensible without ALTER TABLE locks)
- `created_by_team_member_id` references `team_members`, not `auth.users` (identity model alignment)
- `relationship_type` enables semantic meaning (AI context, filtering)
- `sort_order` enables stable UI ordering

**Indexes:**
- `ix_note_links_note` - Index on `note_id` for loading links by note
- `ix_note_links_entity` - Composite index on `(entity_type, entity_id)` for reverse lookups
- `ux_note_links_unique_active` - Unique constraint on `(note_id, entity_type, entity_id)` where `is_deleted = false`
- `ix_note_links_purge` - Index for purge operations on deleted records
- `ix_note_links_sort` - Index on `(note_id, sort_order)` for ordered queries
- `ix_note_links_relationship` - Index for relationship_type filtering

**RLS Policies:**
- `note_links_select` - Organization-scoped, see own links or links on shared notes
- `note_links_write` - Can only modify own links

---

### `public.note_templates`

Templates for commonly used note structures. **Exists in production.**

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

---

## C# Models

### Note.cs

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
    /// <summary>
    /// Entity links loaded from note_links table.
    /// Not mapped to database - populated by service layer.
    /// </summary>
    public List<NoteLink> Links { get; set; } = new();
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

    // ... AI, Soft Delete, Timestamps, Sync fields omitted for brevity

    #region Computed Properties (not mapped to DB)
    public bool HasTags => Tags != null && Tags.Count > 0;
    public bool HasLinks => Links.Count > 0;
    public int LinkCount => Links.Count;
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
        ? (Content.Length > 50 ? Content[..50] + "..." : Content)
        : Title;
    public string DisplayTimestamp { get; } // Human-friendly like "2h ago"
    #endregion
}
```

### NoteLink.cs

Located at: `Tracker/ProCohere.Avalonia/Models/NoteLink.cs`

```csharp
[Table("note_links")]
public class NoteLink : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("note_id")]
    public Guid NoteId { get; set; }

    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Column("entity_title_snapshot")]
    public string? EntityTitleSnapshot { get; set; }

    [Column("relationship_type")]
    public string? RelationshipType { get; set; }

    [Column("sort_order")]
    public short SortOrder { get; set; }

    [Column("created_by_team_member_id")]
    public Guid CreatedByTeamMemberId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Soft delete fields...
}

public static class NoteLinkEntityTypes
{
    public const string Meeting = "meeting";
    public const string TeamMember = "team_member";
    public const string Goal = "goal";
    public const string Task = "task";
    public const string Metric = "metric";
    public const string Target = "target";
    public const string Project = "project";
}

public static class NoteLinkRelationshipTypes
{
    public const string Mentioned = "mentioned";
    public const string ActionItem = "action_item";
    public const string Reference = "reference";
    public const string FollowUp = "follow_up";
}
```

---

## Service Layer

### NotesService.cs

Located at: `Tracker/ProCohere.Avalonia/Services/NotesService.cs`

**Core CRUD:**
- `GetAllNotesAsync()` - Gets all non-deleted notes, ordered by pinned then created_at
- `GetNoteByIdAsync(Guid noteId)` - Get single note by ID
- `CreateNoteAsync(Note note)` - Create new note
- `UpdateNoteAsync(Note note)` - Update existing note
- `DeleteNoteAsync(Guid noteId)` - Soft delete

**Note Links:**
- `GetLinksForNoteAsync(Guid noteId)` - Get all links for a specific note
- `GetLinksForNotesAsync(IEnumerable<Guid> noteIds)` - Batch load links for multiple notes
- `PopulateLinksAsync(IEnumerable<Note> notes)` - Populate Links collection on notes
- `AddNoteLinkAsync(Guid noteId, string entityType, Guid entityId, string? entityTitle)` - Create link
- `RemoveNoteLinkAsync(Guid linkId)` - Soft delete a link
- `GetNotesForEntityViaLinksAsync(string entityType, Guid entityId)` - Get notes linked to an entity

**Search & Filter:**
- `GetPinnedNotesAsync()` - Get pinned notes only
- `SearchNotesAsync(string query)` - Search by title or content

**Helpers:**
- `GetLinkedEntities(Note note)` - Convert Links to display info list

---

## Supported Entity Links

| Entity Type | Constant | Status |
|-------------|----------|--------|
| Team Member | `team_member` | ✅ Supported |
| Meeting | `meeting` | ✅ Supported |
| Project | `project` | ✅ Supported |
| Goal | `goal` | ✅ Supported |
| Task | `task` | ✅ Supported |
| Metric | `metric` | ✅ Supported |
| Target | `target` | ✅ Supported |

> **Design:** A single note can have **multiple links** to different entities. The UI currently shows a simple "Linked" indicator; future enhancement could display individual link badges.

---

## ViewModel Integration

### ChronicleViewModel.cs

The Chronicle tab ViewModel manages note editing with a **staging pattern** for links:

```csharp
// Staging lists for link changes (not saved until note is saved)
private readonly List<(string EntityType, Guid EntityId, string EntityTitle)> _pendingLinks = new();
private readonly List<NoteLink> _linksToRemove = new();

// Combined view of existing + pending links
public IEnumerable<(string Type, Guid Id, string Title)> EditingNoteLinks { get; }
public bool EditingNoteHasLink => EditingNoteLinks.Any();

// Link management
public void AddEntityLink(string entityType, Guid entityId, string entityTitle);
public void RemoveEntityLink(string entityType, Guid entityId);
public void ClearAllEntityLinks();

// Called after note save to persist link changes
private async Task SaveLinkChangesAsync(Guid noteId);
```

**Workflow:**
1. User edits note, adds/removes links (staged in memory)
2. User clicks Save
3. `SaveNoteAsync()` saves the note
4. `SaveLinkChangesAsync()` persists link additions and removals
5. Staging lists are cleared

---

## UI Components

### NoteCard.axaml

Displays a note in the grid with:
- Title and content preview
- Pin indicator
- Link indicator (shows when `HasLinks` is true)
- Relative timestamp

### NoteEditorFlyout.axaml

Editor flyout with:
- Title and content fields
- "Add Link" button (opens EntityPickerDialog)
- Link indicator showing when links exist
- Privacy toggle (Private/Shared)
- Pin toggle

---

## Features Status

| Feature | DB | Model | Service | UI |
|---------|----|----|---------|-----|
| Basic CRUD | ✅ | ✅ | ✅ | ✅ |
| Multi-Entity Links | ✅ | ✅ | ✅ | ✅ |
| Pin/Unpin | ✅ | ✅ | ✅ | ✅ |
| Privacy Toggle | ✅ | ✅ | ✅ | ✅ |
| Search | ✅ | ✅ | ✅ | ⏳ Planned |
| Archive | ✅ | ✅ | ⏳ | ⏳ Planned |
| Categories | ✅ | ✅ | ⏳ | ⏳ Planned |
| Tags | ✅ | ✅ | ⏳ | ⏳ Planned |
| AI Summary | ✅ | ✅ | ⏳ | ⏳ Future |
| Templates | ✅ | ⏳ | ⏳ | ⏳ Future |

---

## Guardrails & Constraints

1. **Author is Immutable:** `author_team_member_id` is set at creation and never changes.

2. **Privacy Default:** Notes default to `is_private = true`. Shared notes are visible to organization members.

3. **Title is Optional:** Notes can be created without a title; `DisplayTitle` falls back to content preview.

4. **Links are Soft Deleted:** Removing a link sets `is_deleted = true`, preserving history.

5. **Single Link Per Entity:** The unique constraint prevents duplicate links to the same entity.

6. **Title Snapshot:** `entity_title_snapshot` captures the entity's title at link time for display without fetching.

---

## Important Implementation Notes

1. **No FK Columns on Notes:** Entity linking is exclusively via `note_links` table. The `notes` table has NO `linked_*_id` columns.

2. **Links Not Auto-Loaded:** The `Links` collection must be populated explicitly via `PopulateLinksAsync()` after loading notes.

3. **Staged Link Changes:** Link additions/removals are staged in memory and only persisted when the note is saved.

4. **EntityPickerDialog:** Used to select entities for linking. Supports filtering by entity type.
