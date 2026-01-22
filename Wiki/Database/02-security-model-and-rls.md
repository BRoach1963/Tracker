# 02 – Security Model and Row Level Security (RLS)

This document describes **how security is enforced in the ProCohere database**.

It explains the interaction between SQL privileges (GRANTS), Row Level Security (RLS),
and the helper functions that implement ProCohere’s authorization rules.

This is the most important document in the database specification.

---

## 1. Security Philosophy

The ProCohere database is designed under the assumption that:

- Clients are untrusted
- API layers can contain bugs
- Developers will occasionally forget to filter queries correctly

For that reason, **the database itself is the primary security boundary**.

Application code is treated as a convenience layer, not a security layer.

---

## 2. Two Layers of Protection

Security is enforced through **two independent mechanisms**:

1. **GRANTS** – Who may attempt to access an object
2. **Row Level Security (RLS)** – Which rows are visible or mutable

Both must allow an operation for it to succeed.

---

## 3. SQL GRANTS (Privileges)

### 3.1 Purpose of GRANTS

GRANTS determine:

- Which roles can `SELECT`, `INSERT`, `UPDATE`, or `DELETE`
- Which roles may execute functions
- Which roles may reference tables in joins

GRANTS do **not** decide *which rows* are accessible.

---

### 3.2 Roles Used by ProCohere

ProCohere relies on the standard Supabase roles:

- `anon` – unauthenticated access
- `authenticated` – signed-in users
- `service_role` – trusted backend operations

Most tables grant broad access to `authenticated`,
but rely on RLS to enforce correctness.

---

## 4. Row Level Security (RLS)

### 4.1 Why RLS Is Mandatory

RLS ensures that:

- Every query is automatically filtered by organization
- Visibility rules cannot be bypassed accidentally
- Security rules live close to the data

Without RLS, a single missing `WHERE` clause can leak data.

---

### 4.2 RLS Enablement

For ProCohere tables:

- RLS is enabled on all product tables
- Some tables use **FORCE RLS** to prevent bypass
- Policies exist for `SELECT`, `INSERT`, `UPDATE`, and `DELETE` where appropriate

---

## 5. Organization Scoping

### 5.1 Central Rule

Every RLS policy begins by enforcing:

```
organization_id = get_current_organization_id()
```

This ensures:

- No cross-organization access
- No reliance on client-provided org identifiers

---

### 5.2 Why Organization Is Resolved, Not Passed

Passing `organization_id` from the client is unsafe.

Resolution inside the database ensures:
- Identity integrity
- Tamper resistance
- Consistent scoping

---

## 6. Identity Resolution in RLS

RLS policies do not rely on `auth.uid()` directly.

Instead, they rely on helper functions that resolve:

- The current internal user
- The current team member
- The current organization

This indirection allows the identity model to evolve safely.

---

## 7. Visibility Rules

### 7.1 Visibility Is Computed

Visibility is not implied by ownership alone.

Examples of visibility rules:

- You can see your own data
- You can see data of direct reports
- You can see data of indirect reports
- You can see meeting data only if you are an attendee

---

### 7.2 Centralized Visibility Functions

Visibility logic is centralized in helper functions such as:

- `rls_is_visible_team_member(target_team_member_id)`
- `rls_can_see_meeting(meeting_id)`

RLS policies call these functions instead of embedding logic inline.

---

## 8. Policy Structure Pattern

Most RLS policies follow a consistent structure:

```
USING (
  organization_id = get_current_organization_id()
  AND <visibility condition>
  AND is_deleted = false
)
```

This pattern ensures predictability and auditability.

---

## 9. INSERT and UPDATE Policies

### 9.1 INSERT Policies

INSERT policies typically enforce:

- Organization matches current organization
- Ownership fields reference visible team members

---

### 9.2 UPDATE Policies

UPDATE policies typically enforce:

- Row remains in the same organization
- User has visibility rights to the owner
- Soft delete flags are respected

---

## 10. FORCE RLS Usage

Some tables are marked with **FORCE RLS**.

This means:

- Even table owners cannot bypass RLS
- SECURITY DEFINER functions must still respect policies

This is used for highly sensitive data.

---

## 11. Common Failure Modes

### 11.1 Missing Organization Check

If a policy does not enforce organization scoping,
the table is considered insecure.

---

### 11.2 Inline Visibility Logic

Embedding hierarchy logic directly in policies leads to:

- Duplication
- Inconsistent behavior
- Hard-to-audit security rules

All visibility logic must live in helper functions.

---

## 12. Security Invariants

The following must always hold:

- Every table has RLS enabled
- Every policy enforces organization scoping
- Visibility is computed centrally
- GRANTS never replace RLS
- FORCE RLS is used intentionally

Violating any of these invalidates the security model.

---

## 13. What Comes Next

The next document explains **how session and identity resolution actually works**,
including the exact role of `auth.users`, `public.users`, and `procohere.team_members`.

---

**Next:** `03-session-and-identity.md`
