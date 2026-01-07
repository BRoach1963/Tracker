# Tracker Organization Model Migration Plan
## From Owner-Based to Organization-Based Architecture

**Status**: PLANNING  
**Priority**: HIGH - Foundation for all future work  
**Created**: January 4, 2026  
**Version**: 1.1 (Updated with confirmed decisions)

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Confirmed Decisions](#confirmed-decisions)
3. [Current vs Target Architecture](#current-vs-target-architecture)
4. [Database Schema Changes](#database-schema-changes)
5. [Vector Store Strategy](#vector-store-strategy)
6. [SQL Server Support](#sql-server-support)
7. [RLS Policy Updates](#rls-policy-updates)
8. [Application Changes](#application-changes)
9. [Migration Path](#migration-path)
10. [Timeline Estimate](#timeline-estimate)

---

## Executive Summary

### What We're Changing
Moving from **owner-based isolation** (each manager owns their data) to **organization-based isolation** (data belongs to the org, managers have access based on role/team).

### Why This Matters
| Scenario | Current (Owner-Based) | Target (Org-Based) |
|----------|----------------------|-------------------|
| Sarah moves from Brian's team to Alice's | Data stays with Brian, Alice sees nothing | Manager changes, data stays in org |
| Brian leaves company | His data is orphaned/deleted | Data persists, reassigned to new manager |
| HR needs org-wide view | Impossible | Role-based access to all employees |
| Leadership dashboard | Aggregate manually | Query across org |

---

## Confirmed Decisions

| Decision | Choice | Notes |
|----------|--------|-------|
| **Organization model** | Single org per user | Users belong to ONE org only. No crossover. Orgs are separate billable entities |
| **Naming convention** | Keep `TeamMember` | Don't rename to Employee - fits app dynamic, doesn't matter to user |
| **SQL Server** | Required for V1 | PostgreSQL is default. SQL Server is option (likely requires service assistance/add-on cost) |
| **Local SQLite** | REMOVE entirely | No more local SQLite. Only PostgreSQL or SQL Server |
| **Vector storage** | Same DB as app data | Unified in PostgreSQL (pgvector) or SQL Server (VARBINARY) |

### Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Organization scope** | All data has `organization_id` | Matches Pro Causa, enables team movement |
| **Vector storage** | Same DB as app data | Single source of truth, unified backups |
| **PostgreSQL vectors** | `pgvector` extension | Native, fast, well-supported |
| **SQL Server vectors** | `VARBINARY(MAX)` + app-side similarity | No native support, manual calculation |
| **Local SQLite** | REMOVED | No local cache - single DB only |

---

## Current vs Target Architecture

### Current: Owner-Based

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CURRENT ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   User (Brian)                          User (Alice)                        │
│   ├── TeamMembers (his reports)         ├── TeamMembers (her reports)       │
│   ├── Meetings (his 1:1s)               ├── Meetings (her 1:1s)             │
│   ├── Tasks (he assigned)               ├── Tasks (she assigned)            │
│   └── OKRs (his OKRs)                   └── OKRs (her OKRs)                 │
│                                                                             │
│   PostgreSQL RLS: owner_id = current_user_id                                │
│                                                                             │
│   Vector Store: Separate SQLite per machine (%LocalAppData%\Tracker\)       │
│                                                                             │
│   PROBLEM: Sarah in Brian's team → Brian leaves → Sarah's history lost      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Target: Organization-Based

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           TARGET ARCHITECTURE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Organization (Prickly Cactus Software)                                    │
│   │                                                                         │
│   ├── Users (auth + role)                                                   │
│   │   ├── Brian (role: manager)                                             │
│   │   ├── Alice (role: manager)                                             │
│   │   └── Carol (role: hr_admin)                                            │
│   │                                                                         │
│   ├── Employees (org-level, NOT owned by manager)                           │
│   │   ├── Sarah (current_manager: Brian → Alice)  ← CAN CHANGE!             │
│   │   ├── Mike (current_manager: Brian)                                     │
│   │   └── John (current_manager: Alice)                                     │
│   │                                                                         │
│   ├── Meetings (belong to org, linked to manager + employee)                │
│   │   └── manager_id + employee_id (who conducted meeting with whom)        │
│   │                                                                         │
│   ├── Tasks, OKRs, Kudos, Reviews... (all org-level)                        │
│   │                                                                         │
│   └── Vectors (embedded data for AI, same DB)                               │
│       ├── employee_vectors (searchable employee profiles)                   │
│       ├── meeting_vectors (searchable meeting notes)                        │
│       └── doc_vectors (help documentation)                                  │
│                                                                             │
│   PostgreSQL RLS:                                                           │
│   - organization_id = current_user_org  (always)                            │
│   - Additional checks based on role:                                        │
│     - manager: sees their direct reports + meetings they conducted          │
│     - hr_admin: sees all employees                                          │
│     - admin: sees everything                                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Database Schema Changes

### New Core Tables

```sql
-- ============================================================================
-- ORGANIZATIONS TABLE
-- ============================================================================
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(63) UNIQUE,  -- tracker-mycompany (for future multi-tenant)
    timezone VARCHAR(50) DEFAULT 'America/Chicago',
    logo_url TEXT,
    is_active BOOLEAN DEFAULT true,
    subscription_tier VARCHAR(50) DEFAULT 'team',
    max_users INT DEFAULT 10,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- USERS TABLE (replaces current simple users table)
-- ============================================================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    password_hash TEXT NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(255),
    role VARCHAR(50) NOT NULL DEFAULT 'manager',  -- 'admin', 'hr_admin', 'manager', 'viewer'
    is_active BOOLEAN DEFAULT true,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, email)
);

CREATE INDEX idx_users_org_id ON users(organization_id);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
```

### Renamed/Updated Tables

```sql
-- ============================================================================
-- TEAM_MEMBERS TABLE (keeping name, adding org scope)
-- Key change: belongs to ORG, not to a specific manager
-- ============================================================================
CREATE TABLE team_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Current manager (CAN CHANGE without losing history)
    current_manager_id UUID REFERENCES users(id) ON DELETE SET NULL,
    
    -- Team member details
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255),
    job_title VARCHAR(255),
    department VARCHAR(255),
    hire_date DATE,
    termination_date DATE,
    birthday DATE,
    
    -- Status
    is_active BOOLEAN DEFAULT true,
    employment_status VARCHAR(50) DEFAULT 'active',  -- active, on_leave, terminated
    
    -- Profile
    profile_image BYTEA,
    linkedin_url VARCHAR(255),
    phone VARCHAR(50),
    
    -- Metadata
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    
    UNIQUE(organization_id, email)
);

CREATE INDEX idx_team_members_org_id ON team_members(organization_id);
CREATE INDEX idx_team_members_manager_id ON team_members(current_manager_id);
CREATE INDEX idx_team_members_is_active ON team_members(is_active);

-- ============================================================================
-- MANAGER_HISTORY TABLE (track who managed whom, when)
-- ============================================================================
CREATE TABLE manager_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    manager_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_date DATE NOT NULL,
    end_date DATE,  -- NULL = current
    reason VARCHAR(255),  -- 'reorg', 'promotion', 'manager_departure'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_manager_history_team_member ON manager_history(team_member_id);
CREATE INDEX idx_manager_history_manager ON manager_history(manager_id);

-- ============================================================================
-- MEETINGS TABLE (was: one_on_ones)
-- ============================================================================
CREATE TABLE meetings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- WHO: Both manager and team member are referenced
    manager_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- WHAT
    title VARCHAR(255),
    meeting_date TIMESTAMP NOT NULL,
    duration_minutes INT DEFAULT 30,
    status VARCHAR(50) DEFAULT 'scheduled',
    notes TEXT,
    
    -- Metadata
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_meetings_org_id ON meetings(organization_id);
CREATE INDEX idx_meetings_manager_id ON meetings(manager_id);
CREATE INDEX idx_meetings_team_member_id ON meetings(team_member_id);
CREATE INDEX idx_meetings_date ON meetings(meeting_date);

-- ============================================================================
-- TASKS TABLE
-- ============================================================================
CREATE TABLE tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- WHO
    assigned_by UUID REFERENCES users(id),        -- Manager who assigned
    assigned_to UUID REFERENCES team_members(id),  -- Team member assigned to
    
    -- WHAT
    title VARCHAR(255) NOT NULL,
    description TEXT,
    due_date DATE,
    status VARCHAR(50) DEFAULT 'pending',
    priority VARCHAR(50) DEFAULT 'medium',
    
    -- Metadata
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tasks_org_id ON tasks(organization_id);
CREATE INDEX idx_tasks_assigned_to ON tasks(assigned_to);
CREATE INDEX idx_tasks_status ON tasks(status);

-- Similar pattern for: okrs, kpis, projects, kudos, feedback, goals, surveys, reviews
-- All have: organization_id + relevant user/employee references
```

---

## Vector Store Strategy

### The Problem

Current VectorStore uses **local SQLite** at `%LocalAppData%\Tracker\vectors.db`:
- ❌ Data stuck on one machine
- ❌ Can't share vectors across devices
- ❌ Separate backup concern
- ❌ Not filtered by organization

### The Solution: Unified Database Vectors

**Goal: Vectors live in the SAME database as app data.**

### PostgreSQL: pgvector Extension

```sql
-- Enable pgvector (requires PostgreSQL 11+)
CREATE EXTENSION IF NOT EXISTS vector;

-- ============================================================================
-- VECTOR_EMBEDDINGS TABLE
-- Unified table for all embeddings (docs, employees, meetings, etc.)
-- ============================================================================
CREATE TABLE vector_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- What this vector represents
    entity_type VARCHAR(50) NOT NULL,  -- 'doc', 'team_member', 'meeting', 'task', 'okr'
    entity_id VARCHAR(255) NOT NULL,   -- ID of the source entity
    chunk_index INT DEFAULT 0,         -- For chunked documents
    
    -- The actual content and embedding
    content TEXT NOT NULL,             -- Original text that was embedded
    embedding vector(1536),            -- OpenAI ada-002 produces 1536 dimensions
    
    -- Metadata (JSON for flexibility)
    metadata JSONB,
    
    -- Timestamps
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(organization_id, entity_type, entity_id, chunk_index)
);

-- HNSW index for fast approximate nearest neighbor search
CREATE INDEX idx_embeddings_vector ON vector_embeddings 
    USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Standard indexes
CREATE INDEX idx_embeddings_org_id ON vector_embeddings(organization_id);
CREATE INDEX idx_embeddings_entity_type ON vector_embeddings(entity_type);
CREATE INDEX idx_embeddings_entity_id ON vector_embeddings(entity_id);

-- ============================================================================
-- VECTOR SEARCH FUNCTION
-- ============================================================================
CREATE OR REPLACE FUNCTION search_vectors(
    p_org_id UUID,
    p_query_embedding vector(1536),
    p_entity_types TEXT[] DEFAULT NULL,  -- Filter by type: ARRAY['team_member', 'meeting']
    p_limit INT DEFAULT 10,
    p_min_similarity FLOAT DEFAULT 0.5
)
RETURNS TABLE (
    entity_type VARCHAR(50),
    entity_id VARCHAR(255),
    content TEXT,
    similarity FLOAT,
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        v.entity_type,
        v.entity_id,
        v.content,
        1 - (v.embedding <=> p_query_embedding) AS similarity,
        v.metadata
    FROM vector_embeddings v
    WHERE v.organization_id = p_org_id
      AND (p_entity_types IS NULL OR v.entity_type = ANY(p_entity_types))
      AND 1 - (v.embedding <=> p_query_embedding) >= p_min_similarity
    ORDER BY v.embedding <=> p_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;
```

### SQL Server: Custom Solution

SQL Server doesn't have native vector support. Two options:

#### Option A: Store as VARBINARY, Calculate in App (Simpler)

```sql
-- ============================================================================
-- SQL SERVER: VECTOR_EMBEDDINGS TABLE
-- ============================================================================
CREATE TABLE vector_embeddings (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    organization_id UNIQUEIDENTIFIER NOT NULL,
    
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(255) NOT NULL,
    chunk_index INT DEFAULT 0,
    
    content NVARCHAR(MAX) NOT NULL,
    embedding VARBINARY(MAX) NOT NULL,  -- Serialized float array
    embedding_dimensions INT NOT NULL DEFAULT 1536,
    
    metadata NVARCHAR(MAX),  -- JSON string
    
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    updated_at DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE CASCADE,
    CONSTRAINT UQ_embedding UNIQUE (organization_id, entity_type, entity_id, chunk_index)
);

CREATE INDEX idx_embeddings_org_id ON vector_embeddings(organization_id);
CREATE INDEX idx_embeddings_entity_type ON vector_embeddings(entity_type);
```

**App-side similarity calculation:**
```csharp
// Load all org vectors into memory (or paginate)
// Calculate cosine similarity in C#
public float CosineSimilarity(float[] a, float[] b)
{
    float dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
}
```

#### Option B: Azure AI Search (Enterprise, More Complex)

For large-scale SQL Server deployments, integrate Azure AI Search as a sidecar:
- Index vectors in Azure AI Search
- Query returns entity IDs
- Join back to SQL Server for full data

**Not recommended for V1** - adds complexity and cloud dependency.

### Recommendation

| Database | Vector Solution | Complexity |
|----------|-----------------|------------|
| PostgreSQL | pgvector (native) | ⭐ Low |
| SQL Server | VARBINARY + app-side calc | ⭐⭐ Medium |
| SQL Server Enterprise | Azure AI Search | ⭐⭐⭐ High |

**Both PostgreSQL (pgvector) and SQL Server (VARBINARY) implementations required for V1.**
- PostgreSQL is the default
- SQL Server is an option (may require service assistance/add-on cost)

---

## RLS Policy Updates

### New RLS Strategy

```sql
-- ============================================================================
-- RLS POLICIES FOR ORGANIZATION MODEL
-- ============================================================================

-- Helper function to get current user's organization
CREATE OR REPLACE FUNCTION current_user_org_id() RETURNS UUID AS $$
    SELECT NULLIF(current_setting('app.current_org_id', true), '')::UUID;
$$ LANGUAGE sql STABLE;

-- Helper function to get current user's role
CREATE OR REPLACE FUNCTION current_user_role() RETURNS TEXT AS $$
    SELECT current_setting('app.current_user_role', true);
$$ LANGUAGE sql STABLE;

-- Helper function to get current user's ID
CREATE OR REPLACE FUNCTION current_user_id() RETURNS UUID AS $$
    SELECT NULLIF(current_setting('app.current_user_id', true), '')::UUID;
$$ LANGUAGE sql STABLE;

-- ============================================================================
-- TEAM_MEMBERS: Org-scoped with role-based visibility
-- ============================================================================
ALTER TABLE team_members ENABLE ROW LEVEL SECURITY;

-- Admin/HR see all team members in org
CREATE POLICY team_members_admin_policy ON team_members
    FOR ALL
    USING (
        organization_id = current_user_org_id()
        AND current_user_role() IN ('admin', 'hr_admin')
    );

-- Managers see their direct reports
CREATE POLICY team_members_manager_policy ON team_members
    FOR SELECT
    USING (
        organization_id = current_user_org_id()
        AND current_manager_id = current_user_id()
    );

-- Managers can update their direct reports
CREATE POLICY team_members_manager_update_policy ON team_members
    FOR UPDATE
    USING (
        organization_id = current_user_org_id()
        AND current_manager_id = current_user_id()
    );

-- ============================================================================
-- MEETINGS: See meetings you conducted OR about team members you manage
-- ============================================================================
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;

CREATE POLICY meetings_policy ON meetings
    FOR ALL
    USING (
        organization_id = current_user_org_id()
        AND (
            current_user_role() IN ('admin', 'hr_admin')
            OR manager_id = current_user_id()
        )
    );

-- ============================================================================
-- VECTORS: Same org, filtered by entity access
-- ============================================================================
ALTER TABLE vector_embeddings ENABLE ROW LEVEL SECURITY;

-- For now: org-level access (all users see all org vectors)
-- Future: could filter by entity_type + user role
CREATE POLICY vectors_policy ON vector_embeddings
    FOR ALL
    USING (
        organization_id = current_user_org_id()
    );
```

### Updated RLS Connection Interceptor

```csharp
// Set BOTH user ID AND org ID on connection open
public class RlsConnectionInterceptor : DbConnectionInterceptor
{
    private readonly Guid _userId;
    private readonly Guid _orgId;
    private readonly string _role;

    public RlsConnectionInterceptor(Guid userId, Guid orgId, string role)
    {
        _userId = userId;
        _orgId = orgId;
        _role = role;
    }

    private async Task SetContextAsync(DbConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            SET app.current_user_id = '{_userId}';
            SET app.current_org_id = '{_orgId}';
            SET app.current_user_role = '{_role}';
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}
```

---

## Application Changes

### 1. AuthenticatedUser Updates

```csharp
public class AuthenticatedUser
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }  // NEW
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string Role { get; set; }  // NEW: 'admin', 'hr_admin', 'manager', 'viewer'
    public DateTime? LastLoginAt { get; set; }
}
```

### 2. VectorStore Refactor

```csharp
public interface IVectorStore
{
    Task<Guid> StoreAsync(string entityType, string entityId, string content, float[] embedding, Dictionary<string, object>? metadata = null);
    Task<List<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 10, string[]? entityTypes = null, float minSimilarity = 0.5f);
    Task DeleteAsync(string entityType, string entityId);
    Task ReindexEntityAsync(string entityType, string entityId);
}

// PostgreSQL implementation using pgvector
public class PostgresVectorStore : IVectorStore
{
    private readonly Func<TrackerDbContext> _contextFactory;
    // Uses TrackerDbContext which already has org context via RLS
}

// SQL Server implementation with app-side similarity
public class SqlServerVectorStore : IVectorStore
{
    private readonly Func<TrackerDbContext> _contextFactory;
    // Loads vectors, calculates similarity in memory
}

// NOTE: Local SQLite VectorStore REMOVED - all vectors stored in main database
```

### 3. Data Model Renames

| Current | New | Notes |
|---------|-----|-------|
| `TeamMember` | `TeamMember` | **Keep existing name** - fits app dynamic |
| `OneOnOne` | `Meeting` | Generic, could support other meeting types |
| `owner_id` | `organization_id` | Org-scoped, not user-scoped |
| `User` | `User` | Same name, but now has `organization_id` + `role` |

### 4. Context Factory Updates

```csharp
public class OrgContextFactory : IDisposable
{
    private readonly DatabaseSettings _settings;
    private readonly Guid _userId;
    private readonly Guid _orgId;
    private readonly string _role;

    public TrackerDbContext CreateContext()
    {
        return new TrackerDbContext(_settings, _userId, _orgId, _role);
    }
}
```

---

## Migration Path

### Phase 1: Schema Foundation (Week 1)

1. **Create new database scripts** following Pro Causa pattern:
   - `00_CreateDatabase.sql` - Database + extensions (pgvector)
   - `01_CreateSchema_Organizations.sql` - Organizations, users
   - `02_CreateSchema_Employees.sql` - Employees, manager_history
   - `03_CreateSchema_Meetings.sql` - Meetings, tasks, etc.
   - `04_CreateSchema_Vectors.sql` - Vector embeddings table
   - `05_CreateRlsPolicies.sql` - All RLS policies
   - `06_CreateAdminUser.sql` - Default admin

2. **Update EF Core models** - Add Organization entity, update relationships

3. **Update RlsConnectionInterceptor** - Set org_id + role

### Phase 2: Application Integration (Week 2)

4. **Refactor VectorStore** - Create interface, PostgreSQL implementation

5. **Update AuthenticationManager** - Include org_id and role in auth flow

6. **Update Login flow** - Select organization (if user belongs to multiple)

7. **Update TrackerDbManager** - Org-aware queries

### Phase 3: Feature Updates (Week 3)

8. **Update ViewModels** - Update org-aware queries (TeamMember naming unchanged)

9. **Add Manager History tracking** - When manager changes

10. **Update AI/RAG** - Use new PostgresVectorStore

11. **Test team movement scenario** - Sarah moves from Brian to Alice

### Phase 4: SQL Server Support (Week 4, REQUIRED for V1)

12. **Create SQL Server schema scripts** - Mirror PostgreSQL structure

13. **Implement SqlServerVectorStore** - VARBINARY + app-side similarity

14. **Test SQL Server deployment** - Enterprise scenario

---

## Timeline Estimate

| Phase | Task | Effort |
|-------|------|--------|
| **1** | Database schema scripts | 4-6 hours |
| **1** | EF Core model updates | 2-3 hours |
| **1** | RLS policy updates | 2 hours |
| **2** | VectorStore refactor | 4-6 hours |
| **2** | Auth/Context factory updates | 3-4 hours |
| **2** | Login flow updates | 2-3 hours |
| **3** | ViewModel updates | 6-8 hours |
| **3** | Manager history feature | 2-3 hours |
| **3** | AI/RAG integration | 3-4 hours |
| **3** | Testing | 4-6 hours |
| **4** | SQL Server support (REQUIRED) | 8-10 hours |

**Total: ~45-60 hours (2-3 weeks focused work) - includes SQL Server support**

---

## Open Questions (Status)

1. **Multi-org users?** - Can a user belong to multiple organizations?
   - ✅ **ANSWERED: NO** - Single org per user, no crossover between orgs (separate billable entities)

2. **Organization creation?** - Who can create organizations?
   - Recommendation: Admin via SQL or separate admin tool for V1

3. **Data export on org delete?** - What happens to data when org is deleted?
   - Recommendation: Soft delete with 30-day retention

4. **Vector re-indexing?** - When to regenerate embeddings?
   - Recommendation: Background job on entity update + manual "refresh" button

5. **SQL Server availability?** - How do customers get SQL Server support?
   - ✅ **ANSWERED:** Must be available V1, likely requires service assistance/add-on

---

## Decision Checklist

- [x] ✅ Single org per user (no multi-org for V1) - **CONFIRMED: "single org - multiple users - but each user belongs to one org, no crossover"**
- [x] ✅ pgvector for PostgreSQL, VARBINARY for SQL Server - **CONFIRMED**
- [x] ✅ Keep TeamMember naming (don't rename to Employee) - **CONFIRMED: "leave it team member - fits the dynamic of the application"**
- [x] ✅ Remove local SQLite entirely - **CONFIRMED: "let's move to only postgres (or sql)"**
- [x] ✅ SQL Server must be available for V1 - **CONFIRMED: "postgres is default, but option for switching - will require service"**
- [ ] Manager history tracking from day 1
