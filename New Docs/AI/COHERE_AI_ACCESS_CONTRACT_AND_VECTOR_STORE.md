# Cohere AI: Capabilities, Data Access Contract, and Vector Store Definition
_Last updated: 2026-01-22_

This document defines what **Cohere** is responsible for, how the product calls it, how Cohere is allowed to read data, and how Cohere stores/retrieves embeddings for **documentation** and **user data**.

It is written to be implementation-ready for:
- In-app chat (instructional help)
- In-context “Suggest” (agenda + prep prompts)
- Insights generation (startup popup + later surface)
- Action execution (create meeting, create tasks, etc.)

---

## 1. Cohere: What it is (product definition)

Cohere is the AI layer inside Pro Cohere. It is intentionally **optional** and **non-coercive**:
- It suggests; it does not enforce.
- It is context-aware; it does not hallucinate missing facts.
- It respects visibility and never broadens access beyond what the user already has.

Cohere operates in four modes:

### 1.1 Instructional Help Chat
**Purpose:** Explain product functionality, workflows, and “how do I…” questions.

**Primary grounding:** vectorized help/reference documentation.

**Examples:**
- “How do I link a metric to a goal?”
- “What does carry-forward do for prep items?”

### 1.2 In-Context Suggestions (UI “Suggest”)
**Purpose:** Generate suggestion lists and structured drafts while the user is on a specific screen.

This includes:
- **Meeting agenda suggestions** based on meeting type, attendees, linked entities, and recent activity.
- **Assigned prep prompt suggestions** tailored to the assignee and meeting purpose.
- **Contextual drafts** that look like the “shape” of your UI objects (agenda items with talking points, prep prompts, etc.).

**Key requirement:** The UI provides screen context so Cohere knows where it is.

### 1.3 Insights Engine
**Purpose:** Generate insights from user data (and optionally enrich wording with AI) for display:
- on app launch (popup)
- and later in a dedicated insights surface

Insights are stored as data objects (not just chat text).

### 1.4 Action Agent
**Purpose:** Execute operations when the user asks for them.

Examples:
- “Create a 1:1 for me and Janet next week, add suggested agenda items.”
- “Create a task to follow up on the KPI gap.”
- “Schedule a meeting and assign prep to the attendees.”

Actions must be executed through server-side functions (RPC / APIs), not ad-hoc SQL.

---

## 2. Cohere High-Level Flow

Every Cohere request follows the same high-level pipeline, with different “tools” enabled depending on mode.

1) Receive request (chat message or UI command)  
2) Build **request context** (screen + user + entity references)  
3) Retrieve grounding context:
   - Documentation RAG (help docs)
   - Data context (targeted structured reads and/or semantic retrieval)
4) Optionally call actions (“tools”) if the user asked Cohere to do something  
5) Return response payload (text, suggestions, or action results)

---

## 3. Data Access Contract

### 3.1 Non-negotiables
1) **Never broaden visibility.** If the user cannot see the underlying entity, Cohere cannot retrieve or use it.
2) **All access is tenant/org scoped.**
3) Cohere is allowed to read only through:
   - existing RLS-protected table access, and/or
   - explicit RPC functions that enforce RLS/organization boundaries

### 3.2 Context payload (the UI must send this)
Every Cohere invocation includes a small “screen context” payload.

Minimum payload:
```json
{
  "feature_area": "meetings|tasks|goals|metrics|feedback|me",
  "screen_entity_type": "meeting|task|goal|metric|team_member|none",
  "screen_entity_id": "uuid-or-null",
  "organization_id": "uuid",
  "current_user_team_member_id": "uuid",
  "ui_state": {
    "meeting_type": "planning|1on1|team|...",
    "selected_attendee_team_member_ids": ["uuid", "..."],
    "linked_entities": [
      { "type": "goal|metric|task|project|...", "id": "uuid" }
    ]
  }
}
```

Notes:
- If the user is on a meeting screen, `screen_entity_type=meeting` and `screen_entity_id` must be provided.
- For “Suggest agenda” / “Suggest prep”, include meeting type and attendees (or Cohere has to re-fetch them).

### 3.3 Read models Cohere is allowed to request
Cohere may request:
- meeting header + attendees
- agenda items + prep items
- tasks (especially “open/overdue/recently updated”)
- goals/metrics/projects that are linked or relevant to the meeting/attendees
- a narrow slice of recent activity (time-window limited)
- documentation chunks (help files)

Cohere must avoid “dumping the org.” It should fetch targeted subsets using intent detection and multi-hop querying.

### 3.4 Action models Cohere is allowed to execute
All actions are implemented as RPC/API calls. Cohere never writes SQL.

Minimum action toolset:
- `CreateMeeting`
- `UpdateMeeting`
- `AddAgendaItemsToMeeting`
- `AddPrepItemsToMeeting`
- `CreateTask`
- `LinkEntities`
- `MarkPrepPrepared`
- `MarkAgendaDiscussed`
- `RecordInsightDismissal`

Each action returns:
- created/updated entity IDs
- any computed defaults applied server-side
- validation errors (typed)

---

## 4. Response Contracts by Mode

### 4.1 Help Chat Response
```json
{
  "mode": "help_chat",
  "answer_markdown": "string",
  "citations": [
    { "doc_id": "help/meetings.md", "section": "Agenda items", "chunk_id": "uuid" }
  ],
  "followups": ["string", "string"]
}
```

### 4.2 Suggest Agenda Items Response
Agenda items are conversation starters and need depth (not flat titles).

```json
{
  "mode": "suggest_agenda",
  "meeting_id": "uuid",
  "suggestions": [
    {
      "display_title": "string",
      "shared_context": "string|null",
      "private_context": "string|null",
      "talking_points": [
        { "text": "string", "discussed": false, "order": 1 }
      ],
      "visibility_scope": "meeting|personal",
      "linked_entities": [
        { "type": "goal|metric|task|project", "id": "uuid", "title_snapshot": "string" }
      ]
    }
  ]
}
```

### 4.3 Suggest Assigned Prep Response
Prep items are prompts designed to help someone show up prepared.

```json
{
  "mode": "suggest_prep",
  "meeting_id": "uuid",
  "assigned_to_team_member_id": "uuid",
  "suggestions": [
    {
      "title": "string",
      "prep_prompt": "string",
      "visibility_scope": "meeting|individual",
      "linked_entities": [
        { "type": "goal|metric|task|project", "id": "uuid", "title_snapshot": "string" }
      ]
    }
  ]
}
```

Visibility nuance:
- If `visibility_scope='individual'` and the prep item is assigned, the **assignee and assignor** should see it.

### 4.4 Insights Response
```json
{
  "mode": "insights",
  "insights": [
    {
      "insight_type": "string",
      "title": "string",
      "summary": "string",
      "severity": "low|medium|high",
      "entities": [{ "type": "task|goal|metric|team_member|meeting", "id": "uuid" }],
      "recommended_actions": [
        { "action": "CreateTask|CreateMeeting|LinkEntity", "payload": {} }
      ]
    }
  ]
}
```

### 4.5 Action Execution Response
```json
{
  "mode": "action",
  "tool_calls": [
    { "tool": "CreateMeeting", "status": "ok|error", "result": {} }
  ],
  "final_markdown": "string"
}
```

---

## 5. Vector Store Definition (Docs + User Data)

### 5.1 Why one table works
You can store both documentation and user data embeddings in a single table because:
- `entity_type` distinguishes the domain
- `metadata` distinguishes source details
- org scoping + RLS filters ensure multi-tenancy and visibility boundaries

### 5.2 Extension
```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

### 5.3 Table: `procohere.vector_embeddings`
This schema matches the architecture direction of:
- pgvector-backed storage
- HNSW index for ANN search
- multi-tenant organization scoping
- chunking via `chunk_index`

```sql
CREATE TABLE IF NOT EXISTS procohere.vector_embeddings
(
  id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id       uuid NOT NULL REFERENCES public.organizations(id),

  entity_type           varchar(50) NOT NULL,
  entity_id             uuid NOT NULL,
  chunk_index           integer NOT NULL DEFAULT 0,

  content_hash          varchar(64) NOT NULL,
  content_preview       varchar(500),
  content               text,

  embedding             vector(768),
  embedding_dimensions  integer NOT NULL DEFAULT 768,

  model_name            varchar(100) NOT NULL DEFAULT 'text-embedding-004',
  model_version         varchar(50),

  metadata              jsonb,

  created_at            timestamptz NOT NULL DEFAULT now(),
  updated_at            timestamptz NOT NULL DEFAULT now(),
  is_deleted            boolean NOT NULL DEFAULT false,
  deleted_at            timestamptz,
  deleted_by            uuid REFERENCES public.users(id),

  UNIQUE (organization_id, entity_type, entity_id, chunk_index)
);
```

### 5.4 Indexes
HNSW for similarity search, plus partial indexes for filtering.

```sql
CREATE INDEX IF NOT EXISTS idx_vector_embeddings_embedding
ON procohere.vector_embeddings
USING hnsw (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_vector_embeddings_entity_type
ON procohere.vector_embeddings (organization_id, entity_type)
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_vector_embeddings_entity
ON procohere.vector_embeddings (organization_id, entity_type, entity_id)
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_vector_embeddings_metadata_gin
ON procohere.vector_embeddings
USING gin (metadata)
WHERE is_deleted = false;
```

### 5.5 Entity type conventions
Use `entity_type` values that are stable and easy to filter:
- `doc_help` (documentation chunks)
- `team_member`
- `meeting`
- `meeting_agenda_item`
- `meeting_prep_item`
- `task`
- `goal`
- `metric`
- `project`
- `feedback`
- `insight` (optional, if you want “insight history” searchable)

### 5.6 Metadata conventions
Metadata is where you store “what this chunk is” without bloating `entity_type`.

Docs chunk example:
```json
{
  "source": "help_docs",
  "doc_id": "Resources/Help/meetings.md",
  "header_path": "Meetings > Agenda Items",
  "version": "git_sha_or_semver"
}
```

User data chunk example:
```json
{
  "source": "user_data",
  "fields": ["title", "notes", "status", "due_at"],
  "time_window": "last_90_days",
  "indexed_at": "2026-01-22T00:00:00Z"
}
```

### 5.7 Chunking rules
Documentation chunking:
- split by markdown headers (##/###), then paragraphs
- max chunk size ~500 chars
- overlap ~50 chars
- minimum chunk size ~100 chars

User data chunking:
- one entity can become 1..N chunks depending on size
- chunk boundaries should preserve meaning (title + key fields + recent notes)
- keep a consistent “narrative template” per entity type

### 5.8 Store/write contract
Cohere does not write embeddings from the client as the end user. Indexing is performed by:
- a server-side worker/service role, or
- a privileged internal process

Write operations:
- Upsert (by unique org/type/id/chunk) when content_hash changes
- Mark is_deleted when an entity is deleted
- Re-index selectively when an entity changes

### 5.9 Search contract
Search always applies:
1) `organization_id` filter
2) `entity_type` filter (optional but recommended)
3) `is_deleted=false`
4) similarity cutoff
5) RLS visibility checks (see section 6)

---

## 6. RLS and Visibility (Vector Store Safety)

### 6.1 Principle
Embeddings must be readable only when the underlying entity is readable.

The easiest safe pattern is:
- Vector table has its own RLS
- Policy calls a helper function that checks visibility by running an `EXISTS` query against the underlying entity table
- Because those underlying tables already have RLS, the check cannot “see through” permissions

### 6.2 RLS on vector_embeddings
```sql
ALTER TABLE procohere.vector_embeddings ENABLE ROW LEVEL SECURITY;
```

Read policy (authenticated):
- requires org match
- requires entity visibility check
- requires not deleted

Write policy:
- service role only

---

## 7. Minimum RPC Surface (server-side)

### 7.1 Read RPCs
- `GetMeetingContext(meeting_id)`
- `GetAttendeeContext(team_member_ids[], time_window)`
- `GetLinkedEntities(entity_refs[])`
- `GetRecentActivity(team_member_id, time_window)`

These allow Cohere to build suggestions without pulling massive data blocks.

### 7.2 Write RPCs (actions)
- `CreateMeeting(...)`
- `CreateTask(...)`
- `AddAgendaItems(meeting_id, items[])`
- `AddPrepItems(meeting_id, items[])`
- `LinkEntities(...)`

---

## 8. Implementation Notes (what to build first)

Recommended sequencing:
1) Docs vector store + Help Chat (already your baseline)
2) Screen-context payload + “Suggest agenda” + “Suggest prep” using structured reads first
3) Insights generation (stored objects)
4) Then extend vector store to user data for richer semantic retrieval across historical text

---

## 9. Done Definition / Acceptance Criteria

You can consider Cohere “aligned and working” when:

Help:
- Answers “how do I…” using doc chunks and cites the right doc sections.

Suggest:
- “Suggest agenda” produces agenda items with shared_context + talking_points (not just titles).
- “Suggest prep” produces prompts that are specific to assignee + meeting type.
- Suggestions are optional and editable.

Insights:
- Startup popup shows insights sourced from real data.
- Insights are stored and viewable later.

Actions:
- “Create a 1:1 with Janet with suggested agenda items” creates the meeting and attaches agenda items using server-side functions.
- No direct SQL is emitted by the model.

Security:
- Cohere cannot retrieve embeddings for entities the user cannot access.
- Org boundaries are enforced everywhere.

