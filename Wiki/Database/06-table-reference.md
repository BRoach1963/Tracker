# 06 – Table Reference (Functional Specification)

This document is the **authoritative table-by-table reference** for the `procohere` schema.

It is intentionally explicit and verbose.
For each table, it defines:
- Purpose and responsibility
- Ownership and identity semantics
- Relationships and cardinality
- Row Level Security (RLS) expectations
- Soft-delete and lifecycle behavior
- Indexing and constraint intent

This document does **not** repeat:
- Architecture rules (see `01`)
- Security mechanics (see `02`)
- Session resolution (see `03`)

Those documents are authoritative.

---

## Reading Notes

- Column *names* referenced here are canonical where known.
- Column *lists* are described conceptually to avoid ambiguity about intent.
- Exact DDL (types, defaults) belongs in migrations and schema dumps, not here.
- If a table exists in the database and is not documented here, this document is incomplete.

---

## People & Access Control Domain

### Table: `team_members`

**Purpose**  
Represents a person within an organization as a ProCohere product actor.

**Core Responsibilities**
- Acts as the primary identity for ownership and visibility
- Participates in management hierarchy
- Serves as the anchor for role assignment

**Key Columns (Conceptual)**
- `id`
- `organization_id`
- `linked_user_id` (nullable, FK to `public.users`)
- `manager_team_member_id` (nullable, self-FK)
- lifecycle fields (`is_deleted`, timestamps)

**Relationships**
- Self-referencing hierarchy via `manager_team_member_id`
- Referenced by nearly all other product tables as owner/actor

**RLS Behavior**
- Organization-scoped
- Visibility governed by hierarchy helper functions
- Soft-deleted team members are not visible to application-visible roles

**Constraints / Indexes**
- Enforce uniqueness of `(organization_id, linked_user_id)` for active rows
- Prevent cross-org manager relationships
- Support hierarchy traversal queries

---

### Table: `roles`

**Purpose**  
Defines organization-scoped roles (Admin, Manager, Team Member, Viewer).

**Key Columns**
- `id`
- `organization_id`
- `name`
- lifecycle fields

**Relationships**
- Referenced by role assignment tables or columns

**RLS Behavior**
- Organization-scoped
- Typically visible to org admins and managers

**Constraints**
- Role names unique per organization (active rows)

---

## Meetings & Agenda Domain

### Table: `meetings`

**Purpose**  
Represents a scheduled or completed meeting.

**Key Columns**
- `id`
- `organization_id`
- meeting metadata (title, type, scheduled time, status)
- lifecycle fields

**Relationships**
- Parent to agenda items
- Joined to attendees via `meeting_attendees`

**RLS Behavior**
- Visible only to attendees
- Visibility computed via centralized meeting visibility functions

**Indexes**
- `(organization_id, scheduled_at)`
- `(organization_id, status)`

---

### Table: `meeting_attendees`

**Purpose**  
Associates team members with meetings.

**Key Columns**
- `meeting_id`
- `team_member_id`
- `organization_id`

**Relationships**
- FK to `meetings`
- FK to `team_members`

**RLS Behavior**
- Inherits visibility from parent meeting
- Must not leak meeting existence

**Constraints**
- Prevent duplicate attendance rows
- Prevent cross-org linkage

---

### Table: `agenda_items` (or equivalent)

**Purpose**  
Represents structured discussion points within meetings.

**Key Columns**
- `id`
- `meeting_id`
- `organization_id`
- content fields
- lifecycle fields

**RLS Behavior**
- Inherits visibility from parent meeting

---

## Goals & Metrics Domain

### Table: `goals`

**Purpose**  
Represents a long-lived objective owned by a team member.

**Key Columns**
- `id`
- `organization_id`
- `owner_team_member_id`
- status, timeframe fields
- lifecycle fields

**RLS Behavior**
- Visibility via `rls_is_visible_team_member(owner_id)`
- Soft-deleted goals excluded by default

**Indexes**
- `(organization_id, owner_team_member_id)`
- `(organization_id, status)`

---

### Table: `metrics`

**Purpose**  
Represents a measurable indicator, optionally linked to a goal.

**Key Columns**
- `id`
- `organization_id`
- `owner_team_member_id` (nullable if shared)
- value definition fields
- lifecycle fields

**RLS Behavior**
- Visibility tied to owner visibility
- Goal linkage must not widen visibility

---

## Tasks & Work Items Domain

### Table: `tasks`

**Purpose**  
Represents actionable work items.

**Key Columns**
- `id`
- `organization_id`
- `assigned_team_member_id`
- source reference fields
- completion fields
- lifecycle fields

**Relationships**
- May reference meetings, agenda items, or goals

**RLS Behavior**
- Visibility via assigned team member
- Source linkage must not leak visibility

**Indexes**
- `(organization_id, assigned_team_member_id)`
- `(organization_id, completed_at)`

---

## Reviews & Feedback Domain

### Table: `performance_reviews`

**Purpose**  
Represents a structured performance review artifact.

**Key Columns**
- `id`
- `organization_id`
- reviewee_team_member_id
- reviewer_team_member_id
- cycle identifiers
- lifecycle fields

**RLS Behavior**
- Highly restrictive
- Often requires FORCE RLS
- Visibility may depend on role and review state

**Constraints**
- Enforce uniqueness per cycle/reviewer/reviewee (active rows)

---

### Table: `feedback_items`

**Purpose**  
Represents discrete feedback entries.

**Key Columns**
- `id`
- `organization_id`
- author_team_member_id
- subject_team_member_id
- content fields
- lifecycle fields

**RLS Behavior**
- Visibility tightly controlled
- Must not leak feedback existence

---

## Activity & Audit Domain

### Table: `activity_feed`

**Purpose**  
Append-only record of significant events.

**Key Columns**
- `id`
- `organization_id`
- actor_team_member_id
- event type
- payload fields
- created_at

**Lifecycle Rules**
- Append-only
- Updates and deletes are forbidden or extremely restricted

**RLS Behavior**
- Visibility derived from referenced entities
- Must not leak metadata for invisible entities

**Indexes**
- `(organization_id, created_at)`
- `(organization_id, actor_team_member_id)`

---

## Cross-Table Invariants

The following must hold across all tables:

- All FKs must reference rows in the same organization
- All ownership references point to `team_members`
- All tenant-scoped tables have RLS enabled
- Soft-delete semantics are consistent

---

## Change Control

Adding or modifying a table requires:
- Updating this document
- Verifying RLS policies exist and are correct
- Verifying indexes support primary access paths
- Verifying constraints enforce stated invariants

---

**Next:** `07-functions-reference.md`
