-- ============================================================================
-- NOTE_LINKS TABLE DDL
-- Clean creation script - no data exists, no migration needed
-- Run this in Supabase SQL Editor
-- ============================================================================

-- Drop existing table (no data to preserve)
DROP TABLE IF EXISTS procohere.note_links;

-- Create table with correct schema
CREATE TABLE procohere.note_links (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id uuid NOT NULL REFERENCES public.organizations(id),
  note_id uuid NOT NULL REFERENCES procohere.notes(id),
  
  -- Entity reference (polymorphic link)
  entity_type text NOT NULL,
  entity_id uuid NOT NULL,
  entity_title_snapshot text,
  
  -- Semantic metadata
  relationship_type text,  -- 'mentioned', 'action_item', 'reference', 'follow_up'
  sort_order smallint DEFAULT 0,
  
  -- Audit (uses team_member identity, not auth.users)
  created_by_team_member_id uuid NOT NULL REFERENCES procohere.team_members(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  
  -- Soft delete
  is_deleted boolean NOT NULL DEFAULT false,
  deleted_at timestamptz,
  deleted_by uuid REFERENCES procohere.team_members(id),
  
  -- Consistency constraint
  CONSTRAINT note_links_soft_delete_consistency
    CHECK ((is_deleted = false AND deleted_at IS NULL) OR (is_deleted = true AND deleted_at IS NOT NULL))
);

-- Indexes
CREATE INDEX ix_note_links_note ON procohere.note_links (note_id) WHERE is_deleted = false;
CREATE INDEX ix_note_links_entity ON procohere.note_links (entity_type, entity_id) WHERE is_deleted = false;
CREATE UNIQUE INDEX ux_note_links_unique_active ON procohere.note_links (note_id, entity_type, entity_id) WHERE is_deleted = false;
CREATE INDEX ix_note_links_purge ON procohere.note_links (deleted_at) WHERE is_deleted = true;
CREATE INDEX ix_note_links_sort ON procohere.note_links (note_id, sort_order) WHERE is_deleted = false;

-- RLS
ALTER TABLE procohere.note_links ENABLE ROW LEVEL SECURITY;

CREATE POLICY note_links_select ON procohere.note_links FOR SELECT
  USING (organization_id = procohere.get_current_organization_id());

CREATE POLICY note_links_write ON procohere.note_links FOR ALL
  USING (organization_id = procohere.get_current_organization_id())
  WITH CHECK (organization_id = procohere.get_current_organization_id());
