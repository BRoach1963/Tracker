-- ============================================================================
-- CREATE VIEW: v_team_members (joins team_members with users for birthday/hire_date)
-- Date: 2026-01-18
-- Description: Creates a view that exposes team_members data along with
--              birthday and hire_date from the users table.
--              This allows PostgREST queries to get all fields in one call.
-- ============================================================================

-- Prerequisites: 
-- 1. procohere.users table must have birthday and hire_date columns:
--    ALTER TABLE procohere.users ADD COLUMN IF NOT EXISTS birthday date;
--    ALTER TABLE procohere.users ADD COLUMN IF NOT EXISTS hire_date date;

-- Drop existing view if it exists
DROP VIEW IF EXISTS procohere.v_team_members;

-- Create the view
CREATE OR REPLACE VIEW procohere.v_team_members AS
SELECT 
    tm.id,
    tm.organization_id,
    tm.user_id,
    tm.first_name,
    tm.last_name,
    tm.job_title,
    tm.email,
    tm.phone,
    tm.avatar_url,
    tm.linkedin_url,
    tm.x_profile_url,
    tm.notes,
    tm.is_active,
    tm.manager_user_id,
    tm.manager_team_member_id,
    tm.created_at,
    tm.updated_at,
    tm.is_deleted,
    tm.deleted_at,
    tm.deleted_by,
    -- Fields from users table
    u.birthday,
    u.hire_date
FROM procohere.team_members tm
LEFT JOIN procohere.users u ON tm.user_id = u.id;

-- Grant access to the view
GRANT SELECT ON procohere.v_team_members TO authenticated;
GRANT SELECT ON procohere.v_team_members TO anon;

-- ============================================================================
-- USAGE IN C#:
-- Option A: Update the Table attribute
--   [Table("v_team_members", Schema = "procohere")]
--   public class TeamMemberDetail : BaseModel { ... }
--
-- Option B: If schema is configured at client level, just:
--   [Table("v_team_members")]
--   public class TeamMemberDetail : BaseModel { ... }
-- ============================================================================

-- Test the view:
-- SELECT id, first_name, last_name, email, birthday, hire_date 
-- FROM procohere.v_team_members 
-- WHERE organization_id = 'your-org-id' 
-- LIMIT 5;
