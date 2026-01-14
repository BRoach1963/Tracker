# Copilot Instructions for Tracker Project

## Project Overview
Tracker is a WPF desktop application for professional relationship management, built with .NET 8 and following MVVM architecture.

## Key Technologies
- **UI**: WPF with XAML, MVVM pattern
- **Database**: Supabase PostgreSQL using Dapper (direct SQL)
- **Backend**: Supabase for authentication, database, and subscriptions
- **Testing**: xUnit with Moq

## Architecture
- **Views**: XAML files in `/Views/` - UI only, no business logic
- **ViewModels**: In `/ViewModels/` - UI logic, commands, data binding
- **Services**: In `/Services/` - Business logic, wraps repositories
- **Repositories**: In `/Services/Data/Repositories/` - ALL database access (Dapper + SQL)
- **Managers**: In `/Managers/` - Singleton services (UserSettingsManager, NotificationManager)

## Data Access Architecture (Dapper)
- **Connection Factory**: `Services/Data/DapperConnectionFactory.cs` - creates PostgreSQL connections
- **Base Repository**: `Services/Data/BaseRepository.cs` - shared CRUD operations
- **Entity Repositories**: `Services/Data/Repositories/*.cs` - entity-specific queries
- **Services Layer**: `Services/*.cs` - business logic, calls repositories
- **Rule**: SQL lives ONLY in repositories. Never in ViewModels or Services.
- **Documentation**: See `/New Docs/Dapper/` for comprehensive architecture docs

## Important Patterns
1. **User-Specific Settings**: Settings stored per-Supabase-user at `%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`
2. **Logging**: Use `LoggingManager.GetComponentLogger("ComponentName")` for all logging
3. **Commands**: Use `TrackerCommand` for ICommand implementations
4. **Events**: Use `DataMessenger` for cross-component communication
5. **Soft Delete**: Never hard delete - set `is_deleted = true`, `deleted_at`, `deleted_by`
6. **All IDs are GUIDs**: Supabase uses UUID, C# uses `Guid`. Never use `int` for entity IDs.

## Coding Standards
Follow the guidelines in [CODING_GUIDELINES.md](.github/CODING_GUIDELINES.md):
- DRY, KISS, YAGNI principles
- SOLID principles
- Proper error handling with logging
- XML documentation on public APIs
- Unit tests for business logic

## Database Considerations
- Supabase PostgreSQL is the only database (no SQLite/SQL Server)
- Row-Level Security (RLS) enforced at database level
- Use repositories for all database operations (never direct SQL in ViewModels)
- All tables have: id (UUID), is_deleted, created_at, updated_at, deleted_at, deleted_by

## Naming Conventions (Legacy → Current)
| Old Name | Current Name | Database Table |
|----------|--------------|----------------|
| OKR / ObjectiveKeyResult | Goal | `goals` |
| KPI / KeyPerformanceIndicator | Metric | `metrics` |
| KeyResult | Target | `targets` |
| OneOnOne | Meeting (Type=OneOnOne) | `meetings` |

## When Making Changes
1. Check for existing patterns in the codebase
2. Follow MVVM - don't put logic in Views
3. Use async/await for I/O operations
4. Add appropriate logging
5. Consider multi-user scenarios (shared database)
6. Update tests if modifying business logic
7. **Update Dapper documentation if applicable** (see below)

## Documentation Sync Requirements (MANDATORY)

When modifying data access code, you MUST update the corresponding documentation in `/New Docs/Dapper/`. This keeps docs accurate and saves future debugging time.

### Dapper Documentation Files
| File | Update When... |
|------|----------------|
| `01_ARCHITECTURE_OVERVIEW.md` | Changing overall data access patterns, adding new architectural layers |
| `02_CONNECTION_MANAGEMENT.md` | Modifying `DapperConnectionFactory`, connection strings, RLS token handling |
| `03_BASE_REPOSITORY.md` | Changing `BaseRepository.cs`, adding new shared CRUD methods |
| `04_ENTITY_REPOSITORIES.md` | Adding/modifying any repository in `/Services/Data/Repositories/` |
| `05_AUTHENTICATION_FLOW.md` | Changing auth flow, `AuthenticationSettings`, login/signup process |
| `06_SUPABASE_RLS_INTEGRATION.md` | Modifying RLS patterns, JWT handling, security context |
| `07_ADDING_NEW_ENTITIES.md` | If the process for adding new entities changes |
| `08_TROUBLESHOOTING.md` | Discovering new common issues or solutions |
| `09_QUICK_REFERENCE.md` | Adding new repositories, changing method signatures |

### Triggers for Documentation Updates
- Adding a new repository → Update `04_ENTITY_REPOSITORIES.md` and `09_QUICK_REFERENCE.md`
- Changing `AuthenticationSettings` properties → Update `05_AUTHENTICATION_FLOW.md`
- Modifying `BaseRepository` methods → Update `03_BASE_REPOSITORY.md`
- Adding new SQL patterns → Update relevant repository docs
- Changing model properties that affect data mapping → Update entity docs
- Fixing a bug related to Dapper/data access → Consider adding to `08_TROUBLESHOOTING.md`

### Documentation Update Checklist
When committing data access changes, verify:
- [ ] Code examples in docs match the actual implementation
- [ ] Property names, method signatures, and types are accurate
- [ ] Any new patterns are documented with examples
- [ ] Quick reference tables are up to date

## WPF / C# Specific Guidance

### Architecture
- Use MVVM strictly: no business logic in code-behind; code-behind only for view-specific glue that cannot be expressed cleanly in XAML.
- Prefer commands over events; keep behaviors reusable and lightweight.
- Keep ViewModels testable: depend on interfaces, avoid direct Dispatcher usage where possible.

### Performance & UI Thread
- Avoid heavy work during layout/measure/arrange; keep converters fast and allocation-light.
- Use virtualization for ItemsControl/DataGrid/TreeView scenarios; do not disable virtualization for convenience.
- Avoid excessive visual tree walking; cache references if needed and keep lookups minimal.
- Use async patterns for I/O and long operations, but marshal to UI thread only at the final UI update boundary.

### Bindings & Memory
- Keep binding expressions simple; avoid deeply nested binding paths.
- Unsubscribe from events and messages; prevent memory leaks (WeakEventManager or weak messenger patterns when appropriate).
- Prefer observable collections with minimal churn; batch updates when possible.

### XAML Conventions
- Centralize styles/resources; avoid duplicated control templates.
- Prefer explicit DataTemplates over runtime type checks in code.
- Avoid overly clever triggers; keep UI behavior discoverable and maintainable.

## MAUI / C# Specific Guidance

### Architecture
- Use MVVM: Views contain no business logic; place state/behavior in ViewModels and services.
- Keep UI concerns (navigation, dialogs, toasts, permissions) behind interfaces injected into ViewModels.
- Avoid tight coupling to static platform APIs; wrap them in abstractions.

### Performance & Responsiveness
- Never block the UI thread (no .Result/.Wait); use async/await end-to-end.
- Use CancellationToken for long-running operations and page navigation scenarios.
- Favor incremental loading / virtualization patterns for large lists; avoid heavy work in constructors.
- Dispose subscriptions and timers; avoid event handler leaks on pages and controls.

### State & Data
- Treat models as immutable where practical; avoid "god models" that mix UI and domain logic.
- Use DTOs for API boundaries; do not leak transport models into ViewModels.
- Cache and sync explicitly: separate local persistence (SQLite) from remote calls (HTTP).

### UI Conventions
- Prefer compiled bindings where possible and keep binding paths simple.
- Keep XAML minimal: styles/resources for reuse, avoid complex triggers that hide logic.
