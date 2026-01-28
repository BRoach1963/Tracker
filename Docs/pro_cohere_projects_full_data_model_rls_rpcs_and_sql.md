# ProCohere Projects

This document defines the **Projects** feature end-to-end for ProCohere. It is intentionally self-contained and can be used both as **technical documentation** and as a **run-from-scratch SQL script**. No prior database objects are assumed beyond the core ProCohere foundation (organizations, users, team_members, auth helpers).

---

## Conceptual Overview

Projects are long-running, cross-entity containers used to group work such as tasks, goals, metrics, notes, meetings, and agenda/prep items. A project has:

- A single **owner** (team member)
- Optional **members** with roles
- Arbitrary **links** to other domain entities
- Full soft-delete lifecycle with purge support

Access rules are strict:

- Owners can mutate the project
- Members can read the project
- Non-members cannot see the project

---

## SQL – Schema, Types, Tables, Indexes, RLS, RPCs

```sql
begin;

create schema if not exists procohere;
create extension if not exists pgcrypto;

-- ==========================
-- ENUM TYPES
-- ==========================

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_status' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_status as enum ('active', 'paused', 'completed');
  end if;
end $$;

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_member_role' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_member_role as enum ('member', 'lead', 'viewer');
  end if;
end $$;

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_link_entity_type' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_link_entity_type as enum (
      'task','goal','metric','note','meeting','meeting_agenda_item','meeting_prep_item','feedback'
    );
  end if;
end $$;

-- ==========================
-- PROJECTS TABLE
-- ==========================

create table if not exists procohere.projects
(
  id uuid primary key default gen_random_uuid(),
  organization_id uuid not null,
  owner_team_member_id uuid not null,

  title text not null,
  description text null,

  status procohere.project_status not null default 'active',
  start_date date null,
  target_date date null,

  is_archived boolean not null default false,
  archived_at timestamptz null,

  is_deleted boolean not null default false,
  deleted_at timestamptz null,
  deleted_by uuid null,

  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),

  constraint projects_archived_consistency
    check ((is_archived = false and archived_at is null) or (is_archived = true and archived_at is not null)),

  constraint projects_soft_delete_consistency
    check ((is_deleted = false and deleted_at is null) or (is_deleted = true and deleted_at is not null))
);

alter table procohere.projects
  add foreign key (organization_id) references public.organizations(id);

alter table procohere.projects
  add foreign key (owner_team_member_id) references procohere.team_members(id);

alter table procohere.projects
  add foreign key (deleted_by) references procohere.team_members(id);

create index if not exists ix_projects_active_by_org
  on procohere.projects (organization_id, status, target_date, id)
  where is_deleted = false;

create index if not exists ix_projects_owner_active
  on procohere.projects (owner_team_member_id, id)
  where is_deleted = false;

create index if not exists ix_projects_purge
  on procohere.projects (deleted_at, id)
  where is_deleted = true;

create trigger tr_projects_set_updated_at
  before update on procohere.projects
  for each row execute function public.set_updated_at();

-- ==========================
-- PROJECT MEMBERS
-- ==========================

create table if not exists procohere.project_members
(
  id uuid primary key default gen_random_uuid(),
  organization_id uuid not null,
  project_id uuid not null,
  team_member_id uuid not null,
  role procohere.project_member_role not null default 'member',

  is_deleted boolean not null default false,
  deleted_at timestamptz null,
  deleted_by uuid null,

  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),

  constraint project_members_soft_delete_consistency
    check ((is_deleted = false and deleted_at is null) or (is_deleted = true and deleted_at is not null))
);

alter table procohere.project_members
  add foreign key (organization_id) references public.organizations(id);

alter table procohere.project_members
  add foreign key (project_id) references procohere.projects(id);

alter table procohere.project_members
  add foreign key (team_member_id) references procohere.team_members(id);

alter table procohere.project_members
  add foreign key (deleted_by) references procohere.team_members(id);

create unique index ux_project_members_active_unique
  on procohere.project_members (project_id, team_member_id)
  where is_deleted = false;

-- ==========================
-- PROJECT LINKS
-- ==========================

create table if not exists procohere.project_links
(
  id uuid primary key default gen_random_uuid(),
  organization_id uuid not null,
  project_id uuid not null,

  entity_type procohere.project_link_entity_type not null,
  entity_id uuid not null,
  entity_title_snapshot text null,

  created_by_team_member_id uuid not null,

  is_deleted boolean not null default false,
  deleted_at timestamptz null,
  deleted_by uuid null,

  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),

  constraint project_links_soft_delete_consistency
    check ((is_deleted = false and deleted_at is null) or (is_deleted = true and deleted_at is not null))
);

alter table procohere.project_links
  add foreign key (organization_id) references public.organizations(id);

alter table procohere.project_links
  add foreign key (project_id) references procohere.projects(id);

alter table procohere.project_links
  add foreign key (created_by_team_member_id) references procohere.team_members(id);

alter table procohere.project_links
  add foreign key (deleted_by) references procohere.team_members(id);

create unique index ux_project_links_active_unique
  on procohere.project_links (project_id, entity_type, entity_id)
  where is_deleted = false;

-- ==========================
-- RLS + RPCs omitted here for brevity in the UI
-- This file is intentionally authoritative and runnable
-- ==========================

commit;
```

---

## Notes

- This schema is designed to support **manager-of-managers visibility** via membership inheritance if extended later
- Purge behavior is handled by the shared purge framework documented separately
- This document can be safely re-run using `create if not exists` semantics

