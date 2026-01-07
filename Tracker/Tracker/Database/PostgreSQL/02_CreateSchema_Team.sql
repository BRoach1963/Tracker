/*
 * TRACKER DATABASE - TEAM SCHEMA
 * PostgreSQL Edition
 * 
 * Creates the team management tables:
 * - team_members: Employees/direct reports being managed
 * - manager_history: Tracks manager assignments over time
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- TEAM_MEMBERS TABLE
-- =============================================================================
-- The core entity representing employees/team members being managed.
-- Each team member belongs to one organization and has a current manager (user).

CREATE TABLE IF NOT EXISTS team_members (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope (required for RLS)
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Current manager (the user managing this team member)
    current_manager_user_id UUID REFERENCES users(id),
    
    -- Basic info
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    nick_name VARCHAR(50),
    email VARCHAR(200),
    cell_phone VARCHAR(20),
    job_title VARCHAR(100),
    hire_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Meeting tracking
    last_one_on_one_date TIMESTAMPTZ,
    one_on_one_cadence INT NOT NULL DEFAULT 14,  -- Days between meetings
    
    -- Computed/cached fields
    open_task_count INT NOT NULL DEFAULT 0,
    
    -- Social profiles
    linked_in_profile VARCHAR(500),
    facebook_profile VARCHAR(500),
    instagram_profile VARCHAR(500),
    x_profile VARCHAR(500),
    
    -- Notes (rich text)
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

-- Indexes for team_members
CREATE INDEX IF NOT EXISTS ix_team_members_organization_id 
    ON team_members(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_members_manager 
    ON team_members(current_manager_user_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_members_is_active 
    ON team_members(organization_id, is_active) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_members_name 
    ON team_members(organization_id, last_name, first_name) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_members_email 
    ON team_members(organization_id, email) WHERE email IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_members_one_on_one 
    ON team_members(organization_id, last_one_on_one_date, one_on_one_cadence) 
    WHERE is_active AND NOT is_deleted;

-- Full-text search index on names
CREATE INDEX IF NOT EXISTS ix_team_members_name_search 
    ON team_members USING gin(
        (coalesce(first_name, '') || ' ' || coalesce(last_name, '') || ' ' || coalesce(nick_name, '')) 
        gin_trgm_ops
    ) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_team_members_modified_at ON team_members;
CREATE TRIGGER trg_team_members_modified_at
    BEFORE UPDATE ON team_members
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE team_members IS 'Employees/direct reports being managed within an organization';
COMMENT ON COLUMN team_members.organization_id IS 'The organization this team member belongs to';
COMMENT ON COLUMN team_members.current_manager_user_id IS 'The user currently managing this team member';
COMMENT ON COLUMN team_members.one_on_one_cadence IS 'Target days between one-on-one meetings';
COMMENT ON COLUMN team_members.open_task_count IS 'Cached count of open tasks for dashboard performance';

-- =============================================================================
-- MANAGER_HISTORY TABLE
-- =============================================================================
-- Tracks manager assignment changes over time for compliance and reporting.
-- When a team member's manager changes, the old record is ended and a new one begins.

CREATE TABLE IF NOT EXISTS manager_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope (required for RLS)
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- The team member whose manager changed
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    
    -- The manager during this period
    manager_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Period of management
    start_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_date TIMESTAMPTZ,  -- NULL means current manager
    
    -- Reason for change
    change_reason VARCHAR(500),
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100)
);

-- Indexes for manager_history
CREATE INDEX IF NOT EXISTS ix_manager_history_organization_id 
    ON manager_history(organization_id);
CREATE INDEX IF NOT EXISTS ix_manager_history_team_member 
    ON manager_history(team_member_id, start_date DESC);
CREATE INDEX IF NOT EXISTS ix_manager_history_manager 
    ON manager_history(manager_user_id, start_date DESC);
CREATE INDEX IF NOT EXISTS ix_manager_history_current 
    ON manager_history(team_member_id) WHERE end_date IS NULL;
CREATE INDEX IF NOT EXISTS ix_manager_history_date_range 
    ON manager_history(organization_id, start_date, end_date);

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_manager_history_modified_at ON manager_history;
CREATE TRIGGER trg_manager_history_modified_at
    BEFORE UPDATE ON manager_history
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE manager_history IS 'Audit trail of manager assignments for compliance reporting';
COMMENT ON COLUMN manager_history.end_date IS 'NULL indicates this is the current manager';
COMMENT ON COLUMN manager_history.change_reason IS 'Optional reason for the manager change';

-- =============================================================================
-- TRIGGER: Auto-create manager history on team member manager change
-- =============================================================================

CREATE OR REPLACE FUNCTION handle_manager_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only process if manager actually changed
    IF OLD.current_manager_user_id IS DISTINCT FROM NEW.current_manager_user_id THEN
        -- End the previous manager's period
        IF OLD.current_manager_user_id IS NOT NULL THEN
            UPDATE manager_history
            SET end_date = CURRENT_TIMESTAMP,
                modified_at = CURRENT_TIMESTAMP
            WHERE team_member_id = NEW.id
              AND end_date IS NULL
              AND manager_user_id = OLD.current_manager_user_id;
        END IF;
        
        -- Start a new period with the new manager
        IF NEW.current_manager_user_id IS NOT NULL THEN
            INSERT INTO manager_history (
                organization_id,
                team_member_id,
                manager_user_id,
                start_date,
                created_by
            ) VALUES (
                NEW.organization_id,
                NEW.id,
                NEW.current_manager_user_id,
                CURRENT_TIMESTAMP,
                NEW.modified_by
            );
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_team_members_manager_change ON team_members;
CREATE TRIGGER trg_team_members_manager_change
    AFTER UPDATE OF current_manager_user_id ON team_members
    FOR EACH ROW
    EXECUTE FUNCTION handle_manager_change();

-- =============================================================================
-- PERSONAL_DETAILS TABLE
-- =============================================================================
-- Extended personal information for team members (family, interests, etc.)

CREATE TABLE IF NOT EXISTS personal_details (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Link to team member
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    
    -- Personal info
    date_of_birth DATE,
    anniversary DATE,
    hometown VARCHAR(200),
    current_city VARCHAR(200),
    
    -- Family
    spouse_partner_name VARCHAR(200),
    children_info TEXT,  -- JSON array or free text
    
    -- Interests
    hobbies TEXT,
    favorite_foods TEXT,
    dietary_restrictions VARCHAR(500),
    
    -- Work preferences
    communication_style VARCHAR(100),
    best_contact_time VARCHAR(100),
    
    -- Emergency contact
    emergency_contact_name VARCHAR(200),
    emergency_contact_phone VARCHAR(50),
    emergency_contact_relationship VARCHAR(100),
    
    -- Free-form notes
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

-- Indexes for personal_details
CREATE INDEX IF NOT EXISTS ix_personal_details_organization_id 
    ON personal_details(organization_id) WHERE NOT is_deleted;
CREATE UNIQUE INDEX IF NOT EXISTS ix_personal_details_team_member 
    ON personal_details(team_member_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_personal_details_birthday 
    ON personal_details(organization_id, EXTRACT(MONTH FROM date_of_birth), EXTRACT(DAY FROM date_of_birth))
    WHERE date_of_birth IS NOT NULL AND NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_personal_details_modified_at ON personal_details;
CREATE TRIGGER trg_personal_details_modified_at
    BEFORE UPDATE ON personal_details
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE personal_details IS 'Extended personal info for team members (family, interests, etc.)';

-- =============================================================================
-- TEAMS TABLE
-- =============================================================================
-- Optional grouping of team members into teams/departments

CREATE TABLE IF NOT EXISTS teams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Team lead (optional)
    lead_user_id UUID REFERENCES users(id),
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Team membership (many-to-many)
CREATE TABLE IF NOT EXISTS team_memberships (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    team_id UUID NOT NULL REFERENCES teams(id),
    team_member_id UUID NOT NULL REFERENCES team_members(id),
    joined_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_primary BOOLEAN NOT NULL DEFAULT false,  -- Primary team for the member
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    
    -- Constraints
    CONSTRAINT uq_team_membership UNIQUE (team_id, team_member_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_teams_organization_id 
    ON teams(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_team_memberships_team 
    ON team_memberships(team_id);
CREATE INDEX IF NOT EXISTS ix_team_memberships_member 
    ON team_memberships(team_member_id);

-- Triggers for modified_at
DROP TRIGGER IF EXISTS trg_teams_modified_at ON teams;
CREATE TRIGGER trg_teams_modified_at
    BEFORE UPDATE ON teams
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE teams IS 'Optional groupings of team members into teams/departments';
COMMENT ON TABLE team_memberships IS 'Many-to-many relationship between teams and team members';

\echo 'Team schema (team_members, manager_history, personal_details, teams) created successfully.'
