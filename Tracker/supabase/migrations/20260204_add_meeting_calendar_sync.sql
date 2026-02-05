-- Migration: Add calendar sync support to meetings
-- Date: 2026-02-04
-- Description: Adds Google Calendar and Outlook integration fields to meetings table

SET search_path TO procohere;

-- Add calendar sync columns to meetings table
ALTER TABLE meetings 
    ADD COLUMN IF NOT EXISTS calendar_event_id TEXT, -- External calendar event ID
    ADD COLUMN IF NOT EXISTS calendar_provider TEXT, -- 'google', 'microsoft', 'apple'
    ADD COLUMN IF NOT EXISTS calendar_link_id UUID REFERENCES calendar_integrations(id), -- Which calendar integration was used
    ADD COLUMN IF NOT EXISTS last_synced_at TIMESTAMPTZ, -- Last successful sync timestamp
    ADD COLUMN IF NOT EXISTS sync_status TEXT; -- 'synced', 'pending', 'error', 'unsync'

-- Create index for finding meetings by calendar event ID
CREATE INDEX IF NOT EXISTS idx_meetings_calendar_event ON meetings(calendar_event_id) 
    WHERE calendar_event_id IS NOT NULL;

-- Create index for meetings that need syncing
CREATE INDEX IF NOT EXISTS idx_meetings_calendar_provider ON meetings(calendar_provider) 
    WHERE calendar_provider IS NOT NULL AND is_deleted = FALSE;

-- Add comments
COMMENT ON COLUMN meetings.calendar_event_id IS 'External calendar event ID (Google/Outlook)';
COMMENT ON COLUMN meetings.calendar_provider IS 'Calendar provider: google, microsoft, apple';
COMMENT ON COLUMN meetings.calendar_link_id IS 'FK to calendar_integrations - which OAuth connection synced this';
COMMENT ON COLUMN meetings.last_synced_at IS 'When this meeting was last synced to external calendar';
COMMENT ON COLUMN meetings.sync_status IS 'Sync status: synced, pending, error, unsynced';
