-- ============================================================================
-- CREATE Script: risks
-- Purpose: Track risks that can be attached to projects, goals, tasks, or metrics
-- Date: 2026-01-15
-- ============================================================================

-- Create enum for risk severity levels
DO $$ BEGIN
    CREATE TYPE risk_severity AS ENUM ('low', 'medium', 'high', 'critical');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Create enum for risk status
DO $$ BEGIN
    CREATE TYPE risk_status AS ENUM ('identified', 'assessing', 'mitigating', 'monitoring', 'resolved', 'accepted');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

CREATE TABLE IF NOT EXISTS risks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Polymorphic association - what entity is this risk attached to?
    entity_type VARCHAR(50) NOT NULL,  -- 'project', 'goal', 'task', 'metric'
    entity_id UUID NOT NULL,
    
    -- Risk details
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Risk assessment
    severity risk_severity NOT NULL DEFAULT 'medium',
    probability VARCHAR(50) DEFAULT 'possible',  -- unlikely, possible, likely, almost_certain
    impact VARCHAR(50) DEFAULT 'moderate',       -- minimal, moderate, significant, severe
    
    -- Risk management
    status risk_status NOT NULL DEFAULT 'identified',
    mitigation_strategy TEXT,
    contingency_plan TEXT,
    
    -- Ownership
    owner_team_member_id UUID REFERENCES team_members(id),
    
    -- Dates
    identified_date DATE NOT NULL DEFAULT CURRENT_DATE,
    target_resolution_date DATE,
    resolved_date DATE,
    
    -- Audit columns
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id UUID REFERENCES users(id),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id)
);

-- Add comments
COMMENT ON TABLE risks IS 'Risks that can be attached to projects, goals, tasks, or metrics';
COMMENT ON COLUMN risks.entity_type IS 'Type of entity: project, goal, task, metric';
COMMENT ON COLUMN risks.entity_id IS 'UUID of the related entity';
COMMENT ON COLUMN risks.severity IS 'Overall severity: low, medium, high, critical';
COMMENT ON COLUMN risks.probability IS 'Likelihood of occurrence: unlikely, possible, likely, almost_certain';
COMMENT ON COLUMN risks.impact IS 'Impact if it occurs: minimal, moderate, significant, severe';
COMMENT ON COLUMN risks.status IS 'Current status: identified, assessing, mitigating, monitoring, resolved, accepted';

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_risks_org ON risks(organization_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_risks_entity ON risks(entity_type, entity_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_risks_severity ON risks(severity) WHERE is_deleted = false AND status != 'resolved';
CREATE INDEX IF NOT EXISTS idx_risks_owner ON risks(owner_team_member_id) WHERE is_deleted = false;

-- ============================================================================
-- Usage Examples:
-- 
-- Add risk to a project:
--   INSERT INTO risks (organization_id, entity_type, entity_id, name, severity)
--   VALUES (org_id, 'project', project_id, 'Key resource leaving', 'high');
--
-- Add risk to a goal:
--   INSERT INTO risks (organization_id, entity_type, entity_id, name, severity)
--   VALUES (org_id, 'goal', goal_id, 'Market conditions changing', 'medium');
--
-- Get all risks for a project:
--   SELECT * FROM risks 
--   WHERE entity_type = 'project' AND entity_id = project_id AND is_deleted = false;
--
-- Get all high/critical risks across organization:
--   SELECT r.*, 
--          CASE r.entity_type 
--            WHEN 'project' THEN (SELECT name FROM projects WHERE id = r.entity_id)
--            WHEN 'goal' THEN (SELECT title FROM goals WHERE id = r.entity_id)
--          END as entity_name
--   FROM risks r
--   WHERE r.organization_id = org_id 
--   AND r.severity IN ('high', 'critical')
--   AND r.status NOT IN ('resolved', 'accepted')
--   AND r.is_deleted = false;
-- ============================================================================
