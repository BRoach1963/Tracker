# ViewModels Fix Plan

**Goal:** Ensure all ViewModels use the new models (Meeting/Goal/Target/Metric/TrackerTask) and depend on services/managers instead of `TrackerDbManager` and legacy entities.

## Current Problems

- Many ViewModels call `TrackerDbManager.Instance` directly for CRUD.
- Several ViewModels still expose legacy types in their public surface:
  - `ObjectiveKeyResult`, `KeyResultMeasurable`, `OneOnOne`, KPI concepts.
- Some ViewModels own long-running subscriptions/disposables that need cleanup (see Phase 2 analysis docs).

## High-Risk / High-Impact ViewModels

Use the grep results and prior analysis (Phase 2 + Tier docs) as a guide; key ViewModels include:

- Dialog/ViewModels that still use legacy data access or types:
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/OneOnOneViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/MeetingViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/GoalViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/FeedbackViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/SettingsViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/SetupWizardViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DialogViewModels/LoginDialogViewModel.cs`
- Main screens and dashboards:
  - `Tracker/Tracker/Tracker/ViewModels/TrackerMainViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/GoalsViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/QuickNotesViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/PerformanceReviewsViewModel.cs`
  - `Tracker/Tracker/Tracker/ViewModels/DashboardViewModel.cs`

## Work Patterns (Apply Across ViewModels)

1. **Remove Direct `TrackerDbManager` Usage**
   - Inject services/managers (via constructor or service locator pattern already in use) instead of calling `TrackerDbManager.Instance`.
   - Example: replace `TrackerDbManager.Instance.GetMeetingTemplatesAsync()` with `IMeetingTemplateService`.
2. **Replace Legacy Types**
   - `ObjectiveKeyResult` → `Goal` + `Target` as per `TIER_2_OBJECTIVE_KEYRESULT_CONSOLIDATION.md`.
   - `KeyResultMeasurable` → `TargetMeasurable` as per `Tracker/CONSOLIDATION_3_MEASURABLE_ANALYSIS.md`.
   - OneOnOne-specific models → `Meeting` with `MeetingType`.
3. **Align Collections with TrackerDataManager**
   - Where ViewModels currently read from `TrackerDataManager.OKRs` or legacy collections, update them to bind to the Goal/Target collections instead, after TrackerDataManager is updated.
4. **Lifecycle & Disposal (from Phase 2 docs)**
   - Ensure ViewModels implement `IDisposable` where they own subscriptions (events, messengers, timers).
   - Unsubscribe in `Dispose` to avoid memory leaks.

## Special Case: OKR/Goals UI

- Files: `Tracker/Tracker/Tracker/ViewModels/GoalsViewModel.cs`, OKR-related dialog ViewModels, and `Tracker/Tracker/Tracker/Controls/OkrsControl.xaml.cs`.
- Follow the consolidation docs:
  - `TIER_2_OBJECTIVE_KEYRESULT_CONSOLIDATION.md`
  - `Tracker/CONSOLIDATION_3_MEASURABLE_ANALYSIS.md`
- End state:
  - Goals/Targets are the only models exposed to the UI.
  - ViewModels no longer reference `ObjectiveKeyResult` or `KeyResultMeasurable`.

Use this file as the ViewModel-focused checklist; coordinate changes with `MANAGERS_FIX_PLAN.md` so that ViewModels consume the new manager/service APIs instead of the legacy ones.
