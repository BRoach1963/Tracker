-- ============================================================================
-- TRACKER DATABASE - FEEDBACK AND RECOGNITION
-- ============================================================================

-- ============================================================================
-- FEEDBACK
-- Feedback given to/from team members
-- ============================================================================
CREATE TABLE feedback (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who gave/received
    from_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    to_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Type
    feedback_type feedback_type NOT NULL DEFAULT 'general',
    
    -- Sentiment (positive, neutral, constructive)
    sentiment feedback_sentiment NOT NULL DEFAULT 'neutral',
    
    -- Content
    content TEXT NOT NULL,
    
    -- Context
    context_type VARCHAR(50),  -- project, meeting, task, general
    context_id UUID,  -- Reference to the related entity
    
    -- Visibility
    is_private BOOLEAN NOT NULL DEFAULT false,  -- Only visible to giver/receiver
    
    -- Was this requested?
    is_requested BOOLEAN NOT NULL DEFAULT false,
    request_id UUID,  -- If this was in response to a request
    
    -- AI summary/tags
    ai_summary TEXT,
    ai_tags JSONB,
    
    -- Acknowledged by recipient?
    is_acknowledged BOOLEAN NOT NULL DEFAULT false,
    acknowledged_at TIMESTAMPTZ,
    
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
CREATE INDEX idx_feedback_org ON feedback(organization_id);
CREATE INDEX idx_feedback_from ON feedback(from_team_member_id);
CREATE INDEX idx_feedback_to ON feedback(to_team_member_id);
CREATE INDEX idx_feedback_type ON feedback(to_team_member_id, feedback_type) WHERE is_deleted = false;
CREATE INDEX idx_feedback_recent ON feedback(to_team_member_id, created_at DESC) WHERE is_deleted = false;
CREATE INDEX idx_feedback_sync ON feedback(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER feedback_updated_at
    BEFORE UPDATE ON feedback
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER feedback_sync
    BEFORE UPDATE ON feedback
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- FEEDBACK_REQUESTS
-- Requests for feedback from others
-- ============================================================================
CREATE TABLE feedback_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Requester
    requester_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Who is being asked for feedback
    requested_from_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- About whom? (could be self or another person)
    about_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Request details
    message TEXT,  -- Optional message explaining what feedback is wanted
    
    -- Context
    context_type VARCHAR(50),  -- project, skill, general
    context_id UUID,
    
    -- Due date
    due_date DATE,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'pending',  -- pending, completed, declined, expired
    completed_at TIMESTAMPTZ,
    declined_at TIMESTAMPTZ,
    decline_reason TEXT,
    
    -- Response
    response_feedback_id UUID REFERENCES feedback(id) ON DELETE SET NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_feedback_requests_org ON feedback_requests(organization_id);
CREATE INDEX idx_feedback_requests_requester ON feedback_requests(requester_team_member_id);
CREATE INDEX idx_feedback_requests_from ON feedback_requests(requested_from_team_member_id);
CREATE INDEX idx_feedback_requests_pending ON feedback_requests(requested_from_team_member_id) 
    WHERE status = 'pending';

-- Trigger
CREATE TRIGGER feedback_requests_updated_at
    BEFORE UPDATE ON feedback_requests
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- RECOGNITION
-- Public praise and recognition for team members
-- ============================================================================
CREATE TABLE recognition (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who gave/received
    from_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    to_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Recognition details
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    
    -- Badge/category
    badge_type VARCHAR(100),  -- team_player, innovator, customer_focus, etc.
    
    -- Linked to?
    project_id UUID REFERENCES projects(id) ON DELETE SET NULL,
    goal_id UUID REFERENCES goals(id) ON DELETE SET NULL,
    
    -- Values alignment (company values this demonstrates)
    company_values JSONB,
    
    -- Visibility
    is_public BOOLEAN NOT NULL DEFAULT true,  -- Show in team feed?
    
    -- Reactions
    reactions_count INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id)
);

-- Indexes
CREATE INDEX idx_recognition_org ON recognition(organization_id);
CREATE INDEX idx_recognition_from ON recognition(from_team_member_id);
CREATE INDEX idx_recognition_to ON recognition(to_team_member_id);
CREATE INDEX idx_recognition_recent ON recognition(organization_id, created_at DESC) 
    WHERE is_deleted = false AND is_public = true;

-- ============================================================================
-- RECOGNITION_REACTIONS
-- Reactions to recognition posts
-- ============================================================================
CREATE TABLE recognition_reactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    recognition_id UUID NOT NULL REFERENCES recognition(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    reaction_type VARCHAR(50) NOT NULL DEFAULT 'like',  -- like, celebrate, support, etc.
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (recognition_id, team_member_id, reaction_type)
);

-- Index
CREATE INDEX idx_recognition_reactions_recognition ON recognition_reactions(recognition_id);
CREATE INDEX idx_recognition_reactions_member ON recognition_reactions(team_member_id);

-- ============================================================================
-- PERFORMANCE_REVIEWS
-- Periodic performance review records
-- ============================================================================
CREATE TABLE performance_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who is being reviewed
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Reviewer (usually manager)
    reviewer_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Review period
    review_period_start DATE NOT NULL,
    review_period_end DATE NOT NULL,
    review_type VARCHAR(50) NOT NULL DEFAULT 'annual',  -- annual, mid_year, quarterly, probation
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'draft',  -- draft, self_review, manager_review, calibration, complete
    
    -- Self review
    self_review_content JSONB,
    self_review_submitted_at TIMESTAMPTZ,
    
    -- Manager review
    manager_review_content JSONB,
    manager_review_submitted_at TIMESTAMPTZ,
    
    -- Overall rating
    overall_rating INTEGER,  -- 1-5 scale
    rating_label VARCHAR(100),  -- 'Exceeds', 'Meets', etc.
    
    -- Summary
    strengths TEXT,
    areas_for_improvement TEXT,
    goals_for_next_period TEXT,
    
    -- Sign-off
    employee_acknowledged BOOLEAN NOT NULL DEFAULT false,
    employee_acknowledged_at TIMESTAMPTZ,
    employee_comments TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_performance_reviews_org ON performance_reviews(organization_id);
CREATE INDEX idx_performance_reviews_member ON performance_reviews(team_member_id);
CREATE INDEX idx_performance_reviews_reviewer ON performance_reviews(reviewer_team_member_id);
CREATE INDEX idx_performance_reviews_period ON performance_reviews(team_member_id, review_period_start);

-- Trigger
CREATE TRIGGER performance_reviews_updated_at
    BEFORE UPDATE ON performance_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

SELECT 'Feedback and recognition tables created successfully' AS status;
