# 01 – Architecture Overview

This document describes the **architectural role and structure** of Tracker.Core.

---

## Purpose

Tracker.Core is the **shared foundation layer** for all Tracker UI projects.

It provides:
- Data models (C# classes mapping to Supabase tables)
- Repository interfaces and implementations (Dapper-based data access)
- Shared enums and interfaces
- Supabase connection configuration

It does NOT provide:
- UI code (no WPF, no Avalonia, no XAML)
- Business logic or workflows
- ViewModels or Commands
- State management

---

## Project Dependencies

```
┌─────────────────────────────────────┐
│     ProCohere.Avalonia (UI)         │
│     - Views, ViewModels             │
│     - Services (business logic)     │
│     - Managers (state)              │
└──────────────┬──────────────────────┘
               │ references
               ▼
┌─────────────────────────────────────┐
│         Tracker.Core                │
│     - DataModels (entities)         │
│     - Repositories (data access)    │
│     - Interfaces                    │
│     - Enums                         │
└──────────────┬──────────────────────┘
               │ uses
               ▼
┌─────────────────────────────────────┐
│     Supabase PostgreSQL             │
│     (via Dapper + Npgsql)           │
└─────────────────────────────────────┘
```

---

## Folder Structure

```
Tracker.Core/
├── Common/
│   └── Enums/              # All enum definitions
├── Data/
│   ├── BaseRepository.cs   # Generic CRUD base class
│   ├── DapperConnectionFactory.cs  # Connection creation
│   ├── IRepository.cs      # Repository interface
│   ├── IUnitOfWork.cs      # Transaction support (optional)
│   └── Repositories/       # Entity-specific repositories
├── DataModels/             # C# classes for database entities
├── Interfaces/             # Shared interfaces (IMeasurable, etc.)
├── Services/
│   └── Backend/
│       └── SupabaseConfig.cs  # Connection strings
└── Tracker.Core.csproj
```

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Dapper | 2.1.35 | Micro-ORM for SQL mapping |
| Npgsql | 8.0.5 | PostgreSQL ADO.NET provider |
| supabase-csharp | 0.16.2 | Supabase client (auth, storage) |

---

## Key Design Decisions

### 1. Dapper Over EF Core
We use Dapper (micro-ORM) instead of Entity Framework Core because:
- Direct SQL control for complex queries
- Better performance for read-heavy workloads
- Explicit query visibility (no "magic" queries)
- Simpler debugging

### 2. Repository Pattern
Every database table has a corresponding repository:
- `IRepository<T>` defines the interface
- `BaseRepository<T>` provides generic CRUD
- Entity repositories add entity-specific queries

### 3. Soft Delete Only
All deletes are soft deletes:
- `is_deleted = true`
- `deleted_at = UTC timestamp`
- `deleted_by = user ID`

Hard deletes only for test cleanup or admin operations.

### 4. UTC Timestamps
All timestamps are UTC. Conversion to local time happens only in the UI layer.

### 5. GUIDs for All IDs
All entity IDs are `Guid` (UUID in PostgreSQL). No integer auto-increment IDs.

---

## Patterns Used

### AuditableEntity Base Class
All entities inherit from `AuditableEntity`:
```csharp
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

### Repository Construction
Repositories require `IDapperConnectionFactory` and `ILogger`:
```csharp
public class MeetingRepository : BaseRepository<Meeting>, IMeetingRepository
{
    public MeetingRepository(
        IDapperConnectionFactory connectionFactory,
        ILogger<MeetingRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "meetings";
    }
}
```

### Query Pattern
All queries filter by `is_deleted = false`:
```csharp
const string sql = @"
    SELECT * FROM meetings
    WHERE organization_id = @OrgId
      AND is_deleted = false
    ORDER BY scheduled_at DESC";
```

---

## What Goes Where

| If you need to... | Put it in... |
|-------------------|--------------|
| Map a database table | `DataModels/` |
| Query the database | `Data/Repositories/` |
| Define a status/type | `Common/Enums/` |
| Share an interface | `Interfaces/` |
| Configure connections | `Services/Backend/` |

---

## Invariants

These rules are NEVER violated:

1. No UI code in Tracker.Core
2. No business logic in repositories (queries only)
3. All queries respect soft delete
4. All timestamps are UTC
5. All IDs are GUIDs
6. Repositories never throw business exceptions (only data exceptions)

