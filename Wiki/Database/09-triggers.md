# 09 – Triggers (Functional Specification)

This document is the **authoritative functional specification** for all database triggers
used in the ProCohere database.

Triggers are **not business logic**.
They exist to enforce invariants, maintain metadata, and protect correctness guarantees
that cannot be reliably enforced by application code alone.

If a trigger’s purpose is unclear, it is a liability.
Every trigger must be explainable in terms of **what invariant it protects**.

---

## 0. Scope and Intent

This document defines:

- Why triggers exist in this database
- What classes of triggers are allowed
- What classes of triggers are forbidden
- The specific triggers used by ProCohere and their responsibilities
- How triggers interact with RLS, functions, and constraints

This document does **not**:
- Describe operational procedures
- Replace migration scripts as the source of DDL truth
- Contain application-level business workflows

If a trigger exists in the database and is not documented here, this document is incomplete.

---

## 1. Trigger Philosophy

Triggers are used **sparingly and deliberately**.

Rules:

- Triggers enforce invariants, not workflows
- Triggers must be deterministic
- Triggers must not depend on client input
- Triggers must not silently change business meaning
- Triggers must not bypass RLS unintentionally

Triggers are part of the database’s correctness surface.

---

## 2. Classes of Allowed Triggers

Only the following categories of triggers are permitted.

---

### 2.1 Lifecycle Metadata Triggers

These triggers maintain standard lifecycle fields.

#### Example: `set_updated_at`

**Purpose**
- Ensure `updated_at` reflects the last mutation time

**Trigger Timing**
- `BEFORE UPDATE`

**Required Behavior**
- Set `updated_at = now()`
- Must not modify any other fields

**Constraints**
- Must not bypass RLS
- Must not raise errors for valid updates
- Must be safe under concurrent updates

---

### 2.2 Invariant Enforcement Triggers

These triggers enforce rules that cannot be expressed cleanly with constraints alone.

Typical use cases:
- Preventing illegal state transitions
- Enforcing immutability of specific columns
- Blocking cross-organization reassignment

**Examples**
- Prevent changing `organization_id` after insert
- Prevent reassignment of ownership once a record is finalized

**Rules**
- Must fail loudly (raise exception) on violation
- Must not silently “fix” invalid input
- Must not contain complex branching logic

---

### 2.3 Append-Only Protection Triggers

Used for audit and activity tables.

**Purpose**
- Prevent UPDATE or DELETE on append-only tables

**Trigger Timing**
- `BEFORE UPDATE` and/or `BEFORE DELETE`

**Required Behavior**
- Raise an exception on attempted mutation
- Allow INSERT only

**Rationale**
- Preserves audit integrity
- Prevents silent history rewriting

---

## 3. Forbidden Trigger Patterns

The following are explicitly forbidden:

- Triggers that implement business workflows
- Triggers that create or modify related rows implicitly
- Triggers that perform cross-table writes
- Triggers that contain visibility or role logic
- Triggers that “repair” invalid data silently
- Triggers that rely on session state beyond invariant checks

If a trigger needs to know *who* the user is to behave correctly, it likely does not belong in the database.

---

## 4. Interaction with RLS

Triggers execute within the context of the statement that fired them.

Rules:

- Triggers must not be used to bypass RLS
- SECURITY DEFINER triggers must be treated with extreme caution
- FORCE RLS must be considered when triggers operate on sensitive tables

If a trigger writes to another table, that write must also pass RLS unless explicitly documented and justified.

---

## 5. Standard Triggers Used by ProCohere

This section documents the triggers that are expected to exist.

---

### 5.1 `set_updated_at` Trigger

**Tables**
- Applied to most mutable tables in `public` and `procohere`

**Invariant Protected**
- `updated_at` always reflects last modification time

**Notes**
- Must be consistent across all tables
- Trigger name and function name should be standardized

---

### 5.2 Organization Immutability Trigger (If Present)

**Purpose**
- Prevent changing `organization_id` after insert

**Invariant Protected**
- Tenant ownership cannot be reassigned silently

**Behavior**
- On UPDATE, if `OLD.organization_id IS DISTINCT FROM NEW.organization_id` → reject

---

### 5.3 Append-Only Enforcement Trigger (If Present)

**Tables**
- Activity / audit tables

**Invariant Protected**
- Historical integrity

**Behavior**
- Reject UPDATE and DELETE

---

## 6. Trigger Performance Considerations

Rules:

- Triggers must be constant-time per row
- Triggers must not perform table scans
- Triggers must not query large unrelated tables

If a trigger’s cost scales with table size, it is incorrectly designed.

---

## 7. Testing and Validation

Every trigger must be validated with:

- A positive case (valid operation succeeds)
- A negative case (invalid operation fails)
- Confirmation that error messages are clear
- Confirmation that no silent mutation occurs

Trigger behavior must be tested alongside RLS.

---

## 8. Change Control Rules

Any change involving triggers requires:

- Documentation update in this file
- Explanation of the invariant being enforced
- Review for unintended side effects
- Review for RLS interaction

Adding a trigger without documentation is forbidden.

---

## 9. Failure Conditions

The database is considered incorrectly designed if:

- Triggers silently modify business data
- Triggers encode business workflows
- Triggers bypass RLS
- Triggers are relied on to “fix” bad application behavior

---

## 10. Summary

Triggers are a sharp tool.

Used correctly, they enforce invariants the application cannot.
Used incorrectly, they hide logic and create surprises.

This document defines how triggers are used safely and predictably in ProCohere.

---

**Next:** `10-grants-and-acls.md`
