# 06c – Goals & Targets Domain Tables

This document covers all tables related to goals and targets (OKRs) in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 4

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | goals | ✅ GoalDetail.cs (fixed) |
| 2 | targets | ✅ TargetDetail.cs (fixed) |
| 3 | goal_categories | ❌ No model |
| 4 | goal_templates | ❌ No model |

---

## procohere.goals

**Purpose**  
OKR-style goals owned by team members. Supports hierarchy via parent_goal_id.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| owner_id | uuid | NO | FK → team_members |
| parent_goal_id | uuid | YES | FK → goals (hierarchy) |
| category_id | uuid | YES | FK → goal_categories |
| title | text | NO | |
| description | text | YES | |
| goal_type | text | NO | 'growth', 'execution', 'operational', 'directional' |
| status | text | NO | 'not_started', 'on_track', 'at_risk', 'completed', etc. |
| priority | text | YES | 'low', 'medium', 'high' |
| start_date | date | YES | |
| due_date | date | YES | |
| completed_at | timestamptz | YES | |
| progress_percent | integer | NO | 0-100, default 0 |
| source_type | text | YES | Origin entity type (e.g., 'meeting') |
| source_id | uuid | YES | Origin entity ID |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `GoalDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- `owner_id` changed from `Guid?` to `Guid` (DB is NOT NULL)
- `progress_percent` changed from `int?` to `int` (DB is NOT NULL)
- Added `source_type` and `source_id` columns

**RLS:** Organization isolation.

---

## procohere.targets

**Purpose**  
Key Results - measurable outcomes attached to goals.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| goal_id | uuid | NO | FK → goals |
| title | text | NO | |
| description | text | YES | |
| target_type | text | NO | 'numeric', 'boolean', 'milestone' |
| target_value | numeric | YES | Target to achieve |
| current_value | numeric | NO | Current progress |
| unit | text | YES | Unit of measure |
| status | text | NO | |
| due_date | date | YES | |
| completed_at | timestamptz | YES | |
| sort_order | integer | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `TargetDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- Added `organization_id` column
- Added `updated_at` column
- Added `deleted_at` column
- Added `deleted_by` column

**RLS:** Organization isolation.

---

## procohere.goal_categories

**Purpose**  
Categories for organizing goals (e.g., "Revenue", "Customer", "Operations").

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| name | text | NO | |
| description | text | YES | |
| color | text | YES | Hex color for UI |
| sort_order | integer | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**Note:** Goals have `category_id` FK but category management UI not implemented.

**RLS:** Organization isolation.

---

## procohere.goal_templates

**Purpose**  
Reusable goal templates with pre-defined targets.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| created_by | uuid | NO | FK → team_members |
| category_id | uuid | YES | FK → goal_categories |
| name | text | NO | |
| description | text | YES | |
| goal_type | text | NO | |
| default_targets | jsonb | YES | Pre-defined target definitions |
| is_system_template | boolean | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**Note:** Template system for goals not yet implemented.

**RLS:** Organization isolation.
