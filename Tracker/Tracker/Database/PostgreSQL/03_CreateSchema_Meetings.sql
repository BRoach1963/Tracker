/*
 * TRACKER DATABASE - MEETINGS SCHEMA
 * PostgreSQL Edition
 * 
 * Creates the meeting and task management tables:
 * - one_on_ones: One-on-one meeting records
 * - talking_points: Meeting agenda items
 * - projects: Work projects for organization
 * - tasks: Individual tasks for team members
 * - objectives/key_results: OKR tracking
 * - kpis: Key performance indicators
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- ONE_ON_ONES TABLE
-- =============================================================================
-- One-on-one meeting records between manager and team member

CREATE TABLE IF NOT EXISTS one_on_ones (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Participants
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    manager_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Meeting details
    meeting_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    duration_minutes INT DEFAULT 30,
    location VARCHAR(200),
    meeting_type VARCHAR(50) DEFAULT 'regular',  -- regular, skip_level, check_in, career
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'scheduled',  -- scheduled, completed, cancelled
    
    -- Notes
    notes TEXT,
    private_notes TEXT,  -- Manager's private notes
    
    -- Sentiment tracking
    overall_sentiment VARCHAR(20),  -- positive, neutral, negative, mixed
    sentiment_score DECIMAL(3,2),  -- -1.0 to 1.0
    
    -- Follow-up
    next_meeting_date TIMESTAMPTZ,
    follow_up_required BOOLEAN NOT NULL DEFAULT false,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes for one_on_ones
CREATE INDEX IF NOT EXISTS ix_one_on_ones_organization_id 
    ON one_on_ones(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_one_on_ones_team_member 
    ON one_on_ones(team_member_id, meeting_date DESC) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_one_on_ones_manager 
    ON one_on_ones(manager_user_id, meeting_date DESC) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_one_on_ones_date 
    ON one_on_ones(organization_id, meeting_date) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_one_on_ones_status 
    ON one_on_ones(organization_id, status) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_one_on_ones_follow_up 
    ON one_on_ones(organization_id, follow_up_required) 
    WHERE follow_up_required AND NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_one_on_ones_modified_at ON one_on_ones;
CREATE TRIGGER trg_one_on_ones_modified_at
    BEFORE UPDATE ON one_on_ones
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

-- Trigger to update team member's last_one_on_one_date
CREATE OR REPLACE FUNCTION update_last_one_on_one()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'completed' THEN
        UPDATE team_members
        SET last_one_on_one_date = NEW.meeting_date,
            modified_at = CURRENT_TIMESTAMP
        WHERE id = NEW.team_member_id
          AND (last_one_on_one_date IS NULL OR last_one_on_one_date < NEW.meeting_date);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_one_on_ones_update_last ON one_on_ones;
CREATE TRIGGER trg_one_on_ones_update_last
    AFTER INSERT OR UPDATE OF status, meeting_date ON one_on_ones
    FOR EACH ROW
    EXECUTE FUNCTION update_last_one_on_one();

COMMENT ON TABLE one_on_ones IS 'One-on-one meeting records between managers and team members';
COMMENT ON COLUMN one_on_ones.private_notes IS 'Manager-only notes not visible to team member';
COMMENT ON COLUMN one_on_ones.sentiment_score IS 'AI-derived sentiment from -1.0 (negative) to 1.0 (positive)';

-- =============================================================================
-- TALKING_POINTS TABLE
-- =============================================================================
-- Agenda items for one-on-one meetings

CREATE TABLE IF NOT EXISTS talking_points (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Link to meeting (optional - can be a standing item)
    one_on_one_id UUID REFERENCES one_on_ones(id),
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    
    -- Content
    title VARCHAR(500) NOT NULL,
    description TEXT,
    
    -- Source
    source VARCHAR(50) DEFAULT 'manager',  -- manager, team_member, ai_suggested
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'pending',  -- pending, discussed, deferred, cancelled
    discussed_at TIMESTAMPTZ,
    
    -- Priority
    priority INT NOT NULL DEFAULT 0,  -- Lower = higher priority
    
    -- Recurring
    is_recurring BOOLEAN NOT NULL DEFAULT false,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes for talking_points
CREATE INDEX IF NOT EXISTS ix_talking_points_organization_id 
    ON talking_points(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_talking_points_one_on_one 
    ON talking_points(one_on_one_id) WHERE one_on_one_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_talking_points_team_member 
    ON talking_points(team_member_id, status) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_talking_points_pending 
    ON talking_points(team_member_id, priority) 
    WHERE status = 'pending' AND NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_talking_points_modified_at ON talking_points;
CREATE TRIGGER trg_talking_points_modified_at
    BEFORE UPDATE ON talking_points
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE talking_points IS 'Agenda items for one-on-one meetings';
COMMENT ON COLUMN talking_points.source IS 'Who created: manager, team_member, or ai_suggested';

-- =============================================================================
-- PROJECTS TABLE
-- =============================================================================
-- Work projects that tasks can be associated with

CREATE TABLE IF NOT EXISTS projects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'active',  -- active, on_hold, completed, cancelled
    
    -- Dates
    start_date DATE,
    target_end_date DATE,
    actual_end_date DATE,
    
    -- Progress
    percent_complete DECIMAL(5,2) DEFAULT 0,
    
    -- Priority
    priority INT NOT NULL DEFAULT 0,
    
    -- Links
    project_url VARCHAR(500),
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes for projects
CREATE INDEX IF NOT EXISTS ix_projects_organization_id 
    ON projects(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_projects_status 
    ON projects(organization_id, status) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_projects_modified_at ON projects;
CREATE TRIGGER trg_projects_modified_at
    BEFORE UPDATE ON projects
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE projects IS 'Work projects for task organization and tracking';

-- =============================================================================
-- TASKS TABLE (called individual_tasks in C# model)
-- =============================================================================
-- Action items assigned to team members

CREATE TABLE IF NOT EXISTS tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Assignment
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    assigned_by_user_id UUID REFERENCES users(id),
    
    -- Optional links
    one_on_one_id UUID REFERENCES one_on_ones(id),
    project_id UUID REFERENCES projects(id),
    
    -- Content
    title VARCHAR(500) NOT NULL,
    description TEXT,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'not_started',  -- not_started, in_progress, blocked, completed, cancelled
    
    -- Priority (matches C# enum: Low=0, Medium=1, High=2, Critical=3)
    priority INT NOT NULL DEFAULT 1,
    
    -- Dates
    due_date TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    
    -- Effort tracking
    estimated_hours DECIMAL(6,2),
    actual_hours DECIMAL(6,2),
    
    -- Follow-up
    follow_up_date TIMESTAMPTZ,
    
    -- Notes
    notes TEXT,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes for tasks
CREATE INDEX IF NOT EXISTS ix_tasks_organization_id 
    ON tasks(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_team_member 
    ON tasks(team_member_id, status) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_project 
    ON tasks(project_id) WHERE project_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_one_on_one 
    ON tasks(one_on_one_id) WHERE one_on_one_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_status 
    ON tasks(organization_id, status) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_due_date 
    ON tasks(organization_id, due_date) 
    WHERE due_date IS NOT NULL AND status NOT IN ('completed', 'cancelled') AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_tasks_overdue 
    ON tasks(team_member_id, due_date) 
    WHERE status NOT IN ('completed', 'cancelled') AND NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_tasks_modified_at ON tasks;
CREATE TRIGGER trg_tasks_modified_at
    BEFORE UPDATE ON tasks
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

-- Trigger to update team member's open_task_count
CREATE OR REPLACE FUNCTION update_open_task_count()
RETURNS TRIGGER AS $$
DECLARE
    v_team_member_id UUID;
    v_new_count INT;
BEGIN
    -- Determine which team member to update
    IF TG_OP = 'DELETE' THEN
        v_team_member_id := OLD.team_member_id;
    ELSE
        v_team_member_id := NEW.team_member_id;
        -- If team member changed, also update old one
        IF TG_OP = 'UPDATE' AND OLD.team_member_id != NEW.team_member_id THEN
            SELECT COUNT(*) INTO v_new_count
            FROM tasks
            WHERE team_member_id = OLD.team_member_id
              AND status NOT IN ('completed', 'cancelled')
              AND NOT is_deleted;
            
            UPDATE team_members
            SET open_task_count = v_new_count,
                modified_at = CURRENT_TIMESTAMP
            WHERE id = OLD.team_member_id;
        END IF;
    END IF;
    
    -- Update the current team member's count
    SELECT COUNT(*) INTO v_new_count
    FROM tasks
    WHERE team_member_id = v_team_member_id
      AND status NOT IN ('completed', 'cancelled')
      AND NOT is_deleted;
    
    UPDATE team_members
    SET open_task_count = v_new_count,
        modified_at = CURRENT_TIMESTAMP
    WHERE id = v_team_member_id;
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_tasks_update_count ON tasks;
CREATE TRIGGER trg_tasks_update_count
    AFTER INSERT OR UPDATE OR DELETE ON tasks
    FOR EACH ROW
    EXECUTE FUNCTION update_open_task_count();

COMMENT ON TABLE tasks IS 'Action items assigned to team members';
COMMENT ON COLUMN tasks.priority IS '0=Low, 1=Medium, 2=High, 3=Critical';

-- =============================================================================
-- OBJECTIVES TABLE (OKRs)
-- =============================================================================

CREATE TABLE IF NOT EXISTS objectives (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Optional assignment
    team_member_id UUID REFERENCES team_members(id),
    
    -- Content
    title VARCHAR(500) NOT NULL,
    description TEXT,
    
    -- Time period
    period_start DATE,
    period_end DATE,
    period_name VARCHAR(50),  -- e.g., "Q1 2025"
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'active',  -- draft, active, completed, cancelled
    
    -- Progress
    progress_percent DECIMAL(5,2) DEFAULT 0,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_objectives_organization_id 
    ON objectives(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_objectives_team_member 
    ON objectives(team_member_id) WHERE team_member_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_objectives_period 
    ON objectives(organization_id, period_start, period_end) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_objectives_modified_at ON objectives;
CREATE TRIGGER trg_objectives_modified_at
    BEFORE UPDATE ON objectives
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE objectives IS 'OKR Objectives for goal tracking';

-- =============================================================================
-- KEY_RESULTS TABLE
-- =============================================================================

CREATE TABLE IF NOT EXISTS key_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Parent objective
    objective_id UUID NOT NULL REFERENCES objectives(id),
    
    -- Content
    title VARCHAR(500) NOT NULL,
    description TEXT,
    
    -- Metrics
    target_value DECIMAL(18,4) NOT NULL DEFAULT 100,
    current_value DECIMAL(18,4) NOT NULL DEFAULT 0,
    unit VARCHAR(50),  -- e.g., "%", "count", "$"
    
    -- Progress
    progress_percent DECIMAL(5,2) GENERATED ALWAYS AS (
        CASE WHEN target_value = 0 THEN 0
             ELSE LEAST(100, (current_value / target_value) * 100)
        END
    ) STORED,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'on_track',  -- on_track, at_risk, behind, completed
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_key_results_organization_id 
    ON key_results(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_key_results_objective 
    ON key_results(objective_id) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_key_results_modified_at ON key_results;
CREATE TRIGGER trg_key_results_modified_at
    BEFORE UPDATE ON key_results
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

-- Trigger to update objective progress when key result changes
CREATE OR REPLACE FUNCTION update_objective_progress()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE objectives o
    SET progress_percent = (
        SELECT COALESCE(AVG(kr.progress_percent), 0)
        FROM key_results kr
        WHERE kr.objective_id = o.id
          AND NOT kr.is_deleted
    ),
    modified_at = CURRENT_TIMESTAMP
    WHERE o.id = COALESCE(NEW.objective_id, OLD.objective_id);
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_key_results_update_objective ON key_results;
CREATE TRIGGER trg_key_results_update_objective
    AFTER INSERT OR UPDATE OR DELETE ON key_results
    FOR EACH ROW
    EXECUTE FUNCTION update_objective_progress();

COMMENT ON TABLE key_results IS 'Key Results linked to Objectives (OKR framework)';

-- =============================================================================
-- KPIS TABLE
-- =============================================================================

CREATE TABLE IF NOT EXISTS kpis (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Optional assignment
    team_member_id UUID REFERENCES team_members(id),
    
    -- Content
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    
    -- Metrics
    target_value DECIMAL(18,4),
    current_value DECIMAL(18,4),
    unit VARCHAR(50),
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Tracking
    measurement_frequency VARCHAR(50) DEFAULT 'monthly',  -- daily, weekly, monthly, quarterly
    last_measured_at TIMESTAMPTZ,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- KPI historical values
CREATE TABLE IF NOT EXISTS kpi_measurements (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    kpi_id UUID NOT NULL REFERENCES kpis(id),
    measured_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    value DECIMAL(18,4) NOT NULL,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_kpis_organization_id 
    ON kpis(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_kpis_team_member 
    ON kpis(team_member_id) WHERE team_member_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_kpi_measurements_kpi 
    ON kpi_measurements(kpi_id, measured_at DESC);

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_kpis_modified_at ON kpis;
CREATE TRIGGER trg_kpis_modified_at
    BEFORE UPDATE ON kpis
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE kpis IS 'Key Performance Indicators for tracking metrics';
COMMENT ON TABLE kpi_measurements IS 'Historical KPI measurement values';

-- =============================================================================
-- NOTES TABLE
-- =============================================================================
-- General notes that can be linked to various entities

CREATE TABLE IF NOT EXISTS notes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Optional links (polymorphic)
    team_member_id UUID REFERENCES team_members(id),
    one_on_one_id UUID REFERENCES one_on_ones(id),
    project_id UUID REFERENCES projects(id),
    
    -- Content
    title VARCHAR(500),
    content TEXT NOT NULL,
    
    -- Classification
    note_type VARCHAR(50) DEFAULT 'general',  -- general, private, follow_up, praise, concern
    
    -- Status
    is_pinned BOOLEAN NOT NULL DEFAULT false,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_notes_organization_id 
    ON notes(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_notes_team_member 
    ON notes(team_member_id) WHERE team_member_id IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_notes_one_on_one 
    ON notes(one_on_one_id) WHERE one_on_one_id IS NOT NULL AND NOT is_deleted;

-- Full-text search on notes content
CREATE INDEX IF NOT EXISTS ix_notes_content_search 
    ON notes USING gin(to_tsvector('english', coalesce(title, '') || ' ' || content)) 
    WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_notes_modified_at ON notes;
CREATE TRIGGER trg_notes_modified_at
    BEFORE UPDATE ON notes
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE notes IS 'General notes linked to team members, meetings, or projects';

-- =============================================================================
-- PERFORMANCE_REVIEWS TABLE
-- =============================================================================

CREATE TABLE IF NOT EXISTS performance_reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Participants
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    reviewer_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Review period
    review_period_start DATE NOT NULL,
    review_period_end DATE NOT NULL,
    review_type VARCHAR(50) NOT NULL DEFAULT 'annual',  -- annual, mid_year, quarterly, probation
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'draft',  -- draft, in_progress, pending_approval, completed
    
    -- Ratings (1-5 scale typically)
    overall_rating DECIMAL(3,2),
    
    -- Content
    achievements TEXT,
    areas_for_improvement TEXT,
    goals_for_next_period TEXT,
    manager_comments TEXT,
    employee_comments TEXT,
    
    -- Dates
    completed_at TIMESTAMPTZ,
    acknowledged_at TIMESTAMPTZ,  -- When employee acknowledged
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_performance_reviews_organization_id 
    ON performance_reviews(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_performance_reviews_team_member 
    ON performance_reviews(team_member_id, review_period_end DESC) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_performance_reviews_status 
    ON performance_reviews(organization_id, status) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_performance_reviews_modified_at ON performance_reviews;
CREATE TRIGGER trg_performance_reviews_modified_at
    BEFORE UPDATE ON performance_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE performance_reviews IS 'Formal performance review records';

\echo 'Meetings schema (one_on_ones, talking_points, projects, tasks, objectives, key_results, kpis, notes, performance_reviews) created successfully.'
