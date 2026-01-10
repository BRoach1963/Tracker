-- ============================================================================
-- TRACKER DATABASE - FULL WIPE
-- ============================================================================
-- Drops ALL tables, types, and extensions to start fresh
-- Run this FIRST, then run scripts 01-18 in order
-- ============================================================================

-- Drop ALL tables (using CASCADE handles dependencies)
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

SELECT 'Full wipe completed - database is clean' AS status;
