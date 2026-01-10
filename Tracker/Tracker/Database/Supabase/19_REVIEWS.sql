-- ============================================================================
-- TRACKER DATABASE - PERFORMANCE REVIEWS
-- ============================================================================
-- Review templates, cycles, and individual reviews
-- ============================================================================

-- ============================================================================
-- ENUMS
-- ============================================================================

-- Review cycle status
CREATE TYPE review_cycle_status AS ENUM (
    'draft',
    'active',
    'completed',
    'cancelled'
);

-- Review status
CREATE TYPE review_status AS ENUM (
    'not_started',
    'in_progress',
    'submitted',
    'acknowledged',
    'completed'
);

-- Question type for review templates
CREATE TYPE review_question_type AS ENUM (
    'rating',           -- 1-5 scale
    'text',             -- Free text
    'yes_no',           -- Boolean
    'multiple_choice',  -- Select from options
    'competency'        -- Competency rating with levels
);

-- ============================================================================
-- REVIEW TEMPLATES
-- Reusable templates for performance reviews
-- ============================================================================
CREATE TABLE review_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Settings
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Review type
    review_type VARCHAR(50) NOT NULL DEFAULT 'annual',  -- annual, quarterly, probation, project
    
    -- Options
    include_self_review BOOLEAN NOT NULL DEFAULT true,
    include_peer_review BOOLEAN NOT NULL DEFAULT false,
    include_upward_review BOOLEAN NOT NULL DEFAULT false,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id)
);

CREATE INDEX idx_review_templates_org ON review_templates(organization_id);
CREATE INDEX idx_review_templates_active ON review_templates(organization_id, is_active) WHERE is_active = true;

CREATE TRIGGER review_templates_updated_at
    BEFORE UPDATE ON review_templates
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- REVIEW TEMPLATE SECTIONS
-- Group questions into sections (e.g., "Core Competencies", "Goals", "Growth")
-- ============================================================================
CREATE TABLE review_template_sections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID NOT NULL REFERENCES review_templates(id) ON DELETE CASCADE,
    
    -- Basic info
    title VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Settings
    is_required BOOLEAN NOT NULL DEFAULT true,
    weight DECIMAL(5,2) DEFAULT 1.0,  -- For weighted scoring
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_review_template_sections_template ON review_template_sections(template_id);

-- ============================================================================
-- REVIEW TEMPLATE QUESTIONS
-- Individual questions within sections
-- ============================================================================
CREATE TABLE review_template_questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    section_id UUID NOT NULL REFERENCES review_template_sections(id) ON DELETE CASCADE,
    
    -- Question content
    question_text TEXT NOT NULL,
    help_text TEXT,
    
    -- Question type
    question_type review_question_type NOT NULL DEFAULT 'rating',
    
    -- For multiple choice / competency
    options JSONB,  -- Array of options or competency levels
    
    -- Settings
    is_required BOOLEAN NOT NULL DEFAULT true,
    sort_order INTEGER NOT NULL DEFAULT 0,
    weight DECIMAL(5,2) DEFAULT 1.0,
    
    -- For rating questions
    min_rating INTEGER DEFAULT 1,
    max_rating INTEGER DEFAULT 5,
    rating_labels JSONB,  -- {"1": "Needs Improvement", "5": "Exceptional"}
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_review_template_questions_section ON review_template_questions(section_id);

-- ============================================================================
-- REVIEW CYCLES
-- A specific review period (e.g., "Q4 2024 Performance Reviews")
-- ============================================================================
CREATE TABLE review_cycles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    template_id UUID NOT NULL REFERENCES review_templates(id),
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Timeline
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    self_review_due DATE,
    manager_review_due DATE,
    
    -- Status
    status review_cycle_status NOT NULL DEFAULT 'draft',
    
    -- Scope
    include_all_employees BOOLEAN NOT NULL DEFAULT true,
    team_ids UUID[],  -- If not all, which teams
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    launched_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ
);

CREATE INDEX idx_review_cycles_org ON review_cycles(organization_id);
CREATE INDEX idx_review_cycles_status ON review_cycles(organization_id, status);
CREATE INDEX idx_review_cycles_dates ON review_cycles(start_date, end_date);

CREATE TRIGGER review_cycles_updated_at
    BEFORE UPDATE ON review_cycles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- REVIEWS
-- Individual review instance for a team member in a cycle
-- ============================================================================
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    cycle_id UUID NOT NULL REFERENCES review_cycles(id) ON DELETE CASCADE,
    
    -- Who is being reviewed
    reviewee_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Who is reviewing (manager)
    reviewer_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- Status
    status review_status NOT NULL DEFAULT 'not_started',
    
    -- Self review
    self_review_status review_status NOT NULL DEFAULT 'not_started',
    self_review_submitted_at TIMESTAMPTZ,
    
    -- Manager review
    manager_review_status review_status NOT NULL DEFAULT 'not_started',
    manager_review_submitted_at TIMESTAMPTZ,
    
    -- Overall
    overall_rating DECIMAL(3,2),  -- Calculated or manual overall score
    overall_comments TEXT,
    
    -- Manager's summary
    strengths TEXT,
    areas_for_improvement TEXT,
    goals_for_next_period TEXT,
    
    -- Acknowledgment
    acknowledged_at TIMESTAMPTZ,
    acknowledgment_comments TEXT,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_review_per_cycle UNIQUE (cycle_id, reviewee_team_member_id)
);

CREATE INDEX idx_reviews_org ON reviews(organization_id);
CREATE INDEX idx_reviews_cycle ON reviews(cycle_id);
CREATE INDEX idx_reviews_reviewee ON reviews(reviewee_team_member_id);
CREATE INDEX idx_reviews_reviewer ON reviews(reviewer_team_member_id);
CREATE INDEX idx_reviews_status ON reviews(cycle_id, status);

CREATE TRIGGER reviews_updated_at
    BEFORE UPDATE ON reviews
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- REVIEW RESPONSES
-- Answers to individual questions
-- ============================================================================
CREATE TABLE review_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    question_id UUID NOT NULL REFERENCES review_template_questions(id),
    
    -- Who answered (self or manager)
    responder_type VARCHAR(20) NOT NULL,  -- 'self', 'manager', 'peer'
    responder_team_member_id UUID REFERENCES team_members(id),
    
    -- Response
    rating_value INTEGER,
    text_value TEXT,
    selected_option VARCHAR(200),
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_response UNIQUE (review_id, question_id, responder_type)
);

CREATE INDEX idx_review_responses_review ON review_responses(review_id);
CREATE INDEX idx_review_responses_question ON review_responses(question_id);

CREATE TRIGGER review_responses_updated_at
    BEFORE UPDATE ON review_responses
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

SELECT 'Performance review tables created successfully' AS status;
