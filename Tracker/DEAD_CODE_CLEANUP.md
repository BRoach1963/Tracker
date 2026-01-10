# Dead Code Cleanup List

Track dead code identified during the Supabase migration. Delete when encountered during refactoring if it blocks progress, otherwise clean up after migration is complete.

## Status: IN PROGRESS

---

## COMPLETED DELETIONS ✅

### MockData Folder - DELETED
**Location:** Was at `Tracker/MockData/`
**Reason:** Unused - 0 references. Seed data now in Supabase.
- [x] `MockTeamMemberData.cs` - DELETED
- [x] `MockKPIs.cs` - DELETED
- [x] `MockOKRs.cs` - DELETED
- [x] `MockProjects.cs` - DELETED

### DatabaseSeeder - DELETED
- [x] `Tracker/Database/DatabaseSeeder.cs` (~2000+ lines) - DELETED
- [x] `TrackerDbManager.cs` - `SeedSampleDataAsync()` method stubbed to no-op
- [x] `TrackerDbManager.cs` - `ClearAllDataAsync()` method stubbed to no-op

---

## Models Replaced (DELETE OLD MODELS)
**Reason:** Replaced by new Supabase-aligned models

### Old Goal Models to Delete:
- [ ] `Tracker/DataModels/IndividualGoal.cs` - Replaced by `DevelopmentGoal.cs`
- [ ] `Tracker/DataModels/ObjectiveKeyResult.cs` - Replaced by `Goal.cs` 
- [ ] `Tracker/DataModels/KeyResult.cs` - Replaced by `Target.cs`
- [ ] `Tracker/DataModels/KeyResultMeasurable.cs` - Replaced by `TargetMeasurable.cs`

### Old Metric/KPI Models to Delete:
- [ ] `Tracker/DataModels/KeyPerformanceIndicator.cs` - Replaced by `Metric.cs`
- [ ] `Tracker/DataModels/KpiDataSource.cs` - Replaced by `MetricDataSource.cs`

### Old Project/Task Models to Consolidate:
- [ ] `Tracker/DataModels/IndividualTask.cs` - New `TrackerTask.cs` created (keep old until consumers updated)
- [ ] `Tracker/DataModels/Project.cs` - Needs ID→Guid update (keep for now, update later)
- [ ] `Tracker/DataModels/TaskCollection.cs` - Needs ID→Guid update (keep for now)

### Related Files to Update:
- [ ] Rename `GoalCategory.cs` to `DevelopmentGoalEnums.cs` (contains multiple enums now)
- [ ] Rename `ObjectiveStatusEnum.cs` to `OkrStatusEnum.cs` or merge into enums file
- [ ] Delete legacy enum aliases after full migration

---

## Remaining DatabaseSeeder Cleanup (UI)
- [ ] `SettingsViewModel.cs` - Remove `SeedSampleDataCommand` and `ExecuteSeedSampleData()`
- [ ] `SetupWizardViewModel.cs` - Remove `IncludeSampleData` property and related logic
- [ ] `SettingsDialog.xaml` - Remove "Add Sample Data" button
- [ ] `DatabaseSettingsControl.xaml` - Remove sample data button
- [ ] `SetupWizard` - Remove "Include Sample Data" checkbox

---

## Test Files to Clean Up
- [ ] `Tracker.Tests/Database/DatabaseSeederTests.cs`
- [ ] Update any tests referencing `IndividualGoal` → `DevelopmentGoal`

---

## Cleanup Checklist (Post-Migration)
1. ✅ Delete MockData folder
2. ✅ Delete DatabaseSeeder.cs
3. ✅ Stub seed methods in TrackerDbManager
4. [ ] Delete old model files (IndividualGoal, ObjectiveKeyResult, KeyResult, KeyResultMeasurable)
5. [ ] Remove seed UI from Settings and SetupWizard
6. [ ] Update/delete related tests
7. [ ] Run full build to verify no broken references
8. [ ] Delete this file when complete
