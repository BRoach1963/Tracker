# 14 – Hierarchy Model and Visibility Traversal

This document defines **how management hierarchy is represented and traversed**
in the ProCohere database.

Hierarchy is foundational to:
- visibility
- RLS enforcement
- managerial access
- delegation semantics

If hierarchy traversal is incorrect, security is incorrect.

---

## Core Model

Hierarchy is modeled via **adjacency**, not role flags.

Each team member may reference **one manager**.

```
team_members.manager_team_member_id → team_members.id
```

This creates a directed tree per organization.

---

## Structural Guarantees

The hierarchy guarantees:
- One root or multiple roots per organization
- No cycles
- Bounded depth (enforced by policy, not schema)

Cycles are prevented by:
- application validation
- optional trigger enforcement

---

## Traversal Strategy

Visibility traversal is implemented using a **recursive CTE**.

Characteristics:
- Traversal is organization-scoped
- Traversal walks upward (manager chain) and downward (reports)
- Traversal is bounded by organization boundary

Example (conceptual):

```sql
WITH RECURSIVE hierarchy AS (
  SELECT id
  FROM procohere.team_members
  WHERE id = current_team_member_id

  UNION ALL

  SELECT tm.id
  FROM procohere.team_members tm
  JOIN hierarchy h ON tm.manager_team_member_id = h.id
)
SELECT id FROM hierarchy;
```

---

## Visibility Function Integration

Functions such as:

- `rls_is_visible_team_member(team_member_id)`

use hierarchy traversal to determine:

- self-visibility
- direct report visibility
- indirect report visibility

Traversal is **always scoped to organization**.

---

## Performance Considerations

Rules:
- Hierarchy depth must remain small
- Recursive traversal must be indexed on `manager_team_member_id`
- Visibility functions must be stable and deterministic

If hierarchy depth grows unbounded, precomputation may be required.

---

## Alternatives Considered

Alternatives include:
- Closure tables
- Materialized path
- Precomputed ancestor tables

Current design favors:
- Simplicity
- Correctness
- Low write complexity

This decision may be revisited if scale demands it.

---

## RLS Dependency

RLS policies assume:
- Hierarchy traversal is correct
- No cycles exist
- Organization isolation is enforced

Breaking hierarchy invariants breaks RLS correctness.

---

## Change Control

Any change to hierarchy representation requires:
- Updating this document
- Reviewing all visibility functions
- Revalidating RLS policies

Hierarchy changes are security changes.

---

## Summary

Hierarchy defines who can see whom.

It is:
- explicit
- traversable
- enforceable
- security-critical

This document defines the current implementation contract.

---

**End of Database Documentation**
