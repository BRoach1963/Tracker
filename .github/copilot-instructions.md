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
