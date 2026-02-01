-- ============================================================================
-- Goal Health Batch RPC Supporting Indexes
-- ============================================================================
-- These indexes support the procohere.get_goal_health_batch_v2 RPC function
-- which computes derived goal health from linked metrics in a single call.
--
-- Tables involved:
--   - procohere.metric_values: Stores metric value history for trend analysis
--   - procohere.goal_metrics: Links goals to their supporting metrics
--
-- Created: 2026-02-01
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Index: metric_values(metric_id, recorded_at DESC)
-- Purpose: Fast retrieval of latest N metric values for trend calculation
-- Used by: get_goal_health_batch_v2 to get last 3 values per metric
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_metric_values_metric_recorded
ON procohere.metric_values (metric_id, recorded_at DESC)
WHERE is_deleted = false;

-- ----------------------------------------------------------------------------
-- Index: goal_metrics(goal_id)
-- Purpose: Fast lookup of all metrics linked to a specific goal
-- Used by: get_goal_health_batch_v2 to find metrics for each goal
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_goal_metrics_goal_id
ON procohere.goal_metrics (goal_id)
WHERE is_deleted = false;

-- ----------------------------------------------------------------------------
-- Index: goal_metrics(metric_id)
-- Purpose: Fast reverse lookup of all goals linked to a specific metric
-- Used by: Metric detail views showing which goals reference the metric
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_goal_metrics_metric_id
ON procohere.goal_metrics (metric_id)
WHERE is_deleted = false;

-- ============================================================================
-- Verification Query (run after applying)
-- ============================================================================
-- SELECT indexname, indexdef 
-- FROM pg_indexes 
-- WHERE schemaname = 'procohere' 
--   AND tablename IN ('metric_values', 'goal_metrics')
-- ORDER BY tablename, indexname;
