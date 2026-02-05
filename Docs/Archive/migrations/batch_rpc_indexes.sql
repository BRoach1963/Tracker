-- ============================================================================
-- BATCH RPC INDEXES
-- Migration for supporting efficient batch operations
-- ============================================================================
-- Run in Supabase SQL Editor
-- These indexes support the following batch RPCs:
--   - get_project_signals_batch
--   - get_weekly_meeting_load
--   - get_metrics_with_trend_batch
--   - get_goal_health_batch_v2
-- ============================================================================

-- Project signals batch: join project_links to tasks/goals
CREATE INDEX IF NOT EXISTS idx_project_links_project_entity 
ON procohere.project_links(project_id, entity_type, entity_id)
WHERE is_deleted = false;

-- Project signals batch: filter overdue tasks
CREATE INDEX IF NOT EXISTS idx_tasks_due_status 
ON procohere.tasks(due_date, status)
WHERE is_deleted = false;

-- Project signals batch: filter goals needing attention
CREATE INDEX IF NOT EXISTS idx_goals_status 
ON procohere.goals(status)
WHERE is_deleted = false;

-- Weekly meeting load: attendee lookup
CREATE INDEX IF NOT EXISTS idx_meeting_attendees_member_meeting 
ON procohere.meeting_attendees(team_member_id, meeting_id)
WHERE is_deleted = false;

-- Weekly meeting load: date range filter
CREATE INDEX IF NOT EXISTS idx_meetings_scheduled 
ON procohere.meetings(scheduled_at)
WHERE is_deleted = false;

-- Metrics trend batch: latest values lookup
CREATE INDEX IF NOT EXISTS idx_metric_values_metric_recorded_at 
ON procohere.metric_values(metric_id, recorded_at DESC)
WHERE is_deleted = false;

-- Goal health batch: target progress lookup
CREATE INDEX IF NOT EXISTS idx_targets_goal_id 
ON procohere.targets(goal_id)
WHERE is_deleted = false;

-- ============================================================================
-- VERIFY INDEXES
-- ============================================================================
-- Run this query to verify indexes were created:
/*
SELECT indexname, tablename, indexdef 
FROM pg_indexes 
WHERE schemaname = 'procohere'
  AND indexname IN (
    'idx_project_links_project_entity',
    'idx_tasks_due_status',
    'idx_goals_status',
    'idx_meeting_attendees_member_meeting',
    'idx_meetings_scheduled',
    'idx_metric_values_metric_recorded_at',
    'idx_targets_goal_id'
  );
*/
