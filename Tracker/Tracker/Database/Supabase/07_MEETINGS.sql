-- ============================================================================
-- TRACKER DATABASE - MEETINGS AND 1:1s
-- ============================================================================

-- ============================================================================
-- MEETINGS
-- Scheduled meetings including 1:1s, team meetings, etc.
-- ============================================================================
CREATE TABLE meetings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who created it
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Meeting type
    meeting_type meeting_type NOT NULL DEFAULT 'one_on_one',
    
    -- For 1:1s
    manager_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    report_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- For team meetings
    team_id UUID REFERENCES teams(id) ON DELETE SET NULL,
    
    -- Meeting details
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Scheduling
    scheduled_at TIMESTAMPTZ,
    duration_minutes INTEGER NOT NULL DEFAULT 30,
    recurrence_rule VARCHAR(200),  -- iCal RRULE format
    
    -- Location
    location VARCHAR(500),  -- Room name or URL
    
    -- Status
    status meeting_status NOT NULL DEFAULT 'scheduled',
    started_at TIMESTAMPTZ,
    ended_at TIMESTAMPTZ,
    
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
CREATE INDEX idx_meetings_org ON meetings(organization_id);
CREATE INDEX idx_meetings_manager ON meetings(manager_team_member_id);
CREATE INDEX idx_meetings_report ON meetings(report_team_member_id);
CREATE INDEX idx_meetings_team ON meetings(team_id);
CREATE INDEX idx_meetings_scheduled ON meetings(organization_id, scheduled_at) 
    WHERE is_deleted = false;
CREATE INDEX idx_meetings_1on1 ON meetings(manager_team_member_id, report_team_member_id) 
    WHERE meeting_type = 'one_on_one' AND is_deleted = false;
CREATE INDEX idx_meetings_sync ON meetings(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER meetings_updated_at
    BEFORE UPDATE ON meetings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER meetings_sync
    BEFORE UPDATE ON meetings
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- Add FK from tasks to meetings (now that meetings exists)
ALTER TABLE tasks
    ADD CONSTRAINT fk_tasks_meeting
    FOREIGN KEY (meeting_id) REFERENCES meetings(id) ON DELETE SET NULL;

-- ============================================================================
-- MEETING_ATTENDEES
-- Participants in a meeting (for non-1:1 meetings)
-- ============================================================================
CREATE TABLE meeting_attendees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    meeting_id UUID NOT NULL REFERENCES meetings(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Response
    response VARCHAR(50) DEFAULT 'pending',  -- pending, accepted, declined, tentative
    response_at TIMESTAMPTZ,
    
    -- Did they attend?
    attended BOOLEAN,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (meeting_id, team_member_id)
);

-- Index
CREATE INDEX idx_meeting_attendees_meeting ON meeting_attendees(meeting_id);
CREATE INDEX idx_meeting_attendees_member ON meeting_attendees(team_member_id);

-- ============================================================================
-- MEETING_AGENDA_ITEMS
-- Agenda items for a meeting
-- ============================================================================
CREATE TABLE meeting_agenda_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(id) ON DELETE CASCADE,
    
    -- Who added it
    added_by_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- Item details
    title VARCHAR(300) NOT NULL,
    notes TEXT,
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Status
    is_discussed BOOLEAN NOT NULL DEFAULT false,
    discussed_at TIMESTAMPTZ,
    
    -- Time tracking
    time_estimate_minutes INTEGER,
    actual_duration_minutes INTEGER,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_meeting_agenda_meeting ON meeting_agenda_items(meeting_id);

-- Trigger
CREATE TRIGGER meeting_agenda_items_updated_at
    BEFORE UPDATE ON meeting_agenda_items
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- MEETING_NOTES
-- Notes from meetings
-- ============================================================================
CREATE TABLE meeting_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(id) ON DELETE CASCADE,
    
    -- Who wrote it
    author_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- Note content
    content TEXT NOT NULL,
    
    -- Is this private (only visible to author)?
    is_private BOOLEAN NOT NULL DEFAULT false,
    
    -- AI summary
    ai_summary TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Sync metadata
    sync_id UUID DEFAULT gen_random_uuid(),
    sync_version INTEGER DEFAULT 1,
    sync_modified_at TIMESTAMPTZ DEFAULT NOW(),
    sync_status sync_status DEFAULT 'synced'
);

-- Index
CREATE INDEX idx_meeting_notes_meeting ON meeting_notes(meeting_id);
CREATE INDEX idx_meeting_notes_author ON meeting_notes(author_team_member_id);
CREATE INDEX idx_meeting_notes_sync ON meeting_notes(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER meeting_notes_updated_at
    BEFORE UPDATE ON meeting_notes
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER meeting_notes_sync
    BEFORE UPDATE ON meeting_notes
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- ACTION_ITEMS
-- Action items from meetings (similar to tasks but simpler)
-- ============================================================================
CREATE TABLE action_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(id) ON DELETE CASCADE,
    
    -- Assignee
    assignee_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- Item details
    title VARCHAR(300) NOT NULL,
    description TEXT,
    
    -- Due date
    due_date DATE,
    
    -- Status
    is_completed BOOLEAN NOT NULL DEFAULT false,
    completed_at TIMESTAMPTZ,
    
    -- Convert to task?
    converted_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_action_items_meeting ON action_items(meeting_id);
CREATE INDEX idx_action_items_assignee ON action_items(assignee_team_member_id);
CREATE INDEX idx_action_items_pending ON action_items(assignee_team_member_id, due_date) 
    WHERE is_completed = false;

-- Trigger
CREATE TRIGGER action_items_updated_at
    BEFORE UPDATE ON action_items
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- TALKING_POINTS
-- Recurring talking points for 1:1s
-- ============================================================================
CREATE TABLE talking_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- For which 1:1 relationship (manager + report)
    manager_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    report_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Who added it
    added_by_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- Content
    title VARCHAR(300) NOT NULL,
    notes TEXT,
    
    -- Category
    category VARCHAR(100),  -- career, feedback, project, personal, etc.
    
    -- Is this a recurring topic?
    is_recurring BOOLEAN NOT NULL DEFAULT false,
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_discussed_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index
CREATE INDEX idx_talking_points_relationship ON talking_points(manager_team_member_id, report_team_member_id);
CREATE INDEX idx_talking_points_active ON talking_points(manager_team_member_id, report_team_member_id) 
    WHERE is_active = true;

-- Trigger
CREATE TRIGGER talking_points_updated_at
    BEFORE UPDATE ON talking_points
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

SELECT 'Meetings tables created successfully' AS status;
