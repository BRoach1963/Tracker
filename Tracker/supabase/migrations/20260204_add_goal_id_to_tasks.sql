-- Migration: Add goal_id column to tasks table
-- Date: 2026-02-04
-- Purpose: Enable task-goal linking feature (Item #10)
-- Related PR/Issue: PROCOHERE_PRIORITY_BACKLOG.md Item #10

-- Add goal_id column to tasks table with foreign key to goals
ALTER TABLE tasks
ADD COLUMN IF NOT EXISTS goal_id UUID REFERENCES goals(id) ON DELETE SET NULL;

-- Create index for faster lookups when finding tasks for a goal
CREATE INDEX IF NOT EXISTS idx_tasks_goal_id ON tasks(goal_id) WHERE goal_id IS NOT NULL AND is_deleted = false;

-- Add comment to document the column
COMMENT ON COLUMN tasks.goal_id IS 'Optional reference to the goal this task helps achieve. Tasks can be linked to at most one goal.';
