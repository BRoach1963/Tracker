# ProCohere Database Schema Specification

**Version:** 1.0  
**Date:** 2026-01-17  
**Schema:** `procohere`  
**Database:** Supabase PostgreSQL

---

## Table of Contents

1. [Overview](#1-overview)
2. [Prerequisites & Dependencies](#2-prerequisites--dependencies)
3. [Security Architecture](#3-security-architecture)
4. [Table Reference](#4-table-reference)
5. [Entity Relationships](#5-entity-relationships)
6. [Indexes & Performance](#6-indexes--performance)
7. [Conventions & Patterns](#7-conventions--patterns)
8. [Enum Values & Expected Types](#8-enum-values--expected-types)
9. [Triggers](#9-triggers)
10. [RLS Policies](#10-rls-policies)
11. [Grants & Permissions](#11-grants--permissions)

---

## 1. Overview

The `procohere` schema supports a people management and organizational effectiveness platform with the following core domains:

| Domain | Tables | Purpose |
|--------|--------|---------|
| **Organization** | roles, team_members, teams, org_settings | Organizational structure and configuration |
| **Meetings** | meetings, meeting_attendees, meeting_agenda_items, meeting_notes, meeting_summaries, meeting_templates | Meeting lifecycle management |
| **Goals & Targets** | goal_categories, goals, targets, goal_templates | OKR-style goal tracking |
| **Tasks** | tasks | Action item and task management |
| **Feedback** | feedback, feedback_templates | Performance feedback |
| **Notes** | notes | Private and meeting notes |
| **Metrics** | metrics, metric_values | KPI tracking |
| **Surveys** | surveys, survey_questions, survey_responses, survey_answers | Employee surveys |
| **AI** | ai_conversations, ai_messages, ai_insights | AI-powered assistance |
| **Utility** | attachments, tags, entity_tags, notifications, comments, activity_feed | Cross-cutting features |
| **Settings** | user_settings, calendar_integrations | User preferences and integrations |
| **Development** | competencies, team_member_competencies, development_plans, development_plan_items | Career development |
| **Recognition** | kudos | Peer recognition |
| **Reviews** | review_cycles, performance_reviews | Performance review cycles |
| **Audit** | audit_log | System audit trail |

**Total Tables:** 43 (2 commented out - roles, team_members - already contain production data)

---

## 2. Prerequisites & Dependencies

### Required Public Schema Objects

The `procohere` schema depends on these objects in the `public` schema:

| Object | Type | Purpose |
|--------|------|---------|
| `public.organizations` | Table | Parent organizations |
| `public.organization_members` | Table | User ↔ organization membership |
| `public.users` | Table | User accounts (synced with auth.users) |
| `public.set_updated_at()` | Function | Trigger function for updated_at timestamps |
| `auth.uid()` | Function | Supabase Auth - returns current user's UUID |

### Foreign Key References to Public Schema

```
procohere.* → public.organizations(id)      -- organization_id on all tables
procohere.* → public.users(id)              -- deleted_by on all tables
procohere.team_members → public.users(id)   -- linked_user_id
procohere.audit_log → public.users(id)      -- actor_id
```

---

## 3. Security Architecture

### Row-Level Security (RLS) Model

```
┌────────────────────────────────────────────────────────────┐
│                    Supabase Auth                            │
│                    auth.uid() → user UUID                  │
└────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────┐
│          public.organization_members                        │
│     user_id ←→ organization_id mapping                     │
└────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────┐
│       procohere.get_user_org_ids()                         │
│    Returns UUID[] of orgs user belongs to                  │
│    SECURITY DEFINER + search_path = procohere, public      │
└────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────┐
│              RLS Policies on All Tables                    │
│   USING (organization_id = ANY(get_user_org_ids()))        │
│   WITH CHECK (organization_id = ANY(get_user_org_ids()))   │
└────────────────────────────────────────────────────────────┘
```

### Policy Types

| Policy | Applied To | Purpose |
|--------|------------|---------|
| `org_isolation` | 42 tables | Standard org-based isolation |
| `owner_only` | calendar_integrations | Token owner access only |

### Grant Model

| Permission | Granted | Reason |
|------------|---------|--------|
| SELECT | ✅ All tables | Read through RLS |
| INSERT | ❌ | Via RPCs only |
| UPDATE | ❌ | Via RPCs only |
| DELETE | ❌ | Via RPCs only |
| EXECUTE | ✅ get_user_org_ids() | RLS helper function |

---

## 4. Table Reference

### 4.1 Organization Domain

#### Table 1: `roles` (COMMENTED OUT)
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| name | text | NO | - | Role name |
| description | text | YES | - | Role description |
| permissions | jsonb | NO | '{}' | Permission flags |
| is_system_role | boolean | NO | false | System-defined role |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 2: `team_members` (COMMENTED OUT)
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| linked_user_id | uuid | YES | - | FK → users (nullable for external members) |
| role_id | uuid | NO | - | FK → roles |
| manager_team_member_id | uuid | YES | - | FK → team_members (self-referential) |
| first_name | text | NO | - | First name |
| last_name | text | NO | - | Last name |
| email | text | YES | - | Email address |
| job_title | text | YES | - | Job title |
| department | text | YES | - | Department |
| hire_date | date | YES | - | Hire date |
| is_active | boolean | NO | true | Active status |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, linked_user_id) WHERE is_deleted = false AND linked_user_id IS NOT NULL`

#### Table 3: `teams`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| parent_team_id | uuid | YES | - | FK → teams (hierarchy) |
| name | text | NO | - | Team name |
| description | text | YES | - | Team description |
| lead_team_member_id | uuid | YES | - | FK → team_members |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 4: `org_settings`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| default_meeting_duration | int | YES | 30 | Default meeting length (minutes) |
| meeting_reminder_minutes | int | YES | 15 | Reminder before meeting |
| require_agenda | boolean | NO | false | Require agenda for meetings |
| require_notes | boolean | NO | false | Require notes for meetings |
| enable_ai_features | boolean | NO | true | AI features enabled |
| enable_anonymous_feedback | boolean | NO | true | Anonymous feedback enabled |
| fiscal_year_start_month | int | YES | 1 | Fiscal year start (1-12) |
| goal_cycle_type | text | YES | 'quarterly' | Goal cycle type |
| settings_json | jsonb | NO | '{}' | Additional settings |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id) WHERE is_deleted = false`

---

### 4.2 Meetings Domain

#### Table 5: `meetings`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| title | text | NO | - | Meeting title |
| description | text | YES | - | Meeting description |
| meeting_type | text | NO | 'one_on_one' | Type of meeting |
| status | text | NO | 'scheduled' | Meeting status |
| scheduled_at | timestamptz | YES | - | Scheduled start time |
| started_at | timestamptz | YES | - | Actual start time |
| ended_at | timestamptz | YES | - | Actual end time |
| duration_minutes | int | YES | - | Duration in minutes |
| location | text | YES | - | Physical location |
| video_link | text | YES | - | Video conference URL |
| recurrence_rule | text | YES | - | RRULE for recurring |
| parent_meeting_id | uuid | YES | - | FK → meetings (series parent) |
| created_by | uuid | NO | - | FK → team_members |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 6: `meeting_attendees`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| meeting_id | uuid | NO | - | FK → meetings |
| team_member_id | uuid | NO | - | FK → team_members |
| role | text | NO | 'attendee' | Role in meeting |
| response_status | text | NO | 'pending' | RSVP status |
| attended | boolean | YES | - | Actually attended |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(meeting_id, team_member_id) WHERE is_deleted = false`

#### Table 7: `meeting_agenda_items`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| meeting_id | uuid | NO | - | FK → meetings |
| added_by | uuid | NO | - | FK → team_members |
| title | text | NO | - | Agenda item title |
| description | text | YES | - | Agenda item description |
| sort_order | int | NO | 0 | Display order |
| is_private | boolean | NO | false | Private to creator |
| is_completed | boolean | NO | false | Marked as completed |
| completed_at | timestamptz | YES | - | Completion timestamp |
| linked_entity_type | text | YES | - | Linked entity type |
| linked_entity_id | uuid | YES | - | Linked entity ID |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 8: `meeting_notes`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| meeting_id | uuid | NO | - | FK → meetings |
| author_id | uuid | NO | - | FK → team_members |
| content | text | NO | - | Note content |
| is_shared | boolean | NO | false | Shared with attendees |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 9: `meeting_summaries`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| meeting_id | uuid | NO | - | FK → meetings |
| summary | text | NO | - | Summary text |
| key_decisions | jsonb | YES | - | Array of decisions |
| action_items | jsonb | YES | - | Array of action items |
| topics_discussed | jsonb | YES | - | Array of topics |
| sentiment | text | YES | - | Overall sentiment |
| generated_by | text | YES | - | 'ai' or 'manual' |
| is_approved | boolean | NO | false | Approved by attendee |
| approved_by | uuid | YES | - | FK → team_members |
| approved_at | timestamptz | YES | - | Approval timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(meeting_id) WHERE is_deleted = false`

#### Table 10: `meeting_templates`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| created_by | uuid | NO | - | FK → team_members |
| name | text | NO | - | Template name |
| description | text | YES | - | Template description |
| meeting_type | text | NO | 'one_on_one' | Type of meeting |
| default_duration | int | YES | 30 | Default duration |
| default_agenda | jsonb | YES | - | Default agenda items |
| is_system_template | boolean | NO | false | System template |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

---

### 4.3 Goals Domain

#### Table 11: `goal_categories`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| name | text | NO | - | Category name |
| description | text | YES | - | Category description |
| color | text | YES | - | Display color |
| sort_order | int | NO | 0 | Display order |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 12: `goals`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| owner_id | uuid | NO | - | FK → team_members |
| parent_goal_id | uuid | YES | - | FK → goals (cascade) |
| category_id | uuid | YES | - | FK → goal_categories |
| title | text | NO | - | Goal title |
| description | text | YES | - | Goal description |
| goal_type | text | NO | 'individual' | Goal type |
| status | text | NO | 'not_started' | Goal status |
| priority | text | YES | 'medium' | Priority level |
| start_date | date | YES | - | Start date |
| due_date | date | YES | - | Due date |
| completed_at | timestamptz | YES | - | Completion timestamp |
| progress_percent | int | NO | 0 | Progress (0-100) |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Constraint:** `progress_percent >= 0 AND progress_percent <= 100`

#### Table 13: `targets`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| goal_id | uuid | NO | - | FK → goals |
| title | text | NO | - | Target title |
| description | text | YES | - | Target description |
| target_type | text | NO | 'numeric' | Type of target |
| target_value | numeric | YES | - | Target value |
| current_value | numeric | NO | 0 | Current value |
| unit | text | YES | - | Unit of measure |
| status | text | NO | 'not_started' | Target status |
| due_date | date | YES | - | Due date |
| completed_at | timestamptz | YES | - | Completion timestamp |
| sort_order | int | NO | 0 | Display order |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 14: `goal_templates`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| created_by | uuid | NO | - | FK → team_members |
| category_id | uuid | YES | - | FK → goal_categories |
| name | text | NO | - | Template name |
| description | text | YES | - | Template description |
| goal_type | text | NO | 'individual' | Goal type |
| default_targets | jsonb | YES | - | Default target definitions |
| is_system_template | boolean | NO | false | System template |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

---

### 4.4 Tasks Domain

#### Table 15: `tasks`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| assigned_to | uuid | YES | - | FK → team_members |
| created_by | uuid | NO | - | FK → team_members |
| title | text | NO | - | Task title |
| description | text | YES | - | Task description |
| status | text | NO | 'todo' | Task status |
| priority | text | YES | 'medium' | Priority level |
| due_date | timestamptz | YES | - | Due date/time |
| completed_at | timestamptz | YES | - | Completion timestamp |
| source_type | text | YES | - | Source entity type |
| source_id | uuid | YES | - | Source entity ID |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.5 Feedback Domain

#### Table 16: `feedback`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| from_member_id | uuid | NO | - | FK → team_members |
| to_member_id | uuid | NO | - | FK → team_members |
| feedback_type | text | NO | 'general' | Feedback type |
| title | text | YES | - | Feedback title |
| content | text | NO | - | Feedback content |
| visibility | text | NO | 'private' | Visibility level |
| is_anonymous | boolean | NO | false | Anonymous feedback |
| rating | int | YES | - | Rating (1-5) |
| meeting_id | uuid | YES | - | FK → meetings |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Constraint:** `rating >= 1 AND rating <= 5`

#### Table 17: `feedback_templates`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| created_by | uuid | NO | - | FK → team_members |
| name | text | NO | - | Template name |
| description | text | YES | - | Template description |
| feedback_type | text | NO | 'general' | Feedback type |
| prompts | jsonb | YES | - | Feedback prompts |
| is_system_template | boolean | NO | false | System template |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

---

### 4.6 Notes Domain

#### Table 18: `notes`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| created_by | uuid | NO | - | FK → team_members |
| meeting_id | uuid | YES | - | FK → meetings |
| team_member_id | uuid | YES | - | FK → team_members (about) |
| title | text | YES | - | Note title |
| content | text | NO | - | Note content |
| is_private | boolean | NO | true | Private note |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.7 Metrics Domain

#### Table 19: `metrics`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| owner_id | uuid | YES | - | FK → team_members |
| name | text | NO | - | Metric name |
| description | text | YES | - | Metric description |
| metric_type | text | NO | 'number' | Metric type |
| unit | text | YES | - | Unit of measure |
| target_value | numeric | YES | - | Target value |
| current_value | numeric | YES | - | Current value |
| direction | text | YES | 'higher_is_better' | Value direction |
| frequency | text | YES | 'weekly' | Update frequency |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 20: `metric_values`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| metric_id | uuid | NO | - | FK → metrics |
| recorded_by | uuid | YES | - | FK → team_members |
| value | numeric | NO | - | Recorded value |
| recorded_at | timestamptz | NO | now() | Recording timestamp |
| notes | text | YES | - | Notes |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.8 Surveys Domain

#### Table 21: `surveys`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| created_by | uuid | NO | - | FK → team_members |
| title | text | NO | - | Survey title |
| description | text | YES | - | Survey description |
| status | text | NO | 'draft' | Survey status |
| is_anonymous | boolean | NO | false | Anonymous responses |
| starts_at | timestamptz | YES | - | Start time |
| ends_at | timestamptz | YES | - | End time |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 22: `survey_questions`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| survey_id | uuid | NO | - | FK → surveys |
| question_text | text | NO | - | Question text |
| question_type | text | NO | 'text' | Question type |
| options | jsonb | YES | - | Multiple choice options |
| is_required | boolean | NO | false | Required question |
| sort_order | int | NO | 0 | Display order |
| min_value | int | YES | - | Min for scale questions |
| max_value | int | YES | - | Max for scale questions |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 23: `survey_responses`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| survey_id | uuid | NO | - | FK → surveys |
| respondent_id | uuid | YES | - | FK → team_members (null if anonymous) |
| submitted_at | timestamptz | YES | - | Submission timestamp |
| is_complete | boolean | NO | false | Response complete |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(survey_id, respondent_id) WHERE is_deleted = false AND respondent_id IS NOT NULL`

#### Table 24: `survey_answers`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| response_id | uuid | NO | - | FK → survey_responses |
| question_id | uuid | NO | - | FK → survey_questions |
| answer_text | text | YES | - | Text answer |
| answer_numeric | numeric | YES | - | Numeric answer |
| answer_json | jsonb | YES | - | Complex answer |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(response_id, question_id) WHERE is_deleted = false`

---

### 4.9 AI Domain

#### Table 25: `ai_conversations`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| title | text | YES | - | Conversation title |
| context_type | text | YES | - | Context entity type |
| context_id | uuid | YES | - | Context entity ID |
| model_used | text | YES | - | AI model identifier |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 26: `ai_messages`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| conversation_id | uuid | NO | - | FK → ai_conversations |
| role | text | NO | - | 'user', 'assistant', 'system' |
| content | text | NO | - | Message content |
| tokens_used | int | YES | - | Token count |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 27: `ai_insights`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | YES | - | FK → team_members (about) |
| generated_for | uuid | NO | - | FK → team_members (recipient) |
| insight_type | text | NO | - | Insight type |
| title | text | NO | - | Insight title |
| content | text | NO | - | Insight content |
| source_type | text | YES | - | Source entity type |
| source_id | uuid | YES | - | Source entity ID |
| relevance_score | numeric | YES | - | Relevance score |
| is_dismissed | boolean | NO | false | Dismissed by user |
| dismissed_at | timestamptz | YES | - | Dismissal timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.10 Utility Domain

#### Table 28: `attachments`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| uploaded_by | uuid | NO | - | FK → team_members |
| entity_type | text | NO | - | Parent entity type |
| entity_id | uuid | NO | - | Parent entity ID |
| file_name | text | NO | - | Original filename |
| file_size | bigint | YES | - | File size in bytes |
| mime_type | text | YES | - | MIME type |
| storage_path | text | NO | - | Storage path/URL |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 29: `tags`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| name | text | NO | - | Tag name |
| color | text | YES | - | Display color |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 30: `entity_tags`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| tag_id | uuid | NO | - | FK → tags |
| entity_type | text | NO | - | Tagged entity type |
| entity_id | uuid | NO | - | Tagged entity ID |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(tag_id, entity_type, entity_id) WHERE is_deleted = false`

#### Table 31: `notifications`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| notification_type | text | NO | - | Notification type |
| title | text | NO | - | Notification title |
| message | text | YES | - | Notification message |
| entity_type | text | YES | - | Related entity type |
| entity_id | uuid | YES | - | Related entity ID |
| is_read | boolean | NO | false | Read status |
| read_at | timestamptz | YES | - | Read timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 32: `calendar_integrations`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| provider | text | NO | - | 'google', 'outlook', etc. |
| external_account_id | text | YES | - | External account ID |
| access_token | text | YES | - | OAuth access token |
| refresh_token | text | YES | - | OAuth refresh token |
| token_expires_at | timestamptz | YES | - | Token expiration |
| sync_enabled | boolean | NO | true | Sync enabled |
| last_synced_at | timestamptz | YES | - | Last sync time |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(team_member_id, provider) WHERE is_deleted = false`
**Special RLS:** `owner_only` policy (not org_isolation)

#### Table 33: `comments`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| author_id | uuid | NO | - | FK → team_members |
| entity_type | text | NO | - | Commented entity type |
| entity_id | uuid | NO | - | Commented entity ID |
| parent_comment_id | uuid | YES | - | FK → comments (thread) |
| content | text | NO | - | Comment content |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 34: `activity_feed`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| actor_id | uuid | NO | - | FK → team_members |
| action | text | NO | - | Action verb |
| entity_type | text | NO | - | Entity type |
| entity_id | uuid | NO | - | Entity ID |
| entity_title | text | YES | - | Entity title (denormalized) |
| metadata | jsonb | YES | - | Additional data |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |

**Note:** Immutable table - no `updated_at` trigger

---

### 4.11 Settings Domain

#### Table 35: `user_settings`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| theme | text | YES | 'system' | UI theme |
| email_notifications | boolean | NO | true | Email notifications |
| push_notifications | boolean | NO | true | Push notifications |
| meeting_reminders | boolean | NO | true | Meeting reminders |
| task_reminders | boolean | NO | true | Task reminders |
| weekly_digest | boolean | NO | true | Weekly digest |
| default_meeting_duration | int | YES | 30 | Default meeting duration |
| timezone | text | YES | 'UTC' | Timezone |
| locale | text | YES | 'en-US' | Locale |
| settings_json | jsonb | NO | '{}' | Additional settings |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(team_member_id) WHERE is_deleted = false`

---

### 4.12 Development Domain

#### Table 36: `competencies`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| name | text | NO | - | Competency name |
| description | text | YES | - | Competency description |
| category | text | YES | - | Competency category |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Unique:** `(organization_id, lower(trim(name))) WHERE is_deleted = false`

#### Table 37: `team_member_competencies`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| competency_id | uuid | NO | - | FK → competencies |
| proficiency_level | int | YES | - | Level (1-5) |
| assessed_by | uuid | YES | - | FK → team_members |
| assessed_at | timestamptz | YES | - | Assessment timestamp |
| notes | text | YES | - | Assessment notes |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Constraint:** `proficiency_level >= 1 AND proficiency_level <= 5`
**Unique:** `(team_member_id, competency_id) WHERE is_deleted = false`

#### Table 38: `development_plans`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| team_member_id | uuid | NO | - | FK → team_members |
| title | text | NO | - | Plan title |
| description | text | YES | - | Plan description |
| status | text | NO | 'active' | Plan status |
| start_date | date | YES | - | Start date |
| target_date | date | YES | - | Target date |
| completed_at | timestamptz | YES | - | Completion timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 39: `development_plan_items`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| development_plan_id | uuid | NO | - | FK → development_plans |
| competency_id | uuid | YES | - | FK → competencies |
| title | text | NO | - | Item title |
| description | text | YES | - | Item description |
| item_type | text | YES | 'action' | Item type |
| status | text | NO | 'not_started' | Item status |
| due_date | date | YES | - | Due date |
| completed_at | timestamptz | YES | - | Completion timestamp |
| sort_order | int | NO | 0 | Display order |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.13 Recognition Domain

#### Table 40: `kudos`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| from_member_id | uuid | NO | - | FK → team_members |
| to_member_id | uuid | NO | - | FK → team_members |
| message | text | NO | - | Kudos message |
| category | text | YES | - | Kudos category |
| is_public | boolean | NO | true | Public visibility |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

---

### 4.14 Reviews Domain

#### Table 41: `review_cycles`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| name | text | NO | - | Cycle name |
| description | text | YES | - | Cycle description |
| cycle_type | text | NO | 'annual' | Cycle type |
| status | text | NO | 'draft' | Cycle status |
| start_date | date | NO | - | Cycle start |
| end_date | date | NO | - | Cycle end |
| review_start_date | date | YES | - | Review period start |
| review_end_date | date | YES | - | Review period end |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

#### Table 42: `performance_reviews`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| review_cycle_id | uuid | NO | - | FK → review_cycles |
| reviewee_id | uuid | NO | - | FK → team_members |
| reviewer_id | uuid | NO | - | FK → team_members |
| review_type | text | NO | 'manager' | Review type |
| status | text | NO | 'pending' | Review status |
| overall_rating | int | YES | - | Rating (1-5) |
| strengths | text | YES | - | Strengths text |
| areas_for_improvement | text | YES | - | Improvement areas |
| goals_for_next_period | text | YES | - | Next period goals |
| additional_comments | text | YES | - | Additional comments |
| submitted_at | timestamptz | YES | - | Submission timestamp |
| acknowledged_at | timestamptz | YES | - | Acknowledgement timestamp |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → users |

**Constraint:** `overall_rating >= 1 AND overall_rating <= 5`
**Unique:** `(review_cycle_id, reviewee_id, reviewer_id, review_type) WHERE is_deleted = false`

---

### 4.15 Audit Domain

#### Table 43: `audit_log`
| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → organizations |
| actor_id | uuid | YES | - | FK → users |
| team_member_id | uuid | YES | - | FK → team_members |
| action | text | NO | - | Action performed |
| entity_type | text | NO | - | Entity type |
| entity_id | uuid | YES | - | Entity ID |
| old_values | jsonb | YES | - | Previous values |
| new_values | jsonb | YES | - | New values |
| ip_address | inet | YES | - | Client IP |
| user_agent | text | YES | - | Client user agent |
| created_at | timestamptz | NO | now() | Event timestamp |

**Note:** Immutable table - no `updated_at`, no `is_deleted` filter on indexes

---

## 5. Entity Relationships

### 5.1 Relationship Diagram (Text)

```
public.organizations
    │
    ├──< procohere.roles
    │       │
    │       └──< procohere.team_members
    │               │
    ├──< procohere.teams ─────< procohere.teams (parent_team_id)
    │       │
    │       └── lead_team_member_id ──> procohere.team_members
    │
    ├──< procohere.org_settings (1:1)
    │
    ├──< procohere.meetings ───< procohere.meetings (parent_meeting_id)
    │       │
    │       ├──< procohere.meeting_attendees
    │       ├──< procohere.meeting_agenda_items
    │       ├──< procohere.meeting_notes
    │       └──< procohere.meeting_summaries (1:1)
    │
    ├──< procohere.goals ──────< procohere.goals (parent_goal_id)
    │       │
    │       └──< procohere.targets
    │
    ├──< procohere.surveys
    │       │
    │       ├──< procohere.survey_questions
    │       └──< procohere.survey_responses
    │               │
    │               └──< procohere.survey_answers
    │
    ├──< procohere.ai_conversations
    │       │
    │       └──< procohere.ai_messages
    │
    ├──< procohere.development_plans
    │       │
    │       └──< procohere.development_plan_items
    │
    ├──< procohere.review_cycles
    │       │
    │       └──< procohere.performance_reviews
    │
    └──< procohere.competencies
            │
            └──< procohere.team_member_competencies
```

### 5.2 Self-Referential Tables

| Table | Column | Purpose |
|-------|--------|---------|
| team_members | manager_team_member_id | Reporting hierarchy |
| teams | parent_team_id | Team hierarchy |
| meetings | parent_meeting_id | Recurring meeting series |
| goals | parent_goal_id | Goal cascade/alignment |
| comments | parent_comment_id | Threaded comments |

### 5.3 Polymorphic Relationships (entity_type + entity_id)

| Table | Used For |
|-------|----------|
| meeting_agenda_items | linked_entity_type/id |
| tasks | source_type/id |
| ai_conversations | context_type/id |
| ai_insights | source_type/id |
| attachments | entity_type/id |
| entity_tags | entity_type/id |
| notifications | entity_type/id |
| comments | entity_type/id |
| activity_feed | entity_type/id |

---

## 6. Indexes & Performance

### 6.1 Index Patterns

All tables follow these index patterns:

1. **Organization Index** (all tables)
   ```sql
   CREATE INDEX idx_{table}_org ON procohere.{table}(organization_id) WHERE is_deleted = false;
   ```

2. **Foreign Key Indexes** (where applicable)
   ```sql
   CREATE INDEX idx_{table}_{fk} ON procohere.{table}({fk_column}) WHERE is_deleted = false;
   ```

3. **Unique Constraints** (with soft delete)
   ```sql
   CREATE UNIQUE INDEX uq_{table}_{columns} ON procohere.{table}({columns}) WHERE is_deleted = false;
   ```

### 6.2 Special Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| meetings | idx_meetings_scheduled | Query by schedule |
| meetings | idx_meetings_status | Query by status |
| goals | idx_goals_due_date | Query by due date |
| notifications | idx_notifications_unread | Unread notifications |
| activity_feed | idx_activity_feed_created | Recent activity |
| audit_log | idx_audit_log_created | Time-based queries |

---

## 7. Conventions & Patterns

### 7.1 Standard Columns (All Tables)

| Column | Type | Purpose |
|--------|------|---------|
| id | uuid | Primary key (gen_random_uuid()) |
| organization_id | uuid | Tenant isolation |
| is_deleted | boolean | Soft delete flag |
| created_at | timestamptz | Creation timestamp |
| updated_at | timestamptz | Last modification |
| deleted_at | timestamptz | Deletion timestamp |
| deleted_by | uuid | Who deleted (FK → users) |

### 7.2 Soft Delete Pattern

- **Never hard delete** - always set `is_deleted = true`
- Set `deleted_at = now()` and `deleted_by = auth.uid()` when deleting
- All queries filter `WHERE is_deleted = false` (enforced via partial indexes)
- `deleted_by` references `public.users(id)` (not team_members) for audit trail

### 7.3 Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Tables | snake_case, plural | `meeting_attendees` |
| Columns | snake_case | `team_member_id` |
| Indexes | idx_{table}_{columns} | `idx_goals_owner` |
| Unique | uq_{table}_{columns} | `uq_tags_org_name` |
| Triggers | tr_{table}_{action} | `tr_goals_set_updated_at` |
| Policies | descriptive_name | `org_isolation` |

### 7.4 Role Inheritance Rule

Team members inherit role from `procohere.roles.permissions`. The application enforces:
- Admin roles can modify all org data
- Manager roles can modify direct reports' data
- Individual roles can only modify own data

---

## 8. Enum Values & Expected Types

### 8.1 Meeting Types
- `one_on_one` - 1:1 meeting
- `team` - Team meeting
- `all_hands` - All-hands meeting
- `skip_level` - Skip-level meeting
- `performance` - Performance review meeting

### 8.2 Meeting Status
- `scheduled` - Meeting scheduled
- `in_progress` - Meeting in progress
- `completed` - Meeting completed
- `cancelled` - Meeting cancelled

### 8.3 Attendee Roles
- `organizer` - Meeting organizer
- `attendee` - Regular attendee
- `optional` - Optional attendee

### 8.4 Response Status
- `pending` - Awaiting response
- `accepted` - Accepted
- `declined` - Declined
- `tentative` - Tentative

### 8.5 Goal Types
- `individual` - Individual goal
- `team` - Team goal
- `department` - Department goal
- `organization` - Organization-wide goal

### 8.6 Goal/Target Status
- `not_started` - Not started
- `in_progress` - In progress
- `at_risk` - At risk
- `on_track` - On track
- `completed` - Completed
- `cancelled` - Cancelled

### 8.7 Priority Levels
- `low` - Low priority
- `medium` - Medium priority
- `high` - High priority
- `critical` - Critical priority

### 8.8 Task Status
- `todo` - To do
- `in_progress` - In progress
- `blocked` - Blocked
- `done` - Done

### 8.9 Feedback Types
- `general` - General feedback
- `praise` - Praise/recognition
- `constructive` - Constructive feedback
- `performance` - Performance feedback

### 8.10 Visibility Levels
- `private` - Private (author only)
- `manager` - Manager visible
- `team` - Team visible
- `organization` - Organization visible

### 8.11 Survey Status
- `draft` - Draft
- `active` - Active/open
- `closed` - Closed
- `archived` - Archived

### 8.12 Question Types
- `text` - Free text
- `number` - Numeric input
- `scale` - 1-N scale
- `single_choice` - Single selection
- `multiple_choice` - Multiple selection
- `date` - Date input

### 8.13 Metric Direction
- `higher_is_better` - Higher values are better
- `lower_is_better` - Lower values are better
- `target_is_better` - Closer to target is better

### 8.14 Review Types
- `manager` - Manager review
- `self` - Self review
- `peer` - Peer review
- `skip_level` - Skip-level review

### 8.15 Calendar Providers
- `google` - Google Calendar
- `outlook` - Microsoft Outlook
- `apple` - Apple Calendar

---

## 9. Triggers

### 9.1 Updated_at Trigger

All mutable tables have this trigger:

```sql
DROP TRIGGER IF EXISTS tr_{table}_set_updated_at ON procohere.{table};
CREATE TRIGGER tr_{table}_set_updated_at
    BEFORE UPDATE ON procohere.{table}
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
```

### 9.2 Tables Without updated_at Trigger

| Table | Reason |
|-------|--------|
| activity_feed | Immutable (append-only) |
| audit_log | Immutable (append-only) |

---

## 10. RLS Policies

### 10.1 Standard Policy (42 tables)

```sql
CREATE POLICY org_isolation ON procohere.{table}
    FOR ALL
    USING (organization_id = ANY(procohere.get_user_org_ids()))
    WITH CHECK (organization_id = ANY(procohere.get_user_org_ids()));
```

### 10.2 Special Policy (calendar_integrations)

```sql
CREATE POLICY owner_only ON procohere.calendar_integrations
    FOR ALL
    USING (
        team_member_id IN (
            SELECT id FROM procohere.team_members
            WHERE linked_user_id = auth.uid()
              AND is_deleted = false
        )
    )
    WITH CHECK (
        team_member_id IN (
            SELECT id FROM procohere.team_members
            WHERE linked_user_id = auth.uid()
              AND is_deleted = false
        )
    );
```

### 10.3 Helper Function

```sql
CREATE OR REPLACE FUNCTION procohere.get_user_org_ids()
RETURNS uuid[]
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = procohere, public
AS $$
    SELECT array_agg(DISTINCT o.id)
    FROM public.organizations o
    JOIN public.organization_members om ON om.organization_id = o.id
    WHERE om.user_id = auth.uid()
      AND om.is_deleted = false
      AND o.is_deleted = false;
$$;
```

---

## 11. Grants & Permissions

### 11.1 Table Grants

All 43 tables receive SELECT-only grants:

```sql
GRANT SELECT ON procohere.{table} TO authenticated;
```

### 11.2 Function Grants

```sql
GRANT EXECUTE ON FUNCTION procohere.get_user_org_ids() TO authenticated;
```

### 11.3 Write Operations

All INSERT, UPDATE, DELETE operations must go through RPC functions (to be created separately). This ensures:

1. Business logic validation
2. Audit logging
3. Consistent organization_id assignment
4. Proper deleted_by tracking

---

## Appendix A: Deployment Checklist

1. ☐ Verify public schema prerequisites exist
2. ☐ Run `01_PROCOHERE_TABLES.sql`
3. ☐ Run `02_PROCOHERE_RLS_POLICIES.sql`
4. ☐ Verify RLS is enabled: `SELECT tablename, rowsecurity FROM pg_tables WHERE schemaname = 'procohere'`
5. ☐ Test helper function: `SELECT procohere.get_user_org_ids()`
6. ☐ Create RPC functions for writes
7. ☐ Seed initial data (roles, org_settings)

---

## Appendix B: Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-17 | Initial specification |

---

*Document generated for ProCohere schema v1.0*
