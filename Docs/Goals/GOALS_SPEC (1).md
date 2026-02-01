
# Goals – Specification (with DB/Wiki Update Instructions)

## Purpose
Goals are stable declarations of intent, evaluated through metrics and discussed through meetings.
In Circle, goals are intent-first and their health is derived, not manually declared.

This document includes:
- Product semantics (Briefing / Me / Circle)
- Explicit engineering instructions for Claude
- Required SQL updates
- Wiki documentation update checklist

---

## Definitions

### Goal
A human-readable statement of desired outcome, owned by one person, supported by metrics.

### Metric
A recurring signal that provides evidence about goal progress.

### Task
An action taken in response to a metric insight or goal discussion.

---

## Non-Negotiable Rules (Circle Contract)
- Goal health is derived exclusively from linked metric signals (via `procohere.goal_metrics`).
- Circle must not surface or prioritize manual goal fields like `goals.status`, `goals.progress_percent`, or `goals.completed_at`.
- Goals must be discussable in meetings via agenda-item linking (SQL updates below).
- Managers-of-managers can observe and comment; they cannot override data they do not own.

---

## Current Goal Schema (What We Already Have)
Tables already present and correct:
- `procohere.goals`
- `procohere.goal_metrics` (goal ↔ metric join, with sort + is_primary)
- `procohere.goal_categories`
- `procohere.goal_templates`

Important existing fields on `procohere.goals`:
- `owner_id` (FK to `procohere.team_members`) ✅
- `parent_goal_id` (self-FK for hierarchy) ✅
- `category_id` ✅
- `goal_type` ✅
- Legacy/compat fields that MUST NOT drive Circle health:
  - `status`
  - `progress_percent`
  - `completed_at`

---

## Derived Goal Health (Authoritative)
Goal health is computed using worst-state logic across supporting metrics:

- If any linked metric is Off Track → Goal = Off Track
- Else if any linked metric is At Risk → Goal = At Risk
- Else if all linked metrics are On Track → Goal = On Track
- Else (no metrics or insufficient data) → Goal = Unknown

Circle uses this derived health only.

---

## Where Goals Show Up

### Briefing
Goals appear only when attention is needed:
- Any supporting metric Off Track / At Risk
- Supporting metrics are stale (cadence violated)
- Goal referenced in a meeting recently

Briefing shows:
- Goal statement
- Derived health
- CTA: Review metrics

No goal status editing in Briefing.

### Me
Me shows:
- Goals I own
- Goals I contribute metrics to

Me may optionally show legacy fields (status/progress) for personal workflow.
Circle must not.

### Circle
Circle is organized by goals:
- Goal headers (statement + owner + derived health + metric count)
- Expand goal to see supporting metrics (signal-first)
- Goal drill-in shows: description, metric micro-trends, recent discussions

---

## Meeting Integration (Critical)
Goals must be linkable from meeting agenda items to support:
- Goal discussed recently
- Circle goal drill-in (recent discussions)
- Pulse synthesis (goals + metrics + tasks + meetings)

Right now, `meeting_agenda_items` has only `linked_entity_title_snapshot` and lacks link primitives.
We must add `linked_entity_type` and `linked_entity_id`.

---

# REQUIRED SQL UPDATES (Run in Supabase)

## 1) Add link primitives to meeting agenda items
```sql
alter table procohere.meeting_agenda_items
  add column if not exists linked_entity_type text null,
  add column if not exists linked_entity_id uuid null;
```

## 2) Enforce linked pair integrity (both null or both set)
```sql
alter table procohere.meeting_agenda_items
  add constraint chk_meeting_agenda_items_linked_pair
  check (
    (linked_entity_type is null and linked_entity_id is null)
    or
    (linked_entity_type is not null and linked_entity_id is not null)
  );
```

## 3) Restrict allowed entity types (start strict)
```sql
alter table procohere.meeting_agenda_items
  add constraint chk_meeting_agenda_items_linked_entity_type
  check (
    linked_entity_type is null
    or linked_entity_type in ('goal','metric','task','project','note','team_member')
  );
```

## 4) Index for Pulse + Circle queries
```sql
create index if not exists ix_meeting_agenda_items_linked_entity
  on procohere.meeting_agenda_items (organization_id, linked_entity_type, linked_entity_id)
  where is_deleted = false;
```

---

# Task Linking Contract (Already Supported)

Tasks already include:
- `tasks.source_type text`
- `tasks.source_id uuid`

And the database already enforces:
- Pair integrity: `chk_tasks_source_pair`
- Allowed source types: `chk_tasks_source_type`

Current allowed `tasks.source_type` values:
- `meeting`
- `agenda_item`
- `goal`
- `feedback`
- `note`

Important implication:
- If we want tasks sourced directly from metrics or projects, we must either:
  - Create tasks from the agenda item that links to the metric/project, or
  - Expand the allowed list in `chk_tasks_source_type` later

For v1 Pulse, tasks sourced from:
- `goal` (direct) ✅
- `agenda_item` (covers goal/metric/project discussions) ✅
- `meeting` (broad) ✅
are sufficient.

---

## App-Layer Requirements (Claude Instructions)

### A) Agenda Item Linking UI
In the agenda item editor:
- Provide a Link to… selector (Goal / Metric / Task / Project / Note / Team Member)
- Persist:
  - `linked_entity_type`
  - `linked_entity_id`
- Maintain:
  - `linked_entity_title_snapshot` as display-only cache

### B) Linking Contract (must be consistent)
- Goal discussion is determined by agenda items where:
  - `linked_entity_type = 'goal'`
  - `linked_entity_id = goals.id`
- Metrics discussed is determined by:
  - `linked_entity_type = 'metric'`
  - `linked_entity_id = metrics.id`
- Same pattern for tasks/projects/notes.

### C) Circle Goal Health Must Ignore Legacy Columns
In Circle:
- Never use `goals.status`
- Never use `goals.progress_percent`
- Never use `goals.completed_at`

Those fields may remain visible in Me (optional), but not in Circle.

---

## Wiki / DB Documentation Updates (Claude Checklist)

### 1) `procohere.meeting_agenda_items`
Add documentation for:
- `linked_entity_type`, `linked_entity_id`
- `chk_meeting_agenda_items_linked_pair`
- `chk_meeting_agenda_items_linked_entity_type`
- `ix_meeting_agenda_items_linked_entity`

Include a short linking contract section:
- How agenda items create durable links to goals/metrics/projects
- How this enables Circle drill-ins and Pulse synthesis

### 2) Goals section
- Clarify Circle goal health is derived from metric signals via `goal_metrics`
- Mark `goals.status`, `progress_percent`, `completed_at` as legacy/personal workflow fields

### 3) Tasks section (source contract)
- Document `tasks.source_type/source_id` usage
- Include the allowed source types from `chk_tasks_source_type`
- Explain the recommended pattern:
  - Task created from a discussion should use `source_type='agenda_item'`

### 4) Query examples to include
- Metrics driving a goal (goal_metrics)
- Goal health summary (derived from metric signals)
- Goals discussed recently (meetings + agenda item links)
- Tasks spawned from goal work (tasks.source_type='goal' and tasks.source_type='agenda_item')

---

## Summary
- Goal schema is mostly correct today.
- The missing piece is agenda item linking primitives.
- Circle uses derived health from metric signals; legacy goal fields must not drive Circle.
- Tasks already have a strong source contract; for v1 Pulse, use `agenda_item` as the bridge for metric/project-driven actions.
