# Cohere Oracle – State Pack Generation & Thread Handoff Specification (v1)

This document defines how Oracle assembles contextual state for a session, how that state is constrained, and how controlled handoff between threads is handled. The design prioritizes clarity, freshness, and minimal retention.

## Core Constraints

• One active AI thread per user at any time

• Session-scoped context only (no long-term conversational memory)

• 90-day retention on stored thread metadata

• No automatic cross-thread leakage

## State Pack Definition

A State Pack is a transient, structured snapshot assembled at invocation time. It represents what Oracle is allowed to know for the current request.

Included signal types:

• Role and scope (IC, Manager, Manager-of-Managers)

• Recent meetings, agenda items, tasks, goals, metrics

• Active alerts, reminders, and deadlines

• Recent AI insights generated for the user

Excluded by default:

• Historical conversations

• Private feedback not authored by the user

• Comparative or cross-user analytics

## Thread Lifecycle

Each user has exactly one active thread. A thread begins when Oracle is first invoked and ends when the session expires or the user explicitly starts a new thread.

## Thread Handoff

When a user requests a new thread, Oracle offers an optional handoff. The user may choose to carry forward a summarized State Pack or start with a clean slate.

Handoff options:

• Fresh start (no carried context)

• Summary-only carryover (no raw data)

## Storage Model

Only minimal metadata is persisted: thread id, user id, organization id, model used, timestamps, and feedback signals. Conversation content is not reused for future inference.

## Failure & Reset Behavior

If state assembly fails or data is incomplete, Oracle degrades gracefully, acknowledging uncertainty and requesting clarification.

This specification ensures continuity without memory creep or risk accumulation.