# Tracker PostgreSQL Migration Plan
## Strategic Architecture Change: SQLite → PostgreSQL + Auth Migration

**Status**: PLANNING (Do Not Implement)  
**Priority**: TOP - Roadmap Priority #1  
**Last Updated**: January 3, 2026

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Current vs Target Architecture](#current-vs-target-architecture)
3. [Critical Decision Points](#critical-decision-points)
4. [Phase 1: Schema Design](#phase-1-schema-design)
5. [Phase 2: Auth Migration](#phase-2-auth-migration)
6. [Phase 3: Application Changes](#phase-3-application-changes)
7. [Phase 4: Data Migration](#phase-4-data-migration)
8. [Phase 5: Team Features](#phase-5-team-features)
9. [Deployment Options](#deployment-options)
10. [Risks & Concerns](#risks--concerns)
11. [Open Questions](#open-questions)
12. [Timeline Estimate](#timeline-estimate)

---

## Executive Summary

### What We're Doing
Migrating Tracker from local SQLite databases to PostgreSQL with self-contained authentication, enabling:
- Multi-user/team collaboration
- User data portability across managers/teams
- Unified tech stack with Pro Causa and Praxis
- Cloud OR self-hosted deployment options

### What's Changing
| Component | Current | Target |
|-----------|---------|--------|
| Database | SQLite (local file per user) | PostgreSQL (remote) |
| Auth | Supabase Auth | PostgreSQL-based (self-contained) |
| Data Location | User's machine | Cloud or self-hosted server |
| Multi-user | ❌ No | ✅ Yes |
| Offline | ✅ Full | ⚠️ Limited/None |
| Team Features | ❌ No | ✅ Yes |

### Why Auth Must Move Out of Supabase
1. **Self-hosted requirement**: Enterprise customers want everything on-prem
2. **Unified stack**: Can't have Supabase auth if customer self-hosts PostgreSQL
3. **Consistency**: Pro Causa and Praxis won't use Supabase
4. **Cost control**: No per-auth-user fees for large teams
5. **Data sovereignty**: Some customers can't have ANY cloud dependency

---

## Current vs Target Architecture

### Current Architecture
```
┌─────────────────────────────────────────────────────────┐
│                    User's Computer                       │
│  ┌──────────────┐    ┌──────────────────────────────┐   │
│  │  Tracker.exe │───▶│  SQLite DB (local file)      │   │
│  │              │    │  - TeamMembers               │   │
│  │              │    │  - Meetings                  │   │
│  │              │    │  - Tasks, OKRs, etc.         │   │
│  └──────┬───────┘    └──────────────────────────────┘   │
│         │                                                │
└─────────┼────────────────────────────────────────────────┘
          │ Auth Only
          ▼
┌─────────────────────┐
│   Supabase Cloud    │
│  - Authentication   │
│  - Subscriptions    │
│  - AI Credits       │
└─────────────────────┘
```

### Target Architecture (Cloud Option)
```
┌─────────────────────┐         ┌─────────────────────────────────┐
│  User's Computer    │         │     PostgreSQL Server           │
│  ┌──────────────┐   │         │     (Supabase or Self-hosted)   │
│  │  Tracker.exe │───┼────────▶│  ┌───────────────────────────┐  │
│  │              │   │         │  │  Auth Tables              │  │
│  │  (No local   │   │         │  │  - users                  │  │
│  │   database)  │   │         │  │  - sessions               │  │
│  └──────────────┘   │         │  │  - password_hashes        │  │
│                     │         │  ├───────────────────────────┤  │
└─────────────────────┘         │  │  Org Tables               │  │
                                │  │  - organizations          │  │
                                │  │  - org_members            │  │
                                │  │  - subscriptions          │  │
                                │  ├───────────────────────────┤  │
                                │  │  App Data (RLS-protected) │  │
                                │  │  - team_members           │  │
                                │  │  - meetings               │  │
                                │  │  - tasks, okrs, etc.      │  │
                                │  └───────────────────────────┘  │
                                └─────────────────────────────────┘
```

### Target Architecture (Self-Hosted Option)
```
┌─────────────────────┐         ┌─────────────────────────────────┐
│  User's Computer    │         │  Customer's Server/Network      │
│  ┌──────────────┐   │         │  ┌───────────────────────────┐  │
│  │  Tracker.exe │───┼────────▶│  │  PostgreSQL               │  │
│  │              │   │   VPN/  │  │  (Same schema as cloud)   │  │
│  │              │   │   LAN   │  │                           │  │
│  └──────────────┘   │         │  │  NO external dependencies │  │
│                     │         │  └───────────────────────────┘  │
└─────────────────────┘         └─────────────────────────────────┘
```

---

## Critical Decision Points

### ⚠️ DECISION 1: Offline Capability
**Question**: Do we maintain any offline capability?

| Option | Pros | Cons |
|--------|------|------|
| **A) No offline** | Simpler, no sync conflicts | Users can't work without internet |
| **B) Read-only cache** | Can view data offline | Complex, stale data issues |
| **C) Full offline + sync** | Best UX | Very complex, conflict resolution hell |

**Recommendation**: Option A (No offline) for v1. Small teams typically have connectivity. Add Option B later if customers demand it.

**YOUR CHOICE**: _________________

---

### ⚠️ DECISION 2: Auth Implementation
**Question**: How do we implement auth without Supabase?

| Option | Pros | Cons |
|--------|------|------|
| **A) Roll our own** | Full control, no dependencies | Security risk, lots of work |
| **B) Use library (e.g., ASP.NET Identity adapted)** | Battle-tested | May not fit desktop app model |
| **C) Lightweight JWT + bcrypt** | Simple, proven patterns | Still need to build session mgmt |
| **D) Keycloak/similar (self-hostable)** | Enterprise-grade | Another service to deploy |

**Recommendation**: Option C - Lightweight JWT + bcrypt. Store users/hashes in PostgreSQL, issue JWTs from app server (or embedded in desktop app for direct DB connection).

**YOUR CHOICE**: _________________

---

### ⚠️ DECISION 3: Database Access Model
**Question**: Does the desktop app connect directly to PostgreSQL, or through an API?

| Option | Pros | Cons |
|--------|------|------|
| **A) Direct DB connection** | Simpler, fewer moving parts | Connection string in app, harder to secure |
| **B) REST API layer** | Better security, rate limiting | Need to build/host API server |
| **C) Hybrid** | Flexibility | Complexity |

**Recommendation**: For cloud (Supabase), use their REST API. For self-hosted, allow direct connection OR self-hosted API.

**YOUR CHOICE**: _________________

---

### ⚠️ DECISION 4: Individual vs Team Data Model
**Question**: Is an "Individual" user just a single-person organization?

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate models** | Simpler queries for individuals | Two code paths to maintain |
| **B) Everyone has an org** | One code path | Individual users have "org of 1" |

**Recommendation**: Option B - Everyone has an org. Individual plan = org with 1 member. Simplifies queries and upgrades.

**YOUR CHOICE**: _________________

---

### ⚠️ DECISION 5: Data Ownership on Team Member Transfer
**Question**: When employee changes managers, who owns the 1:1 history?

| Option | Description |
|--------|-------------|
| **A) History stays with manager** | Original manager retains all meeting notes |
| **B) History transfers to new manager** | New manager gets full history |
| **C) History is shared/visible to both** | Both can access, neither "owns" |
| **D) Employee owns their own history** | Employee controls access |

**Recommendation**: Option C or D - This needs business input. Impacts RLS design significantly.

**YOUR CHOICE**: _________________

---

## Phase 1: Schema Design

### New Tables Required

```sql
-- ============================================
-- CORE IDENTITY / AUTH TABLES
-- ============================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(255),
    avatar_url TEXT,
    email_verified BOOLEAN DEFAULT FALSE,
    email_verification_token VARCHAR(255),
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL, -- hashed JWT or session token
    device_info TEXT,
    ip_address INET,
    created_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    revoked_at TIMESTAMP
);

CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    revoked BOOLEAN DEFAULT FALSE
);

-- ============================================
-- ORGANIZATION / TEAM TABLES
-- ============================================

CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE, -- for URL: tracker.app/org/acme-corp
    owner_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    settings JSONB DEFAULT '{}',
    is_individual BOOLEAN DEFAULT FALSE -- true for single-user "orgs"
);

CREATE TABLE organization_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'member', -- 'owner', 'admin', 'manager', 'member'
    invited_by UUID REFERENCES users(id),
    invited_at TIMESTAMP DEFAULT NOW(),
    joined_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending', -- 'pending', 'active', 'suspended'
    UNIQUE(organization_id, user_id)
);

CREATE TABLE organization_invites (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    role VARCHAR(50) DEFAULT 'member',
    token VARCHAR(255) UNIQUE NOT NULL,
    invited_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    accepted_at TIMESTAMP
);

-- ============================================
-- SUBSCRIPTION / BILLING TABLES
-- ============================================

CREATE TABLE subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    plan_type VARCHAR(50) NOT NULL, -- 'free', 'standard', 'pro', 'team_standard', 'team_pro', 'enterprise'
    status VARCHAR(50) NOT NULL, -- 'active', 'canceled', 'past_due', 'trialing'
    seat_count INTEGER DEFAULT 1,
    price_per_seat DECIMAL(10,2),
    billing_cycle VARCHAR(20), -- 'monthly', 'annual'
    current_period_start TIMESTAMP,
    current_period_end TIMESTAMP,
    stripe_subscription_id VARCHAR(255),
    stripe_customer_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE ai_credits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    credits_remaining INTEGER DEFAULT 0,
    credits_used_this_month INTEGER DEFAULT 0,
    monthly_reset_date TIMESTAMP,
    last_purchase_at TIMESTAMP
);

-- ============================================
-- APP DATA TABLES (migrated from SQLite)
-- ============================================

-- Team members now scoped to organization
CREATE TABLE team_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    manager_user_id UUID REFERENCES users(id), -- the manager who manages this person
    linked_user_id UUID REFERENCES users(id), -- if team member is also a Tracker user
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    role VARCHAR(255),
    department VARCHAR(255),
    hire_date DATE,
    photo_path TEXT,
    notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Meetings now reference org and potentially multiple participants
CREATE TABLE meetings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    created_by_user_id UUID REFERENCES users(id),
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    title VARCHAR(255),
    meeting_date TIMESTAMP NOT NULL,
    duration_minutes INTEGER,
    location VARCHAR(255),
    meeting_type VARCHAR(50), -- 'one_on_one', 'team', 'skip_level'
    status VARCHAR(50) DEFAULT 'scheduled', -- 'scheduled', 'completed', 'canceled'
    notes TEXT,
    private_notes TEXT, -- manager-only notes
    ai_summary TEXT,
    calendar_event_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Similar pattern for all other tables...
-- (tasks, okrs, key_results, kudos, pulse_surveys, performance_reviews, etc.)
-- Each gets organization_id and appropriate user references
```

### Row-Level Security (RLS) Strategy

```sql
-- Enable RLS on all tables
ALTER TABLE team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;
-- ... etc for all tables

-- Example policies

-- Users can only see their own org's team members
CREATE POLICY team_members_org_isolation ON team_members
    FOR ALL
    USING (
        organization_id IN (
            SELECT organization_id FROM organization_members 
            WHERE user_id = current_user_id() AND status = 'active'
        )
    );

-- Managers can only see team members they manage (more restrictive)
CREATE POLICY team_members_manager_only ON team_members
    FOR ALL
    USING (
        manager_user_id = current_user_id()
        OR 
        -- Admins can see all in org
        EXISTS (
            SELECT 1 FROM organization_members 
            WHERE user_id = current_user_id() 
            AND organization_id = team_members.organization_id
            AND role IN ('owner', 'admin')
        )
    );
```

### Migration Mapping: SQLite → PostgreSQL

| SQLite Table | PostgreSQL Table | Changes |
|--------------|------------------|---------|
| `TeamMembers` | `team_members` | +organization_id, +manager_user_id, +linked_user_id |
| `Meetings` | `meetings` | +organization_id, +created_by_user_id |
| `MeetingAgendaItems` | `meeting_agenda_items` | No structural change |
| `IndividualTasks` | `tasks` | +organization_id, +created_by_user_id |
| `ObjectiveKeyResults` | `okrs` | +organization_id |
| `KeyResults` | `key_results` | No structural change |
| `Kudos` | `kudos` | +organization_id, +given_by_user_id |
| `PulseSurveys` | `pulse_surveys` | +organization_id |
| `PerformanceReviews` | `performance_reviews` | +organization_id, +reviewer_user_id |
| `Reminders` | `reminders` | +user_id (personal) |
| `Settings` | Moved to `users.settings` or `organizations.settings` | JSONB column |

---

## Phase 2: Auth Migration

### Current Supabase Auth Flow
```
1. User clicks "Sign In"
2. App calls Supabase Auth API
3. Supabase validates credentials, returns JWT
4. App stores JWT, includes in API calls
5. Supabase validates JWT on each request
```

### New PostgreSQL Auth Flow
```
1. User clicks "Sign In"
2. App sends credentials to auth endpoint (API or direct)
3. Server validates against users table (bcrypt compare)
4. Server generates JWT with user claims
5. App stores JWT
6. Each DB query includes user context (for RLS)
```

### Auth Components to Build

1. **Password Hashing Service**
   ```csharp
   // Using BCrypt.Net-Next package
   public class PasswordService
   {
       public string HashPassword(string password) 
           => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
       
       public bool VerifyPassword(string password, string hash)
           => BCrypt.Net.BCrypt.Verify(password, hash);
   }
   ```

2. **JWT Token Service**
   ```csharp
   public class TokenService
   {
       public string GenerateAccessToken(User user, Organization org);
       public string GenerateRefreshToken();
       public ClaimsPrincipal ValidateToken(string token);
   }
   ```

3. **Session Management**
   - Store sessions in `user_sessions` table
   - Support multiple devices
   - Token refresh logic
   - Session revocation

4. **Auth Endpoints/Methods**
   - `Register(email, password, name)`
   - `Login(email, password)` → returns access + refresh tokens
   - `RefreshToken(refreshToken)` → returns new access token
   - `Logout(sessionId)` → revokes session
   - `ForgotPassword(email)` → sends reset link
   - `ResetPassword(token, newPassword)`
   - `VerifyEmail(token)`

### What to Remove from Current Codebase
- `SupabaseAuthService.cs`
- Supabase SDK auth references
- Supabase JWT validation
- Keep Supabase for legacy users during migration period?

### Security Considerations
- [ ] JWT secret management (config, not hardcoded)
- [ ] Token expiration (access: 15min, refresh: 7 days?)
- [ ] Rate limiting on auth endpoints
- [ ] Account lockout after failed attempts
- [ ] Secure password requirements
- [ ] HTTPS required for all auth traffic

---

## Phase 3: Application Changes

### Files That Touch the Database

Run this to find all database touchpoints:
```powershell
Get-ChildItem -Recurse -Include *.cs | Select-String -Pattern "TrackerDbContext|DbContext|SQLite|GetTeamMembers|GetMeetings|SaveChanges" | Select-Object Path -Unique
```

#### Known Database Touchpoints

| File/Class | What It Does | Changes Needed |
|------------|--------------|----------------|
| `TrackerDbContext.cs` | EF Core context | Replace SQLite provider with Npgsql |
| `TrackerDbManager.cs` | All CRUD operations | Update for multi-tenant queries |
| `TrackerDbManager.*.cs` | Partial classes | Add org context to all queries |
| `MigrationHelper.cs` | SQLite migrations | Rewrite for PostgreSQL |
| `DatabaseBackupService.cs` | Backup/restore | Remove or repurpose |
| `*ViewModel.cs` | Call DB manager | Pass user/org context |
| `*Service.cs` | Business logic | Tenant-aware queries |

### Abstraction Layer Changes

#### Current: Direct SQLite
```csharp
public async Task<List<TeamMember>> GetTeamMembersAsync()
{
    using var context = new TrackerDbContext(_dbPath);
    return await context.TeamMembers.ToListAsync();
}
```

#### Target: Tenant-Aware PostgreSQL
```csharp
public async Task<List<TeamMember>> GetTeamMembersAsync(UserContext userContext)
{
    using var context = CreateContext(userContext);
    return await context.TeamMembers
        .Where(t => t.OrganizationId == userContext.OrganizationId)
        .Where(t => t.ManagerUserId == userContext.UserId || userContext.IsAdmin)
        .ToListAsync();
}
```

### Connection Management

#### Current
```csharp
// Connection string is just a file path
var dbPath = Path.Combine(userDataFolder, "tracker.db");
optionsBuilder.UseSqlite($"Data Source={dbPath}");
```

#### Target
```csharp
// Connection determined by deployment mode
public class DatabaseConnectionFactory
{
    public TrackerDbContext CreateContext(UserContext user)
    {
        var connectionString = _config.GetConnectionString();
        
        var options = new DbContextOptionsBuilder<TrackerDbContext>()
            .UseNpgsql(connectionString)
            .Options;
            
        var context = new TrackerDbContext(options);
        
        // Set RLS context for this connection
        context.Database.ExecuteSqlRaw(
            $"SET app.current_user_id = '{user.UserId}'");
        context.Database.ExecuteSqlRaw(
            $"SET app.current_org_id = '{user.OrganizationId}'");
            
        return context;
    }
}
```

### New Services to Build

1. **OrganizationService**
   - Create organization
   - Invite members
   - Manage roles
   - Transfer ownership

2. **UserService** (replacing SupabaseAuthService)
   - Registration
   - Login/logout
   - Password management
   - Profile management

3. **SubscriptionService** (keep but modify)
   - Plan management
   - Seat counting
   - Billing integration

4. **TeamTransferService** (new)
   - Move employee between managers
   - Handle data ownership/visibility

---

## Phase 4: Data Migration

### Migration Scenarios

#### Scenario A: New User (Easy)
- Signs up → creates PostgreSQL account
- No data to migrate
- Gets org, starts fresh

#### Scenario B: Existing SQLite User → Cloud
1. User signs in with Supabase credentials (transitional)
2. App detects local SQLite database exists
3. Prompts: "Migrate your data to cloud?"
4. Creates PostgreSQL org + user
5. Uploads all SQLite data with new IDs
6. Maps old references to new UUIDs
7. Verifies data integrity
8. Optionally archives/deletes local SQLite

#### Scenario C: Team Migration
1. Admin creates team org
2. Invites existing users
3. Each user migrates their SQLite data
4. Data gets merged under team org
5. Deduplication of team members (by email?)

### Migration Tool Design

```csharp
public class SqliteToPostgresMigrator
{
    public async Task<MigrationResult> MigrateAsync(
        string sqlitePath, 
        UserContext targetUser,
        MigrationOptions options)
    {
        // 1. Validate SQLite database
        // 2. Create ID mapping table (old int IDs → new UUIDs)
        // 3. Migrate in dependency order:
        //    - TeamMembers first
        //    - Then Meetings (references TeamMembers)
        //    - Then AgendaItems (references Meetings)
        //    - Then Tasks, OKRs, etc.
        // 4. Update all foreign keys using mapping
        // 5. Verify counts match
        // 6. Return result with any warnings
    }
}
```

### ID Mapping Challenge

SQLite uses integer IDs. PostgreSQL will use UUIDs.

```csharp
// During migration
var idMap = new Dictionary<(string Table, int OldId), Guid>();

// For each record
var newId = Guid.NewGuid();
idMap[("TeamMembers", oldRecord.Id)] = newId;

// When migrating related records
var newMeeting = new Meeting
{
    Id = Guid.NewGuid(),
    TeamMemberId = idMap[("TeamMembers", oldMeeting.TeamMemberId)]
};
```

### Data Validation Checklist
- [ ] All team members migrated
- [ ] All meetings migrated with correct team member links
- [ ] All tasks migrated
- [ ] All OKRs and key results migrated
- [ ] All kudos migrated
- [ ] All surveys migrated
- [ ] All performance reviews migrated
- [ ] Dates preserved correctly (timezone handling)
- [ ] No orphaned records

---

## Phase 5: Team Features

### Admin Dashboard Requirements

```
┌─────────────────────────────────────────────────────────────────┐
│  ACME Corp - Admin Dashboard                     [Settings] [?] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📊 Team Overview                                               │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐            │
│  │ 12 Members   │ │ 47 1:1s      │ │ 89% Survey   │            │
│  │ 2 pending    │ │ this month   │ │ response     │            │
│  └──────────────┘ └──────────────┘ └──────────────┘            │
│                                                                 │
│  👥 Team Members                              [+ Invite Member] │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Name          │ Role    │ Manager   │ Status │ Actions  │   │
│  │───────────────│─────────│───────────│────────│──────────│   │
│  │ John Smith    │ Admin   │ -         │ Active │ [Edit]   │   │
│  │ Jane Doe      │ Manager │ John      │ Active │ [Edit]   │   │
│  │ Bob Wilson    │ Member  │ Jane      │ Active │ [Edit]   │   │
│  │ alice@ex.com  │ Member  │ -         │ Pending│ [Resend] │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  💳 Subscription                                                │
│  Plan: Team Pro (9 seats) - $81/month                          │
│  Next billing: Feb 1, 2026                    [Manage Billing] │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### User Invitation Flow

```
1. Admin clicks "Invite Member"
2. Enters email address + role (admin/manager/member)
3. System creates invite record + sends email
4. Recipient clicks link → lands on signup/accept page
5. If new user: creates account, then joins org
6. If existing user: just joins org
7. Seat count increments
8. If over seat limit → prompt to upgrade
```

### Team Analytics Ideas

| Metric | Description | Value |
|--------|-------------|-------|
| 1:1 Frequency | Average days between 1:1s per manager | Identifies neglected reports |
| Survey Response Rate | % of surveys completed | Team engagement |
| Goal Completion | % of OKRs hitting targets | Performance tracking |
| Kudos Given | Recognition frequency | Culture metric |
| Meeting Duration Trends | Are 1:1s getting longer/shorter? | Efficiency |
| Manager Leaderboard | Top managers by various metrics | Gamification |

---

## Deployment Options

### Option 1: Supabase Cloud (Default)

**Connection**: Use Supabase PostgreSQL connection string
**Auth**: Our own JWT system, NOT Supabase Auth
**Pros**: Managed, easy, automatic backups
**Cons**: Vendor dependency, data in cloud

```json
{
  "Database": {
    "Provider": "Supabase",
    "ConnectionString": "postgresql://user:pass@db.xyz.supabase.co:5432/postgres"
  }
}
```

### Option 2: Self-Hosted PostgreSQL

**Connection**: Customer provides connection string
**Auth**: Same system, just different DB
**Pros**: Full control, data sovereignty
**Cons**: Customer manages infrastructure

```json
{
  "Database": {
    "Provider": "SelfHosted",
    "ConnectionString": "Host=192.168.1.100;Database=tracker;Username=tracker_app;Password=xxx"
  }
}
```

### App Configuration Flow

```
1. First launch → "Setup Connection"
2. Choose: [Use Prickly Cactus Cloud] or [Connect to Private Server]
3. If Cloud: Just sign up/sign in
4. If Private: Enter connection string + test connection
5. Store preference in local app settings (not in DB)
```

### Enterprise Deployment Package

For self-hosted customers, provide:
- [ ] PostgreSQL schema scripts
- [ ] Docker compose file (PostgreSQL + optional API layer)
- [ ] Setup documentation
- [ ] Migration tools
- [ ] Backup/restore scripts

---

## Risks & Concerns

### 🔴 HIGH RISK

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Data loss during migration** | Users lose history | Extensive testing, backup before migrate, rollback plan |
| **Auth security vulnerability** | Account compromise | Security audit, penetration testing, use proven libraries |
| **Breaking change for existing users** | Churn | Gradual rollout, keep SQLite option during transition |
| **Performance degradation** | Bad UX | Connection pooling, query optimization, caching |

### 🟡 MEDIUM RISK

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Offline users can't work** | Frustration | Clear messaging, consider read-only cache later |
| **Self-hosted complexity** | Support burden | Excellent documentation, setup wizard |
| **Multi-tenant bugs** | Data leakage | RLS testing, security review, audit logging |
| **Team data ownership disputes** | Customer complaints | Clear policies, business decision on ownership model |

### 🟢 LOW RISK

| Risk | Impact | Mitigation |
|------|--------|------------|
| **PostgreSQL learning curve** | Dev time | Team already knows SQL, EF Core abstracts most |
| **Supabase pricing changes** | Cost | Self-hosted option available |

### Performance Considerations

1. **Connection overhead**: PostgreSQL connections have more latency than local SQLite
   - Mitigation: Connection pooling, batch operations

2. **Query complexity**: RLS adds overhead to every query
   - Mitigation: Proper indexing, query optimization

3. **Large data sets**: Some users might have 1000s of meetings
   - Mitigation: Pagination, lazy loading, archiving old data

4. **Concurrent users**: Team features mean more simultaneous connections
   - Mitigation: Connection limits, Supabase scales well

---

## Open Questions

### Business Questions (Need Your Input)

1. **Transition period**: How long do we support SQLite alongside PostgreSQL?
   - [ ] No SQLite after v2.0
   - [ ] SQLite as "legacy" for 1 year
   - [ ] SQLite remains for "local only" tier

2. **Pricing enforcement**: How strict on seat limits?
   - [ ] Hard block at seat limit
   - [ ] Soft warning, grace period
   - [ ] Allow overage with extra charge

3. **Data retention**: When user leaves team, what happens to their data?
   - [ ] Deleted after X days
   - [ ] Anonymized but retained
   - [ ] Transferred to admin
   - [ ] User can export first

4. **Free tier limits**: What's actually free?
   - [ ] 1 user, X team members, Y meetings?
   - [ ] Time-limited trial?
   - [ ] Feature-limited?

### Technical Questions (Need Research)

1. **EF Core + PostgreSQL RLS**: Does setting session variables work cleanly with EF Core connection pooling?

2. **JWT in desktop app**: Where to store JWT securely on Windows? DPAPI? Credential Manager?

3. **Connection string security**: How to protect connection string in self-hosted scenario?

4. **Real-time sync**: Do we need WebSocket/SignalR for team features? Or polling is enough?

---

## Timeline Estimate

### Phase 1: Schema & Infrastructure (3-4 weeks)
- Week 1-2: Design final schema, set up PostgreSQL dev environment
- Week 3-4: Implement RLS policies, test isolation

### Phase 2: Auth Migration (2-3 weeks)
- Week 5-6: Build auth service, JWT handling
- Week 7: Testing, security review

### Phase 3: Application Changes (4-6 weeks)
- Week 8-9: Database abstraction layer
- Week 10-11: Update all ViewModels/Services
- Week 12-13: Testing, bug fixes

### Phase 4: Data Migration (2-3 weeks)
- Week 14: Build migration tool
- Week 15-16: Testing with real user data (volunteers)

### Phase 5: Team Features (3-4 weeks)
- Week 17-18: Org management, invites
- Week 19-20: Admin dashboard, analytics

### Phase 6: Polish & Launch (2-3 weeks)
- Week 21: Beta testing with select customers
- Week 22-23: Bug fixes, documentation, launch

**Total: ~20-25 weeks (5-6 months)**

---

## Next Steps

1. **Review this document** - Add comments, questions
2. **Make decisions** on the 5 critical decision points
3. **Prioritize** - Do we need ALL team features for v1?
4. **Spike** - Quick proof-of-concept on EF Core + PostgreSQL + RLS
5. **Staff** - Do we need additional help for this scope?

---

## Appendix: Current SQLite Schema Reference

For migration mapping, document current schema:

```sql
-- Run against current SQLite to extract schema
SELECT sql FROM sqlite_master WHERE type='table';
```

Tables to migrate:
- TeamMembers
- Meetings  
- MeetingAgendaItems
- IndividualTasks
- ObjectiveKeyResults
- KeyResults
- Kudos
- PulseSurveys
- PulseSurveyQuestions
- PulseSurveyResponses
- PerformanceReviews
- Reminders
- Settings (special handling)
