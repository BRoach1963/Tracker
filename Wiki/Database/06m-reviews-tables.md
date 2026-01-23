# 06m – Reviews Tables

This document covers the **Reviews** domain tables in the `procohere` schema.

---

## procohere.review_cycles

**Purpose**  
Defines performance review cycles (e.g., annual, quarterly) with time periods.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| name | text | NO | - | Cycle name (e.g., "2026 Q1 Review") |
| description | text | YES | - | Cycle description |
| cycle_type | text | NO | - | Type: 'annual', 'semi_annual', 'quarterly' |
| status | text | NO | - | Status: 'draft', 'active', 'completed', 'cancelled' |
| start_date | date | NO | - | Period start date |
| end_date | date | NO | - | Period end date |
| review_start_date | date | YES | - | When reviews can begin |
| review_end_date | date | YES | - | Deadline for reviews |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Organization isolation.

**Model**: `ProCohere.Avalonia.Models.ReviewCycle`

---

## procohere.performance_reviews

**Purpose**  
Individual performance reviews within a cycle.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| review_cycle_id | uuid | NO | - | FK → procohere.review_cycles.id |
| reviewee_id | uuid | NO | - | FK → procohere.team_members.id (person being reviewed) |
| reviewer_id | uuid | NO | - | FK → procohere.team_members.id (person giving review) |
| review_type | text | NO | - | Type: 'manager', 'self', 'peer', '360' |
| status | text | NO | - | Status: 'pending', 'in_progress', 'submitted', 'acknowledged' |
| overall_rating | integer | YES | - | Overall rating (e.g., 1-5) |
| strengths | text | YES | - | Strengths feedback |
| areas_for_improvement | text | YES | - | Areas for improvement |
| goals_for_next_period | text | YES | - | Goals for next period |
| additional_comments | text | YES | - | Additional comments |
| submitted_at | timestamptz | YES | - | When review was submitted |
| acknowledged_at | timestamptz | YES | - | When reviewee acknowledged |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Visible to reviewer, reviewee, and management chain.

**Model**: `ProCohere.Avalonia.Models.PerformanceReview`

---

## Entity Relationships

```
review_cycles (1) ──────────────< performance_reviews (many)
                                        │
                                        ├──── reviewee_id → team_members
                                        └──── reviewer_id → team_members
```

---

## Related Models

All models in `ProCohere.Avalonia/Models/Review.cs`:
- `ReviewCycle` - Performance review cycle definition
- `PerformanceReview` - Individual performance review
