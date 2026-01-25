# Teams Tables (procohere schema)

This document describes the team-related tables in the `procohere` schema.

## Tables

- [team_members](#team_members) - People in the organization
- [teams](#teams) - Groups/departments within the organization
- [team_memberships](#team_memberships) - Many-to-many join between teams and team members

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

## team_memberships

**Many-to-many join table** linking team members to teams. Enables a team member to belong to multiple teams, and a team to have multiple members.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `team_id` | uuid | NO | FK to teams |
| `team_member_id` | uuid | NO | FK to team_members |
| `role` | text | NO | Membership role: 'member', 'lead', or 'viewer' |
| `is_deleted` | boolean | NO | Soft delete flag (default: false) |
| `created_at` | timestamptz | NO | Record creation time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### Constraints

- **Unique active membership**: Only one active (non-deleted) membership per `(team_id, team_member_id)`
- **Role check**: `role IN ('member', 'lead', 'viewer')`

### C# Model

```csharp
[Table("team_memberships")]
public class TeamMembership : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Team.cs`

### Service

```csharp
TeamMembershipService.Instance
```

**Key Methods:**
- `GetMyTeamsAsync()` - Teams current user belongs to
- `GetTeamsForMemberAsync(Guid teamMemberId)` - Teams for any member
- `GetMembersForTeamAsync(Guid teamId)` - Members of a team
- `AddMemberToTeamAsync(Guid teamId, Guid teamMemberId, string role)` - Add member
- `RemoveMemberFromTeamAsync(Guid teamId, Guid teamMemberId)` - Soft delete membership
- `UpdateMembershipRoleAsync(Guid teamId, Guid teamMemberId, string newRole)` - Change role

**File**: `ProCohere.Avalonia/Services/TeamMembershipService.cs`

### Usage Example

```csharp
// Get teams the current user belongs to
var myTeams = await TeamMembershipService.Instance.GetMyTeamsAsync();

// Get members of a specific team
var members = await TeamMembershipService.Instance.GetMembersForTeamAsync(teamId);

// Add someone to a team as a member
await TeamMembershipService.Instance.AddMemberToTeamAsync(teamId, memberId, "member");

// Get team member details for meeting attendee auto-population
var attendees = await TeamMembershipService.Instance.GetTeamMemberDetailsAsync(teamId);
```

---

## Relationships

```
organizations
    └── team_members (1:N)
    └── teams (1:N)
    └── team_memberships (1:N)

team_members
    └── team_members (N:1 via manager_team_member_id - hierarchy)
    └── roles (N:1 via role_id)
    └── team_memberships (1:N) - teams this member belongs to

teams
    └── teams (N:1 via parent_team_id - hierarchy)
    └── team_members (N:1 via lead_team_member_id)
    └── team_memberships (1:N) - members of this team
```

## Important Notes

### Teams vs Manager Hierarchy
- **Teams** = named working groups (Platform, Legal, Store Ops)
- **Manager hierarchy** = reporting chain via `team_members.manager_team_member_id`
- These are **independent** - a team member's manager is separate from their team memberships

### Teams vs Projects
- **Teams** = persistent organizational groups
- **Projects** = time-scoped work containers
- A project can optionally link to a team (future enhancement)

### Team Names
- Team names are stored **only** in `teams.name`
- Don't store team names elsewhere (not in team_members, not in meetings)
- Use the `team_memberships` join table to resolve membership

### Meeting Attendees vs Team Membership
- **Attendees** = meeting-scoped (who is invited to THIS meeting)
- **Team membership** = persistent (who belongs to the team)
- For team meetings, auto-populate attendees FROM team membership, but allow manual edits

## Related Tables

- `v_team_members` - View with computed hierarchy fields
