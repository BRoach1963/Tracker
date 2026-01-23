# Cohere – Chronicle Notes Implementation Plan Review (Concerns & Recommendations)

This document captures key concerns, risks, and recommended adjustments to the Chronicle Notes implementation plan to ensure correctness, scalability, performance, and alignment with the intended UI/UX.

## 1. Executive Summary

The Chronicle concept and overall MVVM/service structure are strong, but the current database linking approach (multiple nullable foreign keys on procohere.notes) will quickly constrain functionality, increase schema churn, and complicate permissions. The UI described already assumes multi-link behavior, which the current schema does not fully support. The recommended v1 correction is to introduce a normalized note_links table and keep notes focused on core note content and lifecycle flags.

## 2. Primary Concern: Entity Linking Model (Schema Mismatch)

Current plan:
- procohere.notes contains multiple nullable FK columns (linked_goal_id, linked_task_id, linked_meeting_id, etc.).

Key risks:
1) One note cannot link to multiple entities of the same type.
   - Example: a single note referencing two tasks or three goals is impossible.
2) Adding new entity types requires schema migrations and client model changes.
   - Increases deployment friction and brittleness.
3) Query patterns become more complex and slower over time.
   - Especially when searching notes by “any linked entity”.
4) Permissions/RLS become harder.
   - Hard to manage link visibility when different entities have different access rules.
5) UI mismatch.
   - The proposed editor uses chips and implies multi-link support; the schema provides only a fixed set of columns.

Recommended v1 adjustment:
Introduce a join table (procohere.note_links) and remove the fixed linked_* FK columns from notes, or keep them only for a single optional “primary link” if needed for UI convenience.

## 3. Organization FK Consistency

The plan references procohere.organizations(id), but Cohere’s broader architecture typically treats public.organizations(id) as the canonical organization table.

Risk:
- Inconsistent org modeling across schemas breaks reuse, RLS policy patterns, and shared cross-product assumptions.

Recommendation:
- organization_id should reference public.organizations(id) unless there is an explicit architectural reason not to.

## 4. Privacy Semantics Must Be Locked Early

Current flag:
- is_private: “Only visible to author”.

Missing specification:
- How private notes behave when linked to shared entities (goals/meetings/tasks).

Recommended rule (v1):
- If is_private = true, the note remains visible only to the author regardless of links.
- Links may still exist, but other users cannot infer note existence via linked entities.

Rationale:
- Preserves psychological safety and prevents accidental disclosure of sensitive context.

## 5. RLS and Permission Model Not Specified

The plan does not define Row Level Security (RLS) policies, which is high risk for a notes/journal system.

Minimum policy outcomes required:
- Author can create/read/update/delete (soft delete) their notes.
- Other org members can read non-private notes.
- Managers do not automatically gain access to private notes unless explicitly designed.
- Archived/deleted notes must respect visibility rules.

Link visibility requirement (if linking is used):
- A user should not see linked entities they lack permission for.
- Notes should not leak existence via links.

## 6. Full-Text Search: Correct Direction, Consider a Future Optimization

The current GIN index on to_tsvector(...) is a good v1 approach.

Future performance note:
- Chronicle will become a high-volume table.
- Consider adding a stored tsvector column (search_vector) maintained by trigger for faster writes/reads and simpler indexing once the feature proves value.

This is not required for v1, but should be noted explicitly to avoid later redesign churn.

## 7. Tags: JSONB Array Is Fine for v1, But Normalize on Write

Using tags as a JSONB array of strings is acceptable for v1.

Concern:
- If tags become important for filtering/search, inconsistent casing and formatting will fragment results.

Recommendation:
- Normalize tags to lowercase on write.
- Consider adding a GIN index on tags if tag-filter becomes a first-class workflow.

## 8. AI Fields: Avoid Staleness and Premature Action Suggestions

ai_summary and ai_suggested_actions stored directly on the note row can become stale and increase update churn.

Recommendation:
- Keep ai_summary optional and generated on demand.
- Defer ai_suggested_actions until the system’s action item/outcome model is fully defined.
- If AI insights become important, consider a separate note_ai_insights table keyed by (note_id, model/version).

## 9. Service/Code Concerns (Correctness & Maintainability)

Key concerns identified in the sample service implementation:

- GetNoteByIdAsync uses Single() semantics that may throw depending on library behavior; ensure consistent null/exception handling.
- CreateNoteAsync sets Id client-side while the DB also defaults the id; choose one approach (DB-generated preferred).
- Ensure schema/table routing is correct (procohere.notes vs public.notes) based on Supabase client configuration.
- Pinned vs unpinned collection split is acceptable for v1, but watch update complexity when editing/pinning/unpinning.

## 10. UI/UX Gaps

Two UI gaps to address:

1) Multi-link UI implies a join-table.
   - Chips for multiple links are a strong UX, but must be backed by a normalized model.

2) Activity tab implies an audit trail.
   - Either remove Activity in v1 or implement a minimal activity stream (created, edited, pinned, archived, link changes).

## 11. Recommended v1-Correct Shape

Recommended v1 architecture:

- procohere.notes: core content + lifecycle flags (pinned/archived/deleted) + tags + category + privacy
- procohere.note_links: (note_id, entity_type, entity_id, created_at, created_by)
- is_private enforced by RLS as author-only
- Search: keep GIN to_tsvector index for v1; consider search_vector later
- AI: summary optional, on-demand; defer action suggestions until outcomes are fully defined

## 12. Decision Needed to Proceed

A single decision must be locked to finalize schema and UI:

Do notes support multiple links (chips) in v1?

- If YES: implement note_links join table now.
- If NO: downgrade the UI to one link per entity type and accept early limitations.

The current plan and UI already imply YES.