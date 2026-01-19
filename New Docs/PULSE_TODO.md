# Pulse Implementation TODO

**Last Updated:** January 19, 2026  
**Status:** In Progress - Tasks Tab Implemented

---

## Current State (Working)

### ✅ Completed
1. **PulseView.axaml** - Main container with header and tab bar
2. **PulseView.axaml.cs** - Code-behind creates `PulseViewModel` as DataContext
3. **PulseViewModel.cs** - Tab switching logic (`IsSubTabGoals`, `IsSubTabMetrics`, `IsSubTabTasks`)
4. **Tab styling** - Matches Circle's pattern (transparent background, green underline when selected)
5. **Shell views created** in `Views/Pulse/`:
   - `GoalsTabView.axaml` / `.cs` - Placeholder
   - `MetricsTabView.axaml` / `.cs` - Placeholder
   - `TasksTabView.axaml` / `.cs` - **FULLY IMPLEMENTED**
6. **Child ViewModel binding** - PulseView now passes child ViewModels to tab views

### Key Lesson Learned
- **Views need DataContext set in code-behind** (like CircleView does)
- **FluentTheme overrides** require targeting `/template/ ContentPresenter#PART_ContentPresenter`
- **x:DataType on parent** when binding IsVisible to parent VM while DataContext goes to child VM

---

## Phase 1: Tasks Tab ✅ COMPLETE

### 1.1 Wire Up TasksTabView ✅
- [x] DataContext bound to `TasksViewModel` via PulseView
- [x] Filter bar with All/Today/Overdue/Completed toggles
- [x] ItemsControl with task list
- [x] Detail flyout panel with slide animation
- [x] Add task dialog integration

### 1.2 Task List Item Template ✅
- [x] Checkbox for completion (with toggle command)
- [x] Task title (with strikethrough when complete)
- [x] Due date with overdue highlighting (red)
- [x] Assignee display
- [x] Priority badge with color coding

---

## Phase 2: Goals Tab

### 2.1 GoalsViewModel Setup
- [ ] Verify `GoalsViewModel.cs` exists and has:
  - `ObservableCollection<GoalDetail> Goals`
  - `LoadGoalsCommand`
  - Filter properties (Active/Archived/All)
- [ ] Set DataContext in `GoalsTabView.axaml.cs`

### 2.2 Goals List UI
- [ ] Filter bar (Active/Archived/All)
- [ ] ItemsControl with goal cards
- [ ] Goal card template:
  - Title
  - Narrative (truncated)
  - Health indicator (colored dot)
  - Target count
  - Due date

### 2.3 Goal Detail Flyout
- [ ] Create `GoalDetailFlyout.axaml`
- [ ] Fields: Title, Narrative, Type, Lifecycle, Health, Targets list
- [ ] Wire up to open on goal click

### 2.4 New Goal Flyout
- [ ] Create `NewGoalFlyout.axaml`
- [ ] Minimal fields to start: Title, Narrative
- [ ] Save command

---

## Phase 3: Metrics Tab

### 3.1 MetricsViewModel Setup
- [ ] Verify `MetricsViewModel.cs` exists and has:
  - `ObservableCollection<MetricDetail> Metrics`
  - `LoadMetricsCommand`
  - Filter properties
- [ ] Set DataContext in `MetricsTabView.axaml.cs`

### 3.2 Metrics List UI
- [ ] Filter bar
- [ ] ItemsControl with metric cards
- [ ] Metric card template:
  - Name
  - Current value + unit
  - Trend indicator (up/down/flat)
  - Last updated

### 3.3 Metric Detail Flyout
- [ ] Create `MetricDetailFlyout.axaml`
- [ ] Fields: Name, Description, Unit, Current Value, History chart
- [ ] Record new value functionality

---

## Phase 4: Integration & Polish

### 4.1 Primary Action Button
- [ ] Add "+ New Goal/Metric/Task" button to header
- [ ] Wire to open appropriate flyout based on selected tab
- [ ] Use `PrimaryActionText` property already in ViewModel

### 4.2 Empty States
- [ ] Design empty state for each tab
- [ ] "No goals yet" with call-to-action
- [ ] "No metrics yet" with call-to-action
- [ ] "No tasks yet" with call-to-action

### 4.3 Loading States
- [ ] Show loading indicator while fetching data
- [ ] Wire up `IsLoading` property

---

## Files Reference

### Existing Files to Use
| File | Purpose |
|------|---------|
| `ViewModels/PulseViewModel.cs` | Tab coordination, already has child VM references |
| `ViewModels/GoalsViewModel.cs` | Goals data (verify exists) |
| `ViewModels/MetricsViewModel.cs` | Metrics data (verify exists) |
| `ViewModels/TasksViewModel.cs` | Tasks data (working) |
| `Models/GoalDetail.cs` | Goal model |
| `Models/MetricDetail.cs` | Metric model (verify exists) |

### Files to Create
| File | Purpose |
|------|---------|
| `Views/Dialogs/GoalDetailFlyout.axaml` | Goal detail/edit |
| `Views/Dialogs/MetricDetailFlyout.axaml` | Metric detail/edit |

---

## Important Patterns

### Setting DataContext (CRITICAL)
```csharp
// In code-behind constructor, AFTER InitializeComponent():
public GoalsTabView()
{
    InitializeComponent();
    DataContext = new GoalsViewModel(); // Or get from parent
}
```

### Tab Button Style Override (FluentTheme)
```xml
<Style Selector="Button.tab-button /template/ ContentPresenter#PART_ContentPresenter">
    <Setter Property="Background" Value="Transparent"/>
</Style>
```

---

## Database Tables (Supabase)

| Table | Key Columns |
|-------|-------------|
| `goals` | id, title, narrative, type, lifecycle, health, user_id |
| `targets` | id, goal_id, title, target_value, current_value |
| `metrics` | id, name, description, unit, current_value, user_id |
| `metric_history` | id, metric_id, value, recorded_at |
| `tasks` | id, title, description, due_date, is_completed, user_id |

---

## Reference Documents
- [PULSE_GOALS_METRICS_IMPLEMENTATION_PLAN.md](PULSE_GOALS_METRICS_IMPLEMENTATION_PLAN.md) - Full design spec
- [DB_SCHEMA_CURRENT.md](DB_SCHEMA_CURRENT.md) - Database schema
