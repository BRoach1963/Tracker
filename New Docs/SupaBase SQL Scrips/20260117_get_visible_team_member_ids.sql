-- ============================================================
-- VISIBILITY FUNCTION: get_visible_team_member_ids
-- Wrapper that returns visible team members based on role
-- Created: 2026-01-17
-- ============================================================
-- 
-- This function encapsulates visibility policy:
--   - Admin: sees entire org
--   - Manager: sees self + all descendants  
--   - IC: sees self + manager + peers (same manager)
--
-- Returns:
--   team_member_id: the visible team member
--   depth: 0=self, 1=direct, 2+=skip-level, -1=manager
--   relation: 'self', 'manager', 'peer', 'direct', 'descendant'
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

-- Grant execute to authenticated users
grant execute on function procohere.get_visible_team_member_ids(uuid, uuid) to authenticated;

-- ============================================================
-- TEST QUERIES (run after deployment to verify)
-- ============================================================
-- Replace UUIDs with actual values from your database
--
-- -- Get org_id and team_member_ids for testing
-- SELECT id, first_name, last_name, manager_team_member_id 
-- FROM procohere.team_members 
-- WHERE is_deleted = false AND is_active = true
-- ORDER BY first_name;
--
-- -- Test as a manager (should see self + descendants)
-- SELECT * FROM procohere.get_visible_team_member_ids(
--     'your-org-id'::uuid,
--     'manager-team-member-id'::uuid
-- );
--
-- -- Test as an IC (should see self + manager + peers)
-- SELECT * FROM procohere.get_visible_team_member_ids(
--     'your-org-id'::uuid,
--     'ic-team-member-id'::uuid
-- );
-- ============================================================
