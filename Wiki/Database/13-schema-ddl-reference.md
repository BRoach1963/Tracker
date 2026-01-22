# 13 – Schema DDL Reference

This document is the **physical schema reference** for the ProCohere database.

It contains the authoritative DDL snapshot:
- table declarations
- columns and data types
- constraints (PK/FK/unique/check)
- indexes
- triggers attached to tables
- RLS policies (optional, if you choose to include them here; otherwise keep in `11`)

This document intentionally contains **no conceptual explanations**.
Conceptual contracts live in `06-tables.md`.

---

## 1. Snapshot Metadata

- Snapshot date: **(fill in)**
- Database: **(fill in)**
- Source: `pg_catalog` / information_schema extraction
- Included schemas: `public`, `procohere`

---

## 2. How to Generate This Snapshot (Required Procedure)

This repository should treat the DDL snapshot as a generated artifact.
Regenerate it whenever schema changes.

### 2.1 Option A: pg_dump (Recommended)
Use `pg_dump` to capture schema-only DDL.

```bash
pg_dump --schema-only --no-owner --no-privileges --schema=public --schema=procohere "$DATABASE_URL" > schema.sql
```

Then paste relevant sections into this file (or store `schema.sql` alongside it if preferred).

If you want indexes and constraints included, keep the defaults (pg_dump includes them).

### 2.2 Option B: Catalog Queries (Targeted Extraction)
If you prefer deterministic, sectioned output:

**List tables**
```sql
select table_schema, table_name
from information_schema.tables
where table_schema in ('public','procohere')
  and table_type = 'BASE TABLE'
order by table_schema, table_name;
```

**List columns**
```sql
select table_schema, table_name, column_name, data_type, is_nullable, column_default
from information_schema.columns
where table_schema in ('public','procohere')
order by table_schema, table_name, ordinal_position;
```

**List indexes**
```sql
select schemaname, tablename, indexname, indexdef
from pg_indexes
where schemaname in ('public','procohere')
order by schemaname, tablename, indexname;
```

**List constraints**
```sql
select
  n.nspname as schema_name,
  c.relname as table_name,
  con.conname as constraint_name,
  pg_get_constraintdef(con.oid) as constraint_def
from pg_constraint con
join pg_class c on c.oid = con.conrelid
join pg_namespace n on n.oid = c.relnamespace
where n.nspname in ('public','procohere')
order by schema_name, table_name, constraint_name;
```

**List triggers**
```sql
select
  n.nspname as schema_name,
  c.relname as table_name,
  t.tgname as trigger_name,
  pg_get_triggerdef(t.oid) as trigger_def
from pg_trigger t
join pg_class c on c.oid = t.tgrelid
join pg_namespace n on n.oid = c.relnamespace
where n.nspname in ('public','procohere')
  and not t.tgisinternal
order by schema_name, table_name, trigger_name;
```

**List RLS policies**
```sql
select
  schemaname,
  tablename,
  policyname,
  permissive,
  roles,
  cmd,
  qual as using_expression,
  with_check as with_check_expression
from pg_policies
where schemaname in ('public','procohere')
order by schemaname, tablename, policyname;
```

---

## 3. DDL Snapshot Content

Paste the captured DDL below this line.

> NOTE: This section must contain real output from the database.
> Placeholder examples are not acceptable for long-term use.

---

## 4. Change Discipline

- Treat this file as generated.
- Regenerate after any migration that changes structure.
- Do not hand-edit individual lines unless regenerating the whole snapshot.
