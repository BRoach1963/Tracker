-- ============================================================================
-- TRACKER DATABASE - PULSE SURVEYS
-- ============================================================================
-- Team health checks, engagement surveys, and pulse surveys
-- ============================================================================

-- ============================================================================
-- ENUMS
-- ============================================================================

-- Survey status
CREATE TYPE survey_status AS ENUM (
    'draft',
    'scheduled',
    'active',
    'closed',
    'cancelled'
);

-- Survey frequency for recurring surveys
CREATE TYPE survey_frequency AS ENUM (
    'once',
    'weekly',
    'biweekly',
    'monthly',
    'quarterly'
);

-- Survey question type
CREATE TYPE survey_question_type AS ENUM (
    'rating',           -- 1-5 or 1-10 scale
    'nps',              -- Net Promoter Score (0-10)
    'text',             -- Free text
    'yes_no',           -- Boolean
    'multiple_choice',  -- Single select
    'multi_select',     -- Multiple select
    'emoji'             -- Emoji scale (😢😐😊)
);

-- ============================================================================
-- SURVEYS
-- Survey definitions
-- ============================================================================
CREATE TABLE surveys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Basic info
    title VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Type
    survey_type VARCHAR(50) NOT NULL DEFAULT 'pulse',  -- pulse, engagement, onboarding, exit, custom
    
    -- Status & scheduling
    status survey_status NOT NULL DEFAULT 'draft',
    frequency survey_frequency NOT NULL DEFAULT 'once',
    
    -- Timeline
    start_date DATE,
    end_date DATE,
    next_send_date DATE,
    
    -- Targeting
    target_all_employees BOOLEAN NOT NULL DEFAULT true,
    target_team_ids UUID[],
    target_team_member_ids UUID[],
    
    -- Settings
    is_anonymous BOOLEAN NOT NULL DEFAULT true,
    allow_comments BOOLEAN NOT NULL DEFAULT true,
    reminder_enabled BOOLEAN NOT NULL DEFAULT true,
    reminder_days_before_close INTEGER DEFAULT 2,
    
    -- Appearance
    welcome_message TEXT,
    thank_you_message TEXT,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_surveys_org ON surveys(organization_id);
CREATE INDEX idx_surveys_status ON surveys(organization_id, status);
CREATE INDEX idx_surveys_type ON surveys(organization_id, survey_type);
CREATE INDEX idx_surveys_next_send ON surveys(next_send_date) WHERE status = 'scheduled';

CREATE TRIGGER surveys_updated_at
    BEFORE UPDATE ON surveys
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- SURVEY QUESTIONS
-- Questions within a survey
-- ============================================================================
CREATE TABLE survey_questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id UUID NOT NULL REFERENCES surveys(id) ON DELETE CASCADE,
    
    -- Question content
    question_text TEXT NOT NULL,
    help_text TEXT,
    
    -- Type
    question_type survey_question_type NOT NULL DEFAULT 'rating',
    
    -- For rating questions
    min_value INTEGER DEFAULT 1,
    max_value INTEGER DEFAULT 5,
    min_label VARCHAR(100),  -- e.g., "Strongly Disagree"
    max_label VARCHAR(100),  -- e.g., "Strongly Agree"
    
    -- For choice questions
    options JSONB,  -- Array of options
    
    -- Settings
    is_required BOOLEAN NOT NULL DEFAULT true,
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Category for grouping/reporting
    category VARCHAR(100),  -- e.g., "Engagement", "Manager", "Culture"
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_survey_questions_survey ON survey_questions(survey_id);

-- ============================================================================
-- SURVEY INSTANCES
-- A specific send of a survey (for recurring surveys)
-- ============================================================================
CREATE TABLE survey_instances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id UUID NOT NULL REFERENCES surveys(id) ON DELETE CASCADE,
    
    -- Period
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- Status
    status survey_status NOT NULL DEFAULT 'active',
    sent_at TIMESTAMPTZ,
    closed_at TIMESTAMPTZ,
    
    -- Stats (denormalized for performance)
    total_recipients INTEGER DEFAULT 0,
    total_responses INTEGER DEFAULT 0,
    response_rate DECIMAL(5,2),
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_survey_instances_survey ON survey_instances(survey_id);
CREATE INDEX idx_survey_instances_period ON survey_instances(period_start, period_end);

-- ============================================================================
-- SURVEY RESPONSES
-- Individual response to a survey instance
-- ============================================================================
CREATE TABLE survey_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id UUID NOT NULL REFERENCES surveys(id) ON DELETE CASCADE,
    instance_id UUID REFERENCES survey_instances(id) ON DELETE CASCADE,
    
    -- Respondent (nullable if anonymous)
    team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- For anonymous surveys, track completion without identity
    anonymous_token UUID DEFAULT gen_random_uuid(),
    
    -- Status
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    is_complete BOOLEAN NOT NULL DEFAULT false,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_survey_responses_survey ON survey_responses(survey_id);
CREATE INDEX idx_survey_responses_instance ON survey_responses(instance_id);
CREATE INDEX idx_survey_responses_member ON survey_responses(team_member_id);
CREATE INDEX idx_survey_responses_complete ON survey_responses(survey_id, is_complete);

-- ============================================================================
-- SURVEY ANSWERS
-- Individual question answers
-- ============================================================================
CREATE TABLE survey_answers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    response_id UUID NOT NULL REFERENCES survey_responses(id) ON DELETE CASCADE,
    question_id UUID NOT NULL REFERENCES survey_questions(id) ON DELETE CASCADE,
    
    -- Answer values
    rating_value INTEGER,
    text_value TEXT,
    selected_options JSONB,  -- For multi-select
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_answer UNIQUE (response_id, question_id)
);

CREATE INDEX idx_survey_answers_response ON survey_answers(response_id);
CREATE INDEX idx_survey_answers_question ON survey_answers(question_id);

SELECT 'Survey tables created successfully' AS status;
