# 06i – Surveys Tables

This document covers the **Surveys** domain tables in the `procohere` schema.

---

## procohere.surveys

**Purpose**  
Survey definitions with support for recurring surveys, targeting, and UX customization.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| created_by | uuid | NO | - | FK → procohere.team_members.id |
| title | text | NO | - | Survey title |
| description | text | YES | - | Survey description |
| status | text | NO | 'draft' | Status: 'draft', 'active', 'closed', 'archived' |
| is_anonymous | boolean | NO | false | Whether responses are anonymous |
| starts_at | timestamptz | YES | - | When survey opens for responses |
| ends_at | timestamptz | YES | - | When survey closes |
| survey_type | text | NO | 'custom' | Type: 'pulse', 'engagement', 'custom' |
| frequency | text | NO | 'one_time' | Frequency: 'one_time', 'weekly', 'biweekly', 'monthly', 'quarterly' |
| next_send_date | timestamptz | YES | - | Next instance send date (recurring) |
| target_all_employees | boolean | NO | true | Send to all employees? |
| target_team_ids | uuid[] | YES | - | Specific teams to target |
| target_team_member_ids | uuid[] | YES | - | Specific team members to target |
| allow_comments | boolean | NO | true | Allow freeform comments |
| reminder_enabled | boolean | NO | false | Send reminders? |
| reminder_days_before_close | integer | YES | - | Days before close to remind |
| welcome_message | text | YES | - | Message shown before starting |
| thank_you_message | text | YES | - | Message shown after completion |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**Indexes**
- `idx_surveys_org` on (organization_id) WHERE is_deleted = false
- `idx_surveys_created_by` on (created_by) WHERE is_deleted = false
- `idx_surveys_status` on (status) WHERE is_deleted = false

**Triggers**
- `tr_surveys_set_updated_at` → public.set_updated_at()

**RLS**  
Organization isolation. Creator and targeted members have access.

**Model**: `ProCohere.Avalonia.Models.Survey`

---

## procohere.survey_questions

**Purpose**  
Questions within a survey, supporting multiple question types.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| survey_id | uuid | NO | - | FK → procohere.surveys.id |
| question_text | text | NO | - | The question text |
| question_type | text | NO | 'text' | Type: 'text', 'rating', 'choice', 'multi_choice' |
| options | jsonb | YES | - | Options for choice questions |
| is_required | boolean | NO | false | Whether answer is required |
| sort_order | integer | NO | 0 | Display order |
| min_value | integer | YES | - | Min value for rating questions |
| max_value | integer | YES | - | Max value for rating questions |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**Indexes**
- `idx_survey_questions_org` on (organization_id) WHERE is_deleted = false
- `idx_survey_questions_survey` on (survey_id) WHERE is_deleted = false

**Triggers**
- `tr_survey_questions_set_updated_at` → public.set_updated_at()

**RLS**  
Inherited from parent survey visibility.

**Model**: `ProCohere.Avalonia.Models.SurveyQuestion`

---

## procohere.survey_responses

**Purpose**  
A respondent's submission header record.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| survey_id | uuid | NO | - | FK → procohere.surveys.id |
| respondent_id | uuid | YES | - | FK → procohere.team_members.id (null if anonymous) |
| submitted_at | timestamptz | YES | - | When response was submitted |
| is_complete | boolean | NO | false | All required questions answered? |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**Indexes**
- `idx_survey_responses_org` on (organization_id) WHERE is_deleted = false
- `idx_survey_responses_survey` on (survey_id) WHERE is_deleted = false
- `idx_survey_responses_respondent` on (respondent_id) WHERE is_deleted = false AND respondent_id IS NOT NULL

**Unique Constraints**
- `uq_survey_responses_respondent` on (survey_id, respondent_id) WHERE is_deleted = false AND respondent_id IS NOT NULL

**Triggers**
- `tr_survey_responses_set_updated_at` → public.set_updated_at()

**RLS**  
Respondent owns their response. Survey creator can see aggregate/anonymous data.

**Model**: `ProCohere.Avalonia.Models.SurveyResponse`

---

## procohere.survey_answers

**Purpose**  
Individual answer to a specific question within a response.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| response_id | uuid | NO | - | FK → procohere.survey_responses.id |
| question_id | uuid | NO | - | FK → procohere.survey_questions.id |
| answer_text | text | YES | - | Text answer |
| answer_numeric | numeric | YES | - | Numeric answer (ratings) |
| answer_json | jsonb | YES | - | JSON answer (multi-choice) |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**Indexes**
- `idx_survey_answers_org` on (organization_id) WHERE is_deleted = false
- `idx_survey_answers_response` on (response_id) WHERE is_deleted = false
- `idx_survey_answers_question` on (question_id) WHERE is_deleted = false

**Unique Constraints**
- `uq_survey_answers_response_question` on (response_id, question_id) WHERE is_deleted = false

**Triggers**
- `tr_survey_answers_set_updated_at` → public.set_updated_at()

**RLS**  
Inherited from parent response visibility.

**Model**: `ProCohere.Avalonia.Models.SurveyAnswer`

---

## procohere.survey_instances

**Purpose**  
Individual "sends" of a recurring survey. Each instance tracks its own response window.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| survey_id | uuid | NO | - | FK → procohere.surveys.id |
| instance_number | integer | NO | 1 | Sequential instance number |
| status | text | NO | 'pending' | Status: 'pending', 'sent', 'active', 'closed' |
| sent_at | timestamptz | YES | - | When instance was sent |
| closes_at | timestamptz | YES | - | When instance closes |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**Triggers**
- `tr_survey_instances_set_updated_at` → public.set_updated_at()

**RLS**  
Inherited from parent survey visibility.

**Model**: `ProCohere.Avalonia.Models.SurveyInstance`

---

## Entity Relationships

```
surveys (1) ──────────────< survey_questions (many)
    │
    │
    ├──────────────────────< survey_instances (many) [for recurring]
    │
    └──────────────────────< survey_responses (many)
                                    │
                                    └────< survey_answers (many)
                                                  │
                                                  └──── references survey_questions
```

---

## Related Models

All models in `ProCohere.Avalonia/Models/Survey.cs`:
- `Survey` - Survey definition with targeting and scheduling
- `SurveyQuestion` - Question within a survey
- `SurveyResponse` - Respondent's submission header
- `SurveyAnswer` - Individual answer to a question
- `SurveyInstance` - Instance of a recurring survey
