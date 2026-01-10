-- ============================================================================
-- TRACKER DATABASE - METRICS (formerly KPIs)
-- ============================================================================

-- ============================================================================
-- METRICS (was KeyPerformanceIndicators)
-- Quantitative measures of performance
-- ============================================================================
CREATE TABLE metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Ownership
    owner_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Metric details
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100),  -- 'Sales', 'Engineering', 'Customer Success', etc.
    
    -- Values
    current_value DECIMAL(18,4) NOT NULL DEFAULT 0,
    target_value DECIMAL(18,4),
    baseline_value DECIMAL(18,4),  -- Starting point for comparison
    unit VARCHAR(50),  -- '%', '$', 'count', 'hours', etc.
    
    -- Target direction
    target_direction metric_target_direction NOT NULL DEFAULT 'higher_is_better',
    
    -- Update frequency
    frequency metric_frequency NOT NULL DEFAULT 'monthly',
    last_updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Composite metrics (calculated from children)
    is_composite BOOLEAN NOT NULL DEFAULT false,
    parent_metric_id UUID REFERENCES metrics(id) ON DELETE SET NULL,
    
    -- Visibility
    is_team_visible BOOLEAN NOT NULL DEFAULT true,
    is_org_visible BOOLEAN NOT NULL DEFAULT false,
    
    -- Thresholds for status
    warning_threshold DECIMAL(18,4),  -- Below this = at risk
    critical_threshold DECIMAL(18,4),  -- Below this = off track
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id),
    
    -- Sync metadata
    sync_id UUID DEFAULT gen_random_uuid(),
    sync_version INTEGER DEFAULT 1,
    sync_modified_at TIMESTAMPTZ DEFAULT NOW(),
    sync_status sync_status DEFAULT 'synced'
);

-- Indexes
CREATE INDEX idx_metrics_org ON metrics(organization_id);
CREATE INDEX idx_metrics_owner ON metrics(owner_team_member_id);
CREATE INDEX idx_metrics_category ON metrics(organization_id, category) WHERE is_deleted = false;
CREATE INDEX idx_metrics_parent ON metrics(parent_metric_id) WHERE parent_metric_id IS NOT NULL;
CREATE INDEX idx_metrics_sync ON metrics(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER metrics_updated_at
    BEFORE UPDATE ON metrics
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER metrics_sync
    BEFORE UPDATE ON metrics
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- METRIC_DATA_SOURCES (was KpiDataSources)
-- Where metric values come from (manual, calculated, external)
-- ============================================================================
CREATE TABLE metric_data_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_id UUID NOT NULL REFERENCES metrics(id) ON DELETE CASCADE,
    
    -- Source type
    source_type VARCHAR(50) NOT NULL,  -- 'manual', 'project', 'task_query', 'child_metric', 'api'
    
    -- Source reference (polymorphic)
    source_id UUID,  -- FK to appropriate table based on source_type
    source_config JSONB,  -- Additional configuration
    
    -- Aggregation
    aggregation_type VARCHAR(50) NOT NULL DEFAULT 'latest',  -- 'latest', 'sum', 'average', 'count'
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_metric_data_sources_metric ON metric_data_sources(metric_id);

-- Trigger
CREATE TRIGGER metric_data_sources_updated_at
    BEFORE UPDATE ON metric_data_sources
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- METRIC_HISTORY
-- Historical values for trending
-- ============================================================================
CREATE TABLE metric_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_id UUID NOT NULL REFERENCES metrics(id) ON DELETE CASCADE,
    
    value DECIMAL(18,4) NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Who/what recorded it
    recorded_by_user_id UUID REFERENCES users(id),
    source VARCHAR(50) DEFAULT 'manual',  -- 'manual', 'api', 'calculated'
    
    notes TEXT
);

-- Indexes
CREATE INDEX idx_metric_history_metric ON metric_history(metric_id);
CREATE INDEX idx_metric_history_date ON metric_history(metric_id, recorded_at DESC);

-- ============================================================================
-- FUNCTION: Get metric status based on value and thresholds
-- ============================================================================
CREATE OR REPLACE FUNCTION get_metric_status(
    p_current_value DECIMAL(18,4),
    p_target_value DECIMAL(18,4),
    p_warning_threshold DECIMAL(18,4),
    p_critical_threshold DECIMAL(18,4),
    p_target_direction metric_target_direction
)
RETURNS goal_status AS $$
BEGIN
    IF p_target_value IS NULL THEN
        RETURN 'not_started';
    END IF;
    
    -- Calculate progress percentage
    DECLARE
        progress DECIMAL(18,4);
    BEGIN
        IF p_target_direction = 'higher_is_better' THEN
            progress := (p_current_value / p_target_value) * 100;
            
            IF progress >= 100 THEN
                RETURN 'completed';
            ELSIF p_critical_threshold IS NOT NULL AND progress < p_critical_threshold THEN
                RETURN 'off_track';
            ELSIF p_warning_threshold IS NOT NULL AND progress < p_warning_threshold THEN
                RETURN 'at_risk';
            ELSE
                RETURN 'on_track';
            END IF;
            
        ELSIF p_target_direction = 'lower_is_better' THEN
            -- Inverted logic
            IF p_current_value <= p_target_value THEN
                RETURN 'completed';
            ELSIF p_critical_threshold IS NOT NULL AND p_current_value > p_critical_threshold THEN
                RETURN 'off_track';
            ELSIF p_warning_threshold IS NOT NULL AND p_current_value > p_warning_threshold THEN
                RETURN 'at_risk';
            ELSE
                RETURN 'on_track';
            END IF;
            
        ELSE  -- target_value (exact match)
            IF p_current_value = p_target_value THEN
                RETURN 'completed';
            ELSE
                RETURN 'on_track';
            END IF;
        END IF;
    END;
END;
$$ LANGUAGE plpgsql;

SELECT 'Metrics tables created successfully' AS status;
