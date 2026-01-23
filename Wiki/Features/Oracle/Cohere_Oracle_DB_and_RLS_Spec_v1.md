# Cohere Oracle – Database & RLS Specification (v1)

This document defines the database structures, retention rules, and Row Level Security (RLS) requirements needed to support Oracle safely. It formalizes how AI-related state is persisted without introducing behavioral memory, surveillance risk, or cross-user leakage.

## Design Goals

• Minimal persistence

• Strong tenant isolation

• Role-scoped visibility

• Auditable but non-intrusive

## Core Tables In Scope

Existing tables leveraged by Oracle:
- procohere.ai_conversations
- procohere.ai_messages
- procohere.ai_insights
- procohere.vector_embeddings
- procohere.notifications (read-only)

## AI Thread Model

Each user has at most one active ai_conversation row at a time. Conversation records store metadata only and are never reused as context for future sessions beyond the active thread.

Required fields:

• organization_id

• team_member_id

• model_used

• created_at / updated_at

• is_deleted

## Message Storage Rules

ai_messages may store raw content for the duration of the active thread. Messages are soft-deleted when a thread ends or after 90 days, whichever comes first.

## AI Insights Persistence

ai_insights represent Oracle speaking asynchronously. Insights are treated as first-class entities and may be surfaced in UI timelines.

Constraints:

• Never comparative

• Never evaluative

• Always role-scoped

• Always dismissible

## Feedback & Ratings

User feedback (thumbs up/down, optional explanation) is stored separately from messages and is never used to alter guardrails.

Feedback usage:

• Aggregate quality analysis

• Prompt tuning (global)

• Model selection decisions

## Row Level Security (RLS)

All AI-related tables enforce:
- organization_id = auth.org_id()
- team_member_id = auth.team_member_id() (where applicable)
- Manager visibility extends only to reports, never peers

## Retention Policy

• Active thread: session duration

• Metadata: 90 days

• Embeddings: tied to source entity lifecycle

• Feedback: retained for aggregate analysis

## Audit & Compliance

Oracle activity is auditable via ai_conversations and ai_insights without storing long-term behavioral history.

This model ensures Oracle remains helpful without becoming a surveillance system.