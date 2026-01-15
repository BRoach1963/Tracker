# Supabase Schema - Live Export

**Source:** Direct query from live Supabase database  
**Exported:** January 14, 2026  
**Total Tables:** 65  
**All IDs:** UUID (gen_random_uuid())

---

## Table Summary (65 Tables)

| Table Name | Column Count | Category |
|------------|--------------|----------|
| action_items | 11 | Meetings |
| activity_log | 14 | Infrastructure |
| ai_conversations | 11 | AI |
| ai_insights | 34 | AI |
| ai_messages | 11 | AI |
| announcement_reads | 4 | Communications |
| announcements | 17 | Communications |
| calendar_links | 20 | Integration |
| development_goal_comments | 7 | Development |
| development_goal_milestones | 11 | Development |
| development_goals | 22 | Development |
| feedback | 25 | Feedback |
| feedback_requests | 16 | Feedback |
| goal_milestones | 10 | Goals |
| goals | 26 | Goals |
| journal_entries | 17 | Notes |
| manager_history | 9 | Teams |
| meeting_agenda_items | 12 | Meetings |
| meeting_attendees | 7 | Meetings |
| meeting_notes | 12 | Meetings |
| meetings | 32 | Meetings |
| metric_data_sources | 8 | Metrics |
| metric_history | 7 | Metrics |
| metrics | 29 | Metrics |
| milestones | 10 | Projects |
| note_templates | 13 | Notes |
| notes | 29 | Notes |
| notification_preferences | 9 | Infrastructure |
| notifications | 18 | Infrastructure |
| organization_snapshots | 23 | Analytics |
| organizations | 11 | Core |
| performance_reviews | 24 | Reviews |
| progress_snapshots | 10 | Analytics |
| project_members | 5 | Projects |
| projects | 21 | Projects |
| recognition | 16 | Recognition |
| recognition_reactions | 5 | Recognition |
| reminder_preferences | 11 | Infrastructure |
| reminders | 22 | Infrastructure |
| review_cycles | 17 | Reviews |
| review_responses | 10 | Reviews |
| review_template_questions | 13 | Reviews |
| review_template_sections | 8 | Reviews |
| review_templates | 13 | Reviews |
| reviews | 19 | Reviews |
| roles | 37 | Security |
| survey_answers | 7 | Surveys |
| survey_instances | 11 | Surveys |
| survey_questions | 14 | Surveys |
| survey_responses | 9 | Surveys |
| surveys | 24 | Surveys |
| talking_points | 12 | Meetings |
| target_measurables | 6 | Goals |
| targets | 15 | Goals |
| task_collection_items | 4 | Tasks |
| task_collections | 7 | Tasks |
| tasks | 27 | Tasks |
| team_member_snapshots | 25 | Analytics |
| team_members | 34 | Teams |
| team_memberships | 8 | Teams |
| team_snapshots | 23 | Analytics |
| teams | 13 | Teams |
| user_roles | 7 | Security |
| user_sessions | 16 | Infrastructure |
| users | 29 | Core |
| vector_embeddings | 18 | AI |

**Total Columns:** ~950+

---

## Detailed Table Schemas

### action_items (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | meeting_id | uuid | NO | - |
| 3 | assignee_team_member_id | uuid | YES | - |
| 4 | title | varchar(300) | NO | - |
| 5 | description | text | YES | - |
| 6 | due_date | date | YES | - |
| 7 | is_completed | boolean | NO | false |
| 8 | completed_at | timestamptz | YES | - |
| 9 | converted_task_id | uuid | YES | - |
| 10 | created_at | timestamptz | NO | now() |
| 11 | updated_at | timestamptz | NO | now() |

---

### activity_log (14 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | actor_user_id | uuid | NO | - |
| 4 | actor_team_member_id | uuid | YES | - |
| 5 | action | varchar(100) | NO | - |
| 6 | entity_type | varchar(50) | NO | - |
| 7 | entity_id | uuid | NO | - |
| 8 | entity_name | varchar(300) | YES | - |
| 9 | old_values | jsonb | YES | - |
| 10 | new_values | jsonb | YES | - |
| 11 | context_type | varchar(50) | YES | - |
| 12 | ip_address | inet | YES | - |
| 13 | user_agent | text | YES | - |
| 14 | created_at | timestamptz | NO | now() |

---

### ai_conversations (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | title | varchar(200) | YES | - |
| 5 | context_entity_type | varchar(50) | YES | - |
| 6 | context_entity_id | uuid | YES | - |
| 7 | is_active | boolean | NO | true |
| 8 | created_at | timestamptz | NO | now() |
| 9 | updated_at | timestamptz | NO | now() |
| 10 | is_deleted | boolean | NO | false |
| 11 | deleted_at | timestamptz | YES | - |

---

### ai_insights (34 columns) ⭐ COMPLETE + EXTENDED

**Note:** 6 columns added via ALTER TABLE (see ALTER_ai_insights_add_columns.sql)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | target_team_id | uuid | YES | - |
| 4 | target_team_member_id | uuid | YES | - |
| 5 | insight_type | varchar(100) | NO | - |
| 6 | category | varchar(100) | NO | - |
| 7 | title | varchar(200) | NO | - |
| 8 | summary | text | NO | - |
| 9 | details | jsonb | YES | - |
| 10 | priority | varchar(20) | NO | 'medium' |
| 11 | recommended_actions | jsonb | YES | - |
| 12 | source_entities | jsonb | YES | - |
| 13 | valid_from | timestamptz | NO | now() |
| 14 | valid_until | timestamptz | YES | - |
| 15 | is_dismissed | boolean | NO | false |
| 16 | dismissed_at | timestamptz | YES | - |
| 17 | dismissed_by | uuid | YES | - |
| 18 | dismiss_reason | text | YES | - |
| 19 | is_actioned | boolean | NO | false |
| 20 | actioned_at | timestamptz | YES | - |
| 21 | action_notes | text | YES | - |
| 22 | created_at | timestamptz | NO | now() |
| 23 | unique_key | varchar(255) | YES | - |
| 24 | is_read | boolean | NO | false |
| 25 | updated_at | timestamptz | NO | now() |
| 26 | is_deleted | boolean | NO | false |
| 27 | deleted_at | timestamptz | YES | - |
| 28 | deleted_by | uuid | YES | - |
| 29 | severity | varchar(20) | NO | 'info' |
| 30 | description | text | YES | - |
| 31 | action_suggestion | text | YES | - |
| 32 | entity_type | varchar(50) | YES | - |
| 33 | entity_id | uuid | YES | - |
| 34 | generated_at | timestamptz | NO | now() |

---

### ai_messages (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | conversation_id | uuid | NO | - |
| 3 | role | varchar(20) | NO | - |
| 4 | content | text | NO | - |
| 5 | prompt_tokens | int4 | YES | - |
| 6 | completion_tokens | int4 | YES | - |
| 7 | total_tokens | int4 | YES | - |
| 8 | model_name | varchar(100) | YES | - |
| 9 | referenced_entities | jsonb | YES | - |
| 10 | message_order | int4 | NO | - |
| 11 | created_at | timestamptz | NO | now() |

---

### announcement_reads (4 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | announcement_id | uuid | NO | - |
| 3 | user_id | uuid | NO | - |
| 4 | read_at | timestamptz | NO | now() |

---

### announcements (17 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | created_by_user_id | uuid | NO | - |
| 4 | title | varchar(200) | NO | - |
| 5 | content | text | NO | - |
| 6 | target_type | varchar(50) | NO | 'organization' |
| 7 | target_team_id | uuid | YES | - |
| 8 | target_role_ids | jsonb | YES | - |
| 9 | publish_at | timestamptz | NO | now() |
| 10 | expires_at | timestamptz | YES | - |
| 11 | is_pinned | boolean | NO | false |
| 12 | priority | varchar(20) | NO | 'normal' |
| 13 | is_published | boolean | NO | true |
| 14 | created_at | timestamptz | NO | now() |
| 15 | updated_at | timestamptz | NO | now() |
| 16 | is_deleted | boolean | NO | false |
| 17 | deleted_at | timestamptz | YES | - |

---

### calendar_links (20 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | user_id | uuid | NO | - |
| 3 | provider | calendar_provider (enum) | NO | - |
| 4 | account_email | varchar(255) | YES | - |
| 5 | account_name | varchar(200) | YES | - |
| 6 | access_token | text | YES | - |
| 7 | refresh_token | text | YES | - |
| 8 | token_expires_at | timestamptz | YES | - |
| 9 | is_active | boolean | NO | true |
| 10 | sync_enabled | boolean | NO | true |
| 11 | sync_meetings_to_calendar | boolean | NO | true |
| 12 | sync_tasks_to_calendar | boolean | NO | false |
| 13 | create_meeting_from_calendar | boolean | NO | false |
| 14 | default_calendar_id | varchar(255) | YES | - |
| 15 | default_calendar_name | varchar(200) | YES | - |
| 16 | last_sync_at | timestamptz | YES | - |
| 17 | last_sync_status | calendar_sync_status (enum) | YES | - |
| 18 | last_sync_error | text | YES | - |
| 19 | created_at | timestamptz | NO | now() |
| 20 | updated_at | timestamptz | NO | now() |

---

### development_goal_comments (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | goal_id | uuid | NO | - |
| 3 | author_team_member_id | uuid | NO | - |
| 4 | content | text | NO | - |
| 5 | comment_type | varchar(50) | NO | 'comment' |
| 6 | created_at | timestamptz | NO | now() |
| 7 | updated_at | timestamptz | NO | now() |

---

### development_goal_milestones (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | goal_id | uuid | NO | - |
| 3 | title | varchar(300) | NO | - |
| 4 | description | text | YES | - |
| 5 | target_date | date | YES | - |
| 6 | completed_at | timestamptz | YES | - |
| 7 | status | milestone_status (enum) | NO | 'not_started' |
| 8 | sort_order | int4 | NO | 0 |
| 9 | notes | text | YES | - |
| 10 | created_at | timestamptz | NO | now() |
| 11 | updated_at | timestamptz | NO | now() |

---

### development_goals (22 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | title | varchar(300) | NO | - |
| 5 | description | text | YES | - |
| 6 | category | dev_goal_category (enum) | NO | 'skill_development' |
| 7 | target_date | date | YES | - |
| 8 | started_at | timestamptz | YES | - |
| 9 | completed_at | timestamptz | YES | - |
| 10 | status | dev_goal_status (enum) | NO | 'draft' |
| 11 | progress_percent | int4 | YES | 0 |
| 12 | why_important | text | YES | - |
| 13 | success_criteria | text | YES | - |
| 14 | support_needed | text | YES | - |
| 15 | resources | text | YES | - |
| 16 | is_private | boolean | NO | false |
| 17 | shared_with_manager | boolean | NO | true |
| 18 | review_id | uuid | YES | - |
| 19 | created_at | timestamptz | NO | now() |
| 20 | updated_at | timestamptz | NO | now() |
| 21 | is_deleted | boolean | NO | false |
| 22 | deleted_at | timestamptz | YES | - |

---

### feedback (25 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | from_team_member_id | uuid | NO | - |
| 4 | to_team_member_id | uuid | NO | - |
| 5 | feedback_type | feedback_type (enum) | NO | 'general' |
| 6 | sentiment | feedback_sentiment (enum) | NO | 'neutral' |
| 7 | content | text | NO | - |
| 8 | context_type | varchar(50) | YES | - |
| 9 | context_id | uuid | YES | - |
| 10 | is_private | boolean | NO | false |
| 11 | is_requested | boolean | NO | false |
| 12 | request_id | uuid | YES | - |
| 13 | ai_summary | text | YES | - |
| 14 | ai_tags | jsonb | YES | - |
| 15 | is_acknowledged | boolean | NO | false |
| 16 | acknowledged_at | timestamptz | YES | - |
| 17 | created_at | timestamptz | NO | now() |
| 18 | updated_at | timestamptz | NO | now() |
| 19 | is_deleted | boolean | NO | false |
| 20 | deleted_at | timestamptz | YES | - |
| 21 | deleted_by | uuid | YES | - |
| 22 | sync_id | uuid | YES | gen_random_uuid() |
| 23 | sync_version | int4 | YES | 1 |
| 24 | sync_modified_at | timestamptz | YES | now() |
| 25 | sync_status | sync_status (enum) | YES | 'synced' |

---

### feedback_requests (16 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | requester_team_member_id | uuid | NO | - |
| 4 | requested_from_team_member_id | uuid | NO | - |
| 5 | about_team_member_id | uuid | NO | - |
| 6 | message | text | YES | - |
| 7 | context_type | varchar(50) | YES | - |
| 8 | context_id | uuid | YES | - |
| 9 | due_date | date | YES | - |
| 10 | status | varchar(50) | NO | 'pending' |
| 11 | completed_at | timestamptz | YES | - |
| 12 | declined_at | timestamptz | YES | - |
| 13 | decline_reason | text | YES | - |
| 14 | response_feedback_id | uuid | YES | - |
| 15 | created_at | timestamptz | NO | now() |
| 16 | updated_at | timestamptz | NO | now() |

---

### goal_milestones (10 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | goal_id | uuid | NO | - |
| 3 | title | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | target_date | date | NO | - |
| 6 | completed_date | date | YES | - |
| 7 | is_completed | boolean | NO | false |
| 8 | sort_order | int4 | NO | 0 |
| 9 | created_at | timestamptz | NO | now() |
| 10 | updated_at | timestamptz | NO | now() |

---

### goals (26 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | owner_team_member_id | uuid | YES | - |
| 4 | created_by_user_id | uuid | NO | - |
| 5 | title | varchar(300) | NO | - |
| 6 | description | text | YES | - |
| 7 | time_period | goal_time_period (enum) | NO | 'q1' |
| 8 | year | int4 | NO | EXTRACT(year FROM CURRENT_DATE) |
| 9 | start_date | date | NO | - |
| 10 | end_date | date | NO | - |
| 11 | status | goal_status (enum) | NO | 'not_started' |
| 12 | status_override | goal_status (enum) | YES | - |
| 13 | progress_percent | numeric | NO | 0 |
| 14 | progress_override | numeric | YES | - |
| 15 | is_team_visible | boolean | NO | true |
| 16 | is_org_visible | boolean | NO | false |
| 17 | project_id | uuid | YES | - |
| 18 | created_at | timestamptz | NO | now() |
| 19 | updated_at | timestamptz | NO | now() |
| 20 | is_deleted | boolean | NO | false |
| 21 | deleted_at | timestamptz | YES | - |
| 22 | deleted_by | uuid | YES | - |
| 23 | sync_id | uuid | YES | gen_random_uuid() |
| 24 | sync_version | int4 | YES | 1 |
| 25 | sync_modified_at | timestamptz | YES | now() |
| 26 | sync_status | sync_status (enum) | YES | 'synced' |

---

### journal_entries (17 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | team_member_id | uuid | NO | - |
| 3 | entry_date | date | NO | - |
| 4 | content | text | NO | - |
| 5 | mood_rating | int4 | YES | - |
| 6 | energy_level | int4 | YES | - |
| 7 | wins | jsonb | YES | - |
| 8 | challenges | jsonb | YES | - |
| 9 | grateful_for | jsonb | YES | - |
| 10 | progress_on_goals | text | YES | - |
| 11 | ai_insights | text | YES | - |
| 12 | created_at | timestamptz | NO | now() |
| 13 | updated_at | timestamptz | NO | now() |
| 14 | sync_id | uuid | YES | gen_random_uuid() |
| 15 | sync_version | int4 | YES | 1 |
| 16 | sync_modified_at | timestamptz | YES | now() |
| 17 | sync_status | sync_status (enum) | YES | 'synced' |

---

### manager_history (9 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | manager_user_id | uuid | NO | - |
| 5 | start_date | date | NO | CURRENT_DATE |
| 6 | end_date | date | YES | - |
| 7 | change_reason | varchar(500) | YES | - |
| 8 | created_at | timestamptz | NO | now() |
| 9 | created_by | uuid | YES | - |

---

### meeting_agenda_items (12 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | meeting_id | uuid | NO | - |
| 3 | added_by_team_member_id | uuid | YES | - |
| 4 | title | varchar(300) | NO | - |
| 5 | notes | text | YES | - |
| 6 | sort_order | int4 | NO | 0 |
| 7 | is_discussed | boolean | NO | false |
| 8 | discussed_at | timestamptz | YES | - |
| 9 | time_estimate_minutes | int4 | YES | - |
| 10 | actual_duration_minutes | int4 | YES | - |
| 11 | created_at | timestamptz | NO | now() |
| 12 | updated_at | timestamptz | NO | now() |

---

### meeting_attendees (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | meeting_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | response | varchar(50) | YES | 'pending' |
| 5 | response_at | timestamptz | YES | - |
| 6 | attended | boolean | YES | - |
| 7 | created_at | timestamptz | NO | now() |

---

### meeting_notes (12 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | meeting_id | uuid | NO | - |
| 3 | author_team_member_id | uuid | YES | - |
| 4 | content | text | NO | - |
| 5 | is_private | boolean | NO | false |
| 6 | ai_summary | text | YES | - |
| 7 | created_at | timestamptz | NO | now() |
| 8 | updated_at | timestamptz | NO | now() |
| 9 | sync_id | uuid | YES | gen_random_uuid() |
| 10 | sync_version | int4 | YES | 1 |
| 11 | sync_modified_at | timestamptz | YES | now() |
| 12 | sync_status | sync_status (enum) | YES | 'synced' |

---

### meetings (32 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | created_by_user_id | uuid | NO | - |
| 4 | meeting_type | meeting_type (enum) | NO | 'one_on_one' |
| 5 | manager_team_member_id | uuid | YES | - |
| 6 | report_team_member_id | uuid | YES | - |
| 7 | team_id | uuid | YES | - |
| 8 | title | varchar(300) | NO | - |
| 9 | description | text | YES | - |
| 10 | scheduled_at | timestamptz | YES | - |
| 11 | duration_minutes | int4 | NO | 30 |
| 12 | recurrence_rule | varchar(200) | YES | - |
| 13 | location | varchar(500) | YES | - |
| 14 | status | meeting_status (enum) | NO | 'scheduled' |
| 15 | started_at | timestamptz | YES | - |
| 16 | ended_at | timestamptz | YES | - |
| 17 | created_at | timestamptz | NO | now() |
| 18 | updated_at | timestamptz | NO | now() |
| 19 | is_deleted | boolean | NO | false |
| 20 | deleted_at | timestamptz | YES | - |
| 21 | deleted_by | uuid | YES | - |
| 22 | sync_id | uuid | YES | gen_random_uuid() |
| 23 | sync_version | int4 | YES | 1 |
| 24 | sync_modified_at | timestamptz | YES | now() |
| 25 | sync_status | sync_status (enum) | YES | 'synced' |
| 26 | calendar_event_id | varchar(255) | YES | - |
| 27 | calendar_provider | calendar_provider (enum) | YES | - |
| 28 | calendar_link_id | uuid | YES | - |
| 29 | video_conference_url | text | YES | - |
| 30 | video_conference_provider | varchar(50) | YES | - |
| 31 | calendar_sync_status | calendar_sync_status (enum) | YES | - |
| 32 | last_synced_at | timestamptz | YES | - |

---

### metric_data_sources (8 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | metric_id | uuid | NO | - |
| 3 | source_type | varchar(50) | NO | - |
| 4 | source_id | uuid | YES | - |
| 5 | source_config | jsonb | YES | - |
| 6 | aggregation_type | varchar(50) | NO | 'latest' |
| 7 | created_at | timestamptz | NO | now() |
| 8 | updated_at | timestamptz | NO | now() |

---

### metric_history (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | metric_id | uuid | NO | - |
| 3 | value | numeric | NO | - |
| 4 | recorded_at | timestamptz | NO | now() |
| 5 | recorded_by_user_id | uuid | YES | - |
| 6 | source | varchar(50) | YES | 'manual' |
| 7 | notes | text | YES | - |

---

### metrics (29 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | owner_team_member_id | uuid | YES | - |
| 4 | created_by_user_id | uuid | NO | - |
| 5 | name | varchar(200) | NO | - |
| 6 | description | text | YES | - |
| 7 | category | varchar(100) | YES | - |
| 8 | current_value | numeric | NO | 0 |
| 9 | target_value | numeric | YES | - |
| 10 | baseline_value | numeric | YES | - |
| 11 | unit | varchar(50) | YES | - |
| 12 | target_direction | metric_target_direction (enum) | NO | 'higher_is_better' |
| 13 | frequency | metric_frequency (enum) | NO | 'monthly' |
| 14 | last_updated_at | timestamptz | YES | now() |
| 15 | is_composite | boolean | NO | false |
| 16 | parent_metric_id | uuid | YES | - |
| 17 | is_team_visible | boolean | NO | true |
| 18 | is_org_visible | boolean | NO | false |
| 19 | warning_threshold | numeric | YES | - |
| 20 | critical_threshold | numeric | YES | - |
| 21 | created_at | timestamptz | NO | now() |
| 22 | updated_at | timestamptz | NO | now() |
| 23 | is_deleted | boolean | NO | false |
| 24 | deleted_at | timestamptz | YES | - |
| 25 | deleted_by | uuid | YES | - |
| 26 | sync_id | uuid | YES | gen_random_uuid() |
| 27 | sync_version | int4 | YES | 1 |
| 28 | sync_modified_at | timestamptz | YES | now() |
| 29 | sync_status | sync_status (enum) | YES | 'synced' |

---

### milestones (10 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | project_id | uuid | NO | - |
| 3 | title | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | target_date | date | NO | - |
| 6 | completed_date | date | YES | - |
| 7 | is_completed | boolean | NO | false |
| 8 | sort_order | int4 | NO | 0 |
| 9 | created_at | timestamptz | NO | now() |
| 10 | updated_at | timestamptz | NO | now() |

---

### note_templates (13 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | created_by_user_id | uuid | NO | - |
| 4 | name | varchar(200) | NO | - |
| 5 | description | text | YES | - |
| 6 | content_template | text | NO | - |
| 7 | template_type | varchar(100) | NO | - |
| 8 | is_personal | boolean | NO | true |
| 9 | sort_order | int4 | NO | 0 |
| 10 | created_at | timestamptz | NO | now() |
| 11 | updated_at | timestamptz | NO | now() |
| 12 | is_deleted | boolean | NO | false |
| 13 | deleted_at | timestamptz | YES | - |

---

### notes (29 columns) *Updated via ALTER*

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | author_team_member_id | uuid | NO | - |
| 4 | title | varchar(300) | YES | - |
| 5 | content | text | NO | - |
| 6 | content_format | varchar(50) | NO | 'plain' |
| 7 | linked_team_member_id | uuid | YES | - |
| 8 | linked_meeting_id | uuid | YES | - |
| 9 | linked_project_id | uuid | YES | - |
| 10 | linked_goal_id | uuid | YES | - |
| 11 | linked_task_id | uuid | YES | - |
| 12 | category | varchar(100) | YES | - |
| 13 | tags | jsonb | YES | - |
| 14 | is_private | boolean | NO | true |
| 15 | is_pinned | boolean | NO | false |
| 16 | pinned_at | timestamptz | YES | - |
| 17 | is_archived | boolean | NO | false | *ADDED*
| 18 | archived_at | timestamptz | YES | - | *ADDED*
| 19 | ai_summary | text | YES | - |
| 20 | ai_suggested_actions | jsonb | YES | - |
| 21 | created_at | timestamptz | NO | now() |
| 22 | updated_at | timestamptz | NO | now() |
| 23 | is_deleted | boolean | NO | false |
| 24 | deleted_at | timestamptz | YES | - |
| 25 | deleted_by | uuid | YES | - |
| 26 | sync_id | uuid | YES | gen_random_uuid() |
| 27 | sync_version | int4 | YES | 1 |
| 28 | sync_modified_at | timestamptz | YES | now() |
| 29 | sync_status | sync_status (enum) | YES | 'synced' |

---

### notification_preferences (9 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | user_id | uuid | NO | - |
| 3 | notification_type | varchar(100) | NO | - |
| 4 | in_app_enabled | boolean | NO | true |
| 5 | email_enabled | boolean | NO | true |
| 6 | push_enabled | boolean | NO | false |
| 7 | email_frequency | varchar(50) | YES | 'immediate' |
| 8 | created_at | timestamptz | NO | now() |
| 9 | updated_at | timestamptz | NO | now() |

---

### notifications (18 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | user_id | uuid | NO | - |
| 4 | notification_type | varchar(100) | NO | - |
| 5 | title | varchar(200) | NO | - |
| 6 | message | text | NO | - |
| 7 | entity_type | varchar(50) | YES | - |
| 8 | entity_id | uuid | YES | - |
| 9 | action_url | varchar(500) | YES | - |
| 10 | priority | varchar(20) | NO | 'normal' |
| 11 | is_read | boolean | NO | false |
| 12 | read_at | timestamptz | YES | - |
| 13 | is_dismissed | boolean | NO | false |
| 14 | dismissed_at | timestamptz | YES | - |
| 15 | email_sent | boolean | NO | false |
| 16 | email_sent_at | timestamptz | YES | - |
| 17 | expires_at | timestamptz | YES | - |
| 18 | created_at | timestamptz | NO | now() |

---

### organization_snapshots (23 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | snapshot_date | date | NO | - |
| 4 | period_type | snapshot_period (enum) | NO | 'weekly' |
| 5 | period_start | date | NO | - |
| 6 | period_end | date | NO | - |
| 7 | total_users | int4 | YES | 0 |
| 8 | active_users | int4 | YES | 0 |
| 9 | total_team_members | int4 | YES | 0 |
| 10 | users_logged_in | int4 | YES | 0 |
| 11 | login_rate | numeric | YES | - |
| 12 | goals_total | int4 | YES | 0 |
| 13 | goals_on_track_rate | numeric | YES | - |
| 14 | goals_completed_this_period | int4 | YES | 0 |
| 15 | one_on_ones_held | int4 | YES | 0 |
| 16 | one_on_one_completion_rate | numeric | YES | - |
| 17 | avg_engagement_score | numeric | YES | - |
| 18 | enps_score | int4 | YES | - |
| 19 | feedback_count | int4 | YES | 0 |
| 20 | recognition_count | int4 | YES | 0 |
| 21 | reviews_in_progress | int4 | YES | 0 |
| 22 | reviews_completed | int4 | YES | 0 |
| 23 | created_at | timestamptz | NO | now() |

---

### organizations (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | name | varchar(200) | NO | - |
| 3 | slug | varchar(100) | YES | - |
| 4 | subscription_tier | varchar(50) | NO | 'free' |
| 5 | max_users | int4 | YES | 5 |
| 6 | max_team_members | int4 | YES | 25 |
| 7 | settings | jsonb | YES | '{}' |
| 8 | is_active | boolean | NO | true |
| 9 | created_at | timestamptz | NO | now() |
| 10 | updated_at | timestamptz | NO | now() |
| 11 | created_by | varchar(100) | YES | - |

---

### performance_reviews (24 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | reviewer_team_member_id | uuid | NO | - |
| 5 | review_period_start | date | NO | - |
| 6 | review_period_end | date | NO | - |
| 7 | review_type | varchar(50) | NO | 'annual' |
| 8 | status | varchar(50) | NO | 'draft' |
| 9 | self_review_content | jsonb | YES | - |
| 10 | self_review_submitted_at | timestamptz | YES | - |
| 11 | manager_review_content | jsonb | YES | - |
| 12 | manager_review_submitted_at | timestamptz | YES | - |
| 13 | overall_rating | int4 | YES | - |
| 14 | rating_label | varchar(100) | YES | - |
| 15 | strengths | text | YES | - |
| 16 | areas_for_improvement | text | YES | - |
| 17 | goals_for_next_period | text | YES | - |
| 18 | employee_acknowledged | boolean | NO | false |
| 19 | employee_acknowledged_at | timestamptz | YES | - |
| 20 | employee_comments | text | YES | - |
| 21 | created_at | timestamptz | NO | now() |
| 22 | updated_at | timestamptz | NO | now() |
| 23 | is_deleted | boolean | NO | false |
| 24 | deleted_at | timestamptz | YES | - |

---

### progress_snapshots (10 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | entity_type | varchar(50) | NO | - |
| 4 | entity_id | uuid | NO | - |
| 5 | snapshot_date | date | NO | - |
| 6 | period_type | snapshot_period (enum) | NO | 'weekly' |
| 7 | metrics | jsonb | NO | '{}' |
| 8 | overall_score | numeric | YES | - |
| 9 | trend_direction | int4 | YES | - |
| 10 | created_at | timestamptz | NO | now() |

---

### project_members (5 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | project_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | role | varchar(100) | YES | - |
| 5 | joined_at | timestamptz | NO | now() |

---

### projects (21 columns) *Updated via ALTER*

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | owner_team_member_id | uuid | YES | - |
| 4 | created_by_user_id | uuid | NO | - |
| 5 | source_agenda_item_id | uuid | YES | - | *ADDED*
| 6 | source_meeting_id | uuid | YES | - | *ADDED*
| 7 | name | varchar(300) | NO | - |
| 8 | description | text | YES | - |
| 9 | color | varchar(7) | YES | - |
| 10 | start_date | date | YES | - |
| 11 | target_end_date | date | YES | - |
| 12 | actual_end_date | date | YES | - |
| 13 | status | task_status (enum) | NO | 'not_started' |
| 14 | progress_percent | numeric | NO | 0 |
| 15 | priority | task_priority (enum) | NO | 'medium' |
| 16 | is_team_visible | boolean | NO | true |
| 17 | created_at | timestamptz | NO | now() |
| 18 | updated_at | timestamptz | NO | now() |
| 19 | is_deleted | boolean | NO | false |
| 20 | deleted_at | timestamptz | YES | - |
| 21 | deleted_by | uuid | YES | - |

---

### recognition (16 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | from_team_member_id | uuid | NO | - |
| 4 | to_team_member_id | uuid | NO | - |
| 5 | title | varchar(200) | NO | - |
| 6 | message | text | NO | - |
| 7 | badge_type | varchar(100) | YES | - |
| 8 | project_id | uuid | YES | - |
| 9 | goal_id | uuid | YES | - |
| 10 | company_values | jsonb | YES | - |
| 11 | is_public | boolean | NO | true |
| 12 | reactions_count | int4 | NO | 0 |
| 13 | created_at | timestamptz | NO | now() |
| 14 | is_deleted | boolean | NO | false |
| 15 | deleted_at | timestamptz | YES | - |
| 16 | deleted_by | uuid | YES | - |

---

### recognition_reactions (5 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | recognition_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | reaction_type | varchar(50) | NO | 'like' |
| 5 | created_at | timestamptz | NO | now() |

---

### reminder_preferences (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | user_id | uuid | NO | - |
| 3 | entity_type | varchar(50) | NO | - |
| 4 | sub_type | varchar(50) | YES | - |
| 5 | enabled | boolean | NO | true |
| 6 | default_minutes_before | int4 | NO | 15 |
| 7 | send_push | boolean | NO | true |
| 8 | send_email | boolean | NO | false |
| 9 | send_in_app | boolean | NO | true |
| 10 | created_at | timestamptz | NO | now() |
| 11 | updated_at | timestamptz | NO | now() |

---

### reminders (22 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | user_id | uuid | NO | - |
| 4 | team_member_id | uuid | YES | - |
| 5 | reminder_type | reminder_type (enum) | NO | - |
| 6 | entity_type | varchar(50) | NO | - |
| 7 | entity_id | uuid | NO | - |
| 8 | title | varchar(300) | NO | - |
| 9 | message | text | YES | - |
| 10 | remind_at | timestamptz | NO | - |
| 11 | minutes_before | int4 | YES | - |
| 12 | status | reminder_status (enum) | NO | 'scheduled' |
| 13 | sent_at | timestamptz | YES | - |
| 14 | dismissed_at | timestamptz | YES | - |
| 15 | snoozed_until | timestamptz | YES | - |
| 16 | send_push | boolean | NO | true |
| 17 | send_email | boolean | NO | false |
| 18 | send_in_app | boolean | NO | true |
| 19 | is_recurring | boolean | NO | false |
| 20 | recurrence_rule | varchar(200) | YES | - |
| 21 | created_at | timestamptz | NO | now() |
| 22 | updated_at | timestamptz | NO | now() |

---

### review_cycles (17 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | template_id | uuid | NO | - |
| 4 | name | varchar(200) | NO | - |
| 5 | description | text | YES | - |
| 6 | start_date | date | NO | - |
| 7 | end_date | date | NO | - |
| 8 | self_review_due | date | YES | - |
| 9 | manager_review_due | date | YES | - |
| 10 | status | review_cycle_status (enum) | NO | 'draft' |
| 11 | include_all_employees | boolean | NO | true |
| 12 | team_ids | uuid[] | YES | - |
| 13 | created_at | timestamptz | NO | now() |
| 14 | updated_at | timestamptz | NO | now() |
| 15 | created_by | uuid | YES | - |
| 16 | launched_at | timestamptz | YES | - |
| 17 | completed_at | timestamptz | YES | - |

---

### review_responses (10 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | review_id | uuid | NO | - |
| 3 | question_id | uuid | NO | - |
| 4 | responder_type | varchar(20) | NO | - |
| 5 | responder_team_member_id | uuid | YES | - |
| 6 | rating_value | int4 | YES | - |
| 7 | text_value | text | YES | - |
| 8 | selected_option | varchar(200) | YES | - |
| 9 | created_at | timestamptz | NO | now() |
| 10 | updated_at | timestamptz | NO | now() |

---

### review_template_questions (13 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | section_id | uuid | NO | - |
| 3 | question_text | text | NO | - |
| 4 | help_text | text | YES | - |
| 5 | question_type | review_question_type (enum) | NO | 'rating' |
| 6 | options | jsonb | YES | - |
| 7 | is_required | boolean | NO | true |
| 8 | sort_order | int4 | NO | 0 |
| 9 | weight | numeric | YES | 1.0 |
| 10 | min_rating | int4 | YES | 1 |
| 11 | max_rating | int4 | YES | 5 |
| 12 | rating_labels | jsonb | YES | - |
| 13 | created_at | timestamptz | NO | now() |

---

### review_template_sections (8 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | template_id | uuid | NO | - |
| 3 | title | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | sort_order | int4 | NO | 0 |
| 6 | is_required | boolean | NO | true |
| 7 | weight | numeric | YES | 1.0 |
| 8 | created_at | timestamptz | NO | now() |

---

### review_templates (13 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | name | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | is_default | boolean | NO | false |
| 6 | is_active | boolean | NO | true |
| 7 | review_type | varchar(50) | NO | 'annual' |
| 8 | include_self_review | boolean | NO | true |
| 9 | include_peer_review | boolean | NO | false |
| 10 | include_upward_review | boolean | NO | false |
| 11 | created_at | timestamptz | NO | now() |
| 12 | updated_at | timestamptz | NO | now() |
| 13 | created_by | uuid | YES | - |

---

### reviews (19 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | cycle_id | uuid | NO | - |
| 4 | reviewee_team_member_id | uuid | NO | - |
| 5 | reviewer_team_member_id | uuid | YES | - |
| 6 | status | review_status (enum) | NO | 'not_started' |
| 7 | self_review_status | review_status (enum) | NO | 'not_started' |
| 8 | self_review_submitted_at | timestamptz | YES | - |
| 9 | manager_review_status | review_status (enum) | NO | 'not_started' |
| 10 | manager_review_submitted_at | timestamptz | YES | - |
| 11 | overall_rating | numeric | YES | - |
| 12 | overall_comments | text | YES | - |
| 13 | strengths | text | YES | - |
| 14 | areas_for_improvement | text | YES | - |
| 15 | goals_for_next_period | text | YES | - |
| 16 | acknowledged_at | timestamptz | YES | - |
| 17 | acknowledgment_comments | text | YES | - |
| 18 | created_at | timestamptz | NO | now() |
| 19 | updated_at | timestamptz | NO | now() |

---

### roles (37 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | name | varchar(50) | NO | - |
| 3 | display_name | varchar(100) | NO | - |
| 4 | description | text | YES | - |
| 5 | can_manage_org | boolean | NO | false |
| 6 | can_manage_billing | boolean | NO | false |
| 7 | can_manage_users | boolean | NO | false |
| 8 | can_invite_users | boolean | NO | false |
| 9 | can_assign_roles | boolean | NO | false |
| 10 | can_manage_teams | boolean | NO | false |
| 11 | can_create_teams | boolean | NO | false |
| 12 | can_create_goals | boolean | NO | false |
| 13 | can_edit_all_goals | boolean | NO | false |
| 14 | can_edit_own_goals | boolean | NO | false |
| 15 | can_view_team_goals | boolean | NO | false |
| 16 | can_view_org_goals | boolean | NO | false |
| 17 | can_create_metrics | boolean | NO | false |
| 18 | can_edit_metrics | boolean | NO | false |
| 19 | can_view_team_metrics | boolean | NO | false |
| 20 | can_view_org_metrics | boolean | NO | false |
| 21 | can_create_tasks | boolean | NO | false |
| 22 | can_assign_tasks | boolean | NO | false |
| 23 | can_view_team_tasks | boolean | NO | false |
| 24 | can_schedule_meetings | boolean | NO | false |
| 25 | can_run_meetings | boolean | NO | false |
| 26 | can_participate_meetings | boolean | NO | false |
| 27 | can_view_meeting_notes | boolean | NO | false |
| 28 | can_give_feedback | boolean | NO | false |
| 29 | can_receive_feedback | boolean | NO | false |
| 30 | can_view_team_feedback | boolean | NO | false |
| 31 | can_view_team_analytics | boolean | NO | false |
| 32 | can_view_org_analytics | boolean | NO | false |
| 33 | can_export_data | boolean | NO | false |
| 34 | is_system_role | boolean | NO | false |
| 35 | sort_order | int4 | NO | 0 |
| 36 | created_at | timestamptz | NO | now() |
| 37 | updated_at | timestamptz | NO | now() |

---

### survey_answers (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | response_id | uuid | NO | - |
| 3 | question_id | uuid | NO | - |
| 4 | rating_value | int4 | YES | - |
| 5 | text_value | text | YES | - |
| 6 | selected_options | jsonb | YES | - |
| 7 | created_at | timestamptz | NO | now() |

---

### survey_instances (11 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | survey_id | uuid | NO | - |
| 3 | period_start | date | NO | - |
| 4 | period_end | date | NO | - |
| 5 | status | survey_status (enum) | NO | 'active' |
| 6 | sent_at | timestamptz | YES | - |
| 7 | closed_at | timestamptz | YES | - |
| 8 | total_recipients | int4 | YES | 0 |
| 9 | total_responses | int4 | YES | 0 |
| 10 | response_rate | numeric | YES | - |
| 11 | created_at | timestamptz | NO | now() |

---

### survey_questions (14 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | survey_id | uuid | NO | - |
| 3 | question_text | text | NO | - |
| 4 | help_text | text | YES | - |
| 5 | question_type | survey_question_type (enum) | NO | 'rating' |
| 6 | min_value | int4 | YES | 1 |
| 7 | max_value | int4 | YES | 5 |
| 8 | min_label | varchar(100) | YES | - |
| 9 | max_label | varchar(100) | YES | - |
| 10 | options | jsonb | YES | - |
| 11 | is_required | boolean | NO | true |
| 12 | sort_order | int4 | NO | 0 |
| 13 | category | varchar(100) | YES | - |
| 14 | created_at | timestamptz | NO | now() |

---

### survey_responses (9 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | survey_id | uuid | NO | - |
| 3 | instance_id | uuid | YES | - |
| 4 | team_member_id | uuid | YES | - |
| 5 | anonymous_token | uuid | YES | gen_random_uuid() |
| 6 | started_at | timestamptz | NO | now() |
| 7 | completed_at | timestamptz | YES | - |
| 8 | is_complete | boolean | NO | false |
| 9 | created_at | timestamptz | NO | now() |

---

### surveys (24 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | title | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | survey_type | varchar(50) | NO | 'pulse' |
| 6 | status | survey_status (enum) | NO | 'draft' |
| 7 | frequency | survey_frequency (enum) | NO | 'once' |
| 8 | start_date | date | YES | - |
| 9 | end_date | date | YES | - |
| 10 | next_send_date | date | YES | - |
| 11 | target_all_employees | boolean | NO | true |
| 12 | target_team_ids | uuid[] | YES | - |
| 13 | target_team_member_ids | uuid[] | YES | - |
| 14 | is_anonymous | boolean | NO | true |
| 15 | allow_comments | boolean | NO | true |
| 16 | reminder_enabled | boolean | NO | true |
| 17 | reminder_days_before_close | int4 | YES | 2 |
| 18 | welcome_message | text | YES | - |
| 19 | thank_you_message | text | YES | - |
| 20 | created_at | timestamptz | NO | now() |
| 21 | updated_at | timestamptz | NO | now() |
| 22 | created_by | uuid | YES | - |
| 23 | is_deleted | boolean | NO | false |
| 24 | deleted_at | timestamptz | YES | - |

---

### talking_points (12 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | manager_team_member_id | uuid | NO | - |
| 3 | report_team_member_id | uuid | NO | - |
| 4 | added_by_team_member_id | uuid | YES | - |
| 5 | title | varchar(300) | NO | - |
| 6 | notes | text | YES | - |
| 7 | category | varchar(100) | YES | - |
| 8 | is_recurring | boolean | NO | false |
| 9 | is_active | boolean | NO | true |
| 10 | last_discussed_at | timestamptz | YES | - |
| 11 | created_at | timestamptz | NO | now() |
| 12 | updated_at | timestamptz | NO | now() |

---

### target_measurables (6 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | target_id | uuid | NO | - |
| 3 | measurable_type | varchar(50) | NO | - |
| 4 | measurable_id | uuid | NO | - |
| 5 | aggregation_type | varchar(50) | NO | 'latest' |
| 6 | created_at | timestamptz | NO | now() |

---

### targets (15 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | goal_id | uuid | NO | - |
| 3 | title | varchar(300) | NO | - |
| 4 | description | text | YES | - |
| 5 | target_value | numeric | NO | - |
| 6 | current_value | numeric | NO | 0 |
| 7 | starting_value | numeric | NO | 0 |
| 8 | unit | varchar(50) | YES | - |
| 9 | weight | numeric | NO | 1.0 |
| 10 | status | goal_status (enum) | NO | 'not_started' |
| 11 | sort_order | int4 | NO | 0 |
| 12 | created_at | timestamptz | NO | now() |
| 13 | updated_at | timestamptz | NO | now() |
| 14 | is_deleted | boolean | NO | false |
| 15 | deleted_at | timestamptz | YES | - |

---

### task_collection_items (4 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | collection_id | uuid | NO | - |
| 3 | task_id | uuid | NO | - |
| 4 | created_at | timestamptz | NO | now() |

---

### task_collections (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | name | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | query_config | jsonb | YES | - |
| 6 | created_at | timestamptz | NO | now() |
| 7 | updated_at | timestamptz | NO | now() |

---

### tasks (27 columns) *Updated via ALTER*

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | owner_team_member_id | uuid | YES | - |
| 4 | created_by_user_id | uuid | NO | - |
| 5 | parent_task_id | uuid | YES | - |
| 6 | project_id | uuid | YES | - |
| 7 | goal_id | uuid | YES | - |
| 8 | meeting_id | uuid | YES | - |
| 9 | source_agenda_item_id | uuid | YES | - | *ADDED*
| 10 | source_meeting_id | uuid | YES | - | *ADDED*
| 11 | title | varchar(300) | NO | - |
| 12 | description | text | YES | - |
| 13 | notes | text | YES | - | *ADDED*
| 14 | status | task_status (enum) | NO | 'not_started' |
| 15 | priority | task_priority (enum) | NO | 'medium' |
| 16 | due_date | timestamptz | YES | - |
| 17 | completed_at | timestamptz | YES | - |
| 18 | sort_order | int4 | NO | 0 |
| 19 | created_at | timestamptz | NO | now() |
| 20 | updated_at | timestamptz | NO | now() |
| 21 | is_deleted | boolean | NO | false |
| 22 | deleted_at | timestamptz | YES | - |
| 23 | deleted_by | uuid | YES | - |
| 24 | sync_id | uuid | YES | gen_random_uuid() |
| 25 | sync_version | int4 | YES | 1 |
| 26 | sync_modified_at | timestamptz | YES | now() |
| 27 | sync_status | sync_status (enum) | YES | 'synced' |

---

### team_member_snapshots (25 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | snapshot_date | date | NO | - |
| 5 | period_type | snapshot_period (enum) | NO | 'weekly' |
| 6 | period_start | date | NO | - |
| 7 | period_end | date | NO | - |
| 8 | goals_total | int4 | YES | 0 |
| 9 | goals_on_track | int4 | YES | 0 |
| 10 | goals_at_risk | int4 | YES | 0 |
| 11 | goals_completed | int4 | YES | 0 |
| 12 | goal_progress_avg | numeric | YES | - |
| 13 | tasks_total | int4 | YES | 0 |
| 14 | tasks_completed | int4 | YES | 0 |
| 15 | tasks_overdue | int4 | YES | 0 |
| 16 | task_completion_rate | numeric | YES | - |
| 17 | one_on_ones_held | int4 | YES | 0 |
| 18 | one_on_ones_scheduled | int4 | YES | 0 |
| 19 | meetings_attended | int4 | YES | 0 |
| 20 | feedback_given | int4 | YES | 0 |
| 21 | feedback_received | int4 | YES | 0 |
| 22 | recognition_given | int4 | YES | 0 |
| 23 | recognition_received | int4 | YES | 0 |
| 24 | engagement_score | numeric | YES | - |
| 25 | created_at | timestamptz | NO | now() |

---

### team_members (34 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | manager_user_id | uuid | YES | - |
| 4 | linked_user_id | uuid | YES | - |
| 5 | first_name | varchar(100) | NO | - |
| 6 | last_name | varchar(100) | NO | - |
| 7 | nickname | varchar(50) | YES | - |
| 8 | email | varchar(255) | YES | - |
| 9 | phone | varchar(50) | YES | - |
| 10 | job_title | varchar(200) | YES | - |
| 11 | department | varchar(200) | YES | - |
| 12 | hire_date | date | YES | - |
| 13 | birthday | date | YES | - |
| 14 | location | varchar(200) | YES | - |
| 15 | bio | text | YES | - |
| 16 | avatar_url | text | YES | - |
| 17 | linkedin_url | varchar(500) | YES | - |
| 18 | employment_status | employment_status (enum) | NO | 'active' |
| 19 | termination_date | date | YES | - |
| 20 | is_active | boolean | NO | true |
| 21 | active_goal_count | int4 | NO | 0 |
| 22 | open_task_count | int4 | NO | 0 |
| 23 | last_meeting_date | timestamptz | YES | - |
| 24 | next_meeting_date | timestamptz | YES | - |
| 25 | created_at | timestamptz | NO | now() |
| 26 | updated_at | timestamptz | NO | now() |
| 27 | created_by | uuid | YES | - |
| 28 | is_deleted | boolean | NO | false |
| 29 | deleted_at | timestamptz | YES | - |
| 30 | deleted_by | uuid | YES | - |
| 31 | sync_id | uuid | YES | gen_random_uuid() |
| 32 | sync_version | int4 | YES | 1 |
| 33 | sync_modified_at | timestamptz | YES | now() |
| 34 | sync_status | sync_status (enum) | YES | 'synced' |

---

### team_memberships (8 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | team_id | uuid | NO | - |
| 3 | team_member_id | uuid | NO | - |
| 4 | is_lead | boolean | NO | false |
| 5 | joined_at | timestamptz | NO | now() |
| 6 | left_at | timestamptz | YES | - |
| 7 | created_at | timestamptz | NO | now() |
| 8 | created_by | uuid | YES | - |

---

### team_snapshots (23 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | team_id | uuid | NO | - |
| 4 | snapshot_date | date | NO | - |
| 5 | period_type | snapshot_period (enum) | NO | 'weekly' |
| 6 | period_start | date | NO | - |
| 7 | period_end | date | NO | - |
| 8 | member_count | int4 | YES | 0 |
| 9 | active_member_count | int4 | YES | 0 |
| 10 | goals_total | int4 | YES | 0 |
| 11 | goals_on_track | int4 | YES | 0 |
| 12 | goals_completed | int4 | YES | 0 |
| 13 | goal_completion_rate | numeric | YES | - |
| 14 | tasks_total | int4 | YES | 0 |
| 15 | tasks_completed | int4 | YES | 0 |
| 16 | task_completion_rate | numeric | YES | - |
| 17 | one_on_ones_completion_rate | numeric | YES | - |
| 18 | team_meetings_held | int4 | YES | 0 |
| 19 | avg_engagement_score | numeric | YES | - |
| 20 | survey_response_rate | numeric | YES | - |
| 21 | feedback_exchanges | int4 | YES | 0 |
| 22 | recognition_count | int4 | YES | 0 |
| 23 | created_at | timestamptz | NO | now() |

---

### teams (13 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | name | varchar(200) | NO | - |
| 4 | description | text | YES | - |
| 5 | color | varchar(7) | YES | - |
| 6 | lead_user_id | uuid | YES | - |
| 7 | is_active | boolean | NO | true |
| 8 | created_at | timestamptz | NO | now() |
| 9 | updated_at | timestamptz | NO | now() |
| 10 | created_by | uuid | YES | - |
| 11 | is_deleted | boolean | NO | false |
| 12 | deleted_at | timestamptz | YES | - |
| 13 | deleted_by | uuid | YES | - |

---

### user_roles (7 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | user_id | uuid | NO | - |
| 3 | organization_id | uuid | NO | - |
| 4 | role_id | uuid | NO | - |
| 5 | team_id | uuid | YES | - |
| 6 | created_at | timestamptz | NO | now() |
| 7 | assigned_by | uuid | YES | - |

---

### user_sessions (16 columns)

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | user_id | uuid | NO | - |
| 3 | device_id | text | NO | - |
| 4 | device_name | text | YES | - |
| 5 | device_type | varchar(50) | NO | - |
| 6 | os_name | varchar(100) | YES | - |
| 7 | app_version | varchar(50) | YES | - |
| 8 | refresh_token_hash | text | YES | - |
| 9 | last_active_at | timestamptz | NO | now() |
| 10 | last_ip_address | inet | YES | - |
| 11 | is_active | boolean | NO | true |
| 12 | revoked_at | timestamptz | YES | - |
| 13 | revoked_reason | text | YES | - |
| 14 | expires_at | timestamptz | YES | - |
| 15 | created_at | timestamptz | NO | now() |
| 16 | updated_at | timestamptz | NO | now() |

---

### users (29 columns) *Updated via ALTER*

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | supabase_auth_id | uuid | YES | - |
| 3 | organization_id | uuid | YES | - |
| 4 | firm_id | uuid | YES | - | *ADDED*
| 5 | email | varchar(255) | NO | - |
| 6 | username | varchar(200) | YES | - | *ADDED*
| 7 | display_name | varchar(200) | NO | - |
| 8 | first_name | varchar(100) | YES | - |
| 9 | last_name | varchar(100) | YES | - |
| 10 | avatar_url | text | YES | - |
| 11 | phone | varchar(50) | YES | - |
| 12 | timezone | varchar(100) | YES | 'UTC' |
| 13 | linked_team_member_id | uuid | YES | - |
| 14 | preferences | jsonb | YES | '{}' |
| 15 | notification_settings | jsonb | YES | (default JSON) |
| 16 | is_active | boolean | NO | true |
| 17 | is_admin | boolean | NO | false | *ADDED*
| 18 | role | varchar(50) | NO | 'manager' | *ADDED*
| 19 | password_hash | text | YES | - | *ADDED*
| 20 | is_email_verified | boolean | NO | false |
| 21 | last_login_at | timestamptz | YES | - |
| 22 | created_at | timestamptz | NO | now() |
| 23 | updated_at | timestamptz | NO | now() |
| 24 | created_by | varchar(100) | YES | - |
| 25 | is_deleted | boolean | NO | false |
| 26 | deleted_at | timestamptz | YES | - |
| 27 | deleted_by | varchar(100) | YES | - |
| 28 | job_title | varchar(200) | YES | - |
| 29 | company | varchar(200) | YES | - |

---

### vector_embeddings (18 columns) *Updated via ALTER*

| # | Column | Type | Nullable | Default |
|---|--------|------|----------|---------|
| 1 | id | uuid | NO | gen_random_uuid() |
| 2 | organization_id | uuid | NO | - |
| 3 | entity_type | varchar(50) | NO | - |
| 4 | entity_id | uuid | NO | - |
| 5 | chunk_index | int4 | NO | 0 | *ADDED*
| 6 | content_hash | varchar(64) | NO | - |
| 7 | content_preview | varchar(500) | YES | - |
| 8 | content | text | YES | - | *ADDED*
| 9 | embedding | vector | YES | - |
| 10 | embedding_dimensions | int4 | NO | 1536 | *ADDED*
| 11 | metadata | jsonb | YES | - |
| 12 | model_name | varchar(100) | NO | 'text-embedding-ada-002' |
| 13 | model_version | varchar(50) | YES | - |
| 14 | created_at | timestamptz | NO | now() |
| 15 | updated_at | timestamptz | NO | now() |
| 16 | is_deleted | boolean | NO | false | *ADDED*
| 17 | deleted_at | timestamptz | YES | - | *ADDED*
| 18 | deleted_by | uuid | YES | - | *ADDED*

---

## Schema Complete ✅

**All 65 tables fully documented!**

Total columns: ~950+

---

## Standard Patterns

### Soft Delete (most tables)
```
is_deleted boolean NOT NULL DEFAULT false
deleted_at timestamptz
deleted_by uuid (optional)
```

### Audit Timestamps (all tables)
```
created_at timestamptz NOT NULL DEFAULT now()
updated_at timestamptz NOT NULL DEFAULT now()
```

### Organization Scoping (most tables)
```
organization_id uuid NOT NULL (FK to organizations)
```

### All IDs
```
id uuid NOT NULL DEFAULT gen_random_uuid()
```

---

## C# Model Mapping Notes

When creating C# models:

1. **All `id` columns** → `public Guid Id { get; set; }`
2. **All `*_id` FK columns** → `public Guid ForeignKeyId { get; set; }`
3. **All `timestamptz` columns** → `public DateTime ColumnName { get; set; }`
4. **All `jsonb` columns** → `public Dictionary<string, object>? Details { get; set; }` or custom type
5. **All `varchar(n)` columns** → `public string ColumnName { get; set; } = string.Empty;`
6. **All `text` columns** → `public string? ColumnName { get; set; }`
7. **All `boolean` columns** → `public bool ColumnName { get; set; }`
8. **All `int4` columns** → `public int ColumnName { get; set; }`
9. **All `USER-DEFINED` (enums)** → Create matching C# enum

---

**Last Updated:** January 14, 2026
