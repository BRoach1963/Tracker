# Tier 2: ObjectiveKeyResult Consolidation Analysis

**Status:** READY FOR EXECUTION  
**Complexity:** HIGH  
**Files Affected:** 1 model + 50+ code references  
**Phase 2 Effort:** 15-20 hours  

---

## Current State

### ObjectiveKeyResult Model
- **Location:** [DataModels/ObjectiveKeyResult.cs](DataModels/ObjectiveKeyResult.cs)
- **Size:** 204 lines
- **Key Properties:**
  - `int ObjectiveId` - Primary key (int-based, legacy)
  - `Guid? OrganizationId` - Mixed ID type (indicates partial migration)
  - `string Title` - Objective statement
  - `List<KeyResult> KeyResults` - Child elements (KeyResult model doesn't exist!)
  - Computed properties: Status, CompletionPercentage, MeetingCount, etc.

### Problem: Missing KeyResult Model
ObjectiveKeyResult references `List<KeyResult> KeyResults`, but **KeyResult.cs doesn't exist**:
- Only ViewModels exist: KeyResultViewModel
- Only UI exists: KeyResultItem.xaml.cs
- **KeyResult was never implemented as a data model**

This means the OKR framework was partially implemented - the model structure exists but child entity is missing.

---

## Supabase Schema Equivalent

### Current Structure (ObjectiveKeyResult)
```csharp
ObjectiveKeyResult
├── ObjectiveId (int)
├── Title (objective statement)
├── KeyResults (List<KeyResult>) ❌ MISSING MODEL
├── Status (computed from KRs)
└── CompletionPercentage (computed from KRs)
```

### Supabase Structure (Goals + Targets)
```sql
goals table
├── id (Guid)
├── title (objective/goal statement)
├── organization_id (Guid FK)
└── goal_type (enum: "strategic", "team", "individual")

targets table (Key Results)
├── id (Guid)
├── goal_id (Guid FK)
├── name (KR description)
├── target_value (decimal)
└── current_value (decimal)

target_measurables table (measurable outcomes)
├── id (Guid)
├── target_id (Guid FK)
├── measurable_id (Guid FK)
└── measurable_type (string)
```

---

## Usage Analysis

### DbContext References (3)
- `TrackerDbContext.ObjectiveKeyResults` DbSet
- `DatabaseSeeder` references (old seeding logic)
- `OkrProgressService` queries

### Service References (6 Services)
1. **OkrProgressService.cs** - Core OKR progress tracking
2. **GoalIndexer.cs** - AI vector indexing for OKRs
3. **InsightEngine.cs** - At-risk OKR detection
4. **OkrTrajectoryAnalyzer.cs** - Trajectory analysis
5. **AIInsightGenerator.cs** - Insight generation
6. **ExcelExportService.cs** - OKR export

### ViewModel References (4)
1. **ReportsViewModel.cs** - OKR reporting charts
2. **QuickNotesViewModel.cs** - OKR display
3. **TrackerMainViewModel.cs** - Selected OKR tracking
4. **KeyResultViewModel.cs** - Dialog for KR creation (orphaned)

### Test References (2)
1. **EntityCrudTests.cs** - OKR CRUD tests
2. **TestDataBuilder.cs** - OKR test fixtures

### Total Code Impact
- **~50 file references** to ObjectiveKeyResult
- **~200 lines of code** directly using ObjectiveKeyResult
- **2-3 major services** depend on OKR structure

---

## Consolidation Approach

### Strategy: Replace ObjectiveKeyResult with Goal + Target Pattern

**Key Decision Points:**

1. **Delete ObjectiveKeyResult.cs** - Use Goal + Target instead
2. **Update DbContext** - Remove ObjectiveKeyResults DbSet
3. **Migrate Services** - Update OkrProgressService to work with Goal/Target
4. **Update ViewModels** - Reference Goal/Target instead of ObjectiveKeyResult
5. **Fix Status Calculation** - Move from KPI enum to Goal status enum
6. **Create KeyResult as computed model** - If needed by UI (not persisted)

### Implementation Steps

**Phase 1 (Immediate - Tier 2):**
1. Delete ObjectiveKeyResult.cs
2. Update DbContext to remove the DbSet
3. Create migration: drop objectiveKeyResults table
4. Update OkrProgressService to use Goal/Target queries
5. Update OKR-related services (GoalIndexer, InsightEngine, etc.)
6. Update ViewModels (ReportsViewModel, QuickNotesViewModel, etc.)
7. Update tests to use Goal/Target fixtures

**Phase 2 (UI Updates):**
1. Update OKR dialog/form to use Goal/Target
2. Update reports to query from goals/targets
3. Refactor OKR charts to work with new structure
4. Update quick notes OKR selection

**Phase 3 (Cleanup):**
1. Delete KeyResultViewModel (now orphaned)
2. Remove OKR-specific enums (ObjectiveStatusEnum, TimePeriodEnum if not used elsewhere)
3. Update Excel export to use Goal/Target queries

---

## Risk Assessment

### High Risk
- ⚠️ **Heavy usage in reports** - OkrProgressService is core to analytics
- ⚠️ **AI indexing** - GoalIndexer depends on OKR structure for vector embeddings
- ⚠️ **Computed properties** - Status calculations need to move to service layer

### Medium Risk
- ⚠️ **Database seeding** - Need to update seed data for new schema
- ⚠️ **Test fixtures** - Many tests depend on ObjectiveKeyResult factory

### Low Risk
- ✅ **ViewModel logic** - Most logic can move to services
- ✅ **UI updates** - Goal/Target UI patterns already exist (Goal model is used)
- ✅ **Schema alignment** - Supabase already has goals/targets structure

---

## Estimated Phase 2 Effort Breakdown

| Task | Hours | Complexity |
|------|-------|-----------|
| Delete model + DbContext | 2 | Low |
| OkrProgressService refactor | 4 | Medium |
| Other service updates (GoalIndexer, InsightEngine) | 3 | Medium |
| ViewModel updates (Reports, QuickNotes, etc.) | 3 | Medium |
| Test updates | 2 | Low |
| Database seeding updates | 2 | Low |
| **Total** | **16** | - |

**Plus UI updates in Phase 3:** 4-6 hours

---

## Decision Required

### Option A: AGGRESSIVE (Recommended)
**Execute immediately in Tier 2:**
- Delete ObjectiveKeyResult.cs
- Force migration to Goal/Target pattern
- Update all references
- **Cost:** 16 hours Phase 2
- **Benefit:** Complete schema alignment, no OKR-specific code

### Option B: CONSERVATIVE
**Defer to Phase 2:**
- Fix references to deleted models (KeyResult, KPI enum)
- Keep ObjectiveKeyResult functional for now
- Migrate to Goal/Target in future consolidation
- **Cost:** 2-3 hours now, 12+ hours later
- **Benefit:** Smaller immediate change, time to plan migration

### Recommendation
**AGGRESSIVE** - Aligns with user's stated preference: "we should always lean towards new schema, only pushbacks should be with lost functionality"

- Supabase has goals/targets table (perfect replacement)
- Goal model already in use (code patterns exist)
- No lost functionality (Goal is superset of OKR)
- Cleaner schema alignment

---

## Pre-Execution Checklist

Before executing consolidation:

- [ ] Backup database schema for ObjectiveKeyResults table
- [ ] Backup existing ObjectiveKeyResult code (git commit)
- [ ] List all files to be modified (50+ refs)
- [ ] Verify Goal model can support all OKR properties
- [ ] Check if Goal/Target UI dialogs support all OKR properties
- [ ] Review OkrProgressService for service-layer-appropriate code
- [ ] Identify which AI insights can move to Goal tier

---

## Immediate Next Steps

**If AGGRESSIVE approach approved:**

1. List all files to be modified (find -r "ObjectiveKeyResult")
2. Create per-file modification plan
3. Delete ObjectiveKeyResult.cs
4. Update OkrProgressService (queries)
5. Update other services (batch updates)
6. Update ViewModels (batch updates)
7. Update tests
8. Build and validate

**Estimated execution time:** 6-8 hours continuous work (or 2 days at normal pace)

---

## Backout Plan

If consolidation encounters blockers:
1. Restore ObjectiveKeyResult.cs from git
2. Revert DbContext changes
3. Revert individual file changes (cherry-pick)
4. Keep Goal/Target pattern changes (valuable regardless)

Risk of backout: **LOW** - most changes are delete/replace operations

---

## Notes

### Interesting Finding: KeyResult Model Missing
- ObjectiveKeyResult references `List<KeyResult> KeyResults`
- KeyResult model was never created
- Only UI (KeyResultItem) and ViewModel (KeyResultViewModel) exist
- This suggests OKR framework was abandoned mid-implementation

### Status Calculation Complexity
Current ObjectiveKeyResult calculates status from KeyResults:
```csharp
if (KeyResults.Any(kr => kr.Status == KpiStatusEnum.OffTarget))
    return ObjectiveStatusEnum.OffTrack;
```

This references **KpiStatusEnum** (deleted in Consolidation #2), which means current code is already broken for OKR status.

**Solution:** Move status calculation to service layer (OkrProgressService) or Goal service, where it can compute from Target progress.

---

## Consolidation Decision

**🔴 AWAITING USER DECISION:**

Should we proceed with AGGRESSIVE consolidation of ObjectiveKeyResult → Goal + Target?

- ✅ Matches Supabase schema perfectly
- ✅ No lost functionality
- ✅ Fixes broken KPI enum references
- ✅ Aligns with stated preference ("lean towards new schema")
- ⚠️ 16-20 hours Phase 2 effort
- ⚠️ High code churn (50+ file updates)

**Recommendation:** YES - approve aggressive consolidation
