# Pro Cohere – AI Usage, Limits & Safeguards Specification (v1)

This document defines how AI usage is governed in Pro Cohere.
Its purpose is to ensure trust, predictability, cost control, and user confidence while keeping AI helpful, non-intrusive, and non-judgmental.

## Guiding Principles

• AI is assistive, never authoritative
• Visibility over opacity
• Limits are protective, not punitive
• Degradation over denial
• Cost-aware by design

## Scope of AI Usage

AI usage in Pro Cohere includes:
• Oracle conversational sessions
• AI-generated insights
• Summaries (meetings, goals, metrics, digests)
• On-demand suggestions when explicitly requested

## Usage Model (v1)

• One active AI session per user
• Sessions are isolated by default
• No long-term conversational memory
• Context is session-scoped only

## Quota Model

Quotas are expressed in:
• Requests per period
• Token-equivalent budgets (abstracted from users)

Quotas may be enforced at:
• Organization level
• User level (derived from org policy)

## Visibility & Transparency

Users can always see:
• Whether AI is enabled
• Approximate usage level (low / moderate / high)
• When they are nearing limits

Exact token counts are never exposed.

## Approaching Limits

When nearing limits:
• Non-blocking warning indicators appear
• No interruption of ongoing work
• No behavioral pressure language

## Limit Reached Behavior

When limits are reached:
• Existing sessions may complete
• New AI requests are deferred
• Non-AI features continue uninterrupted
• Clear, calm explanation is shown

## Feedback & Rating Signals

Users may optionally rate AI responses:
• Thumbs up / thumbs down
• Optional qualitative feedback on negative ratings

Feedback is used to:
• Improve prompts
• Identify failure patterns
• Tune guardrails

Feedback does NOT:
• Change user quotas
• Alter AI personality
• Affect other users’ limits directly

## Global vs User-Level Learning

Feedback is aggregated globally and anonymized.
No per-user behavioral modeling is performed in v1.

## Guardrails (Hard Rules)

Oracle must never:
• Evaluate people
• Score individuals
• Compare employees
• Recommend disciplinary action
• Assign blame
• Infer intent or motivation
• Reveal private or anonymous feedback
• Fabricate certainty from incomplete data

## Failure & Degradation Modes

If AI services are unavailable:
• Features fail silently where possible
• Clear fallback messaging is shown
• No blocking of core workflows

## Retention & Data Handling

• AI sessions retained for 90 days (default)
• Insights may be retained longer if accepted
• Logs stored for audit and cost analysis

## Future Considerations (v2)

• Per-team quotas
• User-selectable verbosity
• Advanced model selection
• Paid tier differentiation