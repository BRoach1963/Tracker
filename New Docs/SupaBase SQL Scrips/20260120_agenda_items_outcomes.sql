-- ============================================================
-- AGENDA ITEMS OUTCOMES MIGRATION
-- Date: 2026-01-20
-- Purpose: Add outcomes tracking and carry-forward support for agenda items
-- ============================================================

-- ============================================================
-- PART 1: ENHANCE MEETING_AGENDA_ITEMS TABLE
-- Add carry-forward tracking columns
-- ============================================================

-- Add anchor person for carry-forward items
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS anchor_team_member_id uuid REFERENCES procohere.team_members(id);

-- Add carry-forward lifecycle state
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS carry_forward_state text DEFAULT NULL;

-- Add expiration tracking
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS carry_forward_expires_at timestamptz;

-- Add meeting count for expiration rule (expires after 2 meetings)
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS carry_forward_meeting_count int DEFAULT 0;

-- Add link to source agenda item (for tracing carry-forward chains)
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS source_agenda_item_id uuid REFERENCES procohere.meeting_agenda_items(id);

-- Constraint for carry-forward states
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_carry_forward_state'
    ) THEN
        ALTER TABLE procohere.meeting_agenda_items
        ADD CONSTRAINT chk_carry_forward_state 
        CHECK (carry_forward_state IS NULL OR carry_forward_state IN (
            'pending', 'surfaced', 'resolved', 'converted', 'expired'
        ));
    END IF;
END $$;

-- Comments for documentation
COMMENT ON COLUMN procohere.meeting_agenda_items.anchor_team_member_id IS 
    'Person this carry-forward is anchored to. Required when status=deferred.';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_state IS 
    'Lifecycle state for carried-forward items: pending, surfaced, resolved, converted, expired.';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_expires_at IS 
    'When this carry-forward expires (30 days from deferral or 2 meetings).';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_meeting_count IS 
    'Number of meeting opportunities since deferral. Expires at 2.';
COMMENT ON COLUMN procohere.meeting_agenda_items.source_agenda_item_id IS 
    'If this item was carried forward, points to the original agenda item.';

-- Index for carry-forward queries
CREATE INDEX IF NOT EXISTS idx_agenda_items_carry_forward
ON procohere.meeting_agenda_items(organization_id, anchor_team_member_id, carry_forward_state)
WHERE is_deleted = false AND carry_forward_state IS NOT NULL;

-- Index for source chain lookup
CREATE INDEX IF NOT EXISTS idx_agenda_items_source
ON procohere.meeting_agenda_items(source_agenda_item_id)
WHERE is_deleted = false AND source_agenda_item_id IS NOT NULL;

-- ============================================================
-- PART 2: CREATE AGENDA_ITEM_OUTCOMES TABLE
-- Track outcomes from agenda item discussions
-- ============================================================

CREATE TABLE IF NOT EXISTS procohere.agenda_item_outcomes (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id     uuid NOT NULL REFERENCES public.organizations(id),
    agenda_item_id      uuid NOT NULL REFERENCES procohere.meeting_agenda_items(id) ON DELETE CASCADE,
    outcome_type        text NOT NULL,
    
    -- For task/goal/meeting outcomes, link to the created entity
    linked_entity_type  text,
    linked_entity_id    uuid,
    
    -- For decision/feedback/notes outcomes, store content inline
    content             text,
    visibility          text NOT NULL DEFAULT 'attendees',
    
    -- Metadata
    created_by          uuid NOT NULL REFERENCES procohere.team_members(id),
    is_deleted          boolean NOT NULL DEFAULT false,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    deleted_at          timestamptz,
    deleted_by          uuid REFERENCES public.users(id)
);

-- Constraint for outcome types
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_outcome_type'
    ) THEN
        ALTER TABLE procohere.agenda_item_outcomes
        ADD CONSTRAINT chk_outcome_type 
        CHECK (outcome_type IN (
            'task_created', 
            'goal_created', 
            'goal_updated', 
            'follow_up_scheduled',
            'decision_recorded', 
            'feedback_captured', 
            'notes_added'
        ));
    END IF;
END $$;

-- Constraint for visibility values
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_outcome_visibility'
    ) THEN
        ALTER TABLE procohere.agenda_item_outcomes
        ADD CONSTRAINT chk_outcome_visibility 
        CHECK (visibility IN ('private', 'attendees', 'team', 'organization'));
    END IF;
END $$;

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_outcomes_agenda_item
ON procohere.agenda_item_outcomes(agenda_item_id) 
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_outcomes_org
ON procohere.agenda_item_outcomes(organization_id) 
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_outcomes_type
ON procohere.agenda_item_outcomes(organization_id, outcome_type) 
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_outcomes_linked_entity
ON procohere.agenda_item_outcomes(linked_entity_type, linked_entity_id) 
WHERE is_deleted = false AND linked_entity_type IS NOT NULL;

-- Trigger for updated_at
DROP TRIGGER IF EXISTS tr_agenda_item_outcomes_set_updated_at ON procohere.agenda_item_outcomes;
CREATE TRIGGER tr_agenda_item_outcomes_set_updated_at
    BEFORE UPDATE ON procohere.agenda_item_outcomes
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- ============================================================
-- PART 3: ROW LEVEL SECURITY
-- ============================================================

ALTER TABLE procohere.agenda_item_outcomes ENABLE ROW LEVEL SECURITY;

-- Drop existing policy if exists
DROP POLICY IF EXISTS org_isolation ON procohere.agenda_item_outcomes;

-- Organization isolation policy
CREATE POLICY org_isolation ON procohere.agenda_item_outcomes
    FOR ALL
    USING (organization_id = procohere.get_user_organization_id());

-- ============================================================
-- PART 4: GRANTS
-- ============================================================

GRANT SELECT, INSERT, UPDATE, DELETE ON procohere.agenda_item_outcomes TO authenticated;

-- ============================================================
-- PART 5: COMMENTS
-- ============================================================

COMMENT ON TABLE procohere.agenda_item_outcomes IS 
    'Records outcomes from agenda item discussions: decisions, feedback, notes, and links to created entities.';

COMMENT ON COLUMN procohere.agenda_item_outcomes.outcome_type IS 
    'Type of outcome: task_created, goal_created, goal_updated, follow_up_scheduled, decision_recorded, feedback_captured, notes_added';

COMMENT ON COLUMN procohere.agenda_item_outcomes.linked_entity_type IS 
    'For entity-creating outcomes (task_created, goal_created, follow_up_scheduled), the type of entity created.';

COMMENT ON COLUMN procohere.agenda_item_outcomes.linked_entity_id IS 
    'For entity-creating outcomes, the ID of the created entity.';

COMMENT ON COLUMN procohere.agenda_item_outcomes.content IS 
    'For content outcomes (decision_recorded, feedback_captured, notes_added), the actual content.';

COMMENT ON COLUMN procohere.agenda_item_outcomes.visibility IS 
    'Who can see this outcome: private (creator only), attendees (meeting attendees), team (creator team), organization (entire org).';

-- ============================================================
-- VERIFICATION QUERIES (run manually to verify)
-- ============================================================

-- Check new columns on meeting_agenda_items:
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_schema = 'procohere' AND table_name = 'meeting_agenda_items'
-- ORDER BY ordinal_position;

-- Check agenda_item_outcomes table:
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_schema = 'procohere' AND table_name = 'agenda_item_outcomes'
-- ORDER BY ordinal_position;

-- Check constraints:
-- SELECT conname, contype, pg_get_constraintdef(oid) 
-- FROM pg_constraint 
-- WHERE conrelid = 'procohere.agenda_item_outcomes'::regclass;
