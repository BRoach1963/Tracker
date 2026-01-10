-- ============================================================================
-- TRACKER DATABASE - SEED DATA: DEFAULT ROLES
-- ============================================================================
-- Run this after all tables are created to set up default roles

-- ============================================================================
-- INSERT DEFAULT ROLES
-- Roles are global (not per-organization) in this schema
-- ============================================================================

-- Admin role - full access
INSERT INTO roles (name, display_name, description, is_system_role, sort_order,
    can_manage_org, can_manage_billing, can_manage_users, can_invite_users, can_assign_roles,
    can_manage_teams, can_create_teams,
    can_create_goals, can_edit_all_goals, can_edit_own_goals, can_view_team_goals, can_view_org_goals,
    can_create_metrics, can_edit_metrics, can_view_team_metrics, can_view_org_metrics,
    can_create_tasks, can_assign_tasks, can_view_team_tasks,
    can_schedule_meetings, can_run_meetings, can_participate_meetings, can_view_meeting_notes,
    can_give_feedback, can_receive_feedback, can_view_team_feedback,
    can_view_team_analytics, can_view_org_analytics, can_export_data)
VALUES (
    'admin', 'Admin', 'Full administrative access to all features', true, 100,
    true, true, true, true, true,  -- org/user management
    true, true,  -- team management
    true, true, true, true, true,  -- goals
    true, true, true, true,  -- metrics
    true, true, true,  -- tasks
    true, true, true, true,  -- meetings
    true, true, true,  -- feedback
    true, true, true  -- analytics
);

-- Manager role - manage their team
INSERT INTO roles (name, display_name, description, is_system_role, sort_order,
    can_manage_org, can_manage_billing, can_manage_users, can_invite_users, can_assign_roles,
    can_manage_teams, can_create_teams,
    can_create_goals, can_edit_all_goals, can_edit_own_goals, can_view_team_goals, can_view_org_goals,
    can_create_metrics, can_edit_metrics, can_view_team_metrics, can_view_org_metrics,
    can_create_tasks, can_assign_tasks, can_view_team_tasks,
    can_schedule_meetings, can_run_meetings, can_participate_meetings, can_view_meeting_notes,
    can_give_feedback, can_receive_feedback, can_view_team_feedback,
    can_view_team_analytics, can_view_org_analytics, can_export_data)
VALUES (
    'manager', 'Manager', 'Manage team members, goals, and performance', true, 75,
    false, false, true, true, false,  -- limited org/user management
    true, true,  -- team management
    true, true, true, true, true,  -- goals (team scope enforced by RLS)
    true, true, true, false,  -- metrics (team only)
    true, true, true,  -- tasks
    true, true, true, true,  -- meetings
    true, true, true,  -- feedback
    true, false, true  -- analytics (team only)
);

-- Team Lead role - lead a small team
INSERT INTO roles (name, display_name, description, is_system_role, sort_order,
    can_manage_org, can_manage_billing, can_manage_users, can_invite_users, can_assign_roles,
    can_manage_teams, can_create_teams,
    can_create_goals, can_edit_all_goals, can_edit_own_goals, can_view_team_goals, can_view_org_goals,
    can_create_metrics, can_edit_metrics, can_view_team_metrics, can_view_org_metrics,
    can_create_tasks, can_assign_tasks, can_view_team_tasks,
    can_schedule_meetings, can_run_meetings, can_participate_meetings, can_view_meeting_notes,
    can_give_feedback, can_receive_feedback, can_view_team_feedback,
    can_view_team_analytics, can_view_org_analytics, can_export_data)
VALUES (
    'team_lead', 'Team Lead', 'Lead and coordinate team activities', true, 50,
    false, false, false, false, false,  -- no org/user management
    false, false,  -- no team management
    true, false, true, true, false,  -- goals (own + view team)
    true, true, true, false,  -- metrics (team only)
    true, true, true,  -- tasks
    true, true, true, true,  -- meetings
    true, true, true,  -- feedback
    true, false, false  -- analytics (team only, no export)
);

-- Member role - individual contributor
INSERT INTO roles (name, display_name, description, is_system_role, sort_order,
    can_manage_org, can_manage_billing, can_manage_users, can_invite_users, can_assign_roles,
    can_manage_teams, can_create_teams,
    can_create_goals, can_edit_all_goals, can_edit_own_goals, can_view_team_goals, can_view_org_goals,
    can_create_metrics, can_edit_metrics, can_view_team_metrics, can_view_org_metrics,
    can_create_tasks, can_assign_tasks, can_view_team_tasks,
    can_schedule_meetings, can_run_meetings, can_participate_meetings, can_view_meeting_notes,
    can_give_feedback, can_receive_feedback, can_view_team_feedback,
    can_view_team_analytics, can_view_org_analytics, can_export_data)
VALUES (
    'member', 'Member', 'Team member with standard access', true, 25,
    false, false, false, false, false,  -- no org/user management
    false, false,  -- no team management
    true, false, true, true, false,  -- goals (own + view team)
    true, false, true, false,  -- metrics (create own, view team)
    true, false, true,  -- tasks (create own, view team)
    true, false, true, true,  -- meetings (schedule, participate)
    true, true, false,  -- feedback (give/receive, not view team)
    false, false, false  -- no analytics
);

-- Viewer role - read-only access
INSERT INTO roles (name, display_name, description, is_system_role, sort_order,
    can_manage_org, can_manage_billing, can_manage_users, can_invite_users, can_assign_roles,
    can_manage_teams, can_create_teams,
    can_create_goals, can_edit_all_goals, can_edit_own_goals, can_view_team_goals, can_view_org_goals,
    can_create_metrics, can_edit_metrics, can_view_team_metrics, can_view_org_metrics,
    can_create_tasks, can_assign_tasks, can_view_team_tasks,
    can_schedule_meetings, can_run_meetings, can_participate_meetings, can_view_meeting_notes,
    can_give_feedback, can_receive_feedback, can_view_team_feedback,
    can_view_team_analytics, can_view_org_analytics, can_export_data)
VALUES (
    'viewer', 'Viewer', 'Read-only access to view team data', true, 10,
    false, false, false, false, false,  -- no org/user management
    false, false,  -- no team management
    false, false, false, true, false,  -- goals (view team only)
    false, false, true, false,  -- metrics (view team only)
    false, false, true,  -- tasks (view team only)
    false, false, true, true,  -- meetings (participate, view notes)
    false, true, false,  -- feedback (receive only)
    false, false, false  -- no analytics
);

SELECT 'Default roles created successfully' AS status;
SELECT name, display_name, sort_order FROM roles ORDER BY sort_order DESC;
