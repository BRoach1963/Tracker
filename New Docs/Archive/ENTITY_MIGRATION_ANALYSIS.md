# C# Entity to Supabase Schema Migration Analysis

**Generated:** January 9, 2026  
**Purpose:** Comprehensive comparison of OLD C# entity models vs NEW Supabase schema

---

## Part 1: Complete Catalog of OLD C# Entities

### Core Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **Organization** | Organizations | Id (GUID), Name, Slug, IsActive, SubscriptionTier, MaxUsers, MaxTeamMembers | Has Users, TeamMembers | Multi-tenant root |
| **User** | Users | Id, OrganizationId, SupabaseUserId, Username, Email, DisplayName, IsActive, IsAdmin, Role, PasswordHash, FirmId | Belongs to Organization, Has ManagedTeamMembers, ManagerHistories | Login entity |
| **TeamMember** | TeamMembers | Id, OrganizationId, CurrentManagerUserId, FirstName, LastName, NickName, Email, CellPhone, JobTitle, BirthDay, HireDate, TerminationDate, IsActive, ManagerId (legacy), ProfileImage, LinkedInProfile, FacebookProfile, InstagramProfile, XProfile, Specialty (enum), SkillLevel (enum), Role (enum) | Belongs to Organization, CurrentManager (User), Has ManagerHistories | Runtime: LastOneOnOneDate, OpenTaskCount, ActiveGoalCount, UpcomingMeetingCount, NextOneOnOneDate, M365 Presence, Slack Presence |
| **ManagerHistory** | ManagerHistory | Id (GUID), OrganizationId, TeamMemberId, ManagerUserId, ManagerSupabaseId, StartDate, EndDate, Reason, Notes | Belongs to Organization, TeamMember, Manager (User) | Tracks manager changes |

### OKR/Goal Entities (OLD Terminology)

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **ObjectiveKeyResult** | ObjectiveKeyResults | ObjectiveId, OrganizationId, Title, Description, Owner (TeamMember), StartDate, EndDate, TimePeriod (enum), Year, ProjectId, StatusOverride (enum) | Has KeyResults, Owner | Computed: Status, CompletionPercentage, MeetingCount, LinkedKpiCount, LinkedProjectCount |
| **KeyResult** | KeyResults | Id, OrganizationId, OkrId, Title, Description, TargetValue, CurrentValue, StartingValue, Unit, Weight, SortOrder, TargetDirection (enum) | Belongs to Okr, Has Measurables | Computed: Progress, Status |
| **KeyResultMeasurable** | KeyResultMeasurables | Id, OrganizationId, KeyResultId, MeasurableType (enum), MeasurableId, AggregationType (enum), Weight, SortOrder | Belongs to KeyResult | Polymorphic link to KPI/Project/TaskCollection |
| **IndividualGoal** | IndividualGoals | Id, OrganizationId, TeamMemberId, Title, Description, Category (enum), Status (enum), TargetDate, ProgressPercent, Notes | Belongs to TeamMember, Has Milestones | Personal development goals |
| **GoalMilestone** | GoalMilestones | Id, OrganizationId, GoalId, Description, IsCompleted, CompletedDate, SortOrder | Belongs to IndividualGoal | |

### KPI/Metrics Entities (OLD Terminology)

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **KeyPerformanceIndicator** | KeyPerformanceIndicators | KpiId, OrganizationId, Name, Description, Value, TargetValue, Unit, Category, Owner (TeamMember), LastUpdated, TargetDirection (enum), Frequency (enum), IsComposite, ParentKpiId | Has ChildKpis, DataSources, ParentKpi, Owner | Implements IMeasurable, IKpiSource; Computed: Status, PercentComplete |
| **KpiDataSource** | KpiDataSources | Id, OrganizationId, KpiId, SourceType (enum), SourceId, AggregationType (enum), Weight, QueryCriteria (JSON), SortOrder | Belongs to Kpi | Polymorphic source link |

### Task Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **IndividualTask** | IndividualTasks | Id, OrganizationId, Description, IsCompleted, DueDate, Notes, Owner (TeamMember), ProjectId, ParentTaskId | Belongs to Project, ParentTask, Has Subtasks | Implements ITask; Computed: MeetingCount, IsOverdue, SubtaskProgress |
| **MeetingTask** | MeetingTasks | Id, OrganizationId, Description, DueDate, IsCompleted, Notes, Owner (TeamMember), OneOnOneId | Belongs to OneOnOne | Implements ITask; Action items from 1:1s |
| **TaskCollection** | TaskCollections | Id, OrganizationId, Name, Description | Has Items | Implements IMeasurable, IKpiSource |
| **TaskCollectionItem** | TaskCollectionItems | Id, OrganizationId, CollectionId, TaskId, SortOrder | Belongs to Collection, Task | |

### Meeting/1:1 Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **OneOnOne** | OneOnOnes | Id, OrganizationId, ManagerUserId, Description, TeamMember, Date, StartTime, EndTime, Duration, IsRecurring, Status (enum), Agenda, Notes, Feedback, GoogleCalendarEventId, CalendarEventId, TeamsMeetingUrl, TeamsMeetingId, GoogleMeetUrl, CalendarEventEtag, LastSyncedAt, SyncStatus, IsSyncedToGoogle | Has AgendaItems, Tasks, LinkedTasks, LinkedOkrs, LinkedKpis | Calendar sync properties |
| **AgendaItem** | AgendaItems | Id, OrganizationId, Description, Category (enum), Priority (enum), Resolution, IsCompleted, LinkedTaskId, OneOnOneId | Belongs to OneOnOne, Has LinkedItems | INotifyPropertyChanged |
| **Meeting** | Meetings | OrganizationId, Type (enum), Title, PrimaryAttendeeId, Date, StartTime, EndTime, Duration, Status (enum), IsRecurring, RecurringSeriesId, ProjectId, Notes, Location | Belongs to PrimaryAttendee (TeamMember), Project | Generic meeting entity |
| **CalendarLink** | CalendarLinks | Id, OrganizationId, OneOnOneId, ProviderId, ExternalEventId, ETag, LastSyncedAt, LastSyncDirection (enum), Status (enum), LastError | Belongs to OneOnOne | Multi-provider calendar sync |
| **OneOnOneLinkedTask** | OneOnOneLinkedTasks | Id, OneOnOneId, TaskId, DiscussionNotes | Links OneOnOne ↔ IndividualTask | Junction table |
| **OneOnOneLinkedOkr** | OneOnOneLinkedOkrs | Id, OneOnOneId, OkrId, DiscussionNotes | Links OneOnOne ↔ OKR | Junction table |
| **OneOnOneLinkedKpi** | OneOnOneLinkedKpis | Id, OneOnOneId, KpiId, DiscussionNotes | Links OneOnOne ↔ KPI | Junction table |

### Project Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **Project** | Projects | ID, OrganizationId, Name, Description, StartDate, EndDate, Status, Owner (TeamMember), Budget | Has Tasks, TeamMembers, Milestones, Dependencies, Risks | Implements IMeasurable, IKpiSource; Computed: Progress, TotalTasks |
| **Milestone** | Milestones | ID, OrganizationId, ProjectId, Name, Description, TargetDate, IsAchieved | Belongs to Project | |
| **ProjectDependency** | ProjectDependencies | ID, OrganizationId, Name, ProjectId, DependentProjectID, RequiredProjectID, Description, ExpectedCompletionDate | Belongs to Project | |
| **Risk** | Risks | ID, OrganizationId, ProjectId, Name, Description, Severity (enum), MitigationStrategy, IdentifiedDate, IsMitigated | Belongs to Project | |

### Feedback & Recognition Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **Feedback** | Feedbacks | Id, OrganizationId, TeamMemberId, Date, Type (enum), Title, Content, Context, OneOnOneId | Belongs to TeamMember | |
| **Kudos** | Kudos | Id, OrganizationId, UserId, TeamMemberId, Title, Message, Category (enum), LinkedTaskId, LinkedOkrId, LinkedMeetingId, DeliveryChannel (enum), DeliveryStatus (enum), DeliveredAt, DeliveryError, ScheduledFor, IsPublic, MentionInMeetingPrep | Belongs to TeamMember | Delivery system for Teams/Slack/Email |

### Performance Review Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **ReviewTemplate** | ReviewTemplates | Id, OrganizationId, Name, Description, ReviewType (enum), IsDefault, IsActive | Has Sections | |
| **ReviewTemplateSection** | ReviewTemplateSections | Id, ReviewTemplateId, Title, Description, SortOrder | Belongs to Template, Has Questions | |
| **ReviewTemplateQuestion** | ReviewTemplateQuestions | Id, ReviewTemplateSectionId, Text, QuestionType (enum), SortOrder, IsRequired, RatingMin, RatingMax, RatingLabels | Belongs to Section | |
| **PerformanceReviewCycle** | PerformanceReviewCycles | Id, OrganizationId, Name, Description, ReviewTemplateId, Status (enum), SelfReviewStartDate, SelfReviewDueDate, ManagerReviewStartDate, ManagerReviewDueDate, CalibrationDate, ShareDate | Has Reviews, Template | |
| **PerformanceReview** | PerformanceReviews | Id, OrganizationId, PerformanceReviewCycleId, TeamMemberId, Status (enum), OverallRating | Belongs to Cycle, TeamMember | |

### Survey Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **PulseSurvey** | PulseSurveys | Id, OrganizationId, Title, Description, Status (enum), SentDate, DueDate, ClosedDate, IsAnonymous | Has Questions, Responses | |
| **PulseSurveyQuestion** | PulseSurveyQuestions | Id, PulseSurveyId, Text, QuestionType (enum), SortOrder, RatingMin, RatingMax, RatingMinLabel, RatingMaxLabel, Category, IsRequired | Belongs to Survey | |
| **PulseSurveyResponse** | PulseSurveyResponses | Id, PulseSurveyId, TeamMemberId, SubmittedAt | Has Answers | |
| **PulseSurveyAnswer** | PulseSurveyAnswers | Id, PulseSurveyResponseId, PulseSurveyQuestionId, RatingValue, TextValue, BoolValue | Belongs to Response, Question | |

### Notes & AI Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **QuickNote** | QuickNotes | Id, OrganizationId, Title, Content, Category (enum), LinkedEntityType (enum), LinkedEntityId, TeamMemberId, ProjectId, OneOnOneId, IsPinned, IsArchived, Tags | Polymorphic links | |
| **VectorEmbedding** | VectorEmbeddings | Id (GUID), OrganizationId, EntityType, EntityId, ChunkIndex, Content, Embedding (byte[]), EmbeddingDimensions, MetadataJson | Belongs to Organization | Semantic search support |
| **Insight** | Insights | Id, UniqueKey, Type (enum), Severity (enum), Title, Description, ActionSuggestion, GeneratedAt, DismissedAt, ActedOnAt, IsRead, EntityType, EntityId | | AI-generated insights (not persisted to DB?) |

### Other Entities

| Entity | Table Name | Key Properties | Relationships | Special Behavior |
|--------|------------|---------------|---------------|------------------|
| **Reminder** | Reminders | Id, OrganizationId, Type (enum), Status (enum), Title, Message, DueDateTime, SnoozedUntil, OneOnOneId, TeamMemberId, TaskId, GoalId, IsRecurring, RecurrenceIntervalDays | | Notification scheduling |
| **ProgressSnapshot** | ProgressSnapshots | Id, EntityType, EntityId, SnapshotDate, CurrentValue, TargetValue, Progress, UserId, CreatedAt | | Historical tracking for analytics |
| **MeetingPrep** | (Runtime only) | MeetingId, TeamMember, MeetingDate, GeneratedAt, Sections, AiSuggestedAgenda, OverdueTaskCount, OpenActionItemCount, OkrsAtRiskCount, DaysSinceLastMeeting | | Not persisted |
| **DailyBriefing** | (Runtime only) | GeneratedAt, Greeting, MeetingsToday, Insights, UpcomingBirthdays, UpcomingAnniversaries, ActiveOkrCount, etc. | | Not persisted |
| **LinkedItem** | (Embedded) | Type (enum), ItemId, Title | | Used in AgendaItem |

---

## Part 2: OLD → NEW Terminology Mapping

| OLD C# Term | NEW Supabase Term | Notes |
|-------------|-------------------|-------|
| **ObjectiveKeyResult** | **goals** | OKRs renamed to Goals |
| **KeyResult** | **targets** | Key Results renamed to Targets |
| **KeyResultMeasurable** | **target_measurables** | Same concept, renamed |
| **KeyPerformanceIndicator** | **metrics** | KPIs renamed to Metrics |
| **KpiDataSource** | **metric_data_sources** | Same concept, renamed |
| **IndividualTask** | **tasks** | Simplified name |
| **MeetingTask** | **action_items** | Clearer name for meeting outcomes |
| **OneOnOne** | **meetings** (meeting_type='one_on_one') | Unified into meetings table |
| **AgendaItem** | **meeting_agenda_items** | Renamed |
| **QuickNote** | **notes** | Simplified name |
| **Kudos** | **recognition** | More professional term |
| **IndividualGoal** | ❌ **MISSING** | Not in new schema! |
| **Feedback** (C#) | **feedback** | Same concept, expanded |
| **PulseSurvey** | ❌ **MISSING** | Not in new schema! |
| **PerformanceReview** | ❌ **MISSING** | Not in new schema! |

---

## Part 3: NEW Supabase Schema Tables

### Core Tables (02_CORE_TABLES.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **organizations** | id, name, slug, subscription_tier, max_users, max_team_members, settings (JSONB), is_active, created_at, updated_at, created_by | Multi-tenant root |
| **roles** | id, name, display_name, description, 20+ permission booleans, is_system_role, sort_order | Permission definitions |
| **users** | id, supabase_auth_id, organization_id, email, display_name, first_name, last_name, avatar_url, phone, timezone, linked_team_member_id, preferences (JSONB), notification_settings (JSONB), is_active, is_email_verified, last_login_at, is_deleted | Login users |
| **user_roles** | id, user_id, organization_id, role_id, team_id, assigned_by | User-role assignments |

### Teams Tables (03_TEAMS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **teams** | id, organization_id, name, description, color, lead_user_id, is_active, is_deleted | Team groupings |
| **team_members** | id, organization_id, manager_user_id, linked_user_id, first_name, last_name, nickname, email, phone, job_title, department, hire_date, birthday, location, bio, avatar_url, linkedin_url, employment_status (enum), termination_date, is_active, active_goal_count, open_task_count, last_meeting_date, next_meeting_date, sync_* fields, is_deleted | People being managed |
| **team_memberships** | id, team_id, team_member_id, is_lead, joined_at, left_at | Many-to-many team assignments |
| **manager_history** | id, organization_id, team_member_id, manager_user_id, start_date, end_date, change_reason | Manager change tracking |

### Goals Tables (04_GOALS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **goals** | id, organization_id, owner_team_member_id, created_by_user_id, title, description, time_period (enum), year, start_date, end_date, status (enum), status_override, progress_percent, progress_override, is_team_visible, is_org_visible, project_id, sync_* fields, is_deleted | Main goal entity |
| **targets** | id, goal_id, title, description, target_value, current_value, starting_value, unit, weight, status (enum), sort_order, is_deleted | Measurable outcomes |
| **target_measurables** | id, target_id, measurable_type, measurable_id, aggregation_type | Links to metrics/projects |
| **goal_milestones** | id, goal_id, title, description, target_date, completed_date, is_completed, sort_order | Goal milestones |

### Metrics Tables (05_METRICS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **metrics** | id, organization_id, owner_team_member_id, created_by_user_id, name, description, category, current_value, target_value, baseline_value, unit, target_direction (enum), frequency (enum), last_updated_at, is_composite, parent_metric_id, is_team_visible, is_org_visible, warning_threshold, critical_threshold, sync_* fields, is_deleted | KPI replacement |
| **metric_data_sources** | id, metric_id, source_type, source_id, source_config (JSONB), aggregation_type | Data source links |
| **metric_history** | id, metric_id, value, recorded_at, recorded_by_user_id, source, notes | Historical values |

### Projects & Tasks Tables (06_PROJECTS_TASKS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **projects** | id, organization_id, owner_team_member_id, created_by_user_id, name, description, color, start_date, target_end_date, actual_end_date, status (enum), progress_percent, priority (enum), is_team_visible, is_deleted | Project entity |
| **project_members** | id, project_id, team_member_id, role, joined_at | Project assignments |
| **milestones** | id, project_id, title, description, target_date, completed_date, is_completed, sort_order | Project milestones |
| **tasks** | id, organization_id, owner_team_member_id, created_by_user_id, parent_task_id, project_id, goal_id, meeting_id, title, description, status (enum), priority (enum), due_date, completed_at, sort_order, sync_* fields, is_deleted | Unified task entity |
| **task_collections** | id, organization_id, name, description, query_config (JSONB) | Task groupings |
| **task_collection_items** | id, collection_id, task_id | Collection memberships |

### Meetings Tables (07_MEETINGS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **meetings** | id, organization_id, created_by_user_id, meeting_type (enum), manager_team_member_id, report_team_member_id, team_id, title, description, scheduled_at, duration_minutes, recurrence_rule, location, status (enum), started_at, ended_at, sync_* fields, is_deleted | Unified meeting entity |
| **meeting_attendees** | id, meeting_id, team_member_id, response, response_at, attended | Meeting participants |
| **meeting_agenda_items** | id, meeting_id, added_by_team_member_id, title, notes, sort_order, is_discussed, discussed_at, time_estimate_minutes, actual_duration_minutes | Agenda items |
| **meeting_notes** | id, meeting_id, author_team_member_id, content, is_private, ai_summary, sync_* fields | Meeting notes |
| **action_items** | id, meeting_id, assignee_team_member_id, title, description, due_date, is_completed, completed_at, converted_task_id | Meeting action items |
| **talking_points** | id, manager_team_member_id, report_team_member_id, added_by_team_member_id, title, notes, category, is_recurring, is_active, last_discussed_at | Recurring 1:1 topics |

### Feedback Tables (08_FEEDBACK.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **feedback** | id, organization_id, from_team_member_id, to_team_member_id, feedback_type (enum), sentiment (enum), content, context_type, context_id, is_private, is_requested, request_id, ai_summary, ai_tags (JSONB), is_acknowledged, acknowledged_at, sync_* fields, is_deleted | Feedback given |
| **feedback_requests** | id, organization_id, requester_team_member_id, requested_from_team_member_id, about_team_member_id, message, context_type, context_id, due_date, status, response_feedback_id | Feedback solicitation |
| **recognition** | id, organization_id, from_team_member_id, to_team_member_id, title, message, badge_type, project_id, goal_id, company_values (JSONB), is_public, reactions_count, is_deleted | Public recognition |
| **recognition_reactions** | id, recognition_id, team_member_id, reaction_type | Recognition likes |

### Notes Tables (09_NOTES.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **notes** | id, organization_id, author_team_member_id, title, content, content_format, linked_team_member_id, linked_meeting_id, linked_project_id, linked_goal_id, linked_task_id, category, tags (JSONB), is_private, is_pinned, pinned_at, ai_summary, ai_suggested_actions (JSONB), sync_* fields, is_deleted | Free-form notes |
| **note_templates** | id, organization_id, created_by_user_id, name, description, content_template, template_type, is_personal, sort_order, is_deleted | Reusable templates |
| **journal_entries** | id, team_member_id, entry_date, content, mood_rating, energy_level, wins (JSONB), challenges (JSONB), grateful_for (JSONB), progress_on_goals, ai_insights, sync_* fields | Self-reflection journals |

### AI/Vector Tables (10_AI_VECTORS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **vector_embeddings** | id, organization_id, entity_type, entity_id, content_hash, content_preview, embedding (vector), metadata (JSONB), model_name, model_version | Semantic search |
| **ai_conversations** | id, organization_id, team_member_id, title, context_entity_type, context_entity_id, is_active, is_deleted | AI chat sessions |
| **ai_messages** | id, conversation_id, role, content, prompt_tokens, completion_tokens, total_tokens, model_name, referenced_entities (JSONB), message_order | Chat messages |
| **ai_insights** | id, organization_id, target_team_id, target_team_member_id, insight_type, category, title, summary, details (JSONB), priority, recommended_actions (JSONB), source_entities (JSONB), valid_from, valid_until, is_dismissed, dismissed_at, dismissed_by, is_actioned, actioned_at, action_notes | Proactive insights |

### Activity Tables (11_ACTIVITY_NOTIFICATIONS.sql)

| Table | Columns | Purpose |
|-------|---------|---------|
| **activity_log** | id, organization_id, actor_user_id, actor_team_member_id, action, entity_type, entity_id, entity_name, old_values (JSONB), new_values (JSONB), context_type, ip_address, user_agent | Audit trail |
| **notifications** | id, organization_id, user_id, notification_type, title, message, entity_type, entity_id, action_url, priority, is_read, read_at, is_dismissed, dismissed_at, email_sent, email_sent_at, expires_at | User notifications |
| **notification_preferences** | id, user_id, notification_type, in_app_enabled, email_enabled, push_enabled, email_frequency | Notification settings |
| **announcements** | id, organization_id, created_by_user_id, title, content, target_type, target_team_id, target_role_ids (JSONB), publish_at, expires_at, is_pinned, priority, is_published, is_deleted | Org announcements |
| **announcement_reads** | id, announcement_id, user_id, read_at | Read tracking |

---

## Part 4: Gap Analysis

### 🔴 CRITICAL: Entities Missing in Supabase

| Old C# Entity | Old Purpose | Impact | Recommendation |
|---------------|-------------|--------|----------------|
| **IndividualGoal** | Personal development goals (career growth, certifications) | Users cannot track personal development | ADD: Create `personal_goals` table |
| **PulseSurvey** + **PulseSurveyQuestion** + **PulseSurveyResponse** + **PulseSurveyAnswer** | Team health/engagement surveys | Cannot conduct engagement surveys | ADD: Create survey tables |
| **ReviewTemplate** + **ReviewTemplateSection** + **ReviewTemplateQuestion** + **PerformanceReviewCycle** + **PerformanceReview** | Performance review system | Cannot conduct performance reviews | ADD: Create performance review tables |
| **Reminder** | Notification scheduling (birthdays, meetings, tasks) | No proactive reminders | ADD: Create `reminders` table |
| **ProgressSnapshot** | Historical tracking for trajectory analysis | Cannot do trend analysis or predictions | ADD: Create `progress_snapshots` table |

### 🟠 MODERATE: Properties/Columns Missing in Supabase

| Entity | Missing Properties | Impact |
|--------|-------------------|--------|
| **TeamMember** | ProfileImage (binary), FacebookProfile, InstagramProfile, XProfile, Specialty (enum), SkillLevel (enum), Role (enum), NickName (vs nickname), CellPhone (vs phone) | Social profiles lost, engineering specialties lost |
| **User** | Username, FirmId, PasswordHash | Local auth info lost (may be intentional for Supabase auth) |
| **OneOnOne → meetings** | GoogleCalendarEventId, CalendarEventId, TeamsMeetingUrl, TeamsMeetingId, GoogleMeetUrl, CalendarEventEtag, LastSyncedAt, SyncStatus, IsSyncedToGoogle, Feedback (text field), Agenda (text field) | **Calendar sync completely missing!** |
| **AgendaItem → meeting_agenda_items** | Category (enum), Priority (enum), Resolution, LinkedTaskId, LinkedItems collection | Cannot categorize/prioritize agenda items |
| **Project** | Budget, TeamMembers collection | No budget tracking for projects |
| **Kudos → recognition** | DeliveryChannel, DeliveryStatus, DeliveredAt, DeliveryError, ScheduledFor, MentionInMeetingPrep, LinkedTaskId, LinkedOkrId, LinkedMeetingId | **External delivery system missing!** |
| **Risk** | Entire entity missing | Cannot track project risks |
| **ProjectDependency** | Entire entity missing | Cannot track project dependencies |

### 🟡 MINOR: Functionality Differences

| Area | C# Behavior | Supabase Behavior | Notes |
|------|-------------|-------------------|-------|
| **IDs** | Mix of int and GUID | All UUID | Migration will need ID mapping |
| **Soft Delete** | AuditableEntity base class | Per-table is_deleted | Consistent in new schema |
| **Sync** | Various sync fields | Consistent sync_* pattern | Better in new schema |
| **Enums** | C# enums | PostgreSQL ENUMs | Need enum mapping |
| **Binary Data** | ProfileImage as byte[] | avatar_url as URL | Storage strategy change |
| **Runtime Props** | Many computed properties | Database functions | Good separation |

### 🟢 NEW in Supabase (Not in C#)

| Feature | Tables | Benefit |
|---------|--------|---------|
| **Roles & Permissions** | roles, user_roles | Fine-grained access control |
| **Teams Hierarchy** | teams, team_memberships | Proper team organization |
| **Journal Entries** | journal_entries | Personal reflection/mood tracking |
| **Note Templates** | note_templates | Reusable note structures |
| **Feedback Requests** | feedback_requests | Formal feedback solicitation |
| **Recognition Reactions** | recognition_reactions | Social engagement features |
| **AI Conversations** | ai_conversations, ai_messages | Persistent AI chat history |
| **AI Insights** | ai_insights | Structured proactive insights |
| **Metric History** | metric_history | Historical metric values |
| **Announcements** | announcements, announcement_reads | Org-wide communications |
| **Activity Log** | activity_log | Complete audit trail |
| **Notification Preferences** | notification_preferences | Per-type notification control |

---

## Part 5: Summary

### 🔴 CRITICAL Gaps (Would Break Core Functionality)

1. **Performance Reviews** - Entire system missing (5 tables)
2. **Pulse Surveys** - Entire system missing (4 tables)
3. **Personal Development Goals** - IndividualGoal entity missing
4. **Calendar Sync** - No CalendarLink table, no sync fields on meetings
5. **External Kudos Delivery** - Teams/Slack/Email delivery system missing
6. **Reminders** - No reminder/notification scheduling system
7. **Progress Snapshots** - Cannot do trend analysis or trajectory prediction

### 🟠 MODERATE Gaps (Nice-to-Have Missing)

1. **Project Risks** - Risk entity missing
2. **Project Dependencies** - ProjectDependency entity missing
3. **Project Budget** - Budget field missing
4. **Team Member Social Profiles** - Facebook, Instagram, X profiles missing
5. **Team Member Skills** - Specialty, SkillLevel enums missing
6. **Agenda Item Categories** - Category, Priority enums missing
7. **Profile Images** - Changed to URL (might need migration)

### 🟢 NEW Features (Improvements)

1. **Fine-grained permissions** - Roles table with 20+ permission flags
2. **Team hierarchy** - Teams and team_memberships for proper org structure
3. **Journal entries** - Personal reflection/mood tracking (new feature!)
4. **Feedback requests** - Formal feedback solicitation workflow
5. **Recognition reactions** - Social engagement on kudos
6. **AI conversation history** - Persistent chat context
7. **Structured AI insights** - Actionable insight system
8. **Metric history** - Historical values for trending
9. **Complete audit log** - activity_log table
10. **Org announcements** - Company-wide communication system
11. **Granular notification preferences** - Per-type settings

---

## Recommended Actions

### Immediate (Before Migration)

1. **Add Performance Review tables** - Required for annual reviews
2. **Add Pulse Survey tables** - Required for engagement tracking
3. **Add personal_goals table** - Required for career development
4. **Add calendar_links table** - Required for calendar sync
5. **Add reminders table** - Required for proactive notifications
6. **Add progress_snapshots table** - Required for analytics

### Consider Adding

1. **project_risks** table - For risk management
2. **project_dependencies** table - For dependency tracking
3. **Add missing team_member columns** - Social profiles, skills

### Migration Notes

1. **ID Conversion** - All int IDs → UUID
2. **Binary Images** - Upload to storage, store URLs
3. **Enum Mapping** - Create PostgreSQL ENUMs matching C# enums
4. **Calendar Sync** - Design new cloud-based calendar sync approach
5. **Kudos Delivery** - Design new notification/delivery system
