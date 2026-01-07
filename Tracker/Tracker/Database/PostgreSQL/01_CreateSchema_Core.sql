/*
 * TRACKER DATABASE - CORE SCHEMA
 * PostgreSQL Edition
 * 
 * Creates the foundational tables for multi-tenant organization support:
 * - organizations: Multi-tenant organization entities
 * - users: Application users linked to Supabase auth
 * 
 * Conventions:
 * - snake_case for all identifiers (PostgreSQL best practice)
 * - UUIDs for primary keys (better for distributed systems)
 * - Consistent audit columns on all tables
 * - Soft delete via is_deleted flag
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- UTILITY FUNCTIONS
-- =============================================================================

-- Function to update modified_at timestamp automatically
CREATE OR REPLACE FUNCTION update_modified_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.modified_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function to generate URL-safe slugs from names
CREATE OR REPLACE FUNCTION generate_slug(input_text TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN lower(
        regexp_replace(
            regexp_replace(
                trim(input_text),
                '[^a-zA-Z0-9\s-]', '', 'g'  -- Remove special chars
            ),
            '[\s]+', '-', 'g'  -- Replace spaces with hyphens
        )
    );
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- =============================================================================
-- ORGANIZATIONS TABLE
-- =============================================================================
-- The multi-tenant root entity. All data belongs to exactly one organization.
-- Each organization is a separate billable entity with its own subscription tier.

CREATE TABLE IF NOT EXISTS organizations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Core fields
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(200) NOT NULL,  -- URL-friendly identifier (e.g., "acme-corp")
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Subscription/billing
    subscription_tier VARCHAR(50) NOT NULL DEFAULT 'free',  -- free, professional, enterprise
    max_users INT NOT NULL DEFAULT 1,
    max_team_members INT NOT NULL DEFAULT 10,
    
    -- Supabase integration
    supabase_org_id VARCHAR(100),  -- Link to Supabase organization if any
    
    -- Settings (JSON for flexibility)
    settings JSONB DEFAULT '{}',
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100),
    
    -- Constraints
    CONSTRAINT uq_organizations_slug UNIQUE (slug),
    CONSTRAINT chk_organizations_subscription_tier 
        CHECK (subscription_tier IN ('free', 'professional', 'enterprise'))
);

-- Indexes for organizations
CREATE INDEX IF NOT EXISTS ix_organizations_slug ON organizations(slug) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_organizations_is_active ON organizations(is_active) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_organizations_subscription_tier ON organizations(subscription_tier) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_organizations_modified_at ON organizations;
CREATE TRIGGER trg_organizations_modified_at
    BEFORE UPDATE ON organizations
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE organizations IS 'Multi-tenant organizations - the root entity for all data';
COMMENT ON COLUMN organizations.slug IS 'URL-friendly unique identifier derived from name';
COMMENT ON COLUMN organizations.subscription_tier IS 'Billing tier: free, professional, or enterprise';
COMMENT ON COLUMN organizations.settings IS 'Organization-specific settings stored as JSON';

-- =============================================================================
-- USERS TABLE
-- =============================================================================
-- Application users linked to Supabase authentication.
-- Each user belongs to exactly one organization.
-- The user with role 'owner' is the organization admin.

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization link
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Core identity
    username VARCHAR(200) NOT NULL,
    email VARCHAR(200),
    display_name VARCHAR(200),
    
    -- Supabase integration
    supabase_user_id UUID,  -- Links to Supabase auth.users
    
    -- Role within organization
    role VARCHAR(50) NOT NULL DEFAULT 'member',  -- owner, admin, member, viewer
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_login TIMESTAMPTZ,
    
    -- Preferences (JSON for flexibility)
    preferences JSONB DEFAULT '{}',
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by VARCHAR(100),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by VARCHAR(100),
    
    -- Constraints
    CONSTRAINT uq_users_org_username UNIQUE (organization_id, username),
    CONSTRAINT uq_users_supabase_id UNIQUE (supabase_user_id),
    CONSTRAINT chk_users_role CHECK (role IN ('owner', 'admin', 'member', 'viewer'))
);

-- Indexes for users
CREATE INDEX IF NOT EXISTS ix_users_organization_id ON users(organization_id) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_users_supabase_user_id ON users(supabase_user_id) WHERE supabase_user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_users_email ON users(email) WHERE email IS NOT NULL AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_users_is_active ON users(organization_id, is_active) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_users_role ON users(organization_id, role) WHERE NOT is_deleted;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_users_modified_at ON users;
CREATE TRIGGER trg_users_modified_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE users IS 'Application users linked to Supabase auth, scoped to organizations';
COMMENT ON COLUMN users.organization_id IS 'The organization this user belongs to';
COMMENT ON COLUMN users.supabase_user_id IS 'Foreign key to Supabase auth.users for SSO';
COMMENT ON COLUMN users.role IS 'User role: owner (org admin), admin, member, or viewer';
COMMENT ON COLUMN users.preferences IS 'User-specific preferences stored as JSON';

-- =============================================================================
-- GRANT PERMISSIONS
-- =============================================================================
-- These should be run by a superuser after creating the tracker_app role

-- GRANT SELECT, INSERT, UPDATE, DELETE ON organizations TO tracker_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON users TO tracker_app;
-- GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO tracker_app;

\echo 'Core schema (organizations, users) created successfully.'
