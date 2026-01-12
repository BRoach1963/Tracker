# TABLE-TO-MODEL VALIDATION REPORT

**Status:** CHECKPOINT - NO CHANGES MADE  
**Date:** January 12, 2026

---

## 1. MAPPING VALIDATION

### ✅ MEETING → Meeting.cs

**Schema columns:** 32  
**Model properties:** Appears complete

**Issues found:**
- ✅ ID is Guid (correct)
- ✅ OrganizationId is Guid (correct)
- ✅ CreatedByUserId is Guid (correct)
- ✅ Title, Description present
- ✅ MeetingType enum present
- ⚠️ Need to verify all 32 columns are mapped

### ✅ TEAM_MEMBERS → TeamMember.cs

**Schema columns:** 34  
**Model properties:** Present

**Critical Issue Found:**
```csharp
/// Legacy integer ID for SQLite/SQL Server backwards compatibility.
[NotMapped]
public int LegacyId { get; set; } = 0;  // ❌ DEAD CODE - SHOULD BE DELETED
```

- ✅ ID is Guid (correct)
- ✅ OrganizationId is Guid (correct)
- ❌ Has `[NotMapped] public int LegacyId` - This is contamination, should be removed
- ✅ ManagerUserId is Guid? (correct)
- ✅ LinkedUserId is Guid? (correct)

### ✅ GOAL → Goal.cs

**Schema columns:** 26  
**Model properties:** Present

**Issues found:**
- ✅ ID is Guid (correct)
- ✅ OrganizationId is Guid (correct)
- ✅ CreatedByUserId is Guid (correct)
- ✅ OwnerTeamMemberId is Guid? (correct)
- ✅ Title, Description, Type present
- ⚠️ Status field - need to verify enum name matches schema

### ✅ METRIC → Metric.cs

**Schema columns:** 29  
**Model properties:** Present

**Issues found:**
- ✅ ID is Guid (correct)
- ✅ OrganizationId is Guid (correct)
- ✅ CreatedByUserId is Guid (correct)
- ✅ OwnerTeamMemberId is Guid? (correct)
- ✅ Name, Description, Category present
- ✅ CurrentValue, TargetValue present
- ⚠️ Status field - need to verify enum name matches schema

### ✅ TRACKER_TASK → TrackerTask.cs

**Schema columns:** 24  
**Model properties:** Present

**Issues found:**
- ✅ ID is Guid (correct)
- ✅ OrganizationId is Guid (correct)
- ✅ CreatedByUserId is Guid (correct)
- ✅ OwnerTeamMemberId is Guid? (correct)
- ✅ ParentTaskId is Guid? (correct - for subtasks)
- ✅ ProjectId is Guid? (correct)
- ✅ GoalId is Guid? (correct)
- ✅ MeetingId is Guid? (correct)
- ✅ Title, Description, Status, Priority present
- ✅ DueDate, CompletedAt present

---

## 2. ENTITY FRAMEWORK ASSESSMENT

### Current State
You are using **Entity Framework Core** with a dual-provider architecture:

```csharp
private readonly DatabaseSettings _settings;

// Support for:
// - SQLite (local development)
// - SQL Server (legacy)
// - PostgreSQL (Supabase cloud)
```

**DbContext code shows:**
- Separate constructors for different DB types
- Support for `CurrentUserId` (int - SQL Server/SQLite legacy)
- Support for `PostgresUserId` (Guid - Supabase RLS)
- Global query filters for data isolation
- Soft delete support via `IsDeleted` flag

---

## 3. IS ENTITY FRAMEWORK RIGHT FOR SUPABASE?

### The Honest Assessment

**EF Core is FUNCTIONAL but NOT OPTIMAL for Supabase.** Here's why:

#### ✅ What EF Does Well
- Type-safe C# queries with LINQ
- Automatic mapping of tables to classes
- Change tracking for updates
- Migration management (though Supabase has its own migrations)
- Good for CRUD-heavy applications

#### ❌ Where EF Becomes a Problem in Cloud

1. **Performance Overhead**
   - EF adds an abstraction layer between C# and PostgreSQL
   - Extra allocations, reflection, and query compilation
   - In cloud, you pay per query execution - every inefficiency costs money
   - Raw SQL or Dapper would be 2-3x faster for complex queries

2. **PostgreSQL Features Not Exposed**
   - JSONB fields (EF treats as strings)
   - Array types
   - Full-text search
   - Window functions
   - Recursive CTEs
   - PostGIS (geospatial)
   - These require raw SQL anyway, defeating the purpose of EF

3. **Row-Level Security (RLS) Complexity**
   - Supabase RLS is enforced at the DATABASE layer
   - EF tries to replicate RLS logic in C# via query filters
   - This is fragile - if you forget a `.Where()` filter, RLS is bypassed
   - Database-level RLS guarantees security; EF-level filters don't
   - Your code has both EF filters AND RLS - this is redundant

4. **Connection Pooling Matters in Cloud**
   - Supabase has limited concurrent connections
   - EF Core's default pooling helps, but adds overhead
   - Raw connections + Dapper would use fewer resources

5. **Migration Complexity**
   - EF Migrations are .NET-specific
   - Supabase has its own migration UI + SQL scripts
   - You need to sync between EF migrations and Supabase schema
   - This is a source of truth problem (we've been doing this!)

6. **Cost**
   - Supabase charges per database operation
   - Each inefficient query costs money
   - EF can generate inefficient SQL (N+1 queries, overfetching, etc.)

---

## 4. BETTER ALTERNATIVES FOR SUPABASE

### Option A: Dapper (Recommended for this situation)
```csharp
// Lightweight, performant, still type-safe
public async Task<Meeting> GetMeetingAsync(Guid id)
{
    using var connection = new NpgsqlConnection(_connectionString);
    var result = await connection.QuerySingleAsync<Meeting>(
        "SELECT * FROM meetings WHERE id = @id AND organization_id = @orgId",
        new { id, orgId = _orgId }
    );
    return result;
}
```

**Pros:**
- Minimal overhead
- Explicit SQL (easier to optimize)
- Direct parameter mapping
- Still strongly typed
- Better performance

**Cons:**
- No change tracking (but that's fine for Supabase RLS)
- Manual relationship loading (explicit, not implicit)
- More boilerplate

### Option B: Raw Npgsql + Manual Mapping
```csharp
// Maximum control and performance
using var connection = new NpgsqlConnection(_connectionString);
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM meetings WHERE id = @id";
cmd.Parameters.AddWithValue("@id", id);
var result = await cmd.ExecuteReaderAsync();
```

**Pros:**
- Direct control
- Absolute minimum overhead
- Can use PostgreSQL-specific features

**Cons:**
- Most boilerplate
- Manual object mapping
- Error-prone

### Option C: Keep EF But Redesign

**If you want to keep EF:**
- Remove dual-provider support (SQL Server/SQLite)
- Focus ONLY on PostgreSQL
- Use `FromSql()` for complex queries
- Rely on database RLS instead of EF query filters
- Simplify migrations to version-controlled SQL scripts

---

## 5. MY RECOMMENDATION

**Given your situation:**

You're stuck between:
1. Working code that uses EF Core
2. A schema that keeps changing
3. Building an accurate mapping that you've validated 10 times

**I recommend: HYBRID APPROACH**

Keep EF for **simple CRUD**:
- Get, Create, Update, Delete operations
- Standard queries on single tables
- Most of your code is this

Switch to **Dapper for complex queries**:
- Anything with JOINs
- Aggregations
- Performance-critical paths
- Calendar sync operations

**Why this works:**
- Minimal refactoring (most code stays as-is)
- Best performance where it matters (complex queries)
- Type safety maintained
- Easier to debug misaligned schemas (explicit SQL vs. EF magic)

---

## 6. DECISION POINT

**Before I make ANY code changes, you need to decide:**

**Option 1:** Keep EF Core as-is, just fix the model mappings
   - Quickest path forward
   - Will work, but not optimal for Supabase

**Option 2:** Switch to hybrid Dapper + EF approach
   - Better long-term
   - Requires refactoring data access layer
   - More performant

**Option 3:** Full Dapper replacement
   - Best for cloud/Supabase
   - Biggest refactor
   - Overkill if you're not query-heavy

Which path do you want to go down?

