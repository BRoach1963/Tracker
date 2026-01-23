# 06f – Feedback Domain Tables

This document covers the feedback tables in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 2

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | feedback | ✅ FeedbackDetail.cs (fixed) |
| 2 | feedback_templates | ❌ No model |

---

## procohere.feedback

**Purpose**  
Feedback given between team members (praise, constructive, coaching).

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| from_member_id | uuid | NO | FK → team_members (sender) |
| to_member_id | uuid | NO | FK → team_members (recipient) |
| feedback_type | text | NO | 'general', 'praise', 'constructive', 'coaching' |
| title | text | YES | |
| content | text | NO | |
| visibility | text | NO | 'private', 'shared' |
| is_anonymous | boolean | NO | |
| rating | integer | YES | 1-5 |
| meeting_id | uuid | YES | FK → meetings (optional association) |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `FeedbackDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- Added `organization_id` column
- `to_member_id` changed from `Guid?` to `Guid` (DB is NOT NULL)
- Added `updated_at` column
- Added `deleted_at` column
- Added `deleted_by` column

**RLS:** Organization isolation.

---

## procohere.feedback_templates

**Purpose**  
Reusable feedback templates with prompts.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| created_by | uuid | NO | FK → team_members |
| name | text | NO | |
| description | text | YES | |
| feedback_type | text | NO | |
| prompts | jsonb | YES | Structured prompts for feedback |
| is_system_template | boolean | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**Note:** Template system for feedback not yet implemented.

**RLS:** Organization isolation.
