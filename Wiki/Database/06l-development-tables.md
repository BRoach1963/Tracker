# 06l – Development Tables

This document covers the **Development** domain tables in the `procohere` schema.

---

## procohere.competencies

**Purpose**  
Organization-defined competencies/skills that team members can be assessed against.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| name | text | NO | - | Competency name |
| description | text | YES | - | Competency description |
| category | text | YES | - | Category: 'technical', 'leadership', 'communication', etc. |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Organization isolation.

**Model**: `ProCohere.Avalonia.Models.Competency`

---

## procohere.team_member_competencies

**Purpose**  
Tracks a team member's proficiency level in a specific competency.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| team_member_id | uuid | NO | - | FK → procohere.team_members.id |
| competency_id | uuid | NO | - | FK → procohere.competencies.id |
| proficiency_level | integer | YES | - | Level (e.g., 1-5) |
| assessed_by | uuid | YES | - | FK → procohere.team_members.id (assessor) |
| assessed_at | timestamptz | YES | - | When assessed |
| notes | text | YES | - | Assessment notes |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Visible to team member, assessor, and management chain.

**Model**: `ProCohere.Avalonia.Models.TeamMemberCompetency`

---

## procohere.development_plans

**Purpose**  
Career development plans for team members.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| team_member_id | uuid | NO | - | FK → procohere.team_members.id |
| title | text | NO | - | Plan title |
| description | text | YES | - | Plan description |
| status | text | NO | - | Status: 'draft', 'active', 'completed', 'cancelled' |
| start_date | date | YES | - | Plan start date |
| target_date | date | YES | - | Target completion date |
| completed_at | timestamptz | YES | - | When completed |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Visible to team member and management chain.

**Model**: `ProCohere.Avalonia.Models.DevelopmentPlan`

---

## procohere.development_plan_items

**Purpose**  
Individual action items within a development plan.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| development_plan_id | uuid | NO | - | FK → procohere.development_plans.id |
| competency_id | uuid | YES | - | FK → procohere.competencies.id (optional link) |
| title | text | NO | - | Item title |
| description | text | YES | - | Item description |
| item_type | text | YES | - | Type: 'training', 'project', 'mentoring', 'reading', etc. |
| status | text | NO | - | Status: 'not_started', 'in_progress', 'completed' |
| due_date | date | YES | - | Due date |
| completed_at | timestamptz | YES | - | When completed |
| sort_order | integer | NO | 0 | Display order |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Inherited from parent development plan visibility.

**Model**: `ProCohere.Avalonia.Models.DevelopmentPlanItem`

---

## Entity Relationships

```
competencies (1) ──────────────< team_member_competencies (many)
    │                                    │
    │                                    └──── references team_members
    │
    └──────────────────────────< development_plan_items (many, optional)

development_plans (1) ─────────< development_plan_items (many)
    │
    └──── references team_members
```

---

## Related Models

All models in `ProCohere.Avalonia/Models/Development.cs`:
- `Competency` - Organization-defined skill/competency
- `TeamMemberCompetency` - Team member's proficiency in a competency
- `DevelopmentPlan` - Career development plan
- `DevelopmentPlanItem` - Action item within a plan
