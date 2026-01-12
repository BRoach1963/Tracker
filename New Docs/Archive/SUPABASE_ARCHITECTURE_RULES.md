# Supabase Architecture Rules for Tracker

## Overview

Tracker uses Supabase as its cloud backend. Our Supabase project is hosted in **Virginia (Eastern US)**. These rules ensure consistent performance nationwide and maintain exit optionality.

---

## Rule 1: Treat Supabase as "Postgres + Services", Not Magic

### DO NOT:
- Write business logic tightly coupled to Supabase client helpers
- Scatter auth/role logic across the UI
- Depend on Supabase-specific SQL extensions unless necessary

### DO:
- Keep domain logic in your .NET layer
- Use Supabase as a **data plane**, not a brain
- Business rules live in `Services/`, not in SQL or UI

### Why?
Supabase is PostgreSQL with conveniences. Treat it that way. Your app logic belongs in C#.

---

## Rule 2: Keep a Clean API Boundary

Even with a desktop app, maintain discipline:

### DO NOT:
- Make "chatty" direct table access from the UI
- Have ViewModels call repositories for every field change
- Scatter database calls throughout the codebase

### DO:
- Centralize data access through Repositories
- Use RPCs for complex operations
- Use Edge Functions for server-side logic when needed
- Prefer aggregated read models over multiple queries

### Why?
This helps **now** (reduces latency from Virginia to West Coast users) and **later** (enables migration if needed).

---

## Rule 3: Assume Single-Write Region Forever

### Reality Check:
- True multi-region writes are complex **everywhere**
- Almost every successful SaaS:
  - Picks a **single write region**
  - Pushes **reads closer to users**
  - Designs the **UI to hide write latency**

### Our Approach:
- Write region: Virginia (us-east-1)
- Optimize reads with caching
- Use optimistic UI updates to hide latency
- Batch writes where possible

### Why?
Multi-region writes add massive complexity. Hide latency in the UI instead.

---

## Rule 4: Optimize for Exit Optionality, Not Premature Migration

### YOU WANT:
- A clean schema (standard PostgreSQL)
- No vendor-locked data types
- Clear ownership boundaries
- Portable SQL (no Supabase-specific syntax in core schema)

### YOU DO NOT WANT:
- A second platform "just in case"
- Dual-write systems
- Hybrid cloud experiments

### Why?
**Optionality comes from simplicity, not redundancy.**

If we ever need to migrate away from Supabase, we want:
1. Standard PostgreSQL schema → easy to move
2. Business logic in .NET → no rewrite needed
3. Clean repository pattern → swap the data layer

---

## Performance Strategy for Single-Region Backend

Since our Supabase is in Virginia but users are nationwide:

### 1. Aggressive Local Caching
- Cache frequently-read data (team members, org settings, templates)
- Use SQLite as a local cache/offline store
- Sync in background, show cached data immediately

### 2. Optimistic UI Updates
- Update UI immediately on user action
- Send to server in background
- Rollback only on failure (rare)

### 3. Batch Operations
- Don't save on every keystroke
- Debounce changes (e.g., 500ms after typing stops)
- Batch multiple changes into single requests

### 4. Lazy Loading
- Don't fetch everything at startup
- Load data as user navigates
- Paginate large datasets

### 5. Background Sync
- Sync changes when app is idle
- Pre-fetch likely-needed data
- Use change tracking to minimize sync payload

---

## Data Access Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         UI Layer                            │
│                  (Views, ViewModels)                        │
└─────────────────────────┬───────────────────────────────────┘
                          │ Bindings & Commands
┌─────────────────────────▼───────────────────────────────────┐
│                      Services Layer                         │
│         (Business Logic, Orchestration)                     │
│   - No direct DB access                                     │
│   - Coordinates repositories                                │
│   - Implements business rules                               │
└─────────────────────────┬───────────────────────────────────┘
                          │ Repository Interfaces
┌─────────────────────────▼───────────────────────────────────┐
│                    Repository Layer                         │
│         (IGoalRepository, ITaskRepository, etc.)            │
│   - CRUD operations                                         │
│   - Query composition                                       │
│   - Caching decisions                                       │
└─────────────────────────┬───────────────────────────────────┘
                          │
         ┌────────────────┴────────────────┐
         │                                 │
┌────────▼────────┐              ┌─────────▼─────────┐
│  Local SQLite   │              │    Supabase       │
│  (Cache/Offline)│◄────sync────►│   (PostgreSQL)    │
└─────────────────┘              └───────────────────┘
```

---

## Schema Portability Checklist

✅ Use standard PostgreSQL types (UUID, TIMESTAMPTZ, VARCHAR, etc.)
✅ Use standard SQL for RLS policies
✅ Avoid Supabase-specific functions in core logic
✅ Keep trigger functions simple and portable
✅ Document any Supabase-specific features used

### Supabase-Specific Features We Use (Documented):
- `auth.uid()` - For RLS policies (standard Supabase auth)
- `gen_random_uuid()` - Standard PostgreSQL (pgcrypto)
- `pgvector` extension - For AI embeddings (portable to any Postgres with pgvector)

---

## Summary

| Principle | Implementation |
|-----------|----------------|
| Supabase = Postgres + Services | Business logic in .NET, not SQL |
| Clean API Boundary | Repositories, not scattered DB calls |
| Single Write Region | Virginia + optimistic UI + caching |
| Exit Optionality | Portable schema, no vendor lock-in |

**The goal: Users in California should have the same experience as users in Virginia.**

This is achieved through smart caching, optimistic updates, and batching - NOT through complex multi-region infrastructure.
