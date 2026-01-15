-- ============================================================================
-- ALTER TABLE: users - Add Missing Columns
-- Date: 2026-01-14
-- Purpose: Add columns that exist in the C# User model but were missing
--          from the original schema design
-- ============================================================================

-- Add firm_id for licensing/firm association
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS firm_id UUID;

COMMENT ON COLUMN users.firm_id IS 'The firm/license this user belongs to. Used for multi-tenant licensing.';

-- Add username for login identifier (Windows username, SSO identifier, etc.)
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS username VARCHAR(200);

COMMENT ON COLUMN users.username IS 'Login identifier (Windows username, SSO identifier, or email). Used for authentication.';

-- Add is_admin for administrator privileges
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS is_admin BOOLEAN NOT NULL DEFAULT false;

COMMENT ON COLUMN users.is_admin IS 'Whether this user has administrator privileges for database management, user cleanup, etc.';

-- Add role for role-based access control
-- Note: This is the legacy single-role field. For fine-grained RBAC, use user_roles table.
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS role VARCHAR(50) NOT NULL DEFAULT 'manager';

COMMENT ON COLUMN users.role IS 'Primary role: admin, hr_admin, manager, viewer. For fine-grained permissions, see user_roles table.';

-- Add password_hash for local authentication scenarios
-- Only used when authenticating against local PostgreSQL (not Supabase Auth)
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS password_hash TEXT;

COMMENT ON COLUMN users.password_hash IS 'BCrypt-hashed password for local authentication. NULL when using Supabase Auth.';

-- ============================================================================
-- Verification Query
-- Run this after the ALTER statements to confirm columns were added
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'users'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 24 original + 5 new = 29 columns
-- ============================================================================
