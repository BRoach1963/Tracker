-- ============================================================================
-- ALTER TABLE: calendar_links - Add Missing Columns
-- Date: 2026-01-15
-- Purpose: Add sync_token column for incremental calendar synchronization
-- ============================================================================

-- Add sync_token for delta sync support
-- Google Calendar: syncToken from Events.list response
-- Outlook: deltaLink from delta query
-- Without this, every sync would be a full sync (wasteful)
ALTER TABLE calendar_links 
ADD COLUMN IF NOT EXISTS sync_token TEXT;

COMMENT ON COLUMN calendar_links.sync_token IS 'Delta sync token from calendar provider for incremental synchronization. Enables fetching only changed events.';

-- ============================================================================
-- Verification Query
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'calendar_links'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 20 original + 1 new = 21 columns
-- ============================================================================
