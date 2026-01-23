# Cohere – Metric UI Specification (v1)

This document defines how Metrics are presented in the Cohere user interface. The Metric UI is intentionally designed to surface signals without turning them into judgments, dashboards, or performance scorecards. Role-based differences are explicit and foundational.

## 1. Guiding UI Principles

Metric UI in Cohere must:

- Emphasize direction over precision
- Preserve narrative and context
- Avoid evaluative or comparative framing by default
- Prevent ranking and scorecard behavior
- Respect role-based visibility and responsibility

## 2. Core Metric UI Surfaces

### 2.1 Metric Library (Discovery View)

Purpose:
Enable discovery and awareness of available metrics without interpretation.

Presentation:
- Row-based listing
- Metric name
- Scope (individual, team, org)
- Source
- Lifecycle state (Active, Dormant, Retired)
- Directional trend indicator (↗ → ↘)
- Count of associated goals

Rules:
- No charts by default
- No rankings or sorting by value
- Numeric values hidden by default (toggleable)
- Dormant metrics remain visible but muted

### 2.2 Metrics Within Goals

Metrics associated with a goal provide contextual signals only.

Default State:
- Collapsed list
- Directional indicators only
- No numeric values shown

Expanded State:
- Optional sparkline (short time window)
- Definition and steward visible
- Numeric values toggleable

Metrics never imply success or failure of a goal.

### 2.3 Metrics in Meetings and Agenda Items

Metrics appear in meetings only when explicitly referenced.

Behavior:
- Inline with agenda item context
- Directional trend only
- No interpretation text

Metrics are never auto-inserted into meetings.

### 2.4 Metric Detail View

Metric Detail View is accessed intentionally and used infrequently.

Displays:
- Metric definition
- Scope and source
- Steward
- Lifecycle state
- Trend visualization (limited horizon)
- Definition change history
- Associated goals and meetings

Explicitly excludes:
- Rankings
- Targets or thresholds
- Performance labels

## 3. Numeric Values and Sparklines

Numeric values:
- Hidden by default
- Toggleable per user
- Never required to interpret metrics

Sparklines:
- Shown only in expanded or detail views
- Limited time windows
- Neutral color palette

## 4. Role-Based Metric Views

### 4.1 IC View

ICs:
- See only their own metrics
- See no peer or team-member comparisons
- View trends and narrative context only
- Never see rankings or distributions

This preserves psychological safety and trust.

### 4.2 Manager View

Managers have expanded visibility to support responsibility.

Managers may:
- View team-level distributions
- See ranges, medians, and variance
- Identify outliers
- Drill into individual metrics intentionally

Managers may not:
- Rank or sort individuals
- View leaderboards
- Export ranked metric lists

Comparative views are distribution-based, not ordinal.

## 5. Sensitivity Handling

Sensitive metrics (e.g., velocity, load, PTO usage) receive additional safeguards:

- No default comparison views
- Explicit user action required to view detail
- No cross-individual displays for ICs
- Neutral visual language

## 6. Explicitly Forbidden UI Patterns

The following patterns are not permitted:

- Dashboards
- Leaderboards
- Scorecards
- Red/yellow/green status indicators
- Threshold or target lines
- Automated callouts or alerts

## 7. Guiding Principle

Metrics are quiet observers that speak only when invited, and never without context.