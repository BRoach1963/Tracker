# ProCohere Design Decisions

**Document Created:** January 17, 2026  
**Status:** Active Development  
**Last Updated:** January 17, 2026

---

## Overview

This document captures ALL key architectural, UX, and data design decisions made for ProCohere. This is the single source of truth for design intent and should be updated as decisions evolve.

---

## Table of Contents

1. [Core Philosophy](#1-core-philosophy)
2. [Navigation & Views](#2-navigation--views)
3. [Manager Hierarchy](#3-manager-hierarchy)
4. [Visibility & Scope Rules](#4-visibility--scope-rules)
5. [Tasks Architecture](#5-tasks-architecture)
6. [Task Provenance](#6-task-provenance)
7. [Agenda Item Lifecycle](#7-agenda-item-lifecycle)
   - 7.1 [Agenda Item Deferral (Carry-Forward)](#71-agenda-item-deferral-carry-forward)
8. [Database Schema Strategy](#8-database-schema-strategy)
9. [Index Strategy](#9-index-strategy)
10. [UI Component Decisions](#10-ui-component-decisions)
11. [Future Considerations](#11-future-considerations)
12. [Key Files Reference](#12-key-files-reference)

---

## 1. Core Philosophy

### The Two Lenses

ProCohere has two primary ways to view data:

| Lens | View | Purpose | Mental Model |
|------|------|---------|--------------|
| **People** | Circle | Relationship-centric | "Who is this person? What are they working on?" |
| **Operations** | Pulse | Work-centric | "What needs to get done? What's the status?" |

### Key Principle
Same underlying data, different entry points. A task assigned to Alice appears in:
- **Circle** → Alice's profile → Tasks tab
- **Pulse** → Tasks list → Filtered/grouped view

This is NOT duplication - it's contextual access to the same data.

### MVVM Compliance
- Views contain NO business logic
- ViewModels handle UI state and commands
- Services contain business logic
- Repositories handle ALL database access
- **Never violate this. Ever.**

---

## 2. Navigation & Views

### Main Navigation Structure

```
┌─────────────────────────────────────┐
│  🏠 Briefing    (daily/weekly ops)  │
│  👥 Circle      (people & teams)    │
│  📊 Pulse       (tasks/goals/work)  │
│  ⚙️ Settings                        │
└─────────────────────────────────────┘
```

### Briefing View (formerly "Today")

**Renamed because:** "Today" was too limiting - managers need to see ahead.

**Implementation:**
- Navigation item: `NavigationItem.Briefing`
- Scope toggle: **Today** / **This Week**
- `BriefingScope` enum with `Today` and `Week` values
- Date range text updates dynamically based on scope

**Content Sections:**
- Upcoming meetings (for me + my team)
- Tasks due (for me + my team)  
- Goals at risk
- Team updates / activity

**Files:**
- `MainWindowViewModel.cs` - Navigation enum
- `MainWindow.axaml` - Navigation UI
- `TodayView.axaml` - The view (name kept for now)
- `TodayViewModel.cs` - Scope logic, data loading

---

## 3. Manager Hierarchy

### Decision: Unlimited Depth

**The data model supports unlimited hierarchy depth.** UI handles presentation constraints.

### Rationale
- Real orgs vary wildly (2 levels to 10+)
- Fixed depth = schema changes as orgs grow
- Depth is a UI/query concern, not a data constraint

### Data Model
```
team_members
├── id (uuid)
├── manager_id (uuid, self-referential FK) ← THIS IS THE KEY
└── ...
```

This is an **adjacency list** model. Simple, proven, flexible.

### Hierarchy Function (CORNERSTONE)

```sql
procohere.get_team_descendants(manager_id uuid, max_depth int DEFAULT NULL)
RETURNS TABLE (team_member_id uuid, depth int, path uuid[])
```

**This function is the canonical primitive.** Use it everywhere:
- UI queries ("show me my team")
- RPCs ("get tasks for my org")
- Future RLS policies (optional)

### UI Behavior

| View | Default | Expandable |
|------|---------|------------|
| Circle | Direct reports only | Yes - drill into any person |
| Team List | Direct reports + rollup count | Yes - expand nested |
| Briefing | All descendants rolled up | Grouped by direct report |

### Badge Display
- Show **"manages N"** on team members with reports
- N = total descendants (recursive count from function)
- Click to drill into that person's team

### What We're NOT Changing
- `team_members.manager_id` stays as-is
- No materialized path, no nested sets
- Adjacency list is the right baseline

---

## 4. Visibility & Scope Rules

### The Behavioral Contract

**A manager can see:**
1. Their own data (tasks, goals, meetings, etc.)
2. Direct reports' data
3. Indirect reports' data (all descendants)
4. Anything they're explicitly a participant in

**A manager can edit:**
1. Their own data
2. Tasks they created (regardless of assignee)
3. Goals they own
4. Meeting agendas for meetings they organize

### Enforcement Strategy

**Phase 1 (Now):** Query filters in application code
- Services use `get_team_descendants()` to scope queries
- Simpler to implement and debug
- Good enough for single-org scenarios

**Phase 2 (Later):** RLS policies
- Stronger enforcement at database level
- Required for multi-tenant or sensitive data
- Can layer on top of Phase 1

### Decision: Start with Query Filters
We're NOT doing full RLS scope today, but the hierarchy function makes it easy to add later.

---

## 5. Tasks Architecture

### Where Tasks Live

Tasks appear in **two places** with different contexts:

| Location | Scope | Use Case |
|----------|-------|----------|
| **Circle → Person → Tasks tab** | Tasks for/by that person | "What's Alice working on?" |
| **Pulse → Tasks** | All tasks, filterable | "What needs to get done?" |

### Circle Tasks Tab
- Shows: `assigned_to = person_id` OR `created_by = person_id`
- Grouped by: Assigned to them / Created by them
- Quick actions: Mark complete, reassign, edit

### Pulse Tasks View
- Full task list with filters
- Group by: Status, Priority, Due Date, Assignee
- Kanban or list view (user preference)
- Filter by: My tasks, My team's tasks, All

### Task States
```
todo → in_progress → done
         ↓
      blocked
```

### Task Priority
`low` | `medium` | `high` | `urgent`

---

## 6. Task Provenance

### The Problem
Tasks come from many sources. We need to track origin for:
- Traceability ("where did this come from?")
- Navigation ("take me to the source")
- Reporting ("how many tasks came from meetings?")

### Current Schema (Already Exists)
```sql
source_type     text,    -- 'meeting', 'agenda_item', 'goal', 'feedback', 'note'
source_id       uuid,    -- Points to source entity
```

### Integrity Rules (Implemented)

**1. CHECK constraint on source_type values:**
```sql
ALTER TABLE procohere.tasks
ADD CONSTRAINT chk_tasks_source_type 
CHECK (source_type IS NULL OR source_type IN (
    'meeting', 'agenda_item', 'goal', 'feedback', 'note'
));
```

**2. Pair constraint (both null or both set):**
```sql
ALTER TABLE procohere.tasks
ADD CONSTRAINT chk_tasks_source_pair
CHECK (
    (source_type IS NULL AND source_id IS NULL)
    OR (source_type IS NOT NULL AND source_id IS NOT NULL)
);
```

### Key Decision: "Manual" = NULL/NULL

**We dropped 'manual' from allowed values.** Manually created tasks are represented by:
- `source_type = NULL`
- `source_id = NULL`

This is cleaner because:
- `'manual'` would require a `source_id` pointing at... nothing
- NULL/NULL is semantically correct: "no source"
- One fewer enum value to maintain

### Source Types

| source_type | source_id points to | Use Case |
|-------------|---------------------|----------|
| `meeting` | `meetings.id` | Task created during meeting |
| `agenda_item` | `meeting_agenda_items.id` | Task from agenda item action |
| `goal` | `goals.id` | Task supporting a goal |
| `feedback` | `feedback.id` | Task from feedback |
| `note` | `notes.id` | Task from a note |
| `NULL` | `NULL` | Manual creation (user created directly) |

### Indexes for Provenance Queries
```sql
-- Basic provenance lookup
CREATE INDEX idx_tasks_source
ON procohere.tasks(source_type, source_id) WHERE is_deleted = false;

-- Org-scoped provenance ("tasks spawned from X in this org")
CREATE INDEX idx_tasks_org_source
ON procohere.tasks(organization_id, source_type, source_id) WHERE is_deleted = false;
```

### Optional: FK Columns (Deferred)

For even stronger integrity, could add explicit FK columns:
```sql
source_meeting_id uuid REFERENCES procohere.meetings(id),
source_agenda_item_id uuid REFERENCES procohere.meeting_agenda_items(id)
```

**Deferred** - the CHECK constraints are sufficient for now.

### UI Display
- Task detail: "Created from: [Meeting Name] > [Agenda Item]"
- Click navigates to source
- Meeting detail: "Tasks created: N"

---

## 7. Agenda Item Lifecycle

### The Problem
`is_completed boolean` is too limited. Agenda items have multiple outcomes.

### Decision: Add Status (Additive, Not Replacement)

**Migration approach:** ADD status column, DON'T remove is_completed yet.

```sql
-- Step 1: Add new column
ALTER TABLE procohere.meeting_agenda_items 
ADD COLUMN status text NOT NULL DEFAULT 'open';

-- Step 2: Backfill from existing data
UPDATE procohere.meeting_agenda_items
SET status = CASE WHEN is_completed THEN 'discussed' ELSE 'open' END;

-- Step 3: Keep is_completed for now (deprecate later)
-- DO NOT DROP YET
```

### Status Values

| Status | Description | Typical Flow |
|--------|-------------|--------------|
| `open` | Not yet discussed | Starting state |
| `discussed` | Covered, no action needed | Meeting concluded |
| `action_created` | Task was created | Links to task |
| `deferred` | Moved to future meeting | Carries forward |
| `dropped` | Won't discuss | Soft removal |

### Future: Timestamps
Will add later when needed:
- `discussed_at`
- `status_changed_at`

### Why Additive Migration?
- No breaking changes to existing code
- Can deprecate `is_completed` after UI migrates
- Lower risk, same end result

---

## 7.1 Agenda Item Deferral (Carry-Forward)

### Decision: Option A - Copy + Link (Zero Schema Changes)

We use the existing `linked_entity_type` / `linked_entity_id` columns to represent deferrals.

**Why:**
- Zero migrations required
- Supports arbitrary chain length (A → B → C)
- Supports "one deferred item becomes multiple follow-ups"
- No FK constraint fights

**Trade-off accepted:** This overloads `linked_entity_type` slightly - it normally means "what this agenda item is about", but for deferrals it means "this is a continuation of that agenda item."

### Deferral Convention

When deferring an agenda item:

| Row | Field | Value |
|-----|-------|-------|
| **Old item** | `status` | `'deferred'` |
| **New item** | `status` | `'open'` |
| **New item** | `linked_entity_type` | `'agenda_item'` |
| **New item** | `linked_entity_id` | `<old_item_id>` |

**Result:**
- Old meeting shows the item was deferred
- New meeting shows the carried item as open
- Ancestry is traceable by following the link chain

### Semantic Clarity Rule

**UI/query logic must treat `linked_entity_type='agenda_item'` as a special case** - it means "carry-forward chain", not "this item is about another agenda item."

```
linked_entity_type = 'agenda_item'  → This is a DEFERRAL (continuation)
linked_entity_type = 'task'         → This item is ABOUT that task
linked_entity_type = 'goal'         → This item is ABOUT that goal
```

### Auto Carry-Forward Logic (Hybrid Approach)

**Default behavior:** Auto-carry-forward into the next matching meeting, **only if:**
- Meeting is recurring (team sync, weekly 1:1)
- Next meeting already exists (don't invent meetings)
- Item is flagged as deferrable: `status = 'deferred'` AND not private AND not dropped

**Always allow manual override:**
- User can remove from next meeting
- User can choose a different meeting ("defer to product sync instead")
- User can split into multiple agenda items

**Rationale:** 80% of deferrals are "same meeting next week" - don't make users do clerical work.

### Future Consideration: Origin Columns (Not Now)

If semantic ambiguity becomes a problem, we could add:
```sql
-- Where this agenda item came from
origin_type text,  -- 'deferred', 'ai_suggestion', 'template', 'task_review', 'manual'
origin_id uuid     -- Points to source (deferred agenda item, template, etc.)

-- What this agenda item is about (unchanged)
linked_entity_type text,
linked_entity_id uuid
```

This separates "provenance" from "subject" cleanly. **Deferred until we feel the ambiguity biting us.**

---

## 8. Database Schema Strategy

### Guiding Principles

1. **No breaking migrations** while actively wiring UI and seed scripts
2. **Additive columns + backfills** over "replace/drop"
3. **One canonical function** for hierarchy - every query (UI/RPC/RLS) uses the same primitive
4. **RPC-first enforcement** before complex RLS
5. **Soft delete everywhere** - `is_deleted`, `deleted_at`, `deleted_by`

---

## 8.1 Execution Plan (Official)

### Phase 1: Hierarchy + Access Primitives

**Step 1: Canonical descendants function**

```sql
procohere.get_team_descendants(
    p_organization_id uuid,
    p_manager_id uuid,
    p_include_self boolean DEFAULT false
) RETURNS SETOF uuid
```

- Recursive CTE with cycle protection
- Returns team_member_ids a manager can see
- **This is THE foundation for everything:**
  - "Brian sees Troy's team"
  - Circle rollups
  - Filtering goals/tasks/feedback to "my org scope"

**Step 2 (Optional): Visible team members wrapper**

```sql
procohere.get_visible_team_members(p_manager_id uuid)
RETURNS TABLE (id, name, manager_id, depth, path)
```

Hides the CTE from app code, returns enriched data.

---

### Phase 2: Agenda Items That Spawn Work

**Step 3: Add agenda item status (ADDITIVE)**

```sql
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN status text NOT NULL DEFAULT 'open';

ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN status_changed_at timestamptz;
```

**Backfill logic:**
```sql
UPDATE procohere.meeting_agenda_items
SET status = CASE 
    WHEN is_completed = true THEN 'discussed'
    ELSE 'open'
END;
```

**Keep `is_completed` for now** - deprecate after UI migrates.

**Step 4: Indexes for agenda workflows**

```sql
-- Agenda items within a meeting
CREATE INDEX idx_agenda_items_meeting_status
ON procohere.meeting_agenda_items(organization_id, meeting_id, status, sort_order)
WHERE is_deleted = false;

-- Open items across all meetings (for "carry forward" feature)
CREATE INDEX idx_agenda_items_org_status
ON procohere.meeting_agenda_items(organization_id, status)
WHERE is_deleted = false;
```

---

### Phase 3: Tasks - Placement + Provenance Integrity

**Step 5: UI placement (product decision, not schema)**

Tasks show in two places - this is a VIEW decision, not schema:
- **Circle → Person → Tasks tab** = filtered by assignee/creator
- **Pulse → Tasks** = full task management

No schema change required. Just query filters.

**Step 6: Tighten task provenance**

**A. Constrain source_type (CHECK):**
```sql
ALTER TABLE procohere.tasks
ADD CONSTRAINT chk_tasks_source_type 
CHECK (source_type IS NULL OR source_type IN (
    'meeting', 'agenda_item', 'goal', 'feedback', 'note', 'manual'
));
```

**B. Add high-value indexes:**
```sql
-- Core task queries (Circle/Pulse filters)
CREATE INDEX idx_tasks_assignee_status_due
ON procohere.tasks(organization_id, assigned_to, status, due_date)
WHERE is_deleted = false;

-- Provenance lookup ("tasks spawned from X")
CREATE INDEX idx_tasks_source
ON procohere.tasks(organization_id, source_type, source_id)
WHERE is_deleted = false AND source_type IS NOT NULL;
```

**C. Optional FK columns (best integrity):**
```sql
ALTER TABLE procohere.tasks
ADD COLUMN source_meeting_id uuid REFERENCES procohere.meetings(id);

ALTER TABLE procohere.tasks
ADD COLUMN source_agenda_item_id uuid REFERENCES procohere.meeting_agenda_items(id);

-- Enforce only one FK set
ALTER TABLE procohere.tasks
ADD CONSTRAINT chk_tasks_single_source CHECK (
    (source_meeting_id IS NULL AND source_agenda_item_id IS NULL) OR
    (source_meeting_id IS NOT NULL AND source_agenda_item_id IS NULL) OR
    (source_meeting_id IS NULL AND source_agenda_item_id IS NOT NULL)
);
```

---

### Phase 4: Manager-of-Managers Visibility (Enforcement)

**Step 7: RPC-first enforcement**

- All RPCs use `get_team_descendants()` to scope queries
- Services filter by descendant IDs before returning data
- Easier to debug than RLS
- Good enough for single-org scenarios

**Step 8: RLS hardening (LATER)**

Once flows stabilize:
- RLS policies use helper function checking `owner_id` or "is in descendants"
- Keep functions `STABLE SECURITY DEFINER`
- Ensure indexed paths for performance

---

### Execution Order (Summary)

| Order | Change | Risk | Status |
|-------|--------|------|--------|
| 1 | `procohere.get_team_descendants()` function | None | ✅ DONE (2026-01-17) |
| 2 | `meeting_agenda_items.status` + backfill | Low | ✅ DONE (2026-01-17) |
| 3 | Indexes (agenda + tasks core filters) | None | ✅ DONE (2026-01-17) |
| 4 | `tasks.source_type` CHECK constraint | Low | ✅ DONE (2026-01-17) |
| 5 | `tasks.source_pair` CHECK constraint | Low | ✅ DONE (2026-01-17) |
| 6 | `idx_tasks_org_source` index | None | ✅ DONE (2026-01-17) |
| 7 | `idx_meeting_agenda_items_org_status` index | None | ✅ DONE (2026-01-17) |
| 8 | Optional: FK provenance columns | Low | 🔲 Deferred |
| 9 | RLS hardening | Medium | 🔲 Later |

### Already Complete
- ✅ `tasks.source_type` column
- ✅ `tasks.source_id` column
- ✅ `team_members.manager_team_member_id` (adjacency list)
- ✅ All soft delete columns
- ✅ Organization isolation via basic RLS
- ✅ `meeting_agenda_items.status` column (2026-01-17)
- ✅ `idx_tasks_source` index (2026-01-17)
- ✅ `idx_meeting_agenda_items_status` index (2026-01-17)
- ✅ `procohere.get_team_descendants()` function (2026-01-17)
- ✅ `chk_tasks_source_type` constraint - values: meeting, agenda_item, goal, feedback, note (2026-01-17)
- ✅ `chk_tasks_source_pair` constraint - both null or both set (2026-01-17)
- ✅ `idx_tasks_org_source` index - org-scoped provenance queries (2026-01-17)
- ✅ `idx_meeting_agenda_items_org_status` index - org-wide status queries (2026-01-17)
- ✅ Dropped 'manual' from source_type - manual = NULL/NULL (2026-01-17)

---

## 9. Index Strategy

### High-Value Indexes (Day-to-Day UX)

These are the queries that run constantly in Circle and Pulse:

```sql
-- Tasks: "show me tasks for this person"
CREATE INDEX idx_tasks_assignee_status_due 
ON procohere.tasks(organization_id, assigned_to, status, due_date) 
WHERE is_deleted = false;

-- Goals: "show me goals for this person"
CREATE INDEX idx_goals_owner_status_due 
ON procohere.goals(organization_id, owner_id, status, due_date) 
WHERE is_deleted = false;

-- Meetings: "show me meetings for this person"
CREATE INDEX idx_meeting_attendees_member 
ON procohere.meeting_attendees(team_member_id, meeting_id) 
WHERE is_deleted = false;
```

### Provenance Indexes (Secondary)

```sql
-- Tasks: "show me tasks from this source"
CREATE INDEX idx_tasks_source 
ON procohere.tasks(source_type, source_id) 
WHERE is_deleted = false AND source_type IS NOT NULL;

-- Tasks: "show me tasks from this meeting" (if FK added)
CREATE INDEX idx_tasks_source_meeting 
ON procohere.tasks(source_meeting_id) 
WHERE is_deleted = false AND source_meeting_id IS NOT NULL;
```

### Agenda Item Indexes

```sql
-- Agenda items by status
CREATE INDEX idx_agenda_items_status 
ON procohere.meeting_agenda_items(status) 
WHERE is_deleted = false;
```

---

## 10. UI Component Decisions

### Circle View (Person Detail)

| Tab | Content | Query Scope |
|-----|---------|-------------|
| **Overview** | Summary, role, manager, contact | Single person |
| **Team** | Direct reports with "manages N" badges | `manager_id = person` |
| **Tasks** | Tasks assigned to/by this person | `assigned_to` OR `created_by` |
| **Goals** | Goals owned by this person | `owner_id = person` |
| **Meetings** | Meetings with this person | `meeting_attendees` |
| **Notes** | Private notes about this person | `team_member_id = person` |
| **Feedback** | Feedback given/received | `from_member` OR `to_member` |

### Pulse View (Operations)

| Section | Content | Filters |
|---------|---------|---------|
| **Tasks** | Task list/kanban | Status, Priority, Assignee, Due |
| **Goals** | Goal progress | Status, Owner, Category |
| **Meetings** | Calendar/list | Date range, Type |
| **Metrics** | KPI dashboards | Owner, Category |

### Briefing View (Daily/Weekly)

| Section | Scope | Content |
|---------|-------|---------|
| **My Meetings** | User only | Today's/week's calendar |
| **Team Meetings** | All descendants | Rollup of team calendars |
| **Tasks Due** | User + team | Grouped by assignee |
| **Goals at Risk** | User + team | Status = at_risk or behind |
| **Activity** | All descendants | Recent changes, updates |

---

## 11. Future Considerations

### Not Yet Decided

| Topic | Options | Notes |
|-------|---------|-------|
| Task auto-creation | From agenda items automatically? | Could be noisy |
| Notifications | In-app? Email? Push? | Scope TBD |
| Permission granularity | Can managers edit subordinate tasks? | Currently yes |

### Decided (See Relevant Sections)

| Topic | Decision | Section |
|-------|----------|---------|
| Deferred agenda items | Copy + link (Option A), hybrid auto-carry | §7.1 |
| Task provenance | CHECK constraint on source_type | §6 |
| Hierarchy queries | `get_team_descendants()` function | §3 |

### Explicitly Deferred to Phase 2+

- Cross-organization hierarchy (multi-tenant edge case)
- External calendar sync (Google, Outlook, iCal)
- AI-powered meeting summaries
- Full RLS enforcement (vs query filters)
- Real-time collaboration / presence

### Technical Debt to Address

- [ ] Rename `TodayView.axaml` to `BriefingView.axaml`
- [ ] Consolidate `source_type` values (ensure consistency)
- [ ] Add validation for polymorphic references
- [ ] Deprecate `is_completed` after `status` is stable

---

## 12. Key Files Reference

### Navigation & Shell
| File | Purpose |
|------|---------|
| `MainWindowViewModel.cs` | Navigation state, current view |
| `MainWindow.axaml` | Shell layout, nav sidebar |

### Briefing
| File | Purpose |
|------|---------|
| `TodayView.axaml` | Briefing UI (needs rename) |
| `TodayViewModel.cs` | Scope toggle, data loading |

### Data Layer
| File | Purpose |
|------|---------|
| `Services/DashboardService.cs` | Loads briefing data |
| `Services/Supabase/SupabaseDataClient.cs` | Data client |
| `Models/Supabase/*.cs` | Entity models |

### Schema
| File | Purpose |
|------|---------|
| `Database Documentation/ProCohere Schema/PROCOHERE_SCHEMA_FINAL.sql` | Full schema |
| `New Docs/SupaBase SQL Scripts/*.sql` | Migration scripts |

---

## Revision History

| Date | Change | Author |
|------|--------|--------|
| 2026-01-17 | Initial document - captured all design decisions from ChatGPT session | - |
| 2026-01-17 | Added integrity concerns for source_type/source_id per ChatGPT review | - |
| 2026-01-17 | Changed agenda status migration to additive (not replacement) | - |
| 2026-01-17 | Added visibility/scope rules section | - |
| 2026-01-17 | Added index strategy section | - |
