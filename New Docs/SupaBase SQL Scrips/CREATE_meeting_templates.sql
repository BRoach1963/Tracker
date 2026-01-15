-- ============================================================================
-- CREATE Script: Meeting Templates tables
-- Purpose: Store reusable meeting templates with pre-defined agenda items
-- Date: 2026-01-15
-- ============================================================================

-- Create meeting_templates table
CREATE TABLE IF NOT EXISTS meeting_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Template metadata
    name VARCHAR(200) NOT NULL,
    description TEXT,
    meeting_type VARCHAR(50) NOT NULL DEFAULT 'one_on_one',
    suggested_duration_minutes INT NOT NULL DEFAULT 30,
    
    -- Display/sorting
    sort_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit columns
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES users(id)
);

-- Create meeting_template_items table
CREATE TABLE IF NOT EXISTS meeting_template_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID NOT NULL REFERENCES meeting_templates(id) ON DELETE CASCADE,
    
    -- Agenda item content
    title VARCHAR(300) NOT NULL,
    notes TEXT,
    time_estimate_minutes INT,
    
    -- Display/sorting
    sort_order INT NOT NULL DEFAULT 0,
    
    -- Audit columns
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Add comments
COMMENT ON TABLE meeting_templates IS 'Reusable meeting templates with pre-defined agenda items';
COMMENT ON TABLE meeting_template_items IS 'Agenda items belonging to a meeting template';

COMMENT ON COLUMN meeting_templates.meeting_type IS 'Type of meeting this template is for: one_on_one, team_meeting, all_hands, project, interview, other';
COMMENT ON COLUMN meeting_templates.is_active IS 'Whether this template is available for use';
COMMENT ON COLUMN meeting_template_items.template_id IS 'Parent template. CASCADE delete removes items when template is deleted.';

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_meeting_templates_org ON meeting_templates(organization_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_meeting_templates_type ON meeting_templates(meeting_type) WHERE is_deleted = false AND is_active = true;
CREATE INDEX IF NOT EXISTS idx_meeting_template_items_template ON meeting_template_items(template_id);

-- ============================================================================
-- Usage:
-- 
-- "Save as Template" from meeting:
--   INSERT INTO meeting_templates (organization_id, created_by_user_id, name, meeting_type, ...)
--   INSERT INTO meeting_template_items (template_id, title, notes, sort_order, ...)
--     -- Copy from meeting_agenda_items
--
-- "Create Meeting from Template":
--   SELECT * FROM meeting_template_items WHERE template_id = ?
--   INSERT INTO meeting_agenda_items (meeting_id, title, notes, sort_order, ...)
--     -- Copy template items to new meeting
-- ============================================================================
