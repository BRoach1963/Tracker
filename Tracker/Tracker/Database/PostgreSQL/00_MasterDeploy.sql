/*
 * TRACKER DATABASE - MASTER DEPLOYMENT SCRIPT
 * PostgreSQL Edition with pgvector
 * 
 * This master script executes all sub-scripts in the correct order
 * to create a fully optimized Tracker database on PostgreSQL.
 * 
 * PREREQUISITES:
 * 1. PostgreSQL 15 or later (recommended: PostgreSQL 16+)
 * 2. pgvector extension available (for AI/semantic search features)
 * 3. CREATEDB permissions for application user
 * 
 * DEPLOYMENT STEPS:
 * 1. Create the database: CREATE DATABASE tracker_db;
 * 2. Connect to the database: \c tracker_db
 * 3. Execute scripts in order (01_ through 06_)
 * 4. Or use this master script with psql:
 *    psql -h localhost -U postgres -d tracker_db -f 00_MasterDeploy.sql
 * 
 * FEATURES:
 * - Multi-tenant organization support
 * - Row Level Security (RLS) for data isolation
 * - pgvector for AI/semantic search embeddings
 * - Optimistic concurrency with xmin
 * - Soft delete support with indexed is_deleted columns
 * - Automatic audit field population via triggers
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- =============================================================================
-- STEP 0: Create database (run separately as superuser)
-- =============================================================================
-- CREATE DATABASE tracker_db
--     WITH ENCODING = 'UTF8'
--     LC_COLLATE = 'en_US.UTF-8'
--     LC_CTYPE = 'en_US.UTF-8'
--     TEMPLATE = template0;

-- =============================================================================
-- STEP 1: Enable extensions
-- =============================================================================
\echo 'Enabling extensions...'
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";  -- pgvector for AI embeddings
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- For fuzzy text search

-- =============================================================================
-- STEP 2: Create schema objects
-- =============================================================================
\echo 'Creating core schema (organizations, users)...'
\ir 01_CreateSchema_Core.sql

\echo 'Creating team schema (team_members, manager_history)...'
\ir 02_CreateSchema_Team.sql

\echo 'Creating meetings schema (one_on_ones, tasks, projects)...'
\ir 03_CreateSchema_Meetings.sql

\echo 'Creating vector embeddings schema...'
\ir 04_CreateSchema_Vectors.sql

\echo 'Creating Row Level Security policies...'
\ir 05_CreateRlsPolicies.sql

\echo 'Creating views and functions...'
\ir 06_CreateViewsAndFunctions.sql

-- =============================================================================
-- STEP 3: Verify deployment
-- =============================================================================
\echo ''
\echo '=============================================='
\echo 'Deployment complete! Verifying tables...'
\echo '=============================================='

SELECT 
    schemaname,
    tablename,
    (SELECT COUNT(*) FROM information_schema.columns c 
     WHERE c.table_schema = t.schemaname 
     AND c.table_name = t.tablename) as column_count
FROM pg_tables t
WHERE schemaname = 'public'
ORDER BY tablename;

\echo ''
\echo 'Verifying RLS policies...'
SELECT tablename, policyname, permissive, roles, cmd
FROM pg_policies
WHERE schemaname = 'public'
ORDER BY tablename, policyname;

\echo ''
\echo 'Tracker PostgreSQL database deployment complete!'
