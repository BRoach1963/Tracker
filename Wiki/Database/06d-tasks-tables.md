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

**Model:** `TaskDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- Added `organization_id` column
- `created_by` changed from `Guid?` to `Guid` (DB is NOT NULL)
- Added `updated_at` column
- Added `deleted_at` column
- Added `deleted_by` column

**Provenance:** Tasks track where they came from via `source_type` and `source_id`. This enables showing "Created from Meeting: Weekly 1:1" in the UI.

**RLS:** Organization isolation.
