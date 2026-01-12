# Services Fix Plan

**Goal:** Remove `TrackerDbManager` and legacy entity usage from services; depend on Supabase-backed repositories and canonical models.

## Current Problems

- Many services still call `TrackerDbManager.Instance` directly.
- Some services still reason in terms of OKRs/KPIs/OneOnOnes instead of Goal/Target/Metric/Meeting.
- Data access is mixed: EF Core DbContext via `TrackerDbManager` + future Supabase access.

## Target Architecture

- Introduce repository interfaces per aggregate, e.g.:
  - `ITeamMemberRepository`, `IMeetingRepository`, `ITaskRepository`, `IGoalRepository`, `ITargetRepository`, `IMetricRepository`, `ISurveyRepository`, `IReviewRepository`.
- Implement repositories against Supabase/Postgres (and/or EF Core over the Supabase schema, depending on current design).
- Refactor services to depend on these interfaces via DI; **no direct `TrackerDbManager` usage in services**.

## High-Priority Services To Refactor

Use `TrackerDbManager.Instance` usages as a checklist; key examples include:

- `Tracker/Tracker/Tracker/Services/SearchService.cs`
  - Currently pulls data via `TrackerDbManager` for global search.
  - Refactor to query through repositories so search is schema-aligned.
- `Tracker/Tracker/Tracker/Services/ReminderService.cs`
  - Creates reminders by calling `TrackerDbManager.Instance.AddReminderAsync`.
  - Replace with `IReminderService` + `IReminderRepository` that operate on Supabase entities.
- AI/Insights services under `Tracker/Tracker/Tracker/Services/AI/`
  - Example: `Insights/Analyzers/ActionItemStalenessAnalyzer.cs` takes a `TrackerDbManager` reference.
  - Refactor analyzers to depend on read-only repositories for meetings/tasks/goals instead.
- Any service that still uses OKR/KPI-specific methods on `TrackerDbManager`.

## Concrete Work Items

1. **Define Repository Interfaces**
   - Create interfaces in a `Services/Contracts` or `Repositories` namespace for each core aggregate.
   - Ensure they expose async methods aligned with Supabase tables.
2. **Implement Repositories**
   - Implement against the Supabase DbContext or Supabase client wrappers.
   - Add logging via `LoggingManager.GetComponentLogger`.
3. **Refactor Services Off `TrackerDbManager`**
   - For each service using `TrackerDbManager.Instance`:
     - Inject the appropriate repository interfaces.
     - Replace calls like `TrackerDbManager.Instance.GetTasksAsync()` with repository calls.
   - Keep behavior identical where possible; only change the access path.
4. **Remove Legacy Methods From `TrackerDbManager`**
   - After services are moved, delete unused methods from `TrackerDbManager`.
   - Ultimately, treat `TrackerDbManager` as a thin legacy adapter until it can be removed entirely.

Use this file as the checklist for service-level work; cross-reference with `MANAGERS_FIX_PLAN.md` and `VIEWMODELS_FIX_PLAN.md` for callers that still expect the old patterns.
