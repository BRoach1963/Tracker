# 07 – Interfaces

This document describes the **shared interfaces** defined in Tracker.Core.

---

## Repository Interfaces

### IRepository<T>
**File:** `Data/IRepository.cs`

Generic repository interface - every entity repository implements this.

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
    
    // Delete
    Task<bool> DeleteAsync(Guid id, Guid deletedByUserId);
    Task<bool> DeleteBatchAsync(IEnumerable<Guid> ids, Guid deletedByUserId);
    Task<bool> PermanentlyDeleteAsync(Guid id);
    
    // Existence
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
}
```

---

### IDapperConnectionFactory
**File:** `Data/DapperConnectionFactory.cs`

Single point of database connection creation.

```csharp
public interface IDapperConnectionFactory
{
    IDbConnection CreateConnection();
}
```

---

## Domain Interfaces

### IMeasurable
**File:** `Interfaces/IMeasurable.cs`

Entities that can have metrics tracked against them.

```csharp
public interface IMeasurable
{
    Guid Id { get; }
    string Title { get; }
    decimal? CurrentValue { get; }
    decimal? TargetValue { get; }
    string? Unit { get; }
}
```

Implemented by:
- `Metric`
- `Target`

---

### ITask
**File:** `Interfaces/ITask.cs`

Common interface for task-like entities.

```csharp
public interface ITask
{
    Guid Id { get; }
    string Title { get; }
    string? Description { get; }
    WorkItemStatus Status { get; }
    WorkItemPriority Priority { get; }
    DateTime? DueDate { get; }
    Guid? AssignedToId { get; }
}
```

Implemented by:
- `TrackerTask`
- `MeetingPrepItem`

---

### ICloseable
**File:** `Interfaces/ICloseable.cs`

Entities that can be closed/completed.

```csharp
public interface ICloseable
{
    bool IsClosed { get; }
    DateTime? ClosedAt { get; }
    void Close();
}
```

Implemented by:
- `TrackerTask`
- `Goal`
- `Project`

---

### IKpiSource
**File:** `Interfaces/IKpiSource.cs`

Entities that provide KPI/metric data.

```csharp
public interface IKpiSource
{
    Guid Id { get; }
    string Name { get; }
    decimal GetCurrentValue();
    decimal? GetTargetValue();
}
```

Implemented by:
- `Metric`
- `MetricDataSource`

---

### IChatProvider
**File:** `Interfaces/IChatProvider.cs`

AI/Chat service provider interface.

```csharp
public interface IChatProvider
{
    Task<string> SendMessageAsync(string message);
    Task<string> GetSummaryAsync(string content);
}
```

Used by AI services in UI project.

---

## Usage Patterns

### Repository Interface in DI
```csharp
// Registration (in UI project)
services.AddSingleton<IMeetingRepository, MeetingRepository>();

// Usage (in ViewModel/Service)
public class MeetingService
{
    private readonly IMeetingRepository _meetingRepo;
    
    public MeetingService(IMeetingRepository meetingRepo)
    {
        _meetingRepo = meetingRepo;
    }
}
```

### Checking Interface Implementation
```csharp
if (entity is IMeasurable measurable)
{
    var progress = measurable.CurrentValue / measurable.TargetValue;
}
```

---

## File Locations

| File | Interface |
|------|-----------|
| `Data/IRepository.cs` | IRepository<T> |
| `Data/IUnitOfWork.cs` | IUnitOfWork |
| `Data/DapperConnectionFactory.cs` | IDapperConnectionFactory |
| `Interfaces/IMeasurable.cs` | IMeasurable |
| `Interfaces/ITask.cs` | ITask |
| `Interfaces/ICloseable.cs` | ICloseable |
| `Interfaces/IKpiSource.cs` | IKpiSource |
| `Interfaces/IChatProvider.cs` | IChatProvider |

---

## Invariants

1. All repository methods are async (return `Task<T>`)
2. All IDs are `Guid`
3. Interfaces are in `Tracker.Core.Interfaces` namespace (except data interfaces)
4. No implementation details in interfaces

