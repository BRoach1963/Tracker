-- ============================================================================
-- TRACKER DATABASE - TEAMS AND TEAM MEMBERS
-- ============================================================================

-- ============================================================================
-- TEAMS
-- Groupings of team members (Engineering, Sales, Legal, etc.)
-- ============================================================================
CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    description TEXT,
    color VARCHAR(7),  -- Hex color for UI (#FF5733)
    
    -- Leadership
    lead_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id)
);

-- Indexes
CREATE INDEX idx_teams_org ON teams(organization_id);
CREATE INDEX idx_teams_lead ON teams(lead_user_id);
CREATE INDEX idx_teams_active ON teams(organization_id, is_active) WHERE is_active = true AND is_deleted = false;

-- Trigger
CREATE TRIGGER teams_updated_at
    BEFORE UPDATE ON teams
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add FK from user_roles to teams (now that teams exists)
ALTER TABLE user_roles 
    ADD CONSTRAINT fk_user_roles_team 
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE CASCADE;

-- ============================================================================
-- TEAM_MEMBERS
-- People being managed (employees, direct reports)
-- ============================================================================
CREATE TABLE team_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Manager (the user managing this person)
    manager_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    
    -- Link to user account (if team member also has login)
    linked_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    
    -- Personal info
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    nickname VARCHAR(50),
    email VARCHAR(255),
    phone VARCHAR(50),
    
    -- Work info
    job_title VARCHAR(200),
    department VARCHAR(200),
    hire_date DATE,
    
    -- Optional details
    birthday DATE,
    location VARCHAR(200),
    bio TEXT,
    avatar_url TEXT,
    
    -- Social links
    linkedin_url VARCHAR(500),
    
    -- Status
    employment_status employment_status NOT NULL DEFAULT 'active',
    termination_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Cached counts (updated by triggers/app)
    active_goal_count INTEGER NOT NULL DEFAULT 0,
    open_task_count INTEGER NOT NULL DEFAULT 0,
    
    -- Meeting tracking
    last_meeting_date TIMESTAMPTZ,
    next_meeting_date TIMESTAMPTZ,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id),
    
    -- Sync metadata (for future offline)
    sync_id UUID DEFAULT gen_random_uuid(),
    sync_version INTEGER DEFAULT 1,
    sync_modified_at TIMESTAMPTZ DEFAULT NOW(),
    sync_status sync_status DEFAULT 'synced'
);

-- Indexes
CREATE INDEX idx_team_members_org ON team_members(organization_id);
CREATE INDEX idx_team_members_manager ON team_members(manager_user_id);
CREATE INDEX idx_team_members_email ON team_members(email);
CREATE INDEX idx_team_members_active ON team_members(organization_id, is_active) 
    WHERE is_active = true AND is_deleted = false;
CREATE INDEX idx_team_members_sync ON team_members(sync_modified_at) 
    WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER team_members_updated_at
    BEFORE UPDATE ON team_members
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER team_members_sync
    BEFORE UPDATE ON team_members
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- Add FK from users to team_members (now that team_members exists)
ALTER TABLE users 
    ADD CONSTRAINT fk_users_linked_team_member 
    FOREIGN KEY (linked_team_member_id) REFERENCES team_members(id) ON DELETE SET NULL;

-- ============================================================================
-- TEAM_MEMBERSHIPS
-- Which team members belong to which teams (many-to-many)
-- ============================================================================
CREATE TABLE team_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Role within the team
    is_lead BOOLEAN NOT NULL DEFAULT false,
    
    -- Dates
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    left_at TIMESTAMPTZ,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    
    -- One membership per team/member combo
    UNIQUE (team_id, team_member_id)
);

-- Indexes
CREATE INDEX idx_team_memberships_team ON team_memberships(team_id);
CREATE INDEX idx_team_memberships_member ON team_memberships(team_member_id);

-- ============================================================================
-- MANAGER_HISTORY
-- Track when team members change managers
-- ============================================================================
CREATE TABLE manager_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    manager_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Period
    start_date DATE NOT NULL DEFAULT CURRENT_DATE,
    end_date DATE,  -- NULL = current manager
    
    -- Reason for change
    change_reason VARCHAR(500),
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id)
);

-- Indexes
CREATE INDEX idx_manager_history_member ON manager_history(team_member_id);
CREATE INDEX idx_manager_history_manager ON manager_history(manager_user_id);
CREATE INDEX idx_manager_history_current ON manager_history(team_member_id, end_date) 
    WHERE end_date IS NULL;

SELECT 'Teams and team members tables created successfully' AS status;
