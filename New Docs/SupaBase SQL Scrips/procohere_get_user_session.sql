-- ============================================================
-- procohere.get_user_session RPC function
-- Schema: procohere
-- Purpose: Returns complete session data for authenticated user
--          including team member and role information
-- ============================================================

CREATE OR REPLACE FUNCTION procohere.get_user_session(p_product_key text)
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
BEGIN
    -- Get current user
    v_user_id := auth.uid();
    
    IF v_user_id IS NULL THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'Not authenticated'
        );
    END IF;

    -- Check product access using PUBLIC schema function
    -- IMPORTANT: Must use public. prefix since this function is in procohere schema
    v_has_access := public.user_has_active_product_access(p_product_key);
    
    IF NOT v_has_access THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'No active license for this product'
        );
    END IF;

    -- Get user info from auth.users
    SELECT id, email, raw_user_meta_data->>'display_name' as display_name
    INTO v_user
    FROM auth.users
    WHERE id = v_user_id;

    -- Get team member for this user from procohere schema
    -- Note: department column removed as it doesn't exist in the actual table
    SELECT tm.id, tm.organization_id, tm.first_name, tm.last_name, 
           tm.email, tm.job_title, tm.role_id
    INTO v_team_member
    FROM procohere.team_members tm
    WHERE tm.linked_user_id = v_user_id
      AND tm.is_deleted = false
      AND tm.is_active = true
    LIMIT 1;

    IF v_team_member.id IS NULL THEN
        RETURN jsonb_build_object(
            'has_access', false,
            'error', 'No team member record found'
        );
    END IF;

    -- Get role from procohere schema
    SELECT r.id, r.name, r.permissions
    INTO v_role
    FROM procohere.roles r
    WHERE r.id = v_team_member.role_id
      AND r.is_deleted = false;

    -- Return full session payload
    RETURN jsonb_build_object(
        'has_access', true,
        'error', null,
        'user', jsonb_build_object(
            'id', v_user.id,
            'email', v_user.email,
            'display_name', v_user.display_name
        ),
        'team_member', jsonb_build_object(
            'id', v_team_member.id,
            'organization_id', v_team_member.organization_id,
            'first_name', v_team_member.first_name,
            'last_name', v_team_member.last_name,
            'full_name', v_team_member.first_name || ' ' || v_team_member.last_name,
            'email', v_team_member.email,
            'job_title', v_team_member.job_title,
            'role_id', v_team_member.role_id
        ),
        'role', jsonb_build_object(
            'id', v_role.id,
            'name', v_role.name,
            'permissions', v_role.permissions
        )
    );
END;
$$;

-- Grant execute to authenticated users
GRANT EXECUTE ON FUNCTION procohere.get_user_session(text) TO authenticated;

-- Documentation
COMMENT ON FUNCTION procohere.get_user_session(text) IS 
'Returns complete session data for the authenticated user.

Parameters:
  - p_product_key: The product key to check access for (e.g., ''procohere'')

Returns JSONB with:
  - has_access: boolean - whether user has valid access
  - error: string or null - error message if access denied
  - user: object with id, email, display_name
  - team_member: object with team member details
  - role: object with role id, name, and permissions

Prerequisites:
  - procohere schema must be exposed in PostgREST (API Settings > Exposed schemas)
  - public.user_has_active_product_access function must exist
  - User must have active team_member record linked to their auth.users.id

Usage from C# (Supabase client configured for procohere schema):
  var result = await _procohereClient.Rpc("get_user_session", new { p_product_key = "procohere" });

Security:
  - SECURITY DEFINER: Runs with function owner privileges
  - search_path set explicitly to prevent injection
  - Checks product access before returning any data
';
