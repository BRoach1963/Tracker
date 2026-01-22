# Cohere – Metrics Foundations and Framing (v1)

This document defines the foundational principles for Metrics in Cohere. Metrics are designed to function as signals that inform human judgment, not as targets, grades, or evaluative instruments. This specification establishes guardrails before defining metric lifecycle, UI, or AI behavior.

## 1. Core Definition

A metric in Cohere is a signal, not a target.

Metrics exist to observe reality, detect change, and inform conversation. They never determine success or failure and never replace human interpretation.

## 2. Relationship Between Goals and Metrics

Goals express intent. Metrics express observation.

Key rules:
- A goal may exist without metrics
- A metric may exist without goals
- A metric may support multiple goals
- Metrics never complete goals
- Goals never own metrics

Metrics are associated with goals only to provide narrative context.

## 3. Manual (Human-Curated) Metrics

Cohere supports manually entered metrics, referred to as human-curated signals.

These metrics exist for signals that cannot be reliably automated, including:
- Confidence or readiness indicators
- Risk or uncertainty assessments
- Sentiment or morale signals
- Situational or temporary measures

Human-curated metrics are:
- Optional
- Intentionally updated
- Explicitly subjective
- Always paired with narrative context

## 4. Guardrails for Manual Metrics

To preserve trust and prevent misuse:

- Manual metrics do not update automatically
- No cadence is enforced unless chosen by the user
- Updating a manual metric prompts a gentle reflection ('What changed?')
- Manual metrics never drive automation, alerts, or evaluation

## 5. Metrics Without Goal Context

Metrics should never be interpreted without narrative context, but they may be discovered without a goal.

Cohere supports a metric discovery model that allows users to:
- Explore available metrics
- Understand metric scope and source
- See high-level trend indicators

Metrics in this state are read-only and non-evaluative.

## 6. Metric Discovery (Library Model)

The Metric Library exists to support curiosity and preparation, not analysis.

Characteristics:
- No dashboards or rankings
- No thresholds or alerts
- No interpretation or recommendations
- Clear provenance (source, scope, steward)

Metrics gain meaning only when associated with goals, agenda items, or discussion.

## 7. Explicit Non-Goals

Metrics in Cohere are explicitly not used for:

- Performance scoring
- Ranking individuals or teams
- Automated evaluation
- Silent health or lifecycle changes
- Enforcing targets or quotas

## 8. Guiding Principle

Metrics observe. Humans decide.

Metrics gain meaning only through conversation and narrative context.