# ProCohere.Avalonia – Wiki Index

This folder contains the **authoritative code specification** for the ProCohere.Avalonia UI project.

ProCohere.Avalonia is the **Avalonia UI desktop application** - the user-facing product.

---

## Document Index

| # | Document | Purpose |
|---|----------|---------|
| 01 | [architecture-overview](01-architecture-overview.md) | Project structure, MVVM layers, technology stack |
| 02 | [application-lifecycle](02-application-lifecycle.md) | App startup, auth flow, window management |
| 03 | [navigation-system](03-navigation-system.md) | NavigationItem enum, sidebar, view switching |
| 04 | [authentication-flow](04-authentication-flow.md) | AuthService, two Supabase clients, DPAPI storage |
| 05 | [viewmodels-reference](05-viewmodels-reference.md) | All ViewModels with properties and commands |
| 06 | [services-reference](06-services-reference.md) | All Services with methods and patterns |
| 07 | [models-reference](07-models-reference.md) | All DTOs with Supabase table mappings |
| 08 | [views-reference](08-views-reference.md) | All Views, dialogs, and controls |
| 09 | [converters-reference](09-converters-reference.md) | Value converters for XAML bindings |
| 10 | [theming-reference](10-theming-reference.md) | Light/Dark themes, color system |

---

## Reading Order

### New Engineers
1. [01-architecture-overview](01-architecture-overview.md) - Project structure
2. [02-application-lifecycle](02-application-lifecycle.md) - Startup flow
3. [03-navigation-system](03-navigation-system.md) - App structure
4. [05-viewmodels-reference](05-viewmodels-reference.md) - Where logic lives

### Working on a Feature
1. [05-viewmodels-reference](05-viewmodels-reference.md) - Find the ViewModel
2. [08-views-reference](08-views-reference.md) - Find the View
3. [06-services-reference](06-services-reference.md) - Find the Service

### Authentication Issues
1. [04-authentication-flow](04-authentication-flow.md) - Auth architecture
2. [06-services-reference](06-services-reference.md) - AuthService details

### UI/Styling Issues
1. [10-theming-reference](10-theming-reference.md) - Theme system
2. [09-converters-reference](09-converters-reference.md) - Value converters
3. [08-views-reference](08-views-reference.md) - View structure

---

## Technology Stack

| Package | Version | Purpose |
|---------|---------|---------|
| Avalonia | 11.3.11 | Cross-platform UI framework |
| CommunityToolkit.Mvvm | 8.2.1 | MVVM infrastructure |
| Supabase | 1.1.1 | REST API client |
| AsyncImageLoader.Avalonia | - | Async avatar loading |
| AdysTech.CredentialManager | - | Windows credential storage |

---

## Key Architectural Rules

1. **MVVM Strict** - No business logic in code-behind
2. **Services are Singletons** - Use `ServiceName.Instance`
3. **Two Supabase Clients** - public schema (auth), procohere schema (data)
4. **RLS Enforced** - User only sees permitted data
5. **Theme-aware UI** - All colors via DynamicResource
6. **Soft Delete Only** - Set `is_deleted = true`, never hard delete

---

## Folder Structure

```
ProCohere.Avalonia/
├── App.axaml / App.axaml.cs    # Application entry, lifecycle
├── Program.cs                   # Avalonia bootstrap
├── ViewLocator.cs              # ViewModel → View resolution
├── Assets/                     # Icons, images
├── Converters/                 # Value converters
├── Models/                     # DTOs for Supabase
├── Services/                   # Business logic, API calls
├── Themes/                     # Light/Dark theme resources
├── ViewModels/                 # MVVM ViewModels
└── Views/
    ├── *.axaml                 # Main views
    ├── Briefing/               # Briefing section views
    ├── Controls/               # Reusable controls, flyouts
    ├── Dialogs/                # Modal dialog windows
    └── Pulse/                  # Pulse section tab views
```

---

## Navigation Items

| Item | View | Description | Who Sees |
|------|------|-------------|----------|
| Briefing | BriefingView | Daily/weekly summary | Everyone |
| Me | MeView | Personal tasks, goals, meetings | Everyone |
| Circle | CircleView | Team management | Managers only |
| Pulse | PulseView | Goals, Metrics, Tasks | Everyone |
| Chronicle | ChronicleView | Notes | Everyone |
| Settings | SettingsView | Profile, theme, logout | Everyone |

---

## Key Files by Category

### Entry Points
| File | Purpose |
|------|---------|
| `App.axaml.cs` | Startup, auto-login check |
| `Views/SplashWindow.axaml` | Startup splash |
| `Views/LoginWindow.axaml` | Authentication UI |
| `Views/MainWindow.axaml` | Main app shell |

### ViewModels (Largest)
| File | Lines | Purpose |
|------|-------|---------|
| `CircleViewModel.cs` | ~1847 | Team management |
| `MeViewModel.cs` | ~1207 | Personal hub |
| `AuthService.cs` | ~1068 | Authentication |
| `GoalsService.cs` | ~783 | Goal CRUD |
| `MetricsService.cs` | ~764 | Metric CRUD |
| `BriefingViewModel.cs` | ~685 | Daily summary |

### Services
| File | Purpose |
|------|---------|
| `AuthService.cs` | Auth, session, profile |
| `GoalsService.cs` | Goal CRUD |
| `MetricsService.cs` | Metric CRUD |
| `TaskService.cs` | Task CRUD |
| `MeetingService.cs` | Meeting CRUD |
| `TeamService.cs` | Team members |
| `NotesService.cs` | Notes CRUD |
| `ThemeService.cs` | Theme switching |

---

## Related Documentation

- [Wiki/Database/](../Database/) - Database schema and RLS policies
- [Wiki/Tracker.Core/](../Tracker.Core/) - Shared data access layer

---

## Change Discipline

When modifying ProCohere.Avalonia:
1. **Update this documentation** - Keep docs in sync with code
2. **Follow MVVM** - No logic in code-behind
3. **Use theme brushes** - DynamicResource for all colors
4. **Test both themes** - Light and Dark modes
5. **Check RLS implications** - Data queries respect permissions
6. **Build and run** - Verify before committing
