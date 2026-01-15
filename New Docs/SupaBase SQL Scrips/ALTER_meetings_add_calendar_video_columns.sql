-- ============================================================================
-- ALTER Script: Add generic calendar sync and video conference columns to meetings
-- Purpose: Replace provider-specific columns with generic approach
-- Date: 2026-01-15
-- ============================================================================

-- ============================================================================
-- CALENDAR SYNC COLUMNS (generic - one provider at a time)
-- ============================================================================

-- External calendar event ID (Google event ID or Outlook event ID)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS calendar_event_id varchar(255);

-- Which calendar provider this meeting is synced to
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS calendar_provider varchar(50);

-- ETag/change token from the calendar provider (for change detection)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS calendar_etag varchar(500);

-- FK to calendar_links (which OAuth connection was used to sync)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS calendar_link_id uuid REFERENCES calendar_links(id) ON DELETE SET NULL;

-- Calendar sync status (synced, pending, error, not_synced)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS calendar_sync_status varchar(50) DEFAULT 'not_synced';

-- When the meeting was last synced with external calendar
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS last_synced_at timestamptz;

-- ============================================================================
-- VIDEO CONFERENCE COLUMNS (generic - one platform at a time)
-- ============================================================================

-- The join URL for the video meeting (Teams, Google Meet, Zoom, etc.)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS video_conference_url varchar(500);

-- Which video platform is being used
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS video_conference_provider varchar(50);

-- Provider-specific meeting ID (for API calls to update/cancel)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS video_conference_id varchar(255);

-- ============================================================================
-- DOCUMENTATION COMMENTS
-- ============================================================================

COMMENT ON COLUMN meetings.calendar_event_id IS 'External calendar event ID from Google Calendar or Outlook. Provider-agnostic.';
COMMENT ON COLUMN meetings.calendar_provider IS 'Calendar provider: google, outlook, apple. Only one sync per meeting.';
COMMENT ON COLUMN meetings.calendar_etag IS 'ETag/change token from calendar provider for efficient sync.';
COMMENT ON COLUMN meetings.calendar_link_id IS 'FK to calendar_links - which OAuth connection was used to sync this meeting.';
COMMENT ON COLUMN meetings.calendar_sync_status IS 'Sync state: synced, pending, error, not_synced.';
COMMENT ON COLUMN meetings.last_synced_at IS 'Timestamp of last successful sync with external calendar.';

COMMENT ON COLUMN meetings.video_conference_url IS 'Join URL for video meeting (Teams, Google Meet, Zoom).';
COMMENT ON COLUMN meetings.video_conference_provider IS 'Video platform: teams, google_meet, zoom, webex.';
COMMENT ON COLUMN meetings.video_conference_id IS 'Provider-specific meeting ID for API operations.';

-- ============================================================================
-- INDEXES
-- ============================================================================

CREATE INDEX IF NOT EXISTS idx_meetings_calendar_event_id ON meetings(calendar_event_id) WHERE calendar_event_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_meetings_calendar_link_id ON meetings(calendar_link_id);
CREATE INDEX IF NOT EXISTS idx_meetings_calendar_sync_status ON meetings(calendar_sync_status) WHERE calendar_sync_status != 'synced';

-- ============================================================================
-- Verification query (run after migration):
-- SELECT calendar_provider, COUNT(*) FROM meetings WHERE calendar_event_id IS NOT NULL GROUP BY calendar_provider;
-- SELECT video_conference_provider, COUNT(*) FROM meetings WHERE video_conference_url IS NOT NULL GROUP BY video_conference_provider;
-- ============================================================================
