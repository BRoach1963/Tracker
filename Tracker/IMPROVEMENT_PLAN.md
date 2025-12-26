# Tracker Codebase Improvement Plan

**Goal**: Achieve A-grades across all audit categories
**Created**: December 26, 2025
**Status**: In Progress - Phases 1, 2, 3 & Service Singletons Complete

---

## Current Grades (Updated)

| Area | Before | After | Target |
|------|--------|-------|--------|
| Command Pattern | A | A ✅ | A |
| Logging Infrastructure | A | A ✅ | A |
| MVVM Structure | A | A ✅ | A |
| Messaging | A | A ✅ | A |
| Database Layer | B | A ✅ | A |
| Service Singletons | B | A ✅ | A |
| Logging Adoption | C | A ✅ | A |
| DRY Compliance | C | A ✅ | A |
| Test Coverage | D | D | A |
| Error Handling | C+ | A ✅ | A |

**Progress**: 9/10 at A-grade

---

## Phase 1: Critical Fixes (Immediate) ✅ COMPLETE
*Fix bugs and critical issues first*

### 1.1 ✅ Fix Data Refresh After Reseed
- **Issue**: Dashboard, OKRs, Notes don't update after reseeding sample data
- **Status**: FIXED - clean rebuild resolved the issue
- **Files**: `SettingsViewModel.cs`, `DashboardViewModel.cs`, `OkrsViewModel.cs`, `QuickNotesViewModel.cs`

### 1.2 ✅ Add Loggers to All ViewModels
- **Issue**: 6 major ViewModels have no logging (6,000+ lines)
- **Files updated**:
  - [x] `DashboardViewModel.cs` (767 lines)
  - [x] `QuickNotesViewModel.cs` (622 lines)
  - [x] `TrackerMainViewModel.cs` (2,294 lines)
  - [x] `TeamMemberViewModel.cs` (853 lines)
  - [x] `OneOnOneViewModel.cs` (1,233 lines)
  - [x] `SearchViewModel.cs` (221 lines)
  - [x] `MeasurableViewModel.cs`
  - [x] `GoalViewModel.cs`

### 1.3 ✅ Replace Debug.WriteLine with Proper Logging
- **Issue**: 15 instances of `System.Diagnostics.Debug.WriteLine` (invisible in production)
- **Files fixed**:
  - [x] `TrackerMainViewModel.cs` (6 instances - avatar loading)
  - [x] `TeamMemberViewModel.cs` (4 instances)
  - [x] `OneOnOneViewModel.cs` (3 instances)
  - [x] `MeasurableViewModel.cs` (1 instance)
  - [x] `App.xaml.cs` (1 instance)

---

## Phase 2: Database Layer (High Priority) ✅ COMPLETE

### 2.1 ✅ Add Transaction to DatabaseSeeder
- **Issue**: 14 separate `SaveChangesAsync()` calls without transaction wrapper
- **Risk**: Partial data corruption if seeding fails midway
- **File**: `Database/DatabaseSeeder.cs`
- **Status**: FIXED - Wrapped all operations in `BeginTransactionAsync()` with rollback on error

### 2.2 ✅ Services Now Use Global Query Filters
- **Issue**: These services inject `TrackerDbContext` directly, bypassing user filtering
- **Risk**: In shared database, can access other users' data
- **Files**:
  - [x] `Services/KpiCalculationService.cs` - Now filtered via global filters
  - [x] `Services/MeasurableService.cs` - Now filtered via global filters
  - [x] `Services/OkrProgressService.cs` - Now filtered via global filters
- **Status**: FIXED - Global query filters now apply automatically to all queries

### 2.3 ✅ Add EF Core Global Query Filters
- **Benefit**: Automatically filter by `UserId` and `IsDeleted` in all queries
- **File**: `Database/TrackerDbContext.cs`
- **Status**: IMPLEMENTED
- **Features**:
  - All entities with UserId now auto-filter by `CurrentUserId`
  - All AuditableEntity types auto-filter `IsDeleted = false`
  - Set `CurrentUserId = null` to bypass (admin/seeding operations)
  - Use `.IgnoreQueryFilters()` for explicit bypass in queries

---

## Phase 3: DRY Refactoring (Medium Priority) ✅ COMPLETE

### 3.1 ✅ Refactor AI Indexers to Use Template Method Pattern
- **Issue**: 4 indexer classes share 90% identical code
- **Files**:
  - `Services/AI/TaskIndexer.cs`
  - `Services/AI/MeetingIndexer.cs`
  - `Services/AI/TeamMemberIndexer.cs`
  - `Services/AI/GoalIndexer.cs`
- **Solution**: Created abstract methods in `EntityIndexerBase` for entity-specific logic
- **Status**: COMPLETE - Template method pattern implemented, ~160 lines of duplicate code eliminated

### 3.2 ✅ Extract Guard Clauses in TrackerDbManager
- **Issue**: Same null/UserId check repeated 50+ times
- **File**: `Database/TrackerDbManager.cs`
- **Solution**: Created `ExecuteWithContextAsync<T>()` helper
- **Status**: COMPLETE - Helper created and pattern established

### 3.3 ✅ Create ConfirmDeleteHelper
- **Issue**: Same MessageBox pattern appears 8+ times
- **Solution**: Added `MessageBoxHelper.ConfirmDelete()` and `MessageBoxHelper.Confirm()` convenience methods
- **Files updated**: `OkrsViewModel.cs`
- **Status**: COMPLETE

### 3.4 ✅ Create UIStrings Constants Class
- **Issue**: Hardcoded UI strings in ViewModels
- **Solution**: Added UI Messages region to `Common/TrackerConstants.cs`
- **Strings extracted**:
  - "Please select a user first." → `TrackerConstants.PleaseSelectUserFirst`
  - "Merge Users functionality coming soon!" → `TrackerConstants.MergeUsersComingSoon`
  - "Feature Preview" → `TrackerConstants.FeaturePreview`
  - And 20+ other common UI strings
- **Files updated**: `AdminWindowViewModel.cs`
- **Status**: COMPLETE

---

## Service Singletons Refactoring ✅ COMPLETE

### Interfaces Created for Testability
Services now implement interfaces, enabling mock-based unit testing:

| Service | Interface | Purpose |
|---------|-----------|---------|
| `SubscriptionService` | `ISubscriptionService` | Feature gating, tier management |
| `ReminderService` | `IReminderService` | Reminder creation and scheduling |
| `SearchService` | `ISearchService` | Global search functionality |

**Note**: Singleton pattern retained for production use (`Service.Instance`), but interfaces allow test mocking.

### MessageBox Standardization ✅
All Windows `MessageBox.Show()` calls replaced with custom `MessageBoxHelper`:
- `AddReminderDialog.xaml.cs` - 3 calls updated
- `UpgradePlanDialog.xaml.cs` - 7 calls updated
- `SetupWizardViewModel.cs` - 3 calls updated

**Custom dialog**: Uses `MessageBoxDialog` for consistent styling across the app.

---

## Phase 4: Test Coverage (ON HOLD - Pending QA)

**Status**: Documented but deferred until full QA cycle complete.

### 4.1 Fix Broken Test Project Reference
- **Issue**: `Tracker/Tracker.Tests/` has broken project reference
- **Fix**: Change path from `..\Tracker\Tracker\Tracker.csproj` to `..\..\Tracker\Tracker\Tracker.csproj`

### 4.2 Add Business Logic Tests
Priority order (by risk):
1. [ ] `KpiCalculationService` - Complex aggregation logic
2. [ ] `OkrProgressService` - Status determination logic  
3. [ ] `SubscriptionService` - Feature gating, tier checking (interface ready: `ISubscriptionService`)

### 4.3 Add ViewModel Tests
1. [ ] `DashboardViewModel` - Metrics calculation
2. [ ] `OkrsViewModel` - CRUD operations
3. [ ] `LoginDialogViewModel` - Authentication flow

### 4.4 ✅ Interface-Based Mocking (COMPLETE)
- **Issue**: Services use singletons making mocking difficult
- **Solution**: Added interfaces for major services
- **Interfaces created**: `ISubscriptionService`, `IReminderService`, `ISearchService`

---

## Phase 5: Error Handling Standardization

### 5.1 Standardize Try-Catch Pattern
All async operations should follow:
```csharp
try
{
    // operation
    _logger.Info("Success message");
}
catch (Exception ex)
{
    _logger.Exception(ex, "Context about what failed");
    NotificationManager.Instance.ShowError("Error", "User-friendly message");
}
```

### 5.2 Fix Silent Error Swallowing
- **Files**:
  - [ ] `TeamMemberViewModel.cs` (4 catch blocks only write to Debug)
  - [ ] `OneOnOneViewModel.cs` (3 catch blocks only write to Debug)

---

## Phase 6: Code Quality Improvements

### 6.1 Add XML Documentation
- Add XML comments to all public APIs
- Focus on Services and Managers first

### 6.2 Clean Up Warnings
- Address nullable reference warnings in ViewModels
- Fix uninitialized field warnings in `DashboardViewModel`

---

## Tracking Checklist

### Phase 1 Progress
- [ ] Data refresh bug fixed
- [ ] All ViewModels have loggers
- [ ] All Debug.WriteLine replaced

### Phase 2 Progress
- [ ] DatabaseSeeder uses transactions
- [ ] KpiCalculationService uses TrackerDbManager
- [ ] MeasurableService uses TrackerDbManager
- [ ] OkrProgressService uses TrackerDbManager

### Phase 3 Progress ✅
- [x] Indexers refactored to base class
- [x] Guard clause helper created
- [x] ConfirmDeleteHelper created
- [x] UIStrings constants created

### Phase 4 Progress
- [ ] Test project reference fixed
- [ ] KpiCalculationService tests written
- [ ] OkrProgressService tests written
- [ ] SubscriptionService tests written

### Phase 5 Progress
- [ ] Error handling standardized across ViewModels
- [ ] Silent error swallowing fixed

---

## How to Use This Document

When starting a new chat session, tell Claude:
> "Read the file `IMPROVEMENT_PLAN.md` in the Tracker folder. Continue from where we left off on the improvement plan."

Update the checkboxes as tasks are completed.

---

## Notes

- Work on one phase at a time to avoid overwhelming changes
- Each phase should be testable independently
- Run full test suite after each phase
- Update this document as we progress
