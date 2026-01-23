# Cohere Oracle – Execution Pipeline Specification (v1, Gemini‑First)

This document defines the end‑to‑end execution pipeline for Oracle. It describes how a request moves from invocation through state assembly, retrieval, prompt construction, model execution, and response handling. The pipeline is designed to be deterministic, auditable, and model‑agnostic.

## Pipeline Overview

Oracle execution follows a fixed sequence:
1. Invocation
2. Intent Classification
3. State Pack Assembly
4. Vector Retrieval (optional)
5. Prompt Assembly
6. Model Execution
7. Response Validation
8. Delivery or Insight Creation

## 1. Invocation

Oracle is invoked explicitly by user request or implicitly by a UI action that clearly indicates intent (e.g., 'generate agenda', 'summarize meeting'). No proactive execution occurs.

## 2. Intent Classification

User input is classified into an intent category. This classification determines allowable actions and data access. If intent cannot be confidently inferred, Oracle requests clarification.

## 3. State Pack Assembly

A transient State Pack is assembled containing only the signals required to fulfill the request. Scope is limited by role and time relevance.

Typical State Pack contents:

• User role and reporting scope

• Relevant meetings, agenda items, tasks, goals, metrics

• Active alerts and deadlines

## 4. Vector Retrieval (Optional)

If the request benefits from semantic recall, Oracle queries the vector_embeddings table using entity‑scoped filters. Only embeddings tied to permitted entities are eligible.

## 5. Prompt Assembly

Prompts are assembled from structured templates. The system prompt encodes guardrails, tone, and non‑goals. State Pack data is injected as structured context, never as narrative.

## 6. Model Execution

Gemini is the default execution model. The pipeline supports swapping models without changing upstream logic. Token usage and latency are recorded for monitoring.

## 7. Response Validation

Responses are validated before delivery. Checks include:
- Policy violations
- Over‑confidence or fabricated certainty
- Comparative or evaluative language

## 8. Delivery & Insight Routing

If the response is a direct answer, it is returned to the user. If it qualifies as an insight, it is persisted as ai_insights and surfaced according to timing rules (today / this week).

## Failure Handling

If any pipeline stage fails, Oracle degrades gracefully. It acknowledges uncertainty and requests clarification rather than guessing.

This pipeline ensures Oracle is predictable, safe, and extensible as additional models and capabilities are introduced.