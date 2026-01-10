-- ============================================================================
-- TRACKER DATABASE - ROW LEVEL SECURITY POLICIES
-- ============================================================================

-- Enable RLS on all tables
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_memberships ENABLE ROW LEVEL SECURITY;
ALTER TABLE manager_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE goals ENABLE ROW LEVEL SECURITY;
ALTER TABLE targets ENABLE ROW LEVEL SECURITY;
ALTER TABLE target_measurables ENABLE ROW LEVEL SECURITY;
ALTER TABLE goal_milestones ENABLE ROW LEVEL SECURITY;
ALTER TABLE metrics ENABLE ROW LEVEL SECURITY;
ALTER TABLE metric_data_sources ENABLE ROW LEVEL SECURITY;
ALTER TABLE metric_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE project_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE milestones ENABLE ROW LEVEL SECURITY;
ALTER TABLE tasks ENABLE ROW LEVEL SECURITY;
ALTER TABLE task_collections ENABLE ROW LEVEL SECURITY;
ALTER TABLE task_collection_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;
ALTER TABLE meeting_attendees ENABLE ROW LEVEL SECURITY;
ALTER TABLE meeting_agenda_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE meeting_notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE action_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE talking_points ENABLE ROW LEVEL SECURITY;
ALTER TABLE feedback ENABLE ROW LEVEL SECURITY;
ALTER TABLE feedback_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE recognition ENABLE ROW LEVEL SECURITY;
ALTER TABLE recognition_reactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE performance_reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE note_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE journal_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE vector_embeddings ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_insights ENABLE ROW LEVEL SECURITY;
ALTER TABLE activity_log ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE notification_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE announcements ENABLE ROW LEVEL SECURITY;
ALTER TABLE announcement_reads ENABLE ROW LEVEL SECURITY;

-- ============================================================================
-- HELPER FUNCTIONS FOR RLS
-- ============================================================================

-- Get current user's ID from JWT
CREATE OR REPLACE FUNCTION auth_user_id()
RETURNS UUID AS $$
BEGIN
    RETURN auth.uid();
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER STABLE;

-- Get current user's organization IDs (they might belong to multiple)
CREATE OR REPLACE FUNCTION user_organization_ids()
RETURNS UUID[] AS $$
BEGIN
    RETURN ARRAY(
        SELECT DISTINCT u.organization_id
        FROM users u
        WHERE u.supabase_auth_id = auth.uid()
          AND u.is_active = true
          AND u.organization_id IS NOT NULL
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER STABLE;

-- Get current user's team_member IDs (one per organization)
CREATE OR REPLACE FUNCTION user_team_member_ids()
RETURNS UUID[] AS $$
BEGIN
    RETURN ARRAY(
        SELECT u.linked_team_member_id
        FROM users u
        WHERE u.supabase_auth_id = auth.uid()
          AND u.is_active = true
          AND u.linked_team_member_id IS NOT NULL
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER STABLE;

-- Check if user has a specific permission in any of their roles
CREATE OR REPLACE FUNCTION user_has_permission(p_permission TEXT)
RETURNS BOOLEAN AS $$
DECLARE
    v_has_permission BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM users u
        JOIN user_roles ur ON ur.user_id = u.id
        JOIN roles r ON r.id = ur.role_id
        WHERE u.supabase_auth_id = auth.uid()
          AND (
            -- Check the specific permission column dynamically
            CASE p_permission
                -- Organization permissions
                WHEN 'can_manage_org' THEN r.can_manage_org
                WHEN 'can_manage_billing' THEN r.can_manage_billing
                -- User permissions
                WHEN 'can_manage_users' THEN r.can_manage_users
                WHEN 'can_invite_users' THEN r.can_invite_users
                WHEN 'can_assign_roles' THEN r.can_assign_roles
                -- Team permissions
                WHEN 'can_manage_teams' THEN r.can_manage_teams
                WHEN 'can_create_teams' THEN r.can_create_teams
                -- Goal permissions
                WHEN 'can_create_goals' THEN r.can_create_goals
                WHEN 'can_edit_all_goals' THEN r.can_edit_all_goals
                WHEN 'can_edit_own_goals' THEN r.can_edit_own_goals
                WHEN 'can_view_team_goals' THEN r.can_view_team_goals
                WHEN 'can_view_org_goals' THEN r.can_view_org_goals
                -- Metric permissions
                WHEN 'can_create_metrics' THEN r.can_create_metrics
                WHEN 'can_edit_metrics' THEN r.can_edit_metrics
                WHEN 'can_view_team_metrics' THEN r.can_view_team_metrics
                WHEN 'can_view_org_metrics' THEN r.can_view_org_metrics
                -- Task permissions
                WHEN 'can_create_tasks' THEN r.can_create_tasks
                WHEN 'can_assign_tasks' THEN r.can_assign_tasks
                WHEN 'can_view_team_tasks' THEN r.can_view_team_tasks
                -- Meeting permissions
                WHEN 'can_schedule_meetings' THEN r.can_schedule_meetings
                WHEN 'can_run_meetings' THEN r.can_run_meetings
                WHEN 'can_participate_meetings' THEN r.can_participate_meetings
                WHEN 'can_view_meeting_notes' THEN r.can_view_meeting_notes
                -- Feedback permissions
                WHEN 'can_give_feedback' THEN r.can_give_feedback
                WHEN 'can_receive_feedback' THEN r.can_receive_feedback
                WHEN 'can_view_team_feedback' THEN r.can_view_team_feedback
                -- Analytics permissions
                WHEN 'can_view_team_analytics' THEN r.can_view_team_analytics
                WHEN 'can_view_org_analytics' THEN r.can_view_org_analytics
                WHEN 'can_export_data' THEN r.can_export_data
                ELSE false
            END
          )
    ) INTO v_has_permission;
    
    RETURN COALESCE(v_has_permission, false);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER STABLE;

-- Check if user manages a specific team member
CREATE OR REPLACE FUNCTION user_manages_team_member(p_team_member_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM users u
        WHERE u.supabase_auth_id = auth.uid()
          AND u.linked_team_member_id = (
              SELECT manager_team_member_id 
              FROM team_members 
              WHERE id = p_team_member_id
          )
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER STABLE;

-- ============================================================================
-- RLS POLICIES - ORGANIZATIONS
-- ============================================================================

CREATE POLICY "Users can view their organizations"
    ON organizations FOR SELECT
    USING (id = ANY(user_organization_ids()));

CREATE POLICY "Admins can update their organization"
    ON organizations FOR UPDATE
    USING (id = ANY(user_organization_ids()) AND user_has_permission('can_manage_organization'));

-- ============================================================================
-- RLS POLICIES - ROLES
-- ============================================================================

-- Roles are global/system-defined, all authenticated users can view
CREATE POLICY "Authenticated users can view roles"
    ON roles FOR SELECT
    USING (true);

-- Only superadmins can modify roles (handled at application level)
CREATE POLICY "No direct role modification"
    ON roles FOR ALL
    USING (false)
    WITH CHECK (false);

-- ============================================================================
-- RLS POLICIES - USERS
-- ============================================================================

CREATE POLICY "Users can view their own user record"
    ON users FOR SELECT
    USING (supabase_auth_id = auth.uid());

CREATE POLICY "Users can update their own user record"
    ON users FOR UPDATE
    USING (supabase_auth_id = auth.uid());

-- ============================================================================
-- RLS POLICIES - TEAM_MEMBERS
-- ============================================================================

CREATE POLICY "Users can view team members in their org"
    ON team_members FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND (
            user_has_permission('can_manage_users')
            OR id = ANY(user_team_member_ids())  -- Can always view self
        )
    );

CREATE POLICY "Managers can edit their team members"
    ON team_members FOR UPDATE
    USING (
        organization_id = ANY(user_organization_ids())
        AND (
            user_has_permission('can_manage_users')
            OR id = ANY(user_team_member_ids())  -- Can edit own profile
        )
    );

-- ============================================================================
-- RLS POLICIES - TASKS
-- ============================================================================

CREATE POLICY "Users can view tasks they have access to"
    ON tasks FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            user_has_permission('can_view_team_tasks')
            OR owner_team_member_id = ANY(user_team_member_ids())
            OR user_manages_team_member(owner_team_member_id)
        )
    );

CREATE POLICY "Users can create tasks"
    ON tasks FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND user_has_permission('can_create_tasks')
    );

CREATE POLICY "Users can update tasks they have access to"
    ON tasks FOR UPDATE
    USING (
        organization_id = ANY(user_organization_ids())
        AND (
            user_has_permission('can_assign_tasks')
            OR owner_team_member_id = ANY(user_team_member_ids())  -- Can edit own tasks
        )
    );

-- ============================================================================
-- RLS POLICIES - GOALS
-- ============================================================================

CREATE POLICY "Users can view goals they have access to"
    ON goals FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            user_has_permission('can_view_org_goals')
            OR user_has_permission('can_view_team_goals')
            OR owner_team_member_id = ANY(user_team_member_ids())
        )
    );

CREATE POLICY "Users can create goals"
    ON goals FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND user_has_permission('can_create_goals')
    );

CREATE POLICY "Users can update goals they have access to"
    ON goals FOR UPDATE
    USING (
        organization_id = ANY(user_organization_ids())
        AND (
            user_has_permission('can_edit_all_goals')
            OR (user_has_permission('can_edit_own_goals') AND owner_team_member_id = ANY(user_team_member_ids()))
        )
    );

-- ============================================================================
-- RLS POLICIES - METRICS
-- ============================================================================

CREATE POLICY "Users can view metrics they have access to"
    ON metrics FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            user_has_permission('can_view_org_metrics')
            OR user_has_permission('can_view_team_metrics')
            OR owner_team_member_id = ANY(user_team_member_ids())
        )
    );

CREATE POLICY "Users can create metrics"
    ON metrics FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND user_has_permission('can_create_metrics')
    );

CREATE POLICY "Users can update metrics they have access to"
    ON metrics FOR UPDATE
    USING (
        organization_id = ANY(user_organization_ids())
        AND (
            user_has_permission('can_edit_metrics')
            OR owner_team_member_id = ANY(user_team_member_ids())
        )
    );

-- ============================================================================
-- RLS POLICIES - FEEDBACK
-- ============================================================================

CREATE POLICY "Users can view feedback they gave or received"
    ON feedback FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            user_has_permission('can_view_team_feedback')
            OR from_team_member_id = ANY(user_team_member_ids())
            OR to_team_member_id = ANY(user_team_member_ids())
        )
    );

CREATE POLICY "Users can give feedback"
    ON feedback FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND user_has_permission('can_give_feedback')
        AND from_team_member_id = ANY(user_team_member_ids())
    );

-- ============================================================================
-- RLS POLICIES - MEETINGS
-- ============================================================================

CREATE POLICY "Users can view meetings they are part of"
    ON meetings FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            manager_team_member_id = ANY(user_team_member_ids())
            OR report_team_member_id = ANY(user_team_member_ids())
            OR EXISTS (
                SELECT 1 FROM meeting_attendees ma 
                WHERE ma.meeting_id = meetings.id 
                  AND ma.team_member_id = ANY(user_team_member_ids())
            )
            OR team_id IN (
                SELECT team_id FROM team_memberships 
                WHERE team_member_id = ANY(user_team_member_ids())
            )
        )
    );

CREATE POLICY "Users can create meetings"
    ON meetings FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND user_has_permission('can_schedule_meetings')
    );

-- ============================================================================
-- RLS POLICIES - NOTES
-- ============================================================================

CREATE POLICY "Users can view their own notes"
    ON notes FOR SELECT
    USING (
        organization_id = ANY(user_organization_ids())
        AND is_deleted = false
        AND (
            author_team_member_id = ANY(user_team_member_ids())
            OR (is_private = false AND (
                linked_team_member_id = ANY(user_team_member_ids())
                OR user_manages_team_member(linked_team_member_id)
            ))
        )
    );

CREATE POLICY "Users can create notes"
    ON notes FOR INSERT
    WITH CHECK (
        organization_id = ANY(user_organization_ids())
        AND author_team_member_id = ANY(user_team_member_ids())
    );

CREATE POLICY "Users can update their own notes"
    ON notes FOR UPDATE
    USING (
        author_team_member_id = ANY(user_team_member_ids())
    );

-- ============================================================================
-- RLS POLICIES - NOTIFICATIONS
-- ============================================================================

CREATE POLICY "Users can view their own notifications"
    ON notifications FOR SELECT
    USING (
        user_id IN (SELECT id FROM users WHERE supabase_auth_id = auth.uid())
    );

CREATE POLICY "Users can update their own notifications"
    ON notifications FOR UPDATE
    USING (
        user_id IN (SELECT id FROM users WHERE supabase_auth_id = auth.uid())
    );

-- ============================================================================
-- RLS POLICIES - JOURNAL ENTRIES (always private)
-- ============================================================================

CREATE POLICY "Users can only access their own journal entries"
    ON journal_entries FOR ALL
    USING (
        team_member_id = ANY(user_team_member_ids())
    );

SELECT 'Row Level Security policies created successfully' AS status;
