-- ============================================================================
-- TRACKER DATABASE - ACTIVITY LOG AND NOTIFICATIONS
-- ============================================================================

-- ============================================================================
-- ACTIVITY_LOG
-- Audit trail of all important actions
-- ============================================================================
CREATE TABLE activity_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Who did it
    actor_user_id UUID NOT NULL REFERENCES users(id),
    actor_team_member_id UUID REFERENCES team_members(id) ON DELETE SET NULL,
    
    -- What happened
    action VARCHAR(100) NOT NULL,  -- created, updated, deleted, assigned, completed, etc.
    entity_type VARCHAR(50) NOT NULL,  -- task, goal, feedback, meeting, etc.
    entity_id UUID NOT NULL,
    entity_name VARCHAR(300),  -- Snapshot of name for display
    
    -- Change details
    old_values JSONB,
    new_values JSONB,
    
    -- Context
    context_type VARCHAR(50),  -- What triggered this? bulk_update, api, ui, automation
    ip_address INET,
    user_agent TEXT,
    
    -- Timestamp
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_activity_log_org ON activity_log(organization_id);
CREATE INDEX idx_activity_log_actor ON activity_log(actor_user_id);
CREATE INDEX idx_activity_log_entity ON activity_log(entity_type, entity_id);
CREATE INDEX idx_activity_log_recent ON activity_log(organization_id, created_at DESC);
CREATE INDEX idx_activity_log_action ON activity_log(organization_id, action, entity_type);

-- Partition by month for large datasets (optional - run manually if needed)
-- CREATE TABLE activity_log_y2024m01 PARTITION OF activity_log 
--     FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

-- ============================================================================
-- NOTIFICATIONS
-- In-app notifications for users
-- ============================================================================
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Recipient
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Notification details
    notification_type VARCHAR(100) NOT NULL,  -- task_assigned, feedback_received, etc.
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    
    -- Link to relevant entity
    entity_type VARCHAR(50),
    entity_id UUID,
    action_url VARCHAR(500),  -- Deep link in app
    
    -- Priority
    priority VARCHAR(20) NOT NULL DEFAULT 'normal',  -- low, normal, high, urgent
    
    -- Status
    is_read BOOLEAN NOT NULL DEFAULT false,
    read_at TIMESTAMPTZ,
    
    is_dismissed BOOLEAN NOT NULL DEFAULT false,
    dismissed_at TIMESTAMPTZ,
    
    -- Email status (if notification was also emailed)
    email_sent BOOLEAN NOT NULL DEFAULT false,
    email_sent_at TIMESTAMPTZ,
    
    -- Expiry (some notifications expire)
    expires_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_unread ON notifications(user_id) 
    WHERE is_read = false AND is_dismissed = false;
CREATE INDEX idx_notifications_recent ON notifications(user_id, created_at DESC);
CREATE INDEX idx_notifications_type ON notifications(user_id, notification_type);

-- ============================================================================
-- NOTIFICATION_PREFERENCES
-- User preferences for notifications
-- ============================================================================
CREATE TABLE notification_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Notification type
    notification_type VARCHAR(100) NOT NULL,  -- task_assigned, feedback_received, etc.
    
    -- Channels
    in_app_enabled BOOLEAN NOT NULL DEFAULT true,
    email_enabled BOOLEAN NOT NULL DEFAULT true,
    push_enabled BOOLEAN NOT NULL DEFAULT false,  -- For mobile later
    
    -- Frequency for digest emails
    email_frequency VARCHAR(50) DEFAULT 'immediate',  -- immediate, daily, weekly, never
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (user_id, notification_type)
);

-- Index
CREATE INDEX idx_notification_preferences_user ON notification_preferences(user_id);

-- Trigger
CREATE TRIGGER notification_preferences_updated_at
    BEFORE UPDATE ON notification_preferences
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- ANNOUNCEMENT
-- Organization-wide announcements
-- ============================================================================
CREATE TABLE announcements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Author
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- Content
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    
    -- Visibility
    target_type VARCHAR(50) NOT NULL DEFAULT 'organization',  -- organization, team, role
    target_team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    target_role_ids JSONB,  -- Array of role IDs
    
    -- Scheduling
    publish_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    
    -- Priority
    is_pinned BOOLEAN NOT NULL DEFAULT false,
    priority VARCHAR(20) NOT NULL DEFAULT 'normal',
    
    -- Status
    is_published BOOLEAN NOT NULL DEFAULT true,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_announcements_org ON announcements(organization_id);
CREATE INDEX idx_announcements_active ON announcements(organization_id, publish_at, expires_at) 
    WHERE is_published = true AND is_deleted = false;
CREATE INDEX idx_announcements_team ON announcements(target_team_id) 
    WHERE is_published = true AND is_deleted = false;

-- Trigger
CREATE TRIGGER announcements_updated_at
    BEFORE UPDATE ON announcements
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- ANNOUNCEMENT_READS
-- Track who has read announcements
-- ============================================================================
CREATE TABLE announcement_reads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    announcement_id UUID NOT NULL REFERENCES announcements(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    read_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    UNIQUE (announcement_id, user_id)
);

-- Index
CREATE INDEX idx_announcement_reads_announcement ON announcement_reads(announcement_id);
CREATE INDEX idx_announcement_reads_user ON announcement_reads(user_id);

SELECT 'Activity log and notifications tables created successfully' AS status;
