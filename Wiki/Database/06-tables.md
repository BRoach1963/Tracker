# 06 – Table Reference (Authoritative)

This document is the **authoritative, exhaustive table dictionary** for the ProCohere database.

It is grounded directly in the extracted schema and RLS metadata contained in `PROCOHERE_DATABASE_TECHNICAL_REFERENCE.md` and supporting dumps.

---

## Conventions

For every table, the following sections are provided:
- Purpose
- Columns (name, type, nullability, default, meaning)
- Keys and constraints
- Indexes
- Triggers
- RLS behavior
- Visibility semantics and notes

No tables are conceptual. Everything here reflects real schema state.

---

## AI Domain

### procohere.ai_conversations

**Purpose**  
Represents a persistent AI interaction thread scoped to a team member and optional context entity.

**Columns**
- id (uuid, PK, default gen_random_uuid()) – Conversation identifier
- organization_id (uuid, FK → public.organizations.id)
- team_member_id (uuid, FK → procohere.team_members.id) – Owner of the conversation
- title (text, nullable)
- context_type (text, nullable) – e.g. meeting, task, goal
- context_id (uuid, nullable)
- model_used (text, nullable)
- is_deleted (boolean, default false)
- created_at (timestamptz, default now())
- updated_at (timestamptz, default now())
- deleted_at (timestamptz, nullable)
- deleted_by (uuid, FK → public.users.id)

**RLS**  
Owner-only visibility via team_member_id.

---

### procohere.ai_messages

**Purpose**  
Stores individual message turns within an AI conversation.

**Columns**
- id (uuid, PK)
- organization_id (uuid)
- conversation_id (uuid, FK → ai_conversations.id)
- role (text) – system/user/assistant
- content (text)
- tokens_used (integer, nullable)
- is_deleted (boolean)
- created_at, updated_at, deleted_at, deleted_by

**RLS**  
Inherited via parent ai_conversations visibility.

---

### procohere.ai_insights

**Purpose**  
AI-generated insights produced for or about a team member.

**Columns**
- id (uuid, PK)
- organization_id (uuid)
- team_member_id (uuid, nullable) – generator context
- generated_for (uuid, FK → team_members.id)
- insight_type (text)
- title (text)
- content (text)
- source_type, source_id
- relevance_score (numeric)
- is_dismissed (boolean)
- dismissed_at (timestamptz)
- lifecycle columns

**RLS**  
Visible to generated_for and their management chain.

---

## Recognition

### procohere.kudos

**Purpose**  
Peer or manager recognition messages.

**Columns**
- id (uuid, PK)
- organization_id (uuid)
- from_member_id (uuid, FK → team_members.id)
- to_member_id (uuid, FK → team_members.id)
- message (text)
- category (text)
- is_public (boolean)
- lifecycle columns

**RLS**  
Visible to sender, recipient, and management chain.

---

## Meetings

### procohere.meeting_series

**Purpose**  
Defines recurrence metadata for meetings.

**RLS**  
Disabled – access controlled via meetings.

---

### procohere.meetings

**Purpose**  
Represents a scheduled or completed meeting instance.

**Visibility**  
Owner (created_by), attendees, and management chain.

---

### procohere.meeting_agenda_items

**Purpose**  
Agenda entries scoped to a meeting. These are **conversation containers** – not simple checklist items. Each agenda item can include shared/private context, structured talking points, and tracked outcomes.

**Columns**
- id (uuid, PK, default gen_random_uuid())
- organization_id (uuid, FK → public.organizations.id)
- meeting_id (uuid, FK → procohere.meetings.id)
- added_by (uuid, FK → procohere.team_members.id) – who added this agenda item
- title (text, not null) – original raw title as entered
- display_title (text, nullable) – optional styled/formatted title for UI
- description (text, nullable) – legacy notes field
- shared_context (text, nullable) – context visible to all meeting participants
- private_context (text, nullable) – context visible only to item creator
- talking_points (jsonb, nullable) – structured array of `{id, text, discussed, order}`
- outcome_type (text, nullable) – constrained to: 'discussed', 'decision', 'deferred', 'blocked'
- outcome_summary (text, nullable) – freeform summary of discussion outcome
- visibility_scope (text, default 'meeting') – constrained to: 'meeting', 'personal'
- linked_entity_type (text, nullable) – e.g. 'goal', 'metric', 'task' (from legacy)
- linked_entity_id (uuid, nullable) – reference to linked entity
- linked_entity_title_snapshot (text, nullable) – denormalized title at time of linking (populated by app on link)
- sort_order (integer)
- status (text, default 'pending') – 'pending', 'in_progress', 'completed', 'deferred'
- is_completed (boolean, default false)
- completed_at (timestamptz, nullable)
- discussed_at (timestamptz, nullable) – auto-set by trigger when outcome_type becomes non-null
- is_private (boolean, default false) – legacy; prefer visibility_scope
- is_deleted (boolean, default false)
- created_at (timestamptz, default now())
- updated_at (timestamptz, default now())
- deleted_at (timestamptz, nullable)
- deleted_by (uuid, FK → public.users.id)

**Constraints**
```sql
-- Outcome type must be a controlled value
ALTER TABLE procohere.meeting_agenda_items
  ADD CONSTRAINT meeting_agenda_items_outcome_type_chk
  CHECK (outcome_type IS NULL OR outcome_type IN ('discussed','decision','deferred','blocked'));

-- Visibility scope must be controlled
ALTER TABLE procohere.meeting_agenda_items
  ADD CONSTRAINT meeting_agenda_items_visibility_scope_chk
  CHECK (visibility_scope IN ('meeting','personal'));

-- Enforce alignment between visibility_scope and legacy is_private flag
ALTER TABLE procohere.meeting_agenda_items
  ADD CONSTRAINT meeting_agenda_items_visibility_alignment_chk
  CHECK (
    (visibility_scope = 'meeting' AND is_private = false)
    OR
    (visibility_scope = 'personal' AND is_private = true)
  );
```

**Triggers**
```sql
-- Auto-set discussed_at when outcome_type becomes non-null
CREATE OR REPLACE FUNCTION procohere.set_discussed_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.outcome_type IS NOT NULL AND OLD.outcome_type IS NULL AND NEW.discussed_at IS NULL THEN
    NEW.discussed_at := now();
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER tr_meeting_agenda_items_set_discussed_at
BEFORE UPDATE ON procohere.meeting_agenda_items
FOR EACH ROW EXECUTE FUNCTION procohere.set_discussed_at();
```

**Indexes**
```sql
-- Filter by outcome presence
CREATE INDEX IF NOT EXISTS idx_meeting_agenda_items_meeting_outcome
ON procohere.meeting_agenda_items (meeting_id, outcome_type)
WHERE is_deleted = false;

-- Filter by visibility scope
CREATE INDEX IF NOT EXISTS idx_meeting_agenda_items_visibility
ON procohere.meeting_agenda_items (meeting_id, visibility_scope)
WHERE is_deleted = false;
```

**Talking Points JSONB Schema**
Each talking point must have a stable UUID for check-off, reordering, and editing:
```json
[
  {"id": "550e8400-e29b-41d4-a716-446655440000", "text": "Point to discuss", "discussed": false, "order": 0},
  {"id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8", "text": "Another point", "discussed": true, "order": 1}
]
```

**Outcome Types**
| Value | Meaning |
|-------|---------|
| discussed | Topic was discussed, no specific outcome |
| decision | A decision was made |
| deferred | Moved to future meeting |
| blocked | Cannot proceed, waiting on something |

**Snapshot Population**  
The `linked_entity_title_snapshot` field must be populated by app code when linking. This prevents historical drift when entity titles change. The DB does not auto-populate this.

**RLS**  
Visibility determined by visibility_scope:
- `meeting`: visible to all meeting attendees
- `personal`: visible only to added_by

---

### procohere.meeting_prep_items

**Purpose**  
Pre-meeting preparation items with optional AI-assisted prep prompts and linked context. Supports carrying forward incomplete items between meetings.

**Columns**
- id (uuid, PK, default gen_random_uuid())
- organization_id (uuid, FK → public.organizations.id)
- meeting_id (uuid, FK → procohere.meetings.id)
- requested_by_team_member_id (uuid, FK → procohere.team_members.id) – who requested this prep
- assigned_to_team_member_id (uuid, FK → procohere.team_members.id, nullable)
- title (text, not null)
- body (text, nullable) – detailed description
- source_type (text, nullable) – where this came from: 'manual', 'ai_suggested', 'carried_forward'
- source_snapshot (text, nullable) – context snapshot at creation time
- linked_entity_type (text, nullable) – e.g. 'goal', 'metric', 'task', 'contact'
- linked_entity_id (uuid, nullable) – reference to linked entity
- linked_entity_title_snapshot (text, nullable) – denormalized title at time of linking (populated by app on link)
- prep_prompt (text, nullable) – AI prompt to help prepare this item
- prep_response (text, nullable) – stored AI-generated prep content
- prepared_at (timestamptz, nullable) – auto-set by trigger when prep_response is filled
- visibility_scope (text, default 'meeting') – constrained to: 'meeting', 'personal', 'assigned'
- status (text, default 'pending') – 'pending', 'in_progress', 'completed'
- overridden_status (text, nullable) – manual override of status
- due_at (timestamptz, nullable) – when prep should be ready
- sort_order (integer)
- assignee_notes (text, nullable) – notes from assigned person
- carry_forward (boolean, default false) – should this carry to next meeting if incomplete
- carried_from_prep_item_id (uuid, FK → meeting_prep_items.id, nullable) – lineage tracking
- completed_at (timestamptz, nullable)
- completed_by_team_member_id (uuid, FK → procohere.team_members.id, nullable)
- status_updated_at (timestamptz, nullable)
- status_updated_by_team_member_id (uuid, FK → procohere.team_members.id, nullable)
- is_deleted (boolean, default false)
- created_at (timestamptz, default now())
- updated_at (timestamptz, default now())
- deleted_at (timestamptz, nullable)
- deleted_by (uuid, FK → public.users.id)

**Constraints**
```sql
-- Visibility scope must be controlled
ALTER TABLE procohere.meeting_prep_items
  ADD CONSTRAINT meeting_prep_items_visibility_scope_chk
  CHECK (visibility_scope IN ('meeting','personal','assigned'));
```

**Triggers**
```sql
-- Auto-set prepared_at when prep_response gets filled
CREATE OR REPLACE FUNCTION procohere.set_prepared_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.prep_response IS NOT NULL AND COALESCE(NEW.prep_response,'') <> '' AND NEW.prepared_at IS NULL THEN
    NEW.prepared_at := now();
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER tr_meeting_prep_items_set_prepared_at
BEFORE UPDATE ON procohere.meeting_prep_items
FOR EACH ROW EXECUTE FUNCTION procohere.set_prepared_at();
```

**Indexes**
```sql
-- Filter by preparation status
CREATE INDEX IF NOT EXISTS idx_meeting_prep_items_prepared
ON procohere.meeting_prep_items (meeting_id, prepared_at)
WHERE is_deleted = false;
```

**Linked Entity Support**
Prep items can link to:
- Goals (procohere.goals)
- Metrics (procohere.metrics)
- Tasks (procohere.tasks)
- Contacts (procohere.contacts)

**Snapshot Population**  
The `linked_entity_title_snapshot` field must be populated by app code when linking. This prevents historical drift when entity titles change. The DB does not auto-populate this.

**AI Prep Flow**
1. User or AI suggests prep_prompt
2. AI generates prep_response
3. `prepared_at` timestamp auto-set by trigger
4. Response displayed in prep UI

**Visibility Modes**
| Scope | Meaning |
|-------|---------|
| meeting | All meeting attendees |
| personal | Only requested_by_team_member_id |
| assigned | assigned_to + requested_by |

**RLS**  
Enforces ownership/assignment based on visibility_scope; UI may further filter.

---

### procohere.meeting_notes

**Purpose**  
Notes captured during or after meetings.

**RLS**  
Forced RLS; visible only via meeting access.

---

### procohere.meeting_summaries

**Purpose**  
AI-generated meeting summaries.

**RLS**  
Inherited from meeting visibility.

---

## Goals & Metrics

### procohere.goals

**Purpose**  
Hierarchical objectives owned by a team member.

**Columns** include owner_id, parent_goal_id.

**Visibility**  
Owner and management chain.

---

### procohere.metrics

**Purpose**  
Quantitative or qualitative measurements.

**Visibility**  
Owner OR visible team member.

---

### procohere.goal_metrics

**Purpose**  
Many-to-many join between goals and metrics.

**RLS**  
Forced; requires visibility to both goal and metric.

---

## Surveys

### procohere.surveys

**Purpose**  
Survey definitions.

### procohere.survey_questions
### procohere.survey_responses
### procohere.survey_answers

Visibility follows survey ownership and respondent rules.

---

## Teams & Competencies

### procohere.teams

**Purpose**  
Logical team groupings, distinct from management hierarchy.

---

### procohere.competencies
### procohere.team_member_competencies
### procohere.development_plans
### procohere.development_plan_items

Used for growth and performance tracking.

---

## Calendar Integration

### procohere.calendar_integrations

**Purpose**  
OAuth tokens and sync metadata.

**RLS**  
Forced; owner-only.

---

## Cross-Cutting

### procohere.attachments
### procohere.comments
### procohere.entity_tags
### procohere.tags
### procohere.notifications
### procohere.audit_log

All inherit visibility from parent entity.

---

## Invariants

- Linking entities never widens visibility
- UI visibility may exceed RLS visibility
- RLS is always authoritative

---

End of Table Reference
