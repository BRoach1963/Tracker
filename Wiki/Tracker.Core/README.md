# Tracker.Core – Wiki Index

This folder contains the **authoritative code specification** for the Tracker.Core project.

Tracker.Core is the **shared foundation layer** - data models, repositories, interfaces, and enums used by all UI projects.

---

## Structure

01 Architecture Overview  
02 Data Access Layer  
03 Connection & Configuration  
04 Base Repository Pattern  

05 Data Models (Entity Reference)  
06 Repositories (Query Reference)  
07 Interfaces  
08 Enums Reference  

---

## Reading Order

New engineers:
1 → 2 → 5

Adding new entities:
5 → 6 → 4

Database queries:
4 → 6 → 2

---

## Project Responsibility

Tracker.Core is:
- Shared between all UI projects (ProCohere.Avalonia, Tracker WPF, etc.)
- Contains NO UI code
- Contains NO business logic (that lives in UI project services)
- Pure data access and models

If code needs a UI framework reference, it does NOT belong in Tracker.Core.

---

## Key Files (Quick Reference)

| File | Purpose |
|------|---------|
| `Data/DapperConnectionFactory.cs` | Creates PostgreSQL connections |
| `Data/BaseRepository.cs` | Generic CRUD operations |
| `Data/IRepository.cs` | Repository interface contract |
| `Services/Backend/SupabaseConfig.cs` | Connection strings & config |
| `DataModels/AuditableEntity.cs` | Base class for all entities |

---

## Naming Conventions

| Database | C# Model | Notes |
|----------|----------|-------|
| `meetings` | `Meeting` | Singular class name |
| `team_members` | `TeamMember` | Singular class name |
| `goals` | `Goal` | Was "OKR" in legacy |
| `metrics` | `Metric` | Was "KPI" in legacy |
| `targets` | `Target` | Was "KeyResult" in legacy |
| `tracker_tasks` | `TrackerTask` | Prefixed to avoid System.Threading.Tasks conflict |

---

## Change Discipline

When modifying Tracker.Core:
1. Update this documentation
2. Ensure changes don't break other projects
3. Run full solution build
4. Update Database Wiki if schema changes involved

