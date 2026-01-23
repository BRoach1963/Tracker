# Pro Cohere Authentication & Licensing Documentation

**Version:** 1.0  
**Last Updated:** January 16, 2026  
**Author:** Brian Roach / Copilot  

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Authentication Flow](#2-authentication-flow)
3. [User Onboarding](#3-user-onboarding)
4. [Session Management](#4-session-management)
5. [Multi-Tenancy Model](#5-multi-tenancy-model)
6. [Licensing & Subscription System](#6-licensing--subscription-system)
7. [Seat License Management](#7-seat-license-management)
8. [Current Implementation Status](#8-current-implementation-status)
9. [Gap Analysis & Required Work](#9-gap-analysis--required-work)
10. [Database Schema Reference](#10-database-schema-reference)
11. [Multi-Product Architecture](#11-multi-product-architecture)
12. [Beta Launch Strategy](#12-beta-launch-strategy)

---

## 1. Architecture Overview

### 1.1 Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Authentication | Supabase Auth (GoTrue) | Email/password, OAuth, JWT tokens |
| Session Storage | Windows DPAPI | Secure local credential storage |
| Database | Supabase PostgreSQL | User profiles, org data |
| API Access | Supabase Postgrest | REST queries with RLS |
| Authorization | Row-Level Security (RLS) | Data isolation per user/org |

### 1.2 Key Entities Relationship

```
┌─────────────────────────────────────────────────────────────────┐
│                    LICENSING / BILLING                          │
│  ┌───────────────┐                                              │
│  │organizations  │ ← subscription_tier, max_users, max_team_members
│  │  (Tenant)     │                                              │
│  └───────┬───────┘                                              │
│          │                                                       │
│          │ 1:N                                                   │
│          ▼                                                       │
│  ┌───────────────┐      ┌───────────────┐                       │
│  │    users      │─────▶│  auth.users   │ (Supabase Auth)       │
│  │ (App Users)   │      │ (JWT Source)  │                       │
│  │  firm_id?     │      └───────────────┘                       │
│  └───────┬───────┘                                              │
│          │                                                       │
│          │ 1:1 (optional)                                        │
│          ▼                                                       │
│  ┌───────────────┐                                              │
│  │ team_members  │ ← People being TRACKED (may not have login)  │
│  │  (Trackees)   │                                              │
│  └───────────────┘                                              │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Identity Concepts

| Concept | Table | Description |
|---------|-------|-------------|
| **Auth Identity** | `auth.users` (Supabase) | Supabase authentication identity, JWT source |
| **App User** | `users` | Application user profile, linked via `supabase_auth_id` |
| **Team Member** | `team_members` | Person being tracked/managed, may or may not have login |
| **Organization** | `organizations` | Tenant, owns subscription/licensing |
| **Firm** | `users.firm_id` | Billing entity (for seat licenses) |

---

## 2. Authentication Flow

### 2.1 Sign In Flow (Existing User)

```
┌────────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────┐
│   User     │     │  LoginView  │     │ AuthService │     │ Supabase │
└─────┬──────┘     └──────┬──────┘     └──────┬──────┘     └────┬─────┘
      │                   │                   │                  │
      │ Enter credentials │                   │                  │
      │──────────────────▶│                   │                  │
      │                   │                   │                  │
      │                   │ SignInAsync()     │                  │
      │                   │──────────────────▶│                  │
      │                   │                   │                  │
      │                   │                   │ Auth.SignIn()    │
      │                   │                   │─────────────────▶│
      │                   │                   │                  │
      │                   │                   │ JWT + Refresh    │
      │                   │                   │◀─────────────────│
      │                   │                   │                  │
      │                   │                   │ Store in DPAPI   │
      │                   │                   │ (if Remember Me) │
      │                   │                   │                  │
      │                   │ (true, null)      │                  │
      │                   │◀──────────────────│                  │
      │                   │                   │                  │
      │                   │ LoadUserProfileAsync()               │
      │                   │──────────────────▶│                  │
      │                   │                   │                  │
      │                   │                   │ Query users WHERE│
      │                   │                   │ supabase_auth_id │
      │                   │                   │─────────────────▶│
      │                   │                   │                  │
      │                   │                   │ UserProfile      │
      │                   │                   │◀─────────────────│
      │                   │                   │                  │
      │ Navigate to Main  │                   │                  │
      │◀──────────────────│                   │                  │
```

### 2.2 Auto-Login Flow (Returning User)

```
┌────────────┐     ┌─────────────┐     ┌─────────────────────┐     ┌──────────┐
│   App      │     │ AuthService │     │WindowsCredentialSvc │     │ Supabase │
└─────┬──────┘     └──────┬──────┘     └──────────┬──────────┘     └────┬─────┘
      │                   │                       │                     │
      │ TryAutoLoginAsync │                       │                     │
      │──────────────────▶│                       │                     │
      │                   │                       │                     │
      │                   │ HasStoredSession()?   │                     │
      │                   │──────────────────────▶│                     │
      │                   │                       │                     │
      │                   │                       │ Read DPAPI file     │
      │                   │                       │ Decrypt tokens      │
      │                   │                       │                     │
      │                   │ (accessToken,         │                     │
      │                   │  refreshToken)        │                     │
      │                   │◀──────────────────────│                     │
      │                   │                       │                     │
      │                   │ SetSession(tokens)    │                     │
      │                   │────────────────────────────────────────────▶│
      │                   │                       │                     │
      │                   │ Validate/Refresh JWT  │                     │
      │                   │◀────────────────────────────────────────────│
      │                   │                       │                     │
      │  true (logged in) │                       │                     │
      │◀──────────────────│                       │                     │
```

### 2.3 Code Reference: AuthService.cs

```csharp
// Location: Services/AuthService.cs

public async Task<(bool Success, string? Error)> SignInAsync(
    string email, 
    string password, 
    bool persistSession = false)
{
    if (!_isInitialized)
    {
        await InitializeAsync();
    }

    try
    {
        var session = await _client!.Auth.SignIn(email, password);

        if (session?.User != null)
        {
            if (persistSession && !string.IsNullOrEmpty(session.AccessToken))
            {
                // Store in Windows Credential Manager for auto-login
                _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
            }
            else
            {
                // User didn't check "Remember Me" - clear any stored session
                _credentialService.ClearSession();
            }

            AuthStateChanged?.Invoke(this, session.User);
            return (true, null);
        }

        return (false, "Sign in failed. Please check your credentials.");
    }
    catch (GotrueException ex)
    {
        return (false, GetFriendlyAuthError(ex));
    }
}
```

---

## 3. User Onboarding

### 3.1 New User Sign-Up Flow

```
STEP 1: Create Supabase Auth Account
────────────────────────────────────
User enters: email, password, display_name
  ↓
Supabase Auth creates auth.users record
  ↓
Returns: User ID (UUID), JWT tokens

STEP 2: Create Application User Profile  ⚠️ MANUAL STEP REQUIRED
──────────────────────────────────────────────────────────────────
Currently: Must manually create users record with supabase_auth_id
Future: Should auto-create via database trigger or post-signup hook

STEP 3: Assign to Organization  ⚠️ NOT YET AUTOMATED
─────────────────────────────────────────────────────
Currently: organization_id set manually
Future: 
  - Self-service org creation
  - Invite flow (join existing org)
  - Admin assignment

STEP 4: Link to Team Member (Optional)
──────────────────────────────────────
If user is also a team member being tracked:
  - Set users.linked_team_member_id
  - OR set team_members.user_id
```

### 3.2 Sign-Up Code Reference

```csharp
// Location: Services/AuthService.cs

public async Task<(bool Success, string? Error)> SignUpAsync(
    string email, 
    string password, 
    string? displayName = null)
{
    try
    {
        var session = await _client!.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                ["display_name"] = displayName ?? email.Split('@')[0]
            }
        });

        if (session?.User != null)
        {
            // Always persist session for new sign-ups
            _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
            
            AuthStateChanged?.Invoke(this, session.User);
            return (true, null);
        }

        return (false, "Sign up failed. Please try again.");
    }
    catch (GotrueException ex)
    {
        return (false, GetFriendlyAuthError(ex));
    }
}
```

### 3.3 What's Missing in User Onboarding

| Step | Current State | Required Work |
|------|--------------|---------------|
| Auth account creation | ✅ Working | None |
| Email verification | ⚠️ Supabase sends email | UI to handle verification state |
| `users` record creation | ❌ Manual | Auto-create trigger or API call |
| Organization assignment | ❌ Manual | Self-service or invite flow |
| Team member linkage | ❌ Manual | Profile setup wizard |
| Welcome/setup wizard | ❌ None | Create first-run experience |

### 3.4 Required Database Trigger for Auto-User Creation

```sql
-- Trigger to auto-create users record when auth.users is created
CREATE OR REPLACE FUNCTION public.handle_new_auth_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.users (
        supabase_auth_id,
        email,
        display_name,
        role,
        created_at,
        updated_at
    ) VALUES (
        NEW.id,
        NEW.email,
        COALESCE(NEW.raw_user_meta_data->>'display_name', split_part(NEW.email, '@', 1)),
        'manager', -- Default role
        NOW(),
        NOW()
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Attach to auth.users
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW
    EXECUTE FUNCTION public.handle_new_auth_user();
```

---

## 4. Session Management

### 4.1 Session Storage (Windows DPAPI)

```
Location: %LOCALAPPDATA%\ProCohere\session.protected

┌──────────────────────────────────────────────────────────────┐
│  DPAPI-Encrypted File Contents                               │
│  ────────────────────────────────                            │
│  {                                                           │
│    "AccessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", │
│    "RefreshToken": "v1.MDA...",                              │
│    "StoredAt": "2026-01-15T10:30:00Z"                        │
│  }                                                           │
│                                                              │
│  Encrypted with: DataProtectionScope.CurrentUser             │
│  Only decryptable by: Same Windows user account              │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 WindowsCredentialService Implementation

```csharp
// Location: Services/WindowsCredentialService.cs

public class WindowsCredentialService : ICredentialService
{
    private static readonly string SessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere",
        "session.protected");

    public bool StoreSession(string accessToken, string refreshToken)
    {
        var sessionData = new SessionData
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            StoredAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(sessionData);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        
        // Encrypt using DPAPI - only current Windows user can decrypt
        var encryptedBytes = ProtectedData.Protect(
            plainBytes, 
            null, 
            DataProtectionScope.CurrentUser);

        File.WriteAllBytes(SessionFilePath, encryptedBytes);
        return true;
    }

    public (string? AccessToken, string? RefreshToken) GetStoredSession()
    {
        if (!File.Exists(SessionFilePath))
            return (null, null);

        var encryptedBytes = File.ReadAllBytes(SessionFilePath);
        
        // Decrypt using DPAPI
        var plainBytes = ProtectedData.Unprotect(
            encryptedBytes, 
            null, 
            DataProtectionScope.CurrentUser);

        var json = Encoding.UTF8.GetString(plainBytes);
        var sessionData = JsonSerializer.Deserialize<SessionData>(json);
        
        return (sessionData?.AccessToken, sessionData?.RefreshToken);
    }

    public void ClearSession()
    {
        if (File.Exists(SessionFilePath))
            File.Delete(SessionFilePath);
    }
}
```

### 4.3 Token Refresh Flow

```
JWT Access Token: Short-lived (1 hour by default)
Refresh Token: Long-lived (can be configured)

When access token expires:
  1. Supabase client automatically uses refresh token
  2. Gets new access + refresh tokens
  3. AuthService stores new tokens if "Remember Me" was checked
  4. Continues seamlessly
```

---

## 5. Multi-Tenancy Model

### 5.1 Current Data Isolation

```
                    ┌─────────────────────────────────────────┐
                    │            Supabase RLS                 │
                    │  ═══════════════════════════════════    │
                    │                                         │
                    │  Each authenticated request includes:    │
                    │  - auth.uid() = Supabase Auth User ID   │
                    │  - auth.jwt() = Full JWT claims         │
                    │                                         │
                    │  RLS Policies filter data by:           │
                    │  - supabase_auth_id (user's own data)   │
                    │  - organization_id (org's data)         │
                    │  - manager_user_id (manager's team)     │
                    └─────────────────────────────────────────┘
```

### 5.2 Organization-Based Isolation

```sql
-- Example RLS policy for goals table
CREATE POLICY "Users can read their organization's goals"
ON goals
FOR SELECT
USING (
    organization_id IN (
        SELECT organization_id 
        FROM users 
        WHERE supabase_auth_id = auth.uid()
    )
);
```

### 5.3 Current RLS Policies (Simplified)

| Table | Policy | Condition |
|-------|--------|-----------|
| `users` | Read own profile | `supabase_auth_id = auth.uid()` |
| `team_members` | Read managed members | `manager_user_id = current_user_id()` |
| `goals` | Read org goals | `organization_id = user's org` |
| `tasks` | Read created tasks | `created_by_user_id = current_user_id()` |
| `meetings` | Read as creator/attendee | Creator OR in attendees |

---

## 6. Licensing & Subscription System

### 6.1 Subscription Tiers

| Tier | Price | Team Members | AI Features | Database | Key Features |
|------|-------|--------------|-------------|----------|--------------|
| **Free** | $0 | 10 | ❌ None | Local only | Basic tracking |
| **Standard** | $7/user/mo | 100 | AI Help Bot (no data analysis) | Local only | Calendar sync, Basic reports |
| **Pro** | $12/user/mo | Unlimited | Full AI with data analysis | Network DB | Advanced reports, Priority support |
| **Internal** | N/A | Unlimited | All | All | Testing/Development |

### 6.2 Feature Matrix

```csharp
// Location: Services/Subscription/SubscriptionLimits.cs

public class SubscriptionLimits
{
    public SubscriptionTier Tier { get; init; }
    public string DisplayName { get; init; }
    
    // Resource Limits
    public int MaxTeamMembers { get; init; }      // -1 = unlimited
    public int MaxOneOnOnesPerMonth { get; init; }
    public int MaxTasks { get; init; }
    public int MaxProjects { get; init; }
    public int MaxOKRs { get; init; }             // Legacy: now Goals
    public int MaxKPIs { get; init; }             // Legacy: now Metrics
    public int MaxGoals { get; init; }
    
    // Feature Flags
    public bool HasAIAssistant { get; init; }
    public bool HasAIDataAnalysis { get; init; }
    public bool HasCalendarSync { get; init; }
    public bool HasBasicReports { get; init; }
    public bool HasAdvancedReports { get; init; }
    public bool HasEmailSupport { get; init; }
    public bool HasPrioritySupport { get; init; }
    public bool AllowsNetworkDatabase { get; init; }
    
    // AI Budget
    public decimal MonthlyAIBudget { get; init; }
}
```

### 6.3 Organizations Table (License Holder)

```sql
-- organizations table structure
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(100),
    
    -- LICENSING FIELDS
    subscription_tier VARCHAR(50) NOT NULL DEFAULT 'free',  -- free, standard, pro
    max_users INT DEFAULT 5,                                 -- App user seats
    max_team_members INT DEFAULT 25,                         -- Team members to track
    
    settings JSONB DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by VARCHAR(100)
);
```

### 6.4 Current SubscriptionService (WPF - Reference)

```csharp
// Location: Tracker/Services/Subscription/SubscriptionService.cs

public class SubscriptionService : ISubscriptionService
{
    private SubscriptionTier _currentTier;
    private SubscriptionLimits _currentLimits;
    private DateTime? _subscriptionExpiry;
    private string? _customerId;      // Payment provider customer ID
    private string? _subscriptionId;  // Payment provider subscription ID

    public bool HasFeature(string featureName)
    {
        if (!IsActive) return false;

        return featureName.ToLower() switch
        {
            "ai" or "aiassistant" or "helpbot" => _currentLimits.HasAIAssistant,
            "aidataanalysis" or "dataanalysis" => _currentLimits.HasAIDataAnalysis,
            "calendar" or "calendarsync" => _currentLimits.HasCalendarSync,
            "basicreports" => _currentLimits.HasBasicReports,
            "reports" or "advancedreports" => _currentLimits.HasAdvancedReports,
            "emailsupport" => _currentLimits.HasEmailSupport,
            "support" or "prioritysupport" => _currentLimits.HasPrioritySupport,
            "networkdb" or "enterprisedb" => _currentLimits.AllowsNetworkDatabase,
            _ => true // Unknown features default to allowed
        };
    }

    public (bool CanAdd, int Remaining, string? Message) CheckLimit(
        string resourceType, 
        int currentCount)
    {
        var limit = resourceType.ToLower() switch
        {
            "team_members" => _currentLimits.MaxTeamMembers,
            "tasks" => _currentLimits.MaxTasks,
            "projects" => _currentLimits.MaxProjects,
            "goals" => _currentLimits.MaxGoals,
            _ => -1 // Unknown = unlimited
        };

        if (limit == -1) return (true, -1, null); // Unlimited

        var remaining = limit - currentCount;
        if (remaining <= 0)
            return (false, 0, $"You've reached the limit for your plan.");

        return (true, remaining, null);
    }
}
```

---

## 7. Seat License Management

### 7.1 Concept Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SEAT LICENSE MODEL                                │
│                                                                          │
│  Organization (License Holder)                                           │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  subscription_tier: "pro"                                        │    │
│  │  max_users: 10          ← Number of app login seats              │    │
│  │  max_team_members: 100  ← Number of people that can be tracked   │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  App Users (Seat Licenses)      │  Team Members (Tracked People)  │  │
│  │  ───────────────────────────    │  ───────────────────────────    │  │
│  │  🪑 Brian (admin)     - USED    │  👤 Alice (reports to Brian)    │  │
│  │  🪑 Sarah (manager)   - USED    │  👤 Bob (reports to Brian)      │  │
│  │  🪑 Mike (manager)    - USED    │  👤 Carol (reports to Sarah)    │  │
│  │  🪑 [Empty]           - AVAIL   │  👤 Dave (reports to Sarah)     │  │
│  │  🪑 [Empty]           - AVAIL   │  👤 Eve (reports to Mike)       │  │
│  │  ...                            │  ...                             │  │
│  │  (3/10 seats used)              │  (5/100 tracked)                 │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  IMPORTANT DISTINCTION:                                                  │
│  • App Users = People who LOGIN to the app (managers, admins)            │
│  • Team Members = People being TRACKED (may or may not have login)       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Current Schema Support

**Users Table (App Users / Seat Licenses):**
```sql
users (
    id UUID PRIMARY KEY,
    supabase_auth_id UUID,      -- Links to auth for login
    organization_id UUID,        -- Which org they belong to
    firm_id UUID,               -- Billing entity (alternative to org)
    role VARCHAR(50),           -- admin, manager, viewer, hr_admin
    is_active BOOLEAN,          -- Can be deactivated to free seat
    ...
)
```

**Team Members Table (Tracked People):**
```sql
team_members (
    id UUID PRIMARY KEY,
    organization_id UUID,        -- Which org they belong to
    manager_user_id UUID,        -- Their manager (app user)
    user_id UUID,               -- Optional: if they also have app login
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    employment_status,          -- active, on_leave, separated
    is_deleted BOOLEAN,
    ...
)
```

### 7.3 What's NOT Implemented for Seat Licensing

| Feature | Status | Required Work |
|---------|--------|---------------|
| Count active users per org | ❌ Not implemented | Query + UI |
| Enforce max_users limit | ❌ Not implemented | Check on user creation |
| Count team members per org | ❌ Not implemented | Query + UI |
| Enforce max_team_members limit | ❌ Not implemented | Check on team member creation |
| Seat usage dashboard | ❌ Not implemented | Admin UI |
| Invite flow with seat check | ❌ Not implemented | Full invite system |
| Deactivate user (free seat) | ❌ Not implemented | Admin action |
| Upgrade prompt when at limit | ❌ Not implemented | UI + Stripe integration |

### 7.4 Required Seat Validation Functions

```sql
-- Function to check if organization can add another app user
CREATE OR REPLACE FUNCTION can_add_user(org_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    current_count INT;
    max_allowed INT;
BEGIN
    -- Count active users in this org
    SELECT COUNT(*) INTO current_count
    FROM users
    WHERE organization_id = org_id 
      AND is_active = true 
      AND is_deleted = false;
    
    -- Get org's max_users limit
    SELECT max_users INTO max_allowed
    FROM organizations
    WHERE id = org_id;
    
    RETURN current_count < COALESCE(max_allowed, 5);
END;
$$ LANGUAGE plpgsql;

-- Function to check if organization can add another team member
CREATE OR REPLACE FUNCTION can_add_team_member(org_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    current_count INT;
    max_allowed INT;
BEGIN
    SELECT COUNT(*) INTO current_count
    FROM team_members
    WHERE organization_id = org_id 
      AND is_deleted = false;
    
    SELECT max_team_members INTO max_allowed
    FROM organizations
    WHERE id = org_id;
    
    RETURN current_count < COALESCE(max_allowed, 25);
END;
$$ LANGUAGE plpgsql;
```

### 7.5 Required Pro Cohere Seat Service

```csharp
// PROPOSED: Services/SeatLicenseService.cs

public interface ISeatLicenseService
{
    /// <summary>
    /// Gets current seat usage for the user's organization.
    /// </summary>
    Task<SeatUsage> GetSeatUsageAsync();
    
    /// <summary>
    /// Checks if a new app user can be added.
    /// </summary>
    Task<(bool CanAdd, string? Message)> CanAddUserAsync();
    
    /// <summary>
    /// Checks if a new team member can be added.
    /// </summary>
    Task<(bool CanAdd, string? Message)> CanAddTeamMemberAsync();
}

public class SeatUsage
{
    public int UsedUserSeats { get; set; }
    public int MaxUserSeats { get; set; }
    public int UsedTeamMemberSlots { get; set; }
    public int MaxTeamMemberSlots { get; set; }
    public string SubscriptionTier { get; set; }
    
    public bool CanAddUser => UsedUserSeats < MaxUserSeats;
    public bool CanAddTeamMember => UsedTeamMemberSlots < MaxTeamMemberSlots;
    public int RemainingUserSeats => MaxUserSeats - UsedUserSeats;
    public int RemainingTeamMemberSlots => MaxTeamMemberSlots - UsedTeamMemberSlots;
}
```

---

## 8. Current Implementation Status

### 8.1 What's Working ✅

| Feature | Status | Notes |
|---------|--------|-------|
| Supabase Auth (email/password) | ✅ Working | Sign in, sign up, sign out |
| DPAPI Session Storage | ✅ Working | "Remember Me" functionality |
| Auto-login on app restart | ✅ Working | Using stored refresh token |
| JWT token refresh | ✅ Working | Automatic via Supabase client |
| User profile loading | ✅ Working | From `users` table via RLS |
| Basic RLS policies | ✅ Partial | users, team_members, goals, tasks, projects |

### 8.2 What's Missing ❌

| Feature | Priority | Effort |
|---------|----------|--------|
| Auto-create `users` record on signup | 🔴 High | 2-4 hours |
| Organization assignment flow | 🔴 High | 8-16 hours |
| Seat license validation | 🔴 High | 4-8 hours |
| User invitation system | 🟡 Medium | 16-24 hours |
| Subscription service (Pro Cohere) | 🟡 Medium | 8-16 hours |
| Upgrade/downgrade flow | 🟡 Medium | 16-24 hours |
| Admin user management UI | 🟡 Medium | 16-24 hours |
| OAuth providers (Google, Microsoft) | 🟢 Low | 4-8 hours |
| Password reset flow UI | 🟢 Low | 4-8 hours |
| Email verification handling | 🟢 Low | 2-4 hours |

---

## 9. Gap Analysis & Required Work

### 9.1 Phase 1: Critical Path (Must Have for Beta)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 1: USER ONBOARDING (Est. 24-32 hours)                            │
│                                                                          │
│  1. Auto-create users record on Supabase signup                         │
│     - Database trigger or Edge Function                                  │
│     - Set default role, org assignment                                   │
│     - 2-4 hours                                                          │
│                                                                          │
│  2. First-run setup wizard                                              │
│     - Profile completion (name, job title)                               │
│     - Organization: create new OR enter invite code                      │
│     - 8-12 hours                                                         │
│                                                                          │
│  3. Basic seat validation                                               │
│     - SeatLicenseService in Pro Cohere                                   │
│     - Check limits before creating users/team members                    │
│     - Show upgrade prompt when at limit                                  │
│     - 4-8 hours                                                          │
│                                                                          │
│  4. Organization creation flow                                          │
│     - Create new org on first signup (if no invite)                      │
│     - Set as org admin                                                   │
│     - Default to Free tier                                               │
│     - 4-8 hours                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Phase 2: User Management (Post-Beta)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 2: MULTI-USER & INVITES (Est. 40-60 hours)                       │
│                                                                          │
│  1. User invitation system                                              │
│     - Admin sends invite email                                           │
│     - Invite contains org assignment                                     │
│     - Respects seat limits                                               │
│     - 16-24 hours                                                        │
│                                                                          │
│  2. Admin user management UI                                            │
│     - View all org users                                                 │
│     - Activate/deactivate users                                          │
│     - Change user roles                                                  │
│     - 16-24 hours                                                        │
│                                                                          │
│  3. Seat management dashboard                                           │
│     - Visual seat usage                                                  │
│     - Upgrade prompts                                                    │
│     - 8-12 hours                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.3 Phase 3: Billing Integration (Post-Launch)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 3: STRIPE BILLING (Est. 40-60 hours)                             │
│                                                                          │
│  1. Stripe integration                                                  │
│     - Customer portal                                                    │
│     - Subscription management                                            │
│     - Webhook handling                                                   │
│     - 16-24 hours                                                        │
│                                                                          │
│  2. Upgrade/downgrade flows                                             │
│     - In-app upgrade prompts                                             │
│     - Proration handling                                                 │
│     - Downgrade warnings (data limits)                                   │
│     - 16-24 hours                                                        │
│                                                                          │
│  3. Usage-based billing (optional)                                      │
│     - AI token metering                                                  │
│     - Overage handling                                                   │
│     - 8-12 hours                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Database Schema Reference

### 10.1 Core Auth/User Tables

**auth.users (Supabase Managed)**
- `id`: UUID - Primary auth identity
- `email`: User's email
- `email_confirmed_at`: Verification timestamp
- `raw_user_meta_data`: JSON metadata from signup
- `created_at`, `updated_at`

**users (Application)**
| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | App user ID |
| `supabase_auth_id` | UUID | Links to auth.users.id |
| `organization_id` | UUID | FK to organizations |
| `firm_id` | UUID | Billing entity (if different from org) |
| `email` | VARCHAR(255) | User email |
| `display_name` | VARCHAR(200) | Display name |
| `first_name` | VARCHAR(100) | First name |
| `last_name` | VARCHAR(100) | Last name |
| `role` | VARCHAR(50) | admin, manager, viewer, hr_admin |
| `is_admin` | BOOLEAN | Organization admin flag |
| `is_active` | BOOLEAN | Account active (seat in use) |
| `is_email_verified` | BOOLEAN | Email verified flag |
| `last_login_at` | TIMESTAMPTZ | Last login timestamp |

**organizations**
| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Organization ID |
| `name` | VARCHAR(200) | Organization name |
| `slug` | VARCHAR(100) | URL-friendly identifier |
| `subscription_tier` | VARCHAR(50) | free, standard, pro |
| `max_users` | INT | App user seat limit |
| `max_team_members` | INT | Team member tracking limit |
| `settings` | JSONB | Organization settings |
| `is_active` | BOOLEAN | Organization active |

**team_members**
| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Team member ID |
| `organization_id` | UUID | FK to organizations |
| `manager_user_id` | UUID | FK to users (their manager) |
| `user_id` | UUID | FK to users (if they have login) |
| `first_name` | VARCHAR(100) | First name |
| `last_name` | VARCHAR(100) | Last name |
| `email` | VARCHAR(255) | Contact email |
| `employment_status` | ENUM | active, on_leave, separated |

### 10.2 Role-Based Access Control Tables

**roles**
- 37 permission columns (can_manage_org, can_create_goals, etc.)
- Pre-defined system roles + custom org roles

**user_roles**
- Maps users → roles (with optional team scope)

---

## 11. Multi-Product Architecture

### 11.1 Prickly Cactus Software Products

| Product | Description | Target Market |
|---------|-------------|---------------|
| **Pro Cohere** | Team/people management | Managers, HR |
| **Pro Causa** | Case management | Law firms |
| **Threadline** | Communication/therapy notes | Therapists, counselors |

### 11.2 Infrastructure Decision

**ONE Supabase project, multiple PostgreSQL schemas:**

```
┌─────────────────────────────────────────────────────────────┐
│  Supabase Project: prickly-cactus-prod ($25/mo)            │
│                                                              │
│  ┌─────────────────┐  Shared auth.users (Supabase Auth)     │
│  │ public schema   │  Shared organizations, users           │
│  │ (Pro Cohere)    │  Pro Cohere tables (current 65 tables) │
│  └─────────────────┘                                         │
│                                                              │
│  ┌─────────────────┐  FUTURE: When Pro Causa is built       │
│  │ procausa schema │  Cases, clients, documents, etc.       │
│  └─────────────────┘                                         │
│                                                              │
│  ┌─────────────────┐  FUTURE: When Threadline is built      │
│  │ threadline      │  Threads, sessions, notes, etc.        │
│  │ schema          │                                         │
│  └─────────────────┘                                         │
└─────────────────────────────────────────────────────────────┘
```

### 11.3 Current State

- **Pro Cohere**: Tables in `public` schema (default) - NO CHANGE NEEDED
- **Pro Causa**: Empty/test project - PAUSE OR DELETE to save $25/mo
- **Threadline**: Empty/test project - PAUSE OR DELETE to save $25/mo

### 11.4 Future: Adding Another Product

When ready to build Pro Causa or Threadline:

```sql
-- 1. Create the schema
CREATE SCHEMA IF NOT EXISTS procausa;

-- 2. Grant permissions
GRANT USAGE ON SCHEMA procausa TO authenticated;
GRANT ALL ON ALL TABLES IN SCHEMA procausa TO authenticated;
ALTER DEFAULT PRIVILEGES IN SCHEMA procausa 
    GRANT ALL ON TABLES TO authenticated;

-- 3. Create product tables in that schema
CREATE TABLE procausa.cases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES public.organizations(id),
    ...
);
```

### 11.5 Cross-Product Licensing

Organizations can license multiple products:

```sql
-- Option: Add to organizations table
ALTER TABLE organizations ADD COLUMN licensed_products JSONB DEFAULT '["procohere"]';

-- OR: Separate table for more complex licensing
CREATE TABLE product_licenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id),
    product VARCHAR(50) NOT NULL,  -- 'procohere', 'procausa', 'threadline'
    tier VARCHAR(50) NOT NULL,     -- 'free', 'standard', 'pro'
    max_seats INT,
    expires_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT now()
);
```

**Shared across all products:**
- `auth.users` (Supabase Auth)
- `organizations` table
- `users` table (app users)
- Billing/Stripe integration

---

## 12. Beta Launch Strategy

### 12.1 Decision: Manual User Setup for Beta

**Instead of building an in-app first-run wizard**, user setup will be handled manually via Supabase stored procedures during beta. This allows faster time-to-market and lets us iterate on the user model based on real feedback.

### 12.2 Supabase Stored Procedures for Manual Setup

Supabase PostgreSQL fully supports stored procedures and functions. Create these admin utilities:

```sql
-- ===========================================
-- ADMIN UTILITY: Create complete user profile
-- Run from Supabase SQL Editor or Dashboard
-- ===========================================

CREATE OR REPLACE FUNCTION admin_create_user_profile(
    p_auth_id UUID,           -- From Supabase Auth (after user signs up)
    p_email TEXT,
    p_first_name TEXT,
    p_last_name TEXT,
    p_organization_id UUID,   -- Existing org or create new first
    p_role TEXT DEFAULT 'user'
) RETURNS UUID AS $$
DECLARE
    v_user_id UUID;
BEGIN
    -- Create user record
    INSERT INTO users (
        id,
        supabase_auth_id,
        email,
        first_name,
        last_name,
        organization_id,
        role,
        is_active,
        created_at,
        updated_at
    ) VALUES (
        gen_random_uuid(),
        p_auth_id,
        p_email,
        p_first_name,
        p_last_name,
        p_organization_id,
        p_role,
        true,
        now(),
        now()
    )
    RETURNING id INTO v_user_id;
    
    RETURN v_user_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ===========================================
-- ADMIN UTILITY: Create organization
-- ===========================================

CREATE OR REPLACE FUNCTION admin_create_organization(
    p_name TEXT,
    p_subscription_tier TEXT DEFAULT 'free',
    p_max_users INT DEFAULT 5,
    p_max_team_members INT DEFAULT 20
) RETURNS UUID AS $$
DECLARE
    v_org_id UUID;
BEGIN
    INSERT INTO organizations (
        id,
        name,
        subscription_tier,
        max_users,
        max_team_members,
        is_active,
        created_at,
        updated_at
    ) VALUES (
        gen_random_uuid(),
        p_name,
        p_subscription_tier,
        p_max_users,
        p_max_team_members,
        true,
        now(),
        now()
    )
    RETURNING id INTO v_org_id;
    
    RETURN v_org_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ===========================================
-- ADMIN UTILITY: Full onboarding (org + user)
-- ===========================================

CREATE OR REPLACE FUNCTION admin_onboard_new_customer(
    p_auth_id UUID,
    p_email TEXT,
    p_first_name TEXT,
    p_last_name TEXT,
    p_org_name TEXT,
    p_subscription_tier TEXT DEFAULT 'standard'
) RETURNS TABLE (organization_id UUID, user_id UUID) AS $$
DECLARE
    v_org_id UUID;
    v_user_id UUID;
BEGIN
    -- Create organization
    v_org_id := admin_create_organization(
        p_org_name,
        p_subscription_tier,
        CASE p_subscription_tier 
            WHEN 'free' THEN 5
            WHEN 'standard' THEN 25
            WHEN 'pro' THEN 100
            ELSE 5
        END,
        CASE p_subscription_tier 
            WHEN 'free' THEN 20
            WHEN 'standard' THEN 100
            WHEN 'pro' THEN 500
            ELSE 20
        END
    );
    
    -- Create user as org admin
    v_user_id := admin_create_user_profile(
        p_auth_id,
        p_email,
        p_first_name,
        p_last_name,
        v_org_id,
        'admin'  -- First user is org admin
    );
    
    RETURN QUERY SELECT v_org_id, v_user_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
```

### 12.3 Beta Onboarding Workflow

```
┌─────────────────────────────────────────────────────────────────────────┐
│  BETA USER ONBOARDING (Manual Process)                                  │
│                                                                          │
│  1. Customer signs up at Supabase Auth UI or app login screen           │
│     → Creates auth.users record only                                     │
│     → Gets auth_id (UUID)                                               │
│                                                                          │
│  2. Admin (Brian) runs stored procedure in Supabase SQL Editor:         │
│                                                                          │
│     SELECT * FROM admin_onboard_new_customer(                           │
│         'auth-uuid-here',                                                │
│         'customer@example.com',                                          │
│         'Jane',                                                          │
│         'Smith',                                                         │
│         'Acme Corp',                                                     │
│         'standard'                                                       │
│     );                                                                   │
│                                                                          │
│  3. Notify customer their account is ready                               │
│     → They can now log in and use the app                               │
│                                                                          │
│  4. For additional users in same org:                                    │
│                                                                          │
│     SELECT admin_create_user_profile(                                    │
│         'new-user-auth-id',                                              │
│         'employee@acmecorp.com',                                         │
│         'John',                                                          │
│         'Doe',                                                           │
│         'existing-org-id',                                               │
│         'user'                                                           │
│     );                                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 12.4 Why This Approach for Beta

| Benefit | Explanation |
|---------|-------------|
| **Faster launch** | No UI to build for onboarding wizard |
| **Direct control** | Brian can manage exactly who gets access |
| **Learn first** | See what data customers actually need before automating |
| **Flexibility** | Easy to adjust user setup as requirements emerge |
| **Low volume** | Beta = limited users, manual is fine |

### 12.5 What the Desktop App Needs (Minimal)

For beta, the desktop app only needs to:

1. ✅ **Login screen** (already built)
2. ✅ **Load user profile** after login (already built)
3. ⬜ **Show friendly error** if user record doesn't exist yet
   - "Your account is being set up. You'll receive an email when ready."

**NOT needed for beta:**
- ❌ First-run wizard
- ❌ Profile completion form  
- ❌ Organization creation UI
- ❌ Self-service signup flow

---

## Summary: Key Actions by Phase

### Immediate (Before Beta)

1. **Create stored procedures** in Supabase (above SQL)
2. **Add error handling** for missing user record in app
3. **Finish dashboard** implementation

### Beta Period (Iterate Based on Feedback)

4. **Manual onboarding** via stored procedures
5. **Collect feedback** on what users need
6. **Iterate on user model** if needed

### Post-Beta (Before Launch)

7. **Build web admin portal** (invite system, billing)
8. **Add Stripe integration**
9. **Implement SeatLicenseService** in app
10. **Build self-service flows** based on beta learnings

### Post-Launch (Scale)

11. **OAuth provider support** (if customers request)
12. **Upgrade/downgrade flows**
13. **Advanced admin features**

---

*Document maintained by: Brian Roach*  
*Next review: After beta launch*
