# Cohere Oracle – Standing Snapshot Algorithm Specification (v1)

This document defines how Oracle determines its operational awareness at any moment in time. The Standing Snapshot represents the authoritative, role-scoped, time-bound view of reality that Oracle is permitted to reason over during an active session.

## 1. Purpose

The Standing Snapshot exists to ensure Oracle is:
- Context-aware without being invasive
- Helpful without being judgmental
- Informed without fabricating certainty

It intentionally avoids long-term conversational memory and instead relies on a repeatable, deterministic snapshot built at request time.

## 2. Core Principles

- Snapshots are rebuilt on demand
- Scope is strictly role-based
- Data freshness is explicit and enforced
- No implicit inference beyond available data
- No cross-user comparisons
- No historical behavioral profiling

## 3. Role-Based Scope Resolution

Oracle determines scope before any data is retrieved.

IC:
- Self-owned goals, tasks, metrics, meetings, feedback, notes

Manager:
- Self-owned data
- Direct reports’ data

Manager of Managers:
- Self-owned data
- Direct reports
- Indirect reports

Oracle must never compare individuals to one another. Aggregation is permitted only at statistical or trend levels.

## 4. Snapshot Data Domains

The snapshot may include:
- Active goals and targets
- Open and recently completed tasks
- Upcoming and recent meetings
- Agenda items and meeting summaries
- Metrics and recent metric values
- Feedback received (respecting visibility)
- AI insights already generated
- Notifications and reminders

Excluded:
- Archived entities
- Deleted entities
- Private feedback not visible to the requester

## 5. Freshness Rules

Default time windows:
- Tasks: open + last 14 days
- Meetings: ±14 days
- Metrics: last 30 days
- Feedback: last 90 days
- Insights: current week only

Staleness begins at 30 days unless overridden by org settings.

## 6. Assembly Algorithm

1. Resolve role and hierarchy
2. Resolve entity permissions via RLS
3. Fetch structured data (Supabase)
4. Fetch vector matches (pgvector)
5. Normalize and timestamp data
6. Produce a bounded State Pack

The State Pack is immutable for the duration of the request.

## 7. Guardrails

Oracle must never:
- Assign blame
- Score people
- Recommend disciplinary action
- Reveal private feedback indirectly
- Infer intent or motivation
- Fabricate certainty from incomplete data

## 8. Output Contract

The Standing Snapshot is never exposed directly to users. It exists solely as internal context to support:
- User-initiated questions
- Weekly insights
- Agenda generation when requested
- Clarification and planning assistance

Version: v1
Status: Locked for implementation