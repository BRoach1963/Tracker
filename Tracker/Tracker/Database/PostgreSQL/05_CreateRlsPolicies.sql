/*
 * TRACKER DATABASE - ROW LEVEL SECURITY POLICIES
 * PostgreSQL Edition
 * 
 * Implements Row Level Security (RLS) for multi-tenant data isolation.
 * 
 * Security Model:
 * - All data is scoped to an organization
 * - Users can only access data within their organization
 * - Organization ID is passed via session variable: app.current_organization_id
 * - User ID is passed via session variable: app.current_user_id
 * 
 * Usage:
 * Before executing queries, set the session context:
 *   SET app.current_organization_id = 'uuid-here';
 *   SET app.current_user_id = 'uuid-here';
 * 
 * Or use a function:
 *   SELECT set_session_context('org-uuid', 'user-uuid');
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- HELPER FUNCTIONS FOR RLS
-- =============================================================================

-- Function to get current organization ID from session
CREATE OR REPLACE FUNCTION current_organization_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_organization_id', true), '')::UUID;
EXCEPTION
    WHEN invalid_text_representation THEN
        RETURN NULL;
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to get current user ID from session
CREATE OR REPLACE FUNCTION current_app_user_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', true), '')::UUID;
EXCEPTION
    WHEN invalid_text_representation THEN
        RETURN NULL;
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- Convenience function to set session context
CREATE OR REPLACE FUNCTION set_session_context(
    p_organization_id UUID,
    p_user_id UUID DEFAULT NULL
)
RETURNS VOID AS $$
BEGIN
    PERFORM set_config('app.current_organization_id', p_organization_id::TEXT, false);
    IF p_user_id IS NOT NULL THEN
        PERFORM set_config('app.current_user_id', p_user_id::TEXT, false);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to check if current user is org admin/owner
CREATE OR REPLACE FUNCTION is_org_admin()
RETURNS BOOLEAN AS $$
DECLARE
    v_role TEXT;
BEGIN
    SELECT role INTO v_role
    FROM users
    WHERE id = current_app_user_id()
      AND organization_id = current_organization_id()
      AND NOT is_deleted;
    
    RETURN v_role IN ('owner', 'admin');
END;
$$ LANGUAGE plpgsql STABLE;

-- =============================================================================
-- ENABLE RLS ON ALL TABLES
-- =============================================================================

-- Core tables
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Team tables
ALTER TABLE team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE manager_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE personal_details ENABLE ROW LEVEL SECURITY;
ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_memberships ENABLE ROW LEVEL SECURITY;

-- Meeting tables
ALTER TABLE one_on_ones ENABLE ROW LEVEL SECURITY;
ALTER TABLE talking_points ENABLE ROW LEVEL SECURITY;
ALTER TABLE projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE tasks ENABLE ROW LEVEL SECURITY;
ALTER TABLE objectives ENABLE ROW LEVEL SECURITY;
ALTER TABLE key_results ENABLE ROW LEVEL SECURITY;
ALTER TABLE kpis ENABLE ROW LEVEL SECURITY;
ALTER TABLE kpi_measurements ENABLE ROW LEVEL SECURITY;
ALTER TABLE notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE performance_reviews ENABLE ROW LEVEL SECURITY;

-- Vector tables
ALTER TABLE vector_embeddings ENABLE ROW LEVEL SECURITY;
ALTER TABLE document_chunks ENABLE ROW LEVEL SECURITY;

-- =============================================================================
-- POLICIES FOR ORGANIZATIONS
-- =============================================================================
-- Users can only see their own organization

DROP POLICY IF EXISTS organizations_select ON organizations;
CREATE POLICY organizations_select ON organizations
    FOR SELECT
    USING (id = current_organization_id());

DROP POLICY IF EXISTS organizations_update ON organizations;
CREATE POLICY organizations_update ON organizations
    FOR UPDATE
    USING (id = current_organization_id() AND is_org_admin())
    WITH CHECK (id = current_organization_id());

-- No insert/delete - organizations managed via admin API

-- =============================================================================
-- POLICIES FOR USERS
-- =============================================================================
-- Users can see all users in their organization

DROP POLICY IF EXISTS users_select ON users;
CREATE POLICY users_select ON users
    FOR SELECT
    USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS users_insert ON users;
CREATE POLICY users_insert ON users
    FOR INSERT
    WITH CHECK (organization_id = current_organization_id() AND is_org_admin());

DROP POLICY IF EXISTS users_update ON users;
CREATE POLICY users_update ON users
    FOR UPDATE
    USING (organization_id = current_organization_id() 
           AND (id = current_app_user_id() OR is_org_admin()))
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS users_delete ON users;
CREATE POLICY users_delete ON users
    FOR DELETE
    USING (organization_id = current_organization_id() AND is_org_admin());

-- =============================================================================
-- STANDARD ORGANIZATION-SCOPED POLICIES
-- =============================================================================
-- These policies provide standard org-level isolation for most tables

-- Macro to create standard policies for a table
-- (PostgreSQL doesn't have macros, so we'll write them out)

-- team_members policies
DROP POLICY IF EXISTS team_members_select ON team_members;
CREATE POLICY team_members_select ON team_members
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS team_members_insert ON team_members;
CREATE POLICY team_members_insert ON team_members
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS team_members_update ON team_members;
CREATE POLICY team_members_update ON team_members
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS team_members_delete ON team_members;
CREATE POLICY team_members_delete ON team_members
    FOR DELETE USING (organization_id = current_organization_id());

-- manager_history policies
DROP POLICY IF EXISTS manager_history_select ON manager_history;
CREATE POLICY manager_history_select ON manager_history
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS manager_history_insert ON manager_history;
CREATE POLICY manager_history_insert ON manager_history
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS manager_history_update ON manager_history;
CREATE POLICY manager_history_update ON manager_history
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

-- personal_details policies
DROP POLICY IF EXISTS personal_details_select ON personal_details;
CREATE POLICY personal_details_select ON personal_details
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS personal_details_insert ON personal_details;
CREATE POLICY personal_details_insert ON personal_details
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS personal_details_update ON personal_details;
CREATE POLICY personal_details_update ON personal_details
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS personal_details_delete ON personal_details;
CREATE POLICY personal_details_delete ON personal_details
    FOR DELETE USING (organization_id = current_organization_id());

-- teams policies
DROP POLICY IF EXISTS teams_select ON teams;
CREATE POLICY teams_select ON teams
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS teams_insert ON teams;
CREATE POLICY teams_insert ON teams
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS teams_update ON teams;
CREATE POLICY teams_update ON teams
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS teams_delete ON teams;
CREATE POLICY teams_delete ON teams
    FOR DELETE USING (organization_id = current_organization_id());

-- team_memberships policies
DROP POLICY IF EXISTS team_memberships_select ON team_memberships;
CREATE POLICY team_memberships_select ON team_memberships
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS team_memberships_insert ON team_memberships;
CREATE POLICY team_memberships_insert ON team_memberships
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS team_memberships_delete ON team_memberships;
CREATE POLICY team_memberships_delete ON team_memberships
    FOR DELETE USING (organization_id = current_organization_id());

-- one_on_ones policies
DROP POLICY IF EXISTS one_on_ones_select ON one_on_ones;
CREATE POLICY one_on_ones_select ON one_on_ones
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS one_on_ones_insert ON one_on_ones;
CREATE POLICY one_on_ones_insert ON one_on_ones
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS one_on_ones_update ON one_on_ones;
CREATE POLICY one_on_ones_update ON one_on_ones
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS one_on_ones_delete ON one_on_ones;
CREATE POLICY one_on_ones_delete ON one_on_ones
    FOR DELETE USING (organization_id = current_organization_id());

-- talking_points policies
DROP POLICY IF EXISTS talking_points_select ON talking_points;
CREATE POLICY talking_points_select ON talking_points
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS talking_points_insert ON talking_points;
CREATE POLICY talking_points_insert ON talking_points
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS talking_points_update ON talking_points;
CREATE POLICY talking_points_update ON talking_points
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS talking_points_delete ON talking_points;
CREATE POLICY talking_points_delete ON talking_points
    FOR DELETE USING (organization_id = current_organization_id());

-- projects policies
DROP POLICY IF EXISTS projects_select ON projects;
CREATE POLICY projects_select ON projects
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS projects_insert ON projects;
CREATE POLICY projects_insert ON projects
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS projects_update ON projects;
CREATE POLICY projects_update ON projects
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS projects_delete ON projects;
CREATE POLICY projects_delete ON projects
    FOR DELETE USING (organization_id = current_organization_id());

-- tasks policies
DROP POLICY IF EXISTS tasks_select ON tasks;
CREATE POLICY tasks_select ON tasks
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS tasks_insert ON tasks;
CREATE POLICY tasks_insert ON tasks
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS tasks_update ON tasks;
CREATE POLICY tasks_update ON tasks
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS tasks_delete ON tasks;
CREATE POLICY tasks_delete ON tasks
    FOR DELETE USING (organization_id = current_organization_id());

-- objectives policies
DROP POLICY IF EXISTS objectives_select ON objectives;
CREATE POLICY objectives_select ON objectives
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS objectives_insert ON objectives;
CREATE POLICY objectives_insert ON objectives
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS objectives_update ON objectives;
CREATE POLICY objectives_update ON objectives
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS objectives_delete ON objectives;
CREATE POLICY objectives_delete ON objectives
    FOR DELETE USING (organization_id = current_organization_id());

-- key_results policies
DROP POLICY IF EXISTS key_results_select ON key_results;
CREATE POLICY key_results_select ON key_results
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS key_results_insert ON key_results;
CREATE POLICY key_results_insert ON key_results
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS key_results_update ON key_results;
CREATE POLICY key_results_update ON key_results
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS key_results_delete ON key_results;
CREATE POLICY key_results_delete ON key_results
    FOR DELETE USING (organization_id = current_organization_id());

-- kpis policies
DROP POLICY IF EXISTS kpis_select ON kpis;
CREATE POLICY kpis_select ON kpis
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS kpis_insert ON kpis;
CREATE POLICY kpis_insert ON kpis
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS kpis_update ON kpis;
CREATE POLICY kpis_update ON kpis
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS kpis_delete ON kpis;
CREATE POLICY kpis_delete ON kpis
    FOR DELETE USING (organization_id = current_organization_id());

-- kpi_measurements policies
DROP POLICY IF EXISTS kpi_measurements_select ON kpi_measurements;
CREATE POLICY kpi_measurements_select ON kpi_measurements
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS kpi_measurements_insert ON kpi_measurements;
CREATE POLICY kpi_measurements_insert ON kpi_measurements
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

-- notes policies
DROP POLICY IF EXISTS notes_select ON notes;
CREATE POLICY notes_select ON notes
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS notes_insert ON notes;
CREATE POLICY notes_insert ON notes
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS notes_update ON notes;
CREATE POLICY notes_update ON notes
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS notes_delete ON notes;
CREATE POLICY notes_delete ON notes
    FOR DELETE USING (organization_id = current_organization_id());

-- performance_reviews policies
DROP POLICY IF EXISTS performance_reviews_select ON performance_reviews;
CREATE POLICY performance_reviews_select ON performance_reviews
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS performance_reviews_insert ON performance_reviews;
CREATE POLICY performance_reviews_insert ON performance_reviews
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS performance_reviews_update ON performance_reviews;
CREATE POLICY performance_reviews_update ON performance_reviews
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS performance_reviews_delete ON performance_reviews;
CREATE POLICY performance_reviews_delete ON performance_reviews
    FOR DELETE USING (organization_id = current_organization_id());

-- vector_embeddings policies
DROP POLICY IF EXISTS vector_embeddings_select ON vector_embeddings;
CREATE POLICY vector_embeddings_select ON vector_embeddings
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS vector_embeddings_insert ON vector_embeddings;
CREATE POLICY vector_embeddings_insert ON vector_embeddings
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS vector_embeddings_update ON vector_embeddings;
CREATE POLICY vector_embeddings_update ON vector_embeddings
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS vector_embeddings_delete ON vector_embeddings;
CREATE POLICY vector_embeddings_delete ON vector_embeddings
    FOR DELETE USING (organization_id = current_organization_id());

-- document_chunks policies
DROP POLICY IF EXISTS document_chunks_select ON document_chunks;
CREATE POLICY document_chunks_select ON document_chunks
    FOR SELECT USING (organization_id = current_organization_id());

DROP POLICY IF EXISTS document_chunks_insert ON document_chunks;
CREATE POLICY document_chunks_insert ON document_chunks
    FOR INSERT WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS document_chunks_update ON document_chunks;
CREATE POLICY document_chunks_update ON document_chunks
    FOR UPDATE
    USING (organization_id = current_organization_id())
    WITH CHECK (organization_id = current_organization_id());

DROP POLICY IF EXISTS document_chunks_delete ON document_chunks;
CREATE POLICY document_chunks_delete ON document_chunks
    FOR DELETE USING (organization_id = current_organization_id());

-- =============================================================================
-- BYPASS POLICY FOR SERVICE ACCOUNT
-- =============================================================================
-- Create a role that bypasses RLS for administrative tasks

-- DO $$
-- BEGIN
--     IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'tracker_service') THEN
--         CREATE ROLE tracker_service NOLOGIN;
--     END IF;
-- END
-- $$;
-- 
-- ALTER ROLE tracker_service BYPASSRLS;
-- GRANT tracker_service TO tracker_admin;  -- Admin can assume service role

\echo 'Row Level Security policies created successfully.'
\echo ''
\echo 'IMPORTANT: Before executing queries, set the session context:'
\echo '  SELECT set_session_context(''your-org-uuid'', ''your-user-uuid'');'
\echo ''
\echo 'Or manually:'
\echo '  SET app.current_organization_id = ''your-org-uuid'';'
\echo '  SET app.current_user_id = ''your-user-uuid'';'
