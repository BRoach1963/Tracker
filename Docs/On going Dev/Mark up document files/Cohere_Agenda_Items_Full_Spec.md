# Cohere – Agenda Items: Model, Outcomes, UI, and Templates

This document defines how agenda items function within Cohere, including their role in meetings, how outcomes are produced, how the UI should behave, and the initial set of agenda item templates. The goal is to preserve ease of use while enabling meaningful structure, traceability, and future AI assistance.

## 1. Core Principles

Agenda items represent discussion topics within a meeting. They do not own work. Instead, they act as connectors between conversations and the objects that may be affected by those conversations.

Agenda items are intentionally lightweight by default and only reveal additional structure when the user chooses to engage with them.

## 2. What Agenda Items Can Link To

An agenda item may optionally link to a single primary entity:
- Task
- Goal
- Milestone
- Metric
- None (pure discussion)

This link provides context only. Agenda items never directly modify linked entities.

## 3. Outcomes Model

Agenda discussions produce outcomes. Outcomes describe what changed as a result of the discussion.

Supported outcome types:
- Task Created
- Goal Created or Updated
- Follow-Up Scheduled
- Decision Recorded
- Feedback Captured
- Notes Only

There is no standalone 'Action Item' object. Any outcome requiring ownership, tracking, or due dates must be represented as a Task.

## 4. Scenario Handling Examples

### Scenario 1: Metric Discussion with No Performance Issue

A manager discusses a dip in an individual’s velocity metric. The discussion reveals temporary personal factors impacting capacity.

Handling:
- Agenda item links to the metric
- Notes capture factual context
- Feedback is recorded
- Decision recorded to temporarily adjust expectations
- Optional task only if operational adjustments are required
- Sensitive notes are permission-scoped

### Scenario 2: Metric Indicates Systemic Issue

A performance metric highlights a broader team issue.

Handling:
- Agenda item links to the metric
- Decision recorded identifying root cause
- One or more tasks created to investigate and remediate
- Optional goal update if targets need adjustment

### Scenario 3: Task Status Green, Quality Red

Tasks appear complete, but defects are increasing.

Handling:
- Agenda item links to a metric or milestone
- Decision recorded to change process
- Tasks created to implement safeguards
- Follow-up agenda item scheduled to review impact

### Scenario 4: Positive Feedback Only

A team member demonstrates exceptional behavior.

Handling:
- Agenda item may be unlinked or linked for context
- Feedback captured
- No task or follow-up required

### Scenario 5: Goal Adjustment

A goal is no longer realistic due to changing conditions.

Handling:
- Agenda item links to the goal
- Decision recorded explaining adjustment
- Goal updated
- Optional task created for the next concrete step

## 5. Agenda Item UI Behavior

Agenda items default to a collapsed, checklist-style view to maintain focus and speed.

Collapsed State:
- Checkbox
- Title
- Optional subtle indicators for linked items, notes, or outcomes

Expanded State (progressive disclosure):
- Linked entity summary (if present)
- Notes tab
- Outcomes tab

## 6. Agenda Item Mockup (Conceptual)

Collapsed:
☐ Sprint status

Expanded:
▾ Sprint status
Linked: Velocity (last 6 weeks)

[ Notes | Outcomes ]

Notes:
Velocity dipped versus baseline. Root cause identified as temporary PTO increase.
Visibility: Manager + Individual

Outcomes:
Decision recorded
Feedback captured
+ Create task
+ Schedule follow-up

## 7. Agenda Item Templates

Templates provide a starting point but remain fully editable. They are intended to reduce setup friction without enforcing rigid structure.

### 1:1 Check-In

Typical items:
- Personal check-in
- Workload / capacity
- Progress on priorities
- Feedback (two-way)
- Follow-ups

### Sprint / Team Status

Typical items:
- Sprint status
- Risks and dependencies
- Blockers
- Upcoming priorities

### Planning Session

Typical items:
- Goals and success criteria
- Scope and constraints
- Ownership and sequencing
- Risks and assumptions

### Retrospective

Typical items:
- What went well
- What didn’t
- What to change
- Action items

### Ad-Hoc / Issue Review

Typical items:
- Context
- Impact
- Options
- Decision / next steps