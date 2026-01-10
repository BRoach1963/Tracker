-- ============================================================================
-- TRACKER DATABASE - DROP ALL TABLES
-- Run this FIRST to clean slate the database
-- ============================================================================
-- WARNING: This will DELETE ALL DATA. Only run on dev/empty databases!
-- ============================================================================

-- Disable triggers temporarily to avoid FK issues during drop
SET session_replication_role = 'replica';

-- Drop all tables in reverse dependency order
-- Junction tables first, then entities, then core tables

DO $$ 
DECLARE
    r RECORD;
BEGIN
    -- Drop all tables in public schema
    FOR r IN (
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = 'public'
        AND tablename NOT IN ('schema_migrations', 'spatial_ref_sys')  -- Keep system tables
    ) LOOP
        EXECUTE 'DROP TABLE IF EXISTS public.' || quote_ident(r.tablename) || ' CASCADE';
        RAISE NOTICE 'Dropped table: %', r.tablename;
    END LOOP;
    
    -- Drop all custom types/enums
    FOR r IN (
        SELECT typname 
        FROM pg_type 
        WHERE typnamespace = 'public'::regnamespace 
        AND typtype = 'e'
    ) LOOP
        EXECUTE 'DROP TYPE IF EXISTS public.' || quote_ident(r.typname) || ' CASCADE';
        RAISE NOTICE 'Dropped type: %', r.typname;
    END LOOP;
END $$;

-- Re-enable triggers
SET session_replication_role = 'origin';

-- Verify all tables dropped
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- Should return empty or only system tables
