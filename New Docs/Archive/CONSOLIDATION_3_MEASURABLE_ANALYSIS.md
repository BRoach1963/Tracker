# Consolidation #3 Analysis: KeyResultMeasurable → TargetMeasurable

**Status:** Analysis Complete - READY FOR DECISION
**Date:** 2026-01-10
**Scope:** Measurable linking model consolidation

---

## Executive Summary

Two models exist that serve identical purposes:
1. **KeyResultMeasurable.cs** - Legacy model for linking to Key Results (int-based)
2. **TargetMeasurable.cs** - New model for linking to Targets (Guid-based)

These are **literally the same pattern at different schema versions**. The Supabase schema only has `target_measurables` table (renamed from KeyResultMeasurables). The old KeyResult model is being phased out in favor of Target.

**Recommendation:** **CONSOLIDATE: Delete KeyResultMeasurable, use TargetMeasurable as single source of truth**

---

## Model Comparison

### KeyResultMeasurable.cs (Legacy)
```
ID Type:          int Id (auto-increment)
Schema Table:     KeyResultMeasurables (legacy SQL Server/PostgreSQL only)
Parent:           KeyResult (int-based, being phased out)
Polymorphic:      MeasurableType + MeasurableId (int)
Aggregation:      AggregationTypeEnum
Weight:           decimal (1.0 default)
SortOrder:        int
Computed Props:   DisplayName, CurrentProgress, CurrentDisplayValue (at runtime)
Implementation:   int-based, nullable OrganizationId
```

**Key Properties:**
- `int Id` - Primary key, int-based
- `Guid? OrganizationId` - Nullable (compatibility layer)
- `int KeyResultId` - FK to KeyResult (int-based)
- `MeasurableType Type` - enum (Metric, Project, TaskCollection)
- `int MeasurableId` - Polymorphic FK
- `AggregationTypeEnum AggregationType` - enum
- `decimal Weight` - For weighted aggregation
- `int SortOrder` - Display ordering

### TargetMeasurable.cs (New)
```
ID Type:          Guid Id (UUID)
Schema Table:     target_measurables (Supabase - target)
Parent:           Target (Guid-based, modern)
Polymorphic:      MeasurableType + MeasurableId (Guid)
Aggregation:      AggregationTypeEnum
Weight:           REMOVED (not in schema)
SortOrder:        REMOVED (not in schema)
Computed Props:   DisplayName, CurrentProgress (at runtime, no DisplayValue)
Implementation:   Guid-based, modern audit trail
```

**Key Properties:**
- `Guid Id` - Primary key, UUID
- `Guid TargetId` - FK to Target (Guid-based)
- `string MeasurableType` - String instead of enum ("metric", "project", "task_collection")
- `Guid MeasurableId` - Polymorphic FK, UUID
- `AggregationTypeEnum AggregationType` - enum (same as KeyResultMeasurable)
- Removed: Weight, SortOrder (not in Supabase schema)

---

## Schema Analysis

### Supabase (Target Schema)
```sql
CREATE TABLE target_measurables (
    id UUID PRIMARY KEY,
    target_id UUID NOT NULL REFERENCES targets(id),
    
    -- Polymorphic link
    measurable_type VARCHAR(50) NOT NULL,  -- 'metric', 'project', 'task_collection'
    measurable_id UUID NOT NULL,
    
    -- Aggregation only
    aggregation_type VARCHAR(50) NOT NULL DEFAULT 'latest',
    
    created_at TIMESTAMPTZ NOT NULL,
    
    UNIQUE (target_id, measurable_type, measurable_id)
);

CREATE INDEX idx_target_measurables_target ON target_measurables(target_id);
CREATE INDEX idx_target_measurables_measurable ON target_measurables(measurable_type, measurable_id);
```

**Note:** Schema comment says "-- TARGET_MEASURABLES (was KeyResultMeasurables)" - Explicit rename acknowledgement

### Legacy Schema (SQL Server / PostgreSQL Only)
```sql
CREATE TABLE KeyResultMeasurables (
    Id INT PRIMARY KEY,
    UserId INT,
    KeyResultId INT NOT NULL REFERENCES KeyResults(Id),
    MeasurableType NVARCHAR(50),
    MeasurableId INT,
    AggregationType NVARCHAR(50),
    Weight DECIMAL(18,4),  -- GONE IN NEW SCHEMA
    SortOrder INT,         -- GONE IN NEW SCHEMA
    ...
);
```

**Finding:** KeyResultMeasurables exists in legacy databases but NOT in Supabase (target).

---

## Breaking Changes & Phase 2 Work

### Option A: DELETE KeyResultMeasurable (Recommended)

**Immediate Action:**
1. Delete `DataModels/KeyResultMeasurable.cs`
2. Remove from `TrackerDbContext.cs`
3. Update `IMeasurableService.cs` interface to use TargetMeasurable
4. Update all usages to TargetMeasurable

**Phase 2 Code Migration:**

| File | Type | Impact | Effort |
|------|------|--------|--------|
| MeasurableService.cs | Service | Update methods to use TargetMeasurable | 2 hours |
| OkrsViewModel.cs | ViewModel | Update collections/methods | 1.5 hours |
| KeyResultViewModel.cs | ViewModel | Update properties | 1 hour |
| MeasurableViewModel.cs | ViewModel | Update creation logic | 1 hour |
| OkrsControl.xaml.cs | View | Update event handlers | 30 min |
| TrackerDbManager.cs | Service | Remove DeleteKeyResultMeasurableAsync | 30 min |
| ObjectiveKeyResult.cs | Model | Update Measurables property type | 30 min |
| IMeasurableService.cs | Interface | Update signatures | 30 min |
| Tests | Test | Update test data builders, assertions | 2 hours |

**Total Effort:** 9-10 hours (MEDIUM complexity)

### Why This is Safe

1. **KeyResult is already being phased out** - Goal uses Target now
2. **TargetMeasurable already exists** - Not a new model, just new to use
3. **Same polymorphic pattern** - Just using Guid instead of int
4. **Same aggregation logic** - AggregationTypeEnum works for both
5. **Supabase confirms rename** - Schema comment: "was KeyResultMeasurables"
6. **All UI is WPF** - Can update view models and everything stays working

### Why NOT to Keep Both

- ❌ Supabase only has `target_measurables` table - KeyResult table doesn't exist
- ❌ Redundant - Identical functionality
- ❌ Confusing API - "Which measurable type should I use?"
- ❌ Migration nightmare - Have to map both during data import
- ❌ Two parallel code paths that do the same thing
- ❌ KeyResult is being phased out anyway

---

## Code Usage Pattern

**Current (Broken) Dual Pattern:**
```csharp
// OLD - KeyResult path (still in code)
KeyResult kr = new();
kr.Measurables = new List<KeyResultMeasurable>();

// NEW - Target path (what Supabase wants)
Target target = new();
target.Measurables = new List<TargetMeasurable>();  // Already exists in model
```

**After Consolidation:**
```csharp
// ONLY - Target path
Target target = new();
target.Measurables = new List<TargetMeasurable>();  // Single, canonical approach
```

---

## File Dependencies Found

**Direct References to KeyResultMeasurable:**
1. `MeasurableService.cs` - Core service, 3 methods
2. `OkrsViewModel.cs` - ViewModel, collection + methods
3. `KeyResultViewModel.cs` - Dialog ViewModel, properties + collections
4. `MeasurableViewModel.cs` - Dialog ViewModel, creation logic
5. `OkrsControl.xaml.cs` - View code-behind, event handler
6. `TrackerDbManager.cs` - Database layer, deletion method
7. `IMeasurableService.cs` - Interface, 2 method signatures
8. `ObjectiveKeyResult.cs` - Legacy model, Measurables property
9. `TrackerDbContext.cs` - EF Core context, DbSet
10. Test files - EntityCrudTests, TrackerDbManagerTests, etc.

**Total files to update:** ~15-20

---

## Implementation Order (Phase 2)

1. **Update Service Layer:**
   - MeasurableService: Replace KeyResultMeasurable with TargetMeasurable throughout
   - IMeasurableService: Update interface method signatures

2. **Update ViewModels:**
   - KeyResultViewModel: Change collection type to TargetMeasurable
   - MeasurableViewModel: Update creation logic
   - OkrsViewModel: Update handling

3. **Update Models:**
   - ObjectiveKeyResult: If still exists, update Measurables type
   - TrackerDbContext: Remove KeyResultMeasurable DbSet and config

4. **Update Views:**
   - OkrsControl.xaml.cs: Update event handler type

5. **Update Database Layer:**
   - TrackerDbManager: Remove DeleteKeyResultMeasurableAsync method

6. **Update Tests:**
   - Test data builders
   - Test assertions
   - Test helper methods

---

## Weight and SortOrder Handling

**Problem:** TargetMeasurable doesn't have Weight or SortOrder in Supabase schema

**Current Usage in KeyResultMeasurable:**
- Weight: Used for weighted aggregation (default 1.0)
- SortOrder: Used for display ordering (computed property in model)

**Solution for Phase 2:**
1. **Weighted Aggregation:** Move Weight logic to service layer (computed, not persisted)
2. **Sort Ordering:** Move SortOrder to ViewModel if needed, or use Id ordering

**This is safe because:**
- Weights are used during calculation (service layer), not stored
- Ordering can be done in ViewModel/UI tier
- Supabase schema doesn't include them = they weren't critical to persist

---

## Consolidation Status

✅ Analysis Complete
✅ Schema confirms rename (target_measurables was KeyResultMeasurables)
✅ Implementation pattern identified
✅ Phase 2 work documented
⏳ Awaiting User Decision

---

## Decision Required

**Should we DELETE KeyResultMeasurable and consolidate to TargetMeasurable?**

- ✅ YES → Proceed with deletion and Phase 2 migration (9-10 hours work)
- ⏳ NO / MAYBE → Document for later review