# Dapper Architecture Overview

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Audience:** Junior developers, new team members, anyone maintaining Tracker

---

## What is This Document?

This document explains how Tracker connects to and communicates with its database using Dapper and Supabase PostgreSQL. After reading this, you should understand:

1. **Why** we use Dapper instead of Entity Framework Core
2. **How** data flows from UI to database and back
3. **Where** each piece of code lives and what it does
4. **When** to modify each layer

---

## The Big Picture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              TRACKER WPF APP                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐    ┌──────────────────┐    ┌────────────────────────────┐ │
│  │    VIEWS     │───▶│   VIEW MODELS    │───▶│    BUSINESS SERVICES       │ │
│  │  (XAML/UI)   │    │  (C# Commands)   │    │  (IUserService, etc.)      │ │
│  └──────────────┘    └──────────────────┘    └────────────────────────────┘ │
│                                                           │                  │
│                                                           ▼                  │
│                             ┌────────────────────────────────────────────┐   │
│                             │         DAPPER REPOSITORIES                │   │
│                             │   (IUserRepository, IMeetingRepository)    │   │
│                             │                                            │   │
│                             │   • BaseRepository<T> (shared CRUD)        │   │
│                             │   • Entity-specific methods                │   │
│                             └────────────────────────────────────────────┘   │
│                                                           │                  │
│                                                           ▼                  │
│                             ┌────────────────────────────────────────────┐   │
│                             │       DapperConnectionFactory              │   │
│                             │   (Creates NpgsqlConnection to Supabase)   │   │
│                             └────────────────────────────────────────────┘   │
│                                                           │                  │
└───────────────────────────────────────────────────────────│──────────────────┘
                                                            │
                                                            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SUPABASE POSTGRESQL                                │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                     Row-Level Security (RLS)                          │   │
│  │           Enforced at database level - users only see their data      │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   users     │  │  meetings   │  │    goals    │  │   tasks     │        │
│  │             │  │             │  │             │  │             │        │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘        │
│                        ... 60+ tables total ...                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Why Dapper Instead of Entity Framework Core?

### The Decision

We migrated from Entity Framework Core (EF Core) to Dapper + raw SQL for these reasons:

| Aspect | EF Core (Old) | Dapper (New) |
|--------|---------------|--------------|
| **SQL Control** | Generated SQL, sometimes inefficient | Hand-written SQL, full control |
| **Performance** | ORM overhead | Minimal overhead (10-50x faster for reads) |
| **Supabase RLS** | Struggled with PostgreSQL RLS | Works perfectly with RLS |
| **Debugging** | Hard to see actual queries | SQL is right in the code |
| **Learning Curve** | Complex (migrations, tracking, etc.) | Simple (just SQL + mapping) |
| **Complexity** | Change tracker, migrations, DbContext | Just run SQL, get objects |

### What is Dapper?

Dapper is a **micro-ORM**. It does two things:

1. **Execute SQL queries** against a database
2. **Map results to C# objects** automatically

That's it. No change tracking, no migrations, no magic. You write SQL, Dapper runs it and gives you objects.

**Example:**
```csharp
// Dapper - what you see is what you get
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE id = @Id",
    new { Id = userId });
```

---

## Key Technologies

| Technology | Purpose | NuGet Package |
|------------|---------|---------------|
| **Dapper** | SQL execution + object mapping | `Dapper` |
| **Npgsql** | PostgreSQL database driver for .NET | `Npgsql` |
| **Supabase** | Hosted PostgreSQL + Auth + RLS | Supabase.io (cloud) |
| **BCrypt.Net** | Password hashing | `BCrypt.Net-Next` |

---

## Folder Structure

All Dapper-related code lives in these locations:

```
Tracker/
├── Services/
│   ├── Data/                           # ← DAPPER LAYER (data access)
│   │   ├── IRepository.cs              # Generic CRUD interface
│   │   ├── BaseRepository.cs           # Shared CRUD implementation
│   │   ├── DapperConnectionFactory.cs  # Creates database connections
│   │   └── Repositories/               # Entity-specific repositories
│   │       ├── UserRepository.cs
│   │       ├── TeamMemberRepository.cs
│   │       ├── MeetingRepository.cs
│   │       ├── TaskRepository.cs
│   │       ├── GoalRepository.cs
│   │       ├── MetricRepository.cs
│   │       ├── FeedbackRepository.cs
│   │       ├── ProjectRepository.cs
│   │       ├── QuickNoteRepository.cs
│   │       ├── DevelopmentGoalRepository.cs
│   │       ├── PerformanceReviewRepository.cs
│   │       └── PulseSurveyRepository.cs
│   │
│   ├── Business Services/              # ← SERVICE LAYER (business logic)
│   │   ├── UserService.cs
│   │   ├── TeamMemberService.cs
│   │   ├── MeetingService.cs
│   │   ├── TaskService.cs
│   │   ├── GoalService.cs
│   │   └── MetricService.cs
│   │
│   └── Auth/                           # ← AUTHENTICATION
│       └── AuthService.cs              # JWT tokens, password hashing
│
├── DataModels/                         # ← DOMAIN MODELS (POCOs)
│   ├── User.cs
│   ├── TeamMember.cs
│   ├── Meeting.cs
│   ├── TrackerTask.cs
│   ├── Goal.cs
│   ├── Metric.cs
│   └── ... (60+ models)
│
└── Infrastructure/
    └── ServiceConfiguration.cs         # ← DI REGISTRATION
```

---

## The Three Layers

### Layer 1: Repositories (Data Access)

**Location:** `Services/Data/Repositories/`

**Purpose:** The ONLY place that talks to the database. Contains all SQL queries.

**Rule:** ViewModels and Services NEVER write SQL. They call repository methods.

**Example:**
```csharp
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT * FROM users 
            WHERE email = @Email AND is_deleted = false 
            LIMIT 1";
        
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }
}
```

### Layer 2: Business Services

**Location:** `Services/`

**Purpose:** Contains business logic. Wraps repositories. Called by ViewModels.

**Rule:** Services call repositories. They don't write SQL directly.

**Example:**
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        // Business validation could go here
        return await _repository.GetByEmailAsync(email);
    }
}
```

### Layer 3: ViewModels (UI Logic)

**Location:** `ViewModels/`

**Purpose:** Binds to Views. Calls services to get/save data.

**Rule:** ViewModels call services. They NEVER call repositories directly.

**Example:**
```csharp
public class UserViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    
    private async Task LoadUser()
    {
        CurrentUser = await _userService.GetUserByEmailAsync(UserEmail);
    }
}
```

---

## Next Documents

Continue reading in order:

1. **[02_CONNECTION_MANAGEMENT.md](02_CONNECTION_MANAGEMENT.md)** - How database connections work
2. **[03_REPOSITORY_PATTERN.md](03_REPOSITORY_PATTERN.md)** - How repositories are structured
3. **[04_SUPABASE_AND_RLS.md](04_SUPABASE_AND_RLS.md)** - Supabase setup and Row-Level Security
4. **[05_AUTHENTICATION_FLOW.md](05_AUTHENTICATION_FLOW.md)** - Login, signup, JWT tokens
5. **[06_ADDING_NEW_ENTITIES.md](06_ADDING_NEW_ENTITIES.md)** - Step-by-step guide to add new entities
7. **[07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md)** - Common issues and solutions

---

## Quick Reference: Where Do I Change Things?

| I need to... | File to modify |
|--------------|----------------|
| Add a new SQL query | `Services/Data/Repositories/{Entity}Repository.cs` |
| Add business logic | `Services/{Entity}Service.cs` |
| Add a new entity type | See `06_ADDING_NEW_ENTITIES.md` |
| Change how connections work | `Services/Data/DapperConnectionFactory.cs` |
| Register a new service in DI | `Infrastructure/ServiceConfiguration.cs` |
| Change authentication | `Services/Auth/AuthService.cs` |
| Debug connection issues | Check `DapperConnectionFactory.cs` and connection string |

---

## Naming Conventions

### Legacy Names → Current Names

During migration, we renamed several concepts. You may see old names in legacy code:

| Old Name (Legacy) | New Name (Current) | Database Table |
|-------------------|-------------------|----------------|
| OKR / ObjectiveKeyResult | Goal | `goals` |
| KPI / KeyPerformanceIndicator | Metric | `metrics` |
| KeyResult | Target | `targets` |
| OneOnOne | Meeting (with Type = OneOnOne) | `meetings` |
| IndividualGoal | Goal (with owner type) | `goals` |

**When modifying legacy code:** Update to current naming. Don't introduce more legacy names.

---

## Summary

- **Dapper** executes SQL and maps to objects
- **Repositories** are the ONLY place with SQL
- **Services** contain business logic, call repositories
- **ViewModels** call services, never touch the database
- **Supabase PostgreSQL** is our database with Row-Level Security
- All changes flow: ViewModel → Service → Repository → Database

**Next:** Read [02_CONNECTION_MANAGEMENT.md](02_CONNECTION_MANAGEMENT.md) to understand how database connections work.
