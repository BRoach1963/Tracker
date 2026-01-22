# 01 – Architecture Overview

This document describes the **architectural design** of ProCohere.Avalonia.

---

## Purpose

ProCohere.Avalonia is the **desktop application** for the ProCohere product - a professional relationship management tool for managers and individual contributors.

It provides:
- Authentication and session management
- Daily briefings for managers/ICs
- Goal, Metric, and Task tracking (Pulse)
- Meeting management with agenda/prep/notes
- Team member visibility (Circle - for managers)
- Personal profile management (Me)

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| UI Framework | Avalonia 11.3.x |
| MVVM | CommunityToolkit.Mvvm |
| Backend | Supabase (PostgreSQL + Auth + Storage) |
| Data Access (UI) | Supabase REST API via supabase-csharp |
| Data Access (Core) | Dapper via Tracker.Core |
| Credential Storage | Windows Credential Manager |
| Theming | Fluent theme with custom Light/Dark |

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                      Views (XAML)                           │
│   MainWindow, BriefingView, PulseView, CircleView, etc.    │
└─────────────────────────────────┬───────────────────────────┘
                                  │ DataContext binding
                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                     ViewModels                              │
│   MainWindowViewModel, PulseViewModel, GoalsViewModel       │
│   - Commands (IRelayCommand)                                │
│   - Observable Properties                                   │
│   - UI State Management                                     │
└─────────────────────────────────┬───────────────────────────┘
                                  │ calls
                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                     Services (Singletons)                   │
│   AuthService, MeetingService, GoalsService, TaskService    │
│   - Business logic                                          │
│   - Supabase REST API calls                                 │
│   - Data transformation                                     │
└─────────────────────────────────┬───────────────────────────┘
                                  │ uses
                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                     Models (DTOs)                           │
│   MeetingDetail, GoalDetail, TaskDetail, TeamMemberDetail   │
│   - Supabase table mappings                                 │
│   - [Table] and [Column] attributes                         │
└─────────────────────────────────┬───────────────────────────┘
                                  │ maps to
                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   Supabase PostgreSQL                       │
│   procohere schema (app data)                               │
│   public schema (auth, licensing)                           │
└─────────────────────────────────────────────────────────────┘
```

---

## MVVM Pattern (STRICT)

### Views
- XAML files defining UI structure
- Code-behind only for:
  - View-specific event wiring (Loaded, etc.)
  - Dialog show/close logic
  - Things that CANNOT be done in XAML
- **NEVER** business logic in code-behind

### ViewModels
- Inherit from `ViewModelBase` (which inherits `ObservableObject`)
- Use `[ObservableProperty]` for bindable properties
- Use `[RelayCommand]` for commands
- Contain all UI logic and state

### Services
- Singleton pattern (`ServiceName.Instance`)
- Business logic and data access
- No UI dependencies
- Async methods for all I/O

### Models
- DTOs for Supabase table mapping
- Use `[Table("table_name")]` and `[Column("column_name")]`
- Inherit from `BaseModel` for Supabase compatibility
- NOT the same as Tracker.Core entities

---

## Project Dependencies

```
ProCohere.Avalonia
    │
    ├── Tracker.Core (data entities, repositories, enums)
    │
    └── Supabase packages (REST API client)
```

### When to use Tracker.Core vs Supabase REST

| Use Tracker.Core (Dapper) | Use Supabase REST (Services) |
|---------------------------|------------------------------|
| Complex joins | Simple CRUD |
| Bulk operations | Real-time subscriptions |
| Performance-critical queries | Standard UI operations |
| Reporting/analytics | Current UI implementation |

**Current state:** ProCohere.Avalonia uses Supabase REST for all operations. Tracker.Core is referenced but repositories are not directly used in UI.

---

## Key Design Decisions

### 1. Singleton Services
All services are singletons accessed via `ServiceName.Instance`:
```csharp
var meetings = await MeetingService.Instance.GetMeetingsAsync();
```

This simplifies dependency management but makes testing harder. Consider DI in future.

### 2. Two Supabase Clients
AuthService maintains two Supabase clients:
- `_publicClient` - For auth and licensing (public schema)
- `_procohereClient` - For app data (procohere schema)

### 3. Models vs Entities
- `Models/` contains DTOs for Supabase REST API
- `Tracker.Core/DataModels/` contains entities for Dapper
- They map to the SAME database tables but serve different purposes

### 4. Theme-Aware UI
All colors use theme resource keys:
```xml
<Border Background="{DynamicResource CardBackgroundBrush}">
```
Never hardcode colors.

### 5. Async Everything
All I/O operations are async:
```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    IsLoading = true;
    try
    {
        Data = await Service.Instance.GetDataAsync();
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## File Organization

| Folder | Contents |
|--------|----------|
| `/` | App.axaml, Program.cs, ViewLocator.cs |
| `Assets/` | Icons, images, fonts |
| `Converters/` | IValueConverter implementations |
| `Models/` | DTOs for Supabase |
| `Services/` | Business logic, API calls |
| `Themes/` | LightTheme.axaml, DarkTheme.axaml |
| `ViewModels/` | MVVM ViewModels |
| `Views/` | XAML views |
| `Views/Briefing/` | Briefing section views |
| `Views/Controls/` | Reusable UI controls |
| `Views/Dialogs/` | Modal/non-modal dialogs |
| `Views/Pulse/` | Pulse tab views (Goals, Metrics, Tasks) |

---

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Views | `{Name}View.axaml` | `PulseView.axaml` |
| ViewModels | `{Name}ViewModel.cs` | `PulseViewModel.cs` |
| Services | `{Name}Service.cs` | `MeetingService.cs` |
| Models | `{Name}Detail.cs` | `MeetingDetail.cs` |
| Dialogs | `{Name}Dialog.axaml` | `EditMeetingDialog.axaml` |
| Controls | `{Name}Card/Flyout/Panel.axaml` | `GoalCard.axaml` |

---

## Invariants

These rules are NEVER violated:

1. **No business logic in code-behind** - Only view-specific glue
2. **All services are singletons** - Use `Instance` property
3. **All I/O is async** - No blocking calls
4. **Colors via theme resources** - No hardcoded colors
5. **Models inherit BaseModel** - For Supabase compatibility
6. **ViewModels inherit ViewModelBase** - For MVVM infrastructure

