-- ============================================================================
-- MIGRATION: Add job_title and company columns to users table
-- These fields support user profile functionality in the desktop app
-- Run this in Supabase SQL editor
-- ============================================================================

-- Add job_title column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'users' 
        AND column_name = 'job_title'
    ) THEN
        ALTER TABLE users ADD COLUMN job_title VARCHAR(200);
    END IF;
END $$;

-- Add company column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'users' 
        AND column_name = 'company'
    ) THEN
        ALTER TABLE users ADD COLUMN company VARCHAR(200);
    END IF;
END $$;

-- Verify the columns were added
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public' 
AND table_name = 'users'
AND column_name IN ('job_title', 'company');
