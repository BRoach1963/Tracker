-- App secrets table for storing API keys and sensitive configuration
-- These are fetched by authenticated users at runtime, never stored in source code

CREATE TABLE IF NOT EXISTS app_secrets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_name TEXT NOT NULL UNIQUE,
    key_value TEXT NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE app_secrets ENABLE ROW LEVEL SECURITY;

-- Only authenticated users can read secrets (they still can't see the raw table in dashboard)
CREATE POLICY "Authenticated users can read app secrets"
    ON app_secrets
    FOR SELECT
    TO authenticated
    USING (true);

-- No insert/update/delete from client - only via dashboard or service role
-- This ensures only admins can modify secrets

-- Create index for fast lookups
CREATE INDEX idx_app_secrets_key_name ON app_secrets(key_name);

-- Insert the Gemini API key
-- NOTE: After running this migration, update the key_value via Supabase Dashboard > Table Editor
INSERT INTO app_secrets (key_name, key_value, description)
VALUES (
    'gemini_api_key',
    'PLACEHOLDER_UPDATE_VIA_DASHBOARD',
    'Google Gemini API key for AI features'
)
ON CONFLICT (key_name) DO NOTHING;

-- Add a comment for documentation
COMMENT ON TABLE app_secrets IS 'Stores API keys and sensitive configuration. Readable by authenticated users only.';
