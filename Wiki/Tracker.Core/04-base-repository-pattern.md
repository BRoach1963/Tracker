# 04 – Base Repository Pattern

This document describes the **BaseRepository<T>** class and how to create entity repositories.

---

## Overview

`BaseRepository<T>` provides generic CRUD operations for any entity type.

Concrete repositories inherit from it and:
1. Set the `TableName` property
2. Add entity-specific query methods

---

## Class Definition

```csharp
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDapperConnectionFactory _connectionFactory;
    protected readonly ILogger<BaseRepository<T>> _logger;
    protected string TableName { get; set; } = string.Empty;
    
    protected BaseRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<BaseRepository<T>> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
}
```

---

## Standard Operations

### GetByIdAsync
```csharp
public virtual async Task<T?> GetByIdAsync(Guid id)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = "SELECT * FROM {0} WHERE id = @Id AND is_deleted = false LIMIT 1";
    return await connection.QueryFirstOrDefaultAsync<T>(
        string.Format(sql, TableName), 
        new { Id = id });
}
```

### GetAllAsync
```csharp
public virtual async Task<IEnumerable<T>> GetAllAsync()
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = "SELECT * FROM {0} WHERE is_deleted = false ORDER BY id DESC";
    return await connection.QueryAsync<T>(string.Format(sql, TableName));
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

### UpdateAsync
Updates all non-ID columns:
```csharp
public virtual async Task<bool> UpdateAsync(T entity)
{
    // Builds: UPDATE table SET col1=@Col1, col2=@Col2 WHERE id=@Id
}
```

### DeleteAsync (Soft Delete)
```csharp
public virtual async Task<bool> DeleteAsync(Guid id, Guid deletedByUserId)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        UPDATE {0} 
        SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy 
        WHERE id = @Id";
    
    var rows = await connection.ExecuteAsync(
        string.Format(sql, TableName),
        new { Id = id, DeletedAt = DateTime.UtcNow, DeletedBy = deletedByUserId });
    
    return rows > 0;
}
```

### PermanentlyDeleteAsync (Hard Delete)
```csharp
public virtual async Task<bool> PermanentlyDeleteAsync(Guid id)
{
    // WARNING: Only for test cleanup or admin operations
    const string sql = "DELETE FROM {0} WHERE id = @Id";
    // ...
}
```

---

## Creating a New Repository

### Step 1: Define Interface
```csharp
public interface IGoalRepository : IRepository<Goal>
{
    Task<IEnumerable<Goal>> GetByOwnerAsync(Guid ownerTeamMemberId);
    Task<IEnumerable<Goal>> GetByOrganizationAsync(Guid organizationId);
    Task<IEnumerable<Goal>> GetActiveGoalsAsync(Guid organizationId);
}
```

### Step 2: Implement Repository
```csharp
public class GoalRepository : BaseRepository<Goal>, IGoalRepository
{
    public GoalRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<GoalRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "goals";  // MUST match database table name
    }
    
    public async Task<IEnumerable<Goal>> GetByOwnerAsync(Guid ownerTeamMemberId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT * FROM goals
            WHERE owner_team_member_id = @OwnerId
              AND is_deleted = false
            ORDER BY created_at DESC";
        
        return await connection.QueryAsync<Goal>(sql, new { OwnerId = ownerTeamMemberId });
    }
    
    // ... other methods
}
```

### Step 3: Register in DI (in UI project)
```csharp
services.AddSingleton<IGoalRepository, GoalRepository>();
```

---

## Query Patterns

### Organization-Scoped Query
```csharp
const string sql = @"
    SELECT * FROM goals
    WHERE organization_id = @OrgId
      AND is_deleted = false
    ORDER BY title";
```

### Date Range Query
```csharp
const string sql = @"
    SELECT * FROM meetings
    WHERE scheduled_at >= @StartDate
      AND scheduled_at <= @EndDate
      AND is_deleted = false";
```

### JOIN Query
```csharp
const string sql = @"
    SELECT m.*, tm.display_name as assignee_name
    FROM metrics m
    LEFT JOIN team_members tm ON m.assigned_team_member_id = tm.id
    WHERE m.organization_id = @OrgId
      AND m.is_deleted = false";
```

### EXISTS Check
```csharp
const string sql = @"
    SELECT EXISTS(
        SELECT 1 FROM goals 
        WHERE id = @Id AND is_deleted = false
    )";
return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
```

### COUNT Query
```csharp
const string sql = @"
    SELECT COUNT(*) FROM tasks
    WHERE organization_id = @OrgId
      AND status = @Status
      AND is_deleted = false";
return await connection.ExecuteScalarAsync<int>(sql, new { OrgId = orgId, Status = status });
```

---

## Pagination Support

```csharp
public async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    string orderBySql = "id DESC",
    string? whereSql = null,
    object? parameters = null)
{
    // Gets items + total count in single query
    // Returns tuple for pagination UI
}
```

Usage:
```csharp
var (meetings, total) = await _meetingRepo.GetPagedAsync(
    pageNumber: 1,
    pageSize: 20,
    orderBySql: "scheduled_at DESC",
    whereSql: "organization_id = @OrgId",
    parameters: new { OrgId = orgId });
```

---

## Batch Operations

### CreateBatchAsync
```csharp
public virtual async Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities)
{
    using var connection = _connectionFactory.CreateConnection();
    connection.Open();
    using var transaction = connection.BeginTransaction();
    
    var results = new List<T>();
    foreach (var entity in entities)
    {
        var result = await connection.QueryFirstOrDefaultAsync<T>(insertSql, entity, transaction);
        results.Add(result);
    }
    
    transaction.Commit();
    return results;
}
```

### DeleteBatchAsync
```csharp
public virtual async Task<bool> DeleteBatchAsync(IEnumerable<Guid> ids, Guid deletedByUserId)
{
    // Soft deletes all IDs in a single UPDATE
    const string sql = @"
        UPDATE {0}
        SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
        WHERE id = ANY(@Ids)";
}
```

---

## Error Handling

All operations:
1. Wrap in try/catch
2. Log error with context
3. Re-throw exception

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting {TableName} by ID {Id}", TableName, id);
    throw;
}
```

Caller (service/viewmodel) decides how to handle the error.

---

## Existing Repositories

| Repository | Table | Purpose |
|------------|-------|---------|
| `MeetingRepository` | meetings | All meeting types |
| `GoalRepository` | goals | OKRs/Objectives |
| `MetricRepository` | metrics | KPIs/Measures |
| `TargetRepository` | targets | Goal key results |
| `TaskRepository` | tracker_tasks | Action items |
| `TeamMemberRepository` | team_members | Org members |
| `UserRepository` | users | Auth users |
| `ProjectRepository` | projects | Projects |
| `FeedbackRepository` | feedback | Performance feedback |
| `InsightRepository` | insights | AI insights |
| `KudosRepository` | kudos | Recognition |
| `QuickNoteRepository` | quick_notes | Notes |
| `ReminderRepository` | reminders | Reminders |
| `PulseSurveyRepository` | pulse_surveys | Surveys |
| `MeetingTemplateRepository` | meeting_templates | Templates |
| `DevelopmentGoalRepository` | development_goals | Growth goals |
| `TaskCollectionRepository` | task_collections | Task groups |

---

## Invariants

1. `TableName` MUST be set in constructor
2. All queries MUST filter `is_deleted = false` (unless explicitly querying deleted)
3. All queries MUST use parameterized SQL
4. All connections MUST be disposed via `using`
5. Repositories NEVER contain business logic

