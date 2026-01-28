# ProCohere Projects

## Purpose
Projects in ProCohere are **coordination containers**, not a full project‑management system. They exist to group related work (goals, tasks, notes, agenda items, etc.) under a shared outcome and ownership model, while remaining lightweight and flexible.

Projects sit alongside Pulse, Circle, Chronicle, Briefing, and Me:
- **Pulse**: goals, metrics, tasks (global work objects)
- **Projects**: optional grouping and coordination layer
- **Circle**: team context
- **Chronicle**: notes and reports
- **Briefing / Me**: time‑scoped and personal views over the same data

---

## Core Design Principles
1. **Not a PM system** – no dependency graphs, sprint mechanics, or Gantt semantics
2. **Goals can belong to multiple projects**
3. **Projects do not own tasks/goals**; they link to them
4. **Ownership is explicit and enforced at the DB layer**
5. **Soft‑delete first, hard‑delete later (via purge job)**

---

## Tables

### `projects`
Represents the project container itself.

Key columns:
- `id`
- `organization_id`
- `owner_team_member_id`
- `title`
- `description`
- `status` (e.g. active, paused, completed)
- `start_date`
- `target_date`
- `is_archived`, `archived_at`
- `is_deleted`, `deleted_at`, `deleted_by`
- `created_at`, `updated_at`

Notes:
- The **owner** is always a team member
- Only the owner can mutate the project record
- Archiving is distinct from deletion

---

### `project_members`
Defines explicit membership in a project.

Key columns:
- `project_id`
- `team_member_id`
- `role` (default: member)
- `is_deleted`, `deleted_at`, `deleted_by`

Notes:
- Membership controls **visibility**, not ownership
- Only the project owner can add/remove members

---

### `project_links`
Links projects to arbitrary entities (goals, tasks, notes, agenda items, etc.).

Key columns:
- `project_id`
- `entity_type`
- `entity_id`
- `entity_title_snapshot`
- `created_by_team_member_id`
- `is_deleted`, `deleted_at`, `deleted_by`

Notes:
- Entity types are constrained via `allowed_entity_types`
- Links are **unique per (project, entity_type, entity_id)** when active
- Deleting a link never deletes the underlying entity

---

## Ownership & Access Rules (RLS)

### Visibility
A user may **see** a project if:
- They are the project owner, OR
- They are an active member of the project

This rule propagates to:
- `projects`
- `project_members`
- `project_links`

### Write Access
Only the **project owner** may:
- Update or delete a project
- Add/remove project members
- Add/remove project links

Non‑owners always have read‑only access.

---

## Soft‑Delete Semantics

All project tables use soft‑delete fields:
- `is_deleted`
- `deleted_at`
- `deleted_by`

Rules:
- Soft‑deleted rows are invisible via RLS
- Soft deletes preserve FK integrity
- Hard deletes are handled asynchronously by purge jobs

---

## Indexing Strategy

Indexes exist for:
- Active row lookup (`WHERE is_deleted = false`)
- Entity lookup (`entity_type + entity_id`)
- Purge operations (`deleted_at WHERE is_deleted = true`)

This ensures:
- Fast reads for active data
- Predictable purge performance

---

## Lifecycle Summary
1. Project is created by a manager (owner)
2. Members are optionally added
3. Goals/tasks/notes/etc. are linked via `project_links`
4. Project may be archived (still queryable)
5. Project may be soft‑deleted
6. Purge job permanently removes it after retention window

---

## What Projects Are (and Aren’t)

Projects **are**:
- A coordination lens
- A reporting and health surface
- A way to group work without changing ownership

Projects **are not**:
- Task schedulers
- Dependency managers
- Workflow engines

This keeps ProCohere focused on clarity, not process overhead.

