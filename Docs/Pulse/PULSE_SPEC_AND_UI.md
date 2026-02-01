
# Pulse – Specification & UI Design

## Purpose
Pulse is the synthesis layer of the product.

It is not a dashboard, not a report, and not another task list.

Pulse answers three questions:
1. What changed?
2. What needs attention?
3. What story is emerging across goals, metrics, meetings, and actions?

Pulse exists to reduce cognitive load by connecting signals, not presenting raw data.

---

## Core Principles
- Derived, never manually edited
- Time-scoped
- Signal over volume
- Role-aware
- No fake progress

---

## Data Inputs
- Metrics (signals, trends, staleness)
- Goals (intent + derived health)
- Meetings (agenda-item links)
- Tasks (source-linked actions)

Pulse has no tables of its own.

---

## Pulse Time Window
- IC: 7 days
- Manager: 14 days
- Manager-of-managers: 30 days

---

## UI Structure

### 1. Attention Required
Immediate intervention signals only.

Triggers:
- Metric Off Track
- Metric At Risk + degrading
- Repeated goal degradation
- Stale critical metrics

UI:
- Signal dot
- Entity badge
- One-line explanation
- CTA

Max 5 items.

---

### 2. What Changed
Awareness without alarm.

Shows:
- Threshold crossings
- Trend inflections
- Task completions from discussion
- Goal health changes

---

### 3. Recent Discussions
Narrative continuity.

Derived from linked agenda items.

Grouped by entity.

---

### 4. Actions Taken
Reinforce follow-through.

Derived from tasks sourced from goals or agenda items.

---

## Role Differences
IC: personal focus
Manager: team aggregation
MoM: rollups only, explicit drill-down

---

## Escalation Rules
Escalate only on persistence, not noise.

---

## UI Layout
Single column
Card-based
Icons only
Restricted color palette

---

## What Pulse Is Not
- Not a task list
- Not analytics
- Not editable

---

## Performance
Org-scoped queries
Read-only
Existing indexes only

---

## Implementation Notes
No Pulse tables
Derived in ViewModels or RPCs
Graceful empty states

---

## Summary
Pulse connects:
Data → Signals → Conversation → Action
