# Utility Tables (procohere schema)

This document describes utility/shared tables in the `procohere` schema.

## Tables

- [tags](#tags) - Organization-level tags for categorization
- [attachments](#attachments) - File attachments linked to entities

---

## tags

Organization-level tags for categorizing various entities.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `name` | text | NO | Tag name |
| `color` | text | YES | Color hex code (e.g., '#FF5733') |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("tags")]
public class Tag : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Utility.cs`

---

## attachments

File attachments linked to various entities (meetings, tasks, notes, goals, etc.). Uses polymorphic association via entity_type/entity_id.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `uploaded_by` | uuid | NO | FK to team_members who uploaded |
| `entity_type` | text | NO | Type: 'meeting', 'task', 'note', 'goal', etc. |
| `entity_id` | uuid | NO | ID of the linked entity |
| `file_name` | text | NO | Original filename |
| `file_size` | bigint | YES | Size in bytes |
| `mime_type` | text | YES | MIME type (e.g., 'application/pdf') |
| `storage_path` | text | NO | Path in Supabase Storage bucket |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("attachments")]
public class Attachment : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Utility.cs`

### Computed Properties

- `FileSizeDisplay` - Human-readable file size (e.g., "2.5 MB")
- `FileExtension` - Extension from filename (e.g., "PDF")
- `IsImage` - Whether the file is an image based on MIME type

### Entity Types

| Value | Description |
|-------|-------------|
| `meeting` | Attached to a meeting |
| `task` | Attached to a task |
| `note` | Attached to a note |
| `goal` | Attached to a goal |
| `feedback` | Attached to feedback |

---

## Relationships

```
organizations
    └── tags (1:N)
    └── attachments (1:N)

team_members
    └── attachments (1:N via uploaded_by)

attachments
    └── meetings (via entity_type='meeting', entity_id)
    └── tasks (via entity_type='task', entity_id)
    └── notes (via entity_type='note', entity_id)
    └── goals (via entity_type='goal', entity_id)
    └── ... (polymorphic)
```

## Storage Notes

- Attachments reference files stored in Supabase Storage
- `storage_path` contains the bucket path (e.g., `org-{id}/attachments/{file-id}`)
- RLS policies restrict access to organization members
- Files should be deleted from storage when attachment record is hard-deleted
