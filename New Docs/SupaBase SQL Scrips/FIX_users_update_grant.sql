-- ============================================================================
-- FIX: Add missing columns to UPDATE grant for public.users
-- ============================================================================
-- Issue: Users cannot update their own profile because the column-level GRANT
-- is missing birthday, hire_date, and updated_at columns.
--
-- The RLS policy (users_update_self_safe) correctly allows users to update 
-- their own row (id = auth.uid()), but the column-level permissions were 
-- too restrictive.
-- ============================================================================

-- Revoke existing and re-grant with all updatable columns
REVOKE UPDATE ON public.users FROM authenticated;

GRANT UPDATE (
    display_name, 
    avatar_url, 
    phone, 
    timezone, 
    first_name, 
    last_name, 
    job_title, 
    company, 
    birthday,           -- ADDED
    hire_date,          -- ADDED  
    preferences, 
    notification_settings,
    updated_at          -- ADDED
) ON public.users TO authenticated;

-- Verify the grants (run this to check)
-- SELECT grantee, privilege_type, table_name 
-- FROM information_schema.role_table_grants 
-- WHERE table_name = 'users' AND table_schema = 'public';
