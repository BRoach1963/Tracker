# 06j – Projects Tables

This document covers the **Projects** domain tables in the `procohere` schema.

---

## procohere.projects

**Purpose**  
Projects group related work items (goals, tasks, metrics) and team members together.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| owner_team_member_id | uuid | NO | - | FK → procohere.team_members.id |
| title | text | NO | - | Project title |
| description | text | YES | - | Project description |
| status | text | NO | - | Status: 'planning', 'active', 'on_hold', 'completed', 'cancelled' |
| start_date | date | YES | - | Project start date |
| target_date | date | YES | - | Target completion date |
| is_archived | boolean | NO | false | Whether project is archived |
| archived_at | timestamptz | YES | - | When archived |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Organization isolation. Owner and project members have access.

**Model**: `ProCohere.Avalonia.Models.Project`

---

## procohere.project_members

**Purpose**  
Join table associating team members with projects and their role on the project.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| project_id | uuid | NO | - | FK → procohere.projects.id |
| team_member_id | uuid | NO | - | FK → procohere.team_members.id |
| role | text | NO | - | Role: 'owner', 'lead', 'member', 'contributor', 'viewer' |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Inherited from parent project visibility.

**Model**: `ProCohere.Avalonia.Models.ProjectMember`

---

## procohere.project_links

**Purpose**  
Links projects to related entities (goals, tasks, metrics, meetings).

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| project_id | uuid | NO | - | FK → procohere.projects.id |
| entity_type | text | NO | - | Type: 'goal', 'task', 'metric', 'meeting' |
| entity_id | uuid | NO | - | ID of linked entity |
| entity_title_snapshot | text | YES | - | Cached title at link time |
| created_by_team_member_id | uuid | NO | - | FK → procohere.team_members.id |
| created_at | timestamptz | NO | now() | Creation timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Inherited from parent project visibility.

**Model**: `ProCohere.Avalonia.Models.ProjectLink`

---

## Entity Relationships

```
projects (1) ──────────────< project_members (many)
    │                              │
    │                              └──── references team_members
    │
    └──────────────────────< project_links (many)
                                   │
                                   └──── references goals, tasks, metrics, meetings
```

---

## Related Models

All models in `ProCohere.Avalonia/Models/Project.cs`:
- `Project` - Project definition with status and dates
- `ProjectMember` - Team member assignment to project
- `ProjectLink` - Link to related entities
