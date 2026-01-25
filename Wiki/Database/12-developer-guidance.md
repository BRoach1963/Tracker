# 12 – Developer Guidance (Functional Specification)

This document provides **practical, opinionated guidance** for developers
working with the ProCohere database.

Its purpose is simple:

> Enable engineers to query, extend, and modify the database safely  
> **without violating security, correctness, or performance guarantees**

This is the document you read **before** writing SQL.

---

## 1. The Mental Model You Must Use

Before touching the database, internalize these rules:

- The database is the security boundary
- RLS is always on
- Organization scope is never trusted from the client
- Identity resolution is indirect by design
- Visibility is computed, not implied
- Soft delete is the default lifecycle

If your change conflicts with any of these, it is wrong.

---

## 2. Writing SELECT Queries Safely

### 2.1 Never Assume Visibility

You may *see* rows in a query result only because RLS allows it.

Never:
- add manual `WHERE organization_id = ...`
- attempt to “optimize around” RLS
- assume ownership implies visibility

Correct approach:
- write queries as if all rows exist
- let RLS filter what is visible

---

### 2.2 Avoid Debug-Only SQL in Production Code

Queries written for debugging often:
- bypass helper functions
- include privileged joins
- rely on elevated roles

Never promote debug SQL into application code.

---

## 3. Adding a New Table

Before creating a table, answer:

1. Does this belong in `public` or `procohere`?
2. What is the owning organization?
3. Who owns rows?
4. How is visibility computed?
5. Is soft delete required?

Minimum requirements for `procohere` tables:

- `organization_id NOT NULL`
- RLS enabled
- SELECT policy defined
- Indexes supporting RLS predicates

A table without RLS is a security defect.

---

## 4. Adding or Modifying RLS Policies

When changing RLS:

- Start from the canonical patterns in **11 – RLS Policy Reference**
- Enforce organization boundary first
- Use helper functions for visibility
- Enforce lifecycle rules

Never:
- inline hierarchy logic
- reference client-provided IDs
- widen visibility implicitly

Always test:
- cross-organization denial
- NULL-session behavior

---

## 5. Adding Functions

Before adding a function, decide:

- Is this security-critical?
- Will it be called from RLS?
- Does it need SECURITY DEFINER?

Rules:
- Functions used by RLS must be deterministic and cheap
- SECURITY DEFINER must be rare and documented
- Functions must never return cross-org data

If a function feels “convenient but risky”, it probably is.

---

## 6. Indexes and Constraints

Indexes and constraints are part of correctness.

When adding:
- a new FK → ensure same-organization enforcement
- uniqueness → account for soft delete
- indexes → ensure RLS predicates are supported

Never rely on application code to enforce invariants.

---

## 7. Triggers

Triggers must:
- enforce invariants
- maintain metadata
- block illegal states

Triggers must not:
- implement workflows
- create related rows
- hide business logic

If a trigger is surprising, it is wrong.

---

## 8. GRANTS and Privileges

As a developer:
- assume GRANTS are broad
- assume RLS does the real work
- never widen GRANTS casually
- never expose SECURITY DEFINER functions unintentionally

If you think a GRANT change is needed, stop and document why.

---

## 9. Common Mistakes to Avoid

- Adding tables without RLS
- Trusting client org or user IDs
- Inlining visibility logic
- Forgetting soft delete in uniqueness
- Adding SECURITY DEFINER “just to make it work”
- Optimizing before correctness

---

## 10. Supabase C# Client RLS Workaround

### The Problem

The Supabase C# client v1.1.1 has a **critical limitation**: when using `SetSession()` to sync authentication, the Postgrest module does not inherit the Authorization header. This causes `auth.uid()` to return NULL during INSERT/UPDATE/DELETE operations, resulting in RLS policy violations.

**Symptoms:**
- SELECT queries work fine (different RLS conditions)
- INSERT fails with `"new row violates row-level security policy"`
- The client shows a valid session/token when logged
- Database data is correct (user exists, flags are true, etc.)

### The Solution

Use **SECURITY DEFINER RPC functions** instead of direct `.Insert()` calls.

Pattern:
```csharp
// WRONG - will fail with RLS error
var result = await client.From<MeetingDetail>().Insert(meeting);

// CORRECT - use RPC
var rpcResult = await client.Rpc("insert_meeting", new {
    p_id = meeting.Id,
    p_organization_id = orgId,
    // ... other params
});
```

### Affected Operations

All INSERT operations to `procohere` schema tables require RPC wrappers. See **07-functions-reference.md** for the complete list of available RPCs.

### Tables Requiring RPC

| Table | RPC Function |
|-------|--------------|
| meetings | `insert_meeting` |
| meeting_attendees | `insert_meeting_attendee` |
| meeting_agenda_items | `insert_meeting_agenda_item` |
| meeting_prep_items | `insert_meeting_prep_item` |
| meeting_notes | `insert_meeting_note` |
| *(others TBD as needed)* | |

### Long-Term Fix

Options being considered:
1. Upgrade Supabase C# client if/when fixed
2. Patch the client to properly pass headers
3. Continue using RPC pattern (stable, works with RLS)

---

## 11. Review Checklist Before Merging

Before merging DB changes, confirm:

- Organization isolation preserved
- RLS policies exist and are correct
- Indexes support RLS predicates
- Functions are documented
- Triggers enforce clear invariants
- Documentation is updated

If any answer is “I’m not sure”, do not merge.

---

## 12. Final Word

This database is intentionally strict.

That strictness is what allows:
- simple application code
- safe refactoring
- confident evolution over time

Work *with* the rules, not around them.

---

**End of Database Specification**
