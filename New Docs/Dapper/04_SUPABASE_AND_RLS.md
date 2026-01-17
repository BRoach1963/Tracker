# Supabase and Row-Level Security (RLS)

**Document Version:** 2.0  
**Last Updated:** January 17, 2026  
**Prerequisites:** Read [03_REPOSITORY_PATTERN.md](03_REPOSITORY_PATTERN.md) first

---

## Overview

Tracker uses **Supabase** as its PostgreSQL database provider. Supabase provides:

1. **Hosted PostgreSQL** - Fully managed database
2. **Authentication** - User signup/login (we use this)
3. **Row-Level Security (RLS)** - Database-level access control
4. **Realtime** - Live data updates (not currently used)
5. **Storage** - File storage (not currently used)

This document explains how we connect to Supabase and how RLS protects data.

---

## What is Supabase?

Supabase is a "Firebase alternative" built on PostgreSQL. Think of it as:

- PostgreSQL database in the cloud
- With a nice dashboard
- With built-in auth
- With automatic APIs (we don't use these - we use Dapper directly)

**We use Supabase for:**
- ✅ Hosted PostgreSQL database
- ✅ User authentication (signup, login, password reset)
- ✅ Row-Level Security policies

**We DON'T use Supabase for:**
- ❌ Auto-generated REST APIs (we use Dapper)
- ❌ Realtime subscriptions
- ❌ File storage

---

## Connecting to Supabase

### Connection Method

We connect directly to the PostgreSQL database using **Npgsql** (not the Supabase client API).

```csharp
// We do this (direct PostgreSQL via Npgsql)
var connection = new NpgsqlConnection(
    "Server=db.xxx.supabase.co;Port=5432;Database=postgres;...");

// We DON'T do this (Supabase REST API)
var client = new Supabase.Client(url, key);
await client.From<User>().Select().Get();
```

### Why Direct PostgreSQL?

| Supabase Client | Direct PostgreSQL |
|-----------------|-------------------|
| Uses REST API | Uses native PostgreSQL protocol |
| Limited query flexibility | Full SQL power |
| Extra network hop | Direct database access |
| Supabase SDK overhead | Dapper is minimal |

### Connection String

```
Server=db.{project-id}.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password={your-password};SSL Mode=Require
```

---

## Row-Level Security (RLS)

### What is RLS?

RLS is a PostgreSQL feature that automatically filters rows based on who's querying. It's enforced at the **database level**, not the application level.

```sql
-- Example RLS policy: Users can only see their own organization's data
CREATE POLICY "org_isolation" ON meetings
    FOR ALL
    USING (organization_id = current_setting('app.current_org_id')::uuid);
```

### Why RLS Matters

Without RLS, a bug in application code could expose data from other organizations:

```csharp
// BUG: Missing organization filter
var allMeetings = await connection.QueryAsync<Meeting>("SELECT * FROM meetings");
// Returns ALL meetings from ALL organizations! 😱
```

With RLS, the database itself enforces the filter:

```csharp
// Even if we forget the filter...
var allMeetings = await connection.QueryAsync<Meeting>("SELECT * FROM meetings");
// RLS automatically adds: WHERE organization_id = {current_org}
// Only returns current organization's meetings ✅
```

### RLS is a Safety Net

Our repositories include proper WHERE clauses, but RLS provides defense-in-depth:

1. **Application Layer:** Repository filters by organization/user
2. **Database Layer:** RLS double-checks the filter

If application code has a bug, RLS prevents data leakage.

---

## Current RLS Configuration

### Tables with RLS Enabled

Most tables in Supabase have RLS enabled. Key policies:

| Table | Policy | Effect |
|-------|--------|--------|
| `users` | `user_can_see_self` | Users see only their own record |
| `team_members` | `org_isolation` | Users see only their organization's members |
| `meetings` | `created_by_user_id` | Users see only meetings they created |
| `meeting_attendees` | `via_meetings` | Users see attendees for meetings they created |
| `goals` | `org_isolation` | Users see only their organization's goals |
| `tasks` | `org_isolation` | Users see only their organization's tasks |

### How RLS Knows the Current User

Supabase provides `auth.uid()` function that returns the current authenticated user's ID from the JWT token:

```sql
-- RLS policies use auth.uid() to identify the current user
CREATE POLICY "Users can view their own data" ON meetings
    USING (created_by_user_id = auth.uid());
```

---

## Standard RLS Policy Templates

### Simple Ownership Policy (Recommended)

For tables where the user owns the record directly:

```sql
-- Enable RLS on the table
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;

-- SELECT: Users can view their own records
CREATE POLICY "Users can view meetings they created"
ON meetings FOR SELECT
USING (created_by_user_id = auth.uid());

-- INSERT: Users can create records owned by themselves
CREATE POLICY "Users can insert meetings"
ON meetings FOR INSERT
WITH CHECK (created_by_user_id = auth.uid());

-- UPDATE: Users can update their own records
CREATE POLICY "Users can update meetings they created"
ON meetings FOR UPDATE
USING (created_by_user_id = auth.uid());

-- DELETE: Users can delete their own records
CREATE POLICY "Users can delete meetings they created"
ON meetings FOR DELETE
USING (created_by_user_id = auth.uid());
```

### Junction Table Policy (Parent Reference)

For junction tables like `meeting_attendees` that reference a parent table:

```sql
-- Enable RLS
ALTER TABLE meeting_attendees ENABLE ROW LEVEL SECURITY;

-- SELECT: Can view attendees for meetings you created
CREATE POLICY "Users can view meeting_attendees for their meetings"
ON meeting_attendees FOR SELECT
USING (
    EXISTS (
        SELECT 1 FROM meetings m
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- INSERT: Can add attendees to meetings you created
CREATE POLICY "Users can insert meeting_attendees for their meetings"
ON meeting_attendees FOR INSERT
WITH CHECK (
    EXISTS (
        SELECT 1 FROM meetings m
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- UPDATE: Can update attendees for meetings you created
CREATE POLICY "Users can update meeting_attendees for their meetings"
ON meeting_attendees FOR UPDATE
USING (
    EXISTS (
        SELECT 1 FROM meetings m
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- DELETE: Can remove attendees from meetings you created
CREATE POLICY "Users can delete meeting_attendees for their meetings"
ON meeting_attendees FOR DELETE
USING (
    EXISTS (
        SELECT 1 FROM meetings m
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);
```

---

## ⚠️ RLS Anti-Patterns (AVOID THESE)

### 1. Infinite Recursion (CRITICAL)

**The Problem:** Policy on Table A references Table B, and policy on Table B references Table A.

```sql
-- ❌ WRONG: This causes infinite recursion!

-- Policy on meetings:
CREATE POLICY "view meetings" ON meetings FOR SELECT
USING (
    EXISTS (SELECT 1 FROM meeting_attendees ma 
            WHERE ma.meeting_id = meetings.id)
    -- ↑ This triggers meeting_attendees RLS policy
);

-- Policy on meeting_attendees:
CREATE POLICY "view attendees" ON meeting_attendees FOR SELECT
USING (
    EXISTS (SELECT 1 FROM meetings m 
            WHERE m.id = meeting_attendees.meeting_id)
    -- ↑ This triggers meetings RLS policy → INFINITE LOOP!
);
```

**The Error:**
```
ERROR: infinite recursion detected in policy for relation "meetings"
```

**The Solution:** Always have ONE table with a simple, non-referencing policy:

```sql
-- ✅ CORRECT: meetings has simple policy (no subqueries)
CREATE POLICY "view meetings" ON meetings FOR SELECT
USING (created_by_user_id = auth.uid());
-- ↑ No subquery, no cross-table reference

-- ✅ CORRECT: meeting_attendees references meetings (one-way only)
CREATE POLICY "view attendees" ON meeting_attendees FOR SELECT
USING (
    EXISTS (SELECT 1 FROM meetings m 
            WHERE m.id = meeting_attendees.meeting_id
            AND m.created_by_user_id = auth.uid())
);
```

### 2. Using USING vs WITH CHECK Incorrectly

**SELECT/UPDATE/DELETE** use `USING` clause:
- Filters which existing rows the user can see/modify
- Like an automatic WHERE clause

**INSERT** uses `WITH CHECK` clause:
- Validates NEW rows being inserted
- Rejects insert if check fails

```sql
-- ❌ WRONG: INSERT with USING (does nothing)
CREATE POLICY "bad insert" ON meetings FOR INSERT
USING (created_by_user_id = auth.uid());  -- This is ignored!

-- ✅ CORRECT: INSERT with WITH CHECK
CREATE POLICY "good insert" ON meetings FOR INSERT
WITH CHECK (created_by_user_id = auth.uid());
```

### 3. Overly Complex Policies

**Avoid:** Policies with multiple JOINs, complex logic, or business rules.

```sql
-- ❌ WRONG: Too complex, hard to debug, performance issues
CREATE POLICY "complex" ON tasks FOR SELECT
USING (
    owner_id = auth.uid() 
    OR EXISTS (SELECT 1 FROM projects p 
               JOIN project_members pm ON pm.project_id = p.id
               WHERE p.id = tasks.project_id 
               AND pm.user_id = auth.uid()
               AND pm.role IN ('admin', 'editor'))
    OR (is_public = true AND status != 'draft')
);

-- ✅ CORRECT: Keep it simple
CREATE POLICY "simple" ON tasks FOR SELECT
USING (created_by_user_id = auth.uid());
-- Handle complex access logic in application code
```

---

## Fixing Common RLS Errors

### Error: "permission denied for table X"

**Cause:** No RLS policy grants access, or policies reference wrong column.

**Fix:**
```sql
-- Check existing policies
SELECT tablename, policyname, cmd, qual, with_check 
FROM pg_policies 
WHERE tablename = 'your_table';

-- If no policies exist, create them (see templates above)
```

### Error: "infinite recursion detected"

**Cause:** Circular policy references between tables.

**Fix:**
```sql
-- Drop all policies on both tables
DROP POLICY IF EXISTS "policy_name" ON table_a;
DROP POLICY IF EXISTS "policy_name" ON table_b;

-- Recreate with one-way references only
-- Parent table: simple ownership check
-- Child table: references parent
```

### Error: NULL returned for INSERT policy qual

**This is expected!** INSERT policies use `with_check` not `qual`:

```sql
-- When you query pg_policies:
SELECT tablename, policyname, cmd, qual, with_check 
FROM pg_policies 
WHERE tablename = 'meetings';

-- INSERT row will show:
-- qual = NULL (normal)
-- with_check = (your check expression)
```

---

## Supabase Authentication

### SupabaseService

`Services/Backend/SupabaseService.cs` handles Supabase auth:

```csharp
public class SupabaseService
{
    private Supabase.Client? _client;

    public async Task<(bool Success, string? Error)> SignInAsync(
        string email, 
        string password)
    {
        var session = await _client!.Auth.SignIn(email, password);
        return (session?.User != null, null);
    }

    public async Task<(bool Success, string? Error)> SignUpAsync(
        string email, 
        string password, 
        string? displayName = null)
    {
        var session = await _client!.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                ["display_name"] = displayName ?? email.Split('@')[0]
            }
        });
        return (session?.User != null, null);
    }
}
```

### Authentication Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Login Form    │────▶│  SupabaseService │────▶│  Supabase Auth  │
│  (email/pass)   │     │   SignInAsync    │     │    Service      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
                                                 ┌─────────────────┐
                                                 │  JWT Token      │
                                                 │  (access_token) │
                                                 └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Application                                       │
│  - Store access token                                                │
│  - Set app.current_user_id for RLS                                  │
│  - Use token for API calls                                          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Supabase Schema Structure

Our Supabase database has 60+ tables. Key tables:

### Core Tables (Repositories Exist)

```
users                   - Application users
team_members            - Staff/employees
organizations           - Companies/firms
meetings                - All meeting types
tasks                   - Work items/action items
goals                   - OKRs/objectives
targets                 - Key results
metrics                 - KPIs/measurements
projects                - Project containers
feedback                - Feedback records
development_goals       - Personal development goals
performance_reviews     - Review records
pulse_surveys           - Survey definitions
```

### Supporting Tables (No Repositories - Use Raw SQL)

```
meeting_attendees       - Junction: meeting ↔ attendees
meeting_agenda_items    - Agenda items for meetings
survey_questions        - Questions in surveys
survey_responses        - Completed survey responses
metric_history          - Historical metric values
activity_log            - Audit trail
notification_preferences - User notification settings
vector_embeddings       - AI embeddings (future)
```

### Common Column Patterns

Every table follows these patterns:

**Primary Key:**
```sql
id UUID PRIMARY KEY DEFAULT gen_random_uuid()
```

**Audit Columns:**
```sql
created_at TIMESTAMPTZ DEFAULT NOW()
created_by UUID REFERENCES users(id)
updated_at TIMESTAMPTZ
updated_by UUID
```

**Soft Delete:**
```sql
is_deleted BOOLEAN DEFAULT false
deleted_at TIMESTAMPTZ
deleted_by UUID REFERENCES users(id)
```

**Sync Support (for offline):**
```sql
sync_id UUID DEFAULT gen_random_uuid()
sync_version INTEGER DEFAULT 1
sync_modified_at TIMESTAMPTZ DEFAULT NOW()
sync_status sync_status_enum
```

---

## Environment Configuration

### Getting Supabase Credentials

1. Go to [supabase.com](https://supabase.com)
2. Select your project
3. Go to **Settings** → **API**
4. Copy:
   - **Project URL** (e.g., `https://xxx.supabase.co`)
   - **anon/public key** (for client-side auth)
   - **service_role key** (for server-side, keep secret!)
5. Go to **Settings** → **Database**
6. Copy:
   - **Connection string** (for Npgsql/Dapper)

### Storing Credentials

**For Development:**
```json
// appsettings.Development.json
{
  "Supabase": {
    "ProjectUrl": "https://xxx.supabase.co",
    "AnonKey": "eyJ..."
  },
  "ConnectionStrings": {
    "Supabase": "Server=db.xxx.supabase.co;Port=5432;..."
  }
}
```

**For Production:**
```bash
# Environment variables
SUPABASE_PROJECT_URL=https://xxx.supabase.co
SUPABASE_ANON_KEY=eyJ...
TRACKER_SUPABASE_CONNECTION_STRING=Server=db.xxx.supabase.co;...
```

---

## Supabase Dashboard

### Key Areas

| Section | Purpose |
|---------|---------|
| **Table Editor** | View/edit data directly |
| **SQL Editor** | Run SQL queries |
| **Authentication** | See users, configure providers |
| **Database** | Connection strings, settings |
| **Logs** | API and database logs |
| **Settings** | Project configuration |

### Common Tasks

**View table data:**
1. Go to Table Editor
2. Select table from sidebar
3. Browse/filter data

**Run SQL:**
1. Go to SQL Editor
2. Write query
3. Click "Run"

**Check RLS policies:**
1. Go to Authentication → Policies
2. Select table
3. View/edit policies

**Reset a user's password:**
1. Go to Authentication → Users
2. Find user
3. Click "Send password reset"

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| `permission denied for table` | RLS blocking access | Check RLS policies, verify auth.uid() matches |
| `infinite recursion detected` | Circular policy references | Remove cross-table references, use one-way pattern |
| `JWT expired` | Access token timeout | Refresh token or re-authenticate |
| `connection refused` | Project paused (free tier) | Go to dashboard, resume project |
| `no rows returned` | RLS filtering all data | Verify created_by_user_id matches auth.uid() |

### Debugging RLS

**Step 1: Check what policies exist:**
```sql
SELECT tablename, policyname, cmd, qual, with_check 
FROM pg_policies 
WHERE tablename = 'your_table';
```

**Step 2: Test as admin (bypasses RLS):**
```sql
-- In Supabase SQL Editor (runs as admin, bypasses RLS)
SELECT * FROM meetings WHERE id = 'xxx';
-- If this returns data but app doesn't, it's an RLS issue
```

**Step 3: Verify user ID matches:**
```sql
-- Check what created_by_user_id the records have
SELECT id, title, created_by_user_id FROM meetings LIMIT 10;

-- Compare to the user ID your app is sending
-- (Check your app logs for the auth.uid() value)
```

**Step 4: Temporarily disable RLS for testing:**
```sql
-- ⚠️ Only in development!
ALTER TABLE meetings DISABLE ROW LEVEL SECURITY;

-- Run your test

-- Re-enable immediately
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;
```

### Viewing All RLS Policies

```sql
-- See all policies across all tables
SELECT 
    schemaname,
    tablename, 
    policyname, 
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies 
WHERE schemaname = 'public'
ORDER BY tablename, cmd;
```

---

## Best Practices

### DO:
- ✅ Keep policies simple - prefer `created_by_user_id = auth.uid()`
- ✅ Use one-way references for junction tables (child → parent only)
- ✅ Test policies with multiple user accounts
- ✅ Include `is_deleted = false` checks in policies if using soft delete
- ✅ Create separate policies for SELECT, INSERT, UPDATE, DELETE
- ✅ Document your policies in this file when adding new ones

### DON'T:
- ❌ Create circular references between table policies
- ❌ Use complex JOINs or business logic in policies
- ❌ Disable RLS in production
- ❌ Use `service_role` key in client code
- ❌ Share database passwords
- ❌ Use USING clause for INSERT (use WITH CHECK)

### Policy Naming Convention

Use descriptive names that explain what the policy allows:
```sql
-- Good names
"Users can view meetings they created"
"Users can insert their own tasks"
"Users can update meeting_attendees for their meetings"

-- Bad names
"policy1"
"meetings_select"
"rls"
```

---

## Quick Reference: Creating Policies for New Tables

When adding a new table, follow this checklist:

```sql
-- 1. Enable RLS
ALTER TABLE new_table ENABLE ROW LEVEL SECURITY;

-- 2. Create SELECT policy
CREATE POLICY "Users can view their own new_table records"
ON new_table FOR SELECT
USING (created_by_user_id = auth.uid());

-- 3. Create INSERT policy (note: WITH CHECK, not USING)
CREATE POLICY "Users can insert new_table records"
ON new_table FOR INSERT
WITH CHECK (created_by_user_id = auth.uid());

-- 4. Create UPDATE policy
CREATE POLICY "Users can update their own new_table records"
ON new_table FOR UPDATE
USING (created_by_user_id = auth.uid());

-- 5. Create DELETE policy
CREATE POLICY "Users can delete their own new_table records"
ON new_table FOR DELETE
USING (created_by_user_id = auth.uid());

-- 6. Verify
SELECT * FROM pg_policies WHERE tablename = 'new_table';
```

---

## Next Steps

**Next:** Read [05_AUTHENTICATION_FLOW.md](05_AUTHENTICATION_FLOW.md) to understand the complete authentication flow including login, signup, and token management.
