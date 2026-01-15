-- ============================================================================
-- ALTER TABLE: projects - Add Missing Columns
-- Date: 2026-01-15
-- Purpose: Add columns that exist in the C# Project model but were 
--          missing from the original schema design
-- ============================================================================

-- Add source_agenda_item_id for tracking which agenda item created this project
ALTER TABLE projects 
ADD COLUMN IF NOT EXISTS source_agenda_item_id UUID;

COMMENT ON COLUMN projects.source_agenda_item_id IS 'Source agenda item that initiated this project. FK to meeting_agenda_items.';

-- Add source_meeting_id for tracking which meeting originated this project
ALTER TABLE projects 
ADD COLUMN IF NOT EXISTS source_meeting_id UUID;

COMMENT ON COLUMN projects.source_meeting_id IS 'Source meeting from which this project originated. FK to meetings.';

-- ============================================================================
-- Verification Query
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'projects'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 19 original + 2 new = 21 columns
-- ============================================================================
