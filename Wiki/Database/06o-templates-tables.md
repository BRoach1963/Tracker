# Templates Tables (procohere schema)

This document describes the template-related tables in the `procohere` schema.

## Tables

- [meeting_templates](#meeting_templates) - Reusable meeting agenda templates

---

## meeting_templates

Reusable agenda templates for different meeting types. Templates define default duration and agenda items.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `created_by` | uuid | NO | FK to team_members who created |
| `name` | text | NO | Template name |
| `description` | text | YES | Template description |
| `meeting_type` | text | NO | Type: 'one_on_one', 'team', 'project', 'custom' |
| `default_duration` | integer | YES | Default meeting duration (minutes) |
| `default_agenda` | jsonb | YES | Default agenda items as JSON array |
| `is_system_template` | boolean | NO | True if built-in (cannot be deleted) |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("meeting_templates")]
public class MeetingTemplateDetail : BaseModel
```

**File**: `ProCohere.Avalonia/Models/MeetingTemplateDetail.cs`

### default_agenda JSON Structure

Agenda items are stored as a JSON array:

```json
[
  {
    "Id": "guid",
    "Title": "Check-in",
    "Description": "How are things going?",
    "SortOrder": 1,
    "IsOptional": false,
    "SuggestedDurationMinutes": 5
  }
]
```

Parsed into `MeetingTemplateItem` class (not a separate table).

### Meeting Types

| Value | Display Name | Description |
|-------|--------------|-------------|
| `one_on_one` | 1:1 Meeting | Manager/direct report meetings |
| `team` | Team Meeting | Team sync meetings |
| `project` | Project Review | Project-focused meetings |
| `custom` | Custom | User-defined templates |

### Helper Classes

- `MeetingTemplateItem` - Represents a single agenda item in the template
- `TemplateCategory` - Constants for meeting type values

---

## Relationships

```
organizations
    └── meeting_templates (1:N)

team_members
    └── meeting_templates (1:N via created_by)

meeting_templates
    └── meetings (used to create meetings with pre-filled agendas)
```

## Usage Notes

- System templates (`is_system_template = true`) are created during org setup
- Users can create custom templates for their organization
- Templates are applied when creating new meetings to pre-populate agenda items
