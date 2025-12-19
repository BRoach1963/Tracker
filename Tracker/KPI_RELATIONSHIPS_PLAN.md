# KPI Relationships Plan

## Current State
- ✅ KPIs can link to **OKRs** via `OkrId` (optional)
- ✅ KPIs can link to **1:1s** via `OneOnOneLinkedKpi` junction table
- ❌ KPIs **CANNOT** link to **Projects** directly (only indirectly through OKRs)
- ❌ KPIs **CANNOT** link to **Tasks** directly

## Proposed Changes

### 1. Make OKR ProjectId Required
- Add FK constraint: `ProjectId` → `Project.ID` (REQUIRED)
- OKRs MUST belong to a Project

### 2. Add Optional ProjectId FK to KPI
- Add `ProjectId` property to `KeyPerformanceIndicator`
- FK: `ProjectId` → `Project.ID` (NULLABLE)
- Allows standalone project-level KPIs (e.g., "Project completion rate", "Budget adherence")

### 3. Add Optional TaskId FK to KPI
- Add `TaskId` property to `KeyPerformanceIndicator`
- FK: `TaskId` → `IndividualTask.Id` (NULLABLE)
- Allows task-level KPIs (e.g., "Task completion velocity", "Task quality score")

### 4. Keep Existing Relationships
- ✅ `OkrId` → `OKR.ObjectiveId` (NULLABLE) - for OKR Key Results
- ✅ `OneOnOneLinkedKpi` junction table - for 1:1 discussions

## Final KPI Relationships

A KPI can be linked to **ONE** of:
- **Project** (via `ProjectId`) - Project-level metrics
- **Task** (via `TaskId`) - Task-level metrics  
- **OKR** (via `OkrId`) - OKR Key Results
- **None** (all null) - Standalone operational KPIs

A KPI can be **discussed** in multiple **1:1s** (via junction table).

## Use Cases for Team Management

1. **Project KPIs**: "Project completion rate", "Budget adherence", "Timeline accuracy"
2. **Task KPIs**: "Task completion velocity", "Task quality score", "On-time delivery rate"
3. **OKR KPIs**: Key Results that measure OKR progress
4. **Standalone KPIs**: "Team velocity", "System uptime", "Code quality metrics"
5. **1:1 KPIs**: Any KPI discussed in a meeting (via junction table)

