-- ============================================================================
-- ALTER TABLE: notes - Add Missing Columns
-- Date: 2026-01-14
-- Purpose: Add columns that exist in the C# QuickNote model but were missing
--          from the original schema design
-- ============================================================================

-- Add is_archived for archive functionality (used by QuickNotesViewModel)
ALTER TABLE notes 
ADD COLUMN IF NOT EXISTS is_archived BOOLEAN NOT NULL DEFAULT false;

COMMENT ON COLUMN notes.is_archived IS 'Whether this note is archived (hidden from main view but not deleted).';

-- Add archived_at timestamp for tracking when archived
ALTER TABLE notes 
ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ;

COMMENT ON COLUMN notes.archived_at IS 'When the note was archived. NULL if not archived.';

-- ============================================================================
-- Verification Query
-- Run this after the ALTER statements to confirm columns were added
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'notes'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 27 original + 2 new = 29 columns
-- ============================================================================
