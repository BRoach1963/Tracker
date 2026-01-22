# 11 – RLS Policy Reference (Authoritative)

This document defines **exact RLS semantics per entity class**, grounded in the table contracts from `06-tables.md`.

RLS is the security boundary. Application logic does not override it.

---

## Global RLS Rules

All tenant-scoped tables must enforce:

- organization_id = get_current_organization_id()
- soft-delete filtering where applicable
- fail-closed semantics

Policies must be FORCE RLS where bypass would leak data.

---

## Owner-Scoped Entities

Applies to:
- goals
- many metrics
- personal prep items

SELECT:
- rls_is_visible_team_member(owner_team_member_id)

INSERT:
- owner_team_member_id must be visible to inserter
- organization_id immutable

UPDATE:
- owner immutable
- soft delete only when visible

---

## Dual-Scope Entities (Tasks, Assigned Prep)

SELECT:
- rls_is_visible_team_member(owner_team_member_id)
  OR rls_is_visible_team_member(assigned_team_member_id)

INSERT:
- owner must be visible
- assignee (if present) must be visible

UPDATE:
- reassignment only by owner or manager chain

---

## Meeting-Scoped Entities

Applies to:
- meetings
- agenda items
- meeting notes
- meeting summaries

SELECT:
- rls_can_see_meeting(meeting_id)

INSERT:
- meeting must be visible
- creator must be attendee or owner

---

## Prep Items (Hybrid Scope)

SELECT when ANY is true:
- meeting visible
- owner visible
- assignee visible

Linking does not widen access.

---

## AI Entities

AI artifacts inherit visibility from:
- owning team member
- OR referenced parent entity

AI-generated content must not introduce new visibility paths.

---

## Surveys

Survey visibility rules:
- definitions visible to creators/admins
- responses visible to respondent + authorized viewers
- aggregate access must not leak individual responses

---

## Linking Rule (Universal)

Links never grant access.

RLS must not OR visibility based on linked entity access.

---

## Example Policies (Required)

Real policy SQL must be pasted here from production:
- one owner-scoped SELECT
- one dual-scope SELECT
- one meeting-scoped SELECT
- one INSERT with org immutability
- one UPDATE soft-delete

Patterns without real examples are incomplete.

---

## Performance Rules

- All predicates must be index-backed
- Avoid per-row recursion in policy bodies
- Prefer helper functions over inline logic

---

## Change Discipline

- RLS changes are security changes
- Any change requires doc update + review
