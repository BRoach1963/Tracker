# Supabase PostgreSQL Complete Schema

**Source:** Live Supabase instance + SQL migration scripts  
**Last Updated:** January 12, 2026  
**Database:** PostgreSQL (Supabase)  
**All IDs:** UUID (not int)

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

**Additional columns visible in UI (scroll right in pgAdmin):**
- Google Calendar fields (if implemented)
- Outlook Calendar fields (if implemented)
- Teams Meeting URL
- Google Meet URL

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
**Total Columns:** 26

| Column Name | Data Type | Nullable | Notes |
|---|---|---|---|
| `id` | uuid | NO | Primary Key |
| `organization_id` | uuid | NO | FK to organizations |
| `owner_team_member_id` | uuid | YES | Person this goal is for |
| `created_by_user_id` | uuid | NO | Who created it |
| `title` | varchar | NO | Goal title |
| `description` | text | YES | Goal description |
| `status` | goal_status (enum) | NO | draft, active, completed, on_hold, cancelled |
| `goal_type` | varchar | NO | personal, team, organizational |
| `period_start` | date | YES | Start date |
| `period_end` | date | YES | End date |
| `progress_percent` | numeric | YES | Calculated progress 0-100 |
| `priority` | int4 | YES | Priority (0=low to 3=critical) |
| `notes` | text | YES | Additional notes |
| `created_at` | timestamptz | NO | Audit timestamp |
| `updated_at` | timestamptz | NO | Audit timestamp |
| `is_deleted` | bool | NO | Soft delete flag |
| `deleted_at` | timestamptz | YES | When soft deleted |

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
**Total Columns:** 24

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

