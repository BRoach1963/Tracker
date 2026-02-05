# ProCohere Soft Delete & Purge Cron — Full SQL and Lifecycle

This document defines the canonical soft-delete purge system for ProCohere. It is centralized, batch-safe, schema-extensible, and runnable from scratch.

## Conceptual model

- Domain rows are soft-deleted using `is_deleted` + `deleted_at` (+ optional `deleted_by`)
- A scheduled purge job physically deletes rows older than N days
- Purge order is deterministic and controlled by a registry table

---

## SQL — Extension, Registry, Purge Function, Cron Job

```sql
begin;

create schema if not exists procohere;
create extension if not exists pg_cron;

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
on conflict (schema_name, table_name) do nothing;

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
  if p_days is null or p_days <= 0 then
    raise exception 'p_days must be > 0';
  end if;

  if p_batch_size is null or p_batch_size <= 0 then
    raise exception 'p_batch_size must be > 0';
  end if;

  if p_max_passes is null or p_max_passes <= 0 then
    raise exception 'p_max_passes must be > 0';
  end if;

  v_cutoff := now() - make_interval(days => p_days);

  while v_pass < p_max_passes loop
    v_pass := v_pass + 1;
    v_any_deleted := false;

    for r in
      select pt.phase, pt.schema_name, pt.table_name
      from procohere.purge_targets pt
      where pt.is_enabled = true
      order by pt.phase asc, pt.sort_order asc, pt.id asc
    loop
      v_sql :=
        format(
          'with victims as (
             select id
             from %I.%I
             where is_deleted = true
               and deleted_at is not null
               and deleted_at < $1
             order by deleted_at asc, id asc
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

  return;
end;
$$;

select cron.unschedule(jobid)
from cron.job
where jobname = 'monthly_soft_delete_purge';

select cron.schedule(
  'monthly_soft_delete_purge',
  '15 3 1 * *',
  $$
  select *
  from procohere.purge_soft_deleted_older_than(
    30,
    5000,
    30
  );
  $$
);

commit;
```

## Operational notes

- Deletion order is controlled by `phase` then `sort_order`
- Batch deletes reduce lock time
- You can add new tables by inserting into `procohere.purge_targets`
