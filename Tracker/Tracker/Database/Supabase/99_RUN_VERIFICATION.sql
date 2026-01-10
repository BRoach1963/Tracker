-- ============================================================================
-- TRACKER DATABASE - MASTER SETUP SCRIPT
-- ============================================================================
-- 
-- This script runs all database setup scripts in the correct order.
-- 
-- INSTRUCTIONS:
-- 1. Open your Supabase project dashboard
-- 2. Go to SQL Editor
-- 3. Run the scripts in order (00 through 18)
--    OR run this master script (it includes all others)
--
-- NOTE: Supabase SQL Editor may have size limits. If this script is too large,
-- run each numbered script individually in sequence.
--
-- ============================================================================

-- Show what we're about to do
SELECT 'Starting Tracker database setup...' AS status;
SELECT NOW() AS started_at;

-- ============================================================================
-- The individual scripts should be run in this order:
-- 
-- SCHEMA SCRIPTS (00-12):
-- 00_FULL_WIPE.sql              - Drop all tables (clean slate)
-- 01_EXTENSIONS_TYPES.sql       - Enable extensions and create enums
-- 02_CORE_TABLES.sql            - Organizations, roles, users
-- 03_TEAMS.sql                  - Teams and team members
-- 04_GOALS.sql                  - Goals and targets
-- 05_METRICS.sql                - Metrics and history
-- 06_PROJECTS_TASKS.sql         - Projects and tasks
-- 07_MEETINGS.sql               - Meetings and 1:1s
-- 08_FEEDBACK.sql               - Feedback and recognition
-- 09_NOTES.sql                  - Notes and journals
-- 10_AI_VECTORS.sql             - Vector embeddings for AI
-- 11_ACTIVITY_NOTIFICATIONS.sql - Activity log and notifications
-- 12_RLS_POLICIES.sql           - Row Level Security
--
-- NEW FEATURE SCHEMAS (19-23):
-- 19_REVIEWS.sql                - Performance review templates, cycles, reviews
-- 20_SURVEYS.sql                - Pulse surveys and engagement surveys
-- 21_DEVELOPMENT_GOALS.sql      - Personal/career development goals
-- 22_CALENDAR_REMINDERS.sql     - Calendar sync and reminder system
-- 23_PROGRESS_SNAPSHOTS.sql     - Historical analytics snapshots
--
-- SEED DATA SCRIPTS (13-18, 24-28):
-- 13_SEED_ROLES.sql             - Default role seeding function
-- 14_SEED_TEST_ORG.sql          - Test organization
-- 15_SEED_TEST_USERS.sql        - Test users and teams
-- 16_SEED_GOALS_METRICS.sql     - Sample goals and metrics
-- 17_SEED_TASKS_PROJECTS.sql    - Sample tasks and projects
-- 18_SEED_MEETINGS_FEEDBACK.sql - Sample meetings and feedback
-- 24_SEED_REVIEWS.sql           - Sample review templates and reviews
-- 25_SEED_SURVEYS.sql           - Sample surveys and responses
-- 26_SEED_DEV_GOALS.sql         - Sample development goals
-- 27_SEED_CALENDAR_REMINDERS.sql - Sample calendar links and reminders
-- 28_SEED_SNAPSHOTS.sql         - Sample analytics snapshots
--
-- ============================================================================

-- Verification queries to run after setup:

-- Check all tables were created
SELECT 
    schemaname,
    tablename
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;

-- Check extensions
SELECT extname, extversion 
FROM pg_extension 
WHERE extname IN ('uuid-ossp', 'pgcrypto', 'vector');

-- Check custom types
SELECT typname 
FROM pg_type 
WHERE typnamespace = 'public'::regnamespace 
  AND typtype = 'e'
ORDER BY typname;

-- Check roles were created (roles are GLOBAL, not per-org)
SELECT name, display_name, is_system_role, sort_order
FROM roles 
ORDER BY sort_order DESC;

-- Check team members
SELECT 
    tm.first_name || ' ' || tm.last_name AS name,
    tm.job_title,
    tm.email,
    u.display_name AS user_display_name
FROM team_members tm
LEFT JOIN users u ON u.id = tm.linked_user_id
WHERE tm.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY tm.hire_date;

SELECT 'Tracker database setup verification complete!' AS status;
SELECT NOW() AS completed_at;
