# ProCohere Projects — Full Data Model, RLS, RPCs, and SQL

This document defines the **Projects** feature end-to-end for ProCohere. It is intentionally self-contained and can be used both as technical documentation and as a **run-from-scratch SQL script**.

## Scope

Implements:

- `procohere.projects`
- `procohere.project_members`
- `procohere.project_links`
- RLS helper functions:
  - `procohere.rls_can_see_project(uuid)`
  - `procohere.rls_is_project_owner(uuid)`
- Complete RLS policies for all three tables
- RPCs:
  - `rpc_create_project`
  - `rpc_update_project`
  - `rpc_delete_project` (soft-delete)
  - `rpc_transfer_project_ownership` (owner reassignment)
  - `rpc_add_project_member`
  - `rpc_remove_project_member` (soft-delete row)
  - `rpc_add_project_link`
  - `rpc_remove_project_link` (soft-delete row)

## Assumptions (already present in your foundation)

This script assumes these exist and are correct in your database:

- `public.organizations(id)`
- `procohere.team_members(id, organization_id, linked_user_id, ...)`
- `public.set_updated_at()` trigger function
- `procohere.get_current_organization_id()`
- `procohere.get_current_team_member_id()`

## Conceptual model

- A Project has one owner (`owner_team_member_id`).
- Project members can read the project and its links/members.
- Only the owner can create/update/delete projects and manage members/links.
- Soft-delete fields exist on all three tables.
- Physical purge is handled by the centralized purge framework (separate doc).

---

## SQL — Schema, Types, Tables, Constraints, Indexes, Triggers

```sql
begin;

create schema if not exists procohere;
create extension if not exists pgcrypto;

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_status' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_status as enum ('active', 'paused', 'completed');
  end if;
exception
  when duplicate_object then null;
end $$;

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_member_role' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_member_role as enum ('member', 'lead', 'viewer');
  end if;
exception
  when duplicate_object then null;
end $$;

do $$
begin
  if not exists (select 1 from pg_type where typname = 'project_link_entity_type' and typnamespace = 'procohere'::regnamespace) then
    create type procohere.project_link_entity_type as enum (
      'task',
      'goal',
      'metric',
      'note',
      'meeting',
      'meeting_agenda_item',
      'meeting_prep_item',
      'feedback'
    );
  end if;
exception
  when duplicate_object then null;
end $$;

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
  add constraint projects_organization_id_fkey
  foreign key (organization_id) references public.organizations(id);

alter table procohere.projects
  add constraint projects_owner_team_member_id_fkey
  foreign key (owner_team_member_id) references procohere.team_members(id);

alter table procohere.projects
  add constraint projects_deleted_by_fkey
  foreign key (deleted_by) references procohere.team_members(id);

create index if not exists ix_projects_active_by_org
  on procohere.projects (organization_id, status, target_date, id)
  where is_deleted = false;

create index if not exists ix_projects_owner_active
  on procohere.projects (owner_team_member_id, id)
  where is_deleted = false;

create index if not exists ix_projects_purge
  on procohere.projects (deleted_at, id)
  where is_deleted = true;

do $$
begin
  if not exists (select 1 from pg_trigger where tgname = 'tr_projects_set_updated_at') then
    create trigger tr_projects_set_updated_at
      before update on procohere.projects
      for each row
      execute function public.set_updated_at();
  end if;
end $$;

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
  add constraint project_members_organization_id_fkey
  foreign key (organization_id) references public.organizations(id);

alter table procohere.project_members
  add constraint project_members_project_id_fkey
  foreign key (project_id) references procohere.projects(id);

alter table procohere.project_members
  add constraint project_members_team_member_id_fkey
  foreign key (team_member_id) references procohere.team_members(id);

alter table procohere.project_members
  add constraint project_members_deleted_by_fkey
  foreign key (deleted_by) references procohere.team_members(id);

create unique index if not exists ux_project_members_active_unique
  on procohere.project_members (project_id, team_member_id)
  where is_deleted = false;

create index if not exists ix_project_members_active_by_project
  on procohere.project_members (project_id, id)
  where is_deleted = false;

create index if not exists ix_project_members_active_by_member
  on procohere.project_members (team_member_id, project_id, id)
  where is_deleted = false;

create index if not exists ix_project_members_purge
  on procohere.project_members (deleted_at, id)
  where is_deleted = true;

do $$
begin
  if not exists (select 1 from pg_trigger where tgname = 'tr_project_members_set_updated_at') then
    create trigger tr_project_members_set_updated_at
      before update on procohere.project_members
      for each row
      execute function public.set_updated_at();
  end if;
end $$;

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
  add constraint project_links_organization_id_fkey
  foreign key (organization_id) references public.organizations(id);

alter table procohere.project_links
  add constraint project_links_project_id_fkey
  foreign key (project_id) references procohere.projects(id);

alter table procohere.project_links
  add constraint project_links_created_by_fkey
  foreign key (created_by_team_member_id) references procohere.team_members(id);

alter table procohere.project_links
  add constraint project_links_deleted_by_fkey
  foreign key (deleted_by) references procohere.team_members(id);

create unique index if not exists ux_project_links_active_unique
  on procohere.project_links (project_id, entity_type, entity_id)
  where is_deleted = false;

create index if not exists ix_project_links_active_by_project
  on procohere.project_links (project_id, created_at desc, id)
  where is_deleted = false;

create index if not exists ix_project_links_active_by_entity
  on procohere.project_links (entity_type, entity_id, project_id, id)
  where is_deleted = false;

create index if not exists ix_project_links_purge
  on procohere.project_links (deleted_at, id)
  where is_deleted = true;

do $$
begin
  if not exists (select 1 from pg_trigger where tgname = 'tr_project_links_set_updated_at') then
    create trigger tr_project_links_set_updated_at
      before update on procohere.project_links
      for each row
      execute function public.set_updated_at();
  end if;
end $$;

commit;
```

---

## SQL — RLS Helpers, Policies (all three tables)

```sql
begin;

create or replace function procohere.rls_can_see_project(p_project_id uuid)
returns boolean
language sql
stable
security definer
set search_path to 'public', 'procohere'
as $$
  select exists (
    select 1
    from procohere.projects p
    where p.id = p_project_id
      and p.organization_id = procohere.get_current_organization_id()
      and p.is_deleted = false
      and (
        p.owner_team_member_id = procohere.get_current_team_member_id()
        or exists (
          select 1
          from procohere.project_members pm
          where pm.project_id = p.id
            and pm.organization_id = p.organization_id
            and pm.team_member_id = procohere.get_current_team_member_id()
            and pm.is_deleted = false
        )
      )
  );
$$;

create or replace function procohere.rls_is_project_owner(p_project_id uuid)
returns boolean
language sql
stable
security definer
set search_path to 'public', 'procohere'
as $$
  select exists (
    select 1
    from procohere.projects p
    where p.id = p_project_id
      and p.organization_id = procohere.get_current_organization_id()
      and p.is_deleted = false
      and p.owner_team_member_id = procohere.get_current_team_member_id()
  );
$$;

alter table procohere.projects enable row level security;
alter table procohere.project_members enable row level security;
alter table procohere.project_links enable row level security;

drop policy if exists projects_select on procohere.projects;
create policy projects_select
on procohere.projects
for select
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and is_deleted = false
  and (
    owner_team_member_id = procohere.get_current_team_member_id()
    or exists (
      select 1
      from procohere.project_members pm
      where pm.project_id = id
        and pm.organization_id = organization_id
        and pm.team_member_id = procohere.get_current_team_member_id()
        and pm.is_deleted = false
    )
  )
);

drop policy if exists projects_insert on procohere.projects;
create policy projects_insert
on procohere.projects
for insert
to authenticated
with check (
  organization_id = procohere.get_current_organization_id()
  and owner_team_member_id = procohere.get_current_team_member_id()
);

drop policy if exists projects_update on procohere.projects;
create policy projects_update
on procohere.projects
for update
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and is_deleted = false
  and owner_team_member_id = procohere.get_current_team_member_id()
)
with check (
  organization_id = procohere.get_current_organization_id()
  and owner_team_member_id = procohere.get_current_team_member_id()
);

drop policy if exists projects_delete on procohere.projects;
create policy projects_delete
on procohere.projects
for delete
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and owner_team_member_id = procohere.get_current_team_member_id()
);

drop policy if exists project_members_select on procohere.project_members;
create policy project_members_select
on procohere.project_members
for select
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and is_deleted = false
  and procohere.rls_can_see_project(project_id)
);

drop policy if exists project_members_insert on procohere.project_members;
create policy project_members_insert
on procohere.project_members
for insert
to authenticated
with check (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
);

drop policy if exists project_members_update on procohere.project_members;
create policy project_members_update
on procohere.project_members
for update
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
)
with check (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
);

drop policy if exists project_members_delete on procohere.project_members;
create policy project_members_delete
on procohere.project_members
for delete
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
);

drop policy if exists project_links_select on procohere.project_links;
create policy project_links_select
on procohere.project_links
for select
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and is_deleted = false
  and procohere.rls_can_see_project(project_id)
);

drop policy if exists project_links_insert on procohere.project_links;
create policy project_links_insert
on procohere.project_links
for insert
to authenticated
with check (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
  and created_by_team_member_id = procohere.get_current_team_member_id()
);

drop policy if exists project_links_update on procohere.project_links;
create policy project_links_update
on procohere.project_links
for update
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
)
with check (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
);

drop policy if exists project_links_delete on procohere.project_links;
create policy project_links_delete
on procohere.project_links
for delete
to authenticated
using (
  organization_id = procohere.get_current_organization_id()
  and procohere.rls_is_project_owner(project_id)
);

commit;
```

---

## SQL — RPCs (all requested)

```sql
create or replace function procohere.rpc_create_project(
  p_title text,
  p_description text default null,
  p_target_date date default null,
  p_start_date date default null
)
returns procohere.projects
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_org_id uuid;
  v_tm_id uuid;
  v_row procohere.projects;
begin
  v_org_id := procohere.get_current_organization_id();
  v_tm_id := procohere.get_current_team_member_id();

  insert into procohere.projects
  (
    organization_id,
    owner_team_member_id,
    title,
    description,
    start_date,
    target_date
  )
  values
  (
    v_org_id,
    v_tm_id,
    p_title,
    p_description,
    p_start_date,
    p_target_date
  )
  returning * into v_row;

  return v_row;
end;
$$;

create or replace function procohere.rpc_update_project(
  p_project_id uuid,
  p_title text default null,
  p_description text default null,
  p_status procohere.project_status default null,
  p_start_date date default null,
  p_target_date date default null,
  p_is_archived boolean default null
)
returns procohere.projects
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_row procohere.projects;
begin
  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can update the project.';
  end if;

  update procohere.projects
  set
    title = coalesce(p_title, title),
    description = coalesce(p_description, description),
    status = coalesce(p_status, status),
    start_date = coalesce(p_start_date, start_date),
    target_date = coalesce(p_target_date, target_date),
    is_archived = coalesce(p_is_archived, is_archived),
    archived_at = case
      when coalesce(p_is_archived, is_archived) = true and archived_at is null then now()
      when coalesce(p_is_archived, is_archived) = false then null
      else archived_at
    end
  where id = p_project_id
  returning * into v_row;

  if v_row.id is null then
    raise exception 'Project not found.';
  end if;

  return v_row;
end;
$$;

create or replace function procohere.rpc_delete_project(
  p_project_id uuid
)
returns void
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_tm_id uuid;
begin
  v_tm_id := procohere.get_current_team_member_id();

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can delete the project.';
  end if;

  update procohere.projects
  set
    is_deleted = true,
    deleted_at = now(),
    deleted_by = v_tm_id
  where id = p_project_id
    and is_deleted = false;

  update procohere.project_members
  set
    is_deleted = true,
    deleted_at = now(),
    deleted_by = v_tm_id
  where project_id = p_project_id
    and is_deleted = false;

  update procohere.project_links
  set
    is_deleted = true,
    deleted_at = now(),
    deleted_by = v_tm_id
  where project_id = p_project_id
    and is_deleted = false;
end;
$$;

create or replace function procohere.rpc_add_project_member(
  p_project_id uuid,
  p_team_member_id uuid,
  p_role procohere.project_member_role default 'member'
)
returns procohere.project_members
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_org_id uuid;
  v_row procohere.project_members;
begin
  v_org_id := procohere.get_current_organization_id();

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can add members.';
  end if;

  insert into procohere.project_members
  (
    organization_id,
    project_id,
    team_member_id,
    role
  )
  values
  (
    v_org_id,
    p_project_id,
    p_team_member_id,
    p_role
  )
  on conflict on constraint ux_project_members_active_unique
  do update set
    role = excluded.role,
    updated_at = now()
  returning * into v_row;

  return v_row;
end;
$$;

create or replace function procohere.rpc_remove_project_member(
  p_project_id uuid,
  p_team_member_id uuid
)
returns void
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_tm_id uuid;
begin
  v_tm_id := procohere.get_current_team_member_id();

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can remove members.';
  end if;

  update procohere.project_members
  set
    is_deleted = true,
    deleted_at = now(),
    deleted_by = v_tm_id
  where project_id = p_project_id
    and team_member_id = p_team_member_id
    and is_deleted = false;
end;
$$;

create or replace function procohere.rpc_add_project_link(
  p_project_id uuid,
  p_entity_type procohere.project_link_entity_type,
  p_entity_id uuid,
  p_entity_title_snapshot text default null
)
returns procohere.project_links
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_org_id uuid;
  v_tm_id uuid;
  v_row procohere.project_links;
begin
  v_org_id := procohere.get_current_organization_id();
  v_tm_id := procohere.get_current_team_member_id();

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can add links.';
  end if;

  insert into procohere.project_links
  (
    organization_id,
    project_id,
    entity_type,
    entity_id,
    entity_title_snapshot,
    created_by_team_member_id
  )
  values
  (
    v_org_id,
    p_project_id,
    p_entity_type,
    p_entity_id,
    p_entity_title_snapshot,
    v_tm_id
  )
  on conflict on constraint ux_project_links_active_unique
  do update set
    entity_title_snapshot = excluded.entity_title_snapshot,
    updated_at = now()
  returning * into v_row;

  return v_row;
end;
$$;

create or replace function procohere.rpc_remove_project_link(
  p_project_id uuid,
  p_entity_type procohere.project_link_entity_type,
  p_entity_id uuid
)
returns void
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_tm_id uuid;
begin
  v_tm_id := procohere.get_current_team_member_id();

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can remove links.';
  end if;

  update procohere.project_links
  set
    is_deleted = true,
    deleted_at = now(),
    deleted_by = v_tm_id
  where project_id = p_project_id
    and entity_type = p_entity_type
    and entity_id = p_entity_id
    and is_deleted = false;
end;
$$;

create or replace function procohere.rpc_transfer_project_ownership(
  p_project_id uuid,
  p_new_owner_team_member_id uuid
)
returns procohere.projects
language plpgsql
security definer
set search_path to 'public', 'procohere'
as $$
declare
  v_org_id uuid;
  v_actor_tm_id uuid;
  v_row procohere.projects;
  v_target_active boolean;
begin
  v_org_id := procohere.get_current_organization_id();
  v_actor_tm_id := procohere.get_current_team_member_id();

  if p_project_id is null then
    raise exception 'ProjectId is required.';
  end if;

  if p_new_owner_team_member_id is null then
    raise exception 'NewOwnerTeamMemberId is required.';
  end if;

  if not procohere.rls_is_project_owner(p_project_id) then
    raise exception 'Only the project owner can transfer ownership.';
  end if;

  select exists (
    select 1
    from procohere.team_members tm
    where tm.id = p_new_owner_team_member_id
      and tm.organization_id = v_org_id
      and tm.is_deleted = false
      and tm.is_active = true
  )
  into v_target_active;

  if v_target_active = false then
    raise exception 'New owner must be an active team member in the organization.';
  end if;

  update procohere.projects p
  set
    owner_team_member_id = p_new_owner_team_member_id
  where p.id = p_project_id
    and p.organization_id = v_org_id
    and p.is_deleted = false
  returning * into v_row;

  if v_row.id is null then
    raise exception 'Project not found.';
  end if;

  return v_row;
end;
$$;
```

---

## Notes for Claude

- The RPCs are `SECURITY DEFINER` and use owner-check helpers to enforce write rules.
- RLS remains enabled to protect direct table access via PostgREST.
- Soft-delete is implemented for all three tables; purge happens later.

