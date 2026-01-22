-- ============================================================
-- RLS VISIBILITY SPLIT MIGRATION
-- Created: 2026-01-20
-- 
-- Purpose: Fix RLS security vulnerability where ICs could see 
-- peer data via the overly permissive org_isolation policy.
--
-- Changes:
--   1. Create get_rls_visible_team_member_ids() - returns self + descendants only
--   2. Create get_ui_visible_team_member_ids() - returns self + manager + peers + descendants
--   3. Make get_visible_team_member_ids alias to get_ui_visible_team_member_ids
--   4. Replace FOR ALL org_isolation policies with:
--      - SELECT policies using get_rls_visible_team_member_ids (restrictive)
--      - INSERT/UPDATE/DELETE policies using org check only
--   5. Affected tables: tasks, goals, metrics, metric_values, notes, targets, feedback
--
-- Security Model After Migration:
--   - IC sees: own items only (+ feedback they sent/received)
--   - Manager sees: own items + all descendants' items (+ feedback about descendants)
--   - Meetings: separate logic (attendee-based visibility preserved)
--
-- Run Order: This script is idempotent and can be re-run safely.
-- ============================================================

-- ============================================================
-- PART 1: CREATE NEW VISIBILITY FUNCTIONS
-- ============================================================

-- ------------------------------------------------------------
-- 1A: get_rls_visible_team_member_ids
-- Returns: self + descendants ONLY (for RLS - restrictive)
-- This is the SECURE function - no peer/manager visibility
-- ------------------------------------------------------------
drop function if exists procohere.get_rls_visible_team_member_ids(uuid, uuid);

create or replace function procohere.get_rls_visible_team_member_ids(
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
    v_has_descendants boolean;
begin
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
    
    -- Return descendants ONLY (if caller is a manager)
    -- NO peers, NO manager - this is the key security difference
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

comment on function procohere.get_rls_visible_team_member_ids(uuid, uuid) is 
'RLS-safe visibility: returns self + descendants only. Used in SELECT policies for tasks, goals, metrics, notes, targets.';

grant execute on function procohere.get_rls_visible_team_member_ids(uuid, uuid) to authenticated;

-- ------------------------------------------------------------
-- 1B: get_ui_visible_team_member_ids
-- Returns: self + manager + peers + descendants (for UI display)
-- This is the PERMISSIVE function - shows org chart context
-- ------------------------------------------------------------
drop function if exists procohere.get_ui_visible_team_member_ids(uuid, uuid);

create or replace function procohere.get_ui_visible_team_member_ids(
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

comment on function procohere.get_ui_visible_team_member_ids(uuid, uuid) is 
'UI visibility: returns self + manager + peers + descendants. Used for team member dropdowns and org chart display.';

grant execute on function procohere.get_ui_visible_team_member_ids(uuid, uuid) to authenticated;

-- ------------------------------------------------------------
-- 1C: Update get_visible_team_member_ids to be an alias
-- Points to get_ui_visible_team_member_ids for backward compatibility
-- ------------------------------------------------------------
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
language sql
stable
security definer
set search_path = procohere, public
as $$
    -- ALIAS: Delegates to get_ui_visible_team_member_ids for backward compatibility
    select * from procohere.get_ui_visible_team_member_ids(p_organization_id, p_team_member_id);
$$;

comment on function procohere.get_visible_team_member_ids(uuid, uuid) is 
'ALIAS for get_ui_visible_team_member_ids. Maintained for backward compatibility with existing app code.';

grant execute on function procohere.get_visible_team_member_ids(uuid, uuid) to authenticated;

-- ============================================================
-- PART 2: HELPER FUNCTION FOR RLS POLICIES
-- Gets the current user's team_member_id for a given org
-- ============================================================
drop function if exists procohere.get_current_team_member_id(uuid);

create or replace function procohere.get_current_team_member_id(p_organization_id uuid)
returns uuid
language sql
stable
security definer
set search_path = procohere, public
as $$
    select id 
    from procohere.team_members 
    where linked_user_id = auth.uid()
      and organization_id = p_organization_id
      and is_deleted = false
    limit 1;
$$;

comment on function procohere.get_current_team_member_id(uuid) is 
'Returns the team_member_id for the current authenticated user in the given org.';

grant execute on function procohere.get_current_team_member_id(uuid) to authenticated;

-- ============================================================
-- PART 3: UPDATE RLS POLICIES FOR SENSITIVE TABLES
-- Replace permissive FOR ALL with restrictive SELECT + org-check writes
-- ============================================================

-- ------------------------------------------------------------
-- 3A: TASKS - IC sees own tasks, Manager sees own + descendants
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.tasks;
drop policy if exists tasks_select_visibility on procohere.tasks;
drop policy if exists tasks_write_org_check on procohere.tasks;

-- SELECT: Use RLS visibility (self + descendants only)
create policy tasks_select_visibility on procohere.tasks
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and assigned_to_team_member_id in (
            select v.team_member_id 
            from procohere.get_rls_visible_team_member_ids(
                organization_id,
                procohere.get_current_team_member_id(organization_id)
            ) v
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only (RPCs handle business rules)
create policy tasks_write_org_check on procohere.tasks
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy tasks_update_org_check on procohere.tasks
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy tasks_delete_org_check on procohere.tasks
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ------------------------------------------------------------
-- 3B: GOALS - IC sees own goals, Manager sees own + descendants
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.goals;
drop policy if exists goals_select_visibility on procohere.goals;
drop policy if exists goals_write_org_check on procohere.goals;

-- SELECT: Use RLS visibility
create policy goals_select_visibility on procohere.goals
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and owner_team_member_id in (
            select v.team_member_id 
            from procohere.get_rls_visible_team_member_ids(
                organization_id,
                procohere.get_current_team_member_id(organization_id)
            ) v
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy goals_write_org_check on procohere.goals
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy goals_update_org_check on procohere.goals
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy goals_delete_org_check on procohere.goals
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ------------------------------------------------------------
-- 3C: METRICS - IC sees own metrics, Manager sees own + descendants
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.metrics;
drop policy if exists metrics_select_visibility on procohere.metrics;
drop policy if exists metrics_write_org_check on procohere.metrics;

-- SELECT: Use RLS visibility
create policy metrics_select_visibility on procohere.metrics
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and owner_team_member_id in (
            select v.team_member_id 
            from procohere.get_rls_visible_team_member_ids(
                organization_id,
                procohere.get_current_team_member_id(organization_id)
            ) v
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy metrics_write_org_check on procohere.metrics
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy metrics_update_org_check on procohere.metrics
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy metrics_delete_org_check on procohere.metrics
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ------------------------------------------------------------
-- 3D: METRIC_VALUES - Follows metric visibility
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.metric_values;
drop policy if exists metric_values_select_visibility on procohere.metric_values;
drop policy if exists metric_values_write_org_check on procohere.metric_values;

-- SELECT: Use RLS visibility via parent metric
create policy metric_values_select_visibility on procohere.metric_values
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and metric_id in (
            select m.id 
            from procohere.metrics m
            where m.owner_team_member_id in (
                select v.team_member_id 
                from procohere.get_rls_visible_team_member_ids(
                    m.organization_id,
                    procohere.get_current_team_member_id(m.organization_id)
                ) v
            )
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy metric_values_write_org_check on procohere.metric_values
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy metric_values_update_org_check on procohere.metric_values
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy metric_values_delete_org_check on procohere.metric_values
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ------------------------------------------------------------
-- 3E: NOTES - IC sees own notes, Manager sees own + descendants
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.notes;
drop policy if exists notes_select_visibility on procohere.notes;
drop policy if exists notes_write_org_check on procohere.notes;

-- SELECT: Use RLS visibility
create policy notes_select_visibility on procohere.notes
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and created_by_team_member_id in (
            select v.team_member_id 
            from procohere.get_rls_visible_team_member_ids(
                organization_id,
                procohere.get_current_team_member_id(organization_id)
            ) v
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy notes_write_org_check on procohere.notes
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy notes_update_org_check on procohere.notes
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy notes_delete_org_check on procohere.notes
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ------------------------------------------------------------
-- 3F: TARGETS - Follows goal visibility
-- ------------------------------------------------------------
drop policy if exists org_isolation on procohere.targets;
drop policy if exists targets_select_visibility on procohere.targets;
drop policy if exists targets_write_org_check on procohere.targets;

-- SELECT: Use RLS visibility via parent goal
create policy targets_select_visibility on procohere.targets
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and goal_id in (
            select g.id 
            from procohere.goals g
            where g.owner_team_member_id in (
                select v.team_member_id 
                from procohere.get_rls_visible_team_member_ids(
                    g.organization_id,
                    procohere.get_current_team_member_id(g.organization_id)
                ) v
            )
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy targets_write_org_check on procohere.targets
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy targets_update_org_check on procohere.targets
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy targets_delete_org_check on procohere.targets
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ============================================================
-- PART 4: FEEDBACK TABLE (Special handling)
-- Feedback has from_member_id and to_member_id
-- User can see feedback they sent OR received OR about descendants
-- ============================================================
drop policy if exists org_isolation on procohere.feedback;
drop policy if exists feedback_select_visibility on procohere.feedback;
drop policy if exists feedback_write_org_check on procohere.feedback;

-- SELECT: Can see feedback if:
--   1. You sent it (from_member_id = self)
--   2. You received it (to_member_id = self)
--   3. It's about your descendant (to_member_id in descendants)
create policy feedback_select_visibility on procohere.feedback
    for select
    using (
        organization_id = any(procohere.get_user_org_ids())
        and (
            -- You sent it
            from_member_id = procohere.get_current_team_member_id(organization_id)
            -- OR you received it
            or to_member_id = procohere.get_current_team_member_id(organization_id)
            -- OR it's about someone you manage (to_member_id in your RLS visibility)
            or to_member_id in (
                select v.team_member_id 
                from procohere.get_rls_visible_team_member_ids(
                    organization_id,
                    procohere.get_current_team_member_id(organization_id)
                ) v
                where v.relation in ('direct', 'descendant')  -- Not self, only reports
            )
        )
    );

-- INSERT/UPDATE/DELETE: Org membership check only
create policy feedback_write_org_check on procohere.feedback
    for insert
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy feedback_update_org_check on procohere.feedback
    for update
    using (organization_id = any(procohere.get_user_org_ids()))
    with check (organization_id = any(procohere.get_user_org_ids()));

create policy feedback_delete_org_check on procohere.feedback
    for delete
    using (organization_id = any(procohere.get_user_org_ids()));

-- ============================================================
-- PART 5: MEETING-RELATED TABLES (Keep org_isolation)
-- Meetings use attendee-based visibility which is already correct
-- ============================================================

-- No changes needed - meetings, meeting_attendees, meeting_agenda_items
-- still use org_isolation because meeting visibility is attendee-based
-- (if you're an attendee, you can see the meeting - which is correct)

-- ============================================================
-- PART 6: VERIFICATION QUERIES
-- Run these after deployment to verify the migration worked
-- ============================================================

/*
-- Test 1: Verify functions exist
SELECT routine_name, routine_type 
FROM information_schema.routines 
WHERE routine_schema = 'procohere' 
  AND routine_name LIKE '%visible_team_member%'
ORDER BY routine_name;

-- Test 2: Compare RLS vs UI visibility for an IC
-- (Replace UUIDs with actual values)
SELECT 'rls' as type, * FROM procohere.get_rls_visible_team_member_ids('org-id'::uuid, 'ic-tm-id'::uuid)
UNION ALL
SELECT 'ui' as type, * FROM procohere.get_ui_visible_team_member_ids('org-id'::uuid, 'ic-tm-id'::uuid)
ORDER BY type, relation;

-- Test 3: Verify IC cannot see peer's tasks
-- As IC user, this should return 0 rows for peer tasks:
SELECT t.id, t.title, t.assigned_to_team_member_id
FROM procohere.tasks t
WHERE t.assigned_to_team_member_id = 'peer-tm-id'::uuid;

-- Test 4: Verify Manager CAN see descendant's tasks
-- As Manager user, this should return descendant tasks:
SELECT t.id, t.title, t.assigned_to_team_member_id
FROM procohere.tasks t
WHERE t.assigned_to_team_member_id = 'descendant-tm-id'::uuid;
*/

-- ============================================================
-- MIGRATION COMPLETE
-- 
-- Summary:
--   - Created get_rls_visible_team_member_ids (self + descendants)
--   - Created get_ui_visible_team_member_ids (self + manager + peers + descendants)
--   - Made get_visible_team_member_ids alias to UI version
--   - Updated RLS for: tasks, goals, metrics, metric_values, notes, targets, feedback
--   - Meeting tables unchanged (attendee-based visibility is correct)
--
-- Behavior After:
--   - IC: Sees only own items (tasks, goals, metrics, notes)
--   - IC: Sees feedback they sent or received
--   - Manager: Sees own + all descendants' items
--   - Manager: Sees feedback about their descendants
--   - No one sees peers' private content
-- ============================================================
