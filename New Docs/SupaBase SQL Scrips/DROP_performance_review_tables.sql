-- ============================================================================
-- DROP Script: Performance Review Tables
-- Purpose: Remove review system tables - functionality replaced by Meeting 
--          with MeetingType = 'Review'. HR review functions belong in 
--          dedicated HR software (Workday, Lattice, BambooHR, etc.)
-- Date: 2026-01-15
-- ============================================================================

-- IMPORTANT: Run these in order due to foreign key dependencies

-- First drop tables that reference other review tables
DROP TABLE IF EXISTS review_responses CASCADE;

-- Drop the reviews table (individual reviews in a cycle)
DROP TABLE IF EXISTS reviews CASCADE;

-- Drop performance_reviews table (standalone reviews)
DROP TABLE IF EXISTS performance_reviews CASCADE;

-- Drop review cycles
DROP TABLE IF EXISTS review_cycles CASCADE;

-- Drop template questions
DROP TABLE IF EXISTS review_template_questions CASCADE;

-- Drop template sections  
DROP TABLE IF EXISTS review_template_sections CASCADE;

-- Drop review templates
DROP TABLE IF EXISTS review_templates CASCADE;

-- Drop associated enums
DROP TYPE IF EXISTS review_cycle_status CASCADE;
DROP TYPE IF EXISTS review_status CASCADE;
DROP TYPE IF EXISTS review_question_type CASCADE;

-- ============================================================================
-- NOTE: Performance review discussions should now be tracked as:
--   - Meeting with meeting_type = 'review'
--   - Agenda items linked to goals/metrics via linked_entity_type/linked_entity_id
--   - Meeting notes for discussion points
-- ============================================================================
