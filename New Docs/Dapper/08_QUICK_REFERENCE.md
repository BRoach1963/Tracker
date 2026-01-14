# Dapper Quick Reference

**Print this page and keep it handy!**

---

## File Locations

| What | Where |
|------|-------|
| Connection Factory | `Services/Data/DapperConnectionFactory.cs` |
| Base Repository | `Services/Data/BaseRepository.cs` |
| Repository Interface | `Services/Data/IRepository.cs` |
| Entity Repositories | `Services/Data/Repositories/*.cs` |
| Business Services | `Services/*.cs` |
| Data Models | `DataModels/*.cs` |
| DI Registration | `Infrastructure/ServiceConfiguration.cs` |
| Supabase Auth | `Services/Backend/SupabaseService.cs` |

---

## Dapper Query Methods

```csharp
// Multiple rows
await connection.QueryAsync<T>(sql, parameters);

// Single row or null
await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);

// Single row (throws if none)
await connection.QueryFirstAsync<T>(sql, parameters);

// Execute (INSERT/UPDATE/DELETE) - returns affected rows
await connection.ExecuteAsync(sql, parameters);
```

---

## Repository Method Templates

### Get by ID
```csharp
public async Task<T?> GetByIdAsync(Guid id)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = "SELECT * FROM table WHERE id = @Id AND is_deleted = false LIMIT 1";
    return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
}
```

### Get by Filter
```csharp
public async Task<IEnumerable<T>> GetByFilterAsync(Guid orgId, string status)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        SELECT * FROM table 
        WHERE organization_id = @OrgId AND status = @Status AND is_deleted = false
        ORDER BY created_at DESC";
    return await connection.QueryAsync<T>(sql, new { OrgId = orgId, Status = status });
}
```

### Create
```csharp
public async Task<T> CreateAsync(T entity)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        INSERT INTO table (column1, column2, organization_id) 
        VALUES (@Column1, @Column2, @OrganizationId) 
        RETURNING *";
    return await connection.QueryFirstOrDefaultAsync<T>(sql, entity);
}
```

### Update
```csharp
public async Task<bool> UpdateAsync(T entity)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        UPDATE table SET column1 = @Column1, column2 = @Column2, updated_at = NOW() 
        WHERE id = @Id AND is_deleted = false";
    var rows = await connection.ExecuteAsync(sql, entity);
    return rows > 0;
}
```

### Soft Delete
```csharp
public async Task<bool> DeleteAsync(Guid id, Guid deletedByUserId)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        UPDATE table SET is_deleted = true, deleted_at = NOW(), deleted_by = @DeletedBy 
        WHERE id = @Id";
    var rows = await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedByUserId });
    return rows > 0;
}
```

---

## Connection String Format

```
Server=db.{project}.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password={password};SSL Mode=Require
```

---

## Standard Table Columns

Every table has:
- `id` (UUID) - Primary key
- `organization_id` (UUID) - FK to organizations
- `created_at` (timestamp) - Auto-set on create
- `created_by` (UUID) - FK to users
- `updated_at` (timestamp) - Set on update
- `updated_by` (UUID) - FK to users
- `is_deleted` (boolean) - Soft delete flag
- `deleted_at` (timestamp) - When deleted
- `deleted_by` (UUID) - Who deleted

---

## Type Mappings

| PostgreSQL | C# |
|------------|------|
| `uuid` | `Guid` |
| `text`, `varchar` | `string` |
| `integer` | `int` |
| `bigint` | `long` |
| `boolean` | `bool` |
| `timestamptz` | `DateTime` |
| `numeric` | `decimal` |

---

## DI Registration Pattern

```csharp
// In ServiceConfiguration.cs
services.AddScoped<IMyRepository, MyRepository>();
services.AddScoped<IMyService, MyService>();
```

---

## Common WHERE Patterns

```sql
-- Active records only
WHERE is_deleted = false

-- By organization
WHERE organization_id = @OrgId AND is_deleted = false

-- By status
WHERE status = @Status AND is_deleted = false

-- Search (case-insensitive)
WHERE title ILIKE @Query AND is_deleted = false
-- Use: new { Query = $"%{searchTerm}%" }

-- Date range
WHERE created_at >= @StartDate AND created_at <= @EndDate AND is_deleted = false

-- NULL check
WHERE parent_id IS NULL AND is_deleted = false

-- IN list
WHERE id = ANY(@Ids) AND is_deleted = false
-- Use: new { Ids = ids.ToArray() }
```

---

## Error Quick Fixes

| Error | Fix |
|-------|-----|
| Connection refused | Supabase paused? Check dashboard |
| SSL required | Add `SSL Mode=Require` |
| No service registered | Add to ServiceConfiguration.cs |
| Properties null | Check column name mapping |
| Empty results | Check is_deleted filter |

---

## Naming Convention Map

| Old Name | New Name | Table |
|----------|----------|-------|
| OKR | Goal | `goals` |
| KPI | Metric | `metrics` |
| KeyResult | Target | `targets` |
| OneOnOne | Meeting | `meetings` |

---

## Layer Responsibilities

```
ViewModel → Service → Repository → Database
    ↓          ↓           ↓
 UI Logic   Business   SQL Only
            Logic
```

**Rule:** SQL only in repositories. Never in ViewModels or Services.

---

## Logging Pattern

```csharp
try
{
    // operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error doing X with {Param}", param);
    throw;
}
```

---

## Documentation Index

1. [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)
2. [02_CONNECTION_MANAGEMENT.md](02_CONNECTION_MANAGEMENT.md)
3. [03_REPOSITORY_PATTERN.md](03_REPOSITORY_PATTERN.md)
4. [04_SUPABASE_AND_RLS.md](04_SUPABASE_AND_RLS.md)
5. [05_AUTHENTICATION_FLOW.md](05_AUTHENTICATION_FLOW.md)
6. [06_ADDING_NEW_ENTITIES.md](06_ADDING_NEW_ENTITIES.md)
7. [07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md)
