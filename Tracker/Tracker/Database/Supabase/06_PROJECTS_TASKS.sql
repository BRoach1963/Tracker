-- ============================================================================
-- TRACKER DATABASE - PROJECTS AND TASKS
-- ============================================================================

-- ============================================================================
-- PROJECTS
-- Work initiatives that contain tasks and link to goals
-- ============================================================================
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Ownership
    owner_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Project details
    name VARCHAR(300) NOT NULL,
    description TEXT,
    color VARCHAR(7),  -- Hex color
    
    -- Timeline
    start_date DATE,
    target_end_date DATE,
    actual_end_date DATE,
    
    -- Status
    status task_status NOT NULL DEFAULT 'not_started',
    progress_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    
    -- Priority
    priority task_priority NOT NULL DEFAULT 'medium',
    
    -- Visibility
    is_team_visible BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id)
);

-- Indexes
CREATE INDEX idx_projects_org ON projects(organization_id);
CREATE INDEX idx_projects_owner ON projects(owner_team_member_id);
CREATE INDEX idx_projects_status ON projects(organization_id, status) WHERE is_deleted = false;

-- Trigger
CREATE TRIGGER projects_updated_at
    BEFORE UPDATE ON projects
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add FK from goals to projects (now that projects exists)
ALTER TABLE goals 
    ADD CONSTRAINT fk_goals_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL;

-- ============================================================================
-- PROJECT_MEMBERS
-- Team members assigned to a project
-- ============================================================================
CREATE TABLE project_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    role VARCHAR(100),  -- 'owner', 'contributor', 'reviewer'
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (project_id, team_member_id)
);

-- Index
CREATE INDEX idx_project_members_project ON project_members(project_id);
CREATE INDEX idx_project_members_member ON project_members(team_member_id);

-- ============================================================================
-- MILESTONES
-- Key deliverables within a project
-- ============================================================================
CREATE TABLE milestones (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    
    title VARCHAR(200) NOT NULL,
    description TEXT,
    target_date DATE NOT NULL,
    completed_date DATE,
    
    is_completed BOOLEAN NOT NULL DEFAULT false,
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_milestones_project ON milestones(project_id);

-- Trigger
CREATE TRIGGER milestones_updated_at
    BEFORE UPDATE ON milestones
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- TASKS
-- Individual work items
-- ============================================================================
CREATE TABLE tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Ownership
    owner_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,  -- Assigned to
    created_by_user_id UUID NOT NULL REFERENCES users(id),  -- Created by
    
    -- Parent task (for subtasks)
    parent_task_id UUID REFERENCES tasks(id) ON DELETE CASCADE,
    
    -- Links
    project_id UUID REFERENCES projects(id) ON DELETE SET NULL,
    goal_id UUID REFERENCES goals(id) ON DELETE SET NULL,
    meeting_id UUID,  -- FK added after meetings table
    
    -- Task details
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Status & Priority
    status task_status NOT NULL DEFAULT 'not_started',
    priority task_priority NOT NULL DEFAULT 'medium',
    
    -- Due date
    due_date TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
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
CREATE INDEX idx_tasks_org ON tasks(organization_id);
CREATE INDEX idx_tasks_owner ON tasks(owner_team_member_id);
CREATE INDEX idx_tasks_parent ON tasks(parent_task_id);
CREATE INDEX idx_tasks_project ON tasks(project_id);
CREATE INDEX idx_tasks_goal ON tasks(goal_id);
CREATE INDEX idx_tasks_due ON tasks(owner_team_member_id, due_date) 
    WHERE is_deleted = false AND status NOT IN ('completed', 'cancelled');
CREATE INDEX idx_tasks_status ON tasks(organization_id, status) WHERE is_deleted = false;
CREATE INDEX idx_tasks_sync ON tasks(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER tasks_updated_at
    BEFORE UPDATE ON tasks
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tasks_sync
    BEFORE UPDATE ON tasks
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- TASK_COLLECTIONS
-- Groups of tasks that can be linked to metrics
-- ============================================================================
CREATE TABLE task_collections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    name VARCHAR(200) NOT NULL,
    description TEXT,
    
    -- Dynamic query to find tasks
    query_config JSONB,  -- e.g., {"project_id": "...", "status": "completed"}
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_task_collections_org ON task_collections(organization_id);

-- Trigger
CREATE TRIGGER task_collections_updated_at
    BEFORE UPDATE ON task_collections
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- TASK_COLLECTION_ITEMS
-- Explicit task assignments to collections
-- ============================================================================
CREATE TABLE task_collection_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    collection_id UUID NOT NULL REFERENCES task_collections(id) ON DELETE CASCADE,
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (collection_id, task_id)
);

-- Index
CREATE INDEX idx_task_collection_items_collection ON task_collection_items(collection_id);
CREATE INDEX idx_task_collection_items_task ON task_collection_items(task_id);

SELECT 'Projects and tasks tables created successfully' AS status;
