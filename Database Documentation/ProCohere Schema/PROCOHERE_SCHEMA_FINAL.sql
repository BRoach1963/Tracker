-- ============================================================
-- PROCOHERE SCHEMA - COMPLETE TABLE DEFINITIONS
-- Version: 1.0
-- Date: 2026-01-17
--
-- Prerequisites:
--   - public schema with organizations, users tables
--   - public.set_updated_at() trigger function
--
-- Security model:
--   - RLS enabled on all tables
--   - authenticated role gets SELECT on tables
--   - all writes go through RPCs (grant EXECUTE per-RPC)
--
-- Notes:
--   - roles and team_members are included but COMMENTED OUT because you already seeded data.
--   - This script is idempotent (safe to re-run):
--       * tables/indexes/triggers use IF NOT EXISTS / DROP IF EXISTS
--       * policies are dropped and recreated
-- ============================================================

create schema if not exists procohere;

-- ============================================================
-- 1. ROLES (COMMENTED OUT - Already exists and contains data)
-- ============================================================
/*
create table if not exists procohere.roles (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    name            text not null,
    description     text,
    permissions     jsonb not null default '{}'::jsonb,
    is_system_role  boolean not null default false,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_roles_organization_id
    on procohere.roles(organization_id)
    where is_deleted = false;

create index if not exists idx_roles_is_system
    on procohere.roles(is_system_role)
    where is_deleted = false;

create unique index if not exists uq_roles_org_name_active
    on procohere.roles (organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_roles_set_updated_at on procohere.roles;
create trigger tr_roles_set_updated_at
    before update on procohere.roles
    for each row execute function public.set_updated_at();
*/

-- ============================================================
-- 2. TEAM_MEMBERS (COMMENTED OUT - Already exists and contains data)
-- ============================================================
/*
create table if not exists procohere.team_members (
    id                      uuid primary key default gen_random_uuid(),
    organization_id         uuid not null references public.organizations(id),
    linked_user_id          uuid references public.users(id),
    role_id                 uuid not null references procohere.roles(id),
    manager_team_member_id  uuid references procohere.team_members(id),
    first_name              text not null,
    last_name               text not null,
    email                   text,
    job_title               text,
    department              text,
    hire_date               date,
    is_active               boolean not null default true,
    is_deleted              boolean not null default false,
    created_at              timestamptz not null default now(),
    updated_at              timestamptz not null default now(),
    deleted_at              timestamptz,
    deleted_by              uuid references public.users(id)
);

create index if not exists idx_team_members_org
    on procohere.team_members(organization_id) where is_deleted = false;

create index if not exists idx_team_members_user
    on procohere.team_members(linked_user_id) where is_deleted = false and linked_user_id is not null;

create index if not exists idx_team_members_manager
    on procohere.team_members(manager_team_member_id) where is_deleted = false;

create index if not exists idx_team_members_role
    on procohere.team_members(role_id) where is_deleted = false;

create unique index if not exists uq_team_members_user_org
    on procohere.team_members(organization_id, linked_user_id)
    where is_deleted = false and linked_user_id is not null;

drop trigger if exists tr_team_members_set_updated_at on procohere.team_members;
create trigger tr_team_members_set_updated_at
    before update on procohere.team_members
    for each row execute function public.set_updated_at();
*/

-- ============================================================
-- 3. TEAMS
-- ============================================================
create table if not exists procohere.teams (
    id                  uuid primary key default gen_random_uuid(),
    organization_id     uuid not null references public.organizations(id),
    parent_team_id      uuid references procohere.teams(id),
    name                text not null,
    description         text,
    lead_team_member_id uuid references procohere.team_members(id),
    is_deleted          boolean not null default false,
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now(),
    deleted_at          timestamptz,
    deleted_by          uuid references public.users(id)
);

create index if not exists idx_teams_org
    on procohere.teams(organization_id) where is_deleted = false;

create index if not exists idx_teams_parent
    on procohere.teams(parent_team_id) where is_deleted = false and parent_team_id is not null;

create unique index if not exists uq_teams_org_name
    on procohere.teams(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_teams_set_updated_at on procohere.teams;
create trigger tr_teams_set_updated_at
    before update on procohere.teams
    for each row execute function public.set_updated_at();

-- ============================================================
-- 4. ORG_SETTINGS
-- ============================================================
create table if not exists procohere.org_settings (
    id                        uuid primary key default gen_random_uuid(),
    organization_id           uuid not null references public.organizations(id),
    default_meeting_duration  int default 30,
    meeting_reminder_minutes  int default 15,
    require_agenda            boolean not null default false,
    require_notes             boolean not null default false,
    enable_ai_features        boolean not null default true,
    enable_anonymous_feedback boolean not null default true,
    fiscal_year_start_month   int default 1,
    goal_cycle_type           text default 'quarterly',
    settings_json             jsonb not null default '{}'::jsonb,
    is_deleted                boolean not null default false,
    created_at                timestamptz not null default now(),
    updated_at                timestamptz not null default now(),
    deleted_at                timestamptz,
    deleted_by                uuid references public.users(id)
);

create unique index if not exists uq_org_settings_org
    on procohere.org_settings(organization_id)
    where is_deleted = false;

drop trigger if exists tr_org_settings_set_updated_at on procohere.org_settings;
create trigger tr_org_settings_set_updated_at
    before update on procohere.org_settings
    for each row execute function public.set_updated_at();

-- ============================================================
-- 5. MEETINGS
-- ============================================================
create table if not exists procohere.meetings (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    title             text not null,
    description       text,
    meeting_type      text not null default 'one_on_one',
    status            text not null default 'scheduled',
    scheduled_at      timestamptz,
    started_at        timestamptz,
    ended_at          timestamptz,
    duration_minutes  int,
    location          text,
    video_link        text,
    recurrence_rule   text,
    parent_meeting_id uuid references procohere.meetings(id),
    created_by        uuid not null references procohere.team_members(id),
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_meetings_org
    on procohere.meetings(organization_id) where is_deleted = false;

create index if not exists idx_meetings_created_by
    on procohere.meetings(created_by) where is_deleted = false;

create index if not exists idx_meetings_scheduled
    on procohere.meetings(scheduled_at) where is_deleted = false;

create index if not exists idx_meetings_status
    on procohere.meetings(status) where is_deleted = false;

create index if not exists idx_meetings_parent
    on procohere.meetings(parent_meeting_id) where is_deleted = false and parent_meeting_id is not null;

drop trigger if exists tr_meetings_set_updated_at on procohere.meetings;
create trigger tr_meetings_set_updated_at
    before update on procohere.meetings
    for each row execute function public.set_updated_at();

-- ============================================================
-- 6. MEETING_ATTENDEES
-- ============================================================
create table if not exists procohere.meeting_attendees (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    meeting_id      uuid not null references procohere.meetings(id),
    team_member_id  uuid not null references procohere.team_members(id),
    role            text not null default 'attendee',
    response_status text not null default 'pending',
    attended        boolean,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_meeting_attendees_org
    on procohere.meeting_attendees(organization_id) where is_deleted = false;

create index if not exists idx_meeting_attendees_meeting
    on procohere.meeting_attendees(meeting_id) where is_deleted = false;

create index if not exists idx_meeting_attendees_member
    on procohere.meeting_attendees(team_member_id) where is_deleted = false;

create unique index if not exists uq_meeting_attendees_meeting_member
    on procohere.meeting_attendees(meeting_id, team_member_id)
    where is_deleted = false;

drop trigger if exists tr_meeting_attendees_set_updated_at on procohere.meeting_attendees;
create trigger tr_meeting_attendees_set_updated_at
    before update on procohere.meeting_attendees
    for each row execute function public.set_updated_at();

-- ============================================================
-- 7. MEETING_AGENDA_ITEMS
-- ============================================================
create table if not exists procohere.meeting_agenda_items (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    meeting_id        uuid not null references procohere.meetings(id),
    added_by          uuid not null references procohere.team_members(id),
    title             text not null,
    description       text,
    sort_order        int not null default 0,
    is_private        boolean not null default false,
    is_completed      boolean not null default false,  -- DEPRECATED: use status instead
    completed_at      timestamptz,                     -- DEPRECATED: kept for compatibility
    status            text not null default 'open',    -- 'open', 'discussed', 'action_created', 'deferred', 'dropped'
    linked_entity_type text,
    linked_entity_id  uuid,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_agenda_items_org
    on procohere.meeting_agenda_items(organization_id) where is_deleted = false;

create index if not exists idx_agenda_items_meeting
    on procohere.meeting_agenda_items(meeting_id) where is_deleted = false;

create index if not exists idx_agenda_items_added_by
    on procohere.meeting_agenda_items(added_by) where is_deleted = false;

create index if not exists idx_meeting_agenda_items_status
    on procohere.meeting_agenda_items(status) where is_deleted = false;

-- Composite index for org-wide status queries (carry-forward, open items)
create index if not exists idx_meeting_agenda_items_org_status
    on procohere.meeting_agenda_items(organization_id, status) where is_deleted = false;

drop trigger if exists tr_meeting_agenda_items_set_updated_at on procohere.meeting_agenda_items;
create trigger tr_meeting_agenda_items_set_updated_at
    before update on procohere.meeting_agenda_items
    for each row execute function public.set_updated_at();

-- ============================================================
-- 8. MEETING_NOTES
-- ============================================================
create table if not exists procohere.meeting_notes (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    meeting_id      uuid not null references procohere.meetings(id),
    author_id       uuid not null references procohere.team_members(id),
    content         text not null,
    is_shared       boolean not null default false,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_meeting_notes_org
    on procohere.meeting_notes(organization_id) where is_deleted = false;

create index if not exists idx_meeting_notes_meeting
    on procohere.meeting_notes(meeting_id) where is_deleted = false;

create index if not exists idx_meeting_notes_author
    on procohere.meeting_notes(author_id) where is_deleted = false;

drop trigger if exists tr_meeting_notes_set_updated_at on procohere.meeting_notes;
create trigger tr_meeting_notes_set_updated_at
    before update on procohere.meeting_notes
    for each row execute function public.set_updated_at();

-- ============================================================
-- 9. MEETING_SUMMARIES
-- ============================================================
create table if not exists procohere.meeting_summaries (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    meeting_id      uuid not null references procohere.meetings(id),
    summary         text not null,
    key_decisions   jsonb,
    action_items    jsonb,
    topics_discussed jsonb,
    sentiment       text,
    generated_by    text,
    is_approved     boolean not null default false,
    approved_by     uuid references procohere.team_members(id),
    approved_at     timestamptz,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_meeting_summaries_org
    on procohere.meeting_summaries(organization_id) where is_deleted = false;

create index if not exists idx_meeting_summaries_meeting
    on procohere.meeting_summaries(meeting_id) where is_deleted = false;

create unique index if not exists uq_meeting_summaries_meeting
    on procohere.meeting_summaries(meeting_id)
    where is_deleted = false;

drop trigger if exists tr_meeting_summaries_set_updated_at on procohere.meeting_summaries;
create trigger tr_meeting_summaries_set_updated_at
    before update on procohere.meeting_summaries
    for each row execute function public.set_updated_at();

-- ============================================================
-- 10. MEETING_TEMPLATES
-- ============================================================
create table if not exists procohere.meeting_templates (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    created_by        uuid not null references procohere.team_members(id),
    name              text not null,
    description       text,
    meeting_type      text not null default 'one_on_one',
    default_duration  int default 30,
    default_agenda    jsonb,
    is_system_template boolean not null default false,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_meeting_templates_org
    on procohere.meeting_templates(organization_id) where is_deleted = false;

create unique index if not exists uq_meeting_templates_org_name
    on procohere.meeting_templates(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_meeting_templates_set_updated_at on procohere.meeting_templates;
create trigger tr_meeting_templates_set_updated_at
    before update on procohere.meeting_templates
    for each row execute function public.set_updated_at();

-- ============================================================
-- 11. GOAL_CATEGORIES (MOVED BEFORE GOALS to satisfy FK)
-- ============================================================
create table if not exists procohere.goal_categories (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    name            text not null,
    description     text,
    color           text,
    sort_order      int not null default 0,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_goal_categories_org
    on procohere.goal_categories(organization_id) where is_deleted = false;

create unique index if not exists uq_goal_categories_org_name
    on procohere.goal_categories(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_goal_categories_set_updated_at on procohere.goal_categories;
create trigger tr_goal_categories_set_updated_at
    before update on procohere.goal_categories
    for each row execute function public.set_updated_at();

-- ============================================================
-- 12. GOALS
-- ============================================================
create table if not exists procohere.goals (
    id               uuid primary key default gen_random_uuid(),
    organization_id  uuid not null references public.organizations(id),
    owner_id         uuid not null references procohere.team_members(id),
    parent_goal_id   uuid references procohere.goals(id),
    category_id      uuid references procohere.goal_categories(id),
    title            text not null,
    description      text,
    goal_type        text not null default 'individual',
    status           text not null default 'not_started',
    priority         text default 'medium',
    start_date       date,
    due_date         date,
    completed_at     timestamptz,
    progress_percent int not null default 0 check (progress_percent >= 0 and progress_percent <= 100),
    is_deleted       boolean not null default false,
    created_at       timestamptz not null default now(),
    updated_at       timestamptz not null default now(),
    deleted_at       timestamptz,
    deleted_by       uuid references public.users(id)
);

create index if not exists idx_goals_org
    on procohere.goals(organization_id) where is_deleted = false;

create index if not exists idx_goals_owner
    on procohere.goals(owner_id) where is_deleted = false;

create index if not exists idx_goals_parent
    on procohere.goals(parent_goal_id) where is_deleted = false and parent_goal_id is not null;

create index if not exists idx_goals_category
    on procohere.goals(category_id) where is_deleted = false and category_id is not null;

create index if not exists idx_goals_status
    on procohere.goals(status) where is_deleted = false;

create index if not exists idx_goals_due_date
    on procohere.goals(due_date) where is_deleted = false;

drop trigger if exists tr_goals_set_updated_at on procohere.goals;
create trigger tr_goals_set_updated_at
    before update on procohere.goals
    for each row execute function public.set_updated_at();

-- ============================================================
-- 13. TARGETS
-- ============================================================
create table if not exists procohere.targets (
    id            uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    goal_id        uuid not null references procohere.goals(id),
    title          text not null,
    description    text,
    target_type    text not null default 'numeric',
    target_value   numeric,
    current_value  numeric not null default 0,
    unit           text,
    status         text not null default 'not_started',
    due_date       date,
    completed_at   timestamptz,
    sort_order     int not null default 0,
    is_deleted     boolean not null default false,
    created_at     timestamptz not null default now(),
    updated_at     timestamptz not null default now(),
    deleted_at     timestamptz,
    deleted_by     uuid references public.users(id)
);

create index if not exists idx_targets_org
    on procohere.targets(organization_id) where is_deleted = false;

create index if not exists idx_targets_goal
    on procohere.targets(goal_id) where is_deleted = false;

drop trigger if exists tr_targets_set_updated_at on procohere.targets;
create trigger tr_targets_set_updated_at
    before update on procohere.targets
    for each row execute function public.set_updated_at();

-- ============================================================
-- 14. GOAL_TEMPLATES
-- ============================================================
create table if not exists procohere.goal_templates (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    created_by      uuid not null references procohere.team_members(id),
    category_id     uuid references procohere.goal_categories(id),
    name            text not null,
    description     text,
    goal_type       text not null default 'individual',
    default_targets jsonb,
    is_system_template boolean not null default false,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_goal_templates_org
    on procohere.goal_templates(organization_id) where is_deleted = false;

create unique index if not exists uq_goal_templates_org_name
    on procohere.goal_templates(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_goal_templates_set_updated_at on procohere.goal_templates;
create trigger tr_goal_templates_set_updated_at
    before update on procohere.goal_templates
    for each row execute function public.set_updated_at();

-- ============================================================
-- 15. TASKS
-- ============================================================
create table if not exists procohere.tasks (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    assigned_to     uuid references procohere.team_members(id),
    created_by      uuid not null references procohere.team_members(id),
    title           text not null,
    description     text,
    status          text not null default 'todo',
    priority        text default 'medium',
    due_date        timestamptz,
    completed_at    timestamptz,
    source_type     text,
    source_id       uuid,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_tasks_org
    on procohere.tasks(organization_id) where is_deleted = false;

create index if not exists idx_tasks_assigned_to
    on procohere.tasks(assigned_to) where is_deleted = false;

create index if not exists idx_tasks_created_by
    on procohere.tasks(created_by) where is_deleted = false;

create index if not exists idx_tasks_status
    on procohere.tasks(status) where is_deleted = false;

create index if not exists idx_tasks_due_date
    on procohere.tasks(due_date) where is_deleted = false and due_date is not null;

create index if not exists idx_tasks_source
    on procohere.tasks(source_type, source_id) where is_deleted = false;

-- Composite index for org-scoped provenance queries ("tasks spawned from X")
create index if not exists idx_tasks_org_source
    on procohere.tasks(organization_id, source_type, source_id) where is_deleted = false;

-- Provenance integrity: source_type and source_id must both be null or both be set
-- Note: 'manual' tasks are represented by (NULL, NULL) - no explicit 'manual' value needed
alter table procohere.tasks
    add constraint if not exists chk_tasks_source_type
    check (source_type is null or source_type in (
        'meeting', 'agenda_item', 'goal', 'feedback', 'note'
    ));

alter table procohere.tasks
    add constraint if not exists chk_tasks_source_pair
    check (
        (source_type is null and source_id is null)
        or (source_type is not null and source_id is not null)
    );

drop trigger if exists tr_tasks_set_updated_at on procohere.tasks;
create trigger tr_tasks_set_updated_at
    before update on procohere.tasks
    for each row execute function public.set_updated_at();

-- ============================================================
-- 16. FEEDBACK
-- ============================================================
create table if not exists procohere.feedback (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    from_member_id  uuid not null references procohere.team_members(id),
    to_member_id    uuid not null references procohere.team_members(id),
    feedback_type   text not null default 'general',
    title           text,
    content         text not null,
    visibility      text not null default 'private',
    is_anonymous    boolean not null default false,
    rating          int check (rating >= 1 and rating <= 5),
    meeting_id      uuid references procohere.meetings(id),
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_feedback_org
    on procohere.feedback(organization_id) where is_deleted = false;

create index if not exists idx_feedback_from
    on procohere.feedback(from_member_id) where is_deleted = false;

create index if not exists idx_feedback_to
    on procohere.feedback(to_member_id) where is_deleted = false;

create index if not exists idx_feedback_meeting
    on procohere.feedback(meeting_id) where is_deleted = false and meeting_id is not null;

drop trigger if exists tr_feedback_set_updated_at on procohere.feedback;
create trigger tr_feedback_set_updated_at
    before update on procohere.feedback
    for each row execute function public.set_updated_at();

-- ============================================================
-- 17. FEEDBACK_TEMPLATES
-- ============================================================
create table if not exists procohere.feedback_templates (
    id               uuid primary key default gen_random_uuid(),
    organization_id  uuid not null references public.organizations(id),
    created_by       uuid not null references procohere.team_members(id),
    name             text not null,
    description      text,
    feedback_type    text not null default 'general',
    prompts          jsonb,
    is_system_template boolean not null default false,
    is_deleted       boolean not null default false,
    created_at       timestamptz not null default now(),
    updated_at       timestamptz not null default now(),
    deleted_at       timestamptz,
    deleted_by       uuid references public.users(id)
);

create index if not exists idx_feedback_templates_org
    on procohere.feedback_templates(organization_id) where is_deleted = false;

create unique index if not exists uq_feedback_templates_org_name
    on procohere.feedback_templates(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_feedback_templates_set_updated_at on procohere.feedback_templates;
create trigger tr_feedback_templates_set_updated_at
    before update on procohere.feedback_templates
    for each row execute function public.set_updated_at();

-- ============================================================
-- 18. NOTES
-- ============================================================
create table if not exists procohere.notes (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    created_by      uuid not null references procohere.team_members(id),
    meeting_id      uuid references procohere.meetings(id),
    team_member_id  uuid references procohere.team_members(id),
    title           text,
    content         text not null,
    is_private      boolean not null default true,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_notes_org
    on procohere.notes(organization_id) where is_deleted = false;

create index if not exists idx_notes_created_by
    on procohere.notes(created_by) where is_deleted = false;

create index if not exists idx_notes_meeting
    on procohere.notes(meeting_id) where is_deleted = false and meeting_id is not null;

create index if not exists idx_notes_about_member
    on procohere.notes(team_member_id) where is_deleted = false and team_member_id is not null;

drop trigger if exists tr_notes_set_updated_at on procohere.notes;
create trigger tr_notes_set_updated_at
    before update on procohere.notes
    for each row execute function public.set_updated_at();

-- ============================================================
-- 19. METRICS
-- ============================================================
create table if not exists procohere.metrics (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    owner_id        uuid references procohere.team_members(id),
    name            text not null,
    description     text,
    metric_type     text not null default 'number',
    unit            text,
    target_value    numeric,
    current_value   numeric,
    direction       text default 'higher_is_better',
    frequency       text default 'weekly',
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_metrics_org
    on procohere.metrics(organization_id) where is_deleted = false;

create index if not exists idx_metrics_owner
    on procohere.metrics(owner_id) where is_deleted = false and owner_id is not null;

create unique index if not exists uq_metrics_org_name
    on procohere.metrics(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_metrics_set_updated_at on procohere.metrics;
create trigger tr_metrics_set_updated_at
    before update on procohere.metrics
    for each row execute function public.set_updated_at();

-- ============================================================
-- 20. METRIC_VALUES
-- ============================================================
create table if not exists procohere.metric_values (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    metric_id       uuid not null references procohere.metrics(id),
    recorded_by     uuid references procohere.team_members(id),
    value           numeric not null,
    recorded_at     timestamptz not null default now(),
    notes           text,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_metric_values_org
    on procohere.metric_values(organization_id) where is_deleted = false;

create index if not exists idx_metric_values_metric
    on procohere.metric_values(metric_id) where is_deleted = false;

create index if not exists idx_metric_values_recorded_at
    on procohere.metric_values(recorded_at) where is_deleted = false;

drop trigger if exists tr_metric_values_set_updated_at on procohere.metric_values;
create trigger tr_metric_values_set_updated_at
    before update on procohere.metric_values
    for each row execute function public.set_updated_at();

-- ============================================================
-- 21. SURVEYS
-- ============================================================
create table if not exists procohere.surveys (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    created_by      uuid not null references procohere.team_members(id),
    title           text not null,
    description     text,
    status          text not null default 'draft',
    is_anonymous    boolean not null default false,
    starts_at       timestamptz,
    ends_at         timestamptz,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_surveys_org
    on procohere.surveys(organization_id) where is_deleted = false;

create index if not exists idx_surveys_created_by
    on procohere.surveys(created_by) where is_deleted = false;

create index if not exists idx_surveys_status
    on procohere.surveys(status) where is_deleted = false;

drop trigger if exists tr_surveys_set_updated_at on procohere.surveys;
create trigger tr_surveys_set_updated_at
    before update on procohere.surveys
    for each row execute function public.set_updated_at();

-- ============================================================
-- 22. SURVEY_QUESTIONS
-- ============================================================
create table if not exists procohere.survey_questions (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    survey_id       uuid not null references procohere.surveys(id),
    question_text   text not null,
    question_type   text not null default 'text',
    options         jsonb,
    is_required     boolean not null default false,
    sort_order      int not null default 0,
    min_value       int,
    max_value       int,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_survey_questions_org
    on procohere.survey_questions(organization_id) where is_deleted = false;

create index if not exists idx_survey_questions_survey
    on procohere.survey_questions(survey_id) where is_deleted = false;

drop trigger if exists tr_survey_questions_set_updated_at on procohere.survey_questions;
create trigger tr_survey_questions_set_updated_at
    before update on procohere.survey_questions
    for each row execute function public.set_updated_at();

-- ============================================================
-- 23. SURVEY_RESPONSES
-- ============================================================
create table if not exists procohere.survey_responses (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    survey_id       uuid not null references procohere.surveys(id),
    respondent_id   uuid references procohere.team_members(id),
    submitted_at    timestamptz,
    is_complete     boolean not null default false,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_survey_responses_org
    on procohere.survey_responses(organization_id) where is_deleted = false;

create index if not exists idx_survey_responses_survey
    on procohere.survey_responses(survey_id) where is_deleted = false;

create index if not exists idx_survey_responses_respondent
    on procohere.survey_responses(respondent_id) where is_deleted = false and respondent_id is not null;

create unique index if not exists uq_survey_responses_respondent
    on procohere.survey_responses(survey_id, respondent_id)
    where is_deleted = false and respondent_id is not null;

drop trigger if exists tr_survey_responses_set_updated_at on procohere.survey_responses;
create trigger tr_survey_responses_set_updated_at
    before update on procohere.survey_responses
    for each row execute function public.set_updated_at();

-- ============================================================
-- 24. SURVEY_ANSWERS
-- ============================================================
create table if not exists procohere.survey_answers (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    response_id     uuid not null references procohere.survey_responses(id),
    question_id     uuid not null references procohere.survey_questions(id),
    answer_text     text,
    answer_numeric  numeric,
    answer_json     jsonb,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_survey_answers_org
    on procohere.survey_answers(organization_id) where is_deleted = false;

create index if not exists idx_survey_answers_response
    on procohere.survey_answers(response_id) where is_deleted = false;

create index if not exists idx_survey_answers_question
    on procohere.survey_answers(question_id) where is_deleted = false;

create unique index if not exists uq_survey_answers_response_question
    on procohere.survey_answers(response_id, question_id)
    where is_deleted = false;

drop trigger if exists tr_survey_answers_set_updated_at on procohere.survey_answers;
create trigger tr_survey_answers_set_updated_at
    before update on procohere.survey_answers
    for each row execute function public.set_updated_at();

-- ============================================================
-- 25. AI_CONVERSATIONS
-- ============================================================
create table if not exists procohere.ai_conversations (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    team_member_id  uuid not null references procohere.team_members(id),
    title           text,
    context_type    text,
    context_id      uuid,
    model_used      text,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_ai_conversations_org
    on procohere.ai_conversations(organization_id) where is_deleted = false;

create index if not exists idx_ai_conversations_member
    on procohere.ai_conversations(team_member_id) where is_deleted = false;

drop trigger if exists tr_ai_conversations_set_updated_at on procohere.ai_conversations;
create trigger tr_ai_conversations_set_updated_at
    before update on procohere.ai_conversations
    for each row execute function public.set_updated_at();

-- ============================================================
-- 26. AI_MESSAGES
-- ============================================================
create table if not exists procohere.ai_messages (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    conversation_id uuid not null references procohere.ai_conversations(id),
    role            text not null,
    content         text not null,
    tokens_used     int,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_ai_messages_org
    on procohere.ai_messages(organization_id) where is_deleted = false;

create index if not exists idx_ai_messages_conversation
    on procohere.ai_messages(conversation_id) where is_deleted = false;

drop trigger if exists tr_ai_messages_set_updated_at on procohere.ai_messages;
create trigger tr_ai_messages_set_updated_at
    before update on procohere.ai_messages
    for each row execute function public.set_updated_at();

-- ============================================================
-- 27. AI_INSIGHTS
-- ============================================================
create table if not exists procohere.ai_insights (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    team_member_id  uuid references procohere.team_members(id),
    generated_for   uuid not null references procohere.team_members(id),
    insight_type    text not null,
    title           text not null,
    content         text not null,
    source_type     text,
    source_id       uuid,
    relevance_score numeric,
    is_dismissed    boolean not null default false,
    dismissed_at    timestamptz,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_ai_insights_org
    on procohere.ai_insights(organization_id) where is_deleted = false;

create index if not exists idx_ai_insights_for
    on procohere.ai_insights(generated_for) where is_deleted = false;

create index if not exists idx_ai_insights_about
    on procohere.ai_insights(team_member_id) where is_deleted = false and team_member_id is not null;

drop trigger if exists tr_ai_insights_set_updated_at on procohere.ai_insights;
create trigger tr_ai_insights_set_updated_at
    before update on procohere.ai_insights
    for each row execute function public.set_updated_at();

-- ============================================================
-- 28. ATTACHMENTS
-- ============================================================
create table if not exists procohere.attachments (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    uploaded_by     uuid not null references procohere.team_members(id),
    entity_type     text not null,
    entity_id       uuid not null,
    file_name       text not null,
    file_size       bigint,
    mime_type       text,
    storage_path    text not null,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_attachments_org
    on procohere.attachments(organization_id) where is_deleted = false;

create index if not exists idx_attachments_entity
    on procohere.attachments(entity_type, entity_id) where is_deleted = false;

create index if not exists idx_attachments_uploaded_by
    on procohere.attachments(uploaded_by) where is_deleted = false;

drop trigger if exists tr_attachments_set_updated_at on procohere.attachments;
create trigger tr_attachments_set_updated_at
    before update on procohere.attachments
    for each row execute function public.set_updated_at();

-- ============================================================
-- 29. TAGS
-- ============================================================
create table if not exists procohere.tags (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    name            text not null,
    color           text,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_tags_org
    on procohere.tags(organization_id) where is_deleted = false;

create unique index if not exists uq_tags_org_name
    on procohere.tags(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_tags_set_updated_at on procohere.tags;
create trigger tr_tags_set_updated_at
    before update on procohere.tags
    for each row execute function public.set_updated_at();

-- ============================================================
-- 30. ENTITY_TAGS
-- ============================================================
create table if not exists procohere.entity_tags (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    tag_id          uuid not null references procohere.tags(id),
    entity_type     text not null,
    entity_id       uuid not null,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_entity_tags_org
    on procohere.entity_tags(organization_id) where is_deleted = false;

create index if not exists idx_entity_tags_tag
    on procohere.entity_tags(tag_id) where is_deleted = false;

create index if not exists idx_entity_tags_entity
    on procohere.entity_tags(entity_type, entity_id) where is_deleted = false;

create unique index if not exists uq_entity_tags_tag_entity
    on procohere.entity_tags(tag_id, entity_type, entity_id)
    where is_deleted = false;

drop trigger if exists tr_entity_tags_set_updated_at on procohere.entity_tags;
create trigger tr_entity_tags_set_updated_at
    before update on procohere.entity_tags
    for each row execute function public.set_updated_at();

-- ============================================================
-- 31. NOTIFICATIONS
-- ============================================================
create table if not exists procohere.notifications (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    team_member_id    uuid not null references procohere.team_members(id),
    notification_type text not null,
    title             text not null,
    message           text,
    entity_type       text,
    entity_id         uuid,
    is_read           boolean not null default false,
    read_at           timestamptz,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_notifications_org
    on procohere.notifications(organization_id) where is_deleted = false;

create index if not exists idx_notifications_member
    on procohere.notifications(team_member_id) where is_deleted = false;

create index if not exists idx_notifications_unread
    on procohere.notifications(team_member_id, is_read)
    where is_deleted = false and is_read = false;

drop trigger if exists tr_notifications_set_updated_at on procohere.notifications;
create trigger tr_notifications_set_updated_at
    before update on procohere.notifications
    for each row execute function public.set_updated_at();

-- ============================================================
-- 32. CALENDAR_INTEGRATIONS
-- ============================================================
create table if not exists procohere.calendar_integrations (
    id                  uuid primary key default gen_random_uuid(),
    organization_id     uuid not null references public.organizations(id),
    team_member_id      uuid not null references procohere.team_members(id),
    provider            text not null,
    external_account_id text,
    access_token        text,
    refresh_token       text,
    token_expires_at    timestamptz,
    sync_enabled        boolean not null default true,
    last_synced_at      timestamptz,
    is_deleted          boolean not null default false,
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now(),
    deleted_at          timestamptz,
    deleted_by          uuid references public.users(id)
);

create index if not exists idx_calendar_integrations_org
    on procohere.calendar_integrations(organization_id) where is_deleted = false;

create index if not exists idx_calendar_integrations_member
    on procohere.calendar_integrations(team_member_id) where is_deleted = false;

create unique index if not exists uq_calendar_integrations_member_provider
    on procohere.calendar_integrations(team_member_id, provider)
    where is_deleted = false;

drop trigger if exists tr_calendar_integrations_set_updated_at on procohere.calendar_integrations;
create trigger tr_calendar_integrations_set_updated_at
    before update on procohere.calendar_integrations
    for each row execute function public.set_updated_at();

-- ============================================================
-- 33. COMMENTS
-- ============================================================
create table if not exists procohere.comments (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    author_id         uuid not null references procohere.team_members(id),
    entity_type       text not null,
    entity_id         uuid not null,
    parent_comment_id uuid references procohere.comments(id),
    content           text not null,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_comments_org
    on procohere.comments(organization_id) where is_deleted = false;

create index if not exists idx_comments_entity
    on procohere.comments(entity_type, entity_id) where is_deleted = false;

create index if not exists idx_comments_author
    on procohere.comments(author_id) where is_deleted = false;

create index if not exists idx_comments_parent
    on procohere.comments(parent_comment_id) where is_deleted = false and parent_comment_id is not null;

drop trigger if exists tr_comments_set_updated_at on procohere.comments;
create trigger tr_comments_set_updated_at
    before update on procohere.comments
    for each row execute function public.set_updated_at();

-- ============================================================
-- 34. ACTIVITY_FEED
-- ============================================================
create table if not exists procohere.activity_feed (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    actor_id        uuid not null references procohere.team_members(id),
    action          text not null,
    entity_type     text not null,
    entity_id       uuid not null,
    entity_title    text,
    metadata        jsonb,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now()
);

create index if not exists idx_activity_feed_org
    on procohere.activity_feed(organization_id) where is_deleted = false;

create index if not exists idx_activity_feed_actor
    on procohere.activity_feed(actor_id) where is_deleted = false;

create index if not exists idx_activity_feed_entity
    on procohere.activity_feed(entity_type, entity_id) where is_deleted = false;

create index if not exists idx_activity_feed_created
    on procohere.activity_feed(created_at desc) where is_deleted = false;

-- ============================================================
-- 35. USER_SETTINGS
-- ============================================================
create table if not exists procohere.user_settings (
    id                       uuid primary key default gen_random_uuid(),
    organization_id          uuid not null references public.organizations(id),
    team_member_id           uuid not null references procohere.team_members(id),
    theme                    text default 'system',
    email_notifications      boolean not null default true,
    push_notifications       boolean not null default true,
    meeting_reminders        boolean not null default true,
    task_reminders           boolean not null default true,
    weekly_digest            boolean not null default true,
    default_meeting_duration int default 30,
    timezone                 text default 'UTC',
    locale                   text default 'en-US',
    settings_json            jsonb not null default '{}'::jsonb,
    is_deleted               boolean not null default false,
    created_at               timestamptz not null default now(),
    updated_at               timestamptz not null default now(),
    deleted_at               timestamptz,
    deleted_by               uuid references public.users(id)
);

create unique index if not exists uq_user_settings_member
    on procohere.user_settings(team_member_id)
    where is_deleted = false;

create index if not exists idx_user_settings_org
    on procohere.user_settings(organization_id) where is_deleted = false;

drop trigger if exists tr_user_settings_set_updated_at on procohere.user_settings;
create trigger tr_user_settings_set_updated_at
    before update on procohere.user_settings
    for each row execute function public.set_updated_at();

-- ============================================================
-- 36. COMPETENCIES
-- ============================================================
create table if not exists procohere.competencies (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    name            text not null,
    description     text,
    category        text,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_competencies_org
    on procohere.competencies(organization_id) where is_deleted = false;

create unique index if not exists uq_competencies_org_name
    on procohere.competencies(organization_id, lower(trim(name)))
    where is_deleted = false;

drop trigger if exists tr_competencies_set_updated_at on procohere.competencies;
create trigger tr_competencies_set_updated_at
    before update on procohere.competencies
    for each row execute function public.set_updated_at();

-- ============================================================
-- 37. TEAM_MEMBER_COMPETENCIES
-- ============================================================
create table if not exists procohere.team_member_competencies (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    team_member_id    uuid not null references procohere.team_members(id),
    competency_id     uuid not null references procohere.competencies(id),
    proficiency_level int check (proficiency_level >= 1 and proficiency_level <= 5),
    assessed_by       uuid references procohere.team_members(id),
    assessed_at       timestamptz,
    notes             text,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_tm_competencies_org
    on procohere.team_member_competencies(organization_id) where is_deleted = false;

create index if not exists idx_tm_competencies_member
    on procohere.team_member_competencies(team_member_id) where is_deleted = false;

create index if not exists idx_tm_competencies_competency
    on procohere.team_member_competencies(competency_id) where is_deleted = false;

create unique index if not exists uq_tm_competencies_member_comp
    on procohere.team_member_competencies(team_member_id, competency_id)
    where is_deleted = false;

drop trigger if exists tr_tm_competencies_set_updated_at on procohere.team_member_competencies;
create trigger tr_tm_competencies_set_updated_at
    before update on procohere.team_member_competencies
    for each row execute function public.set_updated_at();

-- ============================================================
-- 38. DEVELOPMENT_PLANS
-- ============================================================
create table if not exists procohere.development_plans (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    team_member_id  uuid not null references procohere.team_members(id),
    title           text not null,
    description     text,
    status          text not null default 'active',
    start_date      date,
    target_date     date,
    completed_at    timestamptz,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_dev_plans_org
    on procohere.development_plans(organization_id) where is_deleted = false;

create index if not exists idx_dev_plans_member
    on procohere.development_plans(team_member_id) where is_deleted = false;

drop trigger if exists tr_development_plans_set_updated_at on procohere.development_plans;
create trigger tr_development_plans_set_updated_at
    before update on procohere.development_plans
    for each row execute function public.set_updated_at();

-- ============================================================
-- 39. DEVELOPMENT_PLAN_ITEMS
-- ============================================================
create table if not exists procohere.development_plan_items (
    id                  uuid primary key default gen_random_uuid(),
    organization_id     uuid not null references public.organizations(id),
    development_plan_id uuid not null references procohere.development_plans(id),
    competency_id       uuid references procohere.competencies(id),
    title               text not null,
    description         text,
    item_type           text default 'action',
    status              text not null default 'not_started',
    due_date            date,
    completed_at        timestamptz,
    sort_order          int not null default 0,
    is_deleted          boolean not null default false,
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now(),
    deleted_at          timestamptz,
    deleted_by          uuid references public.users(id)
);

create index if not exists idx_dev_plan_items_org
    on procohere.development_plan_items(organization_id) where is_deleted = false;

create index if not exists idx_dev_plan_items_plan
    on procohere.development_plan_items(development_plan_id) where is_deleted = false;

drop trigger if exists tr_dev_plan_items_set_updated_at on procohere.development_plan_items;
create trigger tr_dev_plan_items_set_updated_at
    before update on procohere.development_plan_items
    for each row execute function public.set_updated_at();

-- ============================================================
-- 40. KUDOS
-- ============================================================
create table if not exists procohere.kudos (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    from_member_id  uuid not null references procohere.team_members(id),
    to_member_id    uuid not null references procohere.team_members(id),
    message         text not null,
    category        text,
    is_public       boolean not null default true,
    is_deleted      boolean not null default false,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    deleted_at      timestamptz,
    deleted_by      uuid references public.users(id)
);

create index if not exists idx_kudos_org
    on procohere.kudos(organization_id) where is_deleted = false;

create index if not exists idx_kudos_from
    on procohere.kudos(from_member_id) where is_deleted = false;

create index if not exists idx_kudos_to
    on procohere.kudos(to_member_id) where is_deleted = false;

create index if not exists idx_kudos_created
    on procohere.kudos(created_at desc) where is_deleted = false;

drop trigger if exists tr_kudos_set_updated_at on procohere.kudos;
create trigger tr_kudos_set_updated_at
    before update on procohere.kudos
    for each row execute function public.set_updated_at();

-- ============================================================
-- 41. REVIEW_CYCLES
-- ============================================================
create table if not exists procohere.review_cycles (
    id                uuid primary key default gen_random_uuid(),
    organization_id   uuid not null references public.organizations(id),
    name              text not null,
    description       text,
    cycle_type        text not null default 'annual',
    status            text not null default 'draft',
    start_date        date not null,
    end_date          date not null,
    review_start_date date,
    review_end_date   date,
    is_deleted        boolean not null default false,
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now(),
    deleted_at        timestamptz,
    deleted_by        uuid references public.users(id)
);

create index if not exists idx_review_cycles_org
    on procohere.review_cycles(organization_id) where is_deleted = false;

create index if not exists idx_review_cycles_status
    on procohere.review_cycles(status) where is_deleted = false;

drop trigger if exists tr_review_cycles_set_updated_at on procohere.review_cycles;
create trigger tr_review_cycles_set_updated_at
    before update on procohere.review_cycles
    for each row execute function public.set_updated_at();

-- ============================================================
-- 42. PERFORMANCE_REVIEWS
-- ============================================================
create table if not exists procohere.performance_reviews (
    id                   uuid primary key default gen_random_uuid(),
    organization_id      uuid not null references public.organizations(id),
    review_cycle_id      uuid not null references procohere.review_cycles(id),
    reviewee_id          uuid not null references procohere.team_members(id),
    reviewer_id          uuid not null references procohere.team_members(id),
    review_type          text not null default 'manager',
    status               text not null default 'pending',
    overall_rating       int check (overall_rating >= 1 and overall_rating <= 5),
    strengths            text,
    areas_for_improvement text,
    goals_for_next_period text,
    additional_comments  text,
    submitted_at         timestamptz,
    acknowledged_at      timestamptz,
    is_deleted           boolean not null default false,
    created_at           timestamptz not null default now(),
    updated_at           timestamptz not null default now(),
    deleted_at           timestamptz,
    deleted_by           uuid references public.users(id)
);

create index if not exists idx_perf_reviews_org
    on procohere.performance_reviews(organization_id) where is_deleted = false;

create index if not exists idx_perf_reviews_cycle
    on procohere.performance_reviews(review_cycle_id) where is_deleted = false;

create index if not exists idx_perf_reviews_reviewee
    on procohere.performance_reviews(reviewee_id) where is_deleted = false;

create index if not exists idx_perf_reviews_reviewer
    on procohere.performance_reviews(reviewer_id) where is_deleted = false;

create unique index if not exists uq_perf_reviews_cycle_reviewee_reviewer_type
    on procohere.performance_reviews(review_cycle_id, reviewee_id, reviewer_id, review_type)
    where is_deleted = false;

drop trigger if exists tr_performance_reviews_set_updated_at on procohere.performance_reviews;
create trigger tr_performance_reviews_set_updated_at
    before update on procohere.performance_reviews
    for each row execute function public.set_updated_at();

-- ============================================================
-- 43. AUDIT_LOG
-- ============================================================
create table if not exists procohere.audit_log (
    id              uuid primary key default gen_random_uuid(),
    organization_id uuid not null references public.organizations(id),
    actor_id        uuid references public.users(id),
    team_member_id  uuid references procohere.team_members(id),
    action          text not null,
    entity_type     text not null,
    entity_id       uuid,
    old_values      jsonb,
    new_values      jsonb,
    ip_address      inet,
    user_agent      text,
    created_at      timestamptz not null default now()
);

create index if not exists idx_audit_log_org
    on procohere.audit_log(organization_id);

create index if not exists idx_audit_log_actor
    on procohere.audit_log(actor_id);

create index if not exists idx_audit_log_entity
    on procohere.audit_log(entity_type, entity_id);

create index if not exists idx_audit_log_created
    on procohere.audit_log(created_at);

-- ============================================================
-- ENABLE ROW LEVEL SECURITY
-- ============================================================

alter table if exists procohere.roles enable row level security;
alter table if exists procohere.team_members enable row level security;
alter table if exists procohere.teams enable row level security;
alter table if exists procohere.org_settings enable row level security;
alter table if exists procohere.meetings enable row level security;
alter table if exists procohere.meeting_attendees enable row level security;
alter table if exists procohere.meeting_agenda_items enable row level security;
alter table if exists procohere.meeting_notes enable row level security;
alter table if exists procohere.meeting_summaries enable row level security;
alter table if exists procohere.meeting_templates enable row level security;
alter table if exists procohere.goal_categories enable row level security;
alter table if exists procohere.goals enable row level security;
alter table if exists procohere.targets enable row level security;
alter table if exists procohere.goal_templates enable row level security;
alter table if exists procohere.tasks enable row level security;
alter table if exists procohere.feedback enable row level security;
alter table if exists procohere.feedback_templates enable row level security;
alter table if exists procohere.notes enable row level security;
alter table if exists procohere.metrics enable row level security;
alter table if exists procohere.metric_values enable row level security;
alter table if exists procohere.surveys enable row level security;
alter table if exists procohere.survey_questions enable row level security;
alter table if exists procohere.survey_responses enable row level security;
alter table if exists procohere.survey_answers enable row level security;
alter table if exists procohere.ai_conversations enable row level security;
alter table if exists procohere.ai_messages enable row level security;
alter table if exists procohere.ai_insights enable row level security;
alter table if exists procohere.attachments enable row level security;
alter table if exists procohere.tags enable row level security;
alter table if exists procohere.entity_tags enable row level security;
alter table if exists procohere.notifications enable row level security;
alter table if exists procohere.calendar_integrations enable row level security;
alter table if exists procohere.comments enable row level security;
alter table if exists procohere.activity_feed enable row level security;
alter table if exists procohere.user_settings enable row level security;
alter table if exists procohere.competencies enable row level security;
alter table if exists procohere.team_member_competencies enable row level security;
alter table if exists procohere.development_plans enable row level security;
alter table if exists procohere.development_plan_items enable row level security;
alter table if exists procohere.kudos enable row level security;
alter table if exists procohere.review_cycles enable row level security;
alter table if exists procohere.performance_reviews enable row level security;
alter table if exists procohere.audit_log enable row level security;

-- ============================================================
-- RLS HELPERS
-- ============================================================

create or replace function procohere.get_user_org_ids()
returns setof uuid
language sql
security definer
set search_path = procohere, public
stable
as $$
    select organization_id
    from procohere.team_members
    where linked_user_id = auth.uid()
      and is_deleted = false
$$;

-- ============================================================
-- HIERARCHY FUNCTION: get_team_descendants
-- The canonical primitive for manager-of-managers visibility
-- ============================================================

drop function if exists procohere.get_team_descendants(uuid, uuid, boolean);

create or replace function procohere.get_team_descendants(
    p_organization_id uuid,
    p_manager_id uuid,
    p_include_self boolean default false
)
returns table (
    team_member_id uuid,
    depth int
)
language sql
stable
security definer
set search_path = procohere, public
as $$
    with recursive descendants as (
        select
            tm.id as team_member_id,
            1 as depth
        from procohere.team_members tm
        where tm.organization_id = p_organization_id
          and tm.manager_team_member_id = p_manager_id
          and tm.is_deleted = false
          and tm.is_active = true

        union all

        select
            tm.id,
            d.depth + 1
        from procohere.team_members tm
        join descendants d
          on tm.manager_team_member_id = d.team_member_id
        where tm.organization_id = p_organization_id
          and tm.is_deleted = false
          and tm.is_active = true
          and d.depth < 50
    )
    select team_member_id, depth
    from descendants

    union all
    select p_manager_id, 0
    where p_include_self = true
      and exists (
          select 1
          from procohere.team_members
          where id = p_manager_id
            and organization_id = p_organization_id
            and is_deleted = false
            and is_active = true
      );
$$;

grant execute on function procohere.get_team_descendants(uuid, uuid, boolean) to authenticated;

-- ============================================================
-- VISIBILITY FUNCTION: get_visible_team_member_ids
-- Wrapper that returns visible team members based on role
-- ============================================================

drop function if exists procohere.get_visible_team_member_ids(uuid, uuid);

create or replace function procohere.get_visible_team_member_ids(
    p_organization_id uuid,
    p_team_member_id uuid
)
returns table (
    team_member_id uuid,
    depth int,
    relation text
)
language plpgsql
stable
security definer
set search_path = procohere, public
as $$
declare
    v_manager_id uuid;
    v_has_descendants boolean;
begin
    -- Get caller's manager
    select manager_team_member_id into v_manager_id
    from procohere.team_members
    where id = p_team_member_id 
      and organization_id = p_organization_id
      and is_deleted = false;
    
    -- Check if caller has any descendants (is a manager)
    select exists(
        select 1 
        from procohere.team_members 
        where manager_team_member_id = p_team_member_id
          and organization_id = p_organization_id
          and is_deleted = false
          and is_active = true
    ) into v_has_descendants;
    
    -- Always return self
    return query 
    select p_team_member_id, 0, 'self'::text;
    
    -- Return manager (if exists)
    if v_manager_id is not null then
        return query 
        select v_manager_id, -1, 'manager'::text;
    end if;
    
    -- Return peers (same manager, excluding self)
    if v_manager_id is not null then
        return query
        select tm.id, 0, 'peer'::text
        from procohere.team_members tm
        where tm.manager_team_member_id = v_manager_id
          and tm.id != p_team_member_id
          and tm.organization_id = p_organization_id
          and tm.is_active = true
          and tm.is_deleted = false;
    end if;
    
    -- Return descendants (if caller is a manager)
    if v_has_descendants then
        return query
        select 
            d.team_member_id,
            d.depth,
            case when d.depth = 1 then 'direct'::text else 'descendant'::text end
        from procohere.get_team_descendants(p_organization_id, p_team_member_id, false) d;
    end if;
end;
$$;

grant execute on function procohere.get_visible_team_member_ids(uuid, uuid) to authenticated;

-- ============================================================
-- BASELINE RLS POLICIES (Org isolation)
-- ============================================================

-- Drop policies to allow re-run

do $$
begin
    perform 1;
    -- roles
    execute 'drop policy if exists "org_isolation" on procohere.roles';
    execute 'drop policy if exists "org_isolation" on procohere.team_members';
    execute 'drop policy if exists "org_isolation" on procohere.teams';
    execute 'drop policy if exists "org_isolation" on procohere.org_settings';
    execute 'drop policy if exists "org_isolation" on procohere.meetings';
    execute 'drop policy if exists "org_isolation" on procohere.meeting_attendees';
    execute 'drop policy if exists "org_isolation" on procohere.meeting_agenda_items';
    execute 'drop policy if exists "org_isolation" on procohere.meeting_notes';
    execute 'drop policy if exists "org_isolation" on procohere.meeting_summaries';
    execute 'drop policy if exists "org_isolation" on procohere.meeting_templates';
    execute 'drop policy if exists "org_isolation" on procohere.goal_categories';
    execute 'drop policy if exists "org_isolation" on procohere.goals';
    execute 'drop policy if exists "org_isolation" on procohere.targets';
    execute 'drop policy if exists "org_isolation" on procohere.goal_templates';
    execute 'drop policy if exists "org_isolation" on procohere.tasks';
    execute 'drop policy if exists "org_isolation" on procohere.feedback';
    execute 'drop policy if exists "org_isolation" on procohere.feedback_templates';
    execute 'drop policy if exists "org_isolation" on procohere.notes';
    execute 'drop policy if exists "org_isolation" on procohere.metrics';
    execute 'drop policy if exists "org_isolation" on procohere.metric_values';
    execute 'drop policy if exists "org_isolation" on procohere.surveys';
    execute 'drop policy if exists "org_isolation" on procohere.survey_questions';
    execute 'drop policy if exists "org_isolation" on procohere.survey_responses';
    execute 'drop policy if exists "org_isolation" on procohere.survey_answers';
    execute 'drop policy if exists "org_isolation" on procohere.ai_conversations';
    execute 'drop policy if exists "org_isolation" on procohere.ai_messages';
    execute 'drop policy if exists "org_isolation" on procohere.ai_insights';
    execute 'drop policy if exists "org_isolation" on procohere.attachments';
    execute 'drop policy if exists "org_isolation" on procohere.tags';
    execute 'drop policy if exists "org_isolation" on procohere.entity_tags';
    execute 'drop policy if exists "org_isolation" on procohere.notifications';
    execute 'drop policy if exists "owner_only" on procohere.calendar_integrations';
    execute 'drop policy if exists "org_isolation" on procohere.comments';
    execute 'drop policy if exists "org_isolation" on procohere.activity_feed';
    execute 'drop policy if exists "org_isolation" on procohere.user_settings';
    execute 'drop policy if exists "org_isolation" on procohere.competencies';
    execute 'drop policy if exists "org_isolation" on procohere.team_member_competencies';
    execute 'drop policy if exists "org_isolation" on procohere.development_plans';
    execute 'drop policy if exists "org_isolation" on procohere.development_plan_items';
    execute 'drop policy if exists "org_isolation" on procohere.kudos';
    execute 'drop policy if exists "org_isolation" on procohere.review_cycles';
    execute 'drop policy if exists "org_isolation" on procohere.performance_reviews';
    execute 'drop policy if exists "org_isolation" on procohere.audit_log';
end $$;

-- Org isolation policies
create policy org_isolation on procohere.roles
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.team_members
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.teams
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.org_settings
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meetings
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meeting_attendees
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meeting_agenda_items
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meeting_notes
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meeting_summaries
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.meeting_templates
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.goal_categories
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.goals
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.targets
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.goal_templates
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.tasks
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.feedback
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.feedback_templates
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.notes
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.metrics
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.metric_values
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.surveys
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.survey_questions
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.survey_responses
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.survey_answers
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.ai_conversations
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.ai_messages
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.ai_insights
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.attachments
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.tags
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.entity_tags
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.notifications
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy owner_only on procohere.calendar_integrations
    for all
    using (
        team_member_id in (
            select id
            from procohere.team_members
            where linked_user_id = auth.uid()
              and is_deleted = false
        )
    )
    with check (
        team_member_id in (
            select id
            from procohere.team_members
            where linked_user_id = auth.uid()
              and is_deleted = false
        )
    );

create policy org_isolation on procohere.comments
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.activity_feed
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.user_settings
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.competencies
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.team_member_competencies
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.development_plans
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.development_plan_items
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.kudos
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.review_cycles
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.performance_reviews
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

create policy org_isolation on procohere.audit_log
    for all
    using (organization_id in (select procohere.get_user_org_ids()))
    with check (organization_id in (select procohere.get_user_org_ids()));

-- ============================================================
-- GRANTS
-- ============================================================

grant usage on schema procohere to authenticated;
grant select on all tables in schema procohere to authenticated;
grant execute on function procohere.get_user_org_ids() to authenticated;

