# Roles Tables (procohere schema)

This document describes the role-related tables in the `procohere` schema.

## Tables

- [roles](#roles) - Organization roles with JSONB permissions

---

## roles

Organization-level roles defining permissions for team members.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `name` | text | NO | Role name (e.g., 'Admin', 'Manager') |
| `description` | text | YES | Role description |
| `permissions` | jsonb | NO | Permission structure as JSON |
| `is_system_role` | boolean | NO | True if built-in (cannot be deleted) |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("roles")]
public class Role : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Role.cs`

### Permissions JSON Structure

Permissions are stored as JSONB for flexibility:

```json
{
  "meetings": {
    "create": true,
    "edit": true,
    "delete": false,
    "view_all": true
  },
  "goals": {
    "manage": true,
    "view_team": true
  },
  "team": {
    "manage_members": false,
    "view_org": true
  }
}
```

### System Roles

| Role | Description |
|------|-------------|
| `Admin` | Full access to all features |
| `Manager` | Manage team, create meetings/goals |
| `Member` | Standard user access |
| `Viewer` | Read-only access |

System roles have `is_system_role = true` and cannot be deleted or have their core permissions changed.

---

## Relationships

```
organizations
    └── roles (1:N)

roles
    └── team_members (1:N via role_id)
```

## Notes

- Each team_member references a role_id
- Custom roles can be created per organization
- Permissions are checked at the service layer using the JSONB structure
- No separate permissions/role_permissions tables - all stored in JSONB for simplicity
