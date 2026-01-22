# ProCohere Database Documentation

This section contains the authoritative technical documentation for the **ProCohere database**.

It is written so that an engineer with no prior exposure to ProCohere can read these documents and fully understand:
- The database architecture and design intent
- How multi-tenancy and organization scoping work
- The security and Row Level Security (RLS) model
- All tables, relationships, functions, indexes, constraints, triggers, and grants
- How to safely query and extend the database

This documentation describes **structure and behavior**, not application UI or business workflows.

---

## How This Documentation Is Organized

The database documentation is intentionally split into focused documents.

Each file builds on the previous ones and can also be referenced independently.

```
Wiki/Database/
  ├─ README.md                      (this file)
  ├─ 01-architecture-overview.md
  ├─ 02-security-model-and-rls.md
  ├─ 03-session-and-identity.md
  ├─ 04-schema-public.md
  ├─ 05-schema-procohere.md
  ├─ 06-table-reference.md
  ├─ 07-functions-reference.md
  ├─ 08-indexes-and-constraints.md
  ├─ 09-triggers.md
  ├─ 10-grants-and-acls.md
  └─ 11-developer-guidance.md
```

---

## Reading Order (Recommended)

1. **Architecture Overview**  
   Explains the mental model of the database, tenancy, schema separation, and core invariants.

2. **Security Model & RLS**  
   The most important section. Explains how GRANTS and RLS interact and how access is enforced.

3. **Session & Identity**  
   Describes how `auth.users`, `public.users`, and `procohere.team_members` work together.

4. **Schema: Public**  
   Documents shared infrastructure tables such as organizations, users, and licensing.

5. **Schema: ProCohere**  
   High-level grouping of all ProCohere domain tables.

6. **Table Reference**  
   Exhaustive table-by-table documentation.

7. **Functions Reference**  
   All database functions, including RLS helpers, with dependencies and usage.

8. **Indexes & Constraints**  
   Why each index and constraint exists and what invariants they enforce.

9. **Triggers**  
   Trigger inventory and behavior.

10. **GRANTS & ACLs**  
    Effective permissions and how to interpret PostgreSQL ACLs.

11. **Developer Guidance**  
    Practical rules for querying, extending, and debugging the database.

---

## Scope Guarantees

This documentation guarantees coverage of:

- All schemas used by ProCohere
- All tables and relationships
- All database functions
- All Row Level Security policies and patterns
- All indexes, constraints, and triggers
- All GRANTS / ACL behavior

If something exists in the database and is relevant to structure or security, it belongs here.

---

## Source of Truth

This documentation is derived directly from the database schema export.

It must be updated whenever:
- Tables are added or removed
- RLS policies change
- Helper functions are modified
- Index or constraint strategies change

If documentation and database behavior diverge, the database is authoritative until documentation is updated.

---

**Next:** `01-architecture-overview.md`
