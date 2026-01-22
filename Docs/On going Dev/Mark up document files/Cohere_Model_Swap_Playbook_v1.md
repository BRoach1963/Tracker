# Cohere AI Model Swap Playbook

## Purpose

This playbook defines how Cohere supports multiple AI providers without changing Oracle behavior.

## Provider Neutrality

Oracle behavior is invariant across models. Providers are interchangeable execution engines.

## Supported Providers

- Gemini (default)
- OpenAI
- Anthropic

## Selection Levels

- System default
- Organization override
- User preference (if enabled)

## Configuration Strategy

All model selection occurs via configuration, never prompt logic. Prompts are provider-agnostic.

## Embedding Strategy

Vector dimensions and model metadata are stored per embedding row. Multiple embedding models may coexist.

## Fallback Rules

If a provider fails:
- Retry once
- Fail over to system default
- Log provider + model version

## Guardrails

Behavioral constraints apply regardless of provider. A model swap must never alter tone, scope, or authority.

## Testing Requirement

Any provider swap requires regression validation against the Oracle Acceptance Checklist.