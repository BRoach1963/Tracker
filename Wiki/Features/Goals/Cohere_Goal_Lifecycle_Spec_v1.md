# Cohere – Goal Lifecycle Specification (v1)

This document defines the lifecycle model for Goals in Cohere. The lifecycle describes goal relevance over time, not progress or success. It is designed to support reflection, narrative continuity, and intentional decision-making.

## 1. Purpose of the Goal Lifecycle

The goal lifecycle exists to help managers understand whether a goal still matters, how it is changing, and how it should surface in conversations. Lifecycle state is never a measure of performance.

## 2. Lifecycle States (v1)

### Active

Definition:
The goal matters right now.

Behavior:
- Surfaces in agenda suggestions
- Metrics are visible
- Supporting tasks may be created
- Discussed regularly

### Evolving

Definition:
The goal still matters, but its meaning or scope is changing.

Behavior:
- Surfaces in agendas with reduced urgency
- Metrics are shown but visually muted
- Reframing and discussion encouraged

### Paused

Definition:
The goal matters, but not at this time.

Behavior:
- Does not surface automatically in agendas
- Metrics are hidden by default
- Goal remains visible and can be resumed

### Superseded

Definition:
The goal has been replaced by one or more new goals.

Behavior:
- Terminal state
- Links forward to replacement goals
- Read-only
- Preserves full history

### Retired

Definition:
The goal no longer matters.

Behavior:
- Terminal state
- Does not surface in agendas
- Archived but searchable
- Preserves narrative context

## 3. Allowed Transitions

Goals may transition between lifecycle states only through explicit user action. No automatic or AI-driven lifecycle changes are permitted.

Common transitions:
- Active ↔ Evolving
- Active → Paused
- Evolving → Paused
- Active/Evolving → Superseded
- Active/Evolving/Paused → Retired

## 4. Reflection Prompt: 'What Changed?'

Whenever a lifecycle change occurs, Cohere gently prompts the user with:

'What changed? (optional)'

This prompt is:
- Optional
- One-line, free-form
- Non-judgmental

Its purpose is to preserve context for future reference and AI summarization.

## 5. Meeting Interactions

Meetings and agenda items may:
- Change goal health
- Reframe goal wording
- Transition lifecycle state
- Create supporting tasks
- Supersede a goal with new goals

Meetings may not:
- Automatically complete goals
- Delete goals
- Change lifecycle silently

## 6. Lifecycle Impact on UI Behavior

Lifecycle state directly influences system behavior:

Active:
- Suggested in agendas
- Metrics visible

Evolving:
- Suggested with lower emphasis
- Metrics visually muted

Paused:
- Hidden from agenda suggestions
- Metrics hidden by default

Superseded / Retired:
- Never suggested
- Read-only
- Displayed only in history views

## 7. Guiding Principle

Goal lifecycle describes relevance, not success. Lifecycle changes should feel reflective, not evaluative.