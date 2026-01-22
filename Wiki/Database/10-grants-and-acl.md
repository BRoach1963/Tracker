# 09 – GRANTS and Access Control (Functional Specification)

This document is the **authoritative functional specification** for SQL GRANTS and access control
in the ProCohere database.

GRANTS are **not** a substitute for Row Level Security (RLS).
They define *who may attempt an operation*.
RLS defines *which rows the operation may affect*.

Both must be correct.

---

## 0. Scope and Non-Goals

This document defines:
- Which database roles exist and what they are allowed to do
- How GRANTS and RLS interact
- How functions are exposed safely
- What is explicitly forbidden
- Change-control requirements for GRANTS

This document does **not**:
- Repeat RLS logic (see `02-security-model-and-rls.md`)
- Describe application-layer authorization

If access is allowed by GRANTS but denied by RLS, the operation must fail.
If access is denied by GRANTS, RLS must never be reachable.

---

## 1. Roles and Trust Model

### 1.1 Roles in Use

ProCohere relies on standard Supabase/PostgreSQL roles:

- `anon` – unauthenticated, public access
- `authenticated` – signed-in end users
- `service_role` – trusted backend operations
- `postgres` / owner roles – administrative

---

### 1.2 Trust Boundaries

Rules:

- `anon` and `authenticated` are **untrusted**
- `service_role` is **trusted but dangerous**
- Owner roles are **fully trusted** and restricted to operations staff

No GRANT should assume correctness of caller intent.

---

## 2. GRANTS Philosophy

### 2.1 Minimal Surface Area

GRANTS should:
- Allow only what is necessary
- Rely on RLS for row filtering
- Avoid exposing tables/functions unnecessarily

A GRANT is an *attack surface*.

---

### 2.2 GRANTS Never Encode Business Rules

Forbidden patterns:
- Using GRANTS to encode role logic (manager vs IC)
- Granting access to “safe” subsets of data
- Granting access that bypasses RLS unintentionally

Business logic belongs in RLS and helper functions.

---

## 3. Table GRANTS

### 3.1 Product-Domain Tables (`procohere`)

Typical pattern:

- `authenticated`:
  - `SELECT`
  - `INSERT`
  - `UPDATE`
  - `DELETE`
- RLS enforces all row-level rules

Rules:
- GRANTS should be broad and simple
- RLS must do the real work
- Missing GRANTS must be intentional and documented

---

### 3.2 Public Schema Tables

GRANTS vary by table.

Typical patterns:
- `organizations`: limited `SELECT`, no write
- `users`: self-scoped `SELECT`, limited write
- licensing tables: restricted to admins or service role

RLS should be used when tenant-scoped data exists.

---

## 4. Function GRANTS

Functions are often more dangerous than tables.

---

### 4.1 Execution vs Visibility

Rules:

- A function must not be executable unless intended
- `EXECUTE` GRANT is the security boundary for functions
- SECURITY DEFINER functions must be especially restricted

---

### 4.2 Helper Functions Used by RLS

Rules:

- Functions used only inside RLS may:
  - be unexposed to application roles
  - or be exposed but return safe, scoped results
- They must never return cross-organization data

---

### 4.3 SECURITY DEFINER Functions

Rules:

- Must be explicitly documented
- Must be granted only to required roles
- Must be reviewed for RLS bypass risk
- Should be paired with FORCE RLS on sensitive tables

If a SECURITY DEFINER function exists and is undocumented, this document is incomplete.

---

## 5. View GRANTS (If Used)

If views exist:

- Views must not widen access beyond base tables
- Views must respect underlying RLS
- GRANTS on views must mirror intended table access

Views are not a substitute for RLS.

---

## 6. Common Anti-Patterns (Explicitly Forbidden)

The following patterns are forbidden:

- Granting `SELECT` on tables without RLS
- Granting `EXECUTE` on SECURITY DEFINER functions to `authenticated`
- Granting access to tables “because the app needs it”
- Using GRANTS to compensate for missing RLS policies
- Allowing `anon` access to tenant-scoped tables

---

## 7. Auditing and Verification

### 7.1 Periodic Audit Requirements

The following must be reviewed periodically:

- All GRANTS on product-domain tables
- All function EXECUTE grants
- Any GRANTS involving `service_role`

Audits should confirm:
- GRANTS match documented intent
- No drift has occurred

---

### 7.2 Automated Checks (Recommended)

Where possible:
- Schema diff tooling should flag GRANT changes
- CI should detect undocumented GRANTS
- New SECURITY DEFINER functions should require explicit review

---

## 8. Change Control Rules

Any change to GRANTS must include:

- Documentation update in this file
- Explanation of why the GRANT is required
- Confirmation that RLS still enforces row-level correctness
- Review for unintended access expansion

Removing a GRANT must consider backward compatibility.

---

## 9. Failure Conditions

The system is considered incorrectly secured if:

- GRANTS allow access that bypasses RLS
- SECURITY DEFINER functions are executable by untrusted roles
- Tenant-scoped tables are accessible without RLS
- GRANTS encode business logic

---

## 10. Summary

GRANTS define *who may knock on the door*.
RLS defines *what they may see once inside*.

Both must be correct.
Neither may substitute for the other.

This document defines how GRANTS are used safely and predictably.

---

**Next:** `10-rls-policy-reference.md`
