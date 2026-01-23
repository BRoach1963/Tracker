ProCohere - Oracle AI Session, Threading, and Feedback

Companion Implementation Spec (v1)

# 1. Session and thread model

One active thread per user (hard limit for v1).

Oracle maintains context only within the current session/thread; new chats are isolated by default.

The user can explicitly start a new thread via a 'New thread' action; optional 'handoff' can copy a short summary into the new thread.

# 2. Persistence

## 2.1 What we store

ai_conversations: one row per active thread; include context_type/context_id when launched from a specific entity.

ai_messages: store messages for quota tracking and debugging; keep payload minimal (no large retrieved chunks).

Optional: store a short, model-generated session summary for handoff (few hundred tokens max).

## 2.2 Retention

Retain ai_conversations and ai_messages for 90 days by default.

Soft-delete on user request; enforce org retention policies via scheduled cleanup.

# 3. Usage tracking

Record model_used per request; record tokens_used when provided by the provider.

Compute per-user and per-org daily/monthly usage counters for quotas.

Show settings-page visibility for AI usage and remaining quota; alert near the AI blob as the user approaches the limit.

# 4. Feedback and ratings (thumbs up/down)

## 4.1 Goals

Capture lightweight feedback to improve quality, routing, and prompt templates.

Do not change Oracle's guardrails based on feedback; guardrails remain fixed.

Negative feedback should ask for a short reason (optional) to make it actionable.

## 4.2 Recommended storage

Create a new table: procohere.ai_message_feedback (message_id, organization_id, team_member_id, rating, reason, created_at).

rating: smallint with values {1 = down, 2 = up}.

reason: short text, optional; store only what the user types (no extra analysis).

Link feedback to ai_messages.id so it can be analyzed per model and per prompt version.

## 4.3 How feedback is used

Operational dashboards: percent positive by model, top failure categories, prompts producing low ratings.

Reranking prompts/templates: prioritize improving prompts or retrieval policies that correlate with low ratings.

Optional future: allow org-specific tuning via prompt templates, not via uncontrolled behavior changes.

# 5. Handoff behavior

If user starts a new thread, offer an optional handoff summary: 'Carry over a short summary of what we were doing?'

If accepted: generate a compact summary (facts + open questions + next step) and store it as the first assistant message in the new thread.

If declined: new thread starts empty; no context is reused.