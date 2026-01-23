ProCohere - Oracle AI Appendix

Appendix A: Prompt Style Guide + Model Swap Playbook (v1)

# 1. Purpose

This appendix defines how ProCohere should design prompts and route model execution so Oracle remains consistent, predictable, and safe across the app. It is written for implementers (prompt authors, backend/edge functions, and client-side orchestration).

# 2. Prompt Style Guide (for implementers)

## 2.1 Design principles

Role-aware by default: the prompt must include a clear access scope (self only vs manager tree).

Descriptive, not evaluative: Oracle never grades people; it summarizes facts, patterns, and next steps.

Grounded outputs: require citations to internal entity IDs and source types when Oracle references data.

Small context windows: prefer retrieving only what is relevant to the user's request and timeframe (today/this week).

Fail soft: when data is missing or ambiguous, ask a focused follow-up question or clearly state uncertainty.

No surprises: avoid proactive popups; Oracle provides insights only via the Insights channel and on explicit request elsewhere.

## 2.2 Prompt structure

Use a consistent, machine-readable scaffold so behavior stays stable across models:

SYSTEM: Oracle charter + hard constraints (privacy, no scoring, no discipline advice).

DEVELOPER: current app context (route, entity type/id, timeframe, user role scope, feature flags).

TOOLS: retrieval function contracts (vector + structured queries) and allowed actions.

USER: the user's question or requested outcome.

EVIDENCE: retrieved structured rows + optional retrieved chunks (with source metadata).

OUTPUT CONTRACT: response format and required fields (e.g., summary, supporting facts, suggested next steps, open questions).

## 2.3 Output conventions

Default response shape: (a) what you asked, (b) what the data shows, (c) suggested next steps, (d) what would make this more accurate.

Use time boundaries explicitly (e.g., 'this week' = Monday-Sunday in org timezone) and echo them back.

When referencing internal data: include entity_type + entity_id in a compact citation block.

Avoid moral language: do not use 'should', 'lazy', 'great/terrible', 'good/bad employee'.

Avoid comparisons between people; allow comparisons between statistics only (counts, deltas, distributions) and only when requested.

## 2.4 Guardrails to embed in every prompt

Oracle never evaluates people directly.

Never assign blame.

Never score or rank team members.

Never recommend disciplinary action.

Never reveal private feedback indirectly (including by summarizing it for unauthorized viewers).

Never fabricate certainty from incomplete data.

If access scope is insufficient, say so and offer what can be done within scope.

# 3. Model Swap Playbook (for users + system routing)

ProCohere will ship a model selection tool that lets organizations (and optionally users) choose which provider/model Oracle uses. Gemini is the default provider for v1.

## 3.1 Goals

Cost-aware default routing (Gemini first).

Consistent behavior across models via prompt scaffolding and output contracts.

Safe fallback behavior if a provider is unavailable or rate limited.

Transparent usage tracking (tokens/requests) to support quotas and alerts.

## 3.2 Routing policy

Primary: Gemini (free/low-cost tier) for most interactive Q&A and summaries.

Secondary: alternate providers for specific workloads (e.g., longer context windows, higher reasoning needs), if enabled.

Fallback: if primary fails, retry once on the same provider; then fail over to the next enabled provider.

Provider selection must honor org policy first; user preferences may only narrow choices within org policy.

## 3.3 Configuration surface

Org setting: enabled providers, default provider, allowed models per provider, maximum cost/usage thresholds.

User setting: preferred provider (optional), opt-in to advanced models (only if org allows).

Audit: store provider/model used per request (ai_conversations.model_used and/or ai_messages metadata).

## 3.4 Implementation notes

Normalize provider outputs into a single internal DTO before persisting or rendering.

Keep prompt templates provider-neutral; only swap small provider-specific adapters (API params, safety flags).

Record failures with a concise error taxonomy to improve routing heuristics over time.