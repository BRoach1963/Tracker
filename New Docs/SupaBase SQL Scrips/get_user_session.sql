-- ============================================================
-- get_user_session RPC function
-- Returns the full session payload for ProCohere app login
-- ============================================================

CREATE OR REPLACE FUNCTION public.get_user_session(p_product_key text)
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, procohere
AS $$
DECLARE
    v_user_id uuid;
    v_has_access boolean;
    v_team_member record;
    v_role record;
    v_user record;
    v_result jsonb;
BEGIN
    -- Get the current user from auth context
    v_user_id := auth.uid();
    
    IF v_user_id IS NULL THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'Not authenticated',
            'user', null,
            'team_member', null,
            'role', null
        );
    END IF;

    -- Check product access using the existing function
    v_has_access := public.user_has_active_product_access(p_product_key);
    
    IF NOT v_has_access THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'You do not have access to this product. Please contact your administrator.',
            'user', null,
            'team_member', null,
            'role', null
        );
    END IF;

    -- Get user info from public.users
    SELECT id, email, display_name, avatar_url
    INTO v_user
    FROM public.users
    WHERE id = v_user_id;

    -- Get team member info from procohere.team_members
    SELECT 
        tm.id,
        tm.organization_id,
        tm.first_name,
        tm.last_name,
        tm.email,
        tm.job_title,
        tm.department,
        tm.is_active,
        tm.role_id,
        tm.manager_team_member_id
    INTO v_team_member
    FROM procohere.team_members tm
    WHERE tm.linked_user_id = v_user_id
      AND tm.is_deleted = false
      AND tm.is_active = true
    LIMIT 1;

    -- If no team member found, they have product access but no team member record
    IF v_team_member IS NULL THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'No team member profile found. Please contact your administrator.',
            'user', jsonb_build_object(
                'id', v_user.id,
                'email', v_user.email,
                'display_name', v_user.display_name,
                'avatar_url', v_user.avatar_url
            ),
            'team_member', null,
            'role', null
        );
    END IF;

    -- Get role info from procohere.roles
    SELECT 
        r.id,
        r.name,
        r.description,
        r.permissions,
        r.is_system_role
    INTO v_role
    FROM procohere.roles r
    WHERE r.id = v_team_member.role_id
      AND r.is_deleted = false;

    -- Build the successful response
    RETURN jsonb_build_object(
        'has_access', true,
        'error', null,
        'user', jsonb_build_object(
            'id', v_user.id,
            'email', v_user.email,
            'display_name', v_user.display_name,
            'avatar_url', v_user.avatar_url
        ),
        'team_member', jsonb_build_object(
            'id', v_team_member.id,
            'organization_id', v_team_member.organization_id,
            'first_name', v_team_member.first_name,
            'last_name', v_team_member.last_name,
            'email', v_team_member.email,
            'job_title', v_team_member.job_title,
            'department', v_team_member.department,
            'is_active', v_team_member.is_active,
            'role_id', v_team_member.role_id,
            'manager_team_member_id', v_team_member.manager_team_member_id
        ),
        'role', CASE 
            WHEN v_role IS NOT NULL THEN jsonb_build_object(
                'id', v_role.id,
                'name', v_role.name,
                'description', v_role.description,
                'permissions', v_role.permissions,
                'is_system_role', v_role.is_system_role
            )
            ELSE null
        END
    );
END;
$$;

-- Grant execute to authenticated users
GRANT EXECUTE ON FUNCTION public.get_user_session(text) TO authenticated;

COMMENT ON FUNCTION public.get_user_session(text) IS 
'Returns the full user session for ProCohere app including access status, user info, team member, and role data.';
