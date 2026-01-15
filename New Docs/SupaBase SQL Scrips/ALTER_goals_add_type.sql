-- ============================================================================
-- ALTER Script: Add goal_type column to goals table
-- Purpose: Support categorization of goals as Organizational, Team, or Personal
-- Date: 2026-01-15
-- ============================================================================

-- First, create the enum type if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'goal_type') THEN
        CREATE TYPE goal_type AS ENUM ('organizational', 'team', 'personal');
    END IF;
END$$;

-- Add the type column to goals table
ALTER TABLE goals
ADD COLUMN IF NOT EXISTS type goal_type NOT NULL DEFAULT 'organizational';

-- Add comment for documentation
COMMENT ON COLUMN goals.type IS 'Type of goal: organizational (company-wide), team (team-specific), or personal (individual development)';

-- Create index for filtering by type
CREATE INDEX IF NOT EXISTS idx_goals_type ON goals(type);

-- ============================================================================
-- Verification query (run after migration):
-- SELECT type, COUNT(*) FROM goals GROUP BY type;
-- ============================================================================
