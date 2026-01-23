# Cohere – Reporting Engine Specification (v1)

This document defines the v1 Reporting Engine for Cohere, designed to support exportable, shareable reports without requiring a web portal. Reports are generated server-side via Supabase Edge Functions, stored as immutable snapshots, and delivered to clients through signed download URLs.

## 1. Goals

- Provide polished, repeatable, canned reports with light configuration.
- Support PDF and DOCX for sharing/printing and XLSX for structured exports.
- Enforce privacy and permissions consistently (IC vs manager).
- Use immutable snapshots to preserve historical truth and avoid report drift.
- Design for an easy future migration to a dedicated .NET report service without rewriting templates.

## 2. Non-Goals (v1)

- No custom report builder.
- No BI-style dashboards.
- No scheduled/recurring report runs (planned for v2).
- No rankings or scorecards.

## 3. Locked Constraints (v1)

The following constraints are foundational and locked for v1:

1) Snapshot DTO is the contract: renderers do not query live operational tables.
2) DOCX uses templates: layout is defined by a template file with placeholders.
3) XLSX is data-first: structured exports, minimal styling, no charts by default.

## 4. Architecture Overview

Reporting is implemented as a two-stage pipeline:

Stage A – Snapshot Build (authoritative, permissioned)
- Edge Function validates request and permissions.
- Edge Function calls a Postgres RPC that returns a report-specific Snapshot DTO.
- Snapshot is stored (database JSONB or Storage) and treated as immutable.

Stage B – Rendering (deterministic)
- Renderer loads the snapshot.
- Renderer generates requested artifacts: PDF, DOCX, XLSX.
- Artifacts are stored in Supabase Storage.
- Signed URLs are returned to the client.

## 5. Why Snapshot-First Matters

- Ensures consistent permission enforcement at data assembly time.
- Prevents report drift when underlying notes/goals/metrics change.
- Keeps rendering deterministic and idempotent.
- Enables future migration: a .NET service can render the same snapshot contract.

## 6. Formats

PDF
- Canonical share/print artifact.

DOCX
- Editable narrative artifact generated from a template.

XLSX
- Data export artifact. Tabular sheets with stable columns, minimal styling.
- No charts or rankings by default.

## 7. Storage and Delivery

Artifacts are stored in Supabase Storage under a stable path pattern:
reports/{organizationId}/{reportRunId}/report.<ext>

Clients receive time-limited signed URLs. No web portal is required.

## 8. Data Model (v1)

Tables:

procohere.report_templates
- template_id (text) primary key
- name (text)
- version (int)
- allowed_roles (jsonb)
- supported_formats (jsonb)
- definition_json (jsonb)
- is_active (bool)
- created_at, updated_at

procohere.report_runs
- id (uuid) primary key
- organization_id (uuid)
- requested_by_team_member_id (uuid)
- template_id (text)
- template_version (int)
- inputs_json (jsonb)
- snapshot_json (jsonb) OR snapshot_storage_path (text)
- status (text) queued|running|succeeded|failed
- created_at, completed_at
- error_message (text)

procohere.report_files
- id (uuid) primary key
- report_run_id (uuid)
- format (text) pdf|docx|xlsx
- storage_path (text)
- byte_size (bigint)
- sha256 (text, optional)
- created_at

## 9. Permissions and Privacy Rules

- IC reports never include peer data.
- Manager reports may include team-level aggregations and distributions.
- Private notes are excluded unless requester is the author.
- Notes included in a report must be visible to the requester under normal app rules.
- AI metric summaries remain descriptive and non-judgmental.
- Numeric values are not surfaced by AI unless explicitly requested via report options.
- XLSX may include numeric fields as raw exports but must not rank or score people.

## 10. v1 Canned Template Set

A) 1:1 Packet (Manager ↔ IC)
- Agenda outcomes + carry-forward (for the IC)
- Goals touched in range
- Notes excerpts (privacy-safe)
- Metrics included descriptively; numeric appendix optional

B) Team Weekly Summary (Manager)
- Meetings held + outcomes
- Carry-forward by person
- Goals touched this week
- Team metric distributions (no rankings)

C) Goal Review Packet
- Goal lifecycle + narrative progress
- Linked tasks/milestones
- Metrics context; optional numeric appendix

D) Meeting Minutes
- Agenda + discussion notes
- Decisions
- Action items/outcomes with owners
- Follow-ups carried forward

E) Chronicle Export
- Notes filtered by date range/tags/links
- Private notes excluded unless requester is author
- XLSX supported for structured export

## 11. Inputs and Light Knobs (v1)

Common inputs:
- date_range: preset or explicit start/end
- subject_team_member_ids: 1:1 and personal exports
- meeting_id: Meeting Minutes
- goal_ids: Goal Review
- include_sections: per-template toggles
- include_ai_context: default false
- include_numeric_appendix: default false

Rule: include_numeric_appendix affects appendices/tables; AI narrative stays non-numeric unless explicitly requested.

## 12. Execution Model

Asynchronous execution:
- POST /reports/run creates report_run (queued) and begins processing.
- GET /reports/{id} returns status and signed URLs when available.

Idempotency:
- Rendering is safe to retry from snapshot.
- report_files should upsert by (report_run_id, format).

## 13. DOCX Template Strategy

DOCX uses pre-authored templates with placeholders (e.g., {{ReportTitle}}, {{DateRange}}). Renderer replaces placeholders with snapshot-derived values.

## 14. PDF Strategy

PDF is generated from a stable layout source (HTML or a dedicated PDF library). If Edge runtime constraints appear, generate canonical HTML first so PDF conversion can move to a dedicated service later.

## 15. XLSX Strategy

XLSX exports are tabular:
- Stable columns per report
- Freeze header row
- Filters enabled
- Minimal styling
- No charts by default
- No ranking/sorting by performance in team views

## 16. Roadmap

v2:
- Scheduled runs and reminders
- Saved Report Bundles (template + filters)
- Sharing workflows

v3+:
- Limited custom builder only if demand is proven
- Optional dedicated .NET renderer for heavier workloads