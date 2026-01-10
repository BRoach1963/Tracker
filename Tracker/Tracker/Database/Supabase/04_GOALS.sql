-- ============================================================================
-- TRACKER DATABASE - GOALS (formerly OKRs)
-- ============================================================================

-- ============================================================================
-- GOALS (was ObjectiveKeyResults)
-- The main goal entity - what we want to achieve
-- ============================================================================
CREATE TABLE goals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Ownership
    owner_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,  -- Who owns this goal
    created_by_user_id UUID NOT NULL REFERENCES users(id),  -- Who created it
    
    -- Goal details
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Time period
    time_period goal_time_period NOT NULL DEFAULT 'q1',
    year INTEGER NOT NULL DEFAULT EXTRACT(YEAR FROM CURRENT_DATE),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    
    -- Status
    status goal_status NOT NULL DEFAULT 'not_started',
    status_override goal_status,  -- Manual override
    
    -- Progress (calculated from targets, can be overridden)
    progress_percent DECIMAL(5,2) NOT NULL DEFAULT 0,  -- 0-100
    progress_override DECIMAL(5,2),  -- Manual override
    
    -- Visibility
    is_team_visible BOOLEAN NOT NULL DEFAULT true,
    is_org_visible BOOLEAN NOT NULL DEFAULT false,
    
    -- Linked project (optional)
    project_id UUID,  -- FK added after projects table
    
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
    sync_status sync_status DEFAULT 'synced',
    
    -- Constraints
    CONSTRAINT valid_progress CHECK (progress_percent >= 0 AND progress_percent <= 100),
    CONSTRAINT valid_dates CHECK (end_date >= start_date)
);

-- Indexes
CREATE INDEX idx_goals_org ON goals(organization_id);
CREATE INDEX idx_goals_owner ON goals(owner_team_member_id);
CREATE INDEX idx_goals_creator ON goals(created_by_user_id);
CREATE INDEX idx_goals_period ON goals(organization_id, year, time_period);
CREATE INDEX idx_goals_status ON goals(organization_id, status) WHERE is_deleted = false;
CREATE INDEX idx_goals_active ON goals(organization_id, status, end_date) 
    WHERE is_deleted = false AND status NOT IN ('completed', 'cancelled');
CREATE INDEX idx_goals_sync ON goals(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER goals_updated_at
    BEFORE UPDATE ON goals
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER goals_sync
    BEFORE UPDATE ON goals
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- TARGETS (was KeyResults)
-- Measurable outcomes that indicate goal progress
-- ============================================================================
CREATE TABLE targets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES goals(id) ON DELETE CASCADE,
    
    -- Target details
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Measurement
    target_value DECIMAL(18,4) NOT NULL,
    current_value DECIMAL(18,4) NOT NULL DEFAULT 0,
    starting_value DECIMAL(18,4) NOT NULL DEFAULT 0,
    unit VARCHAR(50),  -- '%', 'deals', 'hours', '$', etc.
    
    -- Weighting (for progress calculation)
    weight DECIMAL(5,2) NOT NULL DEFAULT 1.0,
    
    -- Status
    status goal_status NOT NULL DEFAULT 'not_started',
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT valid_weight CHECK (weight > 0)
);

-- Indexes
CREATE INDEX idx_targets_goal ON targets(goal_id);
CREATE INDEX idx_targets_sort ON targets(goal_id, sort_order);

-- Trigger
CREATE TRIGGER targets_updated_at
    BEFORE UPDATE ON targets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- TARGET_MEASURABLES (was KeyResultMeasurables)
-- Link targets to other measurable entities (metrics, projects, task collections)
-- ============================================================================
CREATE TABLE target_measurables (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    target_id UUID NOT NULL REFERENCES targets(id) ON DELETE CASCADE,
    
    -- Polymorphic link
    measurable_type VARCHAR(50) NOT NULL,  -- 'metric', 'project', 'task_collection'
    measurable_id UUID NOT NULL,
    
    -- How to aggregate values
    aggregation_type VARCHAR(50) NOT NULL DEFAULT 'latest',  -- 'latest', 'sum', 'average', 'count'
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (target_id, measurable_type, measurable_id)
);

-- Index
CREATE INDEX idx_target_measurables_target ON target_measurables(target_id);
CREATE INDEX idx_target_measurables_measurable ON target_measurables(measurable_type, measurable_id);

-- ============================================================================
-- GOAL_MILESTONES
-- Key dates/events for goals
-- ============================================================================
CREATE TABLE goal_milestones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES goals(id) ON DELETE CASCADE,
    
    title VARCHAR(200) NOT NULL,
    description TEXT,
    target_date DATE NOT NULL,
    completed_date DATE,
    
    is_completed BOOLEAN NOT NULL DEFAULT false,
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_goal_milestones_goal ON goal_milestones(goal_id);

-- Trigger
CREATE TRIGGER goal_milestones_updated_at
    BEFORE UPDATE ON goal_milestones
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- FUNCTION: Calculate goal progress from targets
-- ============================================================================
CREATE OR REPLACE FUNCTION calculate_goal_progress(goal_uuid UUID)
RETURNS DECIMAL(5,2) AS $$
DECLARE
    total_weight DECIMAL(10,2);
    weighted_progress DECIMAL(10,2);
    target_record RECORD;
BEGIN
    total_weight := 0;
    weighted_progress := 0;
    
    FOR target_record IN 
        SELECT 
            weight,
            current_value,
            starting_value,
            target_value
        FROM targets
        WHERE goal_id = goal_uuid AND is_deleted = false
    LOOP
        total_weight := total_weight + target_record.weight;
        
        -- Calculate progress for this target
        IF target_record.target_value != target_record.starting_value THEN
            weighted_progress := weighted_progress + (
                target_record.weight * 
                LEAST(100, GREATEST(0, 
                    ((target_record.current_value - target_record.starting_value) / 
                     (target_record.target_value - target_record.starting_value)) * 100
                ))
            );
        END IF;
    END LOOP;
    
    IF total_weight = 0 THEN
        RETURN 0;
    END IF;
    
    RETURN ROUND(weighted_progress / total_weight, 2);
END;
$$ LANGUAGE plpgsql;

SELECT 'Goals tables created successfully' AS status;
