-- ============================================================
-- MEETING PREP ITEMS TABLE CREATION
-- Date: 2026-01-23
-- Purpose: Create procohere.meeting_prep_items table with full prep workflow support
-- 
-- This table supports:
-- - Personal, assigned, and team-scoped prep items
-- - Linked entity references (tasks, goals, metrics, projects)
-- - Prep prompts and captured responses
-- - Status tracking with history
-- - Carry-forward support for recurring meetings
-- ============================================================

-- ============================================================
-- PART 1: CREATE MEETING_PREP_ITEMS TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS procohere.meeting_prep_items (
    -- Identity
    id                              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id                 uuid NOT NULL REFERENCES public.organizations(id),
    meeting_id                      uuid NOT NULL REFERENCES procohere.meetings(id) ON DELETE CASCADE,
    
    -- Ownership & Assignment
    requested_by_team_member_id     uuid NOT NULL REFERENCES procohere.team_members(id),
    assigned_to_team_member_id      uuid REFERENCES procohere.team_members(id),
    
    -- Core Content
    title                           text NOT NULL,
    body                            text,
    assignee_notes                  text,
    
    -- Visibility: 'personal', 'assigned', 'meeting'
    visibility_scope                text NOT NULL DEFAULT 'personal',
    
    -- Status: 'open', 'in_progress', 'done', 'dismissed'
    status                          text NOT NULL DEFAULT 'open',
    status_updated_at               timestamptz,
    status_updated_by_team_member_id uuid REFERENCES procohere.team_members(id),
    overridden_status               boolean NOT NULL DEFAULT false,
    
    -- Due Date & Completion
    due_at                          timestamptz,
    completed_at                    timestamptz,
    completed_by_team_member_id     uuid REFERENCES procohere.team_members(id),
    
    -- Sort Order
    sort_order                      int NOT NULL DEFAULT 0,
    
    -- Carry-Forward Support
    carry_forward                   boolean NOT NULL DEFAULT false,
    carried_from_prep_item_id       uuid REFERENCES procohere.meeting_prep_items(id),
    
    -- Provenance
    source_type                     text,  -- 'manual', 'scaffold', 'ai', 'carry_forward'
    source_snapshot                 text,  -- JSON snapshot of source data
    
    -- Enhanced Prep Fields (linked entity + prompt/response)
    linked_entity_type              text,  -- 'task', 'goal', 'metric', 'project'
    linked_entity_id                uuid,
    linked_entity_title_snapshot    text,  -- Cached title at link time
    
    prep_prompt                     text,  -- What to think about / prepare
    prep_response                   text,  -- The captured preparation/thinking
    prepared_at                     timestamptz,  -- When prep was completed
    
    -- Lifecycle
    is_deleted                      boolean NOT NULL DEFAULT false,
    created_at                      timestamptz NOT NULL DEFAULT now(),
    updated_at                      timestamptz NOT NULL DEFAULT now(),
    deleted_at                      timestamptz,
    deleted_by                      uuid REFERENCES public.users(id)
);

-- ============================================================
-- PART 2: CONSTRAINTS
-- ============================================================

-- Visibility scope constraint
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_prep_item_visibility_scope'
    ) THEN
        ALTER TABLE procohere.meeting_prep_items
        ADD CONSTRAINT chk_prep_item_visibility_scope 
        CHECK (visibility_scope IN ('personal', 'assigned', 'meeting'));
    END IF;
END $$;

-- Status constraint
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_prep_item_status'
    ) THEN
        ALTER TABLE procohere.meeting_prep_items
        ADD CONSTRAINT chk_prep_item_status 
        CHECK (status IN ('open', 'in_progress', 'done', 'dismissed'));
    END IF;
END $$;

-- Source type constraint
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_prep_item_source_type'
    ) THEN
        ALTER TABLE procohere.meeting_prep_items
        ADD CONSTRAINT chk_prep_item_source_type 
        CHECK (source_type IS NULL OR source_type IN ('manual', 'scaffold', 'ai', 'carry_forward'));
    END IF;
END $$;

-- Linked entity type constraint
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_prep_item_linked_entity_type'
    ) THEN
        ALTER TABLE procohere.meeting_prep_items
        ADD CONSTRAINT chk_prep_item_linked_entity_type 
        CHECK (linked_entity_type IS NULL OR linked_entity_type IN ('task', 'goal', 'metric', 'project'));
    END IF;
END $$;

-- Assigned visibility requires assignee constraint
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_prep_item_assigned_requires_assignee'
    ) THEN
        ALTER TABLE procohere.meeting_prep_items
        ADD CONSTRAINT chk_prep_item_assigned_requires_assignee 
        CHECK (visibility_scope != 'assigned' OR assigned_to_team_member_id IS NOT NULL);
    END IF;
END $$;

-- ============================================================
-- PART 3: INDEXES
-- ============================================================

-- Primary query index: by meeting
CREATE INDEX IF NOT EXISTS idx_prep_items_meeting
ON procohere.meeting_prep_items(meeting_id)
WHERE is_deleted = false;

-- Organization scope index
CREATE INDEX IF NOT EXISTS idx_prep_items_org
ON procohere.meeting_prep_items(organization_id)
WHERE is_deleted = false;

-- Requester index (for "my prep items" view)
CREATE INDEX IF NOT EXISTS idx_prep_items_requested_by
ON procohere.meeting_prep_items(requested_by_team_member_id)
WHERE is_deleted = false;

-- Assignee index (for "assigned to me" view)
CREATE INDEX IF NOT EXISTS idx_prep_items_assigned_to
ON procohere.meeting_prep_items(assigned_to_team_member_id)
WHERE is_deleted = false AND assigned_to_team_member_id IS NOT NULL;

-- Status index (for filtering open vs done)
CREATE INDEX IF NOT EXISTS idx_prep_items_status
ON procohere.meeting_prep_items(organization_id, status)
WHERE is_deleted = false;

-- Carry-forward source chain lookup
CREATE INDEX IF NOT EXISTS idx_prep_items_carried_from
ON procohere.meeting_prep_items(carried_from_prep_item_id)
WHERE is_deleted = false AND carried_from_prep_item_id IS NOT NULL;

-- Linked entity lookup (find prep items for a specific task/goal)
CREATE INDEX IF NOT EXISTS idx_prep_items_linked_entity
ON procohere.meeting_prep_items(linked_entity_type, linked_entity_id)
WHERE is_deleted = false AND linked_entity_type IS NOT NULL;

-- Visibility scope index (for RLS-like queries)
CREATE INDEX IF NOT EXISTS idx_prep_items_visibility
ON procohere.meeting_prep_items(organization_id, visibility_scope)
WHERE is_deleted = false;

-- ============================================================
-- PART 4: TRIGGERS
-- ============================================================

-- Updated_at trigger
DROP TRIGGER IF EXISTS tr_meeting_prep_items_set_updated_at ON procohere.meeting_prep_items;
CREATE TRIGGER tr_meeting_prep_items_set_updated_at
    BEFORE UPDATE ON procohere.meeting_prep_items
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- ============================================================
-- PART 5: ROW LEVEL SECURITY
-- ============================================================

ALTER TABLE procohere.meeting_prep_items ENABLE ROW LEVEL SECURITY;

-- Drop existing policies if they exist
DROP POLICY IF EXISTS org_isolation ON procohere.meeting_prep_items;
DROP POLICY IF EXISTS prep_items_visibility ON procohere.meeting_prep_items;

-- Base organization isolation policy
-- Users can only access prep items within their organization
CREATE POLICY org_isolation ON procohere.meeting_prep_items
    FOR ALL
    USING (organization_id = procohere.get_user_organization_id());

-- Visibility policy (more refined - users can see items based on visibility_scope)
-- Note: RLS will filter first by organization, then app layer handles visibility_scope logic
-- Personal: only requester
-- Assigned: requester + assignee
-- Meeting: all attendees (handled by app layer since attendees are in separate table)

-- ============================================================
-- PART 6: GRANTS
-- ============================================================

GRANT SELECT, INSERT, UPDATE, DELETE ON procohere.meeting_prep_items TO authenticated;

-- ============================================================
-- PART 7: COMMENTS
-- ============================================================

COMMENT ON TABLE procohere.meeting_prep_items IS 
    'Pre-meeting preparation items. Supports personal, assigned, and team-wide prep with linked entities and captured responses.';

COMMENT ON COLUMN procohere.meeting_prep_items.requested_by_team_member_id IS 
    'Who created/requested this prep item. Can edit title/body, change assignment.';

COMMENT ON COLUMN procohere.meeting_prep_items.assigned_to_team_member_id IS 
    'Who this prep item is assigned to. Can update status and assignee_notes.';

COMMENT ON COLUMN procohere.meeting_prep_items.visibility_scope IS 
    'Visibility: personal (requester only), assigned (requester + assignee), meeting (all attendees).';

COMMENT ON COLUMN procohere.meeting_prep_items.assignee_notes IS 
    'Notes from the assignee - only assignee can edit this field.';

COMMENT ON COLUMN procohere.meeting_prep_items.overridden_status IS 
    'True if requester manually overrode the status (e.g., marked done on behalf of assignee).';

COMMENT ON COLUMN procohere.meeting_prep_items.carry_forward IS 
    'Whether this item should be carried forward to future meetings if not completed.';

COMMENT ON COLUMN procohere.meeting_prep_items.carried_from_prep_item_id IS 
    'If this was carried forward, points to the original prep item for lineage tracking.';

COMMENT ON COLUMN procohere.meeting_prep_items.source_type IS 
    'Provenance: manual (user-created), scaffold (from template), ai (AI-generated), carry_forward.';

COMMENT ON COLUMN procohere.meeting_prep_items.linked_entity_type IS 
    'Type of linked entity: task, goal, metric, project. Allows prep items to reference specific work items.';

COMMENT ON COLUMN procohere.meeting_prep_items.linked_entity_title_snapshot IS 
    'Cached title of linked entity at link time. Prevents historical drift.';

COMMENT ON COLUMN procohere.meeting_prep_items.prep_prompt IS 
    'Explicit framing of what to think about / prepare. "What blockers exist?" / "Review timeline assumptions"';

COMMENT ON COLUMN procohere.meeting_prep_items.prep_response IS 
    'The actual preparation / thinking captured. This is the cognitive output.';

COMMENT ON COLUMN procohere.meeting_prep_items.prepared_at IS 
    'When the prep was completed (response captured).';

-- ============================================================
-- VERIFICATION QUERIES (run manually)
-- ============================================================

-- Check table exists with all columns:
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'procohere' AND table_name = 'meeting_prep_items'
-- ORDER BY ordinal_position;

-- Check constraints:
-- SELECT conname, contype, pg_get_constraintdef(oid) 
-- FROM pg_constraint 
-- WHERE conrelid = 'procohere.meeting_prep_items'::regclass;

-- Check indexes:
-- SELECT indexname, indexdef
-- FROM pg_indexes
-- WHERE schemaname = 'procohere' AND tablename = 'meeting_prep_items';

-- Check RLS policies:
-- SELECT policyname, permissive, roles, cmd, qual
-- FROM pg_policies
-- WHERE schemaname = 'procohere' AND tablename = 'meeting_prep_items';

-- Test insert (use real IDs from your database):
-- INSERT INTO procohere.meeting_prep_items (
--     organization_id, meeting_id, requested_by_team_member_id,
--     title, visibility_scope, status
-- ) VALUES (
--     'your-org-id', 'your-meeting-id', 'your-team-member-id',
--     'Test prep item', 'personal', 'open'
-- );
