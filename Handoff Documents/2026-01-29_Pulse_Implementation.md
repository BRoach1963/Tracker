# Pulse Feature Implementation Summary

**Date:** January 29, 2026  
**Status:** Phase 1-3 Complete (Infrastructure + ViewModel + View)

---

## What Was Built

### Option B: Hybrid Pulse Implementation

Per user guidance, implemented Pulse as:
1. **Quick Access Strip** at top - buttons to navigate to Goals/Metrics/Tasks browse pages
2. **Synthesis Feed** below - 4 sections of actionable signals

---

## Files Created/Modified

### New Files

1. **[Models/PulseSignal.cs](Tracker/ProCohere.Avalonia/Models/PulseSignal.cs)**
   - `PulseSourceType` enum (Goal, Metric, Meeting, Task)
   - `PulseTriggerReason` enum (ThresholdCrossed, TrendReversal, StatusChange, etc.)
   - `PulseSignalSeverity` enum (Info, Warning, Critical)
   - `PulseSection` enum (AttentionRequired, WhatChanged, RecentDiscussions, ActionsTaken)
   - `PulseSignal` model with all properties per spec data contract

2. **[Services/PulseSignalService.cs](Tracker/ProCohere.Avalonia/Services/PulseSignalService.cs)**
   - Singleton service for generating signals from existing data
   - `GenerateAllSignalsAsync()` - generates all 4 sections
   - `GenerateAttentionSignalsAsync()` - goals at risk, stale metrics, approaching deadlines
   - `GenerateChangeSignalsAsync()` - trend changes, completed tasks
   - `GenerateDiscussionSignalsAsync()` - linked agenda items from meetings
   - `GenerateActionSignalsAsync()` - completed tasks from goals/meetings
   - Role-aware time windows: IC=7d, Manager=14d, MoM=30d
   - Max 5 items in Attention Required

### Modified Files

3. **[ViewModels/PulseViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/PulseViewModel.cs)** - Complete rewrite
   - Quick access navigation events (NavigateToGoalsRequested, etc.)
   - 4 ObservableCollections for signal sections
   - `LoadPulseDataCommand` - loads signals from service
   - UI state properties (HasAttentionItems, ShowEmptyState, etc.)
   - SignalClicked event for navigation

4. **[Views/PulseView.axaml](Tracker/ProCohere.Avalonia/Views/PulseView.axaml)** - Complete rewrite
   - Quick Access Strip with 3 buttons (Goals, Metrics, Tasks)
   - 4 sections with signal cards
   - Loading and empty states
   - Styled signal cards with severity colors
   - Click-to-navigate on signals

5. **[Views/PulseView.axaml.cs](Tracker/ProCohere.Avalonia/Views/PulseView.axaml.cs)** - Complete rewrite
   - Subscribes to navigation events
   - Placeholder handlers for navigation (TODO: wire to main nav)
   - Auto-loads data on initialization

---

## Architecture

```
┌─────────────────────────────────────┐
│           PulseView                 │
│  ┌─────────────────────────────┐   │
│  │    Quick Access Strip       │   │
│  │  [Goals] [Metrics] [Tasks]  │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ ⚠️ Attention Required (5)   │   │
│  │   • Goal X is off track     │   │
│  │   • Metric Y needs update   │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 📊 What Changed             │   │
│  │   • Metric Z is improving   │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 💬 Recent Discussions       │   │
│  │   • Discussed in Meeting A  │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ ✅ Actions Taken            │   │
│  │   • ✓ Task completed        │   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│       PulseViewModel                │
│  • AttentionRequired collection     │
│  • WhatChanged collection           │
│  • RecentDiscussions collection     │
│  • ActionsTaken collection          │
│  • LoadPulseDataCommand             │
│  • Navigation events                │
└─────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│     PulseSignalService              │
│  • GenerateAllSignalsAsync()        │
│  • Time window: 7/14/30 days        │
│  • Max 5 attention items            │
└─────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│      Existing Services              │
│  • GoalsService                     │
│  • MetricsService                   │
│  • TaskService                      │
│  • DashboardService (meetings)      │
└─────────────────────────────────────┘
```

---

## Signal Generation Logic

### Attention Required (max 5, sorted by priority)
| Signal | Source | Trigger | Severity | Priority |
|--------|--------|---------|----------|----------|
| Goal off track | Goals | DerivedHealth = OffTrack | Critical | 100 |
| Goal at risk | Goals | DerivedHealth = AtRisk | Warning | 50 |
| Deadline < 2 days | Goals | DueDate approaching | Critical | 90 |
| Deadline < 7 days | Goals | DueDate approaching | Warning | 45 |
| Metric trending down | Metrics | Trend = TrendingDown | Warning | 60 |
| Stale metric | Metrics | UpdatedAt > 14 days | Warning | 40 |

### What Changed
- Metrics with trend changes (TrendingUp, TrendingDown, MoreVariable)
- Tasks completed that have a source (goal, meeting, agenda_item)

### Recent Discussions
- Agenda items from recent meetings that have linked entities

### Actions Taken
- Completed tasks that originated from goals or meetings

---

## TODOs / Next Steps

1. **Wire Quick Access Navigation**
   - Connect Goals button → Circle Goals tab
   - Connect Metrics button → Circle Metrics tab
   - Connect Tasks button → Tasks browse page

2. **Wire Signal Click Navigation**
   - Goal signals → Goal detail
   - Metric signals → Metric detail
   - Meeting signals → Meeting detail
   - Task signals → Task detail

3. **Role Detection**
   - Currently hardcoded to IC (7 days)
   - Need to detect manager status from team membership

4. **Signal Persistence (optional)**
   - Dismissed/snoozed state could be stored in local settings

---

## Key Design Decisions

1. **No Pulse Tables** - Per spec, all signals are derived from existing data
2. **Singleton Service** - Consistent with existing service patterns
3. **Quick Access Preserved** - Per Option B, users can still quickly get to Goals/Metrics/Tasks
4. **Single Column Feed** - Per spec, no tabs, no dashboards
5. **Card-based UI** - Per spec, clickable cards with icons and colors

---

## Spec Compliance

| Requirement | Status |
|-------------|--------|
| 4 sections (not tabs) | ✅ |
| Max 5 attention items | ✅ |
| Time-scoped (7/14/30d) | ✅ |
| Role-aware | ⚠️ Hardcoded to IC |
| No Pulse tables | ✅ |
| Derived from existing data | ✅ |
| Single column | ✅ |
| Card-based | ✅ |
| Not editable | ✅ |
| Signal-based, not raw data | ✅ |
