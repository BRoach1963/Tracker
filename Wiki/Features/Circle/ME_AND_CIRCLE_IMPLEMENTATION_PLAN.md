# ME and Circle Implementation Plan

**Document Created:** January 20, 2026  
**Status:** Ready for Implementation  
**Based On:** ProCohere_Me_Screen_Spec_v1.docx, ProCohere_Circle_Screen_Spec_v1.docx

---

## Executive Summary

This document provides the implementation plan for two key screens:

1. **ME Screen** - New personal hub (currently does not exist)
2. **Circle Screen** - Enhancement of existing manager view (UI Steps 5-9 pending)

---

## Part A: ME Screen Implementation

### Overview

The ME screen is the **personal operating hub** for every user. It answers: *"What do I need to focus on right now?"*

**Design Principles:**
- Personal-first (no team data)
- Actionable (not a dashboard)
- No comparison/evaluation

### ME Screen Sections

| Section | Description | Data Source |
|---------|-------------|-------------|
| **My Tasks** | Tasks owned by user (self-created + assigned by others) | `tasks WHERE owner_id = current_user` |
| **My Goals** | Goals owned by user with progress, due dates, linked targets | `goals WHERE owner_id = current_user` |
| **My Meetings** | Upcoming/recent meetings with cadence indicators | `meetings WHERE participant includes current_user` |
| **My Feedback** | Feedback received + feedback authored by user | `feedback WHERE recipient_id = current_user OR author_id = current_user` |
| **Oracle Insights** | Passive AI insights for prep/awareness (never evaluative) | AI-generated, deferred to v2 |

### Out of Scope (v1)
- Performance scoring
- Cross-user comparisons
- Historical analytics

---

### ME Implementation Steps

| Step | Task | Effort | Dependencies |
|------|------|--------|--------------|
| ME-1 | Create `MeView.axaml` shell with section layout | Small | None |
| ME-2 | Create `MeViewModel.cs` with data loading | Medium | ME-1 |
| ME-3 | Add `NavigationItem.Me` to MainWindow navigation | Small | ME-1 |
| ME-4 | Build My Tasks section (reuse task card pattern) | Medium | ME-2 |
| ME-5 | Build My Goals section (progress bars, targets) | Medium | ME-2 |
| ME-6 | Build My Meetings section (cadence indicators) | Medium | ME-2 |
| ME-7 | Build My Feedback section (received/authored tabs) | Medium | ME-2 |
| ME-8 | Oracle Insights placeholder (v2) | Small | ME-2 |

---

### ME-1: Create MeView.axaml Shell

**File:** `Views/MeView.axaml`

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│  ME (Header)                                            │
│  Your personal focus hub                                │
├─────────────────────────────────────────────────────────┤
│ ┌───────────────────────────────────────────────────┐   │
│ │  MY TASKS                              [+ New]    │   │
│ │  ┌────────────────────────────────────────────┐   │   │
│ │  │ □ Task 1 - Due Today           Overdue     │   │   │
│ │  │ □ Task 2 - Due Tomorrow        Assigned by │   │   │
│ │  └────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────┘   │
│                                                         │
│ ┌─────────────────────┐ ┌─────────────────────┐         │
│ │ MY GOALS            │ │ MY MEETINGS          │        │
│ │ ● Goal 1    [====]  │ │ 📅 1:1 w/ Manager   │         │
│ │ ● Goal 2    [==  ]  │ │    Tomorrow 2pm     │         │
│ └─────────────────────┘ └─────────────────────┘         │
│                                                         │
│ ┌───────────────────────────────────────────────────┐   │
│ │  MY FEEDBACK                                      │   │
│ │  [Received] [Given]                               │   │
│ └───────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

**Key Bindings:**
- `MyTasks` - ObservableCollection<TaskDetail>
- `MyGoals` - ObservableCollection<GoalDetail>
- `MyMeetings` - ObservableCollection<MeetingDetail>
- `ReceivedFeedback` - ObservableCollection<FeedbackDetail>
- `GivenFeedback` - ObservableCollection<FeedbackDetail>

---

### ME-2: Create MeViewModel.cs

**File:** `ViewModels/MeViewModel.cs`

```csharp
public partial class MeViewModel : ViewModelBase
{
    // Collections
    [ObservableProperty] private ObservableCollection<TaskDetail> _myTasks = new();
    [ObservableProperty] private ObservableCollection<GoalDetail> _myGoals = new();
    [ObservableProperty] private ObservableCollection<MeetingDetail> _myMeetings = new();
    [ObservableProperty] private ObservableCollection<FeedbackDetail> _receivedFeedback = new();
    [ObservableProperty] private ObservableCollection<FeedbackDetail> _givenFeedback = new();
    
    // Computed
    public IEnumerable<TaskDetail> SortedTasks => MyTasks
        .OrderBy(t => GetTaskUrgency(t))
        .ThenBy(t => t.DueDate ?? DateTime.MaxValue);
    
    // Loading
    public async Task LoadDataAsync()
    {
        var currentUserId = AuthenticationSettings.Instance.TeamMemberId;
        
        // Load tasks where I am the owner
        var tasks = await _taskService.GetTasksForOwnerAsync(currentUserId);
        MyTasks = new ObservableCollection<TaskDetail>(tasks);
        
        // Load my goals
        var goals = await _goalService.GetGoalsForOwnerAsync(currentUserId);
        MyGoals = new ObservableCollection<GoalDetail>(goals);
        
        // Load my meetings
        var meetings = await _meetingService.GetMeetingsForParticipantAsync(currentUserId);
        MyMeetings = new ObservableCollection<MeetingDetail>(meetings);
        
        // Load my feedback
        var received = await _feedbackService.GetFeedbackForRecipientAsync(currentUserId);
        ReceivedFeedback = new ObservableCollection<FeedbackDetail>(received);
        
        var given = await _feedbackService.GetFeedbackByAuthorAsync(currentUserId);
        GivenFeedback = new ObservableCollection<FeedbackDetail>(given);
    }
}
```

---

### ME-3: Add Navigation

**File:** `ViewModels/MainWindowViewModel.cs`

Add `Me` to `NavigationItem` enum (if not present):
```csharp
public enum NavigationItem
{
    Briefing,
    Me,      // NEW - Add between Briefing and Circle
    Circle,
    Pulse,
    Chronicle,
    Settings
}
```

**File:** `Views/MainWindow.axaml`

Add navigation button and content panel:
```xml
<!-- In nav buttons -->
<Button Classes="nav-item"
        Classes.selected="{Binding SelectedNavigation, Converter={StaticResource EnumEqualConverter}, ConverterParameter={x:Static vm:NavigationItem.Me}}"
        Command="{Binding SetNavigationCommand}"
        CommandParameter="{x:Static vm:NavigationItem.Me}">
    <StackPanel>
        <PathIcon Data="M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"/>
        <TextBlock Text="Me"/>
    </StackPanel>
</Button>

<!-- In content panels -->
<Grid IsVisible="{Binding SelectedNavigation, Converter={StaticResource EnumEqualConverter}, ConverterParameter={x:Static vm:NavigationItem.Me}}">
    <views:MeView />
</Grid>
```

---

### ME-4 through ME-7: Section Details

#### My Tasks Section
- Full width, dominant position (like IC Briefing)
- Ordered: Overdue → Due Today → Due Soon → Future → No Date
- Shows: checkbox, title, due date (colored), assigned by (if delegated)
- Quick action: [+ New Task] button

#### My Goals Section  
- Card per goal with progress bar
- Shows: goal title, progress %, due date, linked targets count
- Click opens goal detail flyout

#### My Meetings Section
- Upcoming meetings list
- Shows: title, date/time, cadence indicator (weekly, biweekly, etc.)
- Click opens meeting detail flyout

#### My Feedback Section
- Two tabs: [Received] [Given]
- Received: feedback where I'm the recipient
- Given: feedback where I'm the author
- Shows: feedback preview, date, related person

---

## Part B: Circle Screen Enhancement

### Overview

Circle is a **manager-only** view for understanding team activity. The spec says:
- Signal over detail
- No ranking/comparison
- Drill-down by intent

### Current State (Per CIRCLE_UI_PLAN.md)

| Step | Description | Status |
|------|-------------|--------|
| 1 | Database: `get_visible_team_member_ids()` wrapper RPC | ✅ DONE |
| 2 | Model: Add hierarchy fields to `TeamMemberDetail` | ✅ DONE |
| 3 | Service: Create `TeamService` with 2-step fetch | ✅ DONE |
| 4 | ViewModel: Update `CircleViewModel` to use TeamService | ✅ DONE |
| 5 | UI: Add view mode toggle (Flat/Tree) | 🔲 TODO |
| 6 | UI: Build tree view rendering | 🔲 TODO |
| 7 | UI: Enhance member cards | 🔲 TODO |
| 8 | UI: Add manager click filter | 🔲 TODO |
| 9 | UI: Enhance detail panel with team tab | 🔲 TODO |

---

### Spec Requirements Analysis

**Team Member Cards (from spec):**
Cards surface only high-level indicators:
- Time since last 1:1
- Recent activity presence
- Feedback cadence indicators
- **No content shown directly on cards**

**Flyout Panel Tabs (from spec):**
| Tab | Content |
|-----|---------|
| Info | Role and relationship metadata |
| Goals | Shared or manager-relevant goals |
| Tasks | Tasks assigned by manager or shared upward |
| Meetings | Cadence and metadata only |
| Feedback | Feedback manager authored or is permitted to view |

**Oracle Insights:**
- Highlights trends and preparation needs
- Never evaluates people or assigns blame
- Deferred to v2

---

### Circle Implementation Steps (5-9)

#### Step 5: View Mode Toggle

**Current:** No toggle exists
**Target:** Add Flat/Tree toggle to header

```xml
<!-- Add to CircleView header area -->
<StackPanel Orientation="Horizontal" Spacing="4">
    <ToggleButton Classes="view-toggle"
                  IsChecked="{Binding IsFlatView}"
                  Content="Grid"/>
    <ToggleButton Classes="view-toggle"
                  IsChecked="{Binding IsTreeView}"
                  Content="Tree"/>
</StackPanel>
```

**ViewModel additions:**
```csharp
[ObservableProperty] private TeamViewMode _viewMode = TeamViewMode.Flat;

public bool IsFlatView => ViewMode == TeamViewMode.Flat;
public bool IsTreeView => ViewMode == TeamViewMode.Tree;

public enum TeamViewMode { Flat, Tree }
```

---

#### Step 6: Tree View Rendering

**Layout concept:**
```
┌─────────────────────────────────────────────────┐
│ ▼ Alice Chen (Manager)                    5 DR  │
│    ├─ Bob Smith                                 │
│    ├─ Carol Davis                               │
│    └─ ▼ Dave Wilson (Manager)            2 DR  │
│         ├─ Eve Brown                            │
│         └─ Frank Lee                            │
└─────────────────────────────────────────────────┘
```

**Key binding:** `HierarchyDepth` (from RPC) controls indentation
- Depth 0 = no indent
- Depth 1 = 20px indent
- Depth 2 = 40px indent

**Expand/collapse state:** `Dictionary<Guid, bool> ExpandedNodes`

---

#### Step 7: Enhanced Member Cards (per spec)

**Card indicators (high-level signals only):**

| Indicator | Description | Visual |
|-----------|-------------|--------|
| Last 1:1 | Days since last meeting | "3d ago" or "⚠️ 14d" |
| Activity | Any recent activity | Green dot or gray |
| Feedback | Recent feedback given/received | Badge count |

**IMPORTANT (from spec):** No content on cards. Just signals.

```xml
<Border Classes="member-card">
    <Grid ColumnDefinitions="Auto,*,Auto">
        <!-- Avatar -->
        <Border Grid.Column="0" Width="40" Height="40" CornerRadius="20">
            <TextBlock Text="{Binding Initials}"/>
        </Border>
        
        <!-- Name + Role -->
        <StackPanel Grid.Column="1" Margin="12,0">
            <TextBlock Text="{Binding FullName}" FontWeight="Medium"/>
            <TextBlock Text="{Binding Role}" FontSize="12" Opacity="0.7"/>
        </StackPanel>
        
        <!-- Signal Indicators -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8">
            <!-- Last 1:1 indicator -->
            <Border ToolTip.Tip="Last 1:1">
                <TextBlock Text="{Binding DaysSinceLastOneOnOne, StringFormat='{0}d'}"
                           Foreground="{Binding OneOnOneUrgencyBrush}"/>
            </Border>
            
            <!-- Activity dot -->
            <Ellipse Width="8" Height="8" 
                     Fill="{Binding HasRecentActivity, Converter={StaticResource BoolToActivityColor}}"/>
            
            <!-- Feedback badge -->
            <Border IsVisible="{Binding RecentFeedbackCount, Converter={StaticResource GreaterThanZero}}"
                    Background="{DynamicResource BrushPrimary}" CornerRadius="8" Padding="4,2">
                <TextBlock Text="{Binding RecentFeedbackCount}" FontSize="10"/>
            </Border>
        </StackPanel>
    </Grid>
</Border>
```

---

#### Step 8: Manager Click Filter

**Flat view behavior:**
- Clicking a manager card toggles filter to show only their team
- Breadcrumb appears: "All Team > Alice Chen's Team"
- Click breadcrumb to clear filter

**ViewModel:**
```csharp
[ObservableProperty] private TeamMemberDetail? _filterByManager;

public ObservableCollection<TeamMemberDetail> FilteredTeamMembers
{
    get
    {
        if (FilterByManager == null)
            return AllVisibleMembers;
        
        return new ObservableCollection<TeamMemberDetail>(
            AllVisibleMembers.Where(m => 
                m.ManagerTeamMemberId == FilterByManager.Id || 
                m.Id == FilterByManager.Id));
    }
}

[RelayCommand]
private void FilterByManagerClick(TeamMemberDetail manager)
{
    FilterByManager = FilterByManager?.Id == manager.Id ? null : manager;
    OnPropertyChanged(nameof(FilteredTeamMembers));
}
```

---

#### Step 9: Detail Panel Team Tab

**For managers, add "Team" tab showing their direct reports:**

```xml
<!-- In detail panel tab bar -->
<Button Classes="tab-button"
        IsVisible="{Binding SelectedMember.IsManager}"
        Classes.selected="{Binding IsDetailTabTeam}"
        Command="{Binding SetDetailTabCommand}"
        CommandParameter="Team">
    <StackPanel Orientation="Horizontal" Spacing="4">
        <PathIcon Data="M12,5.5A3.5,3.5 0 0,1 15.5,9A3.5,3.5 0 0,1 12,12.5A3.5,3.5 0 0,1 8.5,9A3.5,3.5 0 0,1 12,5.5M5,8C5.56,8 6.08,8.15 6.53,8.42C6.38,9.85 6.8,11.27 7.66,12.38C7.16,13.34 6.16,14 5,14A3,3 0 0,1 2,11A3,3 0 0,1 5,8M19,8A3,3 0 0,1 22,11A3,3 0 0,1 19,14C17.84,14 16.84,13.34 16.34,12.38C17.2,11.27 17.62,9.85 17.47,8.42C17.92,8.15 18.44,8 19,8M5.5,18.25C5.5,16.18 8.41,14.5 12,14.5C15.59,14.5 18.5,16.18 18.5,18.25V20H5.5V18.25M0,20V18.5C0,17.11 1.89,15.94 4.45,15.6C3.86,16.28 3.5,17.22 3.5,18.25V20H0M24,20H20.5V18.25C20.5,17.22 20.14,16.28 19.55,15.6C22.11,15.94 24,17.11 24,18.5V20Z"/>
        <TextBlock Text="Team"/>
        <TextBlock Text="{Binding SelectedMember.DirectReportCount, StringFormat='({0})'}"
                   Opacity="0.7"/>
    </StackPanel>
</Button>

<!-- Team tab content -->
<ScrollViewer IsVisible="{Binding IsDetailTabTeam}">
    <ItemsControl Items="{Binding SelectedMemberDirectReports}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <!-- Compact member card -->
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</ScrollViewer>
```

---

## Implementation Priority

### Phase 1: Circle UI Completion (Steps 5-9)
**Rationale:** Backend work done, just UI remaining. Quick wins.

| Priority | Step | Effort |
|----------|------|--------|
| 1 | Step 7: Enhance cards with indicators | Small |
| 2 | Step 9: Detail panel team tab | Small |
| 3 | Step 5: View mode toggle | Small |
| 4 | Step 6: Tree view rendering | Medium |
| 5 | Step 8: Manager click filter | Small |

### Phase 2: ME Screen Implementation
**Rationale:** New screen, more work, but high user value.

| Priority | Step | Effort |
|----------|------|--------|
| 1 | ME-1 + ME-2: Shell + ViewModel | Medium |
| 2 | ME-3: Navigation integration | Small |
| 3 | ME-4: My Tasks section | Medium |
| 4 | ME-5: My Goals section | Medium |
| 5 | ME-6: My Meetings section | Small |
| 6 | ME-7: My Feedback section | Medium |

---

## Technical Notes

### Shared Patterns to Reuse

1. **Task cards:** Use same pattern from IC Briefing (urgency ordering, due date colors)
2. **Goal progress:** Existing `GoalCard` pattern in Pulse
3. **Meeting cards:** Existing pattern in Circle calendar
4. **Feedback cards:** Existing pattern in Circle feedback tab
5. **Flyouts:** Reuse `MeetingDetailFlyout`, `TeamMemberDetailFlyout`

### Data Access

ME screen queries should filter by `owner_id` or `author_id = current_user` to ensure personal-only data. No team aggregation.

Circle queries already use `TeamService.GetVisibleTeamMembersAsync()` which respects visibility rules.

### Navigation Considerations

The ME screen should likely be the **default landing page** for ICs (not Briefing). Consider:
- Admin/Manager → Briefing (Manager view)
- IC → ME (Personal hub)

This can be handled in `MainWindowViewModel.OnProfileChanged()`.

---

## Deferred Items

| Item | Reason | Target |
|------|--------|--------|
| Oracle Insights (ME) | AI integration needed | v2 |
| Oracle Insights (Circle) | AI integration needed | v2 |
| Agenda quality metrics | Complex analytics | v2+ |
| Advanced behavioral analytics | Complex analytics | v2+ |

---

## File Summary

### New Files (ME)
- `Views/MeView.axaml`
- `Views/MeView.axaml.cs`
- `ViewModels/MeViewModel.cs`

### Modified Files (Circle)
- `Views/CircleView.axaml` (Steps 5-9 UI changes)
- `ViewModels/CircleViewModel.cs` (view mode, filter, detail tab)

### Modified Files (Navigation)
- `Views/MainWindow.axaml` (add ME nav + content)
- `ViewModels/MainWindowViewModel.cs` (add ME to enum)

---

## Ready to Start?

**Recommended first step:** Circle Step 7 (Enhance member cards with indicators)
- Small effort
- Visible improvement
- Uses existing data

Let me know which step you'd like to implement first!
