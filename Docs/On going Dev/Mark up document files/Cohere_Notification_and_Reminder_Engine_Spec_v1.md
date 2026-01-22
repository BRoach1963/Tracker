# Cohere – Notification & Reminder Engine Specification (v1)

This document defines the v1 notification and reminder engine for Pro Cohere.
The goal is to ensure users are informed, prepared, and supported — without noise, pressure, or surveillance.

## 1. Core Principles

• In-app notifications are primary; email is secondary.
• The application is expected to run continuously (system tray when closed).
• Notifications must respect role, ownership, and privacy boundaries.
• No AI intrusion; reminders are factual and time-based.
• Users control frequency, channels, and staleness thresholds.

## 2. Notification Types

• Task reminders (due, overdue)
• Meeting reminders (upcoming, agenda missing)
• Goal check-ins (stale goals, approaching deadlines)
• Metric check-ins (missing updates, manual-entry metrics)
• Feedback-related notices (new feedback received)
• System notices (AI usage, limits, account status)

## 3. Delivery Channels

• In-app toast notifications (primary)
• In-app notification center (persistent)
• Email (digest or escalation only)
• No SMS or push in v1

## 4. Reminder Scheduling

• All reminders are policy-driven and implicit.
• Default staleness threshold: 30 days.
• Defaults may be overridden in user settings.
• Reminders are evaluated locally and server-side for consistency.

## 5. AI Interaction

• AI does not generate reminders autonomously.
• AI insights may reference reminders when explicitly requested.
• AI usage visibility is surfaced in settings.
• Alerts appear when usage approaches limits.

## 6. Persistence & Retention

• Notifications stored for 90 days.
• Read/unread state tracked per user.
• Deleted notifications are soft-deleted.
• No long-term behavioral profiling.

## 7. Guardrails

• No judgmental language.
• No performance scoring.
• No comparison between individuals.
• No disciplinary implications.