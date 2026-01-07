/*
 * TRACKER DATABASE - VIEWS AND FUNCTIONS
 * PostgreSQL Edition
 * 
 * Creates pre-calculated views for dashboard queries and utility functions.
 * These views optimize common queries and provide consistent calculations.
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- VIEW: Team Member Dashboard Summary
-- =============================================================================
-- Pre-aggregated data for the main dashboard view

CREATE OR REPLACE VIEW v_team_member_dashboard AS
SELECT 
    tm.id,
    tm.organization_id,
    tm.current_manager_user_id,
    tm.first_name,
    tm.last_name,
    tm.nick_name,
    COALESCE(tm.nick_name, tm.first_name) || ' ' || COALESCE(tm.last_name, '') AS display_name,
    tm.email,
    tm.job_title,
    tm.hire_date,
    tm.is_active,
    tm.last_one_on_one_date,
    tm.one_on_one_cadence,
    tm.open_task_count,
    
    -- Calculated fields
    CASE 
        WHEN tm.last_one_on_one_date IS NULL THEN -1
        ELSE EXTRACT(DAY FROM (CURRENT_TIMESTAMP - tm.last_one_on_one_date))
    END AS days_since_last_one_on_one,
    
    CASE 
        WHEN tm.last_one_on_one_date IS NULL THEN TRUE
        WHEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - tm.last_one_on_one_date)) > tm.one_on_one_cadence THEN TRUE
        ELSE FALSE
    END AS is_overdue_for_meeting,
    
    -- Manager info
    u.display_name AS manager_name,
    
    -- Task summaries
    (SELECT COUNT(*) FROM tasks t 
     WHERE t.team_member_id = tm.id 
       AND t.status NOT IN ('completed', 'cancelled') 
       AND t.due_date < CURRENT_TIMESTAMP
       AND NOT t.is_deleted) AS overdue_task_count,
    
    -- Tenure
    EXTRACT(YEAR FROM age(CURRENT_DATE, tm.hire_date::DATE)) AS years_of_service,
    
    tm.created_at,
    tm.modified_at

FROM team_members tm
LEFT JOIN users u ON tm.current_manager_user_id = u.id
WHERE NOT tm.is_deleted;

COMMENT ON VIEW v_team_member_dashboard IS 'Pre-aggregated team member data for dashboard display';

-- =============================================================================
-- VIEW: One-on-One Meeting Summary
-- =============================================================================

CREATE OR REPLACE VIEW v_one_on_one_summary AS
SELECT 
    o.id,
    o.organization_id,
    o.team_member_id,
    o.manager_user_id,
    o.meeting_date,
    o.duration_minutes,
    o.status,
    o.overall_sentiment,
    o.follow_up_required,
    
    -- Team member info
    COALESCE(tm.nick_name, tm.first_name) || ' ' || COALESCE(tm.last_name, '') AS team_member_name,
    tm.job_title AS team_member_title,
    
    -- Manager info
    u.display_name AS manager_name,
    
    -- Talking point counts
    (SELECT COUNT(*) FROM talking_points tp 
     WHERE tp.one_on_one_id = o.id AND NOT tp.is_deleted) AS talking_point_count,
    (SELECT COUNT(*) FROM talking_points tp 
     WHERE tp.one_on_one_id = o.id AND tp.status = 'discussed' AND NOT tp.is_deleted) AS discussed_count,
    
    -- Task count created in this meeting
    (SELECT COUNT(*) FROM tasks t 
     WHERE t.one_on_one_id = o.id AND NOT t.is_deleted) AS tasks_created,
    
    o.created_at,
    o.modified_at

FROM one_on_ones o
JOIN team_members tm ON o.team_member_id = tm.id
LEFT JOIN users u ON o.manager_user_id = u.id
WHERE NOT o.is_deleted;

COMMENT ON VIEW v_one_on_one_summary IS 'One-on-one meetings with participant info and counts';

-- =============================================================================
-- VIEW: Task Summary
-- =============================================================================

CREATE OR REPLACE VIEW v_task_summary AS
SELECT 
    t.id,
    t.organization_id,
    t.team_member_id,
    t.project_id,
    t.title,
    t.status,
    t.priority,
    t.due_date,
    t.completed_at,
    
    -- Team member info
    COALESCE(tm.nick_name, tm.first_name) || ' ' || COALESCE(tm.last_name, '') AS assignee_name,
    
    -- Project info
    p.name AS project_name,
    
    -- Calculated fields
    CASE 
        WHEN t.status IN ('completed', 'cancelled') THEN FALSE
        WHEN t.due_date IS NULL THEN FALSE
        WHEN t.due_date < CURRENT_TIMESTAMP THEN TRUE
        ELSE FALSE
    END AS is_overdue,
    
    CASE 
        WHEN t.due_date IS NULL THEN NULL
        ELSE EXTRACT(DAY FROM (t.due_date - CURRENT_TIMESTAMP))
    END AS days_until_due,
    
    t.created_at,
    t.modified_at

FROM tasks t
JOIN team_members tm ON t.team_member_id = tm.id
LEFT JOIN projects p ON t.project_id = p.id
WHERE NOT t.is_deleted;

COMMENT ON VIEW v_task_summary IS 'Tasks with assignee and project info plus status calculations';

-- =============================================================================
-- VIEW: OKR Progress Summary
-- =============================================================================

CREATE OR REPLACE VIEW v_okr_summary AS
SELECT 
    o.id AS objective_id,
    o.organization_id,
    o.team_member_id,
    o.title AS objective_title,
    o.period_name,
    o.period_start,
    o.period_end,
    o.status AS objective_status,
    o.progress_percent AS objective_progress,
    
    -- Team member info (if assigned)
    COALESCE(tm.nick_name, tm.first_name) || ' ' || COALESCE(tm.last_name, '') AS assignee_name,
    
    -- Key result counts
    (SELECT COUNT(*) FROM key_results kr 
     WHERE kr.objective_id = o.id AND NOT kr.is_deleted) AS key_result_count,
    (SELECT COUNT(*) FROM key_results kr 
     WHERE kr.objective_id = o.id AND kr.status = 'completed' AND NOT kr.is_deleted) AS completed_count,
    (SELECT COUNT(*) FROM key_results kr 
     WHERE kr.objective_id = o.id AND kr.status = 'at_risk' AND NOT kr.is_deleted) AS at_risk_count,
    
    o.created_at,
    o.modified_at

FROM objectives o
LEFT JOIN team_members tm ON o.team_member_id = tm.id
WHERE NOT o.is_deleted;

COMMENT ON VIEW v_okr_summary IS 'Objectives with key result progress summary';

-- =============================================================================
-- VIEW: Upcoming Meetings Due
-- =============================================================================

CREATE OR REPLACE VIEW v_meetings_due AS
SELECT 
    tm.id AS team_member_id,
    tm.organization_id,
    tm.current_manager_user_id,
    COALESCE(tm.nick_name, tm.first_name) || ' ' || COALESCE(tm.last_name, '') AS team_member_name,
    tm.job_title,
    tm.last_one_on_one_date,
    tm.one_on_one_cadence,
    
    -- When next meeting is due
    CASE 
        WHEN tm.last_one_on_one_date IS NULL THEN CURRENT_DATE
        ELSE (tm.last_one_on_one_date::DATE + (tm.one_on_one_cadence || ' days')::INTERVAL)::DATE
    END AS next_meeting_due,
    
    -- Days until due (negative = overdue)
    CASE 
        WHEN tm.last_one_on_one_date IS NULL THEN 
            -1 * EXTRACT(DAY FROM age(CURRENT_DATE, tm.hire_date::DATE))::INT
        ELSE 
            (tm.one_on_one_cadence - EXTRACT(DAY FROM age(CURRENT_TIMESTAMP, tm.last_one_on_one_date)))::INT
    END AS days_until_due,
    
    -- Priority (lower = more urgent)
    CASE 
        WHEN tm.last_one_on_one_date IS NULL THEN 0
        WHEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - tm.last_one_on_one_date)) > tm.one_on_one_cadence * 2 THEN 1
        WHEN EXTRACT(DAY FROM (CURRENT_TIMESTAMP - tm.last_one_on_one_date)) > tm.one_on_one_cadence THEN 2
        ELSE 3
    END AS urgency_priority

FROM team_members tm
WHERE tm.is_active 
  AND NOT tm.is_deleted
ORDER BY urgency_priority, days_until_due;

COMMENT ON VIEW v_meetings_due IS 'Team members ranked by meeting urgency';

-- =============================================================================
-- VIEW: Manager Dashboard Stats
-- =============================================================================

CREATE OR REPLACE VIEW v_manager_stats AS
SELECT 
    u.id AS user_id,
    u.organization_id,
    u.display_name AS manager_name,
    
    -- Team counts
    (SELECT COUNT(*) FROM team_members tm 
     WHERE tm.current_manager_user_id = u.id AND tm.is_active AND NOT tm.is_deleted) AS active_team_members,
    
    -- Meeting stats
    (SELECT COUNT(*) FROM team_members tm 
     WHERE tm.current_manager_user_id = u.id 
       AND tm.is_active AND NOT tm.is_deleted
       AND (tm.last_one_on_one_date IS NULL 
            OR EXTRACT(DAY FROM (CURRENT_TIMESTAMP - tm.last_one_on_one_date)) > tm.one_on_one_cadence)
    ) AS overdue_meetings,
    
    -- Meetings this week
    (SELECT COUNT(*) FROM one_on_ones o 
     WHERE o.manager_user_id = u.id 
       AND o.meeting_date >= date_trunc('week', CURRENT_DATE)
       AND o.meeting_date < date_trunc('week', CURRENT_DATE) + INTERVAL '7 days'
       AND NOT o.is_deleted) AS meetings_this_week,
    
    -- Task stats
    (SELECT COUNT(*) FROM tasks t 
     JOIN team_members tm ON t.team_member_id = tm.id
     WHERE tm.current_manager_user_id = u.id
       AND t.status NOT IN ('completed', 'cancelled')
       AND NOT t.is_deleted) AS open_tasks,
    
    (SELECT COUNT(*) FROM tasks t 
     JOIN team_members tm ON t.team_member_id = tm.id
     WHERE tm.current_manager_user_id = u.id
       AND t.status NOT IN ('completed', 'cancelled')
       AND t.due_date < CURRENT_TIMESTAMP
       AND NOT t.is_deleted) AS overdue_tasks,
    
    -- Follow-ups needed
    (SELECT COUNT(*) FROM one_on_ones o 
     WHERE o.manager_user_id = u.id 
       AND o.follow_up_required 
       AND o.status = 'completed'
       AND NOT o.is_deleted) AS pending_follow_ups

FROM users u
WHERE u.is_active AND NOT u.is_deleted;

COMMENT ON VIEW v_manager_stats IS 'Dashboard statistics for managers';

-- =============================================================================
-- UTILITY FUNCTIONS
-- =============================================================================

-- Function to get team member's full history with a manager
CREATE OR REPLACE FUNCTION get_manager_relationship_summary(
    p_team_member_id UUID,
    p_manager_user_id UUID
)
RETURNS TABLE (
    total_one_on_ones BIGINT,
    first_meeting TIMESTAMPTZ,
    last_meeting TIMESTAMPTZ,
    avg_meeting_gap_days NUMERIC,
    total_tasks_assigned BIGINT,
    total_tasks_completed BIGINT,
    avg_sentiment_score NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(o.id) AS total_one_on_ones,
        MIN(o.meeting_date) AS first_meeting,
        MAX(o.meeting_date) AS last_meeting,
        AVG(days_between.gap)::NUMERIC AS avg_meeting_gap_days,
        (SELECT COUNT(*) FROM tasks t 
         WHERE t.team_member_id = p_team_member_id 
           AND t.assigned_by_user_id = p_manager_user_id
           AND NOT t.is_deleted) AS total_tasks_assigned,
        (SELECT COUNT(*) FROM tasks t 
         WHERE t.team_member_id = p_team_member_id 
           AND t.assigned_by_user_id = p_manager_user_id
           AND t.status = 'completed'
           AND NOT t.is_deleted) AS total_tasks_completed,
        AVG(o.sentiment_score)::NUMERIC AS avg_sentiment_score
    FROM one_on_ones o
    LEFT JOIN LATERAL (
        SELECT EXTRACT(DAY FROM (o.meeting_date - LAG(o.meeting_date) 
               OVER (ORDER BY o.meeting_date))) AS gap
    ) days_between ON true
    WHERE o.team_member_id = p_team_member_id
      AND o.manager_user_id = p_manager_user_id
      AND NOT o.is_deleted;
END;
$$ LANGUAGE plpgsql;

-- Function to calculate streak of consecutive on-time meetings
CREATE OR REPLACE FUNCTION get_meeting_streak(p_team_member_id UUID)
RETURNS INT AS $$
DECLARE
    v_streak INT := 0;
    v_cadence INT;
    v_prev_date TIMESTAMPTZ;
    v_curr_date TIMESTAMPTZ;
BEGIN
    SELECT one_on_one_cadence INTO v_cadence
    FROM team_members WHERE id = p_team_member_id;
    
    FOR v_curr_date IN
        SELECT meeting_date FROM one_on_ones
        WHERE team_member_id = p_team_member_id
          AND status = 'completed'
          AND NOT is_deleted
        ORDER BY meeting_date DESC
    LOOP
        IF v_prev_date IS NULL THEN
            v_streak := 1;
        ELSIF EXTRACT(DAY FROM (v_prev_date - v_curr_date)) <= v_cadence + 3 THEN
            v_streak := v_streak + 1;
        ELSE
            EXIT;
        END IF;
        v_prev_date := v_curr_date;
    END LOOP;
    
    RETURN v_streak;
END;
$$ LANGUAGE plpgsql;

-- Function to get organization usage stats
CREATE OR REPLACE FUNCTION get_organization_stats(p_organization_id UUID)
RETURNS TABLE (
    active_users BIGINT,
    active_team_members BIGINT,
    total_one_on_ones BIGINT,
    one_on_ones_this_month BIGINT,
    total_tasks BIGINT,
    open_tasks BIGINT,
    total_embeddings BIGINT,
    storage_used_mb NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        (SELECT COUNT(*) FROM users u 
         WHERE u.organization_id = p_organization_id AND u.is_active AND NOT u.is_deleted),
        (SELECT COUNT(*) FROM team_members tm 
         WHERE tm.organization_id = p_organization_id AND tm.is_active AND NOT tm.is_deleted),
        (SELECT COUNT(*) FROM one_on_ones o 
         WHERE o.organization_id = p_organization_id AND NOT o.is_deleted),
        (SELECT COUNT(*) FROM one_on_ones o 
         WHERE o.organization_id = p_organization_id 
           AND o.meeting_date >= date_trunc('month', CURRENT_DATE)
           AND NOT o.is_deleted),
        (SELECT COUNT(*) FROM tasks t 
         WHERE t.organization_id = p_organization_id AND NOT t.is_deleted),
        (SELECT COUNT(*) FROM tasks t 
         WHERE t.organization_id = p_organization_id 
           AND t.status NOT IN ('completed', 'cancelled') 
           AND NOT t.is_deleted),
        (SELECT COUNT(*) FROM vector_embeddings ve 
         WHERE ve.organization_id = p_organization_id),
        (SELECT COALESCE(SUM(LENGTH(ve.content) + 6144)::NUMERIC / 1048576, 0) -- content + ~6KB per vector
         FROM vector_embeddings ve 
         WHERE ve.organization_id = p_organization_id);
END;
$$ LANGUAGE plpgsql;

\echo 'Views and functions created successfully.'
