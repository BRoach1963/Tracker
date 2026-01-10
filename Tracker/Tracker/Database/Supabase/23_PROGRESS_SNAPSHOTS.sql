-- ============================================================================
-- TRACKER DATABASE - PROGRESS SNAPSHOTS & ANALYTICS
-- ============================================================================
-- Historical snapshots for trend analysis and reporting
-- ============================================================================

-- ============================================================================
-- ENUMS
-- ============================================================================

-- Snapshot period type
CREATE TYPE snapshot_period AS ENUM (
    'daily',
    'weekly',
    'monthly',
    'quarterly',
    'yearly'
);

-- ============================================================================
-- PROGRESS SNAPSHOTS
-- Point-in-time snapshots of key metrics for trend analysis
-- ============================================================================
CREATE TABLE progress_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- What this snapshot is for
    entity_type VARCHAR(50) NOT NULL,  -- team_member, team, goal, project, org
    entity_id UUID NOT NULL,
    
    -- When
    snapshot_date DATE NOT NULL,
    period_type snapshot_period NOT NULL DEFAULT 'weekly',
    
    -- Metrics (flexible JSONB for different entity types)
    metrics JSONB NOT NULL DEFAULT '{}',
    
    -- Common metrics pulled out for querying
    overall_score DECIMAL(5,2),  -- e.g., goal progress %, performance score
    trend_direction INTEGER,  -- -1 down, 0 flat, 1 up (vs previous period)
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_snapshot UNIQUE (entity_type, entity_id, snapshot_date, period_type)
);

CREATE INDEX idx_snapshots_org ON progress_snapshots(organization_id);
CREATE INDEX idx_snapshots_entity ON progress_snapshots(entity_type, entity_id);
CREATE INDEX idx_snapshots_date ON progress_snapshots(snapshot_date);
CREATE INDEX idx_snapshots_lookup ON progress_snapshots(entity_type, entity_id, snapshot_date DESC);

-- ============================================================================
-- TEAM MEMBER SNAPSHOTS
-- Detailed weekly/monthly snapshots for each team member
-- ============================================================================
CREATE TABLE team_member_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Period
    snapshot_date DATE NOT NULL,
    period_type snapshot_period NOT NULL DEFAULT 'weekly',
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- Goals
    goals_total INTEGER DEFAULT 0,
    goals_on_track INTEGER DEFAULT 0,
    goals_at_risk INTEGER DEFAULT 0,
    goals_completed INTEGER DEFAULT 0,
    goal_progress_avg DECIMAL(5,2),  -- Average progress across active goals
    
    -- Tasks
    tasks_total INTEGER DEFAULT 0,
    tasks_completed INTEGER DEFAULT 0,
    tasks_overdue INTEGER DEFAULT 0,
    task_completion_rate DECIMAL(5,2),
    
    -- Meetings
    one_on_ones_held INTEGER DEFAULT 0,
    one_on_ones_scheduled INTEGER DEFAULT 0,
    meetings_attended INTEGER DEFAULT 0,
    
    -- Feedback
    feedback_given INTEGER DEFAULT 0,
    feedback_received INTEGER DEFAULT 0,
    recognition_given INTEGER DEFAULT 0,
    recognition_received INTEGER DEFAULT 0,
    
    -- Engagement (if survey data available)
    engagement_score DECIMAL(5,2),
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_member_snapshot UNIQUE (team_member_id, snapshot_date, period_type)
);

CREATE INDEX idx_member_snapshots_org ON team_member_snapshots(organization_id);
CREATE INDEX idx_member_snapshots_member ON team_member_snapshots(team_member_id);
CREATE INDEX idx_member_snapshots_date ON team_member_snapshots(snapshot_date);

-- ============================================================================
-- TEAM SNAPSHOTS
-- Aggregated team-level metrics
-- ============================================================================
CREATE TABLE team_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    
    -- Period
    snapshot_date DATE NOT NULL,
    period_type snapshot_period NOT NULL DEFAULT 'weekly',
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- Team composition
    member_count INTEGER DEFAULT 0,
    active_member_count INTEGER DEFAULT 0,
    
    -- Goals
    goals_total INTEGER DEFAULT 0,
    goals_on_track INTEGER DEFAULT 0,
    goals_completed INTEGER DEFAULT 0,
    goal_completion_rate DECIMAL(5,2),
    
    -- Tasks
    tasks_total INTEGER DEFAULT 0,
    tasks_completed INTEGER DEFAULT 0,
    task_completion_rate DECIMAL(5,2),
    
    -- Meetings
    one_on_ones_completion_rate DECIMAL(5,2),
    team_meetings_held INTEGER DEFAULT 0,
    
    -- Engagement
    avg_engagement_score DECIMAL(5,2),
    survey_response_rate DECIMAL(5,2),
    
    -- Feedback culture
    feedback_exchanges INTEGER DEFAULT 0,
    recognition_count INTEGER DEFAULT 0,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_team_snapshot UNIQUE (team_id, snapshot_date, period_type)
);

CREATE INDEX idx_team_snapshots_org ON team_snapshots(organization_id);
CREATE INDEX idx_team_snapshots_team ON team_snapshots(team_id);
CREATE INDEX idx_team_snapshots_date ON team_snapshots(snapshot_date);

-- ============================================================================
-- ORGANIZATION SNAPSHOTS
-- Company-wide metrics
-- ============================================================================
CREATE TABLE organization_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Period
    snapshot_date DATE NOT NULL,
    period_type snapshot_period NOT NULL DEFAULT 'weekly',
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- Headcount
    total_users INTEGER DEFAULT 0,
    active_users INTEGER DEFAULT 0,
    total_team_members INTEGER DEFAULT 0,
    
    -- Adoption metrics
    users_logged_in INTEGER DEFAULT 0,
    login_rate DECIMAL(5,2),
    
    -- Goals
    goals_total INTEGER DEFAULT 0,
    goals_on_track_rate DECIMAL(5,2),
    goals_completed_this_period INTEGER DEFAULT 0,
    
    -- Meetings
    one_on_ones_held INTEGER DEFAULT 0,
    one_on_one_completion_rate DECIMAL(5,2),
    
    -- Engagement
    avg_engagement_score DECIMAL(5,2),
    enps_score INTEGER,  -- Employee NPS
    
    -- Feedback & Recognition
    feedback_count INTEGER DEFAULT 0,
    recognition_count INTEGER DEFAULT 0,
    
    -- Reviews
    reviews_in_progress INTEGER DEFAULT 0,
    reviews_completed INTEGER DEFAULT 0,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_org_snapshot UNIQUE (organization_id, snapshot_date, period_type)
);

CREATE INDEX idx_org_snapshots_org ON organization_snapshots(organization_id);
CREATE INDEX idx_org_snapshots_date ON organization_snapshots(snapshot_date);

SELECT 'Progress snapshot tables created successfully' AS status;
