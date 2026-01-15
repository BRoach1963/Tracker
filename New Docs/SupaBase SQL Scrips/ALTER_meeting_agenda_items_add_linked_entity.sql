-- ============================================================================
-- ALTER Script: Add linked entity columns to meeting_agenda_items table
-- Purpose: Allow agenda items to reference existing tasks/goals/metrics/projects for discussion
-- Date: 2026-01-15
-- ============================================================================

-- Add linked_entity_type column (polymorphic type discriminator)
ALTER TABLE meeting_agenda_items
ADD COLUMN IF NOT EXISTS linked_entity_type VARCHAR(50);

-- Add linked_entity_id column (polymorphic FK)
ALTER TABLE meeting_agenda_items
ADD COLUMN IF NOT EXISTS linked_entity_id UUID;

-- Add comments for documentation
COMMENT ON COLUMN meeting_agenda_items.linked_entity_type IS 'Type of entity being discussed: task, goal, metric, project. NULL for standalone agenda items.';
COMMENT ON COLUMN meeting_agenda_items.linked_entity_id IS 'UUID of the entity being discussed. NULL for standalone agenda items.';

-- Create index for finding agenda items by linked entity
CREATE INDEX IF NOT EXISTS idx_meeting_agenda_items_linked_entity 
ON meeting_agenda_items(linked_entity_type, linked_entity_id) 
WHERE linked_entity_id IS NOT NULL;

-- ============================================================================
-- Usage examples:
-- 
-- Standalone agenda item (just a topic):
--   linked_entity_type = NULL, linked_entity_id = NULL
--
-- Discussing an existing task:
--   linked_entity_type = 'task', linked_entity_id = <task_uuid>
--
-- Discussing a goal:
--   linked_entity_type = 'goal', linked_entity_id = <goal_uuid>
--
-- Discussing a metric:
--   linked_entity_type = 'metric', linked_entity_id = <metric_uuid>
--
-- Discussing a project:
--   linked_entity_type = 'project', linked_entity_id = <project_uuid>
-- ============================================================================

-- ============================================================================
-- Verification query (run after migration):
-- SELECT linked_entity_type, COUNT(*) FROM meeting_agenda_items GROUP BY linked_entity_type;
-- ============================================================================
