# Me Goals & Metrics

This document defines:

1. A **wireframe-level specification** for the *Me* goals/metrics section
2. A **data contract** for goal/metric signals that promote items into *Briefing*
3. A **validation checklist** for PRs touching *Me*, *Circle*, or *Briefing*

The goal is consistency, clarity, and preventing the same data from appearing in multiple places without transformation.

---

## 1. Wireframe-Level Specification: Me Goals & Metrics

### 1.1 Purpose of the Section

The *Me* goals/metrics section exists to answer:

> What do I own, what does it mean, and what should I do next?

This section is **persistent**, **personal**, and **interpretive**. It is not a dashboard and not a reporting surface.

---

### 1.2 High-Level Layout

The section should be visually simple and scannable.

Recommended structure (top to bottom):

1. Goals Summary
2. Goal Detail Cards (expandable)
3. Metrics Snapshot (embedded within goals)

Avoid a separate, global "metrics area". Metrics should appear *in context* of goals.

---

### 1.3 Goals Summary (Collapsed View)

**Purpose:** Provide a quick orientation without detail.

Each goal row shows:
- Goal title
- Role (Owner / Contributor / Follower)
- Status (On track / At risk / Off track)
- Next meaningful action (short phrase)

Rules:
- No percentages
- No charts
- No historical data

This view answers:
> What goals do I care about, and are any in trouble?

---

### 1.4 Goal Detail Card (Expanded View)

Expanding a goal reveals contextual detail.

**Required sections:**

- **Status & Confidence**
  - Current state
  - Confidence indicator (e.g., On track / At risk)

- **What Changed Recently**
  - Short, human-readable summary

- **Next Actions**
  - Linked tasks
  - Upcoming meetings
  - Required updates

- **Notes / Reflection**
  - Freeform, personal notes

This is where thinking happens.

---

### 1.5 Metrics Within Me

Metrics should never stand alone in Me.

They appear *only* when tied to a goal.

For each metric, show:
- Current value
- Expected range or target
- Direction (up / down / flat)
- Short interpretation

Example:
> Customer satisfaction is trending down and is now below target.

Avoid:
- Raw charts
- Historical timelines
- Peer comparisons

---

## 2. Data Contract: Goal & Metric Signals for Briefing

### 2.1 Concept

Briefing consumes **signals**, not raw goals or metrics.

A signal represents:
> Something changed that requires attention.

---

### 2.2 Signal Eligibility Rules

A signal may be generated only when one or more of the following occur:

- Threshold crossed
- Trend reversal or stall
- Goal status changes to At risk or Off track
- Deadline or checkpoint approaches
- Explicit review/update is due

No signal means no Briefing item.

---

### 2.3 Signal Data Contract

Each signal must include:

- `signal_id`
- `source_type` (goal | metric)
- `source_id`
- `user_id`
- `trigger_reason`
- `severity` (info | warning | critical)
- `summary`
- `recommended_action`
- `created_at`
- `expires_at`

Optional:
- `linked_task_id`
- `linked_meeting_id`

---

### 2.4 Briefing Representation Rules

When rendered in Briefing:

- Show summary and action only
- No charts
- No raw values unless required for action
- One signal = one actionable item

Briefing items must be dismissible or snoozable.

---

## 3. Validation Checklist for PRs

Use this checklist when reviewing PRs that touch *Me*, *Circle*, or *Briefing*.

---

### 3.1 General

- [ ] The same data does not appear unchanged in multiple surfaces
- [ ] Each surface answers its intended question
- [ ] No new dashboards were introduced into Me or Briefing

---

### 3.2 Me-Specific

- [ ] Goals are personal and ownership-based
- [ ] Metrics are tied to goals
- [ ] Metrics are interpreted, not raw
- [ ] No team comparisons are shown

---

### 3.3 Circle-Specific

- [ ] Goals are shared or team-level
- [ ] Metrics show collective state
- [ ] Ownership is visible
- [ ] No personal scratchpad data appears

---

### 3.4 Briefing-Specific

- [ ] Every item is triggered by a signal
- [ ] Every item implies an action
- [ ] No static metrics or goals appear
- [ ] Items are time-scoped and dismissible

---

## 4. Final Sanity Check

If a reviewer cannot answer the following questions, the PR is not done:

- Why does this appear here?
- What is the user expected to do?
- Why is this not shown somewhere else instead?

If those answers are not obvious, the design or implementation needs another pass.

