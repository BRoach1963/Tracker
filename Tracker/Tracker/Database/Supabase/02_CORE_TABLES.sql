-- ============================================================================
-- TRACKER DATABASE - CORE TABLES
-- Organizations, Roles, Users
-- ============================================================================

-- ============================================================================
-- ORGANIZATIONS
-- The top-level tenant. All data is scoped to an organization.
-- ============================================================================
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Basic info
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(100) UNIQUE,  -- URL-friendly identifier
    
    -- Subscription
    subscription_tier VARCHAR(50) NOT NULL DEFAULT 'free',  -- free, professional, enterprise
    max_users INTEGER DEFAULT 5,
    max_team_members INTEGER DEFAULT 25,
    
    -- Settings
    settings JSONB DEFAULT '{}',
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    
    -- Constraints
    CONSTRAINT valid_subscription CHECK (subscription_tier IN ('free', 'professional', 'enterprise'))
);

-- Indexes
CREATE INDEX idx_organizations_slug ON organizations(slug);
CREATE INDEX idx_organizations_active ON organizations(is_active) WHERE is_active = true;

-- Trigger for updated_at
CREATE TRIGGER organizations_updated_at
    BEFORE UPDATE ON organizations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- ROLES
-- Define what each role can do. Seeded with defaults.
-- ============================================================================
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Identity
    name VARCHAR(50) NOT NULL UNIQUE,  -- admin, manager, team_lead, member, viewer
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    
    -- Organization permissions
    can_manage_org BOOLEAN NOT NULL DEFAULT false,
    can_manage_billing BOOLEAN NOT NULL DEFAULT false,
    
    -- User permissions
    can_manage_users BOOLEAN NOT NULL DEFAULT false,
    can_invite_users BOOLEAN NOT NULL DEFAULT false,
    can_assign_roles BOOLEAN NOT NULL DEFAULT false,
    
    -- Team permissions
    can_manage_teams BOOLEAN NOT NULL DEFAULT false,
    can_create_teams BOOLEAN NOT NULL DEFAULT false,
    
    -- Goal permissions
    can_create_goals BOOLEAN NOT NULL DEFAULT false,
    can_edit_all_goals BOOLEAN NOT NULL DEFAULT false,
    can_edit_own_goals BOOLEAN NOT NULL DEFAULT false,
    can_view_team_goals BOOLEAN NOT NULL DEFAULT false,
    can_view_org_goals BOOLEAN NOT NULL DEFAULT false,
    
    -- Metric permissions
    can_create_metrics BOOLEAN NOT NULL DEFAULT false,
    can_edit_metrics BOOLEAN NOT NULL DEFAULT false,
    can_view_team_metrics BOOLEAN NOT NULL DEFAULT false,
    can_view_org_metrics BOOLEAN NOT NULL DEFAULT false,
    
    -- Task permissions
    can_create_tasks BOOLEAN NOT NULL DEFAULT false,
    can_assign_tasks BOOLEAN NOT NULL DEFAULT false,
    can_view_team_tasks BOOLEAN NOT NULL DEFAULT false,
    
    -- Meeting permissions
    can_schedule_meetings BOOLEAN NOT NULL DEFAULT false,
    can_run_meetings BOOLEAN NOT NULL DEFAULT false,
    can_participate_meetings BOOLEAN NOT NULL DEFAULT false,
    can_view_meeting_notes BOOLEAN NOT NULL DEFAULT false,
    
    -- Feedback permissions
    can_give_feedback BOOLEAN NOT NULL DEFAULT false,
    can_receive_feedback BOOLEAN NOT NULL DEFAULT false,
    can_view_team_feedback BOOLEAN NOT NULL DEFAULT false,
    
    -- Analytics permissions
    can_view_team_analytics BOOLEAN NOT NULL DEFAULT false,
    can_view_org_analytics BOOLEAN NOT NULL DEFAULT false,
    can_export_data BOOLEAN NOT NULL DEFAULT false,
    
    -- System
    is_system_role BOOLEAN NOT NULL DEFAULT false,  -- Can't be deleted
    sort_order INTEGER NOT NULL DEFAULT 0,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Trigger for updated_at
CREATE TRIGGER roles_updated_at
    BEFORE UPDATE ON roles
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- USERS
-- Application users who log in. Links to Supabase auth.
-- ============================================================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Supabase auth link
    supabase_auth_id UUID UNIQUE,  -- Links to auth.users
    
    -- Organization (required after onboarding)
    organization_id UUID REFERENCES organizations(id) ON DELETE SET NULL,
    
    -- Profile
    email VARCHAR(255) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url TEXT,
    phone VARCHAR(50),
    timezone VARCHAR(100) DEFAULT 'UTC',
    
    -- If this user is also a team member (for self-tracking)
    linked_team_member_id UUID,  -- Will be FK after team_members created
    
    -- Settings
    preferences JSONB DEFAULT '{}',
    notification_settings JSONB DEFAULT '{
        "email_notifications": true,
        "meeting_reminders": true,
        "goal_updates": true,
        "feedback_received": true
    }',
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    last_login_at TIMESTAMPTZ,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100)
);

-- Indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_org ON users(organization_id);
CREATE INDEX idx_users_supabase ON users(supabase_auth_id);
CREATE INDEX idx_users_active ON users(organization_id, is_active) WHERE is_active = true AND is_deleted = false;

-- Trigger for updated_at
CREATE TRIGGER users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- USER_ROLES
-- Maps users to roles (many-to-many, but typically one role per org)
-- ============================================================================
CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    
    -- Optional: Team-specific role (for team_lead)
    team_id UUID,  -- Will be FK after teams table created
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_by UUID REFERENCES users(id)
);

-- Indexes
CREATE INDEX idx_user_roles_user ON user_roles(user_id);
CREATE INDEX idx_user_roles_org ON user_roles(organization_id);
CREATE INDEX idx_user_roles_lookup ON user_roles(user_id, organization_id);

-- Each user has one role per org (or per team if team_id specified)
-- Using a unique index with COALESCE since constraints can't use functions
CREATE UNIQUE INDEX idx_user_roles_unique 
    ON user_roles(user_id, organization_id, COALESCE(team_id, '00000000-0000-0000-0000-000000000000'::uuid));

-- ============================================================================
-- USER_SESSIONS
-- Tracks device sessions for "remember me" and device management
-- ============================================================================
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- Device identification
    device_id TEXT NOT NULL,                    -- Unique device fingerprint/identifier
    device_name TEXT,                           -- User-friendly name: "Brian's Windows PC"
    device_type VARCHAR(50) NOT NULL,           -- 'desktop', 'mobile', 'tablet', 'web'
    os_name VARCHAR(100),                       -- 'Windows 11', 'macOS Sonoma', 'iOS 17'
    app_version VARCHAR(50),                    -- '2.1.0' - for tracking which version is used
    
    -- Session tokens (hashed - never store plain tokens!)
    refresh_token_hash TEXT,                    -- bcrypt hash of refresh token
    
    -- Activity tracking
    last_active_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_ip_address INET,                       -- For security audit
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    revoked_at TIMESTAMPTZ,                     -- When session was explicitly revoked
    revoked_reason TEXT,                        -- 'user_logout', 'admin_revoke', 'password_change', 'security'
    
    -- Session limits
    expires_at TIMESTAMPTZ,                     -- Optional hard expiry
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_user_sessions_user ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_device ON user_sessions(device_id);
CREATE INDEX idx_user_sessions_active ON user_sessions(user_id, is_active) WHERE is_active = true;
CREATE INDEX idx_user_sessions_last_active ON user_sessions(last_active_at);

-- One active session per device per user
CREATE UNIQUE INDEX idx_user_sessions_unique_device 
    ON user_sessions(user_id, device_id) WHERE is_active = true;

-- Trigger for updated_at
CREATE TRIGGER user_sessions_updated_at
    BEFORE UPDATE ON user_sessions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

SELECT 'Core tables (organizations, roles, users, user_sessions) created successfully' AS status;
