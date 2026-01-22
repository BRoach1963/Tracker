# 03 – Session and Identity (Functional Specification)

This document is the **functional specification** for how ProCohere resolves identity and session context inside the database.

It is intentionally explicit and covers:
- Which identities exist and which are authoritative in which contexts
- How organization scope is derived
- How the current team member is derived
- What “no session”, “partial session”, and “service role” mean in practice
- The required behavior of all RLS helper functions that depend on session state

This document defines contracts that **must** be preserved when modifying identity tables, helper functions, or RLS policies.

---

## 0. Definitions

### 0.1 Identity Types

ProCohere involves multiple identity representations:

1. **Auth Identity** – Supabase authentication identity (`auth.users.id`)
2. **Internal User** – ProCohere’s internal user record (`public.users.id`)
3. **Team Member** – ProCohere’s product actor (`procohere.team_members.id`)

These are distinct concepts with distinct responsibilities.

---

### 0.2 Authoritative Identity by Concern

The following rules are mandatory:

- **Authentication** is represented by the Auth Identity (`auth.uid()`).
- **Organization ownership / tenant scope** is represented by `public.users.organization_id`.
- **Product-level authorization, ownership, and hierarchy** are represented by `procohere.team_members`.

No other identity type is allowed to “stand in” for these responsibilities.

---

### 0.3 Session Context

“Session context” refers to the set of values the database can derive for the current request:

- Current auth user id (via `auth.uid()`)
- Current organization id (via internal resolution)
- Current team member id (via internal resolution)

A session may be:
- Unauthenticated (no auth uid)
- Authenticated but not provisioned (auth uid exists but no internal user record)
- Provisioned but not a team member (internal user exists but no team member mapping exists)
- Fully provisioned (internal user and team member exist and are active)

RLS behavior must be correct for all cases.

---

## 1. Identity Resolution Chain (Mandatory)

Identity resolution must follow this chain:

```
auth.uid()
  → public.users (by auth_user_id)
  → procohere.team_members (by linked_user_id and organization_id)
```

Rules:

- The database must never accept a caller-provided organization id as authoritative.
- The database must never accept a caller-provided team member id as authoritative.
- Identity resolution must be performed inside the database using server-side joins.
- If identity cannot be resolved, the database must **fail closed**.

---

## 2. `public.users` (Internal User Contract)

### 2.1 Purpose of `public.users`

`public.users` exists to:

- Provide a stable internal user record independent of Supabase auth table shape
- Bind an authenticated identity to exactly one organization context
- Store cross-product user metadata
- Provide a stable FK target for audit / ownership references in shared infrastructure

---

### 2.2 Required Columns (Conceptual)

While the exact column list is defined in the schema docs, the following conceptual fields must exist:

- `auth_user_id` – the Supabase auth uid this user is bound to
- `organization_id` – the tenant organization this user belongs to
- lifecycle fields to disable/remove a user without deleting identity history (soft delete)

---

### 2.3 Organization Binding Rules

Rules:

- A `public.users` row must bind to exactly one organization.
- A single auth uid must not be bound to multiple organizations simultaneously via active internal user rows.
- If organization reassignment is allowed, it must be controlled and auditable.
- If organization reassignment is forbidden, it must be enforced by trigger or policy.

This document assumes organization binding is stable during normal operation.

---

## 3. `procohere.team_members` (Product Actor Contract)

### 3.1 Purpose of Team Members

A team member represents a person within ProCohere’s organization context.

It exists because ProCohere needs:

- Roles and permission models
- Management hierarchies (direct and indirect)
- Visibility computation (“who can see whom”)
- Support for future “non-auth” members (placeholders or invited members)

---

### 3.2 Linking to Internal Users

Team members may be linked to internal users via `linked_user_id` (or equivalent).

Rules:

- Not all team members must be linked to an internal user.
- If linked, the linkage must be unique within an organization for active records.
- Team member linkage must be consistent with the internal user’s organization.

---

### 3.3 Management Hierarchy

Team members may reference other team members as managers.

Rules:

- Hierarchy must not cross organizations.
- Cycles must be prevented (directly or via validation constraints).
- Visibility computation relies on this graph.

The hierarchy rules are enforced by a combination of constraints, application validation, and RLS-dependent visibility functions.
The table reference documents must explicitly define how cycles are prevented (if enforced at DB level) or detected (if enforced in application).

---

## 4. Core Session Helper Functions (Contracts)

This section defines the required functional behavior for session resolution helpers.
Exact SQL definitions appear in the Functions Reference.

---

### 4.1 `get_current_organization_id()` – Required Behavior

**Purpose**

Return the tenant organization id for the current session.

**Inputs**

- None. Uses `auth.uid()` internally.

**Behavior**

- If `auth.uid()` is NULL: return NULL.
- If there is no active `public.users` row for the auth uid: return NULL.
- If the internal user is soft deleted / inactive: return NULL.
- Otherwise return `public.users.organization_id`.

**Security Implications**

- This function is the foundation of tenant isolation.
- Every RLS policy that protects product data must depend on this function directly or indirectly.

**Performance Notes**

- Must be inexpensive and index-supported.
- Must not scan large tables.
- Must use indexed lookup on auth uid.

---

### 4.2 `get_current_team_member_id()` – Required Behavior

**Purpose**

Return the ProCohere team member id for the current session.

**Inputs**

- None. Uses `auth.uid()` internally.

**Behavior**

- If `auth.uid()` is NULL: return NULL.
- Resolve internal user via `public.users` for the auth uid:
  - If missing or inactive: return NULL.
- Resolve team member via `procohere.team_members` where:
  - `linked_user_id` matches the internal user id
  - `organization_id` matches the internal user organization id
  - team member is active (not soft deleted)
- If no active match exists: return NULL.
- If multiple active matches exist: this is a data integrity violation and must be prevented by unique constraints.

**Security Implications**

- This function establishes the current product actor.
- Visibility logic and role checks depend on it.
- Policies must treat NULL as “not authorized”.

**Performance Notes**

- Must be implemented as an indexed join chain.
- Must avoid recursive traversal or expensive computation.
- Must not include complex hierarchy logic.

---

### 4.3 NULL Semantics (Fail Closed)

Both functions may return NULL.

Rules:

- RLS policies must treat NULL session values as “deny”.
- Helper functions must not raise errors for unauthenticated sessions unless explicitly required.
- The default behavior for an unauthenticated session is:
  - no organization
  - no team member
  - no access to tenant-scoped tables

---

## 5. Provisioning States and Expected Behavior

The database must behave correctly under these states.

### 5.1 State: Unauthenticated

- `auth.uid()` is NULL.
- `get_current_organization_id()` returns NULL.
- `get_current_team_member_id()` returns NULL.
- All tenant-scoped RLS policies should deny access.
- Only explicitly public resources may be readable.

---

### 5.2 State: Authenticated but No Internal User Record

- `auth.uid()` exists.
- No corresponding active `public.users` row exists.

Expected behavior:

- organization id resolves to NULL
- team member id resolves to NULL
- RLS denies access to tenant-scoped tables

This state is normal during signup/provisioning windows and must fail closed.

---

### 5.3 State: Internal User Exists but No Team Member Link

- Auth uid resolves to a `public.users` row.
- No active team member exists linked to that internal user.

Expected behavior:

- organization id resolves correctly
- team member id is NULL
- RLS denies access to product-owned data unless policies explicitly allow “org member without team membership”

This state is used to support staged onboarding and invited users.

---

### 5.4 State: Fully Provisioned

- Auth uid resolves to internal user
- Internal user resolves to active team member

Expected behavior:

- organization id resolves correctly
- team member id resolves correctly
- RLS applies normal visibility and role rules

---

## 6. Service Role Semantics

`service_role` is privileged and treated as a trusted backend actor.

However, ProCohere must still define explicit behavior.

### 6.1 Expected Usage

`service_role` should be used for:

- Background jobs
- Admin repair operations
- Migrations and maintenance routines
- Internal services that must operate across organizations (if allowed)

---

### 6.2 Interaction with RLS

Rules:

- If `service_role` is allowed to bypass RLS, that bypass must be explicit and documented.
- For sensitive tables, FORCE RLS should be used to prevent bypass even for privileged execution paths.
- Functions that operate under elevated privileges must be carefully reviewed for cross-organization impact.

---

### 6.3 Identity Under Service Role

Under `service_role`:

- `auth.uid()` may be NULL or may not represent an end-user.
- Therefore, `get_current_organization_id()` and `get_current_team_member_id()` may return NULL.
- Service operations must not rely on session-derived organization context unless explicitly set through controlled mechanisms.

If a service operation needs an org context, it must:
- accept org as an explicit input, and
- validate authorization at the service layer, and
- avoid exposing such functions to application-visible roles.

---

## 7. Required Data Integrity Constraints (Identity Layer)

To support the contracts above, the database must enforce these invariants.

### 7.1 Internal User Uniqueness

- An auth uid must map to at most one active `public.users` row.
- If soft deletes exist, uniqueness must apply to active rows only.

---

### 7.2 Team Member Linkage Uniqueness

- Within an organization, an internal user must map to at most one active team member.
- This must be enforced via a unique constraint or filtered unique index on `(organization_id, linked_user_id)` where active.

---

### 7.3 Cross-Org Link Prevention

- A team member’s organization_id must always match the linked internal user’s organization_id.
- This must be enforced by:
  - RLS on write paths, and
  - application validation, and
  - ideally constraints if possible.

If it is not enforced by constraints, the table documentation must describe how it is prevented operationally.

---

## 8. Downstream Dependencies (Why This Matters)

The following depend on correct session identity resolution:

- `get_current_organization_id()` is required by nearly all RLS policies.
- `get_current_team_member_id()` is required by:
  - visibility checks
  - ownership and creator/actor attribution
  - role checks
  - hierarchy traversal functions
- Misconfiguration can cause:
  - data leakage (incorrect org resolution)
  - invisible data (incorrect team member resolution)
  - broken hierarchy visibility (incorrect linkage constraints)

Identity resolution is the root of correctness for everything else.

---

## 9. Change Control Requirements

Any change to identity resolution must include:

- Updated function specs and SQL definitions
- Updated RLS policies if the helper function signatures or semantics change
- Updated constraints/indexes ensuring uniqueness invariants remain true
- Explicit migration plan for existing data and edge cases

No identity-related change is complete without documentation updates.

---

**Next:** `04-schema-public.md`
