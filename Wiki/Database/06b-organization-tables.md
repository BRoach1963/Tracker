# 06b – Organization Domain Tables

This document covers all tables related to organizational structure in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 4

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | team_members | ✅ TeamMemberSimple (minimal), TeamMemberDetail (view) |
| 2 | roles | ❌ No model |
| 3 | teams | ❌ No model |
| 4 | org_settings | ❌ No model |

---

## procohere.team_members

**Purpose**  
Core table for people in an organization. Links to auth users and defines org hierarchy.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| linked_user_id | uuid | YES | FK → public.users (nullable for external/placeholder members) |
| role_id | uuid | NO | FK → roles |
| first_name | text | YES | |
| last_name | text | YES | |
| display_name | text | YES | Preferred display name |
| email | text | YES | |
| job_title | text | YES | |
| manager_team_member_id | uuid | YES | FK → team_members (self-referential hierarchy) |
| is_active | boolean | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Models:**
- `TeamMemberSimple` (in AuthService.cs) - Minimal model for specific queries ✅
- `TeamMemberDetail` - Maps to `v_team_members` VIEW (includes user data)

**Note:** DB has `display_name` column not exposed in current models.

**RLS:** Organization isolation via get_user_org_ids().

---

## procohere.roles

**Purpose**  
Permission roles that can be assigned to team members. Used to determine admin vs manager vs team member capabilities.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| name | text | NO | 'admin', 'manager', 'team_member', 'viewer' |
| description | text | YES | |
| permissions | jsonb | NO | Permission flags as JSON |
| is_system_role | boolean | NO | System-defined (cannot be deleted) |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `RoleDto` (in SessionDtos.cs) - fetched via `get_user_session` RPC, not direct table query

**Usage:**
- `AuthService.Instance.CurrentRole` exposes the current user's role
- ViewModels check `CurrentRole?.Name` to determine capabilities:
  - `"admin"` or `"manager"` → Manager view (sees Circle, team dashboard)
  - `"team_member"` or `"viewer"` → IC view (personal focus)

**RLS:** Organization isolation.

---

## procohere.teams

**Purpose**  
Organizational teams/groups (Engineering, Marketing, etc.). NOT to be confused with team_members (people).

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| parent_team_id | uuid | YES | FK → teams (hierarchy) |
| name | text | NO | |
| description | text | YES | |
| lead_team_member_id | uuid | YES | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**Note:** `TeamService` works with `team_members` (people), not this `teams` table. This is for organizational units/departments - a future feature.

**RLS:** Organization isolation.

---

## procohere.org_settings

**Purpose**  
Organization-level configuration and preferences.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| default_meeting_duration | integer | YES | Minutes |
| meeting_reminder_minutes | integer | YES | |
| require_agenda | boolean | NO | |
| require_notes | boolean | NO | |
| enable_ai_features | boolean | NO | |
| enable_anonymous_feedback | boolean | NO | |
| fiscal_year_start_month | integer | YES | 1-12 |
| goal_cycle_type | text | YES | 'quarterly', 'monthly', etc. |
| settings_json | jsonb | NO | Additional settings |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**Note:** The app currently uses hardcoded defaults. This table exists for future org-level customization.

**RLS:** Organization isolation.

---

## View: v_team_members

**Note:** `TeamMemberDetail` maps to this VIEW, not the base `team_members` table. The view joins:
- `procohere.team_members`
- `public.users` (for avatar_url, phone, birthday)

This allows accessing user profile data alongside team member data.
