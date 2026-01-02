# Feature 03: Predictive Analytics on OKRs/KPIs
## Technical Specification

**Feature ID:** F-003  
**Priority:** P1  
**Estimated Effort:** 2-3 sprints  
**Status:** Planning

---

## Executive Summary

Move beyond static progress reporting to predictive analytics that forecast whether OKRs and KPIs will meet their targets. The system calculates trajectories based on historical velocity, displays confidence intervals, and recommends corrective actions when goals are at risk.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want to see projected end-of-period values so I know if we'll hit targets | P0 |
| US-002 | As a manager, I want trajectory visualizations so I can understand trends at a glance | P0 |
| US-003 | As a manager, I want confidence intervals so I know how reliable predictions are | P1 |
| US-004 | As a manager, I want "what-if" scenarios so I can plan interventions | P2 |
| US-005 | As a manager, I want AI recommendations when goals are at risk so I know what to do | P1 |
| US-006 | As a manager, I want historical accuracy tracking so I trust the predictions | P2 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PREDICTIVE ANALYTICS SYSTEM                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                    TrajectoryAnalysisService                          │   │
│  │                                                                        │   │
│  │   ┌─────────────────┐    ┌─────────────────┐    ┌────────────────┐   │   │
│  │   │ Velocity        │    │ Projection      │    │ Confidence     │   │   │
│  │   │ Calculator      │───▶│ Engine          │───▶│ Estimator      │   │   │
│  │   └─────────────────┘    └─────────────────┘    └────────────────┘   │   │
│  │                                                                        │   │
│  │   Inputs:                   Outputs:                                   │   │
│  │   - Current progress        - Projected final value                   │   │
│  │   - Historical snapshots    - Days to target (or never)               │   │
│  │   - Time elapsed/remaining  - Required velocity to hit target         │   │
│  │   - External factors        - Confidence interval (low/high)          │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                    Historical Snapshot System                         │   │
│  │                                                                        │   │
│  │   ┌─────────────────┐    ┌─────────────────────────────────────┐     │   │
│  │   │ Snapshot        │    │ progress_snapshots table             │     │   │
│  │   │ Service         │───▶│ (entity_type, entity_id, date, value)│     │   │
│  │   └─────────────────┘    └─────────────────────────────────────┘     │   │
│  │                                                                        │   │
│  │   Captures daily/weekly snapshots for trend analysis                  │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                    Visualization Components                           │   │
│  │                                                                        │   │
│  │   ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │   │
│  │   │ Trajectory      │  │ Burndown/       │  │ Confidence          │  │   │
│  │   │ Chart           │  │ Burnup Chart    │  │ Cone Chart          │  │   │
│  │   └─────────────────┘  └─────────────────┘  └─────────────────────┘  │   │
│  │                                                                        │   │
│  │   ┌─────────────────┐  ┌─────────────────┐                           │   │
│  │   │ Velocity        │  │ What-If         │                           │   │
│  │   │ Sparkline       │  │ Simulator       │                           │   │
│  │   └─────────────────┘  └─────────────────┘                           │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. Trajectory Data Models

```csharp
public class TrajectoryAnalysis
{
    // Identity
    public string EntityType { get; set; }    // "OKR", "KPI", "KeyResult"
    public int EntityId { get; set; }
    public string EntityName { get; set; }
    
    // Current State
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentProgress { get; set; }   // Percentage
    
    // Time Context
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public decimal TimeProgress { get; set; }      // Percentage of time elapsed
    
    // Velocity Metrics
    public decimal CurrentVelocity { get; set; }   // Progress per day
    public decimal RequiredVelocity { get; set; }  // To hit target on time
    public decimal VelocityGap { get; set; }       // Required - Current
    public VelocityTrend VelocityTrend { get; set; } // Accelerating, Stable, Decelerating
    
    // Projections
    public decimal ProjectedFinalValue { get; set; }
    public decimal ProjectedFinalProgress { get; set; }
    public DateTime? ProjectedCompletionDate { get; set; }  // null if never
    public int? DaysToTarget { get; set; }
    
    // Confidence
    public decimal ConfidenceLow { get; set; }     // Pessimistic projection
    public decimal ConfidenceHigh { get; set; }    // Optimistic projection
    public ConfidenceLevel Confidence { get; set; }
    
    // Status
    public TrajectoryStatus Status { get; set; }
    public string StatusDescription { get; set; }
    
    // Historical Data
    public List<ProgressSnapshot> History { get; set; }
    
    // Recommendations
    public List<string> Recommendations { get; set; }
}

public enum VelocityTrend
{
    Accelerating,    // Getting faster
    Stable,          // Consistent pace
    Decelerating,    // Slowing down
    Stalled          // No progress recently
}

public enum TrajectoryStatus
{
    OnTrack,         // Will hit target
    SlightlyBehind,  // Needs 10-20% improvement
    AtRisk,          // Needs 20-50% improvement
    Critical,        // Needs >50% improvement
    WillMiss         // Cannot mathematically hit target
}

public enum ConfidenceLevel
{
    High,            // Consistent historical data
    Medium,          // Some variance in history
    Low,             // High variance or limited data
    Insufficient     // Not enough data points
}

public class ProgressSnapshot
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public decimal Progress { get; set; }
}
```

### 2. Historical Snapshot System

**Purpose:** Capture periodic snapshots of progress for trend analysis.

**Database Schema:**
```sql
CREATE TABLE progress_snapshots (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL,           -- 'OKR', 'KPI', 'KeyResult', 'Project'
    entity_id INTEGER NOT NULL,
    snapshot_date TEXT NOT NULL,
    current_value REAL NOT NULL,
    target_value REAL NOT NULL,
    progress REAL NOT NULL,              -- Percentage
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(entity_type, entity_id, snapshot_date)
);

CREATE INDEX idx_snapshots_entity ON progress_snapshots(entity_type, entity_id);
CREATE INDEX idx_snapshots_date ON progress_snapshots(snapshot_date);
```

**Storage Location:** Main Tracker database (not vectors.db)

**Snapshot Service:**
```csharp
public class ProgressSnapshotService
{
    /// <summary>
    /// Capture snapshots for all trackable entities.
    /// Called daily on app startup (if >24h since last snapshot).
    /// </summary>
    public async Task CaptureSnapshotsAsync()
    {
        var today = DateTime.Today;
        
        // OKRs
        var okrs = await _db.GetOKRs();
        foreach (var okr in okrs.Where(o => o.IsActive && !o.IsDeleted))
        {
            await SaveSnapshotAsync("OKR", okr.ObjectiveId, today, 
                (decimal)okr.CompletionPercentage, 100m, 
                (decimal)okr.CompletionPercentage);
            
            // Also snapshot each Key Result
            foreach (var kr in okr.KeyResults)
            {
                await SaveSnapshotAsync("KeyResult", kr.Id, today,
                    kr.CurrentValue, kr.TargetValue, kr.Progress);
            }
        }
        
        // KPIs
        var kpis = await _db.GetKPIs();
        foreach (var kpi in kpis.Where(k => !k.IsDeleted))
        {
            await SaveSnapshotAsync("KPI", kpi.KpiId, today,
                (decimal)kpi.Value, (decimal)kpi.TargetValue, 
                (decimal)kpi.PercentComplete);
        }
        
        // Projects
        var projects = await _db.GetProjects();
        foreach (var project in projects.Where(p => !p.IsDeleted))
        {
            await SaveSnapshotAsync("Project", project.ID, today,
                (decimal)project.Progress, 100m, (decimal)project.Progress);
        }
    }
    
    public async Task<List<ProgressSnapshot>> GetHistoryAsync(
        string entityType, int entityId, int days = 90)
    {
        // Return snapshots for analysis
    }
}
```

### 3. TrajectoryAnalysisService

```csharp
public class TrajectoryAnalysisService
{
    public async Task<TrajectoryAnalysis> AnalyzeOkrAsync(ObjectiveKeyResult okr)
    {
        var analysis = new TrajectoryAnalysis
        {
            EntityType = "OKR",
            EntityId = okr.ObjectiveId,
            EntityName = okr.Title,
            CurrentValue = (decimal)okr.CompletionPercentage,
            TargetValue = 100m,
            CurrentProgress = (decimal)okr.CompletionPercentage,
            StartDate = okr.StartDate,
            EndDate = okr.EndDate
        };
        
        CalculateTimeMetrics(analysis);
        
        // Get historical data
        analysis.History = await _snapshotService.GetHistoryAsync(
            "OKR", okr.ObjectiveId);
        
        CalculateVelocity(analysis);
        CalculateProjections(analysis);
        CalculateConfidence(analysis);
        DetermineStatus(analysis);
        GenerateRecommendations(analysis);
        
        return analysis;
    }
    
    private void CalculateTimeMetrics(TrajectoryAnalysis analysis)
    {
        var today = DateTime.Today;
        var totalDays = (analysis.EndDate - analysis.StartDate).Days;
        
        analysis.DaysElapsed = Math.Max(1, (today - analysis.StartDate).Days);
        analysis.DaysRemaining = Math.Max(0, (analysis.EndDate - today).Days);
        analysis.TimeProgress = totalDays > 0 
            ? (decimal)analysis.DaysElapsed / totalDays * 100m 
            : 100m;
    }
    
    private void CalculateVelocity(TrajectoryAnalysis analysis)
    {
        // Current velocity = progress / days elapsed
        analysis.CurrentVelocity = analysis.DaysElapsed > 0
            ? analysis.CurrentProgress / analysis.DaysElapsed
            : 0;
        
        // Required velocity = remaining progress / days remaining
        var remainingProgress = analysis.TargetValue - analysis.CurrentProgress;
        analysis.RequiredVelocity = analysis.DaysRemaining > 0
            ? remainingProgress / analysis.DaysRemaining
            : decimal.MaxValue;  // Can't be achieved
        
        analysis.VelocityGap = analysis.RequiredVelocity - analysis.CurrentVelocity;
        
        // Determine trend from history
        analysis.VelocityTrend = DetermineVelocityTrend(analysis.History);
    }
    
    private VelocityTrend DetermineVelocityTrend(List<ProgressSnapshot> history)
    {
        if (history.Count < 3)
            return VelocityTrend.Stable;
        
        // Compare recent velocity vs earlier velocity
        var recent = history.TakeLast(7).ToList();
        var earlier = history.SkipLast(7).TakeLast(7).ToList();
        
        if (recent.Count < 2 || earlier.Count < 2)
            return VelocityTrend.Stable;
        
        var recentVelocity = (recent.Last().Progress - recent.First().Progress) 
            / Math.Max(1, recent.Count - 1);
        var earlierVelocity = (earlier.Last().Progress - earlier.First().Progress) 
            / Math.Max(1, earlier.Count - 1);
        
        var change = recentVelocity - earlierVelocity;
        
        if (recentVelocity < 0.1m)
            return VelocityTrend.Stalled;
        if (change > 0.5m)
            return VelocityTrend.Accelerating;
        if (change < -0.5m)
            return VelocityTrend.Decelerating;
        
        return VelocityTrend.Stable;
    }
    
    private void CalculateProjections(TrajectoryAnalysis analysis)
    {
        // Linear projection
        var totalDays = analysis.DaysElapsed + analysis.DaysRemaining;
        analysis.ProjectedFinalProgress = analysis.CurrentVelocity * totalDays;
        analysis.ProjectedFinalValue = analysis.ProjectedFinalProgress;
        
        // Days to target
        if (analysis.CurrentVelocity > 0)
        {
            var remainingProgress = analysis.TargetValue - analysis.CurrentProgress;
            var daysNeeded = (int)Math.Ceiling(remainingProgress / analysis.CurrentVelocity);
            
            analysis.DaysToTarget = daysNeeded;
            analysis.ProjectedCompletionDate = DateTime.Today.AddDays(daysNeeded);
        }
        else
        {
            analysis.DaysToTarget = null;
            analysis.ProjectedCompletionDate = null;
        }
    }
    
    private void CalculateConfidence(TrajectoryAnalysis analysis)
    {
        if (analysis.History.Count < 5)
        {
            analysis.Confidence = ConfidenceLevel.Insufficient;
            analysis.ConfidenceLow = analysis.ProjectedFinalProgress * 0.7m;
            analysis.ConfidenceHigh = analysis.ProjectedFinalProgress * 1.3m;
            return;
        }
        
        // Calculate standard deviation of daily progress
        var dailyChanges = new List<decimal>();
        for (int i = 1; i < analysis.History.Count; i++)
        {
            dailyChanges.Add(analysis.History[i].Progress - analysis.History[i-1].Progress);
        }
        
        var mean = dailyChanges.Average();
        var variance = dailyChanges.Sum(x => (x - mean) * (x - mean)) / dailyChanges.Count;
        var stdDev = (decimal)Math.Sqrt((double)variance);
        
        // Confidence bounds (roughly 90% confidence interval)
        var margin = stdDev * 1.645m * analysis.DaysRemaining;
        analysis.ConfidenceLow = Math.Max(0, analysis.ProjectedFinalProgress - margin);
        analysis.ConfidenceHigh = analysis.ProjectedFinalProgress + margin;
        
        // Determine confidence level based on coefficient of variation
        var cv = mean != 0 ? stdDev / Math.Abs(mean) : 1m;
        analysis.Confidence = cv switch
        {
            < 0.3m => ConfidenceLevel.High,
            < 0.6m => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };
    }
    
    private void DetermineStatus(TrajectoryAnalysis analysis)
    {
        var gapPercent = analysis.CurrentVelocity > 0 
            ? (analysis.RequiredVelocity - analysis.CurrentVelocity) / analysis.CurrentVelocity * 100m
            : 100m;
        
        // Check if mathematically impossible
        if (analysis.DaysRemaining == 0 && analysis.CurrentProgress < analysis.TargetValue)
        {
            analysis.Status = TrajectoryStatus.WillMiss;
            analysis.StatusDescription = "Time has expired without reaching target";
            return;
        }
        
        if (analysis.ProjectedFinalProgress >= analysis.TargetValue)
        {
            analysis.Status = TrajectoryStatus.OnTrack;
            analysis.StatusDescription = $"Projected to reach {analysis.ProjectedFinalProgress:F0}%";
        }
        else if (gapPercent <= 20)
        {
            analysis.Status = TrajectoryStatus.SlightlyBehind;
            analysis.StatusDescription = $"Need {gapPercent:F0}% velocity increase to hit target";
        }
        else if (gapPercent <= 50)
        {
            analysis.Status = TrajectoryStatus.AtRisk;
            analysis.StatusDescription = $"Need {gapPercent:F0}% velocity increase - intervention needed";
        }
        else if (analysis.DaysRemaining > 0)
        {
            analysis.Status = TrajectoryStatus.Critical;
            analysis.StatusDescription = $"Need {gapPercent:F0}% velocity increase - unlikely to recover";
        }
        else
        {
            analysis.Status = TrajectoryStatus.WillMiss;
            analysis.StatusDescription = "Cannot reach target at current pace";
        }
    }
    
    private void GenerateRecommendations(TrajectoryAnalysis analysis)
    {
        analysis.Recommendations = new List<string>();
        
        if (analysis.Status == TrajectoryStatus.OnTrack)
        {
            analysis.Recommendations.Add("Maintain current pace");
            return;
        }
        
        // Velocity-based recommendations
        if (analysis.VelocityTrend == VelocityTrend.Decelerating)
        {
            analysis.Recommendations.Add("Velocity is decreasing - identify blockers");
        }
        
        if (analysis.VelocityTrend == VelocityTrend.Stalled)
        {
            analysis.Recommendations.Add("Progress has stalled - schedule check-in immediately");
        }
        
        // Gap-based recommendations
        if (analysis.VelocityGap > 0)
        {
            var dailyNeeded = analysis.RequiredVelocity - analysis.CurrentVelocity;
            analysis.Recommendations.Add(
                $"Need {dailyNeeded:F2}% additional progress per day");
        }
        
        // Time-based recommendations
        if (analysis.DaysRemaining <= 7 && analysis.Status != TrajectoryStatus.OnTrack)
        {
            analysis.Recommendations.Add("Less than 1 week remaining - consider scope adjustment");
        }
        
        // Confidence-based recommendations
        if (analysis.Confidence == ConfidenceLevel.Low)
        {
            analysis.Recommendations.Add("High variance in progress - ensure consistent updates");
        }
    }
}
```

### 4. Visualization Components

#### TrajectoryChart (WPF/LiveCharts2)
```csharp
public class TrajectoryChartViewModel : ViewModelBase
{
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    
    public void LoadData(TrajectoryAnalysis analysis)
    {
        var actualData = analysis.History
            .Select(h => new DateTimePoint(h.Date, (double)h.Progress))
            .ToArray();
        
        // Projection line (dashed)
        var projectionStart = analysis.History.LastOrDefault();
        var projectionEnd = new DateTimePoint(
            analysis.EndDate, 
            (double)analysis.ProjectedFinalProgress
        );
        
        // Target line (horizontal)
        var targetLine = new LineSeries<DateTimePoint>
        {
            Values = new[] 
            {
                new DateTimePoint(analysis.StartDate, (double)analysis.TargetValue),
                new DateTimePoint(analysis.EndDate, (double)analysis.TargetValue)
            },
            Stroke = new SolidColorPaint(SKColors.Green, 2),
            GeometrySize = 0,
            LineSmoothness = 0,
            Name = "Target"
        };
        
        // Confidence cone (shaded area)
        // ... implementation with area series
        
        Series = new ISeries[] 
        {
            new LineSeries<DateTimePoint> 
            {
                Values = actualData,
                Name = "Actual Progress"
            },
            // projection line
            // confidence cone
            targetLine
        };
    }
}
```

#### VelocitySparkline
- Mini chart showing velocity trend over last 14 days
- Color-coded: green (stable/accelerating), amber (slowing), red (stalled)

#### ProgressGauge
- Circular gauge showing current vs projected
- Inner ring: current progress
- Outer ring: projected final (with color coding)

---

## Data Flow

### Snapshot Capture Flow
```
App Startup
    │
    ├──▶ Check last snapshot date
    │         │
    │         ├── < 24 hours ago → Skip
    │         │
    │         └── >= 24 hours ago
    │                   │
    │                   ▼
    │         ProgressSnapshotService.CaptureSnapshotsAsync()
    │                   │
    │                   ├── For each active OKR → Save snapshot
    │                   ├── For each Key Result → Save snapshot
    │                   ├── For each KPI → Save snapshot
    │                   └── For each Project → Save snapshot
    │
    └──▶ Continue app startup
```

### Analysis Flow
```
User views OKR/KPI detail
         │
         ▼
TrajectoryAnalysisService.AnalyzeAsync(entity)
         │
         ├── Load historical snapshots
         │
         ├── Calculate time metrics
         │
         ├── Calculate velocity (current & required)
         │
         ├── Determine velocity trend
         │
         ├── Calculate projections
         │
         ├── Calculate confidence intervals
         │
         ├── Determine status
         │
         └── Generate recommendations
                  │
                  ▼
         Return TrajectoryAnalysis
                  │
                  ▼
         Display in UI with charts
```

---

## Configuration

### User Settings
```json
{
    "PredictiveAnalytics": {
        "IsEnabled": true,
        "SnapshotFrequency": "Daily",
        "HistoryRetentionDays": 365,
        "MinDataPointsForPrediction": 5,
        "ShowConfidenceIntervals": true,
        "EnableWhatIfScenarios": false
    }
}
```

---

## Implementation Plan

### Phase 1: Snapshot Infrastructure (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create progress_snapshots table | 2h | None |
| Create ProgressSnapshot model | 1h | None |
| Create ProgressSnapshotService | 4h | Model, table |
| Integrate snapshot capture into startup | 2h | Service |
| Data migration for existing entities | 3h | Service |

### Phase 2: Analysis Engine (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create TrajectoryAnalysis model | 2h | None |
| Implement velocity calculations | 4h | Model |
| Implement projection calculations | 4h | Velocity |
| Implement confidence calculations | 4h | Projections |
| Implement status determination | 2h | All above |
| Implement recommendations engine | 3h | Status |

### Phase 3: Visualizations (Sprint 3)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Install LiveCharts2 NuGet | 1h | None |
| Create TrajectoryChart control | 6h | Analysis model |
| Create VelocitySparkline control | 3h | Analysis model |
| Create ProgressGauge control | 4h | Analysis model |
| Integrate into OKR detail view | 3h | Charts |
| Integrate into KPI detail view | 3h | Charts |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Not enough historical data | High | Seed with synthetic data, show confidence level |
| Snapshot table grows large | Medium | Retention policy, archival strategy |
| Predictions inaccurate | Medium | Show confidence intervals, track accuracy |
| LiveCharts2 performance | Low | Limit data points, lazy loading |

### Mathematical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Linear projection too simplistic | Medium | Consider weighted recent data, polynomial fit |
| Outliers skew predictions | Medium | Use median velocity, outlier detection |
| Zero velocity breaks math | Low | Guard clauses, sensible defaults |

### UX Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Predictions feel judgmental | Medium | Frame as "helper" not "critic" |
| Information overload | Medium | Progressive disclosure, summaries first |
| Distrust of predictions | Low | Show confidence level, historical accuracy |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Prediction accuracy | >70% within confidence interval | Track actual vs predicted |
| Feature usage | >40% of OKR views include trajectory | Analytics |
| Early intervention rate | 20% more at-risk OKRs addressed | Compare before/after |

---

## Dependencies

- Existing: TrackerDbManager, OKR/KPI models
- New: progress_snapshots table, LiveCharts2 NuGet
- Database migration for snapshot table

---

## Future Enhancements

1. **What-If Simulator** - "What if we increase velocity by 20%?"
2. **Team-Level Predictions** - Aggregate trajectories across team
3. **Machine Learning** - Use ML to improve prediction accuracy
4. **Anomaly Detection** - Alert on unusual progress patterns
5. **Comparative Analysis** - Compare similar OKRs from past quarters
6. **Risk Scoring** - Composite risk score across all goals

---

**Document End**
