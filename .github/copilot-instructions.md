# Copilot Instructions for Tracker Project

## Project Overview
Tracker is a WPF desktop application for professional relationship management, built with .NET 8 and following MVVM architecture.

## Key Technologies
- **UI**: WPF with XAML, MVVM pattern
- **Database**: SQLite (local) or SQL Server (shared), using Entity Framework Core
- **Backend**: Supabase for authentication, subscriptions, and cloud sync
- **Testing**: xUnit with Moq

## Architecture
- **Views**: XAML files in `/Views/` - UI only, no business logic
- **ViewModels**: In `/ViewModels/` - UI logic, commands, data binding
- **Services**: In `/Services/` - Business logic, external integrations
- **Managers**: In `/Managers/` - Singleton services (UserSettingsManager, NotificationManager)
- **Database**: In `/Database/` - EF Core context, repositories, migrations

## Important Patterns
1. **User-Specific Settings**: Settings stored per-Supabase-user at `%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`
2. **Logging**: Use `LoggingManager.GetComponentLogger("ComponentName")` for all logging
3. **Commands**: Use `TrackerCommand` for ICommand implementations
4. **Events**: Use `DataMessenger` for cross-component communication

## Coding Standards
Follow the guidelines in [CODING_GUIDELINES.md](.github/CODING_GUIDELINES.md):
- DRY, KISS, YAGNI principles
- SOLID principles
- Proper error handling with logging
- XML documentation on public APIs
- Unit tests for business logic

## Database Considerations
- Support both SQLite and SQL Server
- Custom database location is user-specific
- Use `TrackerDbManager` for all database operations
- Handle offline scenarios gracefully

## When Making Changes
1. Check for existing patterns in the codebase
2. Follow MVVM - don't put logic in Views
3. Use async/await for I/O operations
4. Add appropriate logging
5. Consider multi-user scenarios (shared database)
6. Update tests if modifying business logic

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
