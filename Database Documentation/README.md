# Database Documentation

> **VB Pro Multi-Product SaaS Platform**  
> **Last Updated**: January 17, 2026

## Overview

This directory contains all database schema documentation and SQL scripts for the VB Pro product suite.

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                     SINGLE SUPABASE PROJECT                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    public schema                         │   │
│  │  (shared across all products)                            │   │
│  │                                                          │   │
│  │  • organizations    - Companies that buy products        │   │
│  │  • users            - All user accounts (id = auth.uid)  │   │
│  │  • products         - Reference table of products        │   │
│  │  • organization_products - Licenses (org has product)    │   │
│  │  • user_product_seats    - Seats (user has access)       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│          ┌───────────────────┼───────────────────┐             │
│          ▼                   ▼                   ▼             │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐       │
│  │  procohere   │   │   procausa   │   │  threadline  │  ...  │
│  │   schema     │   │    schema    │   │    schema    │       │
│  │              │   │              │   │              │       │
│  │  meetings    │   │  cases       │   │  sessions    │       │
│  │  tasks       │   │  documents   │   │  clients     │       │
│  │  goals       │   │  court_dates │   │  notes       │       │
│  │  ...         │   │  ...         │   │  ...         │       │
│  └──────────────┘   └──────────────┘   └──────────────┘       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Single User ID (CRITICAL)
- `public.users.id` **equals** `auth.users.id`
- No more `supabase_auth_id` column
- All RLS policies use `auth.uid()` directly
- Simple, fast, no joins needed for auth checks

### 2. Schema-per-Product
- Each product gets its own PostgreSQL schema
- Isolates product data completely
- Allows independent evolution
- Single Supabase bill (cost savings)

### 3. Centralized Licensing
- `organization_products` tracks what's licensed
- `user_product_seats` tracks who can use what
- Helper functions make RLS policies simple

## Documents

### Public Schema (Shared)
| Document | Description |
|----------|-------------|
| [PUBLIC_SCHEMA_OVERVIEW.md](./PUBLIC_SCHEMA_OVERVIEW.md) | Architecture, design, RLS strategy |
| [PUBLIC_SCHEMA_TABLES.md](./PUBLIC_SCHEMA_TABLES.md) | Detailed table definitions |
| [PUBLIC_SCHEMA_SETUP.sql](./PUBLIC_SCHEMA_SETUP.sql) | **Runnable SQL script** for Supabase |

### ProCohere Schema
| Document | Description |
|----------|-------------|
| [PROCOHERE_SCHEMA_OVERVIEW.md](./ProCohere%20Schema/PROCOHERE_SCHEMA_OVERVIEW.md) | Architecture, tables, C# mapping |
| [PROCOHERE_SCHEMA_SETUP.sql](./ProCohere%20Schema/PROCOHERE_SCHEMA_SETUP.sql) | **Runnable SQL script** for Supabase |

### Other Product Schemas (Coming Soon)
| Document | Description |
|----------|-------------|
| `PROCAUSA_SCHEMA_SETUP.sql` | ProCausa (legal) SQL script |
| `THREADLINE_SCHEMA_SETUP.sql` | Threadline (therapy) SQL script |

## Products

| Code | Name | Description | Status |
|------|------|-------------|--------|
| `procohere` | ProCohere | Team relationship management for managers | In Development |
| `procausa` | ProCausa | Case management for legal professionals | Planned |
| `threadline` | Threadline | Therapy practice management | Planned |
| `procliente` | ProCliente | Non-profit client management | Planned |

## Quick Start

### 1. Set Up Public Schema
Run in Supabase SQL Editor:
```sql
-- Copy contents of PUBLIC_SCHEMA_SETUP.sql and run
```

### 2. Create Test Organization + User
Sign up via app - the `handle_new_user()` trigger will:
1. Create a new organization (or use existing)
2. Create user row with `id = auth.users.id`

### 3. Grant Product Seat
```sql
-- After user exists, grant them a ProCohere seat
INSERT INTO public.user_product_seats (user_id, product_id, role)
SELECT 
    u.id,
    p.id,
    'admin'
FROM public.users u
CROSS JOIN public.products p
WHERE u.email = 'test@example.com'
  AND p.code = 'procohere';
```

### 4. Set Up Product Schema
(Once procohere schema docs are created)

## Changelog

| Date | Change |
|------|--------|
| 2026-01-17 | Initial creation - public schema documentation |
