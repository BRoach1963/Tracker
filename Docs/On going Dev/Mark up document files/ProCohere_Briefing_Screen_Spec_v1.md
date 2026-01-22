# Pro Cohere – Briefing Screen Specification (v1)

This document defines the intended behavior, layout, and role-based differentiation for the Briefing screen in Pro Cohere. The Briefing screen is designed to orient the user at the start of their day, answer the most important immediate questions, and establish context without judgment or overwhelm.

## Design Principles

- Briefing is not a dashboard

- One screen, one moment in time

- Visuals are supportive, not evaluative

- Managers reason about systems; ICs reason about obligations

- No rankings, no comparisons, no performance scoring

## Role-Based Variants

The Briefing screen renders differently based on the user’s role. Core layout remains consistent, but content emphasis and visualization differ.

## PART 1: Manager View

### Primary Questions Answered

• Is my team active and moving?

• Where might attention be needed today?

• What is already on my plate right now?

### Key Changes / Additions

1. Add a single visual element: Team Activity Sparkline

2. Retain existing KPI tiles (counts only, no performance semantics)

3. Emphasize team visibility over individual detail

### Team Activity Sparkline

A compact sparkline representing aggregate team activity over a recent time window (e.g., last 7–14 days).

• Represents motion, not health

• Aggregated across tasks, meetings, notes, and updates

• No individual attribution

• No labels implying performance or productivity

### Visual Constraints

• No axes labels

• No numeric values displayed

• No comparisons between people

• Trend only (up, flat, down)

### Content Emphasis

The manager Briefing emphasizes situational awareness and preparedness. It should encourage informed follow-up, not intervention.

## PART 2: IC / Team Member View

### Primary Questions Answered

• What do I need to deal with today?

• What is coming up soon?

• Am I missing anything obvious?

### Key Changes / Additions

1. Replace sparkline with a single binary distribution bar

2. Scope all information strictly to the individual

3. Remove any team-level signaling

### Binary Distribution Bar

A simple horizontal bar showing counts only, such as:

• Tasks due today vs later

• Meetings scheduled vs none

• Open items vs completed

This visual communicates inventory, not progress or performance.

### Visual Constraints

• Counts only, no percentages

• No trend over time

• No comparison to peers or averages

• Neutral labeling (e.g., 'Due Today', not 'Overdue')

### Content Emphasis

The IC Briefing should feel grounding and supportive. It exists to help the user feel oriented, not monitored.

## Shared Elements (Both Roles)

• Welcome header with date

• Quick Actions (Task, Goal, Meeting, Note)

• Upcoming Tasks list

• Upcoming Meetings section

• No alerts or interruptions on load

## Explicit Non-Goals

• No performance evaluation

• No productivity scoring

• No peer comparison

• No AI-initiated commentary on the Briefing screen

## Future Considerations (Out of Scope)

• Mobile adaptations

• Customizable Briefing layouts

• Historical drill-down from visuals