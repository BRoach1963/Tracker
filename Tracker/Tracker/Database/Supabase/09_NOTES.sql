-- ============================================================================
-- TRACKER DATABASE - NOTES AND QUICK CAPTURES
-- ============================================================================

-- ============================================================================
-- NOTES
-- Free-form notes that can be linked to various entities
-- ============================================================================
CREATE TABLE notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Author
    author_team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Note content
    title VARCHAR(300),
    content TEXT NOT NULL,
    
    -- Rich text format
    content_format VARCHAR(50) NOT NULL DEFAULT 'plain',  -- plain, markdown, html
    
    -- Links to other entities (all optional)
    linked_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    linked_meeting_id UUID REFERENCES meetings(id) ON DELETE SET NULL,
    linked_project_id UUID REFERENCES projects(id) ON DELETE SET NULL,
    linked_goal_id UUID REFERENCES goals(id) ON DELETE SET NULL,
    linked_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    
    -- Categorization
    category VARCHAR(100),
    tags JSONB,  -- Array of tag strings
    
    -- Privacy
    is_private BOOLEAN NOT NULL DEFAULT true,  -- Only author can see
    
    -- Pinned/Favorite
    is_pinned BOOLEAN NOT NULL DEFAULT false,
    pinned_at TIMESTAMPTZ,
    
    -- AI enhancements
    ai_summary TEXT,
    ai_suggested_actions JSONB,
    
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
CREATE INDEX idx_notes_org ON notes(organization_id);
CREATE INDEX idx_notes_author ON notes(author_team_member_id);
CREATE INDEX idx_notes_person ON notes(linked_team_member_id) WHERE is_deleted = false;
CREATE INDEX idx_notes_meeting ON notes(linked_meeting_id) WHERE is_deleted = false;
CREATE INDEX idx_notes_project ON notes(linked_project_id) WHERE is_deleted = false;
CREATE INDEX idx_notes_recent ON notes(author_team_member_id, updated_at DESC) WHERE is_deleted = false;
CREATE INDEX idx_notes_pinned ON notes(author_team_member_id) WHERE is_pinned = true AND is_deleted = false;
CREATE INDEX idx_notes_sync ON notes(sync_modified_at) WHERE sync_status != 'synced';
CREATE INDEX idx_notes_tags ON notes USING gin(tags);

-- Triggers
CREATE TRIGGER notes_updated_at
    BEFORE UPDATE ON notes
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER notes_sync
    BEFORE UPDATE ON notes
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

-- ============================================================================
-- NOTE_TEMPLATES
-- Reusable note templates
-- ============================================================================
CREATE TABLE note_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Creator
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Template details
    name VARCHAR(200) NOT NULL,
    description TEXT,
    content_template TEXT NOT NULL,
    
    -- For what context?
    template_type VARCHAR(100) NOT NULL,  -- meeting, one_on_one, project, person, general
    
    -- Is this org-wide or personal?
    is_personal BOOLEAN NOT NULL DEFAULT true,
    
    -- Ordering
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_note_templates_org ON note_templates(organization_id);
CREATE INDEX idx_note_templates_type ON note_templates(organization_id, template_type) 
    WHERE is_deleted = false AND is_personal = false;
CREATE INDEX idx_note_templates_personal ON note_templates(created_by_user_id, template_type) 
    WHERE is_deleted = false AND is_personal = true;

-- Trigger
CREATE TRIGGER note_templates_updated_at
    BEFORE UPDATE ON note_templates
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- JOURNAL_ENTRIES
-- Daily/weekly journal entries for self-reflection
-- ============================================================================
CREATE TABLE journal_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Author
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Date
    entry_date DATE NOT NULL,
    
    -- Content
    content TEXT NOT NULL,
    
    -- Mood tracking
    mood_rating INTEGER,  -- 1-5 scale
    energy_level INTEGER,  -- 1-5 scale
    
    -- Highlights and lowlights
    wins JSONB,  -- Array of wins for the day/week
    challenges JSONB,  -- Array of challenges
    
    -- Gratitude
    grateful_for JSONB,  -- Array of things grateful for
    
    -- Goals reflection
    progress_on_goals TEXT,
    
    -- AI insights
    ai_insights TEXT,
    
    -- Always private
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Sync metadata
    sync_id UUID DEFAULT gen_random_uuid(),
    sync_version INTEGER DEFAULT 1,
    sync_modified_at TIMESTAMPTZ DEFAULT NOW(),
    sync_status sync_status DEFAULT 'synced',
    
    -- One entry per date per person
    UNIQUE (team_member_id, entry_date)
);

-- Indexes
CREATE INDEX idx_journal_entries_member ON journal_entries(team_member_id);
CREATE INDEX idx_journal_entries_date ON journal_entries(team_member_id, entry_date DESC);
CREATE INDEX idx_journal_entries_sync ON journal_entries(sync_modified_at) WHERE sync_status != 'synced';

-- Triggers
CREATE TRIGGER journal_entries_updated_at
    BEFORE UPDATE ON journal_entries
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER journal_entries_sync
    BEFORE UPDATE ON journal_entries
    FOR EACH ROW
    EXECUTE FUNCTION update_sync_metadata();

SELECT 'Notes tables created successfully' AS status;
