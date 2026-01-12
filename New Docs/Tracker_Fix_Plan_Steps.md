# Tracker Fix Plan (Stepwise)

Short, bite-sized steps to migrate off legacy types/TrackerDbManager and finish the refactor.

## Issue List (What we must fix)

1. TrackerDataManager still uses legacy types and TrackerDbManager for tasks/goals/metrics and related counts.
2. ViewModels/services still call TrackerDbManager directly instead of repositories/TrackerDataManager.
3. Meeting↔task/goal/metric linkage still uses legacy link patterns instead of AgendaItem + OriginAgendaItemId.
4. TrackerDbManager still owns DB bootstrap and user/session wiring.

## Stepwise Plan (One chunk at a time)

### Step 1 – Finish TrackerDataManager type replacements
- Replace remaining legacy types in TrackerDataManager:
  - IndividualTask → TrackerTask
  - ObjectiveKeyResult/KeyResult → Goal/Target
  - KeyPerformanceIndicator → Metric
- Swap any TrackerDbManager calls in TrackerDataManager for repository-based calls.
- Build and fix only TrackerDataManager compile errors before touching other files.

### Step 2 – Migrate task/goal/metric flows in ViewModels
- For each ViewModel that uses tasks/goals/metrics and still calls TrackerDbManager:
  - Repoint to TrackerDataManager or the relevant repositories.
  - Keep signatures the same where possible; only change types/paths.
- Build and fix compile errors file-by-file (smallest diff that compiles).

### Step 3 – Replace meeting link tables with AgendaItem
- Identify all usages of MeetingTask/OneOnOneLinkedTask/OneOnOneLinkedOkr/OneOnOneLinkedKpi.
- For each usage:
  - Replace link behavior with AgendaItem.RelatedEntityType/RelatedEntityId for existing entities.
  - Use OriginAgendaItemId on Task/Goal/Metric for entities spun up from an agenda item.
- Remove now-unused link entity types once all call sites are moved.

### Step 4 – Move remaining TrackerDbManager CRUD into repositories
- For any entity where TrackerDbManager still does CRUD and there is a matching repository:
  - Add missing methods to the repository if needed.
  - Update callers to use the repository instead of TrackerDbManager.
- Keep changes localized: one entity family (e.g., Goals) at a time, build after each.

### Step 5 – Extract DB bootstrap/user session responsibilities
- Create a small service (e.g., DatabaseBootstrapService) that:
  - Initializes the DbContext/database according to settings.
  - Exposes HasData/Initialize/TestConnection equivalents.
- Create a small user/session helper to handle Supabase user ↔ local user mapping.
- Update startup code to use these services instead of TrackerDbManager.

### Step 6 – Delete TrackerDbManager and clean up leftovers
- Once no call sites remain and bootstrap/session are moved:
  - Delete TrackerDbManager and its obsolete models.
  - Remove any dead code and unused configuration tied to it.
- Run full build and a basic app smoke test to confirm behavior.

### Step 7 – Tighten meeting/agenda analytics
- Move any remaining “meeting counts” or analytics helpers from the old style into:
  - Repository methods, or
  - A dedicated Analytics/Reporting service that uses the DbContext/repositories.
- Keep the public surface small and focused on UI/reporting needs.
