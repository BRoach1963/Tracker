-- ============================================================
-- PROCOHERE SCHEMA - PART 2: ROW-LEVEL SECURITY & POLICIES
-- Version: 1.0
-- Date: 2026-01-17
--
-- Prerequisites:
--   - 01_PROCOHERE_TABLES.sql must be run FIRST
--   - Users must be authenticated via Supabase Auth
--
-- Security Model:
--   - SELECT-only grants on tables (writes via RPCs)
--   - RLS enabled on all tables
--   - Baseline org_isolation policy on all tables
--   - Owner-only policy on calendar_integrations
--   - All policies have WITH CHECK for write operations
-- ============================================================

-- ============================================================
-- ENABLE ROW-LEVEL SECURITY ON ALL TABLES
-- ============================================================

-- Tables 1-2: COMMENTED OUT (already exist with data)
-- alter table procohere.roles enable row level security;
-- alter table procohere.team_members enable row level security;

-- Tables 3-43:
alter table procohere.teams enable row level security;
alter table procohere.org_settings enable row level security;
alter table procohere.meetings enable row level security;
alter table procohere.meeting_attendees enable row level security;
alter table procohere.meeting_agenda_items enable row level security;
alter table procohere.meeting_notes enable row level security;
alter table procohere.meeting_summaries enable row level security;
alter table procohere.meeting_templates enable row level security;
alter table procohere.goal_categories enable row level security;
alter table procohere.goals enable row level security;
alter table procohere.targets enable row level security;
alter table procohere.goal_templates enable row level security;
alter table procohere.tasks enable row level security;
alter table procohere.feedback enable row level security;
alter table procohere.feedback_templates enable row level security;
alter table procohere.notes enable row level security;
alter table procohere.metrics enable row level security;
alter table procohere.metric_values enable row level security;
alter table procohere.surveys enable row level security;
alter table procohere.survey_questions enable row level security;
alter table procohere.survey_responses enable row level security;
alter table procohere.survey_answers enable row level security;
alter table procohere.ai_conversations enable row level security;
alter table procohere.ai_messages enable row level security;
alter table procohere.ai_insights enable row level security;
alter table procohere.attachments enable row level security;
alter table procohere.tags enable row level security;
alter table procohere.entity_tags enable row level security;
alter table procohere.notifications enable row level security;
alter table procohere.calendar_integrations enable row level security;
alter table procohere.comments enable row level security;
alter table procohere.activity_feed enable row level security;
alter table procohere.user_settings enable row level security;
alter table procohere.competencies enable row level security;
alter table procohere.team_member_competencies enable row level security;
alter table procohere.development_plans enable row level security;
alter table procohere.development_plan_items enable row level security;
alter table procohere.kudos enable row level security;
alter table procohere.review_cycles enable row level security;
alter table procohere.performance_reviews enable row level security;
alter table procohere.audit_log enable row level security;

-- ============================================================
-- RLS HELPER FUNCTION
-- Returns array of organization IDs the current user belongs to
-- SECURITY DEFINER with explicit search_path for safety
-- ============================================================
create or replace function procohere.get_user_org_ids()
returns uuid[]
language sql
stable
security definer
set search_path = procohere, public
as $$
    select array_agg(distinct o.id)
    from public.organizations o
    join public.organization_members om on om.organization_id = o.id
    where om.user_id = auth.uid()
      and om.is_deleted = false
      and o.is_deleted = false;
$$;

-- Grant execute on the helper function
grant execute on function procohere.get_user_org_ids() to authenticated;

-- ============================================================
-- DROP EXISTING POLICIES (Idempotent re-run)
-- ============================================================
do $$
declare
    tbl text;
    tbls text[] := array[
        'roles', 'team_members', 'teams', 'org_settings', 'meetings',
        'meeting_attendees', 'meeting_agenda_items', 'meeting_notes',
        'meeting_summaries', 'meeting_templates', 'goal_categories',
        'goals', 'targets', 'goal_templates', 'tasks', 'feedback',
        'feedback_templates', 'notes', 'metrics', 'metric_values',
        'surveys', 'survey_questions', 'survey_responses', 'survey_answers',
        'ai_conversations', 'ai_messages', 'ai_insights', 'attachments',
        'tags', 'entity_tags', 'notifications', 'calendar_integrations',
        'comments', 'activity_feed', 'user_settings', 'competencies',
        'team_member_competencies', 'development_plans', 'development_plan_items',
        'kudos', 'review_cycles', 'performance_reviews', 'audit_log'
    ];
begin
    foreach tbl in array tbls
    loop
        execute format('drop policy if exists org_isolation on procohere.%I', tbl);
        execute format('drop policy if exists owner_only on procohere.%I', tbl);
    end loop;
end;
$$;

-- ============================================================
-- BASELINE ORG_ISOLATION POLICIES
-- All have WITH CHECK for write operations
-- ============================================================

-- Tables 1-2: COMMENTED OUT (already exist with data)
-- create policy org_isolation on procohere.roles
--     for all
--     using (organization_id = any(procohere.get_user_org_ids()))
--     with check (organization_id = any(procohere.get_user_org_ids()));

-- create policy org_isolation on procohere.team_members
--     for all
--     using (organization_id = any(procohere.get_user_org_ids()))
--     with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 3: teams
create policy org_isolation on procohere.teams
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 4: org_settings
create policy org_isolation on procohere.org_settings
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 5: meetings
create policy org_isolation on procohere.meetings
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 6: meeting_attendees
create policy org_isolation on procohere.meeting_attendees
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 7: meeting_agenda_items
create policy org_isolation on procohere.meeting_agenda_items
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 8: meeting_notes
create policy org_isolation on procohere.meeting_notes
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 9: meeting_summaries
create policy org_isolation on procohere.meeting_summaries
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 10: meeting_templates
create policy org_isolation on procohere.meeting_templates
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 11: goal_categories
create policy org_isolation on procohere.goal_categories
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 12: goals
create policy org_isolation on procohere.goals
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 13: targets
create policy org_isolation on procohere.targets
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 14: goal_templates
create policy org_isolation on procohere.goal_templates
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 15: tasks
create policy org_isolation on procohere.tasks
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 16: feedback
create policy org_isolation on procohere.feedback
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 17: feedback_templates
create policy org_isolation on procohere.feedback_templates
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 18: notes
create policy org_isolation on procohere.notes
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 19: metrics
create policy org_isolation on procohere.metrics
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 20: metric_values
create policy org_isolation on procohere.metric_values
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 21: surveys
create policy org_isolation on procohere.surveys
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 22: survey_questions
create policy org_isolation on procohere.survey_questions
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 23: survey_responses
create policy org_isolation on procohere.survey_responses
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 24: survey_answers
create policy org_isolation on procohere.survey_answers
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 25: ai_conversations
create policy org_isolation on procohere.ai_conversations
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 26: ai_messages
create policy org_isolation on procohere.ai_messages
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 27: ai_insights
create policy org_isolation on procohere.ai_insights
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 28: attachments
create policy org_isolation on procohere.attachments
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 29: tags
create policy org_isolation on procohere.tags
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 30: entity_tags
create policy org_isolation on procohere.entity_tags
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 31: notifications
create policy org_isolation on procohere.notifications
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 32: calendar_integrations (org_isolation removed - owner_only below)

-- Table 33: comments
create policy org_isolation on procohere.comments
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 34: activity_feed
create policy org_isolation on procohere.activity_feed
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 35: user_settings
create policy org_isolation on procohere.user_settings
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 36: competencies
create policy org_isolation on procohere.competencies
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 37: team_member_competencies
create policy org_isolation on procohere.team_member_competencies
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 38: development_plans
create policy org_isolation on procohere.development_plans
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 39: development_plan_items
create policy org_isolation on procohere.development_plan_items
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 40: kudos
create policy org_isolation on procohere.kudos
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 41: review_cycles
create policy org_isolation on procohere.review_cycles
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 42: performance_reviews
create policy org_isolation on procohere.performance_reviews
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- Table 43: audit_log
create policy org_isolation on procohere.audit_log
    for all
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

-- ============================================================
-- SPECIAL POLICY: calendar_integrations (owner-only)
-- Contains OAuth tokens - must restrict to token owner only
-- ============================================================
create policy owner_only on procohere.calendar_integrations
    for all
    using (
        team_member_id in (
            select id from procohere.team_members
            where linked_user_id = auth.uid()
              and is_deleted = false
        )
    )
    with check (
        team_member_id in (
            select id from procohere.team_members
            where linked_user_id = auth.uid()
              and is_deleted = false
        )
    );

-- ============================================================
-- GRANTS (SELECT-only - all writes go through RPCs)
-- ============================================================

-- Tables 1-2: COMMENTED OUT (already exist)
-- grant select on procohere.roles to authenticated;
-- grant select on procohere.team_members to authenticated;

-- Tables 3-43:
grant select on procohere.teams to authenticated;
grant select on procohere.org_settings to authenticated;
grant select on procohere.meetings to authenticated;
grant select on procohere.meeting_attendees to authenticated;
grant select on procohere.meeting_agenda_items to authenticated;
grant select on procohere.meeting_notes to authenticated;
grant select on procohere.meeting_summaries to authenticated;
grant select on procohere.meeting_templates to authenticated;
grant select on procohere.goal_categories to authenticated;
grant select on procohere.goals to authenticated;
grant select on procohere.targets to authenticated;
grant select on procohere.goal_templates to authenticated;
grant select on procohere.tasks to authenticated;
grant select on procohere.feedback to authenticated;
grant select on procohere.feedback_templates to authenticated;
grant select on procohere.notes to authenticated;
grant select on procohere.metrics to authenticated;
grant select on procohere.metric_values to authenticated;
grant select on procohere.surveys to authenticated;
grant select on procohere.survey_questions to authenticated;
grant select on procohere.survey_responses to authenticated;
grant select on procohere.survey_answers to authenticated;
grant select on procohere.ai_conversations to authenticated;
grant select on procohere.ai_messages to authenticated;
grant select on procohere.ai_insights to authenticated;
grant select on procohere.attachments to authenticated;
grant select on procohere.tags to authenticated;
grant select on procohere.entity_tags to authenticated;
grant select on procohere.notifications to authenticated;
grant select on procohere.calendar_integrations to authenticated;
grant select on procohere.comments to authenticated;
grant select on procohere.activity_feed to authenticated;
grant select on procohere.user_settings to authenticated;
grant select on procohere.competencies to authenticated;
grant select on procohere.team_member_competencies to authenticated;
grant select on procohere.development_plans to authenticated;
grant select on procohere.development_plan_items to authenticated;
grant select on procohere.kudos to authenticated;
grant select on procohere.review_cycles to authenticated;
grant select on procohere.performance_reviews to authenticated;
grant select on procohere.audit_log to authenticated;

-- ============================================================
-- END OF PART 2 - Schema deployment complete
-- 
-- Next Steps:
--   1. Create RPCs for INSERT/UPDATE/DELETE operations
--   2. Seed initial data (roles, org_settings, etc.)
-- ============================================================
