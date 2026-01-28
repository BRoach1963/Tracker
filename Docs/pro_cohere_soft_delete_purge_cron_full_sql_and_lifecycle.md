# ProCohere Soft Delete & Purge (pg_cron)

This document defines the **canonical soft-delete purge system** for ProCohere. It is designed to be:

- Centralized
- Batch-safe
- Schema-extensible
- Runnable from scratch

No table-level cascades are used. All destructive deletes are intentional and scheduled.

---

## Conceptual Model

- All domain tables use `is_deleted + deleted_at`
- Physical deletion is deferred
- A single purge function iterates registered tables
- `pg_cron` executes purge on a fixed schedule

---

## SQL – Extension, Registry, Purge Function, Cron Job

```sql
begin;

create schema if not exists procohere;
create extension if not exists pg_cron;

-- ==========================
-- PURGE TARGET REGISTRY
-- ==========================

create table if not exists procohere.purge_targets
(
  id bigserial primary key,
  schema_name text not null default 'procohere',
  table_name text not null,
  phase smallint not null,
  sort_order smallint not null,
  is_enabled boolean not null default true,

  created_at timestamptz not null default now(),

  constraint purge_targets_phase_chk check (phase between 1 and 9),
  constraint purge_targets_unique unique (schema_name, table_name)
);

create index if not exists ix_purge_targets_enabled_order
  on procohere.purge_targets (is_enabled, phase, sort_order, id);

insert into procohere.purge_targets (schema_name, table_name, phase, sort_order)
values
  ('procohere', 'project_links',   1, 10),
  ('procohere', 'project_members', 1, 20),
  ('procohere', 'projects',        3, 10)
on conflict do nothing;

-- ==========================
-- PURGE FUNCTION
-- ==========================

create or replace function procohere.purge_soft_deleted_older_than(
  p_days integer,
  p_batch_size integer default 5000,
  p_max_passes integer default 30
)
returns table
(
  pass_no integer,
  phase smallint,
  schema_name text,
  table_name text,
  rows_deleted integer
)
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_cutoff timestamptz;
  v_pass integer := 0;
  v_any_deleted boolean;
  v_sql text;
  v_rows integer;
  r record;
begin
  v_cutoff := now() - make_interval(days => p_days);

  while v_pass < p_max_passes loop
    v_pass := v_pass + 1;
    v_any_deleted := false;

    for r in
      select *
      from procohere.purge_targets
      where is_enabled = true
      order by phase, sort_order, id
    loop
      v_sql := format(
        'with victims as (
           select id
           from %I.%I
           where is_deleted = true
             and deleted_at < $1
           order by deleted_at
           limit $2
         )
         delete from %I.%I t
         using victims v
         where t.id = v.id',
        r.schema_name, r.table_name,
        r.schema_name, r.table_name
      );

      execute v_sql using v_cutoff, p_batch_size;
      get diagnostics v_rows = row_count;

      if v_rows > 0 then
        v_any_deleted := true;
        pass_no := v_pass;
        phase := r.phase;
        schema_name := r.schema_name;
        table_name := r.table_name;
        rows_deleted := v_rows;
        return next;
      end if;
    end loop;

    exit when v_any_deleted = false;
  end loop;
end;
$$;

-- ==========================
-- CRON SCHEDULE
-- ==========================

select cron.unschedule(jobid)
from cron.job
where jobname = 'monthly_soft_delete_purge';

select cron.schedule(
  'monthly_soft_delete_purge',
  '15 3 1 * *',
  $$
  select *
  from procohere.purge_soft_deleted_older_than(30, 5000, 30);
  $$
);

commit;
```

---

## Operational Notes

- Purge order is controlled by `phase` then `sort_order`
- Tables can be added without modifying the purge function
- The function is safe to run manually for validation
- All deletes are batched to avoid long locks

This design is intentionally boring, predictable, and safe.

