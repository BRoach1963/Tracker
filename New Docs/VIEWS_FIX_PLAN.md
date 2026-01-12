# Views Fix Plan

**Goal:** Ensure WPF views (XAML + code-behind) are thin, bind to ViewModels that use the new models, and do not talk to `TrackerDbManager` or legacy entities directly.

## Current Problems

- Some views still call `TrackerDbManager.Instance` in code-behind (e.g., Daily Briefing, Splash Screen).
- OKR-related controls use `KeyResultMeasurable` and other legacy types in their event handlers.
- A few dialogs embed business logic in code-behind instead of ViewModels.

## High-Priority Views & Controls

- `Tracker/Tracker/Tracker/Views/SplashScreen.xaml.cs`
  - Calls `TrackerDbManager.Instance.GetOrCreateUserAsync`.
  - Should delegate to a startup/identity service instead.
- `Tracker/Tracker/Tracker/Views/Dialogs/DailyBriefingDialog.xaml.cs`
  - References `TrackerDbManager` via `GetDbContext()`.
  - Should be refactored to use a ViewModel + service for data access.
- `Tracker/Tracker/Tracker/Controls/OkrsControl.xaml.cs`
  - Uses `KeyResultMeasurable` in event handlers.
  - Should be updated to work with Goal/Target/TargetMeasurable only.
- Any view that manipulates `ObjectiveKeyResult`, `OneOnOne`, or KPI-specific types.

## Work Patterns

1. **Move Logic to ViewModels**
   - For each code-behind file with non-trivial logic:
     - Create or extend a ViewModel.
     - Move data access and decision logic from code-behind to the ViewModel.
     - Keep code-behind limited to UI wiring (e.g., closing windows, simple event forwarding).
2. **Remove `TrackerDbManager` from Code-Behind**
   - Replace calls like `TrackerDbManager.Instance...` with ViewModel commands that use services.
3. **Align OKR Views With Goal/Target Models**
   - Update bindings and event handlers in OKR-related views to use Goal/Target/TargetMeasurable.
   - Coordinate with changes in `GoalsViewModel` and related ViewModels.
4. **Clean Up Legacy Types in XAML**
   - Search XAML for `ObjectiveKeyResult`, `KeyResultMeasurable`, `OneOnOne`, `KeyPerformanceIndicator`.
   - Replace with bindings to the new models.

Use this file as the UI-focused roadmap; actual data and operations should always flow through ViewModels and services that follow the plans in `VIEWMODELS_FIX_PLAN.md` and `SERVICES_FIX_PLAN.md`.
