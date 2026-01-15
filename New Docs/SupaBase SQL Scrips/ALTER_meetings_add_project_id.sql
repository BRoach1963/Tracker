-- ============================================================================
-- ALTER Script: Add project_id column to meetings table
-- Purpose: Enable linking meetings to projects for project-related meetings
-- Date: 2026-01-15
-- ============================================================================

-- Add the project_id column (nullable - not all meetings are project-related)
ALTER TABLE meetings
ADD COLUMN IF NOT EXISTS project_id uuid REFERENCES projects(id) ON DELETE SET NULL;

-- Add comment for documentation
COMMENT ON COLUMN meetings.project_id IS 'FK to projects table. Populated when meeting_type = project or when meeting is associated with a specific project.';

-- Create index for filtering meetings by project
CREATE INDEX IF NOT EXISTS idx_meetings_project_id ON meetings(project_id);

-- ============================================================================
-- Verification query (run after migration):
-- SELECT project_id, COUNT(*) FROM meetings WHERE project_id IS NOT NULL GROUP BY project_id;
-- ============================================================================
