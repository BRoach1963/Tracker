# Pro Cohere – Settings & Preferences Specification (v1)

This document defines the Settings & Preferences system for Pro Cohere.
It establishes how users and organizations control behavior across notifications, reminders, digests, AI features, and general application behavior.

## Design Principles

• Conservative defaults
• User-controlled, not user-burdened
• Predictable behavior
• Role-aware, not role-fractured
• Explicit overrides beat implicit magic

## Settings Scope

Settings exist at three levels:
• Organization-level defaults
• User-level preferences
• System-enforced constraints

## Organization-Level Settings

• Enable/disable AI features globally
• Default reminder staleness threshold
• Default digest frequency
• Require agenda or notes for meetings
• Retention policies (AI, notifications, logs)

## User-Level Preferences

Notifications & Reminders:
• Enable/disable reminders
• In-app vs email delivery
• Quiet hours
• Staleness threshold override

Digests:
• Enable/disable digests
• Frequency (weekly default)
• AI summarization toggle

Toasts:
• Enable/disable
• Severity filtering
• Sound on/off

AI / Oracle:
• Enable/disable AI insights
• Usage visibility
• Model selection (if allowed)

## Role Awareness

• ICs may only configure settings affecting themselves
• Managers may configure team-level defaults where permitted
• No role allows visibility into another user’s private preferences

## Defaults (v1)

• Notifications: enabled
• Digests: weekly, enabled
• Toasts: enabled
• AI insights: enabled if org allows
• Staleness: 30 days
• Retention: 90 days

## AI-Specific Constraints

• AI cannot override user preferences
• AI behavior must respect quiet hours and delivery channels
• AI usage must be visible to the user

## Audit & Transparency

• Changes to org-level settings are auditable
• User preference changes are private
• AI enable/disable events are logged

## Explicit Exclusions (v1)

• No per-insight tuning
• No personality sliders
• No adaptive AI behavior based on inferred traits

## Future Considerations (v2)

• Preset profiles
• Team-level templates
• Temporary overrides
• Advanced notification routing