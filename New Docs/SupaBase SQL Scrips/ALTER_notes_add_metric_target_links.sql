-- ============================================================================
-- ALTER notes: Add linked_metric_id and linked_target_id columns
-- ============================================================================
-- Date: 2026-01-19
-- Purpose: Enable notes to be linked to metrics and targets for Goals/Metrics system
-- ============================================================================

-- Add linked_metric_id column
ALTER TABLE procohere.notes 
ADD COLUMN IF NOT EXISTS linked_metric_id UUID REFERENCES procohere.metrics(id) ON DELETE SET NULL;

-- Add linked_target_id column  
ALTER TABLE procohere.notes 
ADD COLUMN IF NOT EXISTS linked_target_id UUID REFERENCES procohere.targets(id) ON DELETE SET NULL;

-- Add indexes for the new foreign keys (improves query performance)
CREATE INDEX IF NOT EXISTS idx_notes_linked_metric_id ON procohere.notes(linked_metric_id) WHERE linked_metric_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_notes_linked_target_id ON procohere.notes(linked_target_id) WHERE linked_target_id IS NOT NULL;

-- ============================================================================
-- Verification query (run after to confirm)
-- ============================================================================
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_schema = 'procohere' AND table_name = 'notes'
-- AND column_name IN ('linked_metric_id', 'linked_target_id');
