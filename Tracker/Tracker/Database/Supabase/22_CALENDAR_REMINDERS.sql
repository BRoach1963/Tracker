-- ============================================================================
-- TRACKER DATABASE - CALENDAR INTEGRATION & REMINDERS
-- ============================================================================
-- Calendar sync and reminder system
-- ============================================================================

-- ============================================================================
-- ENUMS
-- ============================================================================

-- Calendar provider
CREATE TYPE calendar_provider AS ENUM (
    'google',
    'microsoft',  -- Outlook/Teams
    'apple',
    'other'
);

-- Calendar sync status
CREATE TYPE calendar_sync_status AS ENUM (
    'pending',
    'synced',
    'failed',
    'cancelled'
);

-- Reminder type
CREATE TYPE reminder_type AS ENUM (
    'meeting',
    'task',
    'goal',
    'review',
    'survey',
    'feedback',
    'one_on_one_prep',
    'custom'
);

-- Reminder status
CREATE TYPE reminder_status AS ENUM (
    'scheduled',
    'sent',
    'dismissed',
    'snoozed',
    'cancelled'
);

-- ============================================================================
-- CALENDAR LINKS
-- User's connected calendar accounts
-- ============================================================================
CREATE TABLE calendar_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Provider
    provider calendar_provider NOT NULL,
    
    -- Account info
    account_email VARCHAR(255),
    account_name VARCHAR(200),
    
    -- Tokens (encrypted in practice)
    access_token TEXT,
    refresh_token TEXT,
    token_expires_at TIMESTAMPTZ,
    
    -- Settings
    is_active BOOLEAN NOT NULL DEFAULT true,
    sync_enabled BOOLEAN NOT NULL DEFAULT true,
    
    -- What to sync
    sync_meetings_to_calendar BOOLEAN NOT NULL DEFAULT true,
    sync_tasks_to_calendar BOOLEAN NOT NULL DEFAULT false,
    create_meeting_from_calendar BOOLEAN NOT NULL DEFAULT false,
    
    -- Default calendar (for providers with multiple calendars)
    default_calendar_id VARCHAR(255),
    default_calendar_name VARCHAR(200),
    
    -- Sync state
    last_sync_at TIMESTAMPTZ,
    last_sync_status calendar_sync_status,
    last_sync_error TEXT,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_calendar_link UNIQUE (user_id, provider, account_email)
);

CREATE INDEX idx_calendar_links_user ON calendar_links(user_id);
CREATE INDEX idx_calendar_links_active ON calendar_links(user_id, is_active) WHERE is_active = true;

CREATE TRIGGER calendar_links_updated_at
    BEFORE UPDATE ON calendar_links
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- ADD CALENDAR FIELDS TO MEETINGS
-- ============================================================================
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS calendar_event_id VARCHAR(255);
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS calendar_provider calendar_provider;
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS calendar_link_id UUID REFERENCES calendar_links(id) ON DELETE SET NULL;
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS video_conference_url TEXT;
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS video_conference_provider VARCHAR(50);  -- zoom, teams, meet, etc.
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS calendar_sync_status calendar_sync_status;
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS last_synced_at TIMESTAMPTZ;

CREATE INDEX idx_meetings_calendar_event ON meetings(calendar_event_id) WHERE calendar_event_id IS NOT NULL;

-- ============================================================================
-- REMINDERS
-- Scheduled reminders for various entities
-- ============================================================================
CREATE TABLE reminders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who gets reminded
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- What type
    reminder_type reminder_type NOT NULL,
    
    -- Reference to the entity
    entity_type VARCHAR(50) NOT NULL,  -- meeting, task, goal, etc.
    entity_id UUID NOT NULL,
    
    -- Content
    title VARCHAR(300) NOT NULL,
    message TEXT,
    
    -- Timing
    remind_at TIMESTAMPTZ NOT NULL,
    
    -- For relative reminders (e.g., "15 min before meeting")
    minutes_before INTEGER,
    
    -- Status
    status reminder_status NOT NULL DEFAULT 'scheduled',
    sent_at TIMESTAMPTZ,
    dismissed_at TIMESTAMPTZ,
    snoozed_until TIMESTAMPTZ,
    
    -- Delivery
    send_push BOOLEAN NOT NULL DEFAULT true,
    send_email BOOLEAN NOT NULL DEFAULT false,
    send_in_app BOOLEAN NOT NULL DEFAULT true,
    
    -- Recurrence
    is_recurring BOOLEAN NOT NULL DEFAULT false,
    recurrence_rule VARCHAR(200),  -- RRULE format
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_reminders_org ON reminders(organization_id);
CREATE INDEX idx_reminders_user ON reminders(user_id);
CREATE INDEX idx_reminders_scheduled ON reminders(remind_at, status) WHERE status = 'scheduled';
CREATE INDEX idx_reminders_entity ON reminders(entity_type, entity_id);

CREATE TRIGGER reminders_updated_at
    BEFORE UPDATE ON reminders
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- REMINDER TEMPLATES
-- Pre-configured reminder settings (e.g., "always remind me 15 min before 1:1s")
-- ============================================================================
CREATE TABLE reminder_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- What this preference applies to
    entity_type VARCHAR(50) NOT NULL,  -- meeting, task, goal, etc.
    sub_type VARCHAR(50),  -- one_on_one, team_meeting, etc.
    
    -- Default reminder settings
    enabled BOOLEAN NOT NULL DEFAULT true,
    default_minutes_before INTEGER NOT NULL DEFAULT 15,
    
    -- Delivery channels
    send_push BOOLEAN NOT NULL DEFAULT true,
    send_email BOOLEAN NOT NULL DEFAULT false,
    send_in_app BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT unique_reminder_pref UNIQUE (user_id, entity_type, sub_type)
);

CREATE INDEX idx_reminder_prefs_user ON reminder_preferences(user_id);

CREATE TRIGGER reminder_prefs_updated_at
    BEFORE UPDATE ON reminder_preferences
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

SELECT 'Calendar and reminder tables created successfully' AS status;
