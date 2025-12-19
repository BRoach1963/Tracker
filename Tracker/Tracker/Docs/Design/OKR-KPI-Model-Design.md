# OKR/KPI/Project/Task Model Design Document

**Version:** 1.0  
**Last Updated:** December 2024  
**Status:** Approved for Implementation

---

## Executive Summary

This document defines the data model and UI design for the OKR (Objectives and Key Results), KPI (Key Performance Indicators), Project, and Task hierarchy in the Tracker application. The design prioritizes **simplicity for end users** while maintaining **flexibility** for various use cases.

---

## Core Philosophy

### Manager-Centric Design
The Tracker application is designed for **team managers** who need to:
- Track team performance through measurable outcomes
- Monitor project progress without deep PM complexity
- Connect daily work (tasks) to strategic goals (OKRs)
- Have flexibility in how they structure their tracking

### Key Design Principle: Nothing is Forced
While there are logical hierarchies, **no relationship is mandatory**. A manager can:
- Create standalone KPIs not tied to any OKR
- Create projects without linking to KPIs
- Create tasks without projects
- Build full hierarchies when it makes sense

---

## Entity Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                              OBJECTIVE (OKR)                                │
│                    "What do we want to achieve?"                            │
│                                                                             │
│                                    │                                        │
│                                    │ MUST have 1+ to be measurable          │
│                                    ▼                                        │
│                                                                             │
│                              KEY RESULT (KR)                                │
│                    "How do we measure success?"                             │
│                                                                             │
│                          ┌───────┼───────┐                                  │
│                          │       │       │                                  │
│                          ▼       ▼       ▼                                  │
│                                                                             │
│                    ┌─────────────────────────────┐                          │
│                    │        IMeasurable          │                          │
│                    │  (Sources that feed a KR)   │                          │
│                    └─────────────────────────────┘                          │
│                          │       │       │                                  │
│                          ▼       ▼       ▼                                  │
│                                                                             │
│                       KPI    Project    Task                                │
│                        │        │     Collection                            │
│                        │        │                                           │
│                        │        ▼                                           │
│                        │     Tasks                                          │
│                        │                                                    │
│                        ▼                                                    │
│                   ┌─────────────────────────────┐                          │
│                   │        IKpiSource           │                          │
│                   │  (Sources that feed a KPI)  │                          │
│                   └─────────────────────────────┘                          │
│                          │       │       │                                  │
│                          ▼       ▼       ▼                                  │
│                                                                             │
│                    Project   Task    Child KPIs                             │
│                       │    Collection (Composite)                           │
│                       │                                                     │
│                       ▼                                                     │
│                    Tasks                                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Entity Definitions

### 1. Objective (OKR)
The qualitative goal - the "what" we want to achieve.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Primary key |
| Title | string | Yes | The objective statement |
| Description | string | No | Extended description |
| OwnerId | int | No | FK to TeamMember |
| TimePeriod | enum | Yes | Quarter (Q1-Q4) or Custom |
| StartDate | DateTime | Yes | Period start |
| EndDate | DateTime | Yes | Period end |
| Status | enum | Yes | NotStarted, OnTrack, AtRisk, OffTrack, Completed |
| Progress | decimal | Calculated | Weighted average of Key Results |
| KeyResults | List | Yes (1+) | Child Key Results |

**Business Rules:**
- An Objective without Key Results is not measurable (warn user, don't prevent)
- Progress is automatically calculated from Key Results
- Status can be manual override or auto-calculated from progress

---

### 2. Key Result (KR)
The measurable outcome - the "how" we measure success. **Key Results are NOT standalone entities** - they exist only within an OKR.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Primary key |
| OkrId | int | Yes | FK to parent OKR |
| Title | string | Yes | What we're measuring |
| TargetValue | decimal | Yes | Goal to achieve |
| CurrentValue | decimal | Yes | Current state |
| StartingValue | decimal | No | Baseline (default 0) |
| Unit | string | Yes | %, points, hours, count, etc. |
| Weight | decimal | No | For weighted averaging (default equal) |
| Measurables | List | No | Sources feeding this KR |

**Business Rules:**
- Progress = (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
- CurrentValue can be manually entered OR calculated from Measurables
- If Measurables exist, CurrentValue is auto-calculated (but can be overridden)

---

### 3. KPI (Key Performance Indicator)
A standalone metric that can exist independently or feed into Key Results.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Primary key |
| Name | string | Yes | KPI name |
| Description | string | No | What this measures |
| Value | decimal | Yes | Current value |
| TargetValue | decimal | Yes | Target value |
| Unit | string | Yes | %, points, $, count, etc. |
| Category | string | No | Grouping category |
| Frequency | enum | No | Daily, Weekly, Monthly, Quarterly |
| IsComposite | bool | No | True if calculated from child KPIs |
| DataSources | List | No | What feeds this KPI |

**Business Rules:**
- KPIs can exist without being linked to any OKR/KR
- Composite KPIs calculate value from child KPIs
- Non-composite KPIs can pull from Projects, Tasks, or manual entry
- Status is calculated: Green (≥90% of target), Amber (70-89%), Red (<70%)

---

### 4. Project
A deliverable with defined scope and timeline.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Primary key |
| Name | string | Yes | Project name |
| Description | string | No | Project description |
| Status | enum | Yes | NotStarted, InProgress, OnHold, Completed, Cancelled |
| StartDate | DateTime | No | Planned start |
| EndDate | DateTime | No | Planned end |
| Progress | decimal | Calculated | From task completion |
| Tasks | List | No | Child tasks |

**Business Rules:**
- Projects can exist without being linked to KPIs or KRs
- Progress = (Completed Tasks / Total Tasks) × 100
- Projects provide IMeasurable interface for KRs
- Projects provide IKpiSource interface for KPIs

---

### 5. Task
The atomic unit of work.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Primary key |
| Title | string | Yes | Task title |
| Description | string | No | Task details |
| Status | enum | Yes | NotStarted, InProgress, Completed, Blocked |
| DueDate | DateTime | No | When it's due |
| ProjectId | int | No | FK to parent Project (optional) |
| AssigneeId | int | No | FK to TeamMember |
| ParentTaskId | int | No | FK for subtasks |
| Subtasks | List | No | Child tasks |

**Business Rules:**
- Tasks can exist without a parent Project
- Tasks with subtasks calculate progress from subtask completion
- Tasks provide IMeasurable interface (individually or as collections)

---

## Interface Definitions

### IMeasurable
Implemented by entities that can feed a Key Result.

```csharp
public interface IMeasurable
{
    int Id { get; }
    string DisplayName { get; }
    decimal Progress { get; }           // 0-100
    string DisplayValue { get; }        // "75%" or "3/4 tasks" or "53 NPS"
    MeasurableType Type { get; }        // KPI, Project, TaskCollection
}

public enum MeasurableType
{
    KPI,
    Project,
    TaskCollection
}
```

**Implementers:**
- `KPI` - Progress based on Value vs TargetValue
- `Project` - Progress based on task completion
- `TaskCollection` - Progress based on completed vs total tasks

---

### IKpiSource
Implemented by entities that can feed a KPI's value.

```csharp
public interface IKpiSource
{
    int Id { get; }
    string DisplayName { get; }
    decimal GetValue();                 // The numeric value to contribute
    KpiSourceType SourceType { get; }   // Project, TaskQuery, ChildKpi, Manual
}

public enum KpiSourceType
{
    Project,        // Project completion %
    TaskQuery,      // Count of tasks matching criteria
    ChildKpi,       // Another KPI (for composites)
    Manual          // Manually entered value
}
```

---

## Relationship Matrix

### What Can Link To What

| Entity | Can Link To | Relationship Type |
|--------|-------------|-------------------|
| OKR | Key Results | Parent-Child (required 1+) |
| Key Result | KPI | IMeasurable (optional) |
| Key Result | Project | IMeasurable (optional) |
| Key Result | TaskCollection | IMeasurable (optional) |
| KPI | Project | IKpiSource (optional) |
| KPI | TaskCollection | IKpiSource (optional) |
| KPI | Child KPIs | IKpiSource (composite only) |
| Project | Tasks | Parent-Child (optional but typical) |
| Task | Subtasks | Parent-Child (optional) |

### Valid "Skip" Scenarios

These direct links that skip intermediate levels are **valid**:

| Skip | Valid? | Use Case |
|------|--------|----------|
| KR → Project (skip KPI) | ✅ Yes | "Launch new feature" - deliverable, not metric |
| KR → Tasks (skip KPI & Project) | ✅ Yes | "Complete 10 customer interviews" |
| KPI → Tasks (skip Project) | ✅ Yes | "Count of support tickets closed" |
| Task → KR (skip Project & KPI) | ✅ Yes | Ad-hoc tasks for a key result |

### Invalid Links

| Link | Valid? | Why |
|------|--------|-----|
| OKR → KPI directly | ❌ No | Must go through Key Result |
| OKR → Project directly | ❌ No | Must go through Key Result |
| OKR → Task directly | ❌ No | Must go through Key Result |
| KR without OKR | ❌ No | KR is meaningless standalone |

---

## Required vs Optional Dependencies

### REQUIRED (Must Have)
| Rule | Reason |
|------|--------|
| OKR must have 1+ Key Results | Otherwise not measurable |
| Key Result must belong to OKR | Meaningless standalone |
| Composite KPI must have child KPIs | Otherwise not composite |

### OPTIONAL (Can Have)
| Rule | Reason |
|------|--------|
| KR can have 0+ Measurables | Manual entry is valid |
| KPI can have 0+ Sources | Manual entry is valid |
| KPI can link to 0+ KRs | Standalone metrics valid |
| Project can have 0+ Tasks | Planning phase |
| Project can link to 0+ KPIs | Standalone projects valid |
| Task can have 0+ Subtasks | Simple tasks valid |
| Task can belong to 0-1 Projects | Standalone tasks valid |

---

## Progress Calculation

### OKR Progress
```
OKR.Progress = WeightedAverage(KeyResults.Progress)

If weights not specified:
OKR.Progress = Average(KeyResults.Progress)
```

### Key Result Progress
```
If Measurables exist:
    KR.CurrentValue = Aggregate(Measurables)  // Sum, Avg, or custom
    
KR.Progress = (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
```

### KPI Value (Non-Composite)
```
If DataSources exist:
    KPI.Value = Aggregate(DataSources.GetValue())
Else:
    KPI.Value = ManualEntry
```

### KPI Value (Composite)
```
KPI.Value = Aggregate(ChildKPIs.Value)  // Sum, Avg, Min, Max, or custom
```

### Project Progress
```
Project.Progress = (CompletedTasks.Count / AllTasks.Count) × 100
```

---

## UI Design

### OKR Card (List View)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎯  Improve Customer Satisfaction                             ● On Track  │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Q1 2025  •  Sarah Johnson                     68% ███████████░░░░░░░░░░░  │
│                                                                             │
│  KEY RESULTS                                                                │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ ✓ Increase NPS from 45 to 60              53/60   ████████████░░░░░░░ │  │
│  │ ◐ Reduce response time to < 2 hrs         2.3 hrs ██████████░░░░░░░░░ │  │
│  │ ○ Achieve 95% satisfaction rating         89%     █████████░░░░░░░░░░ │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                                      │
│  │ 3 KPIs  │  │ 2 Proj  │  │ 8 Tasks │                   [Edit] [Details]  │
│  └─────────┘  └─────────┘  └─────────┘                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Card Elements:**
- Status indicator (🟢 On Track, 🟡 At Risk, 🔴 Off Track)
- Overall progress bar (weighted avg of KRs)
- Inline KR mini-list with individual progress
- Measurable counts (quick reference)
- Owner and time period

---

### OKR Add/Edit Dialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ✕                           Edit OKR                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  OBJECTIVE                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Improve Customer Satisfaction                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌─────────────────┐   │
│  │ Owner: [Sarah     ▼] │  │ Period: [Q1 2025  ▼] │  │ Status: [OnTrack│   │
│  └──────────────────────┘  └──────────────────────┘  └─────────────────┘   │
│                                                                             │
│ ═══════════════════════════════════════════════════════════════════════════ │
│                                                                             │
│  KEY RESULTS                                                    [+ Add KR]  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ KR 1: Increase NPS Score                                    [≡] [✕] │   │
│  │ ───────────────────────────────────────────────────────────────────  │   │
│  │ Target: [60] Current: [53] Unit: [points ▼]           88% ████░░░░  │   │
│  │                                                                      │   │
│  │ MEASURABLES                                          [+ Add Source]  │   │
│  │ ┌──────────────────────────────────────────────────────────────────┐ │   │
│  │ │ 📊 KPI: Customer NPS Score              Current: 53    [✕]       │ │   │
│  │ │ 📁 Project: Support Portal Redesign     Progress: 75%  [✕]       │ │   │
│  │ └──────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [Additional KRs follow same pattern...]                                    │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                              [Cancel]    [Save OKR]         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Add Measurable Popup

When user clicks "+ Add Source" on a Key Result:

```
┌─────────────────────────────────────────────────────────────────┐
│  Add Measurable to Key Result                                   │
│ ───────────────────────────────────────────────────────────────  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 📊  KPI                                                     ││
│  │     Track a metric that measures this result                ││
│  │     [Select existing ▼] or [+ Create New]                   ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 📁  Project                                                 ││
│  │     Track a deliverable that contributes to this result     ││
│  │     [Select existing ▼] or [+ Create New]                   ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ ✓   Tasks                                                   ││
│  │     Count task completions for this result                  ││
│  │     [Select tasks to include...]                            ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│                                     [Cancel]    [Add Selected]  │
└─────────────────────────────────────────────────────────────────┘
```

---

## KPI Page Design

Since KPIs can exist independently, they need their own visualization:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📊  Customer NPS Score                                        ● On Target │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Current: 53    Target: 60    Unit: points                                  │
│  ████████████████████████████████████░░░░░░░░░░░░  88%                     │
│                                                                             │
│  DATA SOURCES                                                               │
│  └── Manual Entry (updated weekly)                                          │
│                                                                             │
│  LINKED TO                                                                  │
│  └── KR: Increase NPS from 45 to 60 (OKR: Improve Customer Satisfaction)   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Notes

### Database Schema Additions

New tables/columns needed:

```sql
-- Key Result entity (if not already separate from OKR)
CREATE TABLE KeyResults (
    Id INTEGER PRIMARY KEY,
    OkrId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    TargetValue DECIMAL NOT NULL,
    CurrentValue DECIMAL NOT NULL,
    StartingValue DECIMAL DEFAULT 0,
    Unit TEXT NOT NULL,
    Weight DECIMAL DEFAULT 1.0,
    SortOrder INTEGER DEFAULT 0,
    FOREIGN KEY (OkrId) REFERENCES ObjectiveKeyResults(Id)
);

-- Measurable links for Key Results
CREATE TABLE KeyResultMeasurables (
    Id INTEGER PRIMARY KEY,
    KeyResultId INTEGER NOT NULL,
    MeasurableType TEXT NOT NULL,  -- 'KPI', 'Project', 'TaskCollection'
    MeasurableId INTEGER NOT NULL, -- FK to appropriate table
    AggregationType TEXT DEFAULT 'Latest', -- 'Latest', 'Sum', 'Average'
    FOREIGN KEY (KeyResultId) REFERENCES KeyResults(Id)
);

-- KPI Data Sources
CREATE TABLE KpiDataSources (
    Id INTEGER PRIMARY KEY,
    KpiId INTEGER NOT NULL,
    SourceType TEXT NOT NULL,  -- 'Project', 'TaskQuery', 'ChildKpi', 'Manual'
    SourceId INTEGER,          -- FK to appropriate table (null for Manual)
    AggregationType TEXT DEFAULT 'Latest',
    FOREIGN KEY (KpiId) REFERENCES KeyPerformanceIndicators(Id)
);

-- Task Collections (for grouping tasks as measurables)
CREATE TABLE TaskCollections (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT
);

CREATE TABLE TaskCollectionItems (
    Id INTEGER PRIMARY KEY,
    CollectionId INTEGER NOT NULL,
    TaskId INTEGER NOT NULL,
    FOREIGN KEY (CollectionId) REFERENCES TaskCollections(Id),
    FOREIGN KEY (TaskId) REFERENCES Tasks(Id)
);
```

### Service Layer

New services needed:

```csharp
public interface IMeasurableService
{
    Task<decimal> GetProgressAsync(IMeasurable measurable);
    Task<string> GetDisplayValueAsync(IMeasurable measurable);
    Task<List<IMeasurable>> GetMeasurablesForKeyResultAsync(int keyResultId);
}

public interface IKpiCalculationService
{
    Task<decimal> CalculateKpiValueAsync(int kpiId);
    Task<decimal> CalculateCompositeKpiValueAsync(int kpiId);
    Task RefreshAllKpiValuesAsync();
}

public interface IOkrProgressService
{
    Task<decimal> CalculateKeyResultProgressAsync(int keyResultId);
    Task<decimal> CalculateOkrProgressAsync(int okrId);
    Task<OkrStatus> DetermineOkrStatusAsync(int okrId);
    Task RefreshAllOkrProgressAsync();
}
```

---

## Migration Path

### Phase 1: Data Model ✅ COMPLETED (Dec 2024)
1. ✅ Add KeyResult as separate entity (if currently embedded in OKR)
2. ✅ Add KeyResultMeasurables linking table
3. ✅ Add KpiDataSources linking table
4. ✅ Add TaskCollections for grouped task measurables
5. ✅ Implement IMeasurable interface on KPI, Project, TaskCollection

**Implementation Notes:**
- New files: `KeyResult.cs`, `KeyResultMeasurable.cs`, `KpiDataSource.cs`, `TaskCollection.cs`, `TaskCollectionItem.cs`
- New interfaces: `IMeasurable.cs`, `IKpiSource.cs`
- New enums: `TimePeriodEnum`, `AggregationTypeEnum`, `KpiFrequencyEnum`
- Updated: `ObjectiveKeyResult` (uses `List<KeyResult>`), `KeyPerformanceIndicator` (removed OkrId, implements interfaces), `Project` (implements interfaces), `IndividualTask` (added ProjectId, ParentTaskId)
- Updated: `TrackerDbContext`, `DatabaseSeeder`, mock data files, ViewModels (minimal changes to compile)

### Phase 2: Business Logic
1. Implement progress calculation services
2. Add automatic progress updates when linked entities change
3. Implement aggregation logic (sum, average, latest)

### Phase 3: UI
1. Redesign OKR Add/Edit dialog with inline KR editing
2. Create Add Measurable popup/dialog
3. Update OKR cards to show KR progress inline
4. Update KPI page to show linked KRs

---

## UI Design Decisions (Dec 2024)

### OKR Page - 3-Panel Layout

**Agreed design:**
- **Left Panel (large)**: OKR Cards with summary (title, progress bar, status, period, KR count)
- **Top-Right Panel**: Key Results list for selected OKR
- **Bottom-Right Panel**: KR Details + Linked Measurables

```
┌─────────────────────────────────────────┬─────────────────────────────────┐
│         OKR CARDS                       │         KEY RESULTS             │
│  ┌───────────────────────────────────┐  │  ┌─────────────────────────────┐│
│  │ 🎯 Improve Customer Satisfaction  │  │  │ KR 1: Increase NPS to 60   ││
│  │ Q1 2025 • Sarah • 68% ██████░░░░  │◀─┼──│ ████████░░░░ 75%  [⋮]     ││
│  │ 3 KRs │ On Track       [⋮]       │  │  └─────────────────────────────┘│
│  └───────────────────────────────────┘  │         [+ Add Key Result]      │
│         [+ Add OKR]                     ├─────────────────────────────────┤
│                                         │         KR DETAILS              │
│                                         │  Current: 53  Target: 60        │
│                                         │  LINKED MEASURABLES             │
│                                         │  📊 KPI: Customer NPS Score     │
│                                         │        [+ Link Measurable]      │
└─────────────────────────────────────────┴─────────────────────────────────┘
```

### CRUD + Duplication for All Entities

**Every entity (OKR, KR, KPI, Project, Task) must support:**
- Add (create new)
- Edit (modify existing)
- Delete (soft delete)
- **Duplicate** (copy with smart defaults)

**Duplication Rules:**
| Entity | Copied | Changed on Duplicate |
|--------|--------|---------------------|
| OKR | Title, Description, Owner, TimePeriod | Title → "Copy of [Title]", Dates → current quarter, KeyResults → empty |
| Key Result | Title, TargetValue, Unit, StartingValue, Weight | Title → "[Title] (Copy)", CurrentValue → StartingValue |
| KPI | Name, Description, TargetValue, Unit, Category | Name → "Copy of [Name]", Value → 0 |
| Project | Name, Description, Owner, TeamMembers | Name → "Copy of [Name]", Tasks → empty |
| Task | Description, Notes, Owner | Description → "[Desc] (Copy)", IsCompleted → false |

**Guiding Principles:**
- Ease of Use
- Accessibility  
- Simplicity

### Action Menus
Each card/item has a [⋮] menu with: Edit, Duplicate, Delete

---

## Open Questions

1. **Weight distribution for KRs**: Should we auto-distribute weights or let users specify?
   - Recommendation: Default to equal weights, allow override

2. **Progress calculation for TaskCollections**: Count-based or percentage-based?
   - Recommendation: Percentage (completed/total × 100)

3. **KPI update frequency**: Real-time or scheduled refresh?
   - Recommendation: On-demand with background refresh option

4. **Circular dependency prevention**: KPI → KR → KPI
   - Recommendation: Validate on link creation, prevent circular refs

---

## Appendix: Terminology Glossary

| Term | Definition |
|------|------------|
| **OKR** | Objective and Key Results - a goal-setting framework |
| **Objective** | The qualitative goal ("what" we want to achieve) |
| **Key Result** | A measurable outcome ("how" we measure success) |
| **KPI** | Key Performance Indicator - a standalone metric |
| **Composite KPI** | A KPI calculated from other KPIs |
| **Measurable** | Any entity that can provide progress to a Key Result |
| **IMeasurable** | Interface implemented by KPI, Project, TaskCollection |
| **IKpiSource** | Interface for entities that feed KPI values |
| **TaskCollection** | A grouped set of tasks treated as single measurable |

---

*This document should be updated as implementation progresses and decisions are refined.*

