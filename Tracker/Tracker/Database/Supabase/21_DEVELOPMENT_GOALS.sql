-- ============================================================================
-- TRACKER DATABASE - DEVELOPMENT GOALS
-- ============================================================================
-- Personal/professional development goals and milestones
-- (Separate from team/org goals - these are individual career growth goals)
-- ============================================================================

-- ============================================================================
-- ENUMS
-- ============================================================================

-- Development goal category
CREATE TYPE dev_goal_category AS ENUM (
    'skill_development',    -- Learn new skills
    'certification',        -- Get certified
    'leadership',           -- Leadership development
    'career_growth',        -- Promotion, role change
    'education',            -- Courses, degrees
    'networking',           -- Build relationships
    'wellness',             -- Work-life balance
    'other'
);

-- Development goal status
CREATE TYPE dev_goal_status AS ENUM (
    'draft',
    'active',
    'on_hold',
    'completed',
    'cancelled'
);

-- Milestone status
CREATE TYPE milestone_status AS ENUM (
    'not_started',
    'in_progress',
    'completed',
    'skipped'
);

-- ============================================================================
-- DEVELOPMENT GOALS
-- Individual professional development goals
-- ============================================================================
CREATE TABLE development_goals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Owner
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Basic info
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Categorization
    category dev_goal_category NOT NULL DEFAULT 'skill_development',
    
    -- Timeline
    target_date DATE,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    
    -- Status
    status dev_goal_status NOT NULL DEFAULT 'draft',
    progress_percent INTEGER DEFAULT 0 CHECK (progress_percent >= 0 AND progress_percent <= 100),
    
    -- Context
    why_important TEXT,  -- Why this goal matters to the person
    success_criteria TEXT,  -- How to know when it's achieved
    
    -- Support
    support_needed TEXT,  -- What help they need
    resources TEXT,  -- Links, books, courses, etc.
    
    -- Visibility
    is_private BOOLEAN NOT NULL DEFAULT false,  -- Only visible to self and manager
    shared_with_manager BOOLEAN NOT NULL DEFAULT true,
    
    -- Link to review (optional)
    review_id UUID REFERENCES reviews(id) ON DELETE SET NULL,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_dev_goals_org ON development_goals(organization_id);
CREATE INDEX idx_dev_goals_member ON development_goals(team_member_id);
CREATE INDEX idx_dev_goals_status ON development_goals(team_member_id, status);
CREATE INDEX idx_dev_goals_category ON development_goals(organization_id, category);

CREATE TRIGGER development_goals_updated_at
    BEFORE UPDATE ON development_goals
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- DEVELOPMENT GOAL MILESTONES
-- Smaller steps toward achieving a development goal
-- ============================================================================
CREATE TABLE development_goal_milestones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES development_goals(id) ON DELETE CASCADE,
    
    -- Basic info
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Timeline
    target_date DATE,
    completed_at TIMESTAMPTZ,
    
    -- Status
    status milestone_status NOT NULL DEFAULT 'not_started',
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Notes
    notes TEXT,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_dev_goal_milestones_goal ON development_goal_milestones(goal_id);
CREATE INDEX idx_dev_goal_milestones_status ON development_goal_milestones(goal_id, status);

CREATE TRIGGER dev_goal_milestones_updated_at
    BEFORE UPDATE ON development_goal_milestones
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- DEVELOPMENT GOAL COMMENTS
-- Manager/self comments and check-ins on progress
-- ============================================================================
CREATE TABLE development_goal_comments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES development_goals(id) ON DELETE CASCADE,
    
    -- Author
    author_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Content
    content TEXT NOT NULL,
    
    -- Type
    comment_type VARCHAR(50) NOT NULL DEFAULT 'comment',  -- comment, check_in, encouragement
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_dev_goal_comments_goal ON development_goal_comments(goal_id);

CREATE TRIGGER dev_goal_comments_updated_at
    BEFORE UPDATE ON development_goal_comments
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

SELECT 'Development goal tables created successfully' AS status;
