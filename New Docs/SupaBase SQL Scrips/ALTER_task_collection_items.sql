-- ============================================================================
-- ALTER Script: task_collection_items
-- Purpose: Add missing columns for sorting and RLS support
-- Date: 2026-01-15
-- ============================================================================

-- Add sort_order column for ordering items within a collection
ALTER TABLE task_collection_items 
ADD COLUMN IF NOT EXISTS sort_order INT NOT NULL DEFAULT 0;

-- Add organization_id for RLS policies (denormalized for performance)
-- This allows RLS to filter without joining to task_collections
ALTER TABLE task_collection_items 
ADD COLUMN IF NOT EXISTS organization_id UUID;

-- Backfill organization_id from parent collection
UPDATE task_collection_items tci
SET organization_id = tc.organization_id
FROM task_collections tc
WHERE tci.collection_id = tc.id
AND tci.organization_id IS NULL;

-- Make organization_id NOT NULL after backfill (only if you want to enforce it)
-- ALTER TABLE task_collection_items ALTER COLUMN organization_id SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE task_collection_items
ADD CONSTRAINT fk_task_collection_items_org 
FOREIGN KEY (organization_id) REFERENCES organizations(id);

-- Add index for RLS filtering
CREATE INDEX IF NOT EXISTS idx_task_collection_items_org 
ON task_collection_items(organization_id);

-- Add index for sort order queries
CREATE INDEX IF NOT EXISTS idx_task_collection_items_sort 
ON task_collection_items(collection_id, sort_order);

-- Add comments
COMMENT ON COLUMN task_collection_items.sort_order IS 'Display order within the collection';
COMMENT ON COLUMN task_collection_items.organization_id IS 'Denormalized org ID for RLS performance';
