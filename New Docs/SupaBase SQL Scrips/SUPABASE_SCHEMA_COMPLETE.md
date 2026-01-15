# Supabase PostgreSQL Complete Schema

**Source:** Live Supabase instance + SQL migration scripts  
**Last Updated:** January 15, 2026  
**Database:** PostgreSQL (Supabase)  
**All IDs:** UUID (not int)

---

## SCHEMA CHANGELOG

### January 15, 2026 (continued)
- **meeting_agenda_items:** Added `linked_entity_type`, `linked_entity_id` for bidirectional entity linking
- **goals:** Added `source_agenda_item_id`, `source_meeting_id` for provenance tracking
- **DELETED MODELS:** Removed `MeetingLinkedGoal.cs`, `MeetingLinkedTask.cs`, `MeetingMetricLink.cs` (wrong design - replaced with provenance tracking)

### January 15, 2026
- **meetings:** Added `project_id`, `deleted_by`, all generic calendar sync columns (`calendar_event_id`, `calendar_provider`, `calendar_etag`, `calendar_link_id`, `calendar_sync_status`, `last_synced_at`), and video conference columns (`video_conference_url`, `video_conference_provider`, `video_conference_id`)
- **meeting_attendees:** Added `external_attendee_email`, `removed_from_calendar_at`, `sync_status` 
- **goals:** Added `type` column (goal_type enum: organizational, team, personal)
- **tasks:** Added `source_agenda_item_id`, `source_meeting_id`, `notes`
- **projects:** Added `source_agenda_item_id`, `source_meeting_id`
- **calendar_links:** Added `sync_token` for delta sync
- **ai_insights:** Added `severity`, `description`, `action_suggestion`, `entity_type`, `entity_id`, `generated_at`
- **vector_embeddings:** Added `chunk_index`, `content`, `embedding_dimensions`, `is_deleted`, `deleted_at`, `deleted_by`

### January 14, 2026
- **users:** Added `firm_id`, `username`, `is_admin`, `role`, `password_hash`
- **notes:** Added `is_archived`, `archived_at`

---

## MEETINGS TABLE

**Purpose:** Unified meeting model (1:1s, team meetings, all-hands, etc.)  
**Total Columns:** 32

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key, auto-generated |
| `organization_id` | uuid | NO | FK to organizations |
| `created_by_user_id` | uuid | NO | Who created the meeting |
| `meeting_type` | meeting_type (enum) | NO | one_on_one, team_meeting, all_hands, project, interview, other |
| `manager_team_member_id` | uuid | YES | Manager/lead for the meeting |
| `report_team_member_id` | uuid | YES | Attendee/report (for 1:1s) |
| `team_id` | uuid | YES | Team this meeting belongs to |
| `project_id` | uuid | YES | FK to projects (for project meetings) |
| `title` | varchar | NO | Meeting title |
| `description` | text | YES | Meeting description |
| `scheduled_at` | timestamptz | YES | Scheduled start time |
| `duration_minutes` | int4 | NO | Duration in minutes |
| `recurrence_rule` | varchar | YES | RRULE for recurring meetings |
| `location` | varchar | YES | Physical or virtual location |
| `status` | meeting_status (enum) | NO | scheduled, in_progress, completed, cancelled |
| `started_at` | timestamptz | YES | When meeting actually started |
| `ended_at` | timestamptz | YES | When meeting actually ended |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |
| `calendar_event_id` | varchar(500) | YES | External calendar event ID (Google/Outlook/etc) |
| `calendar_provider` | varchar(50) | YES | Calendar provider: google, microsoft, apple, other |
| `calendar_etag` | varchar(500) | YES | ETag for change detection |
| `calendar_link_id` | uuid | YES | FK to calendar_links (OAuth connection used) |
| `calendar_sync_status` | varchar(50) | YES | Sync status: synced, pending, out_of_sync, error |
| `last_synced_at` | timestamptz | YES | Last successful calendar sync |
| `video_conference_url` | text | YES | Video conference URL (Teams/Meet/Zoom) |
| `video_conference_provider` | varchar(50) | YES | Video provider: teams, google_meet, zoom, webex |
| `video_conference_id` | varchar(255) | YES | Provider-specific meeting ID |

**NOTE:** Calendar sync uses a generic approach - one provider at a time per meeting.
The `calendar_link_id` references the OAuth connection in `calendar_links` table.

---

## MEETING_ATTENDEES TABLE

**Purpose:** Participants for meetings  
**Total Columns:** 9

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key, auto-generated |
| `meeting_id` | uuid | NO | FK to meetings |
| `team_member_id` | uuid | NO | FK to team_members |
| `response` | varchar(50) | YES | Response: accepted, declined, tentative, none |
| `created_at` | timestamptz | NO | Audit timestamp |
| `external_attendee_email` | varchar(255) | YES | Email for external calendar invites (overrides team_members.email) |
| `removed_from_calendar_at` | timestamptz | YES | When attendee removed/declined from external calendar |
| `sync_status` | varchar(50) | YES | Per-attendee sync status: synced, pending, out_of_sync, error |

**NOTE:** This table does NOT have soft delete columns (is_deleted, deleted_at, deleted_by).
This table does NOT have an updated_at column.

---

## MEETING_AGENDA_ITEMS TABLE

**Purpose:** Agenda items/topics for meetings  
**Total Columns:** 11

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key, auto-generated |
| `meeting_id` | uuid | NO | FK to meetings |
| `title` | varchar | NO | Agenda item title |
| `description` | text | YES | Detailed description |
| `duration_minutes` | int4 | YES | Expected duration for this item |
| `order_index` | int4 | NO | Sort order (0-based) |
| `is_completed` | bool | NO | Whether item was covered |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `linked_entity_type` | varchar(50) | YES | Type of entity being discussed: task, goal, metric, project |
| `linked_entity_id` | uuid | YES | FK to the entity being discussed |

**NOTE:** This table does NOT have soft delete columns (is_deleted, deleted_at, deleted_by).
Agenda items can link to existing entities for discussion, or be standalone topics.

---

## TEAM_MEMBERS TABLE

**Purpose:** Employees/staff being tracked  
**Total Columns:** 34

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `user_id` | uuid | YES | FK to users (if this person also uses Tracker) |
| `email` | varchar | NO | Email address (unique within org) |
| `full_name` | varchar | NO | First and last name |
| `first_name` | varchar | NO | First name |
| `last_name` | varchar | NO | Last name |
| `title` | varchar | YES | Job title |
| `department` | varchar | YES | Department name |
| `manager_id` | uuid | YES | FK to team_members (their manager) |
| `hire_date` | date | YES | Date hired |
| `is_active` | bool | NO | Active/inactive flag |
| `notes` | text | YES | Rich text notes |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |

**Profile & Social (columns 18+):**
- profile_image_url
- linkedin_profile
- twitter_profile
- instagram_profile
- And more...

---

## GOALS TABLE

**Purpose:** OKRs and goals  
**Total Columns:** 29

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `owner_team_member_id` | uuid | YES | Person this goal is for |
| `created_by_user_id` | uuid | NO | Who created it |
| `title` | varchar | NO | Goal title |
| `description` | text | YES | Goal description |
| `status` | goal_status (enum) | NO | draft, active, completed, on_hold, cancelled |
| `type` | goal_type (enum) | NO | organizational, team, personal (DEFAULT: organizational) |
| `period_start` | date | YES | Start date |
| `period_end` | date | YES | End date |
| `progress_percent` | numeric | YES | Calculated progress 0-100 |
| `priority` | int4 | YES | Priority (0=low to 3=critical) |
| `notes` | text | YES | Additional notes |
| `source_agenda_item_id` | uuid | YES | FK to meeting_agenda_items (provenance) |
| `source_meeting_id` | uuid | YES | FK to meetings (provenance, denormalized) |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `type` column uses PostgreSQL enum `goal_type` with values: organizational, team, personal.
`source_agenda_item_id` and `source_meeting_id` track where a goal originated from (if created from a meeting).

---

## METRICS TABLE

**Purpose:** KPIs and performance metrics (replaces KeyPerformanceIndicator)  
**Total Columns:** 29

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `owner_team_member_id` | uuid | YES | Person responsible |
| `created_by_user_id` | uuid | NO | Who created it |
| `name` | varchar | NO | Metric name |
| `description` | text | YES | What this metric measures |
| `metric_type` | varchar | NO | quantitative, qualitative, binary |
| `status` | metric_status (enum) | NO | active, on_hold, archived, discontinued |
| `current_value` | numeric | YES | Current/latest value |
| `target_value` | numeric | YES | Target value |
| `unit_of_measure` | varchar | YES | % or absolute numbers |
| `frequency` | varchar | NO | weekly, monthly, quarterly, annual |
| `data_source` | text | YES | Where data comes from |
| `notes` | text | YES | Additional notes |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |

---

## TASKS TABLE

**Purpose:** Individual work items (replaces IndividualTask)  
**Total Columns:** 27

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `owner_team_member_id` | uuid | NO | Who owns this task |
| `created_by_user_id` | uuid | NO | Who created it |
| `parent_task_id` | uuid | YES | FK to tasks (for subtasks) |
| `project_id` | uuid | YES | FK to projects |
| `goal_id` | uuid | YES | FK to goals |
| `meeting_id` | uuid | YES | FK to meetings (action items) |
| `source_agenda_item_id` | uuid | YES | FK to meeting_agenda_items (source agenda item) |
| `source_meeting_id` | uuid | YES | FK to meetings (source meeting for provenance) |
| `title` | varchar | NO | Task title |
| `description` | text | YES | Task description |
| `status` | task_status (enum) | NO | not_started, in_progress, blocked, completed, cancelled |
| `priority` | int4 | NO | 0=Low, 1=Medium, 2=High, 3=Critical |
| `due_date` | timestamptz | YES | When it's due |
| `completed_at` | timestamptz | YES | When completed |
| `estimated_hours` | numeric | YES | Time estimate |
| `actual_hours` | numeric | YES | Time spent |
| `notes` | text | YES | Additional notes |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `source_agenda_item_id` and `source_meeting_id` track provenance (where task originated from).

---

## PROJECTS TABLE

**Purpose:** Project tracking and organization  
**Total Columns:** 21

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `created_by_user_id` | uuid | NO | Who created it |
| `owner_team_member_id` | uuid | YES | Project owner/lead |
| `team_id` | uuid | YES | FK to teams |
| `name` | varchar | NO | Project name |
| `description` | text | YES | Project description |
| `status` | varchar(50) | NO | Project status |
| `start_date` | date | YES | Project start date |
| `end_date` | date | YES | Project end date |
| `source_agenda_item_id` | uuid | YES | FK to meeting_agenda_items (source agenda item) |
| `source_meeting_id` | uuid | YES | FK to meetings (source meeting for provenance) |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `source_agenda_item_id` and `source_meeting_id` track provenance (where project originated from).

---

## USERS TABLE

**Purpose:** Application users (managers who use Tracker)  
**Total Columns:** 29

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key (matches Supabase auth.users.id) |
| `organization_id` | uuid | NO | FK to organizations |
| `email` | varchar | NO | Email address |
| `full_name` | varchar | YES | Full name |
| `first_name` | varchar | YES | First name |
| `last_name` | varchar | YES | Last name |
| `firm_id` | uuid | YES | FK to firms (licensing) |
| `username` | varchar(200) | YES | Login identifier (Windows username, SSO, etc.) |
| `is_admin` | bool | NO | Administrator privileges (DEFAULT: false) |
| `role` | varchar(50) | NO | Primary role: admin, hr_admin, manager, viewer (DEFAULT: manager) |
| `password_hash` | text | YES | BCrypt hash for local auth (NULL for Supabase Auth) |
| `is_active` | bool | NO | Whether user can log in |
| `avatar_url` | text | YES | Profile picture URL |
| `preferences` | jsonb | YES | User preferences JSON |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |
| `last_login_at` | timestamptz | YES | Last successful login |

**NOTE:** `firm_id`, `username`, `is_admin`, `role`, `password_hash` added via ALTER script.

---

## NOTES TABLE

**Purpose:** Quick notes and annotations  
**Total Columns:** 29

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `created_by_user_id` | uuid | NO | Who created it |
| `team_member_id` | uuid | YES | FK to team_members (associated person) |
| `meeting_id` | uuid | YES | FK to meetings (associated meeting) |
| `title` | varchar | YES | Note title |
| `content` | text | YES | Note content (rich text) |
| `note_type` | varchar(50) | YES | Type of note |
| `is_pinned` | bool | NO | Whether pinned to top |
| `is_archived` | bool | NO | Whether archived (DEFAULT: false) |
| `archived_at` | timestamptz | YES | When archived |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `is_archived`, `archived_at` added via ALTER script for archive functionality.

---

## CALENDAR_LINKS TABLE

**Purpose:** OAuth connections to external calendar providers  
**Total Columns:** 21

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `user_id` | uuid | NO | FK to users (who connected this calendar) |
| `organization_id` | uuid | NO | FK to organizations |
| `provider` | varchar(50) | NO | Calendar provider: google, microsoft, apple |
| `account_email` | varchar | NO | Email of the connected calendar account |
| `access_token` | text | YES | OAuth access token (encrypted) |
| `refresh_token` | text | YES | OAuth refresh token (encrypted) |
| `token_expires_at` | timestamptz | YES | When access token expires |
| `sync_token` | text | YES | Delta sync token for incremental sync |
| `is_primary` | bool | NO | Whether this is the user's primary calendar |
| `sync_enabled` | bool | NO | Whether sync is enabled (DEFAULT: true) |
| `last_synced_at` | timestamptz | YES | Last successful sync |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `sync_token` added via ALTER script for delta/incremental sync support.

---

## AI_INSIGHTS TABLE

**Purpose:** AI-generated insights and recommendations  
**Total Columns:** 22

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `user_id` | uuid | YES | FK to users (target user for insight) |
| `team_member_id` | uuid | YES | FK to team_members (related team member) |
| `insight_type` | varchar(50) | NO | Type: pattern, anomaly, recommendation, alert |
| `category` | varchar(50) | YES | Category: performance, engagement, workload, etc. |
| `summary` | text | NO | Short summary of the insight |
| `description` | text | YES | Detailed explanation |
| `action_suggestion` | text | YES | Recommended action text |
| `severity` | varchar(20) | NO | info, warning, error, critical (DEFAULT: info) |
| `priority` | int4 | YES | Action urgency (0-3) |
| `entity_type` | varchar(50) | YES | Related entity type: task, goal, metric, etc. |
| `entity_id` | uuid | YES | FK to related entity |
| `is_dismissed` | bool | NO | Whether user dismissed this insight |
| `dismissed_at` | timestamptz | YES | When dismissed |
| `generated_at` | timestamptz | NO | When AI generated this insight (DEFAULT: now()) |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `severity`, `description`, `action_suggestion`, `entity_type`, `entity_id`, `generated_at` added via ALTER script.

---

## VECTOR_EMBEDDINGS TABLE

**Purpose:** pgvector embeddings for AI semantic search  
**Total Columns:** 18

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `entity_type` | varchar(50) | NO | Type: task, goal, note, meeting, team_member |
| `entity_id` | uuid | NO | FK to the embedded entity |
| `embedding` | vector(1536) | NO | pgvector embedding (OpenAI ada-002 = 1536 dims) |
| `embedding_dimensions` | int4 | NO | Vector dimensions (DEFAULT: 1536) |
| `chunk_index` | int4 | NO | Chunk index for split content (DEFAULT: 0) |
| `content` | text | YES | Original text that was embedded |
| `content_preview` | varchar(500) | YES | Truncated preview of content |
| `model_name` | varchar(100) | YES | Embedding model used |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag (DEFAULT: false) |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | FK to users who deleted |

**NOTE:** `chunk_index`, `content`, `embedding_dimensions`, `is_deleted`, `deleted_at`, `deleted_by` added via ALTER script.

---

## KEY DATATYPE NOTES

**All IDs:** `uuid` (NOT int)
**All timestamps:** `timestamptz` (with timezone)
**All soft deletes:** `is_deleted` bool + `deleted_at` timestamptz

---

## C# MODEL EXPECTATIONS

Based on schema, C# models should have:

```csharp
public Guid Id { get; set; }                    // UUID
public Guid OrganizationId { get; set; }        // Required FK
public Guid CreatedByUserId { get; set; }       // Required audit
public DateTime CreatedAt { get; set; }         // Audit
public DateTime UpdatedAt { get; set; }         // Audit
public bool IsDeleted { get; set; }             // Soft delete
public DateTime? DeletedAt { get; set; }        // Soft delete
```

**NOT:**
```csharp
public int Id { get; set; }          // ❌ WRONG - schema is UUID
public string ID { get; set; }       // ❌ WRONG - capitalization 
public int? LegacyId { get; set; }   // ❌ WRONG - remove legacy
```

---

## NEXT STEP

Map each C# DataModel to its schema table and fix ID types and property names.

