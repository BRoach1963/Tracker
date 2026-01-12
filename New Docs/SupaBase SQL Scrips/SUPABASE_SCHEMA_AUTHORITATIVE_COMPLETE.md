# SUPABASE COMPLETE SCHEMA - AUTHORITATIVE SOURCE

**Source:** Direct SQL export from Supabase database  
**Status:** THIS IS THE REAL SCHEMA - Use this for all validation  
**Total Tables:** 60+  
**All IDs:** UUID  

---

## KEY DIFFERENCES FROM WHAT I HAD DOCUMENTED

I was working with partial schema (5 tables). The REAL schema includes:

### Tables I MISSED:

1. **action_items** - Meeting action items (not just in tasks)
2. **ai_conversations, ai_insights, ai_messages** - AI features
3. **talking_points** - Recurring discussion topics for 1:1s
4. **development_goals** - Personal development goals (separate from goals)
5. **performance_reviews** - Formal reviews
6. **review_cycles, review_templates, review_responses, review_template_questions/sections**
7. **surveys, survey_instances, survey_questions, survey_responses, survey_answers**
8. **calendar_links** - OAuth connections to Google/Outlook
9. **journal_entries** - Personal journal/reflections
10. **organization_snapshots, team_member_snapshots, team_snapshots, progress_snapshots** - Analytics data
11. **activity_log** - Audit trail
12. **announcements, announcement_reads** - Org communications
13. **feedback, feedback_requests** - Feedback system (separate tables)
14. **recognition, recognition_reactions** - Recognition/praise system
15. **reminders, reminder_preferences** - Reminder system
16. **notification_preferences, notifications** - Notifications
17. **note_templates, notes** - Notes with templates
18. **task_collections, task_collection_items** - Grouping tasks
19. **Vector_embeddings** - pgvector for AI embeddings
20. **user_sessions** - Session management
21. **roles, user_roles** - Role-based access control

---

## CRITICAL SCHEMA FEATURES I MISSED

### Sync Fields (on many tables):
```
sync_id uuid DEFAULT gen_random_uuid(),
sync_version integer DEFAULT 1,
sync_modified_at timestamp with time zone DEFAULT now(),
sync_status sync_status (synced, pending, failed, etc)
```

These are for **offline sync** - the app can work offline and sync back to Supabase.

### Soft Delete Pattern (on many tables):
```
is_deleted boolean NOT NULL DEFAULT false,
deleted_at timestamp with time zone,
deleted_by uuid  -- FK to users (optional)
```

### User-Defined Types (Enums):
The schema uses custom enum types like:
- `meeting_type` (one_on_one, team_meeting, all_hands, project, interview, other)
- `meeting_status` (scheduled, in_progress, completed, cancelled)
- `task_status` (not_started, in_progress, blocked, completed, cancelled)
- `task_priority` (low, medium, high, critical)
- `goal_status` (draft, active, completed, on_hold, cancelled)
- `goal_time_period` (q1, q2, q3, q4, full_year, custom)
- `feedback_type`, `feedback_sentiment`
- `employment_status` (active, on_leave, terminated)
- And many more...

These are PostgreSQL enums - NOT just strings. The C# code needs `enum` types too.

### Calendar Integration:
```csharp
calendar_event_id varchar,
calendar_provider enum,  // google, outlook, etc
calendar_link_id uuid,   // FK to calendar_links
video_conference_url text,
video_conference_provider varchar,
calendar_sync_status enum,
last_synced_at timestamp
```

The `calendar_links` table has OAuth tokens:
```
provider enum (google, outlook, etc)
account_email varchar
access_token text
refresh_token text
token_expires_at timestamp
```

---

## WHAT THIS MEANS FOR THE C# CODE

### 1. You have WAY more models than I thought
The C# project probably has models for:
- Action items (separate from tasks?)
- AI conversations/messages
- Talking points
- Development goals
- Performance reviews + cycles + templates
- Surveys
- Calendar links with OAuth
- Journal entries
- Notifications
- Recognition
- Feedback (separate table)
- And more...

### 2. Every model needs sync fields
If the mobile app or offline features work, every major table has:
- `SyncId` (Guid)
- `SyncVersion` (int)
- `SyncModifiedAt` (DateTime)
- `SyncStatus` (enum)

### 3. Most models need soft delete
```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
public Guid? DeletedBy { get; set; }  // FK to users
```

### 4. Enums are DATABASE-LEVEL enums
Not just C# enums. PostgreSQL has enforced enum types. This matters for:
- Meeting validation (must be one of the allowed meeting types)
- Status validation (can't set invalid status)
- Type validation

---

## IMMEDIATE ISSUES

### Problem 1: Missing Tables in C# Models
Do you have C# models for:
- action_items?
- development_goals?
- calendar_links?
- journal_entries?
- All the review/survey tables?

If not, those need to be created.

### Problem 2: The DbContext is MASSIVE
With 60+ tables, your `TrackerDbContext` needs DbSet properties for all of them. Do you have them all?

### Problem 3: Enum Validation
C# enums won't auto-validate PostgreSQL enums. If you set an invalid enum value, PostgreSQL will reject it. Need to ensure enums match exactly.

### Problem 4: No Going Back to Partial Schema
I can't work with the 5-table schema anymore. Every fix needs to account for the 60-table reality.

---

## NEXT STEPS

**BEFORE I PROPOSE ANY CHANGES:**

1. **Tell me:** Which of these 60 tables do you actually have C# models for?
   - Can you list out which tables are missing?
   - Which tables have models but might be outdated?

2. **Show me:** Your actual `TrackerDbContext.cs` - the full DbSet property list
   - This will tell me if models exist but aren't registered

3. **Confirm:** The sync fields and soft delete pattern
   - Are these implemented across all models?
   - Or just some?

4. **The EF Core question becomes even more critical**
   - With 60 tables and complex relationships, EF Core baggage is even worse
   - But complete refactor would be massive

This changes the entire scope of work. I need to understand the real codebase state before proposing a path forward.

**Don't make any changes yet. Just answer those 4 questions and we'll know exactly what we're dealing with.**

