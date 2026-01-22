# Cohere – Carry Forward: Model, UI, Expiration, and AI Guidance

This document defines the Carry Forward behavior in Cohere. It establishes the v1 constraint that carry-forward items are person-anchored, describes how they surface in meetings, formalizes expiration behavior, and outlines UI and AI guidance to preserve ease of use and user trust.

## 1. Definition of Carry Forward

Carry forward represents an intentional choice to continue an unresolved agenda discussion in a future meeting. It implies continuity of conversation, not ownership, accountability, or work.

Carry forward is not a task, reminder, goal, or assignment. If interim work is required, a Task must be created separately.

## 2. v1 Constraint: Person-Anchored Only

In the initial version of Cohere, all carry-forward items are anchored to a single individual. Even if created during a team meeting, the carry-forward must target one person.

## 3. Anchoring Rules

When a carry-forward outcome is created:
- In a 1:1 meeting, the anchor person defaults to the other attendee.
- In a multi-attendee meeting, Cohere proposes a default anchor based on context (linked task owner, goal owner, or a reasonable fallback).
- The proposed anchor is always visible and editable by the user.

## 4. Behavior When Anchor Is Not in a Meeting

Default behavior:
- Carry-forward items are only suggested when the anchor person is an attendee.
- If the anchor is not present, the carry-forward does not appear automatically.

## 5. Expiration Model

Carry-forward items expire by default to prevent accumulation and loss of trust.

Expiration rules:
- Expires after two meeting opportunities with the anchor person, or
- Expires after 30 days, whichever comes first.

## 6. Carry Forward States

Carry-forward item states:
- Pending
- Surfaced
- Resolved
- Converted
- Expired

## 7. Reminders

Carry-forward items do not generate reminders by default. Users may optionally request a single reminder if the topic is not discussed.

## 8. UI Design Principles

Carry-forward UI should remain invisible unless relevant and must never auto-insert items into agendas.

## 9. UI Suggestions

Carry-forward items appear as suggestions grouped by person. Users explicitly add them to agendas.

## 10. AI Assistance Guidelines

AI may suggest carry-forward but never applies it automatically.

## 11. AI Prompting Instructions

AI must use non-judgmental, tentative language and present all suggestions as optional.

## 12. Guiding Principle

Carry-forward is a memory aid. AI is a copilot, not an author.