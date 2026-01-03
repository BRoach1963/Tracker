# Tracker PostgreSQL Migration Plan
## Strategic Architecture Change: SQLite → PostgreSQL + Auth Migration

**Status**: PLANNING (Do Not Implement)  
**Priority**: TOP - Roadmap Priority #1  
**Last Updated**: January 3, 2026  
**Version**: 2.0 (Revised after architecture discussion)

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Key Decisions Made](#key-decisions-made)
3. [Architecture Options: Organization Model](#architecture-options-organization-model)
4. [Authentication Strategy](#authentication-strategy)
5. [Current vs Target Architecture](#current-vs-target-architecture)
6. [SQLite: Keep or Kill](#sqlite-keep-or-kill)
7. [Pricing Structure Analysis](#pricing-structure-analysis)
8. [Phase 1: Schema Design](#phase-1-schema-design)
9. [Phase 2: Auth Implementation](#phase-2-auth-implementation)
10. [Phase 3: Application Changes](#phase-3-application-changes)
11. [Phase 4: Data Migration](#phase-4-data-migration)
12. [Phase 5: Team Features](#phase-5-team-features)
13. [Deployment Options](#deployment-options)
14. [Risks & Concerns](#risks--concerns)
15. [Open Questions](#open-questions)
16. [Timeline Estimate](#timeline-estimate)

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
| Offline | ✅ Full | ❌ No (killing this) |
| Team Features | ❌ No | ✅ Yes |

### Why Auth Must Move Out of Supabase
1. **Self-hosted requirement**: Enterprise customers want everything on-prem
2. **Unified stack**: Can't have Supabase auth if customer self-hosts PostgreSQL
3. **Consistency**: Pro Causa and Praxis won't use Supabase
4. **Cost control**: No per-auth-user fees for large teams
5. **Data sovereignty**: Some customers can't have ANY cloud dependency

### Risk Clarification
**Data loss during migration is NOT a risk** - There are no production users yet. Only seed/test data exists, which will be recreated.

---

## Key Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Offline Support** | ❌ Kill it | Not needed for small teams, adds complexity |
| **SQLite** | ❌ Kill for product | Keep only for dev/testing, not shipped |
| **Data Migration** | N/A | No users = no data to migrate |
| **Seed Data Feature** | ❌ Remove from product | Only needed for testing, dumb to ship |

### Decisions Still Pending

| Decision | Options | Status |
|----------|---------|--------|
| **Organization Model** | A) Everyone has org, B) Org on-demand, C) No orgs | ⏳ Under discussion |
| **Auth Method** | Password, Windows Auth, both | ⏳ Under discussion |
| **Pricing Structure** | Per-seat vs flat tiers | ⏳ Under discussion |
| **Free Tier Limits** | TBD | ⏳ Needs definition |

---

## Architecture Options: Organization Model

### The Core Question
**Do we need an "organization" concept at all?**

An org is really only useful for:
- ✅ Billing (grouping users for payment)
- ✅ Data sharing (scoping shared data)
- ✅ Seat management (tracking who's in the "team")

It is NOT needed for:
- ❌ Auth/login (just need user accounts)
- ❌ Data isolation for solo users (use user_id)

### Option A: Everyone Has an Org (Hidden for Solos)

```
Solo User → Auto-creates "Org of 1" (invisible in UI) → Data scoped to org
Team User → Creates/Joins Team Org → Data scoped to org
```

**Pros:**
- One code path everywhere
- Easy upgrade (solo → team, just add members)
- Consistent RLS rules

**Cons:**
- Feels heavyweight for solo user
- Creates unnecessary record

### Option B: Org Created On-Demand

```
Solo User → No org, data scoped to user_id
Team Upgrade → Creates org, migrates data to org scope
```

**Pros:**
- Simpler for individuals
- No unnecessary records

**Cons:**
- Two query patterns (user_id vs org_id)
- Data migration when going solo → team

### Option C: No Orgs, Just Ownership + Sharing (SIMPLEST)

```sql
-- Users own their data directly
CREATE TABLE team_members (
    id UUID PRIMARY KEY,
    owner_id UUID REFERENCES users(id), -- Who owns this
    ...
);

-- Sharing is explicit, not implicit
CREATE TABLE data_shares (
    resource_type VARCHAR(50), -- 'team_member', 'meeting', etc.
    resource_id UUID,
    owner_id UUID REFERENCES users(id),
    shared_with_id UUID REFERENCES users(id),
    permission VARCHAR(50) -- 'view', 'edit'
);

-- Billing is separate from data model
CREATE TABLE billing_groups (
    id UUID PRIMARY KEY,
    name VARCHAR(255),
    owner_id UUID REFERENCES users(id)
);

CREATE TABLE billing_group_members (
    billing_group_id UUID,
    user_id UUID
);
```

**Pros:**
- Super simple data model
- Clear ownership
- No migration when "upgrading" to team
- Matches mental model: "I own my data, I share with Bob"
- Billing is cleanly separated from data

**Cons:**
- Sharing is more manual
- No implicit "everyone in org sees this"

### Recommendation for Small Firms

**Option C (No Orgs, Just Sharing)** may be best because:
- A 5-person law firm doesn't think in "organizations"
- They think: "I'm a partner, I have my team, I want to share with Bob"
- Explicit sharing matches how small firms actually work
- Billing group ≠ data scope (clean separation)

### ⏳ DECISION NEEDED: Which model?

```
[ ] Option A: Everyone has org (hidden for solos)
[ ] Option B: Org created on-demand
[ ] Option C: No orgs, just ownership + sharing
```

---

## Authentication Strategy

### The Goal
Self-contained auth that works for:
1. Cloud deployment (Prickly Cactus hosted)
2. Self-hosted deployment (customer's server)
3. Potentially Windows domain environments (future)

### Auth Implementation: Lightweight JWT + bcrypt

```csharp
public class AuthService
{
    // Password hashing (BCrypt.Net-Next package)
    public string HashPassword(string password) 
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    
    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
    
    // JWT token generation
    public string GenerateAccessToken(User user);  // Short-lived (15 min)
    public string GenerateRefreshToken();          // Long-lived (7 days)
    public ClaimsPrincipal ValidateToken(string token);
}
```

### Windows Authentication (Future Consideration)

**Scenario:** Self-hosted PostgreSQL on company network, users already logged into Windows domain.

**Option A: Windows Identity → Database Connection**
```csharp
// PostgreSQL connection uses Windows Integrated Auth
"Host=server;Database=tracker;Integrated Security=true"
```
- ✅ Zero-friction for users
- ❌ Requires Active Directory integration
- ❌ Doesn't work for cloud deployment
- ❌ PostgreSQL on Linux doesn't support well

**Option B: Windows Identity → App Auth (Recommended for Future)**
```csharp
// App reads Windows identity, trusts it
var windowsUser = WindowsIdentity.GetCurrent().Name; // "DOMAIN\username"
// Auto-login or auto-create user based on Windows identity
```
- ✅ Works with any PostgreSQL
- ✅ No password for user
- ❌ Only works on Windows
- ❌ Still need user record in our DB

**Recommendation:** Design with auth provider abstraction, implement Windows Auth later.

```csharp
public interface IAuthProvider
{
    Task<AuthResult> AuthenticateAsync(AuthRequest request);
}

public class PasswordAuthProvider : IAuthProvider { }     // V1
public class WindowsAuthProvider : IAuthProvider { }      // Future
public class SamlAuthProvider : IAuthProvider { }         // Enterprise future
```

### Auth Tables (PostgreSQL)

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255), -- NULL if using Windows Auth
    auth_provider VARCHAR(50) DEFAULT 'password', -- 'password', 'windows', 'saml'
    windows_sid VARCHAR(255), -- For Windows Auth users
    display_name VARCHAR(255),
    email_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
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
    revoked BOOLEAN DEFAULT FALSE
);
```

### What We're Removing
- `SupabaseAuthService.cs` - All Supabase auth code
- Supabase SDK auth references
- Supabase JWT validation

---

## SQLite: Keep or Kill

### Decision: KILL for Product, KEEP for Dev

**For shipped product:** PostgreSQL only. No SQLite option.

**For development:** SQLite remains available but hidden.

```csharp
#if DEBUG
    // SQLite option only exists in debug builds for local testing
    if (Environment.GetEnvironmentVariable("USE_LOCAL_DB") == "true")
    {
        optionsBuilder.UseSqlite($"Data Source={localDbPath}");
    }
    else
#endif
    {
        optionsBuilder.UseNpgsql(connectionString);
    }
```

### Why Kill SQLite for Product
| Reason | Impact |
|--------|--------|
| **Maintenance burden** | Two database providers = two code paths |
| **Conflicts with team features** | Can't do multi-user with local files |
| **No real market** | Who wants local-only in 2026? |
| **Simpler pricing** | No "local tier" to explain |

### What Happens to Offline?
**We're killing offline capability.** Small teams have connectivity. If they don't, they shouldn't use Tracker.

---

## Pricing Structure Analysis

### Original Pricing (Under Review)
| Plan | Price | Notes |
|------|-------|-------|
| Free | $0 | ??? limits |
| Standard | $7/mo | Individual |
| Pro | $12/mo | Individual + more AI |
| Team Standard | $5/seat/mo | 5+ seats |
| Team Pro | $9/seat/mo | 5+ seats + more AI |
| Enterprise | Custom | Custom |

### Problems with Original
1. What's free? No clear limits defined
2. Individual vs Team distinction is confusing
3. What triggers the upgrade? Unclear
4. Why 5 seat minimum? Arbitrary

### Proposed: Flat Tier Pricing (Simpler)

| Tier | Monthly | Annual | Users | Key Limits |
|------|---------|--------|-------|------------|
| **Free** | $0 | $0 | 1 | 3 team members, 5 meetings/mo, no AI |
| **Solo** | $9/mo | $90/yr | 1 | Unlimited team members, AI insights |
| **Team** | $29/mo | $290/yr | Up to 5 | Multi-user, shared workspace |
| **Team+** | $59/mo | $590/yr | Up to 15 | + Admin dashboard |
| **Business** | $99/mo | $990/yr | Up to 30 | + Analytics, priority support |
| **Custom** | Contact | Contact | 30+ | Whatever they need |

### Why Flat Tiers Are Better for Small Teams
- Predictable pricing (no seat math)
- Clear upgrade triggers (need more users or features)
- $99/mo for 30 users = $3.30/user (very competitive vs $9/seat competitors)
- Free tier has clear, useful limits

### Feature Gates by Tier

| Feature | Free | Solo | Team | Team+ | Business |
|---------|:----:|:----:|:----:|:-----:|:--------:|
| **Users** | 1 | 1 | 5 | 15 | 30 |
| **Team Members Tracked** | 3 | ∞ | ∞ | ∞ | ∞ |
| **Meetings/month** | 5 | ∞ | ∞ | ∞ | ∞ |
| **AI Insights** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **AI Suggestions** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Pulse Surveys** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Performance Reviews** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Multi-user** | ❌ | ❌ | ✅ | ✅ | ✅ |
| **Admin Dashboard** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Team Analytics** | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Priority Support** | ❌ | ❌ | ❌ | ❌ | ✅ |

### ⏳ DECISION NEEDED: Pricing Model

```
[ ] Keep original per-seat pricing
[ ] Switch to flat tier pricing
[ ] Hybrid (flat for small, per-seat for large)
```

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
│  ┌──────────────┐   │         │     (Prickly Cactus hosted)     │
│  │  Tracker.exe │───┼────────▶│  ┌───────────────────────────┐  │
│  │              │   │         │  │  Auth Tables              │  │
│  │  (No local   │   │         │  │  - users                  │  │
│  │   database)  │   │         │  │  - sessions               │  │
│  └──────────────┘   │         │  ├───────────────────────────┤  │
│                     │         │  │  Billing Tables           │  │
└─────────────────────┘         │  │  - billing_groups         │  │
                                │  │  - subscriptions          │  │
                                │  ├───────────────────────────┤  │
                                │  │  App Data (RLS-protected) │  │
                                │  │  - team_members           │  │
                                │  │  - meetings               │  │
                                │  │  - data_shares            │  │
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
│  └──────────────┘   │         │  │  Optional: Windows Auth   │  │
│                     │         │  └───────────────────────────┘  │
└─────────────────────┘         └─────────────────────────────────┘
```

---

## Phase 1: Schema Design

### Schema Depends on Org Model Decision

The schema design varies significantly based on which organization model we choose:

### Option C Schema: No Orgs, Just Ownership + Sharing (Recommended)

This is the simplest model - users own their data directly, sharing is explicit.

```sql
-- ============================================
-- CORE IDENTITY / AUTH TABLES
-- ============================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255), -- NULL if using Windows Auth
    auth_provider VARCHAR(50) DEFAULT 'password', -- 'password', 'windows', 'saml'
    windows_sid VARCHAR(255), -- For Windows Auth users
    display_name VARCHAR(255),
    avatar_url TEXT,
    email_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    settings JSONB DEFAULT '{}' -- User-level settings
);

CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
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
    revoked BOOLEAN DEFAULT FALSE
);

-- ============================================
-- BILLING (Separate from Data Model)
-- ============================================

CREATE TABLE billing_groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    owner_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE billing_group_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    billing_group_id UUID REFERENCES billing_groups(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    added_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(billing_group_id, user_id)
);

CREATE TABLE subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    billing_group_id UUID REFERENCES billing_groups(id) ON DELETE CASCADE,
    plan_type VARCHAR(50) NOT NULL, -- 'free', 'solo', 'team', 'team_plus', 'business'
    status VARCHAR(50) NOT NULL, -- 'active', 'canceled', 'past_due', 'trialing'
    user_limit INTEGER DEFAULT 1, -- Max users for this plan
    billing_cycle VARCHAR(20), -- 'monthly', 'annual'
    current_period_start TIMESTAMP,
    current_period_end TIMESTAMP,
    stripe_subscription_id VARCHAR(255),
    stripe_customer_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE ai_credits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE, -- Per-user, not per-org
    credits_remaining INTEGER DEFAULT 0,
    credits_used_this_month INTEGER DEFAULT 0,
    monthly_reset_date TIMESTAMP
);

-- ============================================
-- DATA SHARING (Explicit, not implicit)
-- ============================================

CREATE TABLE data_shares (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_type VARCHAR(50) NOT NULL, -- 'team_member', 'meeting', 'okr', etc.
    resource_id UUID NOT NULL,
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE,
    shared_with_id UUID REFERENCES users(id) ON DELETE CASCADE,
    permission VARCHAR(50) DEFAULT 'view', -- 'view', 'edit', 'admin'
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(resource_type, resource_id, shared_with_id)
);

-- ============================================
-- APP DATA TABLES (Owner-based, not org-based)
-- ============================================

CREATE TABLE team_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE, -- Who "owns" this team member record
    linked_user_id UUID REFERENCES users(id), -- If team member is also a Tracker user
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

CREATE TABLE meetings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE, -- Who created/owns this meeting
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    title VARCHAR(255),
    meeting_date TIMESTAMP NOT NULL,
    duration_minutes INTEGER,
    location VARCHAR(255),
    meeting_type VARCHAR(50) DEFAULT 'one_on_one',
    status VARCHAR(50) DEFAULT 'scheduled',
    notes TEXT,
    private_notes TEXT, -- Owner-only notes
    ai_summary TEXT,
    calendar_event_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE meeting_agenda_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID REFERENCES meetings(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    is_completed BOOLEAN DEFAULT FALSE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE,
    team_member_id UUID REFERENCES team_members(id),
    meeting_id UUID REFERENCES meetings(id),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    due_date DATE,
    priority VARCHAR(20) DEFAULT 'medium',
    status VARCHAR(50) DEFAULT 'pending',
    is_completed BOOLEAN DEFAULT FALSE,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE okrs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE,
    team_member_id UUID REFERENCES team_members(id),
    objective TEXT NOT NULL,
    start_date DATE,
    end_date DATE,
    completion_percentage INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE key_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    okr_id UUID REFERENCES okrs(id) ON DELETE CASCADE,
    description TEXT NOT NULL,
    target_value DECIMAL(10,2),
    current_value DECIMAL(10,2) DEFAULT 0,
    unit VARCHAR(50),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE kudos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE, -- Who gave the kudos
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    category VARCHAR(50),
    is_public BOOLEAN DEFAULT FALSE, -- If true, visible to shared users
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE pulse_surveys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(50) DEFAULT 'draft',
    due_date TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE pulse_survey_questions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id UUID REFERENCES pulse_surveys(id) ON DELETE CASCADE,
    question_text TEXT NOT NULL,
    question_type VARCHAR(50) DEFAULT 'rating',
    sort_order INTEGER DEFAULT 0
);

CREATE TABLE pulse_survey_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id UUID REFERENCES pulse_surveys(id) ON DELETE CASCADE,
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    question_id UUID REFERENCES pulse_survey_questions(id),
    response_value TEXT,
    submitted_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE performance_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES users(id) ON DELETE CASCADE,
    team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    review_period_start DATE,
    review_period_end DATE,
    status VARCHAR(50) DEFAULT 'draft',
    self_review TEXT,
    manager_review TEXT,
    overall_rating INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE reminders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE, -- Personal to user
    title VARCHAR(255) NOT NULL,
    message TEXT,
    due_datetime TIMESTAMP NOT NULL,
    is_completed BOOLEAN DEFAULT FALSE,
    reminder_type VARCHAR(50),
    related_entity_type VARCHAR(50),
    related_entity_id UUID,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### Row-Level Security (RLS) for Option C

```sql
-- Enable RLS on all tables
ALTER TABLE team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;
ALTER TABLE tasks ENABLE ROW LEVEL SECURITY;
-- ... etc

-- Users can see their own data OR data shared with them
CREATE POLICY team_members_access ON team_members
    FOR ALL
    USING (
        owner_id = current_user_id()
        OR 
        id IN (
            SELECT resource_id FROM data_shares 
            WHERE resource_type = 'team_member' 
            AND shared_with_id = current_user_id()
        )
    );

CREATE POLICY meetings_access ON meetings
    FOR ALL
    USING (
        owner_id = current_user_id()
        OR 
        id IN (
            SELECT resource_id FROM data_shares 
            WHERE resource_type = 'meeting' 
            AND shared_with_id = current_user_id()
        )
        OR
        -- Also visible if team_member is shared
        team_member_id IN (
            SELECT resource_id FROM data_shares 
            WHERE resource_type = 'team_member' 
            AND shared_with_id = current_user_id()
        )
    );

-- Helper function to get current user (set via session variable)
CREATE OR REPLACE FUNCTION current_user_id() RETURNS UUID AS $$
    SELECT current_setting('app.current_user_id', true)::UUID;
$$ LANGUAGE SQL STABLE;
```

### Alternative: Option A/B Schema (With Organizations)

If we decide organizations ARE needed, here's that schema:

```sql
-- Only include if we choose Option A or B
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) UNIQUE,
    owner_id UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW(),
    settings JSONB DEFAULT '{}',
    is_individual BOOLEAN DEFAULT FALSE
);

CREATE TABLE organization_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'member',
    status VARCHAR(50) DEFAULT 'active',
    UNIQUE(organization_id, user_id)
);

-- Then all app tables would have organization_id instead of owner_id
-- And RLS would be based on org membership
```

### Migration Mapping: SQLite → PostgreSQL

| SQLite Table | PostgreSQL Table | Changes |
|--------------|------------------|---------|
| `TeamMembers` | `team_members` | +owner_id (UUID), remove int ID |
| `Meetings` | `meetings` | +owner_id (UUID), remove int ID |
| `MeetingAgendaItems` | `meeting_agenda_items` | UUID references |
| `IndividualTasks` | `tasks` | +owner_id (UUID) |
| `ObjectiveKeyResults` | `okrs` | +owner_id (UUID) |
| `KeyResults` | `key_results` | UUID references |
| `Kudos` | `kudos` | +owner_id (UUID) |
| `PulseSurveys` | `pulse_surveys` | +owner_id (UUID) |
| `PerformanceReviews` | `performance_reviews` | +owner_id (UUID) |
| `Reminders` | `reminders` | +user_id (UUID) |
| `Settings` | `users.settings` | JSONB column |

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

### ~~🔴 HIGH RISK~~ - REVISED

| Risk | Impact | Status |
|------|--------|--------|
| ~~**Data loss during migration**~~ | ~~Users lose history~~ | **NOT A RISK** - No production users. Only seed/test data. |
| **Auth security vulnerability** | Account compromise | Use proven libraries (BCrypt, JWT), security review |
| ~~**Breaking change for existing users**~~ | ~~Churn~~ | **NOT A RISK** - No existing users. |

### 🟡 MEDIUM RISK

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Self-hosted complexity** | Support burden | Excellent documentation, setup wizard |
| **Multi-tenant bugs (if using orgs)** | Data leakage | RLS testing, security review |
| **EF Core + RLS compatibility** | Technical issues | Spike/POC before full implementation |

### 🟢 LOW RISK

| Risk | Impact | Mitigation |
|------|--------|------------|
| **PostgreSQL learning curve** | Dev time | Team knows SQL, EF Core abstracts most |
| **Performance vs SQLite** | Slightly slower | Connection pooling, acceptable for use case |

### Performance Considerations

1. **Connection overhead**: PostgreSQL has more latency than local SQLite
   - Acceptable for target use case (not gaming, not high-frequency)
   - Mitigation: Connection pooling

2. **Query complexity**: RLS adds overhead
   - Mitigation: Proper indexing

---

## Open Questions

### ⏳ Decisions Still Needed

| Question | Options | Notes |
|----------|---------|-------|
| **Organization Model** | A) Everyone has org, B) On-demand, C) No orgs | Recommend C for simplicity |
| **Windows Auth** | Now vs Later | Recommend: design for it, implement later |
| **Pricing Model** | Per-seat vs Flat tiers | Recommend: Flat tiers |
| **Free Tier Limits** | TBD | Proposed: 1 user, 3 team members, 5 meetings/mo |
| **Seed Data Feature** | Keep vs Remove | Recommend: Remove from product |

### Technical Questions (Need Research/Spike)

1. **EF Core + PostgreSQL RLS**: Does setting session variables work cleanly with connection pooling?
   - Need proof-of-concept before committing to RLS approach

2. **JWT storage on Windows**: Where to store securely?
   - Options: DPAPI, Windows Credential Manager, encrypted local file

3. **Connection string for self-hosted**: How to secure?
   - User enters once, stored encrypted locally

---

## Timeline Estimate (Revised)

Since there's no data migration burden (no users), timeline is simpler:

### Phase 1: Design & Setup (2 weeks)
- Finalize org model decision
- Set up PostgreSQL dev environment
- Create schema (based on chosen model)
- Spike: EF Core + RLS compatibility

### Phase 2: Auth Implementation (2-3 weeks)
- Build auth service (password + JWT)
- User registration/login flows
- Session management
- Remove Supabase auth code

### Phase 3: Database Layer (3-4 weeks)
- Replace SQLite provider with Npgsql
- Update TrackerDbContext
- Update TrackerDbManager (all methods)
- Add user context to all queries

### Phase 4: UI Updates (2 weeks)
- Login/registration screens
- Remove SQLite-specific UI
- Connection configuration for self-hosted
- Remove seed data feature

### Phase 5: Team Features (3-4 weeks) - IF NEEDED FOR V1
- Sharing UI (if using Option C)
- OR Org management UI (if using Option A/B)
- Billing group management
- Admin dashboard (Team+ tier)

### Phase 6: Testing & Polish (2 weeks)
- End-to-end testing
- Security review
- Documentation
- Self-hosted deployment package

**Total: ~14-17 weeks (3.5-4 months)**

*Note: Could be faster if we defer team features to post-launch.*

---

## Next Steps

### Immediate Actions

1. **Make org model decision** (A, B, or C)
2. **Make pricing decision** (per-seat vs flat)
3. **Spike**: EF Core + PostgreSQL + RLS proof-of-concept
4. **Define free tier limits**

### Before Starting Implementation

- [ ] Org model chosen
- [ ] Pricing structure finalized
- [ ] POC validates EF Core + RLS approach
- [ ] Dev PostgreSQL environment set up

---

## Appendix: Comparison of Org Models

| Aspect | Option A (Everyone Has Org) | Option B (Org On-Demand) | Option C (No Orgs) |
|--------|----------------------------|--------------------------|-------------------|
| **Schema complexity** | Medium | Medium | Low |
| **Query patterns** | One pattern (org-scoped) | Two patterns | One pattern (owner-scoped) |
| **Solo → Team upgrade** | Easy (add members) | Requires data migration | Easy (share data) |
| **Data sharing** | Implicit (same org) | Implicit (same org) | Explicit (data_shares) |
| **Billing** | Tied to org | Tied to org | Separate (billing_groups) |
| **Mental model** | "I'm in an organization" | "I might join an org" | "I own my data, I share it" |
| **Best for** | Enterprise-focused | Hybrid | Small teams |

### Recommendation: Option C for Small Firm Focus

Small firms don't think in "organizations." They think:
- "These are MY team members"
- "I want to share Sarah's info with my partner Bob"
- "Bob and I are on the same bill"

Option C maps directly to this mental model.

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
