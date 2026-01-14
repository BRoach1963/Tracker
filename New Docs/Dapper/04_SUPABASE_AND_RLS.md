# Supabase and Row-Level Security (RLS)

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
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
| `meetings` | `org_isolation` | Users see only their organization's meetings |
| `goals` | `org_isolation` | Users see only their organization's goals |
| `tasks` | `org_isolation` | Users see only their organization's tasks |

### How RLS Knows the Current User

When connecting, we set PostgreSQL session variables:

```sql
-- Set at connection time (handled by connection setup)
SET app.current_user_id = '{user-uuid}';
SET app.current_org_id = '{organization-uuid}';
```

RLS policies reference these variables:

```sql
CREATE POLICY "org_isolation" ON meetings
    USING (organization_id = current_setting('app.current_org_id')::uuid);
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
| `permission denied for table` | RLS blocking access | Check RLS policies, verify session variables |
| `JWT expired` | Access token timeout | Refresh token or re-authenticate |
| `connection refused` | Project paused (free tier) | Go to dashboard, resume project |
| `no rows returned` | RLS filtering all data | Verify organization_id matches session |

### Debugging RLS

To see what RLS is filtering:

```sql
-- In Supabase SQL Editor (as admin)
-- Temporarily disable RLS
ALTER TABLE meetings DISABLE ROW LEVEL SECURITY;

-- Run your query
SELECT * FROM meetings WHERE id = 'xxx';

-- Re-enable RLS
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;
```

### Viewing Session Variables

```sql
-- Check current session settings
SELECT 
    current_setting('app.current_user_id', true) as user_id,
    current_setting('app.current_org_id', true) as org_id;
```

---

## Best Practices

### DO:
- ✅ Always set session variables after authentication
- ✅ Include organization_id in queries (defense-in-depth)
- ✅ Use RLS as a safety net, not the only protection
- ✅ Test with different user accounts to verify isolation

### DON'T:
- ❌ Disable RLS in production
- ❌ Use `service_role` key in client code
- ❌ Share database passwords
- ❌ Bypass RLS for "convenience"

---

## Next Steps

**Next:** Read [05_AUTHENTICATION_FLOW.md](05_AUTHENTICATION_FLOW.md) to understand the complete authentication flow including login, signup, and token management.
