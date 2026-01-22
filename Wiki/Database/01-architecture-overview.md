# 01 – Architecture Overview (Functional Specification)

This document is the **functional architecture specification** for the ProCohere database.

It is written to eliminate ambiguity.
It defines what the database **must do**, what it **must prevent**, and what invariants are required for correctness and security.

If a later document contradicts this one, **this document is authoritative**.

---

## 0. Reader Contract

By the time you finish this document, you will understand:

- The database’s tenancy model and the hard boundary it enforces
- Why schemas are split and what is allowed to live where
- The identity model and how it is resolved inside the database
- The mandatory conventions for tables, policies, functions, and soft delete behavior
- The invariants that must be true for the system to be considered correct

This document intentionally does **not** attempt to list all tables.
That begins in the schema reference documents.

---

## 1. Primary Objective

The database is the **security boundary** and the **correctness boundary**.

The ProCohere database must be safe and correct even when:

- Application code forgets to filter by organization
- Developers write ad-hoc queries during debugging
- A client tampers with request payloads
- A future refactor introduces a missing `WHERE` clause
- An engineer adds a new table and forgets to update one layer of access control

The database must **fail closed**, not fail open.

---

## 2. Tenancy Model (Absolute)

### 2.1 Organization Is the Hard Boundary

All product-domain data is strictly tenant-scoped.

Mandatory rules:

- Every row in every ProCohere product table belongs to exactly one organization.
- There is no shared product data across organizations.
- Cross-organization joins are forbidden for all application-visible roles.

“Forbidden” here means: **the database must prevent it**, not “developers shouldn’t do it”.

---

### 2.2 Organization Scope Must Not Be Client-Provided

The database must not accept organization scope as a trusted input.

Rules:

- Clients may include `organization_id` in payloads for convenience, but the database must validate it.
- RLS policies must enforce that any inserted/updated row’s `organization_id` equals the session’s organization.
- Any design that relies on the client to pass the correct org is considered insecure.

---

### 2.3 Explicit Definition: “Application-Visible Roles”

Unless explicitly stated otherwise, “application-visible roles” refers to:

- `anon`
- `authenticated`

`service_role` is a privileged operational role and is treated separately.
The security model assumes `service_role` is protected and never exposed to clients.

---

## 3. Schema Separation (Contract)

ProCohere uses schema separation to keep responsibilities enforceable and discoverable.

### 3.1 `public` Schema – Cross-Product Infrastructure

`public` may contain:

- `organizations`
- internal user records bridging auth to products
- licensing / billing / seats
- cross-product configuration

`public` must not contain:

- ProCohere domain concepts (goals, meetings, tasks, metrics, reviews)
- ProCohere visibility or hierarchy logic
- ProCohere product-specific audit streams

If a table exists only because ProCohere exists, it belongs in `procohere`.

---

### 3.2 `procohere` Schema – Product Domain

`procohere` contains product-domain data and the rules that govern it.

Mandatory rules for every table in `procohere`:

- Must include `organization_id uuid not null`
- Must have RLS enabled
- Must define policies for all required operations
- Must use the shared identity/visibility helper functions rather than duplicating logic inline

---

## 4. Identity Model (Contract)

### 4.1 Identity Resolution Chain

Identity must be resolved in the database using this chain:

```
auth.users  →  public.users  →  procohere.team_members
```

Rules:

- No ProCohere ownership or visibility logic may be based directly on `auth.users`.
- ProCohere tables must never store `auth.users.id` as the authoritative identity reference.
- ProCohere “actors” are always team members.

This indirection is required so the database can support:

- per-organization membership
- roles and hierarchy
- soft deletion of access without deleting auth users
- future support for non-auth-linked members

---

### 4.2 Team Member as the First-Class Actor

Within ProCohere:

- Every person-like reference in product tables must be a `team_member_id` (or equivalent FK to `procohere.team_members`).
- “Owner”, “assignee”, “reviewer”, “attendee”, “created_by”, “deleted_by” must all refer to team members unless explicitly documented otherwise.

If an exception exists, it must be documented and justified, and it must not weaken RLS.

---

### 4.3 Service Role Behavior (Explicit)

`service_role` is treated as privileged.

Rules:

- `service_role` may be granted broad privileges for operational tasks.
- If `service_role` is used to bypass RLS, that bypass must be explicit and documented.
- For security-sensitive tables, **FORCE RLS** should be used so that even privileged execution paths still respect row filters.

The database documentation must call out where FORCE RLS is used and why.

---

## 5. Row Level Security Model (Architecture)

This section states the architecture rules for RLS.
The implementation details and table-by-table policies are documented later.

### 5.1 RLS Is Mandatory for Product Tables

Rules:

- Every table in `procohere` must have RLS enabled.
- Every policy must enforce organization scoping.
- Every policy must exclude soft-deleted rows by default.
- A table with RLS disabled is considered a security defect.

---

### 5.2 Policy Composition (Mandatory Shape)

Policies must be composed from three layers:

1. **Organization boundary**
2. **Visibility boundary**
3. **Lifecycle boundary (soft delete)**

Conceptual pattern:

- `organization_id = get_current_organization_id()`
- `and <visibility predicate>`
- `and is_deleted = false`

Exact expressions vary, but the three layers must always be present unless explicitly documented.

---

### 5.3 Centralized Visibility Predicates

Visibility logic must not be duplicated across policies.

Rules:

- Hierarchy and “who can see whom” logic must be centralized in functions.
- Meeting visibility must be centralized in functions.
- RLS policies call these functions; they do not implement hierarchy logic inline.

This is required so that visibility changes do not require editing dozens of policies and risking drift.

---

## 6. Soft Delete Model (Architecture)

### 6.1 Soft Delete Is the Default

For tables that represent user-generated domain data, the default lifecycle is soft delete.

Rules:

- Soft delete fields must exist and be consistently named where applicable.
- RLS policies must exclude soft-deleted rows unless the use case explicitly requires historical access.
- Unique constraints must be compatible with soft deletes, typically via filtered unique indexes.

---

### 6.2 Soft Delete Fields (Standard)

For soft-deleted tables, the following fields are standard:

- `is_deleted boolean not null default false`
- `deleted_at timestamptz null`
- `deleted_by uuid null` (FK to the appropriate actor table)

If a table uses `deleted_by`, the actor type must be documented (team member vs internal user).

---

### 6.3 Soft Delete and Relationships

Soft delete does not imply cascade.

Rules:

- Soft-deleting a parent row must not silently delete children.
- Child visibility must remain correct when a parent is soft-deleted.
- If a child references an owner that is soft-deleted, the system must define whether:
  - the child becomes invisible, or
  - the child remains visible but the owner is treated as inactive

This behavior must be consistent and documented in later table references.

---

## 7. Timestamps and Mutability Rules

### 7.1 Standard Mutable Timestamp Fields

Where tables are mutable, they should include:

- `created_at timestamptz not null default now()`
- `updated_at timestamptz not null default now()`

and use a trigger or mechanism to keep `updated_at` correct.

### 7.2 Immutable Columns

Certain columns should be treated as immutable once set, especially:

- `organization_id`
- primary ownership identifiers

If immutability is enforced by trigger or policy, it must be documented in the triggers section later.

---

## 8. Constraints and Index Strategy (Architecture)

Constraints and indexes are not “performance hints” in ProCohere.
They encode invariants.

### 8.1 Organization Scoping Index Baseline

Rules:

- Every high-traffic table must have an index that supports organization scoping.
- Patterns typically include:
  - `(organization_id, <frequent filter column>)`
  - `(organization_id, created_at)` for feeds/timelines
  - `(organization_id, owner_id)` for ownership-scoped data

The index reference document will list each index and the access path it supports.

---

### 8.2 Soft Delete-Compatible Uniqueness

Rules:

- If a uniqueness constraint applies only to active rows, it must be enforced with a filtered unique index like:
  - `... where is_deleted = false`

This prevents historical rows from blocking new inserts while still enforcing invariants among active data.

---

## 9. What “Correct” Means (Failure Conditions)

The database is considered incorrect if any of the following are possible under `anon` or `authenticated`:

- A query can read rows from another organization
- A query can modify rows from another organization
- A query can view a row that violates visibility rules
- Soft-deleted rows appear in normal application queries
- A new table can be added without RLS and still be reachable from application code
- Visibility logic exists in multiple places and can drift

Correctness must be demonstrable by policy inspection and test queries.

---

## 10. Change Control Rules (Documentation Contract)

When making schema changes, the following documentation updates are required:

- New table → must be added to table reference and schema overview
- New RLS policy or policy change → must be added to RLS section and relevant table spec
- New helper function → must be added to functions reference and referenced by dependent policies
- New index or constraint → must be added to index/constraint reference with the access path rationale

If documentation is not updated, the change is incomplete.

---

## 11. Boundary Between Architecture and Implementation

This document defines architecture.
The next documents provide implementation detail.

- `02-security-model-and-rls.md` specifies GRANTS vs RLS interaction and policy patterns in more detail
- `03-session-and-identity.md` specifies how session identity and organization context are resolved
- Schema documents enumerate tables and relationships
- Reference documents list functions, indexes, constraints, triggers, and grants exhaustively

---

**Next:** `02-security-model-and-rls.md`
