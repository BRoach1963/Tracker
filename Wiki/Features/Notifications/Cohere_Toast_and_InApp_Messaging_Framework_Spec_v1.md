# Pro Cohere – Toast & In-App Messaging Framework (v1)

This document defines the Toast & In-App Messaging framework for Pro Cohere.
This system governs short-lived, interruptive messages shown inside the application UI.

## Purpose

To provide timely, contextual feedback to users without overwhelming them.
Toasts are informational and confirmational, not directive or evaluative.

## Design Principles

• Interrupt only when necessary
• Confirm actions, not judge behavior
• Respect user focus
• Never compete with reminders or digests
• Be dismissible and ephemeral

## Toast Types

1. Confirmation Toasts
2. Informational Toasts
3. Warning Toasts (non-critical)
4. System Status Toasts

## Allowed Use Cases

• Action completed successfully
• Action failed with recoverable error
• Data saved or synced
• Settings updated
• Background process finished

## Explicitly Disallowed Use Cases

• Performance feedback
• AI suggestions or nudges
• Reminders or deadlines
• Behavioral correction
• Repeated notifications

## Severity Levels

• Info
• Success
• Warning
• Error

## Lifecycle Rules

• Auto-dismiss after short duration
• Manual dismissal always available
• No stacking beyond a small limit
• New toasts replace older lower-priority ones

## Role Awareness

Toast content is role-neutral.
No role-specific messaging logic.

## AI Interaction

AI must never trigger toasts directly.
AI output may be referenced indirectly (e.g., “Summary generated”).

## Accessibility & UX

• Keyboard dismissible
• Screen-reader friendly
• Respect reduced-motion settings

## Configuration & Settings

• Global enable/disable
• Severity-level filtering
• Sound on/off

## Future Considerations (v2)

• Context-aware placement
• Batch confirmation summaries
• Admin-level overrides