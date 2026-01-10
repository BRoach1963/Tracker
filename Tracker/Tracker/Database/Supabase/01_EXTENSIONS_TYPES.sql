-- ============================================================================
-- TRACKER DATABASE - EXTENSIONS AND TYPES
-- Run this SECOND after dropping all tables
-- ============================================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";      -- For uuid_generate_v4()
CREATE EXTENSION IF NOT EXISTS "pgcrypto";       -- For gen_random_uuid(), crypt()
CREATE EXTENSION IF NOT EXISTS "vector";         -- For AI embeddings (pgvector)

-- ============================================================================
-- DROP EXISTING TYPES (if re-running)
-- ============================================================================
DROP TYPE IF EXISTS goal_status CASCADE;
DROP TYPE IF EXISTS goal_time_period CASCADE;
DROP TYPE IF EXISTS metric_target_direction CASCADE;
DROP TYPE IF EXISTS metric_frequency CASCADE;
DROP TYPE IF EXISTS task_priority CASCADE;
DROP TYPE IF EXISTS task_status CASCADE;
DROP TYPE IF EXISTS meeting_type CASCADE;
DROP TYPE IF EXISTS meeting_status CASCADE;
DROP TYPE IF EXISTS feedback_type CASCADE;
DROP TYPE IF EXISTS feedback_sentiment CASCADE;
DROP TYPE IF EXISTS note_category CASCADE;
DROP TYPE IF EXISTS employment_status CASCADE;
DROP TYPE IF EXISTS sync_status CASCADE;

-- ============================================================================
-- CUSTOM ENUM TYPES
-- ============================================================================

-- Goal (was OKR) related enums
CREATE TYPE goal_status AS ENUM (
    'not_started',
    'on_track', 
    'at_risk',
    'off_track',
    'completed',
    'cancelled'
);

CREATE TYPE goal_time_period AS ENUM (
    'q1', 'q2', 'q3', 'q4',
    'annual',
    'custom'
);

-- Metric (was KPI) related enums
CREATE TYPE metric_target_direction AS ENUM (
    'higher_is_better',
    'lower_is_better',
    'target_value'
);

CREATE TYPE metric_frequency AS ENUM (
    'daily',
    'weekly',
    'monthly',
    'quarterly',
    'annually'
);

-- Task related enums
CREATE TYPE task_priority AS ENUM (
    'low',
    'medium', 
    'high',
    'urgent'
);

CREATE TYPE task_status AS ENUM (
    'not_started',
    'in_progress',
    'blocked',
    'completed',
    'cancelled'
);

-- Meeting related enums
CREATE TYPE meeting_type AS ENUM (
    'one_on_one',
    'team_meeting',
    'all_hands',
    'project',
    'interview',
    'other'
);

CREATE TYPE meeting_status AS ENUM (
    'scheduled',
    'in_progress',
    'completed',
    'cancelled',
    'rescheduled'
);

-- Feedback related enums
CREATE TYPE feedback_type AS ENUM (
    'praise',
    'coaching',
    'collaboration',
    'general'
);

CREATE TYPE feedback_sentiment AS ENUM (
    'positive',
    'neutral',
    'constructive'
);

-- Note category enum
CREATE TYPE note_category AS ENUM (
    'general',
    'meeting',
    'goal',
    'metric',
    'task',
    'team_member',
    'project',
    'idea',
    'follow_up'
);

-- Employment status
CREATE TYPE employment_status AS ENUM (
    'active',
    'on_leave',
    'terminated',
    'contractor'
);

-- Sync status (for future offline support)
CREATE TYPE sync_status AS ENUM (
    'synced',
    'pending',
    'conflict'
);

-- ============================================================================
-- HELPER FUNCTIONS
-- ============================================================================

-- Trigger function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger function to update sync metadata
CREATE OR REPLACE FUNCTION update_sync_metadata()
RETURNS TRIGGER AS $$
BEGIN
    NEW.sync_version = COALESCE(OLD.sync_version, 0) + 1;
    NEW.sync_modified_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function to get current user's organization (for RLS)
CREATE OR REPLACE FUNCTION current_org_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_org_id', true), '')::UUID;
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to get current user ID (for RLS)
CREATE OR REPLACE FUNCTION current_app_user_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', true), '')::UUID;
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

SELECT 'Extensions and types created successfully' AS status;
