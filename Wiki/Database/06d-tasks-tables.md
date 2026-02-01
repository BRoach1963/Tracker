# 06d – Tasks Domain Tables

This document covers the tasks table in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 1

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | tasks | ✅ TaskDetail.cs (fixed) |

---

## procohere.tasks

**Purpose**  
Action items and tasks. Can be standalone or linked to source entities (meetings, goals, agenda items, etc.).

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| assigned_to | uuid | YES | FK → team_members (nullable = unassigned) |
| created_by | uuid | NO | FK → team_members |
| title | text | NO | |
| description | text | YES | |
| status | text | NO | 'not_started', 'in_progress', 'completed', 'blocked' |
| priority | text | YES | 'low', 'medium', 'high' |
| due_date | timestamptz | YES | |
| completed_at | timestamptz | YES | |
| source_type | text | YES | 'meeting', 'agenda_item', 'goal', 'feedback', 'note' |
| source_id | uuid | YES | FK to source entity |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Constraints:**
```sql
-- source_type must be a valid entity type
CONSTRAINT chk_tasks_source_type
CHECK (source_type IS NULL OR source_type IN ('meeting', 'agenda_item', 'goal', 'feedback', 'note'))

-- source_type and source_id must both be set or both NULL
CONSTRAINT chk_tasks_source_pair
CHECK (
  (source_type IS NULL AND source_id IS NULL) OR
  (source_type IS NOT NULL AND source_id IS NOT NULL)
)
```

### Task Source Contract

Tasks track their origin via `source_type` and `source_id`. This enables provenance tracking and enables queries like "show me tasks spawned from goal work."

**Allowed Source Types:**

| source_type | source_id points to | Use Case |
|-------------|---------------------|----------|
| `meeting` | `meetings.id` | Task created during meeting |
| `agenda_item` | `meeting_agenda_items.id` | Task created from agenda item discussion |
| `goal` | `goals.id` | Task supporting a goal |
| `feedback` | `feedback.id` | Task from feedback conversation |
| `note` | `notes.id` | Task extracted from note |

**Recommended Pattern (Pulse/Circle):**
- Task created from a discussion should use `source_type='agenda_item'`
- This preserves the discussion context and enables Pulse to synthesize action patterns
- Tasks directly supporting goals use `source_type='goal'`

**Query Examples:**

```sql
-- Tasks spawned from goal work (direct goal tasks)
SELECT * FROM procohere.tasks
WHERE source_type = 'goal' AND source_id = '<goal_id>';

-- Tasks spawned from discussions (via agenda items)
SELECT t.* FROM procohere.tasks t
JOIN procohere.meeting_agenda_items ai ON t.source_id = ai.id
WHERE t.source_type = 'agenda_item'
  AND ai.linked_entity_type = 'goal'
  AND ai.linked_entity_id = '<goal_id>';
```

**Model:** `TaskDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- Added `organization_id` column
- `created_by` changed from `Guid?` to `Guid` (DB is NOT NULL)
- Added `updated_at` column
- Added `deleted_at` column
- Added `deleted_by` column

**RLS:** Organization isolation.
