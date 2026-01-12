# SCHEMA ANALYSIS: WHICH TABLES NEED C# MODELS?

**Analysis Date:** January 12, 2026  
**Database:** Supabase PostgreSQL (60 tables)  
**Architecture:** Cloud-first, RLS-enforced, offline-sync-ready

---

## 1. TABLE CATEGORIZATION

### CATEGORY A: CORE BUSINESS MODELS (MUST MODEL - 20 tables)

These are the heart of the application. **Users need to interact with these.**

```
✅ users                    - Authentication context
✅ team_members             - Employees being tracked
✅ organizations            - Tenancy/billing root
✅ teams                    - Team groupings
✅ meetings                 - 1:1s, team meetings, etc.
✅ tasks                    - Work items
✅ goals                    - OKRs/objectives
✅ targets                  - Key results attached to goals
✅ metrics                  - KPIs/performance data
✅ projects                 - Project management
✅ milestones               - Project milestones
✅ feedback                 - Feedback given/received
✅ development_goals        - Personal development
✅ performance_reviews      - Formal reviews
✅ recognition              - Praise/kudos
✅ notes                    - Rich notes/documentation
✅ journal_entries          - Personal reflections
✅ calendar_links           - OAuth to Google/Outlook
✅ announcements            - Org communications
✅ reminders                - Task/meeting reminders
```

**Action:** ALL OF THESE NEED C# MODELS

---

### CATEGORY B: RELATIONSHIP/JUNCTION TABLES (CONDITIONAL)

These link two main entities. **Decision: use navigation properties or explicit models?**

```
❓ team_memberships         - Links team → team_members (use nav property in Team class)
❓ project_members          - Links project → team_members (use nav property in Project class)
❓ task_collection_items    - Links collection → tasks (use nav property in TaskCollection class)
❓ goal_milestones          - Links goal → milestones (use nav property in Goal class)
❓ target_measurables       - Links target → measurables (polymorphic, might need explicit model)
```

**Recommendation:** Use EF navigation properties. Create models ONLY if you need to query them independently or store extra metadata (like join_date, role, etc.).

**Current models exist:**
- ✅ TeamMembership
- ✅ ProjectMember  
- ✅ TaskCollectionItem
- ✅ GoalMilestone
- ✅ TargetMeasurable

**Action:** KEEP these, they're already modeled

---

### CATEGORY C: INFRASTRUCTURE/METADATA (NO MODELS NEEDED - 15 tables)

These are database infrastructure, not business data. **Write-only or auth-layer.**

```
❌ activity_log             - Audit trail (write-only via triggers/app logic, no CRUD needed)
❌ user_roles               - RBAC assignments (belongs in Supabase Auth, not app)
❌ roles                    - Role definitions (static config, not app-driven)
❌ user_sessions            - Session tracking (Supabase Auth handles this)
❌ notification_preferences - User settings (store in User.Preferences jsonb field)
❌ reminder_preferences     - User settings (store in User.Preferences jsonb field)
❌ vector_embeddings        - pgvector backend (write via AI service, minimal read)
❌ announcement_reads       - Read receipt tracking (write-only, query via SELECT COUNT)
❌ survey_responses         - Response sessions (just metadata, query aggregates)
❌ survey_instances         - Survey runs (reference data, query-only)
❌ feedback_requests        - Request tracking (could be column on Feedback, not separate table)
❌ manager_history          - Manager changes (audit table, write-only)
❌ review_cycles            - Review periods (mostly static, reference data)
❌ talking_points           - Recurring agenda items (could be in notes or agenda_items)
```

**Why no models:**
- **activity_log** - Audit trail. Write via EF change tracking or triggers. No app CRUD needed.
- **user_roles/roles** - Supabase Auth manages roles. Not needed in app DbContext.
- **user_sessions** - Supabase auth library handles sessions. App doesn't query this.
- **settings** tables - Store in jsonb field on User/Organization instead.
- **vector_embeddings** - AI service writes these. App just passes vectors around (not deserialized).
- **Tracking/audit tables** - Write-only via app logic or DB triggers.

**Action:** DO NOT MODEL THESE

---

### CATEGORY D: ANALYTICS/SNAPSHOTS (OPTIONAL - 5 tables)

These are for dashboards and trend analysis. **Query-heavy, insert-only.**

```
⚠️ organization_snapshots   - Weekly org metrics (could model as read-only, or use raw SQL)
⚠️ team_member_snapshots    - Weekly per-person metrics (same)
⚠️ team_snapshots           - Weekly per-team metrics (same)
⚠️ progress_snapshots       - Weekly per-entity metrics (same)
⚠️ metric_history           - Time series data (same)
```

**Decision:** 
- **Option 1:** Don't model. Query via raw SQL/Dapper for performance.
- **Option 2:** Model as read-only (no change tracking) for simple aggregations.

**Recommendation:** Use raw SQL. Analytics queries are often complex (GROUP BY, window functions, CTEs). EF overhead not worth it.

**Action:** DO NOT MODEL. Query via Dapper or raw SQL.

---

### CATEGORY E: SURVEY/REVIEW SYSTEM (NEED MODELS - 8 tables)

These are complex, structured survey/review workflows.

```
✅ surveys                  - Survey definitions
✅ survey_questions         - Questions in surveys
✅ survey_responses         - Response sessions (person taking survey)
✅ survey_answers           - Individual answers (Q&A)
✅ review_templates         - Review templates
✅ review_template_sections - Sections in template
✅ review_template_questions - Questions in template
✅ reviews                  - Individual reviews
```

**Current status:** 
- Have PulseSurvey models
- Have ReviewTemplate/PerformanceReview models
- May need to verify schema alignment

**Action:** VERIFY THESE EXIST AND MATCH SCHEMA

---

### CATEGORY F: AI & CONTEXT (OPTIONAL - 3 tables)

```
⚠️ ai_conversations         - Chat sessions with AI (might need model for context)
⚠️ ai_messages              - Messages within conversations (might need model)
⚠️ ai_insights              - AI-generated insights (probably don't need model, just notifications)
```

**Decision:** 
- If AI is core feature → model these
- If AI is secondary → store in notes/insights table, don't model separately

**Action:** DEPENDS ON AI PRIORITY

---

## 2. FINAL RECOMMENDATION: WHICH 35-40 TABLES NEED MODELS

### MUST HAVE (20):
```
users
team_members
organizations
teams
meetings
tasks
goals
targets
metrics
projects
milestones
feedback
development_goals
performance_reviews
recognition
notes
journal_entries
calendar_links
announcements
reminders
```

### SHOULD HAVE (10):
```
team_memberships           (junction, but useful as explicit model)
project_members            (junction, but useful as explicit model)
task_collection_items      (junction, but useful as explicit model)
goal_milestones            (already exists)
target_measurables         (already exists)
meeting_agenda_items       (needed - tracks meeting agenda)
pulse_surveys              (already exists)
survey_questions           (already exists)
survey_responses           (already exists)
survey_answers             (already exists)
```

### OPTIONAL (5-10):
```
review_templates           (already exists)
review_template_sections   (already exists)
review_template_questions  (already exists)
reviews / PerformanceReviews (already exists)
metric_history             (time series - optional)
ai_conversations           (if AI is core feature)
ai_messages                (if AI is core feature)
development_goal_comments  (nested comment system)
development_goal_milestones (already exists)
```

### DO NOT MODEL (15-20):
```
activity_log
user_roles
roles
user_sessions
notification_preferences
reminder_preferences
vector_embeddings
announcement_reads
survey_instances
feedback_requests
manager_history
review_cycles
talking_points
organization_snapshots
team_member_snapshots
team_snapshots
progress_snapshots
```

---

## 3. IS ENTITY FRAMEWORK THE RIGHT CHOICE?

**Honest Assessment:**

### EF Core - CONS for Supabase Architecture:

❌ **Massive DbContext Bloat**
- 35-40 models = huge context
- Every model change requires migration
- Change tracking overhead for 60 tables
- Slower startup time

❌ **Migrations Conflict with Supabase**
- Supabase has its own migration system (SQL scripts in UI)
- EF migrations are .NET-specific
- You end up maintaining TWO migration systems
- **Source of truth problem** (we've been doing this for days!)

❌ **RLS Makes EF Filtering Redundant**
- Supabase enforces RLS at database layer
- EF query filters at application layer are REDUNDANT
- If dev forgets `.Where()` filter, RLS still protects (but bad practice)
- Two layers of security = confusion and bugs

❌ **Offline Sync Support**
- EF not designed for offline-first patterns
- You'll need custom sync logic anyway
- Dapper wouldn't help either, but wouldn't add overhead

❌ **Complex Queries Require Raw SQL**
- Window functions (cumulative goals progress)
- CTEs (recursive manager hierarchies)
- Full-text search on notes
- Aggregations (team stats snapshots)
- **→ You'll write raw SQL anyway, defeating EF purpose**

❌ **JSONB Fields Become Strings**
- Supabase uses JSONB for: preferences, settings, metadata
- EF can't query JSONB natively (needs F# extensions or manual mapping)
- **→ More raw SQL**

❌ **PostgreSQL Enums Not Well Supported**
- meeting_type, task_status, goal_status are PostgreSQL enums
- EF treats them as strings
- Type safety lost

❌ **Performance Overhead**
- EF adds reflection, query compilation, change tracking
- Supabase charges per operation
- Every inefficiency costs money
- Dapper would be 2-3x faster

❌ **Null Navigation Properties Are Footguns**
- With 35+ models and relationships, null checks everywhere
- Risk of N+1 queries (query per row)
- EF lazy loading disabled to avoid issues → manual eager loading everywhere

---

### EF Core - PROS for Supabase Architecture:

✅ **LINQ Queries Are Type-Safe**
- Compile-time checking vs. string-based SQL
- Intellisense works
- Refactoring easier

✅ **Change Tracking for Updates**
- `.SaveChanges()` knows what changed
- Useful for "update only modified fields"
- But Supabase RLS means you can't update anyway (RLS blocks it)

✅ **CRUD Operations Simple**
- Get, Create, Update, Delete boilerplate handled
- But these are maybe 20% of your queries
- 80% are complex reads/aggregations

✅ **Some Ecosystem Tools Expect EF**
- Some libraries assume DbContext exists
- But you can work around this

---

## 4. WHAT SHOULD YOU USE INSTEAD?

### OPTION A: DAPPER (Recommended)

**Why Dapper for Supabase:**

✅ Lightweight
- Just query execution + mapping
- No reflection overhead
- Startup time fast

✅ SQL is Explicit
- You control every query
- Window functions, CTEs, full-text search all work
- Performance optimization obvious

✅ Offline Sync Ready
- Easy to intercept queries for sync logging
- Simple to batch operations for offline
- Change tracking is YOUR code (transparent)

✅ PostgreSQL Native
- Use PostgreSQL enums as enums
- Query JSONB fields with operators
- Use array types
- Use pgvector

✅ Supabase Migration Compatible
- Supabase migrations work as-is
- No separate EF migrations
- Single source of truth (SQL scripts in Supabase)

✅ Better Performance
- 2-3x faster than EF for complex queries
- Less memory per query
- Scales better with many models

**Cons:**
- More boilerplate (but manageable)
- Manual mapping for complex objects (but libraries like Dapper.Contrib help)
- Manual relationship loading (explicit is better anyway)

---

### OPTION B: KEEP EF CORE BUT OPTIMIZE

**Only if you:**
1. Strip down to ~25 core models (drop analytics, infrastructure tables)
2. Use Dapper for complex queries (hybrid approach)
3. Use raw SQL for migrations (stop using EF Migrations)
4. Disable global query filters (RLS handles security)

**This means:**
- EF for simple CRUD on core models
- Dapper for complex queries
- Raw SQL for migrations
- Keep DbContext focused

---

### OPTION C: FULL RAW NPGSQL

**Most control but most boilerplate.**

Only if you want absolute performance or doing something exotic.

---

## 5. FINAL RECOMMENDATION

**Switch to DAPPER.**

**Reasoning:**
1. **You have 60 tables** - EF overhead is real
2. **Supabase has migrations** - EF migrations are redundant
3. **RLS enforced at DB** - EF filters are redundant
4. **Offline sync needed** - Dapper easier to instrument
5. **Complex queries inevitable** - You'll write raw SQL anyway
6. **Cloud costs matter** - Every query inefficiency costs money

**Implementation:**
1. Keep C# models (DTOs) as they are
2. Create repository layer with Dapper
3. Use raw SQL migrations (version control SQL scripts)
4. Drop EF Core from DbContext (or keep it minimal for logging)
5. Implement offline sync in repository layer

**Work effort:** ~2-3 days to refactor data access layer
**Payoff:** Better performance, clearer code, easier maintenance, true source of truth

---

## 6. MY HONEST TAKE

EF Core is great for:
- Small applications (10-15 tables)
- Simple CRUD operations
- Teams with limited SQL knowledge

EF Core is **NOT** good for:
- Cloud-native PostgreSQL applications
- Multi-tenant RLS systems
- Complex aggregation queries
- Performance-sensitive operations
- Applications with offline sync

**You're in the "NOT" category.**

Dapper would eliminate:
- The schema validation loop you've been in for 2 days
- The migration confusion (SQL scripts are source of truth)
- The query filter redundancy
- The JSONB/enum/array mapping issues
- The performance concerns

**Do you want me to:**
1. Create a migration plan to Dapper?
2. Start by refactoring the data access layer?
3. Keep EF but implement the "hybrid approach"?

