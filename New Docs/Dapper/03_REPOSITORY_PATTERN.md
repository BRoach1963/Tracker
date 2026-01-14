# Repository Pattern

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Prerequisites:** Read [02_CONNECTION_MANAGEMENT.md](02_CONNECTION_MANAGEMENT.md) first

---

## Overview

Repositories are the **ONLY** place in Tracker that contains SQL queries. They encapsulate all database operations and provide a clean API for services and ViewModels.

**Key Principles:**
- One repository per entity (User → UserRepository)
- All repositories inherit from `BaseRepository<T>`
- Custom queries are added as entity-specific methods
- Soft delete pattern: records are marked deleted, not removed

---

## Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                         IRepository<T>                             │
│                    (Generic CRUD Interface)                        │
│  • GetByIdAsync(id)    • CreateAsync(entity)    • DeleteAsync(id)  │
│  • GetAllAsync()       • UpdateAsync(entity)    • ExistsAsync(id)  │
│  • GetWhereSqlAsync()  • GetPagedAsync()        • CountAsync()     │
└────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────┐
│                       BaseRepository<T>                            │
│                   (Shared Implementation)                          │
│                                                                    │
│  Provides standard implementations for all IRepository methods.    │
│  Uses reflection to build INSERT/UPDATE statements dynamically.    │
│  All queries respect soft delete (is_deleted = false).             │
└────────────────────────────────────────────────────────────────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
          ┌─────────────────┐  ┌──────────────────┐  ┌─────────────────┐
          │  UserRepository │  │ MeetingRepository│  │  GoalRepository │
          │                 │  │                  │  │                 │
          │ • GetByEmail    │  │ • GetByUser      │  │ • GetByOwner    │
          │ • GetBySupabase │  │ • GetUpcoming    │  │ • GetActive     │
          │ • EmailExists   │  │ • GetByDateRange │  │ • GetByOrg      │
          └─────────────────┘  └──────────────────┘  └─────────────────┘
```

---

## The Base Repository

### File Location
`Tracker/Services/Data/BaseRepository.cs`

### Purpose
Provides standard CRUD operations that work for ANY entity. Concrete repositories inherit from this and add entity-specific queries.

### Key Properties

```csharp
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDapperConnectionFactory _connectionFactory;
    protected readonly ILogger<BaseRepository<T>> _logger;
    
    /// <summary>
    /// The table name in Supabase (e.g., "users", "meetings", "goals").
    /// Must be set by derived classes in constructor.
    /// </summary>
    protected string TableName { get; set; } = string.Empty;
}
```

### Constructor Pattern

Every repository sets its table name:

```csharp
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<UserRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "users";  // ← Table name set here
    }
}
```

---

## Standard CRUD Operations

### GetByIdAsync

```csharp
public virtual async Task<T?> GetByIdAsync(Guid id)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = "SELECT * FROM {0} WHERE id = @Id AND is_deleted = false LIMIT 1";
    var query = string.Format(sql, TableName);
    
    return await connection.QueryFirstOrDefaultAsync<T>(query, new { Id = id });
}
```

**Note:** Always checks `is_deleted = false` to respect soft deletes.

### GetAllAsync

```csharp
public virtual async Task<IEnumerable<T>> GetAllAsync()
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = "SELECT * FROM {0} WHERE is_deleted = false ORDER BY id DESC";
    var query = string.Format(sql, TableName);
    
    return await connection.QueryAsync<T>(query);
}
```

### CreateAsync

```csharp
public virtual async Task<T> CreateAsync(T entity)
{
    using var connection = _connectionFactory.CreateConnection();
    
    // Build dynamic INSERT from entity properties
    var properties = typeof(T).GetProperties()
        .Where(p => p.CanRead && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var columnNames = string.Join(", ", properties.Select(p => p.Name.ToLower()));
    var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
    var sql = $"INSERT INTO {TableName} ({columnNames}) VALUES ({parameterNames}) RETURNING *";

    return await connection.QueryFirstOrDefaultAsync<T>(sql, entity);
}
```

**Note:** Uses `RETURNING *` to get the inserted record with database-generated values (id, created_at, etc.).

### UpdateAsync

```csharp
public virtual async Task<bool> UpdateAsync(T entity)
{
    using var connection = _connectionFactory.CreateConnection();

    var properties = typeof(T).GetProperties()
        .Where(p => p.CanRead && p.CanWrite && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var setClause = string.Join(", ", properties.Select(p => $"{p.Name.ToLower()} = @{p.Name}"));
    var sql = $"UPDATE {TableName} SET {setClause}, updated_at = NOW() WHERE id = @Id AND is_deleted = false";

    var result = await connection.ExecuteAsync(sql, entity);
    return result > 0;
}
```

**Note:** Automatically sets `updated_at = NOW()`.

### DeleteAsync (Soft Delete)

```csharp
public virtual async Task<bool> DeleteAsync(Guid id, Guid deletedByUserId)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        UPDATE {0} 
        SET is_deleted = true, deleted_at = NOW(), deleted_by = @DeletedBy 
        WHERE id = @Id";
    var query = string.Format(sql, TableName);
    
    var result = await connection.ExecuteAsync(query, new { Id = id, DeletedBy = deletedByUserId });
    return result > 0;
}
```

**Key Point:** We NEVER hard delete records. All deletes set:
- `is_deleted = true`
- `deleted_at = NOW()`
- `deleted_by = {userId}`

---

## Query Methods

### GetWhereSqlAsync

Execute custom WHERE clauses:

```csharp
public virtual async Task<IEnumerable<T>> GetWhereSqlAsync(string whereSql, object? parameters = null)
{
    using var connection = _connectionFactory.CreateConnection();
    var sql = $"SELECT * FROM {TableName} WHERE ({whereSql}) AND is_deleted = false";
    
    return await connection.QueryAsync<T>(sql, parameters);
}
```

**Usage:**
```csharp
var activeGoals = await _goalRepository.GetWhereSqlAsync(
    "owner_id = @OwnerId AND status = @Status",
    new { OwnerId = userId, Status = "active" });
```

### GetPagedAsync

Paginated results with ordering:

```csharp
public virtual async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    string orderBySql = "id DESC",
    string? whereSql = null,
    object? parameters = null)
{
    using var connection = _connectionFactory.CreateConnection();

    var offset = (pageNumber - 1) * pageSize;
    var baseWhere = "is_deleted = false";
    var fullWhere = string.IsNullOrEmpty(whereSql) ? baseWhere : $"({whereSql}) AND {baseWhere}";

    // Get total count
    var countSql = $"SELECT COUNT(*) FROM {TableName} WHERE {fullWhere}";
    var totalCount = await connection.QueryFirstAsync<int>(countSql, parameters);

    // Get paged results
    var dataSql = $"SELECT * FROM {TableName} WHERE {fullWhere} ORDER BY {orderBySql} LIMIT @PageSize OFFSET @Offset";
    var items = await connection.QueryAsync<T>(dataSql, 
        MergeParameters(parameters, new { PageSize = pageSize, Offset = offset }));

    return (items, totalCount);
}
```

**Usage:**
```csharp
var (meetings, total) = await _meetingRepository.GetPagedAsync(
    pageNumber: 1,
    pageSize: 20,
    orderBySql: "scheduled_at DESC",
    whereSql: "organizer_id = @UserId",
    parameters: new { UserId = currentUserId });
```

---

## Entity-Specific Repositories

Each entity has its own repository with custom query methods.

### Example: UserRepository

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetBySupabaseIdAsync(Guid supabaseId);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetByOrganizationAsync(Guid organizationId);
    Task<bool> EmailExistsAsync(string email);
}

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<UserRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "users";
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT * FROM users 
            WHERE email = @Email AND is_deleted = false 
            LIMIT 1";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(*) FROM users 
            WHERE email = @Email AND is_deleted = false";

        var count = await connection.QueryFirstAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
```

### Example: MeetingRepository

```csharp
public interface IMeetingRepository : IRepository<Meeting>
{
    Task<IEnumerable<Meeting>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Meeting>> GetUpcomingByUserAsync(Guid userId, DateTime fromDate);
    Task<IEnumerable<Meeting>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Meeting>> GetByStatusAsync(string status);
}

public class MeetingRepository : BaseRepository<Meeting>, IMeetingRepository
{
    public MeetingRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<MeetingRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "meetings";
    }

    public async Task<IEnumerable<Meeting>> GetByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT DISTINCT m.* FROM meetings m
            LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
            WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
              AND m.is_deleted = false
            ORDER BY m.scheduled_at DESC";

        return await connection.QueryAsync<Meeting>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Meeting>> GetUpcomingByUserAsync(Guid userId, DateTime fromDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT DISTINCT m.* FROM meetings m
            LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
            WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
              AND m.is_deleted = false
              AND m.scheduled_at >= @FromDate
            ORDER BY m.scheduled_at ASC";

        return await connection.QueryAsync<Meeting>(sql, 
            new { UserId = userId, FromDate = fromDate });
    }
}
```

---

## The 12 Gold Standard Repositories

These are the repositories we maintain:

| Repository | Entity | Table | Key Methods |
|------------|--------|-------|-------------|
| `UserRepository` | User | `users` | GetByEmail, GetBySupabaseId |
| `TeamMemberRepository` | TeamMember | `team_members` | GetByOrganization, GetByManager |
| `MeetingRepository` | Meeting | `meetings` | GetByUser, GetUpcoming, GetByDateRange |
| `TaskRepository` | TrackerTask | `tasks` | GetByOwner, GetByGoal, GetByProject |
| `GoalRepository` | Goal | `goals` | GetByOwner, GetActive, GetByOrganization |
| `MetricRepository` | Metric | `metrics` | GetByOwner, RecordValue, GetHistory |
| `FeedbackRepository` | Feedback | `feedback` | GetForTeamMember, GetFromTeamMember |
| `ProjectRepository` | Project | `projects` | GetByOrganization, GetByOwner |
| `QuickNoteRepository` | QuickNote | `quick_notes` | GetByTeamMember, GetByMeeting |
| `DevelopmentGoalRepository` | DevelopmentGoal | `development_goals` | GetByTeamMember, GetByStatus |
| `PerformanceReviewRepository` | PerformanceReview | `performance_reviews` | GetByTeamMember, GetByReviewCycle |
| `PulseSurveyRepository` | PulseSurvey | `pulse_surveys` | GetByOrganization, GetResponses |

**Rule:** For tables NOT in this list (e.g., `activity_log`, `vector_embeddings`), use raw SQL in services when needed. Don't create repositories for infrastructure tables.

---

## Best Practices

### DO:
- ✅ Use parameterized queries (`@ParameterName`) to prevent SQL injection
- ✅ Always include `is_deleted = false` in WHERE clauses
- ✅ Log errors with entity context (`_logger.LogError(ex, "Error getting user {UserId}", id)`)
- ✅ Use `LIMIT 1` when expecting a single result
- ✅ Dispose connections with `using var connection = ...`

### DON'T:
- ❌ Write SQL outside of repositories
- ❌ Use string concatenation for SQL parameters (`WHERE email = '" + email + "'"`)
- ❌ Forget soft delete checks
- ❌ Create repositories for junction/infrastructure tables
- ❌ Put business logic in repositories (that belongs in services)

---

## Dapper Query Methods Reference

| Method | Returns | Use When |
|--------|---------|----------|
| `QueryAsync<T>()` | `IEnumerable<T>` | Multiple rows expected |
| `QueryFirstOrDefaultAsync<T>()` | `T?` | Single row or null |
| `QueryFirstAsync<T>()` | `T` | Single row, throws if none |
| `QuerySingleOrDefaultAsync<T>()` | `T?` | Exactly one or none |
| `ExecuteAsync()` | `int` (affected rows) | INSERT/UPDATE/DELETE |

---

## Next Steps

**Next:** Read [04_SUPABASE_AND_RLS.md](04_SUPABASE_AND_RLS.md) to understand how Supabase and Row-Level Security work with our repositories.
