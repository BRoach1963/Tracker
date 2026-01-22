# 08 – Indexes and Constraints (Functional Specification)

This document is the **authoritative specification** for how indexes and constraints are used
in the ProCohere database.

Indexes and constraints are not treated as performance tweaks.
They are **correctness, security, and scalability mechanisms**.

If an invariant is important and is not enforced by an index or constraint,
then that invariant is not real.

---

## 0. Scope and Philosophy

This document defines:

- What invariants must be enforced by constraints
- How indexes are designed to support RLS, visibility, and access paths
- How soft delete affects uniqueness and referential integrity
- Required patterns for foreign keys and cross-table consistency
- Change-control rules for adding or modifying indexes and constraints

This document does **not**:
- List raw `CREATE INDEX` statements
- Replace migrations as the source of DDL truth

It defines **intent and guarantees**, not syntax.

---

## 1. Constraints vs Indexes (Clear Separation)

### 1.1 Constraints Define Truth

Constraints exist to make invalid states impossible.

Examples:
- Preventing duplicate active team-member links
- Preventing cross-organization foreign keys
- Preventing cycles or illegal references where feasible

If a condition must *never* be violated, it must be enforced by a constraint where possible.

---

### 1.2 Indexes Enable Access

Indexes exist to make *valid* operations fast and predictable.

They must:
- Support common access paths
- Support RLS predicate evaluation
- Avoid pathological performance under scale

Indexes are never a substitute for constraints.

---

## 2. Organization-Scoped Invariants

### 2.1 Universal Organization Constraint

Every product-domain table must include:

- `organization_id NOT NULL`

And must obey:

- All foreign keys must reference rows in the **same organization**
- No table may reference another organization’s row

---

### 2.2 Enforcing Same-Organization Foreign Keys

Preferred enforcement mechanisms (in order):

1. **Composite foreign keys** including `organization_id`
2. **CHECK constraints** where feasible
3. **RLS + application validation** (only when DB-level enforcement is impossible)

If option 3 is used, it must be explicitly documented for that table.

---

## 3. Soft Delete and Uniqueness

Soft delete complicates uniqueness.
This section defines the required approach.

---

### 3.1 Active-Row Uniqueness

When uniqueness applies only to active rows:

- A filtered (partial) unique index must be used
- The filter must exclude soft-deleted rows

Example intent:
- Only one *active* team member may be linked to a given internal user per organization
- Historical rows must not block new active inserts

---

### 3.2 Forbidden Pattern

The following is forbidden:

- Enforcing uniqueness across *all* rows when soft deletes exist
- Relying on application code to “check first”

If the database allows duplicate active rows, the invariant is broken.

---

## 4. Foreign Key Strategy

### 4.1 Foreign Keys Are Mandatory (With Exceptions)

Rules:

- Foreign keys must be declared wherever referential integrity is required
- Missing FKs must be justified explicitly (performance or migration constraints)

Foreign keys are not optional documentation.
They are executable guarantees.

---

### 4.2 FK Delete and Update Rules

Default rules:

- `ON DELETE CASCADE` is discouraged for domain data
- Soft delete is preferred over cascading delete
- `ON DELETE RESTRICT` or `NO ACTION` is preferred

If cascade behavior exists, it must be documented and intentional.

---

### 4.3 FK and Soft Delete Interaction

Foreign keys do not understand soft delete.

Therefore:

- Soft-deleted parents may still have active children
- Visibility and lifecycle correctness must be handled via RLS and application logic
- Constraints must not assume physical deletion

This behavior must be considered in every FK design.

---

## 5. Indexes and RLS Performance

### 5.1 Why RLS Changes Index Design

RLS predicates are injected into every query.

Therefore:

- Indexes must support RLS predicates efficiently
- Missing indexes can turn simple queries into sequential scans

---

### 5.2 Mandatory Index Components

For most product-domain tables, indexes must support:

- `organization_id`
- ownership or actor identifiers
- lifecycle fields used in predicates (`is_deleted`, `completed_at`, etc.)

Typical intent patterns:
- `(organization_id, owner_team_member_id)`
- `(organization_id, assigned_team_member_id)`
- `(organization_id, created_at)`

Exact index definitions depend on access patterns but intent must be documented.

---

### 5.3 Partial Indexes for Active Rows

Where queries almost always exclude soft-deleted rows:

- Partial indexes filtered on `is_deleted = false` should be used
- This improves performance and reduces index bloat

Partial indexes must align with RLS predicates.

---

## 6. Hierarchy and Graph Constraints

Some invariants cannot be fully enforced by simple constraints.

### 6.1 Management Hierarchy

Rules:

- Team-member hierarchy must not cross organizations
- Cycles must be prevented or detected

Possible enforcement strategies:
- Trigger-based validation
- Deferred constraint checks
- Application-level validation with periodic audits

If cycles are not prevented at DB level, this must be explicitly documented.

---

## 7. Activity and Audit Tables

Activity tables have unique requirements.

### 7.1 Append-Only Constraints

Where applicable:

- Updates should be prevented via triggers or permissions
- Deletes should be forbidden or tightly restricted

This ensures audit integrity.

---

### 7.2 Index Strategy for High Write Volume

Rules:

- Index count must be minimized
- Indexes must support primary read paths (feeds, timelines)
- Over-indexing is a performance risk

Indexes here are carefully chosen, not exhaustive.

---

## 8. Constraint Naming and Discoverability

Constraints and indexes should follow consistent naming patterns.

Goals:

- Make intent discoverable during debugging
- Make migration diffs readable
- Reduce accidental duplication

Exact naming conventions may be documented separately, but consistency is mandatory.

---

## 9. Change Control Rules

Any change involving indexes or constraints must include:

- Explanation of the invariant or access path being enforced
- Impact analysis on write performance
- Verification that RLS predicates remain index-supported
- Documentation update in this file

Dropping or weakening a constraint requires explicit justification.

---

## 10. Failure Modes

The database is considered incorrectly designed if:

- Active duplicate rows are possible where uniqueness is required
- Cross-organization references can exist
- RLS predicates cause widespread sequential scans
- Soft delete semantics break uniqueness or integrity guarantees

---

## 11. Summary

Indexes and constraints are not optional optimizations.
They are part of the database’s **correctness contract**.

If an invariant matters, it must be enforced.
If a query matters, it must be index-supported.

This document defines those expectations.

---

**Next:** `09-grants-and-acl.md`
