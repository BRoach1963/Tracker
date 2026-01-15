-- ============================================================================
-- ALTER TABLE: tasks - Add Missing Columns
-- Date: 2026-01-15
-- Purpose: Add columns that exist in the C# TrackerTask model but were 
--          missing from the original schema design
-- ============================================================================

-- Add source_agenda_item_id for tracking which agenda item created this task
ALTER TABLE tasks 
ADD COLUMN IF NOT EXISTS source_agenda_item_id UUID;

COMMENT ON COLUMN tasks.source_agenda_item_id IS 'Source agenda item that initiated this task. FK to meeting_agenda_items.';

-- Add source_meeting_id for tracking which meeting originated this task
ALTER TABLE tasks 
ADD COLUMN IF NOT EXISTS source_meeting_id UUID;

COMMENT ON COLUMN tasks.source_meeting_id IS 'Source meeting from which this task originated. FK to meetings.';

-- Add notes for additional task notes
ALTER TABLE tasks 
ADD COLUMN IF NOT EXISTS notes TEXT;

COMMENT ON COLUMN tasks.notes IS 'Additional notes about the task.';

-- ============================================================================
-- Verification Query
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'tasks'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 24 original + 3 new = 27 columns
-- ============================================================================
