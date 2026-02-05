
# Circle Metrics – UI & Interaction Specification

## Purpose
Circle Metrics provides a shared, signal-first view of organizational health.
It enables managers and managers-of-managers to detect drift, risk, and progress
without creating dashboards, rankings, or performance pressure.

Circle Metrics exists to drive conversation and alignment, not reporting.

---

## Design Principles
- Signal over numbers
- Trends over snapshots
- Ownership over aggregation
- Conversation over compliance
- One-screen comprehension

---

## Mental Model
Circle answers one question:
“Where should leadership attention go?”

---

## Layout Overview
- Single primary column
- Vertically stacked goal sections
- No horizontal scrolling
- No charts in default view

---

## Goal Section

Each goal renders as a collapsible section.

### Goal Header
Displays:
- Goal name
- Aggregate signal summary (worst-state logic)
- Expand / collapse affordance

Rules:
- Always visible
- No percentages
- No rollups

---

## Metric Row (Default View)

Each metric row displays exactly:
1. Metric name
2. Owner
3. Signal state
4. Trend indicator
5. Last update age

Rules:
- No raw values
- No targets
- No history

---

## Role-Based Grouping

### Manager View
- Goals → Metrics

### Manager-of-Managers View
- Goals → Managers → Metrics

---

## Expansion Behavior

### Expand Metric
Reveals:
- Latest value
- Target (if defined)
- Last update note
- Micro-trend

---

## Editing & Permissions
- Only owners may update metrics
- MoMs are view/comment only

---

## Metrics in Meetings
- At Risk and Off Track metrics suggested in prep
- May be referenced or updated live
- Notes attach to metric update

---

## Anti-Patterns
- Rankings
- Sorting by value
- Heatmaps
- Dashboards

---

## Success Criteria
- 10-second scan for MoMs
- Conversation-first behavior
- Psychological safety

---

## Summary
Circle Metrics surfaces health through signal and trend,
supports leadership alignment,
and avoids metric theater.
