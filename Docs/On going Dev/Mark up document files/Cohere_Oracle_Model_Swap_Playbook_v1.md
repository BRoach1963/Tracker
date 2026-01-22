# Cohere Oracle – Model Swap Playbook (v1)

This document defines how Pro Cohere supports multiple AI models within Oracle without changing user experience, guardrails, or system behavior. The goal is controlled flexibility: users may choose models, while Oracle remains predictable, safe, and cost-aware.

## Core Principles

• Model choice never alters policy or tone

• Guardrails are enforced before and after model execution

• Users may select models, but defaults remain opinionated

• Fallbacks are automatic and invisible

## Supported Models (v1)

• Gemini (default, free tier)

• OpenAI (optional)

• Anthropic (optional)

All models must support:
- System prompts
- Structured context injection
- Deterministic temperature control
- Token usage reporting

## Model Selection Strategy

Model selection occurs at invocation time. The selected model is recorded on the ai_conversation record but does not affect downstream logic.

Selection sources:

• Organization default

• User preference (if enabled)

• Explicit per-session override

## Fallback Rules

If a selected model fails due to timeout, quota, or error, Oracle automatically retries using the organization default model. The user is not interrupted.

## Cost Controls

Token usage is tracked per model and per organization. Soft thresholds trigger warnings; hard limits disable non-default models.

## Behavioral Consistency

All prompts share a common system instruction set. Model-specific tuning is limited to formatting differences only.

## What Model Choice Does NOT Change

• Role scope

• Data visibility

• Insight rules

• Feedback handling

• Ethical constraints

## Future Expansion

Additional models may be introduced by registering a new execution adapter. No schema or policy changes are required.

This playbook ensures Oracle remains model-flexible without becoming model-dependent.