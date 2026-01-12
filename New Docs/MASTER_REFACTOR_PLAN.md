# Tracker Master Refactor Plan

**Goal:** Replace all legacy/local patterns with the new Supabase-first, cloud, multi-role architecture, one layer at a time.

This plan consolidates prior analysis docs (Phase/Tier/Consolidation reports, Supabase migration docs, etc.) into a single execution map.

## Layers & Documents

- **Database Schema (current state)**
  - See: `DB_SCHEMA_CURRENT.md`
  - Source/refs: `Tracker/Tracker/Database/Supabase/README.md`, `ENTITY_MIGRATION_ANALYSIS.md`, `SUPABASE_MIGRATION_STATUS.md`.
- **Domain Models (current state)**
  - See: `MODELS_CURRENT.md`
  - Source/refs: `TIER_2_OBJECTIVE_KEYRESULT_CONSOLIDATION.md`, `TIER_3_CONSOLIDATIONS_ANALYSIS.md`, `Tracker/CONSOLIDATION_2_KPI_METRIC_ANALYSIS.md`, `Tracker/CONSOLIDATION_3_MEASURABLE_ANALYSIS.md`.
- **Services (work remaining)**
  - See: `SERVICES_FIX_PLAN.md`
- **Managers (work remaining)**
  - See: `MANAGERS_FIX_PLAN.md`
- **ViewModels (work remaining)**
  - See: `VIEWMODELS_FIX_PLAN.md`
- **Views (work remaining)**
  - See: `VIEWS_FIX_PLAN.md`

## High-Level Status Snapshot

- **DB Schema:** Supabase/Postgres is the canonical store; schema is aligned with Meeting/Goal/Target/Metric/Task/AgendaItem. Work here is mostly documentation and small cleanup.
- **Domain Models:** New models (Goal, Target, Metric, Meeting, TrackerTask, TargetMeasurable, etc.) exist and map to Supabase. Remaining work is deleting/isolating legacy models and wiring all code to the new ones.
- **Services:** Many services still depend on `TrackerDbManager` and/or legacy entities. Main work is introducing Supabase-backed repositories per aggregate and refactoring services to depend on interfaces, not the legacy manager.
- **Managers:** `TrackerDataManager` and `CalendarSyncManager` still wrap `TrackerDbManager` and old types (OKR/KPI/OneOnOne). They need to move to the new models and repositories, then shrink or disappear.
- **ViewModels:** A number of ViewModels still talk directly to `TrackerDbManager` and legacy types. They must be refactored to use services/managers that expose the new models.
- **Views:** Some XAML code-behind still calls `TrackerDbManager` or uses legacy types in event handlers (e.g., OKR control, Daily Briefing). These should be bound to ViewModels that use the new models.

## Execution Order (Recommended)

1. **Confirm DB & Models**
   - Use `DB_SCHEMA_CURRENT.md` and `MODELS_CURRENT.md` as ground truth.
   - Delete or archive any docs that conflict with these.
2. **Kill Legacy Data Access**
   - Introduce repository interfaces for core aggregates (team members, meetings, tasks, goals, targets, metrics, surveys, reviews).
   - Refactor services off `TrackerDbManager` (see `SERVICES_FIX_PLAN.md`).
   - Gradually retire `TrackerDbManager` implementation.
3. **Clean Up Managers**
   - Refactor `TrackerDataManager` and `CalendarSyncManager` to depend on repositories/services instead of `TrackerDbManager` and legacy entities.
   - Remove OKR/KPI-specific paths as consolidations are executed.
4. **Migrate ViewModels**
   - Replace direct `TrackerDbManager` calls with injected services.
   - Replace legacy types (OneOnOne, ObjectiveKeyResult, KeyResultMeasurable, KPI) with Meeting/Goal/Target/Metric/TargetMeasurable in the ViewModels.
5. **Update Views**
   - Remove DB calls from code-behind.
   - Update OKR/KPI-related views to use Goal/Target/Metric and AgendaItem instead.
6. **AI/Insights Layer**
   - Ensure AI & Insights services use Supabase/pgvector and the new models only.
   - See `AI/AI_DATA_ACCESS_STRATEGY.md` and `AI/AI_INTEGRATION_TECHNICAL_SPECIFICATION.md` for updated direction.

Use this file as the top-level index; detailed per-layer work is in the companion docs in this folder.
