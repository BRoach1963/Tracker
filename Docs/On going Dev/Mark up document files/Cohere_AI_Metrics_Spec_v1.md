# Cohere – AI + Metrics Specification (v1)

This document defines how AI interacts with Metrics in Cohere. AI is intentionally constrained to preserve trust, avoid judgment, and ensure metrics remain signals that support human sensemaking.

## 1. Core Principle

AI narrates signals. Humans interpret meaning.

AI must never evaluate, diagnose, predict outcomes, or assign responsibility based on metrics.

## 2. What AI Is Allowed to Do

AI may:

- Describe recent metric trends using qualitative language
- Reference when and how metrics have been discussed
- Note changes in variability or stability
- Surface when metrics have not been referenced recently
- Offer gentle, optional prompts to revisit a metric

All AI output must remain descriptive and non-directive.

## 3. Explicit Numeric Guardrail (Critical)

AI must not surface numeric metric values by default.

This restriction is intentional and foundational, not a phased limitation.

Rationale:
- Numeric values introduce implied judgment, even without evaluative language
- AI authority combined with numbers creates accidental performance assessment
- Numeric summaries are easily copied or shared without context

AI output must use directional and qualitative descriptors only (e.g., trending upward, stable, more variable).

Numeric values may only be referenced by AI if ALL of the following are true:
- The user explicitly requests numeric inclusion
- Numeric values are already visible in the UI context
- The output is clearly labeled as descriptive

Absent these conditions, AI must avoid numeric references entirely.

## 4. Language and Tone Rules

Allowed language:
- has changed
- has remained stable
- has become more variable
- has been discussed
- may provide context

Disallowed language:
- good / bad
- improved / worsened
- underperforming
- exceeding expectations
- concerning
- should / needs to

## 5. Role-Based Behavior

IC View:
- AI references only the IC’s own metrics
- No peer or team comparisons
- No distribution or relative language

Manager View:
- AI may reference aggregate distributions
- AI never names or implies individuals
- AI never ranks or compares people

## 6. Interaction with Metric Lifecycle

Active metrics may be summarized descriptively.

Dormant metrics may be referenced as inactive but not analyzed.

Retired metrics are referenced only for historical context.

AI must not recommend lifecycle changes.

## 7. Where AI + Metrics Appears

AI summaries appear only in passive or user-invoked contexts:

- Collapsible AI Summary in Metric Detail View
- Contextual summaries when metrics are attached to goals
- Optional agenda preparation assistance

AI must never appear inline with numeric displays or charts.

## 8. Summary Labeling

All AI-generated metric summaries must be clearly labeled as descriptive.

Recommended labels include:
- Context summary
- Descriptive summary

This reinforces that AI output reflects observation, not evaluation.

## 9. Guiding Principle

Metrics observe. AI narrates. Humans decide.