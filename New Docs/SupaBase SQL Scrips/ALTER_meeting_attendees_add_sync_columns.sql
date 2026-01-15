-- ============================================================================
-- ALTER Script: Add calendar sync columns to meeting_attendees table
-- Purpose: Support per-attendee calendar sync tracking
-- Date: 2026-01-15
-- ============================================================================

-- Add external_attendee_email column (for calendar invite email override)
ALTER TABLE meeting_attendees
ADD COLUMN IF NOT EXISTS external_attendee_email varchar(255);

-- Add removed_from_calendar_at column (audit trail for calendar removal)
ALTER TABLE meeting_attendees
ADD COLUMN IF NOT EXISTS removed_from_calendar_at timestamptz;

-- Add sync_status column (per-attendee sync state tracking)
ALTER TABLE meeting_attendees
ADD COLUMN IF NOT EXISTS sync_status varchar(50) DEFAULT 'synced';

-- Add comments for documentation
COMMENT ON COLUMN meeting_attendees.external_attendee_email IS 'Email address used for external calendar invites. Overrides team_members.email if set.';
COMMENT ON COLUMN meeting_attendees.removed_from_calendar_at IS 'Timestamp when attendee removed/declined meeting from their external calendar.';
COMMENT ON COLUMN meeting_attendees.sync_status IS 'Per-attendee calendar sync status: synced, pending, out_of_sync, error.';

-- Create index for finding attendees with sync issues
CREATE INDEX IF NOT EXISTS idx_meeting_attendees_sync_status ON meeting_attendees(sync_status) WHERE sync_status != 'synced';

-- ============================================================================
-- Verification query (run after migration):
-- SELECT sync_status, COUNT(*) FROM meeting_attendees GROUP BY sync_status;
-- ============================================================================
