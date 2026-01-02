# Feature 01: Proactive AI Insights
## Technical Specification

**Feature ID:** F-001  
**Priority:** P0 (Highest)  
**Estimated Effort:** 3-4 sprints  
**Status:** Planning

---

## Executive Summary

Transform Oracle from a reactive Q&A assistant into a proactive management coach that surfaces actionable insights before the manager asks. The system analyzes data patterns and generates notifications/briefings highlighting items requiring attention.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want to see a daily briefing when I open the app so I know what needs my attention today | P0 |
| US-002 | As a manager, I want notifications when team members haven't had a 1:1 in X days so no one falls through the cracks | P0 |
| US-003 | As a manager, I want alerts when OKRs are trending toward failure so I can intervene early | P1 |
| US-004 | As a manager, I want reminders about upcoming birthdays/anniversaries so I can recognize my team | P1 |
| US-005 | As a manager, I want to see stale action items from past meetings so nothing gets forgotten | P0 |
| US-006 | As a manager, I want pulse survey alerts when someone rates poorly so I can follow up | P1 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PROACTIVE INSIGHTS SYSTEM                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────────┐   │
│  │  Insight Engine │────▶│  Insight Store  │────▶│  Notification       │   │
│  │  (Analyzers)    │     │  (SQLite)       │     │  Manager            │   │
│  └────────┬────────┘     └─────────────────┘     └──────────┬──────────┘   │
│           │                                                  │              │
│           ▼                                                  ▼              │
│  ┌─────────────────────────────────────┐     ┌─────────────────────────┐   │
│  │         ANALYZERS                    │     │      UI COMPONENTS      │   │
│  │  ┌───────────┐  ┌───────────────┐   │     │  ┌─────────────────┐    │   │
│  │  │ Meeting   │  │ OKR Trajectory│   │     │  │ Daily Briefing  │    │   │
│  │  │ Cadence   │  │ Analyzer      │   │     │  │ Panel           │    │   │
│  │  └───────────┘  └───────────────┘   │     │  └─────────────────┘    │   │
│  │  ┌───────────┐  ┌───────────────┐   │     │  ┌─────────────────┐    │   │
│  │  │ Birthday/ │  │ Action Item   │   │     │  │ Notification    │    │   │
│  │  │ Anniversary│  │ Staleness    │   │     │  │ Badge/Toast     │    │   │
│  │  └───────────┘  └───────────────┘   │     │  └─────────────────┘    │   │
│  │  ┌───────────┐  ┌───────────────┐   │     │  ┌─────────────────┐    │   │
│  │  │ Survey    │  │ KPI Gap       │   │     │  │ Insight Cards   │    │   │
│  │  │ Sentiment │  │ Analyzer      │   │     │  │ in HelpBot      │    │   │
│  │  └───────────┘  └───────────────┘   │     │  └─────────────────┘    │   │
│  └─────────────────────────────────────┘     └─────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. InsightEngine (`Services/AI/InsightEngine.cs`)

**Purpose:** Coordinates all analyzers and manages insight generation lifecycle.

```csharp
public class InsightEngine
{
    // Singleton for app-wide access
    public static InsightEngine Instance { get; }
    
    // Events
    public event EventHandler<InsightEventArgs> InsightGenerated;
    public event EventHandler<int> InsightsUpdated; // count of new insights
    
    // Core methods
    Task<List<Insight>> GenerateInsightsAsync();
    Task<DailyBriefing> GenerateDailyBriefingAsync();
    Task RunAnalyzersAsync(); // Called on startup and periodically
    
    // Configuration
    InsightSettings Settings { get; set; }
}
```

**Execution Strategy:**
- Run on app startup (after data indexing)
- Run every 4 hours if app remains open
- Run on-demand via refresh button
- Background thread with cancellation support

### 2. Insight Data Model

```csharp
public class Insight
{
    public int Id { get; set; }
    public InsightType Type { get; set; }
    public InsightSeverity Severity { get; set; } // Info, Warning, Critical
    public string Title { get; set; }
    public string Description { get; set; }
    public string ActionSuggestion { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? ActedOnAt { get; set; }
    public bool IsRead { get; set; }
    
    // Context for deep-linking
    public string EntityType { get; set; } // "TeamMember", "Meeting", "OKR"
    public int? EntityId { get; set; }
    
    // For deduplication
    public string UniqueKey { get; set; } // e.g., "meeting_gap_sarah_2025-12"
}

public enum InsightType
{
    MeetingGap,           // No 1:1 in X days
    OkrAtRisk,            // Trajectory predicts miss
    KpiOffTarget,         // KPI significantly below target
    UpcomingBirthday,     // Birthday in next 7 days
    UpcomingAnniversary,  // Work anniversary in next 7 days
    StaleActionItem,      // Action item > 14 days old
    SurveyAlert,          // Low rating in pulse survey
    TaskOverdue,          // Task past due date
    MeetingToday,         // 1:1 scheduled for today
    OkrEndingSoon         // OKR period ending in 7 days
}

public enum InsightSeverity
{
    Info,      // Blue - FYI
    Warning,   // Amber - Needs attention soon
    Critical   // Red - Needs immediate attention
}
```

### 3. Individual Analyzers

#### MeetingCadenceAnalyzer
```csharp
public class MeetingCadenceAnalyzer : IInsightAnalyzer
{
    // Configuration
    public int WarningThresholdDays { get; set; } = 14;
    public int CriticalThresholdDays { get; set; } = 21;
    
    public async Task<List<Insight>> AnalyzeAsync()
    {
        // For each active team member:
        // 1. Find last completed 1:1
        // 2. Calculate days since
        // 3. Generate insight if threshold exceeded
    }
}
```

**Logic:**
| Days Since Last 1:1 | Severity | Title |
|---------------------|----------|-------|
| 14-20 days | Warning | "Check in with {Name}" |
| 21+ days | Critical | "{Name} hasn't had a 1:1 in {X} days" |

#### OkrTrajectoryAnalyzer
```csharp
public class OkrTrajectoryAnalyzer : IInsightAnalyzer
{
    public async Task<List<Insight>> AnalyzeAsync()
    {
        // For each active OKR:
        // 1. Calculate current velocity (progress / days elapsed)
        // 2. Project end-of-period value
        // 3. Compare to target
        // 4. Generate insight if projected to miss
    }
}
```

**Trajectory Calculation:**
```
daysElapsed = (today - startDate).Days
daysTotal = (endDate - startDate).Days
currentProgress = OKR.CompletionPercentage
dailyVelocity = currentProgress / daysElapsed
projectedFinal = dailyVelocity * daysTotal

if (projectedFinal < 90) → Warning
if (projectedFinal < 70) → Critical
```

#### PersonalDateAnalyzer (Birthdays/Anniversaries)
```csharp
public class PersonalDateAnalyzer : IInsightAnalyzer
{
    public int LookAheadDays { get; set; } = 7;
    
    public async Task<List<Insight>> AnalyzeAsync()
    {
        // Find birthdays in next 7 days
        // Find work anniversaries in next 7 days
        // Generate Info-level insights
    }
}
```

#### ActionItemStalenessAnalyzer
```csharp
public class ActionItemStalenessAnalyzer : IInsightAnalyzer
{
    public int StaleThresholdDays { get; set; } = 14;
    
    public async Task<List<Insight>> AnalyzeAsync()
    {
        // Find all action items from meetings
        // Filter to incomplete items older than threshold
        // Generate Warning-level insights
    }
}
```

#### SurveySentimentAnalyzer
```csharp
public class SurveySentimentAnalyzer : IInsightAnalyzer
{
    public int LowRatingThreshold { get; set; } = 3; // Out of 5
    
    public async Task<List<Insight>> AnalyzeAsync()
    {
        // For recent survey responses (last 7 days):
        // Find any rating questions with score <= threshold
        // Generate Warning-level insight
        // Respect anonymity - don't reveal respondent if anonymous
    }
}
```

### 4. InsightStore (`Services/AI/InsightStore.cs`)

**Purpose:** Persist insights to SQLite for history and deduplication.

**Schema:**
```sql
CREATE TABLE insights (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    unique_key TEXT NOT NULL UNIQUE,
    type TEXT NOT NULL,
    severity TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    action_suggestion TEXT,
    entity_type TEXT,
    entity_id INTEGER,
    generated_at TEXT NOT NULL,
    dismissed_at TEXT,
    acted_on_at TEXT,
    is_read INTEGER DEFAULT 0,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_insights_type ON insights(type);
CREATE INDEX idx_insights_severity ON insights(severity);
CREATE INDEX idx_insights_generated ON insights(generated_at);
CREATE INDEX idx_insights_dismissed ON insights(dismissed_at);
```

**Storage Location:** `%LocalAppData%\Tracker\insights.db`

### 5. Daily Briefing Model

```csharp
public class DailyBriefing
{
    public DateTime GeneratedAt { get; set; }
    public string Greeting { get; set; } // "Good morning, Brian!"
    
    // Today's Schedule
    public List<OneOnOne> MeetingsToday { get; set; }
    
    // Attention Required
    public List<Insight> CriticalInsights { get; set; }
    public List<Insight> WarningInsights { get; set; }
    
    // Upcoming
    public List<TeamMember> UpcomingBirthdays { get; set; }
    public List<TeamMember> UpcomingAnniversaries { get; set; }
    
    // Summary Stats
    public int ActiveOkrCount { get; set; }
    public int OkrsOnTrack { get; set; }
    public int OkrsAtRisk { get; set; }
    public int OverdueTaskCount { get; set; }
    
    // AI-Generated Summary (optional, costs API)
    public string AiSummary { get; set; }
}
```

### 6. UI Components

#### DailyBriefingControl.xaml
- Shown on Dashboard or as modal on startup
- Collapsible sections
- Quick action buttons (e.g., "Schedule 1:1" → opens dialog)

#### NotificationBadge
- Shows count of unread insights
- Appears on HelpBot FAB button
- Click opens insight panel

#### InsightCard.xaml
- Reusable card for displaying single insight
- Color-coded by severity
- Dismiss button
- "Take Action" button (deep-links to relevant entity)

---

## Data Flow

### Startup Flow
```
App Startup
    │
    ├──▶ DataIndexer.IndexAllAsync()
    │
    └──▶ InsightEngine.RunAnalyzersAsync()
              │
              ├── MeetingCadenceAnalyzer.AnalyzeAsync()
              ├── OkrTrajectoryAnalyzer.AnalyzeAsync()
              ├── PersonalDateAnalyzer.AnalyzeAsync()
              ├── ActionItemStalenessAnalyzer.AnalyzeAsync()
              └── SurveySentimentAnalyzer.AnalyzeAsync()
                        │
                        ▼
              InsightStore.SaveInsightsAsync()
                        │
                        ▼
              NotificationManager.UpdateBadge()
                        │
                        ▼
              DailyBriefingViewModel.RefreshAsync()
```

### Insight Generation Pipeline
```
Analyzer.AnalyzeAsync()
        │
        ▼
    Generate Insight with UniqueKey
        │
        ▼
    Check InsightStore for existing UniqueKey
        │
        ├── EXISTS & not dismissed → Skip (dedup)
        │
        ├── EXISTS & dismissed → Check if should resurface
        │                        (e.g., meeting gap widened)
        │
        └── NOT EXISTS → Save new insight
                              │
                              ▼
                    Fire InsightGenerated event
```

---

## Configuration

### User Settings
```json
{
    "Insights": {
        "IsEnabled": true,
        "ShowDailyBriefingOnStartup": true,
        "MeetingGapWarningDays": 14,
        "MeetingGapCriticalDays": 21,
        "ActionItemStaleDays": 14,
        "BirthdayLookAheadDays": 7,
        "AnniversaryLookAheadDays": 7,
        "LowSurveyRatingThreshold": 3,
        "EnableAiSummary": false,
        "AnalysisIntervalHours": 4
    }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create Insight data model | 2h | None |
| Create InsightStore with SQLite | 4h | Insight model |
| Create InsightEngine skeleton | 4h | InsightStore |
| Create IInsightAnalyzer interface | 1h | None |
| Add settings schema | 2h | None |

### Phase 2: Analyzers (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| MeetingCadenceAnalyzer | 4h | InsightEngine |
| PersonalDateAnalyzer | 3h | InsightEngine |
| ActionItemStalenessAnalyzer | 4h | InsightEngine |
| OkrTrajectoryAnalyzer | 6h | InsightEngine |
| SurveySentimentAnalyzer | 4h | InsightEngine |
| KpiGapAnalyzer | 4h | InsightEngine |

### Phase 3: UI Components (Sprint 3)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| InsightCard control | 4h | Insight model |
| NotificationBadge control | 3h | InsightStore |
| InsightPanelControl (list view) | 6h | InsightCard |
| Integrate badge into MainWindow | 2h | NotificationBadge |

### Phase 4: Daily Briefing (Sprint 4)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| DailyBriefing model | 2h | None |
| DailyBriefingViewModel | 4h | InsightEngine |
| DailyBriefingControl UI | 8h | ViewModel |
| Startup dialog integration | 3h | DailyBriefingControl |
| Optional AI summary | 4h | GeminiChatService |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Analysis slows startup | High | Run analyzers in background after UI loads |
| Duplicate insights spam user | Medium | UniqueKey deduplication with cooldown period |
| API costs for AI summaries | Medium | Make AI summary opt-in, default off |
| Stale insights after data changes | Medium | Re-run affected analyzers when data modified |

### UX Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Alert fatigue | High | Conservative thresholds, easy dismiss, severity levels |
| Insights feel intrusive | Medium | Startup briefing opt-in, badge is passive |
| False positives annoy users | Medium | Allow threshold customization in settings |

### Data Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Survey anonymity violated | Critical | Never reveal respondent in anonymous surveys |
| Incomplete data causes wrong insights | Medium | Only generate insights with sufficient data |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Insights viewed rate | >60% | InsightStore.is_read tracking |
| Insights acted on rate | >30% | InsightStore.acted_on_at tracking |
| Meeting cadence improvement | 15% fewer gaps | Compare before/after |
| User satisfaction | >4/5 | In-app feedback |

---

## Dependencies

- Existing: TrackerDbManager, DataModels, AI subsystem
- New database: insights.db
- UI: MainWindow modification for badge
- Settings: New InsightSettings section

---

## Future Enhancements

1. **Machine Learning Patterns** - Learn from dismissed insights to reduce noise
2. **Custom Insight Rules** - User-defined triggers
3. **Insight Sharing** - Export/email briefings
4. **Trend Analysis** - "Meeting frequency down 20% this month"
5. **Predictive Scheduling** - "You usually meet Sarah on Thursdays"

---

**Document End**
