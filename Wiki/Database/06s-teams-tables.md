# Teams Tables (procohere schema)

This document describes the team-related tables in the `procohere` schema.

## Tables

- [team_members](#team_members) - People in the organization
- [teams](#teams) - Groups/departments within the organization

---

## team_members

People in an organization. May or may not have a linked user account (for external/placeholder members).

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `linked_user_id` | uuid | YES | FK to public.users (if they have an account) |
| `role_id` | uuid | NO | FK to roles |
| `first_name` | text | YES | First name |
| `last_name` | text | YES | Last name |
| `display_name` | text | YES | Preferred display name |
| `email` | text | YES | Email address |
| `job_title` | text | YES | Job title/position |
| `manager_team_member_id` | uuid | YES | FK to team_members (self-ref hierarchy) |
| `is_active` | boolean | NO | Whether actively employed |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `is_deleted` | boolean | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Models

**Base table model:**
```csharp
[Table("team_members")]
public class TeamMember : BaseModel
```
**File**: `ProCohere.Avalonia/Models/Team.cs`

**View model (with computed fields):**
```csharp
[Table("v_team_members")]
public class TeamMemberDetail : BaseModel
```
**File**: `ProCohere.Avalonia/Models/TeamMemberDetail.cs`

### Hierarchy

Team members form a reporting hierarchy via `manager_team_member_id`:

```
CEO (manager_team_member_id = null)
  ├── VP Engineering (manager = CEO)
  │     ├── Tech Lead 1 (manager = VP)
  │     └── Tech Lead 2 (manager = VP)
  └── VP Sales (manager = CEO)
        └── Sales Rep (manager = VP Sales)
```

---

## teams

Groups/departments within an organization. Supports hierarchical structure for nested teams.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `parent_team_id` | uuid | YES | FK to teams (self-ref hierarchy) |
| `name` | text | NO | Team name |
| `description` | text | YES | Team description |
| `lead_team_member_id` | uuid | YES | FK to team_members (team lead) |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("teams")]
public class Team : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Team.cs`

### Team Hierarchy

Teams can nest via `parent_team_id`:

```
Engineering (parent = null)
  ├── Backend Team (parent = Engineering)
  └── Frontend Team (parent = Engineering)
        └── Mobile Squad (parent = Frontend)
```

---

## Relationships

```
organizations
    └── team_members (1:N)
    └── teams (1:N)

team_members
    └── team_members (N:1 via manager_team_member_id - hierarchy)
    └── roles (N:1 via role_id)

teams
    └── teams (N:1 via parent_team_id - hierarchy)
    └── team_members (N:1 via lead_team_member_id)
```

## Related Tables

- `team_team_members` - Join table linking team_members to teams (many-to-many)
- `v_team_members` - View with computed hierarchy fields

## Notes

- `TeamMember` vs `TeamMemberDetail`: Use `TeamMember` for writes, `TeamMemberDetail` for reads with computed fields
- Team membership is typically managed via a join table (`team_team_members`) not shown here
- The `linked_user_id` allows tracking people who don't have app accounts (e.g., external stakeholders)
