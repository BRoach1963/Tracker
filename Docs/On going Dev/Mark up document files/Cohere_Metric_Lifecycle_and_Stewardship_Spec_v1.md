# Cohere – Metric Lifecycle and Stewardship Specification (v1)

This document defines the lifecycle model and stewardship rules for Metrics in Cohere. Metrics are treated as observational signals that support human sensemaking. They are not evaluative instruments and do not determine outcomes.

## 1. Purpose of the Metric Lifecycle

The metric lifecycle exists to answer a single question:

'Is this signal still meaningful, and should we be paying attention to it?'

Metric lifecycle does not represent progress, success, or failure.

## 2. Metric Lifecycle States (v1)

### Active

Definition:
The metric is meaningful and relevant right now.

Behavior:
- May be associated with goals
- May be referenced in meetings
- May appear in AI summaries (descriptively)
- Visible in Metric Library

### Dormant

Definition:
The metric exists but is not currently being monitored or discussed.

Behavior:
- Remains visible in Metric Library
- Hidden by default in goal and meeting views
- Retains full history
- Can be reactivated instantly

Dormant status prevents deletion from becoming a proxy for temporary irrelevance.

### Retired

Definition:
The metric is no longer meaningful or valid.

Behavior:
- Read-only
- Never surfaces in goals or meetings
- Preserves definition and history
- Remains searchable for historical context

## 3. Explicitly Excluded States

Metrics intentionally do not support:

- Health states
- Progress states
- Superseded states
- Evaluation or scoring states

Metrics describe observation only.

## 4. Metric Stewardship

Metrics do not have owners. They have stewards.

A steward is responsible for:
- Maintaining the metric definition
- Understanding metric provenance
- Explaining changes when needed

Stewardship does not imply responsibility for outcomes.

## 5. Metric Attributes

Each metric includes:

- Source (system, survey, manual)
- Scope (individual, team, organization)
- Steward (person or system role)
- Current lifecycle state

## 6. Manual (Human-Curated) Metrics

Manual metrics receive additional safeguards:

- Steward is always explicit
- Updates are intentional and infrequent
- No automated cadence is enforced
- Updating prompts a gentle reflection ('What changed?')

## 7. Metric Definition Changes

Metric definitions may change over time.

When a definition changes:
- Historical values are preserved
- New values follow the updated definition
- A gentle reflection ('What changed?') is prompted

AI summaries reference definition changes descriptively.

## 8. Relationship to Goals Over Time

Metrics may be associated with multiple goals.

- Attaching or detaching metrics does not affect metric lifecycle
- Goal lifecycle changes do not alter metric state
- Metrics are never inherited automatically by new goals

## 9. Meetings and Metrics

Meetings may:
- Reference metrics
- Attach metrics to goals
- Add narrative context

Meetings may not:
- Change metric lifecycle automatically
- Modify metric definitions silently
- Trigger alerts or actions

## 10. Guiding Principle

Metrics observe. Humans decide.

Lifecycle and stewardship exist to preserve clarity, trust, and historical context.