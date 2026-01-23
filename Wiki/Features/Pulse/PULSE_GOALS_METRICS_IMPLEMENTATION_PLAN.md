# Pulse Implementation Plan: Goals & Metrics

## Executive Summary

The Pulse section of ProCohere Avalonia will implement a **paradigm-shifting** approach to Goals and Metrics that is explicitly **narrative-first, discussion-driven, and manager-friendly**. This is fundamentally different from traditional OKR systems.

### Core Philosophy
> "Goals express intent, Metrics observe reality, Humans decide."

This plan transforms Pulse from a simple Tasks view into a comprehensive Goals & Metrics system with sub-tabs for Goals, Metrics, and Tasks.

---

## Table of Contents

1. [Architectural Overview](#architectural-overview)
2. [Current State Analysis](#current-state-analysis)
3. [UI Structure](#ui-structure)
4. [Goals Implementation](#goals-implementation)
5. [Metrics Implementation](#metrics-implementation)
6. [AI Integration](#ai-integration)
7. [Database Schema Alignment](#database-schema-alignment)
8. [Implementation Phases](#implementation-phases)
9. [Critical Constraints](#critical-constraints)
10. [File Structure](#file-structure)

---

## Architectural Overview

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PULSE VIEW                                      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                      │
│  │   Goals     │    │   Metrics   │    │   Tasks     │     (Sub-tabs)       │
│  │   Tab       │    │   Tab       │    │   Tab       │                      │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘                      │
└─────────┼──────────────────┼──────────────────┼─────────────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            VIEWMODELS                                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │  GoalsViewModel │  │ MetricsViewModel│  │ TasksViewModel  │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
│           │                    │                    │                        │
│           └────────────────────┼────────────────────┘                        │
│                                │                                             │
│                     ┌──────────┴──────────┐                                 │
│                     │   PulseViewModel    │  (Coordinator)                  │
│                     └──────────┬──────────┘                                 │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                             SERVICES                                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │  GoalsService   │  │ MetricsService  │  │  TaskService    │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        SUPABASE (PostgreSQL)                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐             │
│  │  goals   │  │ metrics  │  │ targets  │  │ metric_history   │             │
│  │(26 cols) │  │(29 cols) │  │(15 cols) │  │    (7 cols)      │             │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────────┘             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Current State Analysis

### What Exists Now

1. **Pulse Navigation** - Points to `TasksView` (working)
2. **TasksView/TasksViewModel** - Complete task management (working)
3. **GoalDetail Model** - Basic model in `Models/GoalDetail.cs` (incomplete)
4. **CircleView Goals Tab** - Has basic goals display within Circle (partial)

### What Needs to Change

1. **Pulse becomes tabbed container** with Goals, Metrics, Tasks sub-tabs
2. **Create dedicated PulseView** that hosts the three sub-tabs
3. **Move existing TasksView** to be a sub-view within Pulse
4. **Create GoalsView** following the new narrative-first philosophy
5. **Create MetricsView** following the signals-not-targets philosophy
6. **Create supporting models, services, and flyouts**

---

## UI Structure

### PulseView Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ PULSE                                                    [Search] [+ Goal]  │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌──────────┐ ┌──────────┐ ┌──────────┐                                      │
│ │  Goals   │ │ Metrics  │ │  Tasks   │        (Sub-tab toggle buttons)      │
│ └──────────┘ └──────────┘ └──────────┘                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────┐  ┌──────────────────┐ │
│  │                                                 │  │                  │ │
│  │              Content Area                       │  │  Detail Flyout   │ │
│  │         (Goals / Metrics / Tasks)               │  │    (400px)       │ │
│  │                                                 │  │                  │ │
│  └─────────────────────────────────────────────────┘  └──────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Goals Tab Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER: Scope Filters + Stats                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                             │
│ │  My Goals   │ │ Team Goals  │ │Shared Goals │    (Scope Toggle)           │
│ └─────────────┘ └─────────────┘ └─────────────┘                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ GOAL CARD                                                               │ │
│ │ ┌────────────────────────────────────────────────────────────────────┐  │ │
│ │ │ [On track]                                           Owner: Sarah  │  │ │
│ │ │ Increase Customer Satisfaction Score                               │  │ │
│ │ │ Improve NPS from 42 to 55 through targeted initiatives...          │  │ │
│ │ │                                                                    │  │ │
│ │ │ ↗ NPS Score  ↗ Support Response Time  → Customer Churn             │  │ │
│ │ │                                                                    │  │ │
│ │ │ Last discussed: Jan 15, 2026 in Weekly Sync                        │  │ │
│ │ └────────────────────────────────────────────────────────────────────┘  │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ [More goal cards...]                                                    │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Goal Card Design (CRITICAL - No percentages/progress bars!)

```
┌────────────────────────────────────────────────────────────────────────────┐
│ [Health Badge]                                            Owner: Name      │
├────────────────────────────────────────────────────────────────────────────┤
│ Goal Title (Bold, prominent)                                               │
│ Goal description preview...                                                │
├────────────────────────────────────────────────────────────────────────────┤
│ Associated Metrics: ↗ Metric1  → Metric2  ↘ Metric3    (directional only) │
├────────────────────────────────────────────────────────────────────────────┤
│ Last discussed: [Date] in [Meeting Name]                                   │
└────────────────────────────────────────────────────────────────────────────┘
```

### Goal Detail Flyout

```
┌──────────────────────────────────────────┐
│ [X]                        Goal Title    │
├──────────────────────────────────────────┤
│ [Overview] [Activity]      (Tabs)        │
├──────────────────────────────────────────┤
│                                          │
│ HEALTH                                   │
│ ┌──────────────────────────────────────┐ │
│ │ [On track ▼]                         │ │
│ │ "Team aligned and making progress"   │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ LIFECYCLE                                │
│ ┌──────────────────────────────────────┐ │
│ │ [Active ▼]                           │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ GOAL TYPE                                │
│ ┌──────────────────────────────────────┐ │
│ │ [Execution ▼]                        │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ ASSOCIATED METRICS                       │
│ ┌──────────────────────────────────────┐ │
│ │ ↗ NPS Score           [Remove]       │ │
│ │ → Support Response    [Remove]       │ │
│ │ [+ Associate Metric]                 │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ LINKED TASKS                             │
│ ┌──────────────────────────────────────┐ │
│ │ □ Review feedback forms              │ │
│ │ ☑ Train support team                 │ │
│ │ [+ Add Task]                         │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ AI SUMMARY (Collapsible)                 │
│ ┌──────────────────────────────────────┐ │
│ │ "This goal has been discussed in 3   │ │
│ │ meetings this month. Recent focus    │ │
│ │ centered on support improvements."   │ │
│ └──────────────────────────────────────┘ │
│                                          │
├──────────────────────────────────────────┤
│ [Delete]                    [Edit]       │
└──────────────────────────────────────────┘
```

### Metrics Tab Layout (Library Model)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ METRIC LIBRARY                                              [+ New Metric]  │
├─────────────────────────────────────────────────────────────────────────────┤
│ Lifecycle: [All ▼]    Scope: [All ▼]    Source: [All ▼]                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ METRIC ROW                                                              │ │
│ │ ┌────────────────────────────────────────────────────────────────────┐  │ │
│ │ │ NPS Score                        ↗    [Individual]  [System]       │  │ │
│ │ │ Steward: Sarah Johnson           Active                            │  │ │
│ │ │ Linked to: 2 goals                                                 │  │ │
│ │ └────────────────────────────────────────────────────────────────────┘  │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Support Response Time              →    [Team]        [Manual]         │ │
│ │ Steward: Support Lead              Active                              │ │
│ │ Linked to: 1 goal                                                      │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Metric Detail Flyout

```
┌──────────────────────────────────────────┐
│ [X]                      Metric Name     │
├──────────────────────────────────────────┤
│ [Details] [History]        (Tabs)        │
├──────────────────────────────────────────┤
│                                          │
│ DEFINITION                               │
│ ┌──────────────────────────────────────┐ │
│ │ Measures customer satisfaction via   │ │
│ │ Net Promoter Score survey.           │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ TREND                                    │
│ ┌──────────────────────────────────────┐ │
│ │     ╱──────                          │ │
│ │    ╱        (Simple sparkline)       │ │
│ │ ──╱                                  │ │
│ │ Trending upward                      │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ ATTRIBUTES                               │
│ ┌──────────────────────────────────────┐ │
│ │ Source: System                       │ │
│ │ Scope: Individual                    │ │
│ │ Steward: Sarah Johnson               │ │
│ │ Lifecycle: Active                    │ │
│ │ Frequency: Monthly                   │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ ASSOCIATED GOALS                         │
│ ┌──────────────────────────────────────┐ │
│ │ → Increase Customer Satisfaction     │ │
│ │ → Improve Support Quality            │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ LIFECYCLE                                │
│ ┌──────────────────────────────────────┐ │
│ │ [Active ▼]                           │ │
│ │ (Dormant / Retired)                  │ │
│ └──────────────────────────────────────┘ │
│                                          │
│ AI CONTEXT (Collapsible)                 │
│ ┌──────────────────────────────────────┐ │
│ │ "This metric has remained stable     │ │
│ │ over the past month..."              │ │
│ └──────────────────────────────────────┘ │
│                                          │
├──────────────────────────────────────────┤
│ [Update Value]              [Edit]       │
└──────────────────────────────────────────┘
```

---

## Goals Implementation

### Goal Model (`Models/Goal.cs`)

```csharp
public class Goal
{
    // Identity
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public Guid CreatedByUserId { get; set; }
    
    // Content
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // NEW: Goal Type (from spec)
    public GoalType GoalType { get; set; } = GoalType.Execution;
    
    // Time Period (existing)
    public GoalTimePeriod TimePeriod { get; set; }
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // NEW: Health System (replaces status for display)
    public GoalHealth Health { get; set; } = GoalHealth.OnTrack;
    public string? HealthReason { get; set; }  // "What changed?"
    
    // NEW: Lifecycle (from spec)
    public GoalLifecycle Lifecycle { get; set; } = GoalLifecycle.Active;
    public string? LifecycleReason { get; set; }  // "What changed?"
    public Guid? SupersededById { get; set; }  // Link to replacement goal
    
    // Legacy status (keep for DB compatibility)
    public GoalStatus Status { get; set; }
    public GoalStatus? StatusOverride { get; set; }
    
    // Progress (HIDDEN BY DEFAULT - not shown in UI)
    public decimal ProgressPercent { get; set; }
    public decimal? ProgressOverride { get; set; }
    
    // Visibility
    public bool IsTeamVisible { get; set; } = true;
    public bool IsOrgVisible { get; set; }
    public GoalVisibility Visibility => IsOrgVisible ? GoalVisibility.Org 
        : IsTeamVisible ? GoalVisibility.Team 
        : GoalVisibility.Private;
    
    // Relationships
    public Guid? ProjectId { get; set; }
    public DateTime? LastDiscussedAt { get; set; }  // NEW
    public string? LastDiscussedInMeeting { get; set; }  // NEW
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation (not DB)
    public string? OwnerName { get; set; }
    public List<Metric> AssociatedMetrics { get; set; } = new();
    public List<Target> Targets { get; set; } = new();
    public List<TaskDetail> LinkedTasks { get; set; } = new();
}
```

### Goal Enums

```csharp
// NEW: Goal Types (from spec)
public enum GoalType
{
    Growth,       // Personal development and capability building
    Execution,    // Concrete outcomes and delivery focus
    Operational,  // Stability and sustainability (health)
    Directional   // Learning and assessment (exploratory)
}

// NEW: Health System (replaces evaluative status)
public enum GoalHealth
{
    OnTrack,           // Goal is progressing well
    NeedsAttention,    // Some concerns worth discussing
    AtRisk,            // Significant challenges
    ReframingNeeded    // Goal intent may need reconsideration
}

// NEW: Lifecycle States (from spec)
public enum GoalLifecycle
{
    Active,     // Goal matters right now
    Evolving,   // Meaning or scope is changing
    Paused,     // Matters but not currently
    Superseded, // Replaced by new goals (terminal)
    Retired     // No longer matters (terminal)
}

public enum GoalVisibility
{
    Private,  // Manager + IC only
    Team,     // Team-visible
    Org       // Org-visible
}
```

### IGoalsService Interface

```csharp
public interface IGoalsService
{
    // Queries
    Task<List<Goal>> GetMyGoalsAsync(CancellationToken ct = default);
    Task<List<Goal>> GetTeamGoalsAsync(CancellationToken ct = default);
    Task<List<Goal>> GetSharedGoalsAsync(CancellationToken ct = default);
    Task<Goal?> GetGoalByIdAsync(Guid goalId, CancellationToken ct = default);
    Task<List<Goal>> SearchGoalsAsync(string query, CancellationToken ct = default);
    
    // CRUD
    Task<Goal> CreateGoalAsync(Goal goal, CancellationToken ct = default);
    Task<Goal> UpdateGoalAsync(Goal goal, CancellationToken ct = default);
    Task<bool> DeleteGoalAsync(Guid goalId, CancellationToken ct = default);
    
    // Health & Lifecycle (require reflection note)
    Task<Goal> UpdateHealthAsync(Guid goalId, GoalHealth health, string? reason, CancellationToken ct = default);
    Task<Goal> UpdateLifecycleAsync(Guid goalId, GoalLifecycle lifecycle, string? reason, Guid? supersededById = null, CancellationToken ct = default);
    
    // Metric Association
    Task<Goal> AssociateMetricAsync(Guid goalId, Guid metricId, CancellationToken ct = default);
    Task<Goal> RemoveMetricAssociationAsync(Guid goalId, Guid metricId, CancellationToken ct = default);
    Task<List<Metric>> GetAssociatedMetricsAsync(Guid goalId, CancellationToken ct = default);
}
```

### GoalsViewModel Key Properties

```csharp
public partial class GoalsViewModel : ViewModelBase
{
    // Collections
    [ObservableProperty] private ObservableCollection<Goal> _myGoals = new();
    [ObservableProperty] private ObservableCollection<Goal> _teamGoals = new();
    [ObservableProperty] private ObservableCollection<Goal> _sharedGoals = new();
    
    // View State
    [ObservableProperty] private GoalScope _selectedScope = GoalScope.MyGoals;
    [ObservableProperty] private Goal? _selectedGoal;
    [ObservableProperty] private bool _isGoalDetailOpen;
    [ObservableProperty] private GoalDetailTab _goalDetailTab = GoalDetailTab.Overview;
    
    // Editing
    [ObservableProperty] private bool _isGoalEditorOpen;
    [ObservableProperty] private Goal? _editingGoal;
    
    // Health Change Dialog
    [ObservableProperty] private bool _isHealthChangeDialogOpen;
    [ObservableProperty] private GoalHealth _newHealth;
    [ObservableProperty] private string _healthChangeReason = string.Empty;
    
    // Lifecycle Change Dialog  
    [ObservableProperty] private bool _isLifecycleChangeDialogOpen;
    [ObservableProperty] private GoalLifecycle _newLifecycle;
    [ObservableProperty] private string _lifecycleChangeReason = string.Empty;
    
    // Commands
    [RelayCommand] private void SelectGoal(Goal goal);
    [RelayCommand] private void CloseGoalDetail();
    [RelayCommand] private void CreateNewGoal();
    [RelayCommand] private void EditGoal(Goal goal);
    [RelayCommand] private Task SaveGoalAsync();
    [RelayCommand] private Task DeleteGoalAsync(Goal goal);
    [RelayCommand] private void OpenHealthChangeDialog();
    [RelayCommand] private Task ConfirmHealthChangeAsync();
    [RelayCommand] private void OpenLifecycleChangeDialog();
    [RelayCommand] private Task ConfirmLifecycleChangeAsync();
    [RelayCommand] private Task AssociateMetricAsync(Metric metric);
    [RelayCommand] private Task RemoveMetricAsync(Guid metricId);
}
```

---

## Metrics Implementation

### Metric Model (`Models/Metric.cs`)

```csharp
public class Metric
{
    // Identity
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }  // Steward
    public Guid CreatedByUserId { get; set; }
    
    // Definition
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }  // What this metric measures
    public string? Category { get; set; }
    
    // Values (HIDDEN BY DEFAULT in UI)
    public decimal CurrentValue { get; set; }
    public decimal? TargetValue { get; set; }  // Optional, not emphasized
    public decimal? BaselineValue { get; set; }
    public string? Unit { get; set; }
    
    // Direction (for trend indicators)
    public MetricTargetDirection TargetDirection { get; set; } = MetricTargetDirection.HigherIsBetter;
    
    // Source & Scope (from spec)
    public MetricSource Source { get; set; } = MetricSource.System;
    public MetricScope Scope { get; set; } = MetricScope.Individual;
    
    // Lifecycle (from spec)
    public MetricLifecycle Lifecycle { get; set; } = MetricLifecycle.Active;
    
    // Frequency
    public MetricFrequency Frequency { get; set; } = MetricFrequency.Monthly;
    public DateTime? LastUpdatedAt { get; set; }
    
    // Sensitivity (from spec)
    public bool IsSensitive { get; set; }  // Requires additional safeguards
    
    // Visibility
    public bool IsTeamVisible { get; set; } = true;
    public bool IsOrgVisible { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation (not DB)
    public string? StewardName { get; set; }
    public int LinkedGoalsCount { get; set; }
    public MetricTrend Trend { get; set; }  // Computed from history
}
```

### Metric Enums

```csharp
// Source of metric data (from spec)
public enum MetricSource
{
    System,   // Automated from systems
    Survey,   // From surveys/forms
    Manual    // Human-curated (requires extra safeguards)
}

// Scope of what metric measures (from spec)
public enum MetricScope
{
    Individual,   // Personal metric
    Team,         // Team-level metric
    Organization  // Org-wide metric
}

// Metric lifecycle (from spec) - simpler than goals
public enum MetricLifecycle
{
    Active,   // Meaningful and relevant right now
    Dormant,  // Exists but not being monitored
    Retired   // No longer meaningful (terminal)
}

// Trend indicator (directional only - no numeric values!)
public enum MetricTrend
{
    TrendingUp,     // ↗
    Stable,         // →
    TrendingDown,   // ↘
    MoreVariable,   // ~
    Unknown         // ?
}
```

### IMetricsService Interface

```csharp
public interface IMetricsService
{
    // Library Queries
    Task<List<Metric>> GetAllMetricsAsync(CancellationToken ct = default);
    Task<Metric?> GetMetricByIdAsync(Guid metricId, CancellationToken ct = default);
    Task<List<Metric>> GetMetricsByLifecycleAsync(MetricLifecycle lifecycle, CancellationToken ct = default);
    Task<List<Metric>> GetMetricsByScopeAsync(MetricScope scope, CancellationToken ct = default);
    Task<List<Metric>> GetMetricsBySourceAsync(MetricSource source, CancellationToken ct = default);
    Task<List<Metric>> SearchMetricsAsync(string query, CancellationToken ct = default);
    
    // For Goal Association
    Task<List<Metric>> GetMetricsForGoalAsync(Guid goalId, CancellationToken ct = default);
    Task<List<Metric>> GetAvailableMetricsForAssociationAsync(Guid goalId, CancellationToken ct = default);
    
    // CRUD
    Task<Metric> CreateMetricAsync(Metric metric, CancellationToken ct = default);
    Task<Metric> UpdateMetricAsync(Metric metric, CancellationToken ct = default);
    Task<bool> DeleteMetricAsync(Guid metricId, CancellationToken ct = default);
    
    // Value Update (manual metrics)
    Task<Metric> UpdateValueAsync(Guid metricId, decimal newValue, string? whatChanged, CancellationToken ct = default);
    
    // Lifecycle
    Task<Metric> UpdateLifecycleAsync(Guid metricId, MetricLifecycle lifecycle, CancellationToken ct = default);
    
    // History
    Task<List<MetricHistoryEntry>> GetHistoryAsync(Guid metricId, int limit = 12, CancellationToken ct = default);
    Task<MetricTrend> CalculateTrendAsync(Guid metricId, CancellationToken ct = default);
}
```

### MetricsViewModel Key Properties

```csharp
public partial class MetricsViewModel : ViewModelBase
{
    // Collections
    [ObservableProperty] private ObservableCollection<Metric> _metrics = new();
    
    // Filters
    [ObservableProperty] private MetricLifecycle? _lifecycleFilter;
    [ObservableProperty] private MetricScope? _scopeFilter;
    [ObservableProperty] private MetricSource? _sourceFilter;
    [ObservableProperty] private string _searchQuery = string.Empty;
    
    // View State
    [ObservableProperty] private Metric? _selectedMetric;
    [ObservableProperty] private bool _isMetricDetailOpen;
    [ObservableProperty] private MetricDetailTab _metricDetailTab = MetricDetailTab.Details;
    
    // Editing
    [ObservableProperty] private bool _isMetricEditorOpen;
    [ObservableProperty] private Metric? _editingMetric;
    
    // Value Update Dialog (for manual metrics)
    [ObservableProperty] private bool _isValueUpdateDialogOpen;
    [ObservableProperty] private decimal _newValue;
    [ObservableProperty] private string _whatChangedNote = string.Empty;
    
    // Commands
    [RelayCommand] private void SelectMetric(Metric metric);
    [RelayCommand] private void CloseMetricDetail();
    [RelayCommand] private void CreateNewMetric();
    [RelayCommand] private void EditMetric(Metric metric);
    [RelayCommand] private Task SaveMetricAsync();
    [RelayCommand] private Task DeleteMetricAsync(Metric metric);
    [RelayCommand] private void OpenValueUpdateDialog();
    [RelayCommand] private Task ConfirmValueUpdateAsync();
    [RelayCommand] private Task UpdateLifecycleAsync(MetricLifecycle lifecycle);
}
```

---

## AI Integration

### AI Rules for Goals (CRITICAL)

**AI IS allowed to:**
- Summarize goal history and narrative context
- Highlight when goals have not been discussed recently
- Surface discrepancies between discussion focus and metric signals
- Prepare contextual summaries for meetings
- Suggest (not apply) that a goal may be worth revisiting

**AI is EXPLICITLY NOT allowed to:**
- ❌ Grade or score goals
- ❌ Predict success or failure
- ❌ Compare individuals or teams
- ❌ Interpret metrics as pass/fail indicators
- ❌ Recommend disciplinary or evaluative actions
- ❌ Change goal health, lifecycle, or visibility
- ❌ Automatically create or modify goals

**Allowed AI Language:**
- "has been discussed"
- "has focused on"
- "appears to"
- "may be helpful to revisit"
- "recent conversations centered on"

**Disallowed AI Language:**
- ❌ "should" / "needs to"
- ❌ "is behind" / "is failing"
- ❌ "underperforming"
- ❌ "requires intervention"

### AI Rules for Metrics (CRITICAL)

**AI must NOT surface numeric metric values by default.**

This is intentional and foundational:
- Numeric values introduce implied judgment
- AI authority + numbers creates accidental performance assessment
- Numeric summaries easily copied/shared without context

**AI must use ONLY:**
- Directional descriptors (trending upward, stable, more variable)
- Qualitative descriptors

**Allowed AI Language:**
- "has changed" / "has remained stable"
- "has become more variable"
- "has been discussed" / "may provide context"

**Disallowed AI Language:**
- ❌ "good / bad"
- ❌ "improved / worsened"
- ❌ "underperforming"
- ❌ "exceeding expectations"
- ❌ "concerning"
- ❌ "should / needs to"

### Where AI Appears

1. **Collapsible "AI Summary"** section in Goal Detail flyout
2. **Collapsible "AI Context"** section in Metric Detail flyout
3. Optional summaries during agenda preparation (future)

**AI summaries are NEVER shown inline with health indicators or numeric values.**

---

## Database Schema Alignment

### Required ALTER Scripts

#### 1. Add Goal Lifecycle & Health Columns

```sql
-- Add goal_type enum
CREATE TYPE goal_type AS ENUM ('growth', 'execution', 'operational', 'directional');

-- Add goal_health enum
CREATE TYPE goal_health AS ENUM ('on_track', 'needs_attention', 'at_risk', 'reframing_needed');

-- Add goal_lifecycle enum
CREATE TYPE goal_lifecycle AS ENUM ('active', 'evolving', 'paused', 'superseded', 'retired');

-- Add columns to goals table
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS goal_type goal_type DEFAULT 'execution';
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS health goal_health DEFAULT 'on_track';
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS health_reason TEXT;
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS lifecycle goal_lifecycle DEFAULT 'active';
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS lifecycle_reason TEXT;
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS superseded_by_id UUID REFERENCES procohere.goals(id) ON DELETE SET NULL;
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS last_discussed_at TIMESTAMPTZ;
ALTER TABLE procohere.goals ADD COLUMN IF NOT EXISTS last_discussed_meeting_id UUID REFERENCES procohere.meetings(id) ON DELETE SET NULL;

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_goals_lifecycle ON procohere.goals(lifecycle) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_goals_health ON procohere.goals(health) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_goals_goal_type ON procohere.goals(goal_type) WHERE is_deleted = false;
```

#### 2. Add Metric Lifecycle & Source Columns

```sql
-- Add metric_source enum
CREATE TYPE metric_source AS ENUM ('system', 'survey', 'manual');

-- Add metric_scope enum  
CREATE TYPE metric_scope AS ENUM ('individual', 'team', 'organization');

-- Add metric_lifecycle enum
CREATE TYPE metric_lifecycle AS ENUM ('active', 'dormant', 'retired');

-- Add columns to metrics table
ALTER TABLE procohere.metrics ADD COLUMN IF NOT EXISTS source metric_source DEFAULT 'system';
ALTER TABLE procohere.metrics ADD COLUMN IF NOT EXISTS scope metric_scope DEFAULT 'individual';
ALTER TABLE procohere.metrics ADD COLUMN IF NOT EXISTS lifecycle metric_lifecycle DEFAULT 'active';
ALTER TABLE procohere.metrics ADD COLUMN IF NOT EXISTS is_sensitive BOOLEAN DEFAULT false;
ALTER TABLE procohere.metrics ADD COLUMN IF NOT EXISTS steward_team_member_id UUID REFERENCES procohere.team_members(id) ON DELETE SET NULL;

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_metrics_lifecycle ON procohere.metrics(lifecycle) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_metrics_source ON procohere.metrics(source) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_metrics_scope ON procohere.metrics(scope) WHERE is_deleted = false;
```

#### 3. Create Goal-Metric Association Table

```sql
-- Goal-Metric many-to-many association
CREATE TABLE IF NOT EXISTS procohere.goal_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES procohere.goals(id) ON DELETE CASCADE,
    metric_id UUID NOT NULL REFERENCES procohere.metrics(id) ON DELETE CASCADE,
    associated_at TIMESTAMPTZ DEFAULT NOW(),
    associated_by UUID NOT NULL REFERENCES procohere.team_members(id) ON DELETE SET NULL,
    UNIQUE(goal_id, metric_id)
);

CREATE INDEX idx_goal_metrics_goal_id ON procohere.goal_metrics(goal_id);
CREATE INDEX idx_goal_metrics_metric_id ON procohere.goal_metrics(metric_id);
```

---

## Implementation Phases

### Phase 11A: Pulse View Restructure (2-3 hours)

1. **Create PulseView.axaml** - Container with sub-tabs
2. **Create PulseViewModel.cs** - Coordinate sub-tabs
3. **Create PulseSubTab enum** - Goals, Metrics, Tasks
4. **Move TasksView** - Becomes embedded in Pulse
5. **Update MainWindow** - Point Pulse navigation to new PulseView

### Phase 11B: Goals Models & Enums (1-2 hours)

1. **Update Goal.cs** - Add new properties
2. **Create GoalType.cs** - Growth, Execution, Operational, Directional
3. **Create GoalHealth.cs** - OnTrack, NeedsAttention, AtRisk, ReframingNeeded
4. **Create GoalLifecycle.cs** - Active, Evolving, Paused, Superseded, Retired
5. **Create GoalVisibility.cs** - Private, Team, Org
6. **Create GoalScope.cs** - MyGoals, TeamGoals, SharedGoals

### Phase 11C: Goals Service (2-3 hours)

1. **Create IGoalsService.cs** - Interface
2. **Create GoalsService.cs** - Supabase implementation
3. **Implement queries** - My/Team/Shared goals
4. **Implement CRUD** - Create, Update, Delete
5. **Implement Health/Lifecycle updates** - With reflection prompts
6. **Register in DI**

### Phase 11D: Goals View & ViewModel (4-5 hours)

1. **Create GoalsView.axaml** - Goal list with scope toggle
2. **Create GoalsViewModel.cs** - Full implementation
3. **Create GoalCard.axaml** - Goal list item (NO progress bars!)
4. **Create GoalDetailFlyout.axaml** - Detail view with tabs
5. **Create GoalEditorFlyout.axaml** - Create/edit goal
6. **Create HealthChangeDialog** - Health update with reflection
7. **Create LifecycleChangeDialog** - Lifecycle update with reflection

### Phase 11E: Metrics Models & Enums (1 hour)

1. **Update Metric.cs** - Add new properties
2. **Create MetricSource.cs** - System, Survey, Manual
3. **Create MetricScope.cs** - Individual, Team, Organization
4. **Create MetricLifecycle.cs** - Active, Dormant, Retired
5. **Create MetricTrend.cs** - TrendingUp, Stable, TrendingDown

### Phase 11F: Metrics Service (2-3 hours)

1. **Create IMetricsService.cs** - Interface
2. **Create MetricsService.cs** - Supabase implementation
3. **Implement library queries** - With filters
4. **Implement CRUD** - Create, Update, Delete
5. **Implement value update** - With "What changed?" prompt
6. **Implement trend calculation**
7. **Register in DI**

### Phase 11G: Metrics View & ViewModel (3-4 hours)

1. **Create MetricsView.axaml** - Metric library layout
2. **Create MetricsViewModel.cs** - Full implementation
3. **Create MetricRow.axaml** - Metric list item
4. **Create MetricDetailFlyout.axaml** - Detail view with tabs
5. **Create MetricEditorFlyout.axaml** - Create/edit metric
6. **Create ValueUpdateDialog** - Manual metric update with note

### Phase 11H: Goal-Metric Association (2 hours)

1. **Create goal_metrics association service**
2. **Add metric picker to Goal detail**
3. **Show linked goals in Metric detail**
4. **Wire up association/removal commands**

### Phase 11I: Integration & Polish (2-3 hours)

1. **Wire up cross-navigation** - Click goal from metric detail, etc.
2. **Add loading states**
3. **Add error handling**
4. **Add empty states**
5. **Test full workflow**

### Phase 11J: Database Migration (1 hour)

1. **Create ALTER scripts** for goals table
2. **Create ALTER scripts** for metrics table
3. **Create goal_metrics table**
4. **Run migrations on Supabase**

**Total Estimated Time: 20-25 hours**

---

## Critical Constraints

### NEVER Do (System-Wide)

1. ❌ **Automatically create goals** - Always explicit user action
2. ❌ **Automatically change goal health or lifecycle** - Always requires user
3. ❌ **Use numeric grades or percentages in goal display**
4. ❌ **Create dashboards or leaderboards**
5. ❌ **Rank individuals or teams**
6. ❌ **Use red/yellow/green indicators on goals**
7. ❌ **Show progress bars for goals**
8. ❌ **Interpret metrics as pass/fail**
9. ❌ **Let AI make evaluative statements**
10. ❌ **Surface numeric values by default** (especially via AI)

### ALWAYS Do

1. ✓ **Require explicit user action** for lifecycle changes
2. ✓ **Prompt "What changed?"** for health/lifecycle/value updates
3. ✓ **Log all changes** to history
4. ✓ **Preserve narrative context**
5. ✓ **Use directional indicators** (↗ → ↘) for metrics
6. ✓ **Hide metrics by default** in goal views (collapsible)
7. ✓ **Label AI output** as "descriptive"
8. ✓ **Respect role-based visibility**
9. ✓ **Treat metrics as signals**, not targets
10. ✓ **Keep IC views psychologically safe**

### IC vs Manager Distinctions

**ICs CAN:**
- View goals they own/participate in
- Propose edits to goal wording
- Add personal notes and reflections
- Request discussion in meetings
- Suggest lifecycle changes

**ICs CANNOT:**
- Unilaterally change goal lifecycle
- Change goal health for team goals
- Retire or supersede goals without manager

**Managers CAN:**
- Create goals for individuals or teams
- Change goal lifecycle state
- Set and update goal health
- Associate or remove metrics
- Control goal visibility

---

## File Structure

### New Files to Create

```
ProCohere.Avalonia/
├── Models/
│   ├── Goal.cs (update)
│   ├── GoalType.cs (new)
│   ├── GoalHealth.cs (new)
│   ├── GoalLifecycle.cs (new)
│   ├── GoalVisibility.cs (new)
│   ├── Metric.cs (update)
│   ├── MetricSource.cs (new)
│   ├── MetricScope.cs (new)
│   ├── MetricLifecycle.cs (new)
│   ├── MetricTrend.cs (new)
│   └── MetricHistoryEntry.cs (new)
├── Services/
│   ├── IGoalsService.cs (new)
│   ├── GoalsService.cs (new)
│   ├── IMetricsService.cs (new)
│   ├── MetricsService.cs (new)
│   └── IGoalMetricAssociationService.cs (new)
├── ViewModels/
│   ├── PulseViewModel.cs (new)
│   ├── GoalsViewModel.cs (new)
│   └── MetricsViewModel.cs (new)
├── Views/
│   ├── PulseView.axaml (new)
│   ├── GoalsView.axaml (new)
│   ├── MetricsView.axaml (new)
│   └── Controls/
│       ├── GoalCard.axaml (new)
│       ├── GoalDetailFlyout.axaml (new)
│       ├── GoalEditorFlyout.axaml (new)
│       ├── MetricRow.axaml (new)
│       ├── MetricDetailFlyout.axaml (new)
│       ├── MetricEditorFlyout.axaml (new)
│       └── ReflectionDialog.axaml (new)
└── Converters/
    ├── GoalHealthConverters.cs (new)
    ├── GoalLifecycleConverters.cs (new)
    └── MetricTrendConverters.cs (new)
```

### SQL Scripts to Create

```
New Docs/SupaBase SQL Scripts/
├── ALTER_goals_add_lifecycle_health.sql (new)
├── ALTER_metrics_add_lifecycle_source.sql (new)
└── CREATE_goal_metrics_association.sql (new)
```

---

## Summary

This implementation transforms Pulse from a simple Tasks view into a comprehensive, **narrative-first** Goals & Metrics system that:

1. **Rejects traditional OKR patterns** - No percentages, progress bars, or pass/fail
2. **Emphasizes conversation over metrics** - Goals are discussable objects
3. **Uses Health instead of Status** - Qualitative assessment with reflection
4. **Implements proper Lifecycle** - Describes relevance, not success
5. **Treats Metrics as Signals** - Directional indicators, not targets
6. **Constrains AI appropriately** - Narrates, never evaluates
7. **Respects role boundaries** - IC vs Manager capabilities

The result will be a system where:
> "Goals feel like conversations you are responsible for, not numbers you are accountable to."

---

*Document created: January 19, 2026*
*Based on: Cohere Goals & Metrics Specification v1 (8 documents)*
