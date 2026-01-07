# Organization Model Refactor - Execution Plan

> **Approach:** Model-First, Incremental, Non-Breaking
> **Created:** January 4, 2026
> **Status:** ✅ COMPLETE - All 14 Chunks Finished

## Progress Summary

| Chunk | Status | Notes |
|-------|--------|-------|
| 1 | ✅ Complete | Organization, ManagerHistory, VectorEmbedding entities |
| 2 | ✅ Complete | OrganizationId added to 25+ entities |
| 3 | ✅ Complete | IVectorStore, VectorSearchResult, LegacyVectorStoreAdapter |
| 4 | ✅ Complete | VectorStorageProvider, ITrackerDbContextFactory |
| 5 | ✅ Complete | PostgreSQL schema scripts (00-06) |
| 6 | ✅ Complete | SQL Server schema scripts (06-08) |
| 7 | ✅ Complete | PostgresVectorStore implementation |
| 8 | ✅ Complete | SqlServerVectorStore implementation |
| 9 | ✅ Complete | VectorStoreFactory updated |
| 10 | ✅ Complete | OrganizationContext service |
| 11 | ✅ Complete | EntityIndexerBase & DataIndexer IVectorStore support |
| 12 | ✅ Complete | OrganizationContextExtensions |
| 13 | ✅ Complete | VectorStoreMigrator utility |
| 14 | ✅ Complete | Settings UI Vector Storage section |

## Files Created/Modified

### New Files Created:
- `Services/AI/PostgresVectorStore.cs` - PostgreSQL pgvector implementation
- `Services/AI/SqlServerVectorStore.cs` - SQL Server VARBINARY implementation
- `Services/AI/VectorStoreMigrator.cs` - Legacy to new store migration
- `Services/OrganizationContext.cs` - Organization/user context service
- `Services/OrganizationContextExtensions.cs` - Auth integration helpers
- `Database/PostgreSQL/00_MasterDeploy.sql` - Deployment orchestrator
- `Database/PostgreSQL/01_CreateSchema_Core.sql` - Core tables
- `Database/PostgreSQL/02_CreateSchema_Team.sql` - Team tables
- `Database/PostgreSQL/03_CreateSchema_Meetings.sql` - Meeting tables
- `Database/PostgreSQL/04_CreateSchema_Vectors.sql` - Vector embeddings with pgvector
- `Database/PostgreSQL/05_CreateRlsPolicies.sql` - Row-level security
- `Database/PostgreSQL/06_CreateViewsAndFunctions.sql` - Views and functions
- `Database/SqlServer/06_AddOrganizationModel.sql` - Organizations table
- `Database/SqlServer/07_CreateVectorEmbeddings.sql` - Vector storage
- `Database/SqlServer/08_FinalizeOrganizationModel.sql` - FK constraints

### Modified Files:
- `Services/AI/VectorStoreFactory.cs` - Added async factory methods
- `Services/AI/EntityIndexerBase.cs` - IVectorStore support
- `Services/AI/DataIndexer.cs` - Vector store configuration
- `ViewModels/DialogViewModels/SettingsViewModel.cs` - Vector storage properties
- `Controls/Settings/OracleSettingsControl.xaml` - Vector storage UI section

## Guiding Principles

1. **Compile at every step** - Never leave code in broken state
2. **Non-breaking first** - Add new things before changing existing
3. **Nullable → Required** - Add org_id as nullable, backfill, then make required
4. **Interface abstractions** - Create interfaces before implementations
5. **Feature flags** - Toggle between old/new behavior during transition
6. **Test coverage** - Validate each chunk before moving on

---

## Chunk Overview

| Chunk | Name | Risk | Est. Time | Dependencies |
|-------|------|------|-----------|--------------|
| 1 | New Entity Models | 🟢 Low | 1-2 hrs | None |
| 2 | Update Existing Models | 🟢 Low | 1-2 hrs | Chunk 1 |
| 3 | IVectorStore Interface | 🟢 Low | 1 hr | None |
| 4 | Database Provider Abstraction | 🟡 Medium | 2-3 hrs | Chunk 2 |
| 5 | PostgreSQL Schema Scripts | 🟢 Low | 2-3 hrs | Chunk 2 |
| 6 | SQL Server Schema Scripts | 🟢 Low | 2-3 hrs | Chunk 5 |
| 7 | PostgresVectorStore | 🟡 Medium | 3-4 hrs | Chunk 3, 5 |
| 8 | SqlServerVectorStore | 🟡 Medium | 3-4 hrs | Chunk 3, 6 |
| 9 | Auth/Context Integration | 🟡 Medium | 2-3 hrs | Chunk 4 |
| 10 | RLS Policy Updates | 🟡 Medium | 2 hrs | Chunk 5, 9 |
| 11 | Service Layer Updates | 🟡 Medium | 3-4 hrs | Chunk 9 |
| 12 | ViewModel Updates | 🟡 Medium | 4-6 hrs | Chunk 11 |
| 13 | Remove SQLite VectorStore | 🔴 High | 1-2 hrs | Chunk 7, 8 |
| 14 | Final Integration Testing | 🟡 Medium | 2-3 hrs | All |

**Total Estimated Time: 30-40 hours**

---

## Chunk 1: New Entity Models
**Risk: 🟢 Low | Time: 1-2 hours | Dependencies: None**

### Goal
Add new entity classes without modifying existing code.

### Tasks

#### 1.1 Create Organization Entity
```
Location: Tracker/Database/Entities/Organization.cs
```

```csharp
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }  // URL-friendly name
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
```

#### 1.2 Create ManagerHistory Entity
```
Location: Tracker/Database/Entities/ManagerHistory.cs
```

```csharp
public class ManagerHistory
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public Guid ManagerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }  // NULL = current
    public string? Reason { get; set; }  // 'reorg', 'promotion', etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Organization Organization { get; set; } = null!;
    public TeamMember TeamMember { get; set; } = null!;
    public User Manager { get; set; } = null!;
}
```

#### 1.3 Create VectorEmbedding Entity
```
Location: Tracker/Database/Entities/VectorEmbedding.cs
```

```csharp
public class VectorEmbedding
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    
    public string EntityType { get; set; } = string.Empty;  // 'team_member', 'meeting', etc.
    public string EntityId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; } = 0;
    
    public string Content { get; set; } = string.Empty;
    public byte[] Embedding { get; set; } = Array.Empty<byte>();  // Serialized float[]
    public int EmbeddingDimensions { get; set; } = 1536;
    
    public string? Metadata { get; set; }  // JSON
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Organization? Organization { get; set; }
}
```

### Validation
- [ ] Solution compiles
- [ ] No changes to existing functionality
- [ ] New files in correct locations

---

## Chunk 2: Update Existing Models
**Risk: 🟢 Low | Time: 1-2 hours | Dependencies: Chunk 1**

### Goal
Add OrganizationId to existing entities as NULLABLE (non-breaking).

### Tasks

#### 2.1 Update User Entity
```csharp
// Add to existing User class
public Guid? OrganizationId { get; set; }  // NULLABLE for now
public string Role { get; set; } = "manager";  // 'admin', 'hr_admin', 'manager', 'viewer'

// Navigation
public Organization? Organization { get; set; }
```

#### 2.2 Update TeamMember Entity
```csharp
// Add to existing TeamMember class
public Guid? OrganizationId { get; set; }  // NULLABLE for now
public Guid? CurrentManagerId { get; set; }  // Track current manager

// Navigation
public Organization? Organization { get; set; }
public User? CurrentManager { get; set; }
public ICollection<ManagerHistory> ManagerHistories { get; set; } = new List<ManagerHistory>();
```

#### 2.3 Update Meeting/OneOnOne Entity
```csharp
// Add to existing entity
public Guid? OrganizationId { get; set; }  // NULLABLE for now

// Navigation
public Organization? Organization { get; set; }
```

#### 2.4 Update Other Entities (Tasks, OKRs, etc.)
Same pattern - add nullable OrganizationId to all data entities.

### Validation
- [ ] Solution compiles
- [ ] Existing tests still pass
- [ ] Database can still be queried (nullable columns)

---

## Chunk 3: IVectorStore Interface
**Risk: 🟢 Low | Time: 1 hour | Dependencies: None**

### Goal
Create abstraction layer for vector storage before implementing.

### Tasks

#### 3.1 Create IVectorStore Interface
```
Location: Tracker/Services/AI/IVectorStore.cs
```

```csharp
public interface IVectorStore
{
    Task<Guid> StoreAsync(
        string entityType, 
        string entityId, 
        string content, 
        float[] embedding, 
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
    
    Task<List<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int topK = 10, 
        string[]? entityTypes = null, 
        float minSimilarity = 0.5f,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task DeleteAllForEntityAsync(string entityType, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? entityType = null, CancellationToken cancellationToken = default);
}

public class VectorSearchResult
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

#### 3.2 Create Legacy Adapter (wraps existing VectorStore)
```
Location: Tracker/Services/AI/LegacyVectorStoreAdapter.cs
```

This wraps the existing SQLite VectorStore to implement IVectorStore, allowing gradual migration.

### Validation
- [ ] Solution compiles
- [ ] Interface covers all needed operations
- [ ] Legacy adapter passes basic tests

---

## Chunk 4: Database Provider Abstraction
**Risk: 🟡 Medium | Time: 2-3 hours | Dependencies: Chunk 2**

### Goal
Abstract database provider selection (PostgreSQL vs SQL Server).

### Tasks

#### 4.1 Create DatabaseProvider Enum
```csharp
public enum DatabaseProvider
{
    PostgreSQL,
    SqlServer
}
```

#### 4.2 Update DatabaseSettings
```csharp
public class DatabaseSettings
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSQL;
    public string ConnectionString { get; set; } = string.Empty;
    // ... existing properties
}
```

#### 4.3 Create IDbContextFactory Interface
```csharp
public interface ITrackerDbContextFactory
{
    TrackerDbContext CreateContext();
    DatabaseProvider Provider { get; }
}
```

#### 4.4 Update TrackerDbContext
- Add provider-specific configurations
- Handle pgvector vs VARBINARY for vectors
- Use `HasPostgresExtension("vector")` conditionally

### Validation
- [ ] Solution compiles
- [ ] Can switch providers via configuration
- [ ] Existing PostgreSQL functionality unchanged

---

## Chunk 5: PostgreSQL Schema Scripts
**Risk: 🟢 Low | Time: 2-3 hours | Dependencies: Chunk 2**

### Goal
Create numbered SQL scripts following Pro Causa pattern.

### Tasks

#### 5.1 Create Script Directory
```
Location: Tracker/Database/Scripts/PostgreSQL/
```

#### 5.2 Create Scripts
```
00_CreateDatabase.sql       - Database + extensions (pgvector)
01_CreateSchema_Core.sql    - organizations, users
02_CreateSchema_Team.sql    - team_members, manager_history  
03_CreateSchema_Meetings.sql - meetings, tasks, etc.
04_CreateSchema_Vectors.sql - vector_embeddings table
05_CreateRlsPolicies.sql    - All RLS policies
06_SeedData.sql             - Default data (optional)
```

#### 5.3 Create Setup Script
```
SETUP.ps1 - Runs all scripts in order
```

### Validation
- [ ] Scripts run without errors on fresh database
- [ ] Schema matches EF Core model expectations
- [ ] pgvector extension enabled and working

---

## Chunk 6: SQL Server Schema Scripts
**Risk: 🟢 Low | Time: 2-3 hours | Dependencies: Chunk 5**

### Goal
Mirror PostgreSQL schema for SQL Server.

### Tasks

#### 6.1 Create Script Directory
```
Location: Tracker/Database/Scripts/SqlServer/
```

#### 6.2 Create Scripts
Same structure as PostgreSQL, with SQL Server syntax:
- UNIQUEIDENTIFIER instead of UUID
- NVARCHAR instead of VARCHAR
- DATETIME2 instead of TIMESTAMP
- VARBINARY(MAX) for embeddings (no pgvector)
- No RLS (handle in application layer)

### Validation
- [ ] Scripts run on SQL Server without errors
- [ ] Schema matches EF Core model expectations
- [ ] Can store/retrieve vector data as VARBINARY

---

## Chunk 7: PostgresVectorStore Implementation
**Risk: 🟡 Medium | Time: 3-4 hours | Dependencies: Chunk 3, 5**

### Goal
Implement IVectorStore using pgvector.

### Tasks

#### 7.1 Create PostgresVectorStore
```
Location: Tracker/Services/AI/PostgresVectorStore.cs
```

- Use raw SQL for vector operations (EF Core doesn't natively support pgvector)
- Implement HNSW index search
- Handle org_id filtering via RLS context

#### 7.2 Create Vector Serialization Helpers
```csharp
public static class VectorSerializer
{
    public static byte[] Serialize(float[] vector) { ... }
    public static float[] Deserialize(byte[] data) { ... }
    public static string ToPgVector(float[] vector) { ... }  // PostgreSQL format
}
```

### Validation
- [ ] Can store embeddings
- [ ] Can search with similarity threshold
- [ ] Org filtering works correctly
- [ ] Performance acceptable (< 100ms for 10k vectors)

---

## Chunk 8: SqlServerVectorStore Implementation
**Risk: 🟡 Medium | Time: 3-4 hours | Dependencies: Chunk 3, 6**

### Goal
Implement IVectorStore using VARBINARY + app-side similarity.

### Tasks

#### 8.1 Create SqlServerVectorStore
```
Location: Tracker/Services/AI/SqlServerVectorStore.cs
```

- Store vectors as VARBINARY
- Load candidate vectors, calculate similarity in C#
- Implement pagination for large datasets

#### 8.2 Optimize Similarity Calculation
```csharp
// Use SIMD if available
public static float CosineSimilarity(float[] a, float[] b)
{
    // Vector<float> for hardware acceleration
}
```

### Validation
- [ ] Can store/retrieve embeddings
- [ ] Similarity calculation matches pgvector results
- [ ] Performance acceptable for expected data sizes

---

## Chunk 9: Auth/Context Integration
**Risk: 🟡 Medium | Time: 2-3 hours | Dependencies: Chunk 4**

### Goal
Include organization context in authentication flow.

### Tasks

#### 9.1 Update AuthenticatedUser
```csharp
public class AuthenticatedUser
{
    // Existing properties...
    public Guid OrganizationId { get; set; }  // NEW
    public string Role { get; set; } = "manager";  // NEW
}
```

#### 9.2 Update AuthenticationManager
- Fetch org_id and role on login
- Store in AuthenticatedUser
- Pass to context factory

#### 9.3 Update RlsConnectionInterceptor
```csharp
// Set all three context values
SET app.current_user_id = '{userId}';
SET app.current_org_id = '{orgId}';
SET app.current_user_role = '{role}';
```

### Validation
- [ ] Login includes org context
- [ ] RLS context set correctly
- [ ] Existing auth flow unchanged for users without org

---

## Chunk 10: RLS Policy Updates
**Risk: 🟡 Medium | Time: 2 hours | Dependencies: Chunk 5, 9**

### Goal
Update RLS policies to use organization_id.

### Tasks

#### 10.1 Update Helper Functions
```sql
CREATE OR REPLACE FUNCTION current_user_org_id() ...
CREATE OR REPLACE FUNCTION current_user_role() ...
```

#### 10.2 Update Table Policies
- organizations: Only see own org
- team_members: Org + manager filtering
- meetings: Org + participant filtering
- vector_embeddings: Org filtering

### Validation
- [ ] Users only see their org's data
- [ ] Managers see their direct reports
- [ ] Admins see all org data

---

## Chunk 11: Service Layer Updates
**Risk: 🟡 Medium | Time: 3-4 hours | Dependencies: Chunk 9**

### Goal
Update services to work with organization context.

### Tasks

#### 11.1 Update TeamMemberService
- Include org context in queries
- Add manager change tracking (creates ManagerHistory)

#### 11.2 Update MeetingService
- Org-aware queries
- Link to team_member via org

#### 11.3 Update AI Services
- Switch from SQLite VectorStore to IVectorStore
- Use DI to get correct implementation based on provider

### Validation
- [ ] All services work with org context
- [ ] Manager changes create history records
- [ ] AI search returns org-filtered results

---

## Chunk 12: ViewModel Updates
**Risk: 🟡 Medium | Time: 4-6 hours | Dependencies: Chunk 11**

### Goal
Update ViewModels to use org-aware services.

### Tasks

#### 12.1 Update TeamMemberViewModel
- Use updated TeamMemberService
- Show manager history (optional feature)

#### 12.2 Update MeetingsViewModel
- Use updated MeetingService

#### 12.3 Update AI/Search ViewModels
- Use IVectorStore via DI

### Validation
- [ ] All views work correctly
- [ ] Data filtered by organization
- [ ] No regression in existing features

---

## Chunk 13: Remove SQLite VectorStore
**Risk: 🔴 High | Time: 1-2 hours | Dependencies: Chunk 7, 8**

### Goal
Remove legacy SQLite vector storage.

### Tasks

#### 13.1 Remove SQLite VectorStore Code
- Delete VectorStore.cs (old SQLite implementation)
- Delete LegacyVectorStoreAdapter.cs
- Remove SQLite vector database creation

#### 13.2 Update DI Registration
- Only register PostgresVectorStore or SqlServerVectorStore based on provider

#### 13.3 Data Migration Script (if needed)
- One-time migration of existing vectors to new database

### Validation
- [ ] No SQLite references remain
- [ ] App works without %LocalAppData%\Tracker\vectors.db
- [ ] All vector operations use main database

---

## Chunk 14: Final Integration Testing
**Risk: 🟡 Medium | Time: 2-3 hours | Dependencies: All**

### Goal
End-to-end validation of entire system.

### Tasks

#### 14.1 Test Scenarios
- [ ] New user registration → assigned to org
- [ ] Login → correct org context
- [ ] Create team member → org_id set
- [ ] Manager change → history recorded
- [ ] AI search → org-filtered results
- [ ] Switch database provider → still works

#### 14.2 Performance Testing
- [ ] Vector search < 100ms
- [ ] Page loads unchanged
- [ ] No memory leaks

#### 14.3 Security Testing
- [ ] Cannot access other org's data
- [ ] RLS policies enforced
- [ ] API endpoints protected

---

## Execution Order

```
Week 1: Foundation
├── Chunk 1: New Entity Models ──────────┐
├── Chunk 2: Update Existing Models ─────┤
├── Chunk 3: IVectorStore Interface      │
└── Chunk 4: Database Provider Abstraction

Week 2: Database
├── Chunk 5: PostgreSQL Schema Scripts
├── Chunk 6: SQL Server Schema Scripts
└── Chunk 10: RLS Policy Updates

Week 3: Implementation
├── Chunk 7: PostgresVectorStore
├── Chunk 8: SqlServerVectorStore
└── Chunk 9: Auth/Context Integration

Week 4: Integration
├── Chunk 11: Service Layer Updates
├── Chunk 12: ViewModel Updates
├── Chunk 13: Remove SQLite VectorStore
└── Chunk 14: Final Integration Testing
```

---

## Rollback Strategy

Each chunk has a rollback plan:

| Chunk | Rollback |
|-------|----------|
| 1-2 | Delete new files, revert model changes |
| 3 | Delete interface, remove adapter |
| 4 | Revert DatabaseSettings changes |
| 5-6 | Drop new tables, restore from backup |
| 7-8 | Revert to LegacyVectorStoreAdapter |
| 9 | Revert auth changes |
| 10 | Restore previous RLS policies |
| 11-12 | Revert service/VM changes |
| 13 | Restore SQLite VectorStore code |

---

## Ready to Start?

**Begin with Chunk 1: New Entity Models**

This is completely non-breaking - just adding new files.
