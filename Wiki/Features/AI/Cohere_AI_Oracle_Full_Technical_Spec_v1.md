ProCohere (Cohere) – Oracle AI: Full Technical Specification (UI-Agnostic)

Version: v1
Date: January 19, 2026
Primary Model Provider (v1): Gemini-first

This document specifies what Oracle should do, how Oracle is implemented, and how data is accessed securely. It intentionally avoids UI design, other than light references to notifications and feedback capture.

# 1. Goals and Non-Goals

Goals

Provide a context-aware assistant (“Oracle”) across Cohere: meetings, agenda items, goals, tasks, metrics, feedback, and Chronicle notes.

Use organization-scoped retrieval and strict permission filters to prevent cross-user leakage.

Support both descriptive coaching and numeric-minded workflows without grading or judgement.

Enable deterministic, repeatable generation via saved prompts, run logs, and stored retrieval evidence.

Be model-provider-agnostic with Gemini as the first provider; allow expansion later without refactoring data access.

Non-Goals (v1)

Custom report builder (ad-hoc report designer).

Always-on background monitoring of every metric in real time (we support scheduled checks and gentle nudges).

Automatic grading of people (“Jim is underperforming”) or prescriptive/disciplinary language.

Training/fine-tuning the underlying model (feedback is stored for product improvement and evaluation, not direct model training by default).

# 2. Core Oracle Capabilities

## 2.1 Meeting and Agenda

Draft an agenda for a meeting using attendees, prior carry-forward items, recent activity, open tasks, active goals, and noteworthy metric movement.

Propose discussion prompts that are gentle, curious, and specific (no judgement).

Generate meeting summaries after the meeting (topics, decisions, action items) and propose follow-ups tied to individuals.

Create downstream artifacts: tasks, goal updates, metric stewardship nudges, feedback drafts, and carry-forward agenda items.

## 2.2 Goals

Summarize goal progress with evidence (what changed) without grading.

Detect lifecycle transitions (“superseded”, “changed”) and ask a gentle confirmation question before applying updates.

Suggest next best actions: refine targets, break down into tasks, schedule a check-in agenda item, or capture a Chronicle note.

## 2.3 Metrics

Explain metric movement descriptively by default; include numeric comparisons when requested or when the user is clearly number-driven.

Detect staleness (no recent values) and recommend stewardship actions (reminders, re-baseline, archive).

Tie metrics to goals wherever possible while still allowing a “Metrics Overview” for visibility and stewardship.

Support both system-derived metrics and manually-entered metrics (with provenance).

## 2.4 Chronicle / Notes / Feedback

Summarize a cluster of notes into themes for a time window (weekly reflection, pre-1:1 prep).

Draft feedback in the user’s tone with adjustable privacy/visibility.

Convert observations into action items (tasks), goal adjustments, or follow-up agenda items.

# 3. Data and Context Assembly

Oracle responses are only as good as context. We assemble context in layers, each with strict scoping rules:

Layer 0: User intent and current page context (meeting/goal/task/metric IDs, attendees, time window).

Layer 1: Organization + team member profile context (roles, reporting chain, preferences).

Layer 2: Directly related entities (meeting agenda items, linked tasks/goals/metrics).

Layer 3: Retrieval (vector search + optional keyword search) over organization-scoped embeddings.

Layer 4: Policies and tone guardrails.

## 3.1 Security and Scoping Rules

All retrieval and entity reads are constrained by organization_id.

ICs cannot see peer content unless explicitly shared; managers can see direct/indirect report content only where app permission rules allow.

Private entities (private agenda items, private notes/feedback) are excluded unless the requesting user is the owner/author or has explicit access.

Never include raw personally sensitive content in model context unless needed; summarize where possible.

Store and return ‘retrieval evidence’ (IDs + snippets) for explainability and debugging.

# 4. Vector Retrieval (pgvector)

Cohere has pgvector installed and an embeddings table already exists: procohere.vector_embeddings. At the moment there is no dedicated match_* RPC in procohere; retrieval can be implemented via a scoped RPC for performance and to centralize security checks.

## 4.1 Existing Table: procohere.vector_embeddings

Known columns (summary):

organization_id (tenant scope)

entity_type (task, goal, note, meeting, team_member, etc.)

entity_id (UUID of the embedded entity)

chunk_index (int, per-entity chunk ordering)

content_hash (dedup)

content_preview (short preview)

content (full text for the chunk)

embedding (vector)

embedding_dimensions (default 1536)

metadata (jsonb)

model_name / model_version

soft delete fields (is_deleted, deleted_at, deleted_by)

created_at / updated_at

## 4.2 Retrieval RPC (Recommended)

Create a Postgres function that accepts: organization_id, requester_team_member_id, optional context (entity_type/entity_id), optional filters, query_embedding, and top_k. It returns chunk rows with distance score plus minimal metadata for evidence. This function should enforce organization and permission checks (or call a permission-check helper).

## 4.3 Chunking, Hashing, and Upsert Rules

Chunk per entity with stable chunk boundaries (sentence/paragraph based). Store chunk_index and content_hash.

Compute content_hash over normalized content (trim, collapse whitespace).

Upsert strategy: (org_id, entity_type, entity_id, chunk_index, model_name) unique in practice; update embedding/content when content_hash changes.

Soft delete embeddings when the source entity is deleted or when the embedding model changes and old embeddings are no longer used.

Keep content_preview small, but keep full content to support evidence and excerpting without re-hitting source tables.

## 4.4 Embedding Model Choices

Gemini-first does not require Gemini embeddings, but it is preferred to keep provider consistency if cost/quality is acceptable. If embeddings are generated by a different provider than the chat model, store model_name/model_version and dimensions explicitly (already supported).

# 5. Execution Architecture

## 5.1 Components

Client app (Avalonia): captures user prompt + current context IDs; displays assistant response; captures feedback.

Supabase PostgREST: CRUD for entities (meetings, goals, tasks, metrics, notes, etc.).

Supabase Edge Functions: orchestrate context assembly, retrieval calls, prompt construction, model call (Gemini), and persistence of AI conversation/messages/insights.

Postgres (Supabase): stores entities + AI artifacts + embeddings; runs retrieval RPC.

## 5.2 Request Flow (Chat / Action)

Client sends: prompt, context_type/context_id (optional), meeting_id (optional), attendee IDs (optional), and feature intent (e.g., ‘draft agenda’).

Edge function resolves requester identity, org_id, and permission scope.

Edge function loads Layer 1–2 context (direct entities).

Edge function generates query_embedding (or uses a cached embedding if prompt repeats).

Edge function calls retrieval RPC for Layer 3 context, constrained by org + permissions.

Edge function builds a structured prompt (system + developer + user + context blocks) with tone guardrails.

Edge function calls Gemini model and receives response + token usage.

Edge function persists: ai_conversation, ai_messages, ai_insights (optional), plus a retrieval_evidence blob for auditability.

Client receives response; optionally receives a list of ‘actions’ to confirm (create tasks, add agenda items, etc.).

## 5.3 Determinism and Debuggability

Persist the prompt template version and retrieval evidence per request.

Persist model_used and temperature/top_p settings per request.

Persist the ‘context pack’ IDs so a run can be replayed during debugging.

Keep AI outputs ‘suggestive’ unless the user confirms creation of tasks/goals/agenda items.

# 6. AI Artifacts in the Schema

From your current schema, the following AI tables exist and are the backbone for Oracle:

procohere.ai_conversations (scoped to org + team_member; optional context_type/context_id).

procohere.ai_messages (messages within a conversation; role, content, tokens_used).

procohere.ai_insights (persisted insights; generated_for team member; optional source_type/source_id).

## 6.1 Recommended Additions (Minimal)

ai_runs (one row per model invocation) to store: model, parameters, latency, tokens, prompt_version, and a retrieval_evidence JSON pointer.

ai_retrieval_evidence (optional separate table) to store chunk IDs + distances + excerpts returned for that run.

# 7. Prompting Standards (Non-bossy, Non-judgemental)

Oracle must default to a coaching tone: curious, specific, and respectful. It should describe observations, ask clarifying questions, and offer options. It should avoid grading people or implying discipline.

## 7.1 Tone Rules

Use ‘I noticed…’ or ‘It looks like…’ and cite the evidence (metric movement, recent events).

Prefer questions over conclusions: ‘What changed?’ ‘Anything outside work affecting bandwidth?’

Avoid labels: ‘lazy’, ‘underperforming’, ‘failing’, ‘bad’.

Avoid authority language for managers: no ultimatums; propose conversation starters.

When numeric output is shown, pair it with plain-language interpretation.

## 7.2 Numeric Output Policy

Default output is descriptive.

Include numeric comparisons when: (a) user explicitly requests numbers, (b) the feature is inherently numeric (metric insight), or (c) user preference indicates numeric-minded behavior.

When numbers are included, show: current value, prior comparable period, delta, and timeframe; never imply a value judgement.

# 8. Feedback and Ratings (Thumbs Up / Down)

Yes—add feedback. Gemini will not automatically ‘learn’ from thumbs-up/down. But capturing this is essential for: (1) identifying what helpfully matched user intent, (2) prompt/routing improvements, (3) safety/tone regressions, (4) evaluating model/provider choices, and (5) building a future internal ranking dataset if desired.

## 8.1 What to Capture

Binary rating: thumbs_up / thumbs_down.

Optional quick reason tags: ‘too vague’, ‘too long’, ‘missed context’, ‘felt judgy’, ‘incorrect’, ‘great summary’, ‘great next steps’.

Optional free-text comment: ‘What was good?’ / ‘What should change?’

Whether the suggestion led to an accepted action (created task, added agenda item).

Context: model_used, prompt_version, conversation_id, message_id, and retrieval evidence pointer.

## 8.2 Proposed Schema (New Table)

Create a feedback table keyed to ai_messages (or ai_runs). Minimal recommended columns:

id (uuid)

organization_id (uuid)

conversation_id (uuid)

message_id (uuid)  -- ai_messages.id

rated_by_team_member_id (uuid)

rating (text)  -- 'up' | 'down'

reasons (jsonb)  -- array of reason tags

comment (text)

created_at (timestamptz)

is_deleted / deleted_at / deleted_by (optional)

## 8.3 Using Feedback

Dashboards (internal): top failure reasons, judgy-tone incidents, hallucination reports.

Routing: if Gemini response quality drops for a feature, route that feature to a different model/provider in the future.

Prompt iteration: compare prompt versions against feedback distribution.

User personalization: if a user frequently rates ‘too many numbers’, adapt to more descriptive; if ‘too vague’, provide more structured next steps.

# 9. Background Jobs and Scheduled Insights

Generate weekly prep packs for managers (optional): open items, goal status, metric staleness, carry-forward agenda items.

Generate a gentle reminder when a metric has no updates for its configured frequency window.

Keep all scheduled output opt-in at org/user settings level where appropriate.

# 10. Implementation Checklist (v1)

Confirm and lock vector_embeddings usage as the single embeddings store for v1.

Add retrieval RPC for top-k vector search with org + permission checks.

Edge function: context assembly + retrieval + Gemini call + persistence.

Persist ai_conversations/ai_messages consistently (with model_used and token usage).

Add ai_message_feedback table and wire thumbs up/down capture.

Add prompt versioning and retrieval evidence persistence for debug/replay.