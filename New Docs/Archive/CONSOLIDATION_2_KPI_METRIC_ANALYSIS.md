# Consolidation #2 Analysis: KeyPerformanceIndicator → Metric

**Status:** Analysis Complete - READY FOR DECISION
**Date:** 2026-01-10
**Scope:** KPI vs Metric model consolidation

---

## Executive Summary

Two models exist that serve identical purposes:
1. **KeyPerformanceIndicator.cs** - int-based, legacy (238 lines)
2. **Metric.cs** - Guid-based, new (183 lines)

**Finding:** These are NOT separate concepts. They are the **same model at different stages of migration**. The Supabase schema only has `metrics` table (renamed from KPI). There is NO separate KPI table in the target schema.

**Recommendation:** **CONSOLIDATE: Delete KeyPerformanceIndicator, use Metric as single source of truth**

---

## Model Comparison

### KeyPerformanceIndicator.cs (Legacy)
```
ID Type:          int KpiId (auto-increment)
Schema Table:     key_performance_indicators (legacy SQL Server/PostgreSQL only)
Interfaces:       IMeasurable, IKpiSource
Hierarchy:        Composite (ParentKpiId, ChildKpis)
Status:           KpiStatusEnum (Green, Amber, Red)
Data Sources:     List<KpiDataSource>
Implementation:   Detailed status calculation logic
```

**Key Properties:**
- `int KpiId` - Primary key, int-based
- `Guid? OrganizationId` - Nullable (compatibility layer)
- `string Name, Description`
- `double Value, TargetValue, Unit`
- `TargetDirectionEnum` - enum (GreaterOrEqual, LessOrEqual)
- `KpiFrequencyEnum` - enum (OnDemand, Daily, Weekly, Monthly, etc.)
- `bool IsComposite, int? ParentKpiId`
- `List<KeyPerformanceIndicator> ChildKpis`
- `List<KpiDataSource> DataSources`
- **Status calculation:** Custom green/amber/red logic with 90% threshold
- **Obsolete notes:** `IMeasurable.GuidId` returns Guid.Empty

### Metric.cs (New)
```
ID Type:          Guid Id (UUID)
Schema Table:     metrics (Supabase - target)
Interfaces:       IMeasurable
Hierarchy:        Composite (ParentMetricId, ChildMetrics)
Status:           OkrStatus enum (OnTrack, AtRisk, OffTrack, Completed)
Data Sources:     List<MetricDataSource>
Implementation:   Threshold-based status calculation
```

**Key Properties:**
- `Guid Id` - Primary key, UUID
- `Guid OrganizationId` - Required FK
- `Guid? OwnerTeamMemberId` - Optional owner
- `Guid CreatedByUserId` - Creation tracking
- `string Name, Description, Category`
- `decimal CurrentValue, TargetValue, BaselineValue`
- `string Unit`
- `MetricTargetDirection` - enum (HigherIsBetter, LowerIsBetter, TargetValue)
- `MetricFrequency` - enum (Daily, Weekly, Monthly, Quarterly, Annually)
- `bool IsComposite, Guid? ParentMetricId`
- `List<Metric> ChildMetrics`
- `List<MetricDataSource> DataSources`
- `List<MetricHistory> History` - NEW: tracking changes over time
- **Visibility:** `bool IsTeamVisible, IsOrgVisible`
- **Thresholds:** `decimal? WarningThreshold, CriticalThreshold`
- **Status calculation:** Warning → At Risk, Critical → Off Track

---

## Schema Analysis

### Supabase (Target Schema)
```sql
CREATE TABLE metrics (
    id UUID PRIMARY KEY,
    organization_id UUID NOT NULL,
    owner_team_member_id UUID,
    created_by_user_id UUID NOT NULL,
    name VARCHAR(200),
    description TEXT,
    category VARCHAR(100),
    current_value DECIMAL(18,4),
    target_value DECIMAL(18,4),
    baseline_value DECIMAL(18,4),
    unit VARCHAR(50),
    target_direction metric_target_direction,
    frequency metric_frequency,
    last_updated_at TIMESTAMPTZ,
    is_composite BOOLEAN,
    parent_metric_id UUID,
    is_team_visible BOOLEAN,
    is_org_visible BOOLEAN,
    warning_threshold DECIMAL(18,4),
    critical_threshold DECIMAL(18,4),
    ... audit fields ...
);

CREATE TABLE metric_data_sources (
    id UUID,
    metric_id UUID,
    source_type VARCHAR(50),
    source_id UUID,
    source_config JSONB,
    aggregation_type VARCHAR(50),
    ... audit fields ...
);

CREATE TABLE metric_history (
    id UUID,
    metric_id UUID,
    value_at DECIMAL(18,4),
    recorded_at TIMESTAMPTZ,
    recorded_by_user_id UUID,
    ... audit fields ...
);
```

**Legacy Schema (SQL Server / PostgreSQL Only):**
```sql
CREATE TABLE KeyPerformanceIndicators (
    KpiId INT PRIMARY KEY,
    OwnerId INT,
    [Name] VARCHAR(200),
    [Value] FLOAT,
    TargetValue FLOAT,
    Unit VARCHAR(50),
    ...
);

CREATE TABLE OneOnOneLinkedKpis (  -- ⚠️ TO BE DELETED (OneOnOne deleted)
    OneOnOneId INT,
    KpiId INT,
    ...
);
```

**Finding:** No `key_performance_indicators` table in Supabase. Legacy KPI data should not exist in new schema.

---

## Code Usage Analysis

### KeyPerformanceIndicator References Found (50+ matches)

**High-Risk (Direct Usage):**
1. **Database Layer** (TrackerDbManager.cs):
   - `GetKpiMeetingCountAsync(int kpiId)` - Queries OneOnOneLinkedKpis
   - `LinkKpiToMeetingAsync(int oneOnOneId, int kpiId)` - Links KPI to meetings
   - `UnlinkKpiFromMeetingAsync(int oneOnOneId, int kpiId)` - Unlinks KPI from meetings
   - References to `OneOnOneLinkedKpis` (1:1 linking - WILL BE DELETED with OneOnOne)

2. **Tests** (EntityCrudTests.cs, KeyPerformanceIndicatorTests.cs):
   - 10+ unit tests for KPI CRUD
   - Status calculation tests
   - Test data builders

3. **Legacy Documentation** (USER_OWNERSHIP_ARCHITECTURE.md):
   - Lists KPI as separate from Metric
   - Describes OneOnOne → KPI linking

**Low-Risk (References, not implementations):**
- ViewModels that might reference KPI (to be identified in Phase 2)
- Services that might use KPI (to be identified in Phase 2)

### Metric References Found

- Core model (Metric.cs)
- Limited current usage (new model, not yet integrated)
- Test infrastructure for Metric

---

## Consolidation Decision Matrix

| Aspect | KeyPerformanceIndicator | Metric | Winner |
|--------|---|---|---|
| **Schema Match** | ❌ No table in Supabase | ✅ Direct match (metrics table) | **Metric** |
| **ID Type** | ❌ int (legacy) | ✅ Guid (modern) | **Metric** |
| **Organization Support** | ❌ Nullable (compat) | ✅ Required FK | **Metric** |
| **Audit Fields** | ✅ AuditableEntity | ✅ AuditableEntity | **Tie** |
| **Status Calculation** | ✅ Detailed (90% threshold) | ✅ Flexible (warning/critical) | **Metric** |
| **Hierarchy Support** | ✅ Composite | ✅ Composite | **Tie** |
| **Data Sources** | ✅ KpiDataSource | ✅ MetricDataSource | **Tie** |
| **History Tracking** | ❌ None | ✅ MetricHistory | **Metric** |
| **Visibility Control** | ❌ No team/org visibility | ✅ IsTeamVisible, IsOrgVisible | **Metric** |
| **Thresholds** | ❌ Hard-coded 90% | ✅ Configurable warning/critical | **Metric** |
| **Ownership** | ✅ Owner (TeamMember) | ✅ Owner + CreatedBy | **Metric** |

**Clear Winner:** Metric (9/11 categories)

---

## Breaking Changes & Phase 2 Work

### Option A: DELETE KeyPerformanceIndicator (Recommended)

**Immediate Action:**
1. Delete `DataModels/KeyPerformanceIndicator.cs`
2. Delete `DataModels/KpiDataSource.cs`
3. Delete `DataModels/KpiFrequencyEnum.cs`
4. Delete `DataModels/KpiStatusEnum.cs`
5. Delete `Tests/DataModels/KeyPerformanceIndicatorTests.cs`
6. Delete reference to `OneOnOneLinkedKpis` (depends on deleted OneOnOne model anyway)

**Phase 2 Code Migration:**

| File | Type | Impact | Effort |
|------|------|--------|--------|
| TrackerDbManager.cs | Service | Remove GetKpiMeetingCountAsync, GetKpiMeetingCountsAsync, LinkKpiToMeetingAsync, UnlinkKpiFromMeetingAsync | 2 hours |
| EntityCrudTests.cs | Test | Remove CanCreate_StandaloneKpi test, KPI CRUD tests | 1 hour |
| TestDataBuilder.cs | Test Helper | Remove CreateKpi method | 30 min |
| USER_OWNERSHIP_ARCHITECTURE.md | Docs | Update to remove KPI section, consolidate into Metric | 30 min |
| Any ViewModels referencing KPI | ViewModel | Update to use Metric instead | TBD (discovery needed) |
| Any Services referencing KPI | Service | Update to use Metric instead | TBD (discovery needed) |

**Total Effort:** 4-6 hours + discovery

### Option B: Keep Both (Not Recommended)

**Why not:**
- ❌ Supabase only has `metrics` table - no place for KPI data
- ❌ Redundant - identical functionality
- ❌ Confusing API - "Should I use KPI or Metric?"
- ❌ Migration nightmare - have to map both during data import
- ❌ Tests bloat - maintain tests for both

---

## Recommended Action Plan

### Phase 1B (Next, Now)

1. **Delete KPI models:**
   - KeyPerformanceIndicator.cs
   - KpiDataSource.cs
   - KpiStatusEnum.cs
   - KpiFrequencyEnum.cs

2. **Delete KPI tests:**
   - KeyPerformanceIndicatorTests.cs
   - KPI CRUD test in EntityCrudTests.cs

3. **Update tracking doc:**
   - Document consolidation decision
   - List Phase 2 breaking changes
   - Note orphaned database layer methods

### Phase 2 (Migration)

1. **Update TrackerDbManager.cs:**
   - Refactor KPI linking methods to use Metric
   - Remove OneOnOneLinkedKpis references (safe: OneOnOne deleted)
   - Update method names to reflect Metric usage

2. **Update ViewModels/Services:**
   - Find all KPI references via grep
   - Replace with Metric equivalents
   - Update status enum checks (KpiStatusEnum → OkrStatus)

3. **Update Tests:**
   - Remove CreateKpi helper
   - Add CreateMetric helper (or use existing)
   - Update data builders

4. **Update Documentation:**
   - Remove KPI references
   - Consolidate under "Metric (formerly KPI)"
   - Update architecture docs

---

## Subabase Data Migration Notes

**KPI Data in Legacy Systems:**
- Any KPI data in SQL Server / PostgreSQL must be:
  - Converted from int → Guid ID
  - Updated OrganizationId from nullable → required
  - Mapped to `metrics` table
  - `KpiDataSource` → `metric_data_sources`

**Status Enum Mapping:**
```
Legacy:  KpiStatusEnum.Green      → New: OkrStatus.OnTrack
Legacy:  KpiStatusEnum.Amber      → New: OkrStatus.AtRisk  
Legacy:  KpiStatusEnum.Red        → New: OkrStatus.OffTrack
Legacy:  KpiStatusEnum.OnTarget   → New: OkrStatus.OnTrack
Legacy:  KpiStatusEnum.CloseToTarget → New: OkrStatus.AtRisk
Legacy:  KpiStatusEnum.OffTarget  → New: OkrStatus.OffTrack
```

**OneOnOneLinkedKpis Handling:**
- Mapping for 1:1 meeting → KPI links needs to be determined:
  - Might become: Meeting → Metric source tracking (if applicable)
  - Or might be: Source tracking via AgendaItem (separate consolidation)
  - Note: OneOnOne model is DELETED, so this table has no parent anyway

---

## Files Affected by Consolidation

**To Delete:**
1. `DataModels/KeyPerformanceIndicator.cs`
2. `DataModels/KpiDataSource.cs`
3. `DataModels/KpiStatusEnum.cs`
4. `DataModels/KpiFrequencyEnum.cs`
5. `Tests/DataModels/KeyPerformanceIndicatorTests.cs`

**To Update (Phase 2):**
1. `Database/TrackerDbManager.cs` - 6 methods
2. `Tests/Database/EntityCrudTests.cs` - KPI tests
3. `Tests/Infrastructure/TestDataBuilder.cs` - CreateKpi method
4. Documentation files
5. ViewModels/Services (TBD via grep)

**To Validate:**
1. `DataModels/Metric.cs` - Verify complete
2. `DataModels/MetricDataSource.cs` - Verify exists
3. `Database/Supabase/05_METRICS.sql` - Already correct

---

## Estimated Phase 2 Effort

- **Complexity:** 🟢 LOW
- **Risk:** 🟢 LOW (clear consolidation, one winner)
- **Total Hours:** 4-6 hours
- **Files Affected:** 10-15 files

---

## Consolidation Status

✅ Analysis Complete
⏳ Awaiting User Approval
⏳ Phase 1B: File Deletion
⏳ Phase 2: Code Migration & Tests

---

## Decision Required

**Should we DELETE KeyPerformanceIndicator and consolidate to Metric?**

- ✅ YES → Proceed with deletion and Phase 2 migration
- ⏳ NO / MAYBE → Document for later review