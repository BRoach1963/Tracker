# Managers Fix Plan

**Goal:** Move managers off `TrackerDbManager` and legacy entities; make them orchestrators over the new services and models.

## Key Managers

- `Tracker/Tracker/Tracker/Managers/TrackerDataManager.cs`
- `Tracker/Tracker/Tracker/Managers/CalendarSyncManager.cs`
- Other managers (UserSettingsManager, NotificationManager, etc.) – generally fine, but should not depend on legacy data access or types.

## TrackerDataManager

**Current Role:** Centralized in-memory cache + façade over `TrackerDbManager` for team members, projects, tasks, OKRs, KPIs, feedback, goals, notes, surveys, reviews, etc.

**Current Problems:**
- Heavy direct usage of `TrackerDbManager.Instance` for all entities.
- Methods and collections still expose legacy types:
  - `GetOKRs()`, `AddOKR(ObjectiveKeyResult)`, `UpdateOKR`, `DeleteOKR`.
  - KPI-related methods (to be deleted after KPI → Metric consolidation).
- Mixed responsibility: caching, mapping, and data access all in one place.

**Work Items:**

1. **Finish Type Replacements (already started)**
   - Replace `OneOnOne` with `Meeting`.
   - Replace `IndividualTask` with `TrackerTask`.
   - Replace `KeyPerformanceIndicator` with `Metric`.
   - Replace `ObjectiveKeyResult` with `Goal` + `Target` where applicable.
2. **Introduce Service Dependencies**
   - Instead of calling `TrackerDbManager.Instance`, depend on:
     - Meeting/Task/Goal/Metric services (backed by repositories).
   - Keep TrackerDataManager focused on UI-facing observable collections and cache invalidation.
3. **Prune Legacy APIs**
   - Remove OKR/KPI-specific methods once callers are migrated.
   - Remove any methods that expose int-based IDs or legacy enums.
4. **Long-Term:**
   - Optionally split responsibilities into smaller managers (e.g., GoalsManager, MeetingsManager) if TrackerDataManager stays too large.

## CalendarSyncManager

**Current Role:** Sync meetings with external calendars (e.g., Google) and persist links.

**Current Problems:**
- Uses `TrackerDbManager.Instance.UpdateOneOnOneAsync` and `SaveCalendarLinkAsync`.
- Still thinks in terms of OneOnOne instead of Meeting.

**Work Items:**

1. Refactor to use a `IMeetingService`/`IMeetingRepository` that operates on Meeting entities.
2. Ensure calendar links are attached to Meeting IDs (Guid) and persisted in Supabase.
3. Remove any `OneOnOne`-specific calls once meeting consolidation is complete.

## Other Managers

- Audit UserSettingsManager, NotificationManager, and any other manager to ensure:
  - No direct `TrackerDbManager` calls.
  - No references to legacy models (OKR/KPI/OneOnOne).
  - All persistence goes through appropriate services/repositories.

Use this file as the manager-focused roadmap; for specific service/repository definitions, see `SERVICES_FIX_PLAN.md` and `DB_SCHEMA_CURRENT.md`.
