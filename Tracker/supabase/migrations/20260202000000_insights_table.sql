-- Migration: AI Insights System
-- Created: February 2, 2026
-- Description: Creates insights table for storing AI-generated insights with RLS policies

-- Create insights table
CREATE TABLE IF NOT EXISTS insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Insight metadata
    insight_type TEXT NOT NULL,
    severity TEXT NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    
    -- Related entities (optional - for linking to specific tasks, goals, etc.)
    entity_type TEXT,
    entity_id UUID,
    
    -- Status tracking
    status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'dismissed', 'acted_on', 'snoozed')),
    dismissed_at TIMESTAMPTZ,
    dismissed_by UUID REFERENCES team_members(id),
    snoozed_until TIMESTAMPTZ,
    acted_on_at TIMESTAMPTZ,
    
    -- Analyzer metadata
    analyzer_name TEXT NOT NULL,
    confidence_score FLOAT DEFAULT 1.0 CHECK (confidence_score >= 0 AND confidence_score <= 1),
    metadata JSONB,
    
    -- Standard timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete support
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES team_members(id)
);

-- Indexes for performance
CREATE INDEX idx_insights_user ON insights(user_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_org ON insights(organization_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_status ON insights(status) WHERE NOT is_deleted AND status = 'active';
CREATE INDEX idx_insights_entity ON insights(entity_type, entity_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_created ON insights(created_at DESC) WHERE NOT is_deleted;
CREATE INDEX idx_insights_type ON insights(insight_type) WHERE NOT is_deleted;
CREATE INDEX idx_insights_severity ON insights(severity) WHERE NOT is_deleted;

-- Enable Row Level Security
ALTER TABLE insights ENABLE ROW LEVEL SECURITY;

-- Policy: Users can view their own insights
CREATE POLICY "Users can view their own insights"
    ON insights FOR SELECT
    USING (user_id = auth.uid() AND NOT is_deleted);

-- Policy: Users can update their own insights (dismiss, act on, snooze)
CREATE POLICY "Users can update their own insights"
    ON insights FOR UPDATE
    USING (user_id = auth.uid() AND NOT is_deleted)
    WITH CHECK (user_id = auth.uid() AND NOT is_deleted);

-- Policy: System/Service role can insert insights
CREATE POLICY "System can insert insights"
    ON insights FOR INSERT
    WITH CHECK (true);

-- Policy: Users can soft-delete their own insights
CREATE POLICY "Users can delete their own insights"
    ON insights FOR UPDATE
    USING (user_id = auth.uid())
    WITH CHECK (user_id = auth.uid());

-- Trigger: Update updated_at timestamp
CREATE TRIGGER update_insights_updated_at
    BEFORE UPDATE ON insights
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- RPC: Check for duplicate insights (same type, entity, and user within 24 hours)
CREATE OR REPLACE FUNCTION check_duplicate_insight(
    p_user_id UUID,
    p_insight_type TEXT,
    p_entity_type TEXT,
    p_entity_id UUID
)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM insights
        WHERE user_id = p_user_id
            AND insight_type = p_insight_type
            AND entity_type = p_entity_type
            AND entity_id = p_entity_id
            AND status = 'active'
            AND NOT is_deleted
            AND created_at > NOW() - INTERVAL '24 hours'
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- RPC: Get active insight count for a user
CREATE OR REPLACE FUNCTION get_active_insight_count(p_user_id UUID)
RETURNS INTEGER AS $$
BEGIN
    RETURN (
        SELECT COUNT(*)
        FROM insights
        WHERE user_id = p_user_id
            AND status = 'active'
            AND NOT is_deleted
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- RPC: Cleanup old dismissed/acted-on insights
CREATE OR REPLACE FUNCTION cleanup_old_insights(p_days_old INTEGER DEFAULT 90)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    WITH deleted AS (
        UPDATE insights
        SET is_deleted = TRUE,
            deleted_at = NOW()
        WHERE status IN ('dismissed', 'acted_on')
            AND updated_at < NOW() - (p_days_old || ' days')::INTERVAL
            AND NOT is_deleted
        RETURNING *
    )
    SELECT COUNT(*) INTO deleted_count FROM deleted;
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Comments for documentation
COMMENT ON TABLE insights IS 'AI-generated insights for users based on data analysis';
COMMENT ON COLUMN insights.insight_type IS 'Type of insight: task_overdue, stale_action_item, goal_off_track, etc.';
COMMENT ON COLUMN insights.severity IS 'Insight severity: low, medium, high, critical';
COMMENT ON COLUMN insights.status IS 'Current status: active, dismissed, acted_on, snoozed';
COMMENT ON COLUMN insights.confidence_score IS 'Analyzer confidence (0.0-1.0) in the insight';
COMMENT ON COLUMN insights.metadata IS 'Additional analyzer-specific metadata as JSON';
