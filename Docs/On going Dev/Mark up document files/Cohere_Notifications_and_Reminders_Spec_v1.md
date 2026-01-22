ProCohere - Notification, Reminders, and Toasting Engine

Technical Plan (v1)

# 1. Scope

In-app notifications (primary) and system tray toasts (primary).

Email delivery as a secondary channel (opt-in / fallback).

Scheduled reminders for meetings, tasks, goals/metrics stewardship, and Oracle usage alerts.

Digest capability that is conservative and standards-aligned (weekly by default), with optional AI-generated summaries.

# 2. Design principles

Conservative and consistent: predictable timing, minimal noise, clear user control.

In-app priority: do not rely on email to make the product usable.

Always-on desktop app: background scheduler continues when app is minimized to tray.

Startup resilience: scheduler registers on OS startup and rehydrates pending reminders.

Idempotent delivery: every reminder must be safe to retry without duplicates.

# 3. Core components

## 3.1 Notification engine (client)

Runs as a background hosted service inside the Avalonia app process.

Maintains an in-memory priority queue of due reminders for the signed-in user.

Uses OS toast notifications when the app is in tray/minimized; uses in-app toast when foreground.

De-duplicates by (notification_id) and per-channel delivery markers.

## 3.2 Notification persistence (server)

Use procohere.notifications as the source of truth for pending and delivered notifications.

Write notifications server-side (edge function or DB-triggered routine) for events that are authoritative (meeting schedule changes, task due dates).

Client may create local-only transient toasts for UI micro-events (save success, minor validation).

## 3.3 Scheduling sources

Meetings: scheduled_at and reminder offsets (org_settings + user_settings).

Tasks: due_date with optional reminder rules.

Metric stewardship: staleness threshold starts at 30 days (configurable).

Oracle usage: alert as the user approaches quota (shown near the AI blob and optionally as a toast).

# 4. Digest strategy

## 4.1 Default behavior

Weekly digest enabled by default (user can turn off).

Digest content is mostly deterministic (counts, lists, highlights).

AI is optional inside digests: if enabled, AI produces a short, descriptive summary of the week with no judgment and no scoring.

## 4.2 Digest delivery

In-app digest view is primary; email digest is secondary and follows the same content.

Digest generation can run via edge function on a schedule and store a rendered snapshot for the user.

# 5. Retention

Notifications: retain 90 days by default, configurable per organization.

Dismissed notifications are soft-deleted or marked read; keep enough audit for support/debug.

Digests: retain 90 days by default; allow export via reporting engine if needed.

# 6. Open items to confirm later

Exact OS startup mechanism (Windows Task Scheduler vs startup registry entry) and enterprise deployment expectations.

Email provider choice (Supabase Auth email, Resend, SendGrid, etc.) and deliverability requirements.

Whether to support escalation rules (e.g., if in-app unread for N days then email).