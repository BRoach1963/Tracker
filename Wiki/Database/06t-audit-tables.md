# Audit Tables (procohere schema)

This document describes the audit-related tables in the `procohere` schema.

## Tables

- [audit_log](#audit_log) - Immutable audit trail of all changes

---

## audit_log

Immutable audit trail recording all data changes in the system. This table is append-only - no updates or deletes allowed.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `actor_id` | uuid | YES | FK to public.users (who performed action) |
| `team_member_id` | uuid | YES | FK to team_members (context) |
| `action` | text | NO | Action: 'create', 'update', 'delete', etc. |
| `entity_type` | text | NO | Type of entity affected |
| `entity_id` | uuid | YES | ID of affected entity |
| `old_values` | jsonb | YES | Previous values (for updates/deletes) |
| `new_values` | jsonb | YES | New values (for creates/updates) |
| `ip_address` | inet | YES | Client IP address |
| `user_agent` | text | YES | Client user agent |
| `created_at` | timestamptz | NO | When the action occurred |

### C# Model

```csharp
[Table("audit_log")]
public class AuditLog : BaseModel
```

**File**: `ProCohere.Avalonia/Models/AuditLog.cs`

### Actions

| Action | Description |
|--------|-------------|
| `create` | New record created |
| `update` | Existing record modified |
| `delete` | Record soft-deleted |
| `restore` | Soft-deleted record restored |
| `login` | User logged in |
| `logout` | User logged out |

### Entity Types

Any table name can be an entity type:
- `meeting`, `task`, `goal`, `note`, `feedback`
- `team_member`, `team`, `project`
- `survey`, `review_cycle`, `performance_review`
- etc.

### old_values / new_values JSON Structure

For updates, captures changed fields:

```json
// old_values
{
  "status": "pending",
  "title": "Original Title"
}

// new_values
{
  "status": "completed", 
  "title": "Updated Title"
}
```

---

## Relationships

```
organizations
    └── audit_log (1:N)

public.users
    └── audit_log (1:N via actor_id)

team_members
    └── audit_log (1:N via team_member_id)
```

## Design Notes

- **Immutable**: No `updated_at`, `is_deleted`, or soft delete columns
- **Append-only**: RLS policy allows INSERT only, no UPDATE/DELETE
- **No updated_at**: Records are never modified after creation
- **Indexes**: org_id, actor_id, entity_type+entity_id, created_at for efficient querying
- **Retention**: Consider archiving old records to cold storage after N days
