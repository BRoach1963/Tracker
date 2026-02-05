-- Migration: Add recurring task support
-- Date: 2026-02-04
-- Description: Adds recurrence pattern fields to tasks table

SET search_path TO procohere;

-- Add recurrence columns to tasks table
ALTER TABLE tasks 
    ADD COLUMN is_recurring BOOLEAN DEFAULT FALSE NOT NULL,
    ADD COLUMN recurrence_pattern TEXT, -- 'daily', 'weekly', 'monthly'
    ADD COLUMN recurrence_interval INTEGER DEFAULT 1, -- every N days/weeks/months
    ADD COLUMN recurrence_end_date TIMESTAMPTZ, -- when recurrence stops (nullable = never ends)
    ADD COLUMN parent_recurring_task_id UUID REFERENCES tasks(id) ON DELETE CASCADE; -- link instances to parent

-- Create index for finding recurring task instances
CREATE INDEX idx_tasks_parent_recurring ON tasks(parent_recurring_task_id) WHERE parent_recurring_task_id IS NOT NULL;

-- Create index for active recurring tasks
CREATE INDEX idx_tasks_is_recurring ON tasks(is_recurring) WHERE is_recurring = TRUE;

-- Add comments
COMMENT ON COLUMN tasks.is_recurring IS 'Whether this task has a recurrence pattern';
COMMENT ON COLUMN tasks.recurrence_pattern IS 'Recurrence frequency: daily, weekly, monthly';
COMMENT ON COLUMN tasks.recurrence_interval IS 'Recurrence interval: repeat every N days/weeks/months';
COMMENT ON COLUMN tasks.recurrence_end_date IS 'When recurrence stops (NULL = never ends)';
COMMENT ON COLUMN tasks.parent_recurring_task_id IS 'Links recurring task instances to their parent pattern';
