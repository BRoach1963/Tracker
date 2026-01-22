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
Agenda entries scoped to a meeting.

**Visibility**  
Meeting visibility OR creator visibility.

---

### procohere.meeting_prep_items

**Purpose**  
Pre-meeting preparation items.

**Visibility Modes**
- personal
- assigned
- meeting-scoped

RLS enforces ownership/assignment; UI may further filter.

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
