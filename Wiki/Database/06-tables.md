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

### procohere.vector_embeddings

**Purpose**  
Postgres + pgvector-backed vector store for semantic search (RAG). Stores chunked text and embeddings for both help docs and user data.

**Columns**
- id (uuid, PK, default gen_random_uuid())
- organization_id (uuid, FK → public.organizations.id)
- entity_type (varchar(64), not null) – canonical type (e.g., help_doc, meeting, task, goal)
- entity_id (uuid, not null) – referenced entity id
- chunk_index (integer, not null, default 0) – 0-based chunk ordinal
- content_hash (varchar(64), not null) – stable hash of normalized chunk content
- content_preview (varchar(500), nullable)
- content (text, nullable)
- embedding (vector(768), nullable)
- embedding_dimensions (integer, not null, default 768)
- model_name (varchar(100), not null, default 'text-embedding-004')
- model_version (varchar(50), nullable)
- metadata (jsonb, nullable)
- is_deleted (boolean, default false)
- created_at (timestamptz, default now())
- updated_at (timestamptz, default now())
- deleted_at (timestamptz, nullable)
- deleted_by (uuid, FK → public.users.id, nullable)

**Keys and constraints**
- UNIQUE(organization_id, entity_type, entity_id, chunk_index)
- CHECK(embedding_dimensions = 768)

**Indexes**
- (organization_id, entity_type, entity_id) WHERE is_deleted = false
- HNSW index on embedding (vector_cosine_ops) WHERE is_deleted = false

**Triggers**
- set_updated_at() on UPDATE

**RLS**  
Forced RLS; org-scoped; visibility-preserving. Vector rows are readable only if the user can see the referenced entity (never widens access).


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

**Columns**
- id (uuid, PK, default gen_random_uuid())
- organization_id (uuid, FK → public.organizations.id)
- title (text, not null)
- description (text, nullable)
- meeting_type (text, not null) – 'one_on_one', 'team', 'all_hands', etc.
- status (text, not null) – 'scheduled', 'in_progress', 'completed', 'cancelled'
- scheduled_at (timestamptz, nullable)
- started_at (timestamptz, nullable)
- ended_at (timestamptz, nullable)
- duration_minutes (integer, nullable)
- location (text, nullable)
- video_link (text, nullable)
- recurrence_rule (text, nullable)
- parent_meeting_id (uuid, FK → meetings.id, nullable)
- meeting_series_id (uuid, FK → meeting_series.id, nullable)
- created_by (uuid, FK → team_members.id, not null)
- is_deleted (boolean, not null, default false)
- created_at (timestamptz, not null, default now())
- updated_at (timestamptz, not null, default now())
- deleted_at (timestamptz, nullable)
- deleted_by (uuid, nullable)

**RLS**  
Owner (created_by), attendees, and management chain.

---

### procohere.meeting_agenda_items

**Purpose**  
Agenda entries scoped to a meeting.

**Visibility**  
Meeting visibility OR creator visibility.

---

### procohere.meeting_prep_items

**Purpose**  
Pre-meeting preparation items supporting personal, assigned, and team-wide visibility with linked entities and captured prep responses.

**Columns**
- id (uuid, PK, default gen_random_uuid())
- organization_id (uuid, FK → public.organizations.id)
- meeting_id (uuid, FK → procohere.meetings.id, CASCADE delete)
- requested_by_team_member_id (uuid, FK → procohere.team_members.id) – Creator
- assigned_to_team_member_id (uuid, FK → procohere.team_members.id, nullable) – Assignee
- title (text, not null)
- body (text, nullable)
- assignee_notes (text, nullable) – Only assignee can edit
- visibility_scope (text, default 'personal') – 'personal', 'assigned', 'meeting'
- status (text, default 'open') – 'open', 'in_progress', 'done', 'dismissed'
- status_updated_at (timestamptz, nullable)
- status_updated_by_team_member_id (uuid, FK, nullable)
- overridden_status (boolean, default false)
- due_at (timestamptz, nullable)
- completed_at (timestamptz, nullable)
- completed_by_team_member_id (uuid, FK, nullable)
- sort_order (int, default 0)
- carry_forward (boolean, default false)
- carried_from_prep_item_id (uuid, FK → self, nullable) – Lineage tracking
- source_type (text, nullable) – 'manual', 'scaffold', 'ai', 'carry_forward'
- source_snapshot (text, nullable) – JSON provenance data
- linked_entity_type (text, nullable) – 'task', 'goal', 'metric', 'project'
- linked_entity_id (uuid, nullable)
- linked_entity_title_snapshot (text, nullable) – Cached title at link time
- prep_prompt (text, nullable) – What to think about / prepare
- prep_response (text, nullable) – Captured preparation thinking
- prepared_at (timestamptz, nullable) – When prep was completed
- is_deleted, created_at, updated_at, deleted_at, deleted_by

**Visibility Modes**
- personal: Only visible to the requester
- assigned: Visible to requester AND assignee (requires assigned_to_team_member_id)
- meeting: Visible to all meeting attendees (team prep)

**RLS**  
Organization isolation enforced. App layer handles visibility_scope logic.

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
