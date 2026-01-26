# 06a – Meetings Domain Tables

This document covers all tables related to meetings in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 13

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | meetings | ✅ MeetingDetail.cs |
| 2 | meeting_series | ❓ TBD |
| 3 | meeting_attendees | ✅ MeetingAttendee.cs |
| 4 | meeting_agenda_items | ✅ MeetingAgendaItem.cs |
| 5 | meeting_agenda_item_links | ✅ MeetingAgendaItemLink.cs |
| 6 | allowed_entity_types | N/A (lookup table) |
| 7 | meeting_agenda_scaffolds | ✅ MeetingAgendaScaffold.cs |
| 8 | meeting_agenda_scaffold_items | ✅ MeetingAgendaScaffoldItem.cs |
| 9 | meeting_prep_items | ✅ MeetingPrepItem.cs |
| 10 | meeting_prep_item_links | ✅ MeetingPrepItemLink.cs |
| 11 | meeting_notes | ✅ MeetingNote.cs |
| 12 | meeting_summaries | ❓ TBD |
| 13 | meeting_templates | ✅ MeetingTemplateDetail.cs |

---

## procohere.meetings

**Purpose**  
Represents a scheduled or completed meeting instance.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| title | text | NO | |
| description | text | YES | |
| meeting_type | text | NO | 'one_on_one', 'team', 'all_hands', etc. |
| status | text | NO | 'scheduled', 'in_progress', 'completed', 'cancelled' |
| scheduled_at | timestamptz | YES | |
| started_at | timestamptz | YES | |
| ended_at | timestamptz | YES | |
| duration_minutes | integer | YES | |
| location | text | YES | |
| video_link | text | YES | |
| recurrence_rule | text | YES | |
| parent_meeting_id | uuid | YES | FK → meetings (self-reference) |
| meeting_series_id | uuid | YES | FK → meeting_series |
| created_by | uuid | NO | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingDetail.cs` ✅ Verified match

**RLS:** Owner (created_by), attendees, and management chain.

---

## procohere.meeting_series

**Purpose**  
Defines recurrence metadata for recurring meetings.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| title | text | NO | |
| meeting_type | text | NO | 'one_on_one', 'team', etc. |
| created_by | uuid | NO | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Disabled – access controlled via meetings.

---

## procohere.meeting_attendees

**Purpose**  
Join table linking team members to meetings with attendance metadata.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| team_member_id | uuid | NO | FK → team_members |
| role | text | NO | 'organizer', 'attendee', 'optional' |
| response_status | text | NO | 'pending', 'accepted', 'declined', 'tentative' |
| attended | boolean | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingAttendee` (in MeetingDetail.cs) ✅ Verified match

**RLS:** Inherited from meeting visibility.

---

## procohere.meeting_agenda_items

**Purpose**  
Individual discussion items on meeting agendas with rich conversation tracking.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| added_by | uuid | NO | FK → team_members |
| title | text | NO | |
| description | text | YES | |
| status | text | NO | 'open', 'discussed', 'action_created', 'deferred', 'dropped' |
| sort_order | integer | NO | |
| is_private | boolean | NO | |
| is_completed | boolean | NO | |
| completed_at | timestamptz | YES | |
| display_title | varchar | YES | Editable display title independent of linked entity |
| shared_context | text | YES | Shared framing visible to all attendees |
| private_context | text | YES | Creator-only thinking space |
| talking_points | jsonb | YES | [{id, text, discussed, order}] |
| outcome_type | varchar | YES | 'discussed', 'decision', 'deferred', 'blocked' |
| outcome_summary | text | YES | |
| visibility_scope | varchar | YES | 'meeting' or 'personal' |
| linked_entity_title_snapshot | varchar | YES | Cached linked entity title |
| discussed_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingAgendaItem` (in MeetingDetail.cs) ✅ Verified match (after fix)

**RLS:** Meeting visibility OR creator visibility.

---

## procohere.allowed_entity_types

**Purpose**  
Lookup table defining valid entity types for linking. Data-driven approach avoids ALTER TABLE when adding new types.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| entity_type | text | NO | PK |
| is_active | boolean | NO | Default `true`. Inactive types cannot be used for new links |
| sort_order | integer | NO | Default `0`. For UI picklist ordering |
| created_at | timestamptz | NO | Default `now()` |

**Seed Data:**
| entity_type | is_active | sort_order |
|-------------|-----------|------------|
| `task` | true | 10 |
| `goal` | true | 20 |
| `metric` | true | 30 |
| `project` | true | 40 |

**Grants:**
```sql
GRANT SELECT ON TABLE procohere.allowed_entity_types TO authenticated;
REVOKE ALL ON TABLE procohere.allowed_entity_types FROM anon;
```

**Usage:** 
- RPCs validate `is_active = true` before allowing links
- UI can fetch for dynamic picklists
- Add new types with simple INSERT (no schema change)

---

## procohere.meeting_agenda_item_links

**Purpose**  
Links agenda items to other entities (goals, tasks, metrics). Constrained to one reference link per agenda item.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_agenda_item_id | uuid | NO | FK → meeting_agenda_items |
| link_kind | text | NO | Constrained to `'reference'` only (CHECK) |
| entity_type | text | NO | FK → allowed_entity_types |
| entity_id | uuid | NO | FK to linked entity |
| entity_title_snapshot | varchar | YES | Cached title for display |
| created_at | timestamptz | NO | |

**Constraints:**
```sql
-- Only 'reference' link kind allowed
CONSTRAINT ck_meeting_agenda_item_links_link_kind
CHECK (link_kind IN ('reference'))

-- Entity type validated by FK to lookup table
CONSTRAINT fk_meeting_agenda_item_links_entity_type
FOREIGN KEY (entity_type) REFERENCES procohere.allowed_entity_types(entity_type)

-- One reference link per agenda item (unique index)
CREATE UNIQUE INDEX uq_meeting_agenda_item_links_reference
ON procohere.meeting_agenda_item_links (organization_id, meeting_agenda_item_id, link_kind);
```

**Trigger (optional - enforces is_active):**
```sql
CREATE TRIGGER trg_validate_active_entity_type
BEFORE INSERT OR UPDATE OF entity_type
ON procohere.meeting_agenda_item_links
FOR EACH ROW
EXECUTE FUNCTION procohere.validate_active_entity_type();
```

**Design Decisions:**
- `link_kind` constrained to `'reference'` via CHECK (simple, unlikely to change)
- `entity_type` enforced via FK to `allowed_entity_types` (data-driven, extensible)
- `is_active` check done in RPC for friendly error messages
- Unique index ensures exactly one reference link per agenda item
- `entity_title_snapshot` synced with `meeting_agenda_items.linked_entity_title_snapshot`

**Note:** This table has NO soft-delete columns. Links are hard-deleted.

**Model:** `MeetingAgendaItemLink.cs`

**RLS:** Inherited from agenda item visibility.

**RPCs:** 
- `upsert_meeting_agenda_item_reference_link` - Create/update the reference link
- `delete_meeting_agenda_item_reference_link` - Remove the reference link

See [07-functions-reference.md](07-functions-reference.md) for full RPC documentation.

---

## procohere.meeting_agenda_scaffolds

**Purpose**  
Pre-built agenda structures that can be applied to meetings.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_type | text | NO | |
| name | text | NO | |
| scope | text | NO | 'system', 'organization', 'personal' |
| created_by | uuid | YES | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Organization-scoped.

---

## procohere.meeting_agenda_scaffold_items

**Purpose**  
Individual items within an agenda scaffold template.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| scaffold_id | uuid | NO | FK → meeting_agenda_scaffolds |
| title | text | NO | |
| description | text | YES | |
| sort_order | integer | NO | |
| default_is_private | boolean | NO | |
| target_kind | text | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Inherited from parent scaffold.

---

## procohere.meeting_prep_items

**Purpose**  
Pre-meeting preparation items supporting personal, assigned, and team-wide visibility.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| requested_by_team_member_id | uuid | NO | FK → team_members (creator) |
| assigned_to_team_member_id | uuid | YES | FK → team_members (assignee) |
| title | text | NO | |
| body | text | YES | |
| assignee_notes | text | YES | |
| assignee_notes_updated_at | timestamptz | YES | |
| visibility_scope | text | NO | 'personal', 'assigned', 'meeting' |
| status | text | NO | 'open', 'in_progress', 'done', 'dismissed' |
| status_updated_at | timestamptz | NO | |
| status_updated_by_team_member_id | uuid | YES | FK → team_members |
| overridden_status | boolean | NO | |
| due_at | timestamptz | YES | |
| completed_at | timestamptz | YES | |
| completed_by_team_member_id | uuid | YES | FK → team_members |
| sort_order | integer | NO | |
| carry_forward | boolean | NO | |
| carried_from_prep_item_id | uuid | YES | FK → self |
| source_type | text | NO | 'manual', 'scaffold', 'ai', 'carry_forward' |
| source_snapshot | jsonb | YES | |
| linked_entity_type | varchar | YES | 'task', 'goal', 'metric', 'project' |
| linked_entity_id | uuid | YES | |
| linked_entity_title_snapshot | varchar | YES | |
| prep_prompt | text | YES | |
| prep_response | text | YES | |
| prepared_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | When soft-deleted |
| deleted_by | uuid | YES | FK → team_members (who deleted) |

**Model:** `MeetingPrepItem.cs` ✅ Verified match

**RLS:** Organization isolation enforced. App layer handles visibility_scope logic.

---

## procohere.meeting_prep_item_links

**Purpose**  
Links prep items to other entities for context.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_prep_item_id | uuid | NO | FK → meeting_prep_items |
| link_kind | text | NO | |
| entity_type | text | NO | |
| entity_id | uuid | NO | |
| created_at | timestamptz | NO | |

**Note:** This table does NOT have soft-delete columns.

**Model:** None (not currently used in app)

**RLS:** Inherited from prep item visibility.

---

## procohere.meeting_notes

**Purpose**  
Notes captured during or after meetings. Notes can be tagged with categories for filtering and organization.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| author_id | uuid | NO | FK → team_members |
| content | text | NO | |
| is_shared | boolean | NO | false = private to author |
| tags | jsonb | YES | Array of tag categories (e.g., `["action", "decision"]`) |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Tag Categories:**
Meeting notes support optional tagging with predefined categories:
| Category | Display Name | Color | Purpose |
|----------|-------------|-------|---------|
| `action` | Action Item | #EF4444 (Red) | Tasks to do |
| `decision` | Decision | #10B981 (Green) | Decisions made |
| `question` | Question | #F59E0B (Amber) | Questions raised |
| `followup` | Follow-up | #8B5CF6 (Purple) | Items to follow up on |
| `blocker` | Blocker | #DC2626 (Dark Red) | Blocking issues |
| `idea` | Idea | #3B82F6 (Blue) | Ideas discussed |
| `risk` | Risk | #F97316 (Orange) | Risks identified |

**Indexes:**
| Index | Type | Purpose |
|-------|------|---------|
| `meeting_notes_tags_gin_idx` | GIN | Supports tag filtering queries with `@>` operator |

**Constraints:**
| Constraint | Type | Purpose |
|------------|------|---------||
| `meeting_notes_tags_valid_chk` | CHECK | Validates tags via `is_meeting_note_tags_valid()` function |

**Model:** `MeetingNote.cs` ✅ Verified match (includes tags property)

**RLS:** Forced RLS; visible via meeting access or author if private.

**Schema Change (2026-01-26):**
```sql
-- 1. Add tags column
ALTER TABLE procohere.meeting_notes
ADD COLUMN IF NOT EXISTS tags jsonb NULL;

-- 2. Create validator function
CREATE OR REPLACE FUNCTION procohere.is_meeting_note_tags_valid(p_tags jsonb)
RETURNS boolean
LANGUAGE sql
STABLE
AS $function$
    select
        p_tags is null
        or (
            jsonb_typeof(p_tags) = 'array'
            and (
                select bool_and(val in ('action','decision','question','followup','blocker','idea','risk'))
                from jsonb_array_elements_text(p_tags) as x(val)
            ) is not false
        );
$function$;

-- 3. Add CHECK constraint
ALTER TABLE procohere.meeting_notes
DROP CONSTRAINT IF EXISTS meeting_notes_tags_valid_chk;

ALTER TABLE procohere.meeting_notes
ADD CONSTRAINT meeting_notes_tags_valid_chk
CHECK (procohere.is_meeting_note_tags_valid(tags));

-- 4. GIN index for filtering
CREATE INDEX IF NOT EXISTS meeting_notes_tags_gin_idx
ON procohere.meeting_notes
USING gin (tags);
```

---

## procohere.meeting_summaries

**Purpose**  
AI-generated meeting summaries.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| summary | text | NO | |
| key_decisions | jsonb | YES | |
| action_items | jsonb | YES | |
| topics_discussed | jsonb | YES | |
| sentiment | text | YES | |
| generated_by | text | YES | AI model identifier |
| is_approved | boolean | NO | |
| approved_by | uuid | YES | FK → team_members |
| approved_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Inherited from meeting visibility.

---

## procohere.meeting_templates

**Purpose**  
Reusable meeting templates with default agendas.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| created_by | uuid | NO | FK → team_members |
| name | text | NO | |
| description | text | YES | |
| meeting_type | text | NO | 'one_on_one', 'team', 'project', 'custom' |
| default_duration | integer | YES | minutes |
| default_agenda | jsonb | YES | agenda items as JSON |
| is_system_template | boolean | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingTemplateDetail.cs` ✅ Verified match (after fix)

**RLS:** Organization-scoped; system templates visible to all.

---
