-- ============================================================================
-- CREATE Script: project_dependencies
-- Purpose: Track dependencies between projects (Project A depends on Project B)
-- Date: 2026-01-15
-- ============================================================================

CREATE TABLE IF NOT EXISTS project_dependencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- The project that HAS the dependency (the dependent project)
    dependent_project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    
    -- The project that MUST complete first (the required/prerequisite project)
    required_project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    
    -- Dependency metadata
    dependency_type VARCHAR(50) NOT NULL DEFAULT 'finish_to_start',
    -- Types: finish_to_start (default), start_to_start, finish_to_finish, start_to_finish
    
    description TEXT,
    
    -- Is this a hard dependency (blocking) or soft (informational)?
    is_blocking BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit columns
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by_user_id UUID REFERENCES users(id),
    
    -- Prevent duplicate dependencies
    CONSTRAINT uq_project_dependency UNIQUE (dependent_project_id, required_project_id),
    
    -- Prevent self-referential dependencies
    CONSTRAINT chk_no_self_dependency CHECK (dependent_project_id != required_project_id)
);

-- Add comments
COMMENT ON TABLE project_dependencies IS 'Tracks dependencies between projects';
COMMENT ON COLUMN project_dependencies.dependent_project_id IS 'The project that depends on another project';
COMMENT ON COLUMN project_dependencies.required_project_id IS 'The project that must complete first';
COMMENT ON COLUMN project_dependencies.dependency_type IS 'Type of dependency: finish_to_start, start_to_start, finish_to_finish, start_to_finish';
COMMENT ON COLUMN project_dependencies.is_blocking IS 'If true, dependent project cannot start/finish until requirement is met';

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_project_deps_org ON project_dependencies(organization_id);
CREATE INDEX IF NOT EXISTS idx_project_deps_dependent ON project_dependencies(dependent_project_id);
CREATE INDEX IF NOT EXISTS idx_project_deps_required ON project_dependencies(required_project_id);

-- ============================================================================
-- Usage Examples:
-- 
-- Project A depends on Project B completing first:
--   INSERT INTO project_dependencies (organization_id, dependent_project_id, required_project_id)
--   VALUES (org_id, project_a_id, project_b_id);
--
-- Find all projects that Project X depends on:
--   SELECT p.* FROM projects p
--   JOIN project_dependencies pd ON pd.required_project_id = p.id
--   WHERE pd.dependent_project_id = project_x_id;
--
-- Find all projects waiting on Project Y:
--   SELECT p.* FROM projects p
--   JOIN project_dependencies pd ON pd.dependent_project_id = p.id
--   WHERE pd.required_project_id = project_y_id;
-- ============================================================================
