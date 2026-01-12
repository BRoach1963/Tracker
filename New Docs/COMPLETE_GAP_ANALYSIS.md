# COMPLETE GAP ANALYSIS - C# MODELS vs SUPABASE SCHEMA

**Source:** 51 C# DataModel files + TrackerDbContext.cs + Complete Supabase schema (60 tables)  
**Status:** AUTHORITATIVE - actual codebase state  
**Date:** January 12, 2026

---

## 1. C# MODELS THAT EXIST (51 files)

```
AuditableEntity (base class)
AgendaItem
BusySlot
CalendarLink ✅
ChangeTrackingEntry
DailyBriefing
DevelopmentGoal ✅
DevelopmentGoalComment ✅
DevelopmentGoalMilestone ✅
Feedback ✅
Goal ✅
GoalMilestone ✅
Insight
Kudos ✅
ManagerHistory ✅
Meeting ✅
MeetingAgendaItem
MeetingAttendee ✅
MeetingLinkedGoal
MeetingLinkedTask
MeetingMetricLink ✅
MeetingNote ✅
MeetingPrep
MeetingTemplate ✅
Metric ✅
MetricDataSource ✅
MetricHistory ✅
Milestone ✅
Organization ✅
PerformanceReview ✅
PrepItem
PrepSection
ProgressSnapshot ✅
Project ✅
ProjectDependency ✅
ProjectMember ✅
PulseSurvey ✅
QuickNote
Reminder ✅
Risk ✅
Target ✅
TargetMeasurable ✅
Team ✅
TeamMember ✅
TeamMembership ✅
TimeSlot
TrackerTask ✅
User ✅
VectorEmbedding ✅
TaskCollection ✅
TaskCollectionItem ✅
```

---

## 2. SUPABASE TABLES (60 total) - WHAT'S MISSING IN C#

### MISSING - NO C# MODEL YET:

❌ **action_items** - Meeting action items (not "MeetingTask")
❌ **ai_conversations** - AI chat conversations
❌ **ai_insights** - AI-generated insights (have "Insight" but may not match schema)
❌ **ai_messages** - Messages within conversations
❌ **activity_log** - Audit trail of all actions
❌ **announcement_reads** - Who read announcements
❌ **announcements** - Organization announcements
❌ **calendar_sync_status** - (might be enum on meetings table, not separate table)
❌ **feedback_requests** - Feedback request tracking
❌ **journal_entries** - Personal journal/reflections
❌ **manager_history** - Have model, need to verify it's hooked in DbContext
❌ **meeting_agenda_items** - Have model, need to verify
❌ **notification_preferences** - User notification settings
❌ **notifications** - Individual notifications
❌ **note_templates** - Templates for notes
❌ **notes** - Rich notes (have "QuickNote", might not be same)
❌ **organization_snapshots** - Weekly/periodic snapshots
❌ **recognition** - Recognition entries (have "Kudos", different concept?)
❌ **recognition_reactions** - Reactions to recognition
❌ **reminder_preferences** - User reminder preferences
❌ **review_cycles** - Review cycle management
❌ **review_template_questions** - Questions in templates
❌ **review_template_sections** - Sections in templates
❌ **review_responses** - Answers to review questions
❌ **reviews** - Individual reviews (have "PerformanceReview", might be same)
❌ **roles** - Role definitions (might be missing)
❌ **survey_answers** - Individual survey answers
❌ **survey_instances** - Survey instances/runs
❌ **survey_questions** - Survey questions
❌ **survey_responses** - Survey response sessions
❌ **surveys** - Survey definitions (have "PulseSurvey", might not match)
❌ **talking_points** - Recurring discussion topics
❌ **team_member_snapshots** - Weekly snapshots per team member
❌ **team_snapshots** - Weekly snapshots per team
❌ **user_roles** - User-role assignments
❌ **user_sessions** - Session tracking

**Estimated Missing:** ~30 tables have no C# model or incomplete model

---

## 3. DbContext CHECKS - WHAT'S REGISTERED

From `TrackerDbContext.cs` DbSets I found:

✅ Users
✅ TeamMembers
✅ Meetings
✅ MeetingAttendees
✅ MeetingNotes
✅ Projects
✅ ProjectMembers
✅ TrackerTasks
✅ AgendaItems
✅ Goals
✅ Targets
✅ TargetMeasurables
✅ GoalMilestones
✅ Metrics
✅ MetricDataSources
✅ MeetingMetricLinks
✅ TaskCollections
✅ TaskCollectionItems
✅ Milestones
✅ Risks
✅ ProjectDependencies
✅ ChangeTrackingEntries
✅ Feedbacks
✅ Kudoses
✅ DevelopmentGoals
✅ DevelopmentGoalMilestones
✅ DevelopmentGoalComments
✅ Reminders
✅ MeetingTemplates
✅ MeetingTemplateItems
✅ QuickNotes
✅ PulseSurveys
✅ PulseSurveyQuestions
✅ PulseSurveyResponses
✅ PulseSurveyAnswers
✅ ReviewTemplates
✅ ReviewTemplateSections
✅ ReviewTemplateQuestions
✅ PerformanceReviewCycles
✅ PerformanceReviews
✅ PerformanceReviewSections
✅ PerformanceReviewAnswers
✅ Kudos
✅ ProgressSnapshots
✅ CalendarLinks

**NOT in DbSets (need to check if they exist but aren't registered):**
❌ ActionItems
❌ AIConversations
❌ AIInsights
❌ AIMessages
❌ ActivityLog
❌ Announcements
❌ AnnouncementReads
❌ FeedbackRequests
❌ JournalEntries
❌ ManagerHistory (EXISTS but might not be in DbSets)
❌ MeetingAgendaItems (EXISTS but might not be registered)
❌ NotificationPreferences
❌ Notifications
❌ NoteTemplates
❌ Notes
❌ OrganizationSnapshots
❌ Recognition
❌ RecognitionReactions
❌ ReminderPreferences
❌ ReviewCycles
❌ ReviewResponses
❌ Roles
❌ SurveyAnswers
❌ SurveyInstances
❌ SurveyQuestions
❌ SurveyResponses
❌ Surveys
❌ TalkingPoints
❌ TeamMemberSnapshots
❌ TeamSnapshots
❌ UserRoles
❌ UserSessions
❌ VectorEmbeddings (EXISTS but might not be registered)

---

## 4. SYNC FIELDS STATUS

From what I can see, **not all models have sync fields** (`SyncId`, `SyncVersion`, `SyncModifiedAt`, `SyncStatus`).

The schema shows these fields on:
- meetings
- tasks
- goals
- metrics
- notes
- feedback
- recognition
- development_goals
- journal_entries
- team_members
- meeting_notes
- sync_status enum

**Need to verify:** Do all C# models that need sync fields have them?

---

## 5. SOFT DELETE STATUS

Based on schema, these tables support soft delete:
- meetings (is_deleted, deleted_at, deleted_by)
- tasks (is_deleted, deleted_at, deleted_by)
- goals (is_deleted, deleted_at, deleted_by)
- And ~30 others

**Key question:** Do all AuditableEntity subclasses have:
```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
public Guid? DeletedBy { get; set; }
```

---

## 6. CRITICAL ISSUES IDENTIFIED

### Issue #1: ~30 Missing Models
Supabase has 60 tables, you have 51 model files. Some tables have NO C# representation.

### Issue #2: Models Might Not Be Registered
Even if the C# class exists, if it's not in `TrackerDbContext.DbSets`, EF Core won't manage it.

### Issue #3: Sync Fields Incomplete
Schema supports offline sync on many tables, but not clear if C# models reflect this.

### Issue #4: Soft Delete Inconsistent
Need to verify all models that should have soft delete actually do.

### Issue #5: Database Provider Contamination
DbContext still supports SQLite and SQL Server as fallback options. The constructor comments say "Supabase is ONLY path" but the code still supports 3 providers.

---

## 7. NEXT ACTION ITEMS

**Before fixing any code, need answers:**

1. **List of missing models** - Which of the ~30 missing models should actually be implemented vs which are optional?

2. **Verify DbSet registration** - Are all 51 models registered in DbContext?

3. **Sync field completeness** - Which models have sync fields? All? Some?

4. **Remove database provider support** - Should I strip out SQLite/SQL Server code from DbContext and go Supabase-only?

5. **Enum alignment** - Do C# enums match the PostgreSQL enum types exactly?

