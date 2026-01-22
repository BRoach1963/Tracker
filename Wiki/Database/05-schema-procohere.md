# 05 – ProCohere Schema (Functional Specification)

This document is the **functional specification** for the `procohere` schema.

It does **not** enumerate every table in detail yet.
Its purpose is to:
- Define the *domain model* of the ProCohere product at the database level
- Group tables by responsibility
- Explain how those groups interact
- Define cross-cutting rules that apply to *all* product-domain tables

If you do not understand this document, the table-by-table reference that follows will feel overwhelming.
This document is the map; later documents are the street-level detail.

---

## 0. Scope and Intent

This document defines:
- What kinds of data live in `procohere`
- How that data is conceptually grouped
- The responsibilities and invariants of each group
- How RLS, identity, and visibility apply across the schema

This document does **not**:
- List every column
- Repeat RLS mechanics already defined in `02`
- Repeat identity resolution already defined in `03`

Those documents are authoritative for their respective concerns.

---

## 1. What the `procohere` Schema Represents

The `procohere` schema contains **all product-domain data** for ProCohere.

At a high level, it models:

- People and roles within an organization
- Work artifacts (meetings, agenda items, tasks)
- Long-lived objectives (goals and metrics)
- Evaluation and feedback artifacts
- Activity and audit signals

Every table in this schema:
- Is tenant-scoped
- Is protected by RLS
- Participates in the identity and visibility model

---

## 2. Domain Grouping Overview

All tables in `procohere` fall into one of the following conceptual groups:

1. **People & Access Control**
2. **Meetings & Agenda**
3. **Goals & Metrics**
4. **Tasks & Work Items**
5. **Reviews & Feedback**
6. **Activity & Audit**

These groups are logical, not schema-separated.
They exist to explain intent and interaction patterns.

---

## 3. People & Access Control Domain

### 3.1 Purpose

This domain defines:
- Who exists in the system
- What roles they hold
- How hierarchy is modeled
- How visibility flows through the organization

This is the **foundation** domain for all others.

---

### 3.2 Core Tables (Conceptual)

This domain includes tables such as:
- `team_members`
- `roles`
- role assignment / membership tables (if present)

Exact table definitions are covered later.

---

### 3.3 Invariants

Mandatory rules:

- Every team member belongs to exactly one organization.
- Team members may reference other team members as managers.
- Hierarchy must not cross organizations.
- Cycles in the hierarchy must be prevented or detected.
- Roles are organization-scoped, not global.

---

### 3.4 RLS Implications

- Visibility of people is governed by centralized hierarchy functions.
- Other domains must not re-implement hierarchy logic.
- Ownership and attribution fields reference team members exclusively.

---

## 4. Meetings & Agenda Domain

### 4.1 Purpose

This domain models:
- Meetings as time-bound coordination artifacts
- Agenda items as structured discussion points
- Attendance and participation

Meetings are **containers** for collaborative work.

---

### 4.2 Core Tables (Conceptual)

This domain includes tables such as:
- `meetings`
- `meeting_attendees`
- `agenda_items` (or equivalent)

---

### 4.3 Invariants

Mandatory rules:

- A meeting belongs to exactly one organization.
- A meeting may have multiple attendees.
- Visibility of a meeting is restricted to attendees (unless explicitly documented otherwise).
- Agenda items must belong to a meeting or other valid container.

---

### 4.4 RLS Implications

- Meeting visibility must be computed via centralized functions (e.g., attendee checks).
- Agenda items inherit visibility from their parent meeting.
- Policies must not allow non-attendees to infer meeting existence.

---

## 5. Goals & Metrics Domain

### 5.1 Purpose

This domain models:
- Long-lived objectives (goals)
- Quantifiable or qualitative measurements (metrics)
- Optional relationships between goals and metrics

Goals and metrics outlive meetings and tasks.

---

### 5.2 Core Tables (Conceptual)

This domain includes tables such as:
- `goals`
- `goal_categories`
- `metrics`
- goal–metric association tables (if present)

---

### 5.3 Invariants

Mandatory rules:

- Goals are owned by a team member.
- Metrics may be owned or shared, depending on design.
- Visibility of goals and metrics flows from team member visibility.
- Soft deletes must preserve historical goal/metric data.

---

### 5.4 RLS Implications

- Visibility is determined via `rls_is_visible_team_member(owner_id)`.
- Policies must not assume ownership implies visibility.
- Metrics linked to goals must not widen visibility implicitly.

---

## 6. Tasks & Work Items Domain

### 6.1 Purpose

This domain models:
- Discrete units of work
- Follow-ups and action items
- Work derived from meetings, goals, or ad-hoc creation

Tasks are **actionable**, not aspirational.

---

### 6.2 Core Tables (Conceptual)

This domain includes tables such as:
- `tasks`
- task source / linkage tables (if present)

---

### 6.3 Invariants

Mandatory rules:

- Tasks are owned or assigned to a team member.
- Tasks may reference a source (agenda item, meeting, goal).
- Visibility of tasks follows team member visibility.
- Completion does not imply deletion.

---

### 6.4 RLS Implications

- Tasks must not be visible outside visibility scope of the owner/assignee.
- Source linkage must not leak visibility from a restricted container.

---

## 7. Reviews & Feedback Domain

### 7.1 Purpose

This domain models:
- Performance reviews
- Feedback artifacts
- Evaluation cycles

These artifacts are often **highly sensitive**.

---

### 7.2 Core Tables (Conceptual)

This domain includes tables such as:
- `performance_reviews`
- `feedback_items`
- review cycles or templates

---

### 7.3 Invariants

Mandatory rules:

- Reviews are organization-scoped and cycle-scoped.
- Visibility is tightly controlled and role-dependent.
- Historical reviews must remain accessible for audit but not editable.

---

### 7.4 RLS Implications

- FORCE RLS is likely required for these tables.
- Visibility logic may depend on role, not just hierarchy.
- Policies must be reviewed carefully for leakage paths.

---

## 8. Activity & Audit Domain

### 8.1 Purpose

This domain captures:
- What happened
- When it happened
- Who performed the action

This domain supports:
- Activity feeds
- Audit trails
- Debugging and forensic analysis

---

### 8.2 Core Tables (Conceptual)

This domain includes tables such as:
- `activity_feed`
- audit or event tables

---

### 8.3 Invariants

Mandatory rules:

- Activity rows are append-only.
- Activity rows must not be mutated after creation.
- Soft delete is rarely appropriate here.
- Activity rows must always be organization-scoped.

---

### 8.4 RLS Implications

- Activity visibility is derived from the referenced entities.
- Policies must avoid leaking metadata about invisible entities.
- Performance considerations are critical due to high write volume.

---

## 9. Cross-Domain Rules

The following rules apply across all domains:

- All FKs must reference tables within the same organization.
- No domain may bypass identity or visibility helpers.
- No domain may introduce ad-hoc security rules.
- Cross-domain references must not widen visibility.

---

## 10. Why This Structure Matters

Without clear domain boundaries:

- RLS becomes inconsistent
- Visibility rules drift
- New tables get designed incorrectly
- Security reviews become impossible

This document exists to prevent that outcome.

---

## 11. What Comes Next

The next document enumerates **every table** in the `procohere` schema,
with full detail:

- Purpose
- Columns
- Relationships
- RLS behavior
- Indexes
- Constraints

---

**Next:** `06-table-reference.md`
