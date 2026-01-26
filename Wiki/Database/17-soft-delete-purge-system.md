# Soft Delete Retention & Monthly Purge System

## Overview

ProCohere uses a **soft delete** pattern across nearly all domain tables. Records are marked as deleted using:

- `is_deleted = true`
- `deleted_at = timestamp`
- `deleted_by = team_member_id`

This preserves short-term recoverability and auditability, but without a cleanup mechanism it will cause:
- Table bloat
- Index inefficiency
- Slower queries over time
- Increased storage cost

To address this, a **monthly, automated purge system** has been added to permanently remove soft-deleted records older than a fixed retention window.

---

## Design Goals

1. Preserve soft-delete semantics for normal application behavior
2. Automatically hard-delete records after a safe retention period
3. Avoid FK violations by deleting in dependency-safe order
4. Require no application involvement (DB-managed)
5. Be auditable, deterministic, and repeatable
6. Be cheap to run and safe to retry

---

## Retention Policy

| Property | Value |
|----------|-------|
| Retention window | 30 days |
| Execution frequency | Monthly |
| Execution method | PostgreSQL cron (`pg_cron`) |
| Scope | All ProCohere tables using soft delete |
| Exclusions | Views (never purged) |

---

## Table Eligibility Rules

A table is eligible for purge **if and only if** it meets all of the following:

- Has `is_deleted BOOLEAN`
- Has `deleted_at TIMESTAMPTZ`
- Is **not** a view
- Lives in the `procohere` schema

All eligible tables were verified prior to enabling the purge.

---

## Purge Strategy

### Key Rule
**Child tables must be deleted before parent tables.**

This is enforced by explicitly ordering DELETE statements inside a single purge function.

---

## Core Function

### `procohere.purge_soft_deleted_older_than(days INTEGER)`

#### Purpose
Hard-deletes all rows where:
```sql
is_deleted = true
AND deleted_at < now() - interval 'X days'
```

#### Signature
```sql
CREATE OR REPLACE FUNCTION procohere.purge_soft_deleted_older_than(p_days INTEGER)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER;
```

#### Behavior
- Executes deletes in FK-safe order
- Tracks affected row counts per table
- Returns a JSON summary for auditing/logging
- Idempotent (safe to re-run)
- Raises errors only on true schema violations

#### Example Return Value
```json
{
  "meeting_prep_items": 42,
  "meeting_agenda_items": 18,
  "meetings": 5,
  "projects": 2,
  "tasks": 97
}
```

---

## FK-Aware Delete Ordering

Deletes occur in this conceptual order (simplified):

### Phase 1: Link / Join Tables
- `meeting_agenda_item_links`
- `meeting_prep_item_links`
- `project_links`
- `project_members`
- `entity_tags`
- `note_links`
- `team_memberships`

### Phase 2: Leaf Content Tables
- `comments`
- `attachments`
- `notifications`
- `activity_feed`
- `ai_messages`
- `metric_values`
- `survey_answers`
- `survey_responses`

### Phase 3: Domain Objects
- `meeting_prep_items`
- `meeting_agenda_items`
- `meeting_notes`
- `meetings`
- `projects`
- `tasks`
- `goals`
- `metrics`
- `notes`

### Phase 4: Structural / Admin
- `teams`
- `team_members`
- `roles`
- `review_cycles`
- `surveys`

This order prevents FK constraint failures without needing CASCADE.

---

## Indexing for Purge Performance

Each eligible table must have an index like:

```sql
CREATE INDEX IF NOT EXISTS <table>_purge_idx
ON procohere.<table> (deleted_at)
WHERE is_deleted = true;
```

**Purpose:**
- Keeps purge scans fast
- Avoids full table scans
- Keeps monthly job runtime predictable

---

## Automation via pg_cron

### Extension
```sql
CREATE EXTENSION IF NOT EXISTS pg_cron;
```

No additional Supabase cost. Uses existing Postgres compute.

### Scheduled Job
```sql
SELECT cron.schedule(
  'procohere_monthly_soft_delete_purge',
  '0 3 1 * *',
  $$
  SELECT procohere.purge_soft_deleted_older_than(30);
  $$
);
```

### Schedule Meaning
- Runs at **03:00 UTC**
- On the **1st of every month**
- Off-peak, low user impact

---

## Verification & Monitoring

### Check Job Status
```sql
SELECT jobid, jobname, schedule, active
FROM cron.job
WHERE jobname = 'procohere_monthly_soft_delete_purge';
```

### Manual Dry Run
```sql
SELECT procohere.purge_soft_deleted_older_than(30);
```

Safe to run anytime.

---

## Important Constraints

- Views are **never** purged
- Soft delete RPCs **must** set `deleted_at`
- Purge assumes `deleted_at` is accurate
- No triggers are used (explicit > implicit)
- No cascading deletes (intentional safety)

---

## Future Considerations

- Make retention window configurable per org
- Add `audit_log` entries for purge runs
- Add alerting on unusually large purges
- Optional pre-purge snapshot for enterprise tier

---

## Summary

This system ensures ProCohere:

- Keeps its database clean
- Avoids long-term performance degradation
- Preserves short-term recovery semantics
- Requires zero application changes
- Scales safely as data grows

**This is intentional infrastructure, not cleanup magic.**
