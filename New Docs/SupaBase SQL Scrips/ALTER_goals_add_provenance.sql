-- ============================================================================
-- ALTER Script: Add provenance columns to goals table
-- Purpose: Track which agenda item/meeting a goal originated from
-- Date: 2026-01-15
-- ============================================================================

-- Add source_agenda_item_id column (FK to meeting_agenda_items)
ALTER TABLE goals
ADD COLUMN IF NOT EXISTS source_agenda_item_id UUID;

-- Add source_meeting_id column (FK to meetings, for easier queries)
ALTER TABLE goals
ADD COLUMN IF NOT EXISTS source_meeting_id UUID;

-- Add comments for documentation
COMMENT ON COLUMN goals.source_agenda_item_id IS 'The agenda item from which this goal was created. NULL if goal was created independently.';
COMMENT ON COLUMN goals.source_meeting_id IS 'The meeting from which this goal originated. Denormalized for easier queries. NULL if created independently.';

-- Create indexes for provenance queries
CREATE INDEX IF NOT EXISTS idx_goals_source_agenda_item ON goals(source_agenda_item_id) WHERE source_agenda_item_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_goals_source_meeting ON goals(source_meeting_id) WHERE source_meeting_id IS NOT NULL;

-- ============================================================================
-- Verification query (run after migration):
-- SELECT source_meeting_id IS NOT NULL as from_meeting, COUNT(*) 
-- FROM goals GROUP BY source_meeting_id IS NOT NULL;
-- ============================================================================
