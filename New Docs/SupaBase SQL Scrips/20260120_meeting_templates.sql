-- ============================================================================
-- Migration: Meeting Templates
-- Date: 2026-01-20
-- Description: Adds meeting_templates and meeting_template_items tables
--              for reusable agenda structures
-- ============================================================================

-- ============================================================================
-- Create meeting_templates table
-- ============================================================================
CREATE TABLE IF NOT EXISTS procohere.meeting_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES procohere.organizations(id) ON DELETE CASCADE,
    created_by UUID NOT NULL REFERENCES procohere.team_members(id) ON DELETE CASCADE,
    
    name TEXT NOT NULL,
    description TEXT,
    
    -- Category for grouping: 'one_on_one', 'team', 'project', 'custom'
    category TEXT NOT NULL DEFAULT 'custom',
    
    -- System templates are built-in and cannot be deleted
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Shared templates are visible to all org members
    is_shared BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Add constraint for category values
ALTER TABLE procohere.meeting_templates
ADD CONSTRAINT meeting_templates_category_check
CHECK (category IN ('one_on_one', 'team', 'project', 'custom'));

-- ============================================================================
-- Create meeting_template_items table
-- ============================================================================
CREATE TABLE IF NOT EXISTS procohere.meeting_template_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID NOT NULL REFERENCES procohere.meeting_templates(id) ON DELETE CASCADE,
    
    title TEXT NOT NULL,
    description TEXT,
    
    -- Sort order for display
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Whether this item is optional when applying the template
    is_optional BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Suggested duration in minutes
    suggested_duration_minutes INTEGER,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- Indexes
-- ============================================================================

-- Templates by organization
CREATE INDEX IF NOT EXISTS idx_meeting_templates_organization 
ON procohere.meeting_templates(organization_id) 
WHERE is_deleted = FALSE;

-- Templates by creator
CREATE INDEX IF NOT EXISTS idx_meeting_templates_created_by 
ON procohere.meeting_templates(created_by) 
WHERE is_deleted = FALSE;

-- Templates by category
CREATE INDEX IF NOT EXISTS idx_meeting_templates_category 
ON procohere.meeting_templates(category) 
WHERE is_deleted = FALSE;

-- Shared templates
CREATE INDEX IF NOT EXISTS idx_meeting_templates_shared 
ON procohere.meeting_templates(organization_id) 
WHERE is_shared = TRUE AND is_deleted = FALSE;

-- Template items by template
CREATE INDEX IF NOT EXISTS idx_meeting_template_items_template 
ON procohere.meeting_template_items(template_id);

-- ============================================================================
-- Row Level Security (RLS)
-- ============================================================================

ALTER TABLE procohere.meeting_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE procohere.meeting_template_items ENABLE ROW LEVEL SECURITY;

-- Templates: Users can see their own templates and shared templates in their org
CREATE POLICY "Users can view own and shared templates"
ON procohere.meeting_templates
FOR SELECT
USING (
    created_by = procohere.get_current_team_member_id()
    OR (
        is_shared = TRUE 
        AND organization_id = procohere.get_current_organization_id()
    )
);

-- Templates: Users can create templates
CREATE POLICY "Users can create templates"
ON procohere.meeting_templates
FOR INSERT
WITH CHECK (
    created_by = procohere.get_current_team_member_id()
    AND organization_id = procohere.get_current_organization_id()
);

-- Templates: Users can update their own non-system templates
CREATE POLICY "Users can update own templates"
ON procohere.meeting_templates
FOR UPDATE
USING (
    created_by = procohere.get_current_team_member_id()
    AND is_system = FALSE
);

-- Templates: Users can delete their own non-system templates
CREATE POLICY "Users can delete own templates"
ON procohere.meeting_templates
FOR DELETE
USING (
    created_by = procohere.get_current_team_member_id()
    AND is_system = FALSE
);

-- Template items: Users can view items for templates they can see
CREATE POLICY "Users can view template items"
ON procohere.meeting_template_items
FOR SELECT
USING (
    EXISTS (
        SELECT 1 FROM procohere.meeting_templates t
        WHERE t.id = template_id
        AND (
            t.created_by = procohere.get_current_team_member_id()
            OR (t.is_shared = TRUE AND t.organization_id = procohere.get_current_organization_id())
        )
    )
);

-- Template items: Users can manage items for their own templates
CREATE POLICY "Users can manage template items"
ON procohere.meeting_template_items
FOR ALL
USING (
    EXISTS (
        SELECT 1 FROM procohere.meeting_templates t
        WHERE t.id = template_id
        AND t.created_by = procohere.get_current_team_member_id()
        AND t.is_system = FALSE
    )
);

-- ============================================================================
-- Trigger for updated_at
-- ============================================================================

CREATE TRIGGER update_meeting_templates_updated_at
    BEFORE UPDATE ON procohere.meeting_templates
    FOR EACH ROW
    EXECUTE FUNCTION procohere.update_updated_at_column();

-- ============================================================================
-- Comments
-- ============================================================================

COMMENT ON TABLE procohere.meeting_templates IS 
'Reusable meeting agenda templates for common meeting types';

COMMENT ON TABLE procohere.meeting_template_items IS 
'Individual agenda items within a meeting template';

COMMENT ON COLUMN procohere.meeting_templates.category IS 
'Template category: one_on_one, team, project, or custom';

COMMENT ON COLUMN procohere.meeting_templates.is_system IS 
'System templates are built-in and cannot be deleted by users';

COMMENT ON COLUMN procohere.meeting_templates.is_shared IS 
'Shared templates are visible to all members of the organization';

COMMENT ON COLUMN procohere.meeting_template_items.is_optional IS 
'Optional items may be skipped when applying the template';

COMMENT ON COLUMN procohere.meeting_template_items.suggested_duration_minutes IS 
'Suggested time allocation for this agenda item';
