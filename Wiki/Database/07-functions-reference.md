# 07 – Functions Reference (Authoritative)

This document enumerates **all database functions that participate in security, visibility, identity resolution,
or cross-entity enforcement**.

If a function exists in the database and affects access or correctness, it must appear here.

---

## Conventions

- All functions are assumed to run under RLS unless explicitly SECURITY DEFINER.
- Functions used by RLS **must be stable, deterministic, and index-supported**.
- UI helper functions may return broader sets than RLS allows; this distinction is explicit.

---

## Identity & Session Functions

### get_current_organization_id()
Resolves the organization for the current authenticated session.

- Source: auth.uid()
- Returns NULL when unauthenticated or unprovisioned
- Used by: nearly all RLS policies

---

### get_current_team_member_id()
Resolves the team member row for the current session.

- Organization-scoped
- Returns NULL if user has no team_member mapping
- Soft-deleted team members must not resolve

---

## Hierarchy & Visibility Functions (RLS-Safe)

### rls_is_visible_team_member(target_team_member_id uuid)
Authoritative primitive for **team-member-based visibility**.

Returns true when:
- target is self
- target is a direct or indirect report

Must:
- be organization-scoped
- fail closed
- rely on hierarchy traversal defined in `14-hierarchy-model.md`

---

### rls_can_see_meeting(meeting_id uuid)
Determines whether the current session may see a meeting.

Returns true when:
- current team member is an attendee
- OR explicit meeting-owner logic allows it

Meeting type must not implicitly widen access.

---

### rls_is_meeting_owner(meeting_id uuid)
Returns true when the current team member created the meeting.

Used by:
- update/delete policies
- agenda/prep mutation checks

---

## Entity-Specific Visibility Helpers (If Present)

These helpers may exist for readability and performance.
If they exist in the database, they **must** obey the contracts in `06-tables.md`.

- rls_can_see_task(task_id uuid)
- rls_can_see_goal(goal_id uuid)
- rls_can_see_metric(metric_id uuid)
- rls_can_see_agenda_item(agenda_item_id uuid)

If these helpers do not exist, RLS policies must compose visibility using primitives only.

---

## UI Visibility Helpers (NOT RLS)

### get_ui_visible_team_member_ids(org_id uuid, team_member_id uuid)
Returns the set of team members visible in the UI.

Includes:
- self
- manager
- peers
- descendants

**Important:** This function is NOT used in RLS.
It intentionally returns a superset for UI convenience.

---

## Utility Functions

### set_updated_at()
Trigger helper to maintain updated_at timestamps.

Must:
- be deterministic
- not mutate unrelated rows
- not bypass RLS

---

## SECURITY DEFINER Functions

Any SECURITY DEFINER function must be listed explicitly here.

Rules:
- minimal surface
- explicit EXECUTE grants
- reviewed for RLS bypass risk

---

## Change Discipline

- Adding a function requires updating this document
- Changing a function used by RLS requires re-auditing policies
- Undocumented functions are defects
