# ProCohere — Me Screen Specification

## Purpose

The **Me** screen is a persistent personal workspace.  
It shows **everything the user owns**, organized by type, without prioritization or urgency framing.

Me is **not** a daily work queue.  
Me is **not** a summary or alert surface.  
Those responsibilities belong to **Briefing**.

The Me screen exists to answer one question clearly:

> “What do I own, and where do I manage it?”

---

## Core Principles

- **Ownership over urgency**  
  Items are grouped by responsibility, not ranked by importance or recency.

- **Inventory, not judgment**  
  Counts and statuses are informational only. Me does not pressure or evaluate the user.

- **Persistence**  
  Items remain visible until explicitly resolved or archived. Nothing disappears due to time.

- **Predictable structure**  
  Categories are stable and navigable via tabs. No feed-style stacking.

---

## Navigation Context

The Me screen relies on application navigation for context.

There is **no page header**, subtitle, or explanatory banner.  
Selecting **Me** in the navigation is sufficient to establish scope and ownership.

---

## Layout Overview

The screen is composed of:

- A **tab bar** defining categories of ownership
- A **primary list panel** showing items for the selected tab
- A **detail panel** for inspecting and managing the selected item

This is a classic master–detail layout.

---

## Tabs

Tabs represent **categories of responsibility**, not competing priorities.

The default tabs are:

- **Tasks**
- **Goals**
- **Meetings**
- **Feedback**
- **Notes**

Tabs are:
- Mutually exclusive
- Non-ranked
- Persistent across sessions

Switching tabs does not imply urgency or escalation.

---

## Item Counts

Counts may be displayed at the tab level (e.g., “13 tasks”).

These counts:
- Reflect total ownership
- Are not sorted by urgency
- Do not imply action required

Overdue or stalled indicators, if present, must be visually de-emphasized and informational only.

Urgency logic belongs exclusively in **Briefing**.

---

## Tasks (Me Context)

In Me, tasks represent **owned work**, not a prioritized queue.

Characteristics:
- Ordered predictably (e.g., created date or manual order)
- Not auto-promoted based on due date
- Not collapsed or hidden due to age

Tasks in Me may include:
- Action items
- Follow-ups
- Conversation anchors
- Long-running responsibilities

The Me view does not attempt to normalize or reinterpret task intent.

---

## Goals

Goals in Me represent **long-term intent and development**, not short-term execution.

Goals:
- Persist across time horizons
- Are not ranked against tasks
- May exist independently of current activity

Progress indicators are allowed but must avoid scorecard or OKR framing.

---

## Meetings

Meetings in Me reflect **owned participation**, not calendar urgency.

This includes:
- 1:1s
- Planning sessions
- Review meetings

The Me view shows meeting presence and context, not reminders or alerts.

---

## Feedback

Feedback represents **received or owned feedback artifacts**.

In Me:
- Feedback is visible until addressed or archived
- No urgency language is applied
- Feedback is treated as reference material

---

## Notes

Notes (Chronicle) are first-class owned artifacts.

Notes in Me:
- Are searchable
- Persist indefinitely
- Are not time-scoped

Notes do not compete with tasks or goals for attention.

---

## Detail Panel

Selecting any item opens a detail panel.

The detail panel:
- Shows full context
- Allows editing and resolution
- Does not change global ordering or prioritization

The panel is scoped strictly to the selected item type.

---

## Explicit Non-Goals

The Me screen does **not**:

- Aggregate urgency across categories
- Promote items based on deadlines
- Act as a feed or activity stream
- Replace Briefing
- Score, rank, or judge performance

---

## Relationship to Briefing

**Briefing** is the daily, time-scoped work surface.  
**Me** is the persistent ownership surface.

Briefing answers:
> “What needs my attention now?”

Me answers:
> “What do I own overall?”

These views must remain conceptually and visually distinct.
