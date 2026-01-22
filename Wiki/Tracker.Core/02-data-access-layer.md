# 02 – Data Access Layer

This document describes **how Tracker.Core accesses the Supabase PostgreSQL database**.

---

## Overview

Data access follows a layered pattern:

```
UI Layer (ViewModels/Services)
        │
        ▼
    IRepository<T>           ← Interface
        │
        ▼
    BaseRepository<T>        ← Generic CRUD
        │
        ▼
    EntityRepository         ← Entity-specific queries
        │
        ▼
    IDapperConnectionFactory ← Connection creation
        │
        ▼
    Npgsql (ADO.NET)         ← PostgreSQL driver
        │
        ▼
    Supabase PostgreSQL      ← Database
```

---

## Connection Flow

1. **UI requests data** via repository interface
2. **Repository creates connection** via `IDapperConnectionFactory`
3. **Connection executes SQL** via Dapper
4. **Results mapped to C# objects** via Dapper
5. **Connection disposed** (returned to pool)

---

## File Reference

| File | Purpose |
|------|---------|
| `DapperConnectionFactory.cs` | Creates PostgreSQL connections |
| `IRepository.cs` | Generic repository interface |
| `BaseRepository.cs` | Generic CRUD implementation |
| `Repositories/*.cs` | Entity-specific query methods |

---

## Connection Management

### DapperConnectionFactory

Single point of connection creation:

```csharp
public class DapperConnectionFactory : IDapperConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

### Connection String

Configured in `SupabaseConfig.cs`:
- Host: Supabase project database
- SSL: Required
- Pooling: Enabled (1-20 connections)

### Connection Lifecycle

Connections are:
- Created per-query (using statement)
- Automatically returned to pool
- Never held long-term

```csharp
// Correct pattern
using var connection = _connectionFactory.CreateConnection();
return await connection.QueryAsync<Meeting>(sql, parameters);
// Connection disposed here, returned to pool
```

---

## Repository Interface (IRepository<T>)

Every repository implements this interface:

```csharp
public interface IRepository<T> where T : class
{
    // Read
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetWhereSqlAsync(string whereSql, object? parameters);
    Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(...);
    
    // Create
    Task<T> CreateAsync(T entity);
    Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities);
    
    // Update
    Task<bool> UpdateAsync(T entity);
    Task<bool> UpdateBatchAsync(IEnumerable<T> entities);
    
    // Delete (soft)
    Task<bool> DeleteAsync(Guid id, Guid deletedByUserId);
    Task<bool> DeleteBatchAsync(IEnumerable<Guid> ids, Guid deletedByUserId);
    
    // Delete (hard - admin only)
    Task<bool> PermanentlyDeleteAsync(Guid id);
    
    // Existence
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
}
```

---

## BaseRepository<T>

Generic implementation of `IRepository<T>`:

### Key Features

1. **Dynamic INSERT** - Builds INSERT from entity properties
2. **Soft delete enforcement** - All queries filter `is_deleted = false`
3. **Structured logging** - All operations logged
4. **Exception handling** - Catches and re-throws with context

### Usage

Concrete repositories inherit and set `TableName`:

```csharp
public class GoalRepository : BaseRepository<Goal>, IGoalRepository
{
    public GoalRepository(IDapperConnectionFactory factory, ILogger<GoalRepository> logger)
        : base(factory, logger)
    {
        TableName = "goals";  // Must match database table name
    }
    
    // Add entity-specific methods here
}
```

---

## SQL Patterns

### Standard SELECT

```csharp
const string sql = @"
    SELECT * FROM goals
    WHERE organization_id = @OrgId
      AND is_deleted = false
    ORDER BY created_at DESC";
```

### JOIN with Related Data

```csharp
const string sql = @"
    SELECT m.*, tm.display_name as manager_name
    FROM meetings m
    LEFT JOIN team_members tm ON m.manager_team_member_id = tm.id
    WHERE m.id = @Id AND m.is_deleted = false";
```

### Parameterized Queries

Always use parameters, never string concatenation:

```csharp
// CORRECT
await connection.QueryAsync<Meeting>(sql, new { OrgId = orgId });

// WRONG - SQL injection risk
await connection.QueryAsync<Meeting>($"SELECT * FROM meetings WHERE org = '{orgId}'");
```

---

## Transaction Support

For multi-statement operations:

```csharp
using var connection = _connectionFactory.CreateConnection();
connection.Open();
using var transaction = connection.BeginTransaction();

try
{
    await connection.ExecuteAsync(sql1, params1, transaction);
    await connection.ExecuteAsync(sql2, params2, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## Dapper Features Used

| Feature | Usage |
|---------|-------|
| `QueryAsync<T>` | Select multiple rows |
| `QueryFirstOrDefaultAsync<T>` | Select single row |
| `ExecuteAsync` | INSERT/UPDATE/DELETE |
| `ExecuteScalarAsync<T>` | COUNT, EXISTS |
| Anonymous objects | Parameter passing |
| Column mapping | Via `[Column]` attribute |

---

## Column Mapping

Dapper maps columns to properties by name (case-insensitive).

For snake_case columns, use `[Column]` attribute:

```csharp
[Column("created_at")]
public DateTime CreatedAt { get; set; }

[Column("is_deleted")]
public bool IsDeleted { get; set; }
```

---

## Error Handling

Repositories catch exceptions and:
1. Log with context (entity type, ID, operation)
2. Re-throw (let caller handle)

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting {TableName} by ID {Id}", TableName, id);
    throw;
}
```

---

## Performance Considerations

1. **Connection pooling** - Enabled by default
2. **Async all the way** - No sync-over-async
3. **Projection when possible** - Don't SELECT * if you only need 3 columns
4. **Pagination** - Use `GetPagedAsync` for large result sets
5. **Batch operations** - Use `CreateBatchAsync`/`UpdateBatchAsync` for bulk

---

## Invariants

1. All queries filter `is_deleted = false` (unless explicitly querying deleted records)
2. All connections created via `IDapperConnectionFactory`
3. All connections disposed via `using` statement
4. No raw SQL concatenation - always parameterized
5. No business logic in repositories - queries only

