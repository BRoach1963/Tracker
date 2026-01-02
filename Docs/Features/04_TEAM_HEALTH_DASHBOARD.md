# Feature 04: Team Health Dashboard
## Technical Specification

**Feature ID:** F-004  
**Priority:** P1  
**Estimated Effort:** 2 sprints  
**Status:** Planning

---

## Executive Summary

Create a single-page executive view that provides at-a-glance team health visibility. The dashboard aggregates data across all team members, OKRs, KPIs, meetings, surveys, and tasks to surface patterns and areas requiring attention without requiring drill-down into individual records.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want a single view of team health so I can quickly assess overall status | P0 |
| US-002 | As a manager, I want individual health indicators per team member so I can spot who needs attention | P0 |
| US-003 | As a manager, I want meeting cadence tracking so I know if I'm keeping up with 1:1s | P1 |
| US-004 | As a manager, I want aggregated survey sentiment so I can gauge team morale | P1 |
| US-005 | As a manager, I want OKR portfolio health so I can see overall goal progress | P1 |
| US-006 | As a manager, I want upcoming milestone visibility so I don't miss important dates | P2 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TEAM HEALTH DASHBOARD                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    TeamHealthService                                 │    │
│  │                                                                       │    │
│  │   CalculateTeamHealthAsync() → TeamHealthReport                      │    │
│  │                                                                       │    │
│  │   Components:                                                         │    │
│  │   ┌───────────────┐ ┌───────────────┐ ┌───────────────────────────┐ │    │
│  │   │ Member Health │ │ Meeting       │ │ Goal Portfolio            │ │    │
│  │   │ Calculator    │ │ Cadence Calc  │ │ Calculator                │ │    │
│  │   └───────────────┘ └───────────────┘ └───────────────────────────┘ │    │
│  │   ┌───────────────┐ ┌───────────────┐ ┌───────────────────────────┐ │    │
│  │   │ Survey        │ │ Task Status   │ │ Milestone                 │ │    │
│  │   │ Aggregator    │ │ Aggregator    │ │ Tracker                   │ │    │
│  │   └───────────────┘ └───────────────┘ └───────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Dashboard UI Layout                               │    │
│  │  ┌─────────────────────────────────────────────────────────────┐   │    │
│  │  │              TEAM OVERVIEW HEADER                            │   │    │
│  │  │   Overall Health Score: 78/100  │  12 members  │  8 OKRs    │   │    │
│  │  └─────────────────────────────────────────────────────────────┘   │    │
│  │                                                                      │    │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │    │
│  │  │  TEAM MEMBERS    │  │  OKR PORTFOLIO   │  │  MEETING        │   │    │
│  │  │  Health Grid     │  │  Status Donut    │  │  CADENCE        │   │    │
│  │  │  🟢🟢🟡🟢🔴🟢   │  │  On Track: 5     │  │  Avg: 8.2 days  │   │    │
│  │  │  🟢🟡🟢🟢🟢🟡   │  │  At Risk: 2      │  │  Overdue: 2     │   │    │
│  │  └──────────────────┘  │  Off Track: 1    │  │                 │   │    │
│  │                        └──────────────────┘  └─────────────────┘   │    │
│  │                                                                      │    │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────┐   │    │
│  │  │  SURVEY          │  │  TASK STATUS     │  │  UPCOMING       │   │    │
│  │  │  SENTIMENT       │  │  SUMMARY         │  │  MILESTONES     │   │    │
│  │  │  Trend Chart     │  │  Overdue: 5      │  │  • Sarah Bday   │   │    │
│  │  │  Avg: 3.8/5      │  │  Due Soon: 12    │  │  • Q4 OKR End   │   │    │
│  │  └──────────────────┘  │  Complete: 45    │  │  • John 1yr     │   │    │
│  │                        └──────────────────┘  └─────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. Team Health Data Models

```csharp
public class TeamHealthReport
{
    public DateTime GeneratedAt { get; set; }
    
    // Overall Score (0-100)
    public int OverallHealthScore { get; set; }
    public HealthLevel OverallHealthLevel { get; set; }
    public string OverallSummary { get; set; }
    
    // Team Overview
    public int TotalTeamMembers { get; set; }
    public int ActiveTeamMembers { get; set; }
    
    // Individual Member Health
    public List<MemberHealthSummary> MemberHealth { get; set; }
    
    // Meeting Cadence
    public MeetingCadenceReport MeetingCadence { get; set; }
    
    // Goal Portfolio
    public GoalPortfolioReport GoalPortfolio { get; set; }
    
    // Survey Sentiment
    public SurveySentimentReport SurveySentiment { get; set; }
    
    // Task Status
    public TaskStatusReport TaskStatus { get; set; }
    
    // Upcoming Milestones
    public List<UpcomingMilestone> UpcomingMilestones { get; set; }
    
    // Attention Items
    public List<AttentionItem> RequiresAttention { get; set; }
}

public enum HealthLevel
{
    Excellent,  // 80-100 - Green
    Good,       // 60-79  - Light green
    Fair,       // 40-59  - Yellow
    Poor,       // 20-39  - Orange  
    Critical    // 0-19   - Red
}

public class MemberHealthSummary
{
    public TeamMember Member { get; set; }
    public HealthLevel HealthLevel { get; set; }
    public int HealthScore { get; set; }  // 0-100
    
    // Factors
    public int DaysSinceLastMeeting { get; set; }
    public int OverdueTaskCount { get; set; }
    public int OkrsAtRiskCount { get; set; }
    public decimal? LastSurveyAvgRating { get; set; }
    
    // Quick Status
    public string StatusEmoji { get; set; }  // 🟢🟡🔴
    public string PrimaryConcern { get; set; }  // null if healthy
}

public class MeetingCadenceReport
{
    public decimal AverageDaysBetweenMeetings { get; set; }
    public int MembersOverdue { get; set; }      // >14 days since 1:1
    public int MembersCritical { get; set; }     // >21 days since 1:1
    public int MeetingsThisWeek { get; set; }
    public int MeetingsScheduledNextWeek { get; set; }
    
    // Per-member breakdown
    public List<MemberMeetingStatus> MemberStatuses { get; set; }
}

public class MemberMeetingStatus
{
    public TeamMember Member { get; set; }
    public DateTime? LastMeetingDate { get; set; }
    public DateTime? NextMeetingDate { get; set; }
    public int DaysSinceLast { get; set; }
    public int? DaysUntilNext { get; set; }
    public MeetingCadenceStatus Status { get; set; }
}

public enum MeetingCadenceStatus
{
    OnTrack,     // Met within target cadence
    DueSoon,     // Meeting due in next 3 days
    Overdue,     // >14 days, <21 days
    Critical     // >21 days
}

public class GoalPortfolioReport
{
    // OKR Summary
    public int TotalOkrs { get; set; }
    public int OkrsOnTrack { get; set; }
    public int OkrsAtRisk { get; set; }
    public int OkrsOffTrack { get; set; }
    public decimal AverageOkrProgress { get; set; }
    
    // KPI Summary
    public int TotalKpis { get; set; }
    public int KpisOnTarget { get; set; }
    public int KpisCloseToTarget { get; set; }
    public int KpisOffTarget { get; set; }
    
    // Portfolio Health Score
    public int PortfolioHealthScore { get; set; }
    
    // Trending
    public List<GoalTrendItem> OkrsByStatus { get; set; }
}

public class SurveySentimentReport
{
    public decimal AverageRating { get; set; }
    public decimal PreviousPeriodRating { get; set; }
    public decimal RatingTrend { get; set; }  // +/- change
    public TrendDirection TrendDirection { get; set; }
    
    public int TotalResponses { get; set; }
    public int ResponsesThisPeriod { get; set; }
    
    // Per-question averages (if applicable)
    public List<QuestionRatingSummary> QuestionSummaries { get; set; }
    
    // Concerning responses
    public int LowRatingCount { get; set; }  // Ratings <= 2
}

public enum TrendDirection
{
    Improving,
    Stable,
    Declining
}

public class TaskStatusReport
{
    public int TotalActiveTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueSoon { get; set; }  // Next 7 days
    public int CompletedThisWeek { get; set; }
    public int CompletedThisMonth { get; set; }
    
    // By owner
    public Dictionary<int, int> OverdueByMember { get; set; }
}

public class UpcomingMilestone
{
    public string Title { get; set; }
    public string Type { get; set; }  // Birthday, Anniversary, OKR End, Project Due
    public DateTime Date { get; set; }
    public int DaysAway { get; set; }
    public string Icon { get; set; }
    public TeamMember? RelatedMember { get; set; }
}

public class AttentionItem
{
    public AttentionSeverity Severity { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ActionSuggestion { get; set; }
    public string EntityType { get; set; }
    public int? EntityId { get; set; }
}

public enum AttentionSeverity
{
    Info,
    Warning,
    Critical
}
```

### 2. TeamHealthService

```csharp
public class TeamHealthService
{
    public async Task<TeamHealthReport> GenerateReportAsync()
    {
        var report = new TeamHealthReport
        {
            GeneratedAt = DateTime.Now,
            MemberHealth = new List<MemberHealthSummary>(),
            UpcomingMilestones = new List<UpcomingMilestone>(),
            RequiresAttention = new List<AttentionItem>()
        };
        
        // Get base data
        var members = await _db.GetTeamMembersAsync();
        var activeMembbers = members.Where(m => m.IsActive && !m.IsDeleted).ToList();
        
        report.TotalTeamMembers = members.Count;
        report.ActiveTeamMembers = activeMembers.Count;
        
        // Calculate individual member health
        foreach (var member in activeMembers)
        {
            var memberHealth = await CalculateMemberHealthAsync(member);
            report.MemberHealth.Add(memberHealth);
        }
        
        // Calculate aggregates
        report.MeetingCadence = await CalculateMeetingCadenceAsync(activeMembers);
        report.GoalPortfolio = await CalculateGoalPortfolioAsync();
        report.SurveySentiment = await CalculateSurveySentimentAsync();
        report.TaskStatus = await CalculateTaskStatusAsync(activeMembers);
        
        // Gather milestones
        report.UpcomingMilestones = await GatherUpcomingMilestonesAsync(activeMembers);
        
        // Calculate overall health score
        report.OverallHealthScore = CalculateOverallScore(report);
        report.OverallHealthLevel = ScoreToLevel(report.OverallHealthScore);
        report.OverallSummary = GenerateOverallSummary(report);
        
        // Identify attention items
        report.RequiresAttention = IdentifyAttentionItems(report);
        
        return report;
    }
    
    private async Task<MemberHealthSummary> CalculateMemberHealthAsync(TeamMember member)
    {
        var summary = new MemberHealthSummary
        {
            Member = member
        };
        
        // Last meeting
        var lastMeeting = await _db.GetLastCompletedMeetingAsync(member.Id);
        summary.DaysSinceLastMeeting = lastMeeting != null
            ? (DateTime.Today - lastMeeting.MeetingDate.Date).Days
            : int.MaxValue;
        
        // Overdue tasks
        var tasks = await _db.GetTasksForMemberAsync(member.Id);
        summary.OverdueTaskCount = tasks.Count(t => 
            t.DueDate < DateTime.Today && !t.IsComplete);
        
        // OKRs at risk
        var okrs = await _db.GetOkrsForOwnerAsync(member.Id);
        summary.OkrsAtRiskCount = okrs.Count(o => 
            o.IsActive && o.Status != ObjectiveStatusEnum.OnTrack);
        
        // Survey rating
        var recentResponses = await _db.GetSurveyResponsesForMemberAsync(
            member.Id, DateTime.Today.AddDays(-30));
        if (recentResponses.Any())
        {
            var ratings = recentResponses
                .SelectMany(r => r.Answers)
                .Where(a => a.RatingValue.HasValue)
                .Select(a => a.RatingValue.Value);
            if (ratings.Any())
                summary.LastSurveyAvgRating = (decimal)ratings.Average();
        }
        
        // Calculate score
        summary.HealthScore = CalculateMemberScore(summary);
        summary.HealthLevel = ScoreToLevel(summary.HealthScore);
        summary.StatusEmoji = LevelToEmoji(summary.HealthLevel);
        summary.PrimaryConcern = IdentifyPrimaryConcern(summary);
        
        return summary;
    }
    
    private int CalculateMemberScore(MemberHealthSummary summary)
    {
        // Start at 100, deduct for issues
        var score = 100;
        
        // Meeting cadence (up to -30 points)
        if (summary.DaysSinceLastMeeting > 21)
            score -= 30;
        else if (summary.DaysSinceLastMeeting > 14)
            score -= 15;
        else if (summary.DaysSinceLastMeeting > 10)
            score -= 5;
        
        // Overdue tasks (up to -25 points)
        score -= Math.Min(25, summary.OverdueTaskCount * 5);
        
        // OKRs at risk (up to -25 points)
        score -= Math.Min(25, summary.OkrsAtRiskCount * 10);
        
        // Survey rating (up to -20 points)
        if (summary.LastSurveyAvgRating.HasValue)
        {
            if (summary.LastSurveyAvgRating < 2)
                score -= 20;
            else if (summary.LastSurveyAvgRating < 3)
                score -= 10;
            else if (summary.LastSurveyAvgRating < 3.5m)
                score -= 5;
        }
        
        return Math.Max(0, score);
    }
    
    private int CalculateOverallScore(TeamHealthReport report)
    {
        if (!report.MemberHealth.Any())
            return 100;
        
        // Weighted average of components
        var memberAvg = report.MemberHealth.Average(m => m.HealthScore);
        var meetingScore = CalculateMeetingCadenceScore(report.MeetingCadence);
        var goalScore = report.GoalPortfolio.PortfolioHealthScore;
        var surveyScore = CalculateSurveyScore(report.SurveySentiment);
        var taskScore = CalculateTaskScore(report.TaskStatus);
        
        // Weights
        var score = 
            memberAvg * 0.30 +           // Individual health: 30%
            meetingScore * 0.20 +        // Meeting cadence: 20%
            goalScore * 0.25 +           // Goal portfolio: 25%
            surveyScore * 0.15 +         // Survey sentiment: 15%
            taskScore * 0.10;            // Task status: 10%
        
        return (int)Math.Round(score);
    }
    
    private HealthLevel ScoreToLevel(int score) => score switch
    {
        >= 80 => HealthLevel.Excellent,
        >= 60 => HealthLevel.Good,
        >= 40 => HealthLevel.Fair,
        >= 20 => HealthLevel.Poor,
        _ => HealthLevel.Critical
    };
    
    private string LevelToEmoji(HealthLevel level) => level switch
    {
        HealthLevel.Excellent => "🟢",
        HealthLevel.Good => "🟢",
        HealthLevel.Fair => "🟡",
        HealthLevel.Poor => "🟠",
        HealthLevel.Critical => "🔴",
        _ => "⚪"
    };
}
```

### 3. UI Components

#### TeamHealthDashboard.xaml
Main dashboard control with all sections.

```xml
<UserControl x:Class="Tracker.Controls.TeamHealthDashboard">
    <ScrollViewer>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/> <!-- Header -->
                <RowDefinition Height="Auto"/> <!-- Stats Row -->
                <RowDefinition Height="*"/>    <!-- Main Grid -->
            </Grid.RowDefinitions>
            
            <!-- HEADER -->
            <Border Grid.Row="0" Background="{DynamicResource PrimaryBrush}" Padding="20">
                <Grid>
                    <StackPanel>
                        <TextBlock Text="Team Health Dashboard" FontSize="24" FontWeight="Bold"/>
                        <TextBlock Text="{Binding LastUpdated, StringFormat='Updated {0:g}'}" Opacity="0.8"/>
                    </StackPanel>
                    
                    <StackPanel HorizontalAlignment="Right" Orientation="Horizontal">
                        <TextBlock Text="{Binding OverallHealthScore}" FontSize="48" FontWeight="Bold"/>
                        <TextBlock Text="/100" FontSize="24" VerticalAlignment="Bottom" Margin="0,0,0,10"/>
                    </StackPanel>
                </Grid>
            </Border>
            
            <!-- QUICK STATS ROW -->
            <UniformGrid Grid.Row="1" Columns="4" Margin="20,20,20,0">
                <local:StatCard 
                    Title="Team Members" 
                    Value="{Binding ActiveTeamMembers}" 
                    Icon="AccountGroup"/>
                <local:StatCard 
                    Title="Meetings This Week" 
                    Value="{Binding MeetingsThisWeek}" 
                    Icon="Calendar"/>
                <local:StatCard 
                    Title="OKRs On Track" 
                    Value="{Binding OkrsOnTrack}" 
                    Subtitle="{Binding TotalOkrs, StringFormat='of {0}'}"
                    Icon="Target"/>
                <local:StatCard 
                    Title="Survey Avg" 
                    Value="{Binding SurveyAverage, StringFormat='{0:F1}'}" 
                    Subtitle="/5"
                    Icon="ChartBar"/>
            </UniformGrid>
            
            <!-- MAIN DASHBOARD GRID -->
            <Grid Grid.Row="2" Margin="20">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                
                <!-- TEAM MEMBER GRID -->
                <local:TeamMemberHealthGrid 
                    Grid.Column="0" Grid.Row="0"
                    Members="{Binding MemberHealth}"
                    Margin="0,0,10,10"/>
                
                <!-- OKR PORTFOLIO -->
                <local:GoalPortfolioCard 
                    Grid.Column="1" Grid.Row="0"
                    Portfolio="{Binding GoalPortfolio}"
                    Margin="5,0,5,10"/>
                
                <!-- MEETING CADENCE -->
                <local:MeetingCadenceCard 
                    Grid.Column="2" Grid.Row="0"
                    Cadence="{Binding MeetingCadence}"
                    Margin="10,0,0,10"/>
                
                <!-- SURVEY SENTIMENT -->
                <local:SurveySentimentCard 
                    Grid.Column="0" Grid.Row="1"
                    Sentiment="{Binding SurveySentiment}"
                    Margin="0,10,10,0"/>
                
                <!-- TASK STATUS -->
                <local:TaskStatusCard 
                    Grid.Column="1" Grid.Row="1"
                    TaskStatus="{Binding TaskStatus}"
                    Margin="5,10,5,0"/>
                
                <!-- UPCOMING MILESTONES -->
                <local:UpcomingMilestonesCard 
                    Grid.Column="2" Grid.Row="1"
                    Milestones="{Binding UpcomingMilestones}"
                    Margin="10,10,0,0"/>
            </Grid>
        </Grid>
    </ScrollViewer>
</UserControl>
```

#### TeamMemberHealthGrid
Visual grid showing health status of each team member.

```xml
<ItemsControl ItemsSource="{Binding Members}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Button Command="{Binding DataContext.ViewMemberCommand, 
                              RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                    CommandParameter="{Binding Member}"
                    Style="{StaticResource TransparentButton}"
                    ToolTip="{Binding TooltipText}">
                <Grid Width="60" Height="70" Margin="4">
                    <Ellipse Width="50" Height="50" 
                             Fill="{Binding HealthLevel, Converter={StaticResource HealthLevelToBrush}}"/>
                    <TextBlock Text="{Binding Member.Initials}" 
                               HorizontalAlignment="Center" 
                               VerticalAlignment="Center"
                               FontWeight="Bold"/>
                    <TextBlock Text="{Binding StatusEmoji}" 
                               HorizontalAlignment="Right" 
                               VerticalAlignment="Bottom"
                               FontSize="14"/>
                </Grid>
            </Button>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

#### GoalPortfolioCard
Donut chart showing OKR status distribution.

#### MeetingCadenceCard
Bar chart showing meeting frequency per member.

#### SurveySentimentCard
Trend line chart with average rating over time.

---

## Data Flow

### Dashboard Generation Flow
```
User navigates to Dashboard
         │
         ▼
TeamHealthViewModel.LoadAsync()
         │
         ▼
TeamHealthService.GenerateReportAsync()
         │
         ├──▶ GetTeamMembersAsync()
         │
         ├──▶ For each member: CalculateMemberHealthAsync()
         │         ├── GetLastCompletedMeetingAsync()
         │         ├── GetTasksForMemberAsync()
         │         ├── GetOkrsForOwnerAsync()
         │         └── GetSurveyResponsesForMemberAsync()
         │
         ├──▶ CalculateMeetingCadenceAsync()
         │
         ├──▶ CalculateGoalPortfolioAsync()
         │
         ├──▶ CalculateSurveySentimentAsync()
         │
         ├──▶ CalculateTaskStatusAsync()
         │
         ├──▶ GatherUpcomingMilestonesAsync()
         │
         └──▶ CalculateOverallScore()
                   │
                   ▼
         Return TeamHealthReport
                   │
                   ▼
         Bind to UI components
```

---

## Configuration

### User Settings
```json
{
    "TeamHealthDashboard": {
        "IsEnabled": true,
        "ShowOnStartup": false,
        "MeetingCadenceTargetDays": 14,
        "MeetingCadenceCriticalDays": 21,
        "SurveyLookbackDays": 30,
        "UpcomingMilestoneDays": 14,
        "RefreshIntervalMinutes": 30
    }
}
```

---

## Implementation Plan

### Phase 1: Data Models & Service (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create TeamHealthReport models | 3h | None |
| Create TeamHealthService skeleton | 2h | Models |
| Implement CalculateMemberHealthAsync | 4h | Service |
| Implement CalculateMeetingCadenceAsync | 3h | Service |
| Implement CalculateGoalPortfolioAsync | 3h | Service |
| Implement CalculateSurveySentimentAsync | 3h | Service |
| Implement CalculateTaskStatusAsync | 2h | Service |
| Implement GatherUpcomingMilestonesAsync | 2h | Service |
| Implement overall score calculation | 2h | All above |

### Phase 2: UI Components (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create StatCard control | 2h | None |
| Create TeamMemberHealthGrid | 4h | Models |
| Create GoalPortfolioCard with chart | 4h | Models |
| Create MeetingCadenceCard | 3h | Models |
| Create SurveySentimentCard with chart | 4h | Models |
| Create TaskStatusCard | 2h | Models |
| Create UpcomingMilestonesCard | 2h | Models |
| Create TeamHealthDashboard layout | 4h | All cards |
| Create TeamHealthViewModel | 3h | Service |
| Navigation integration | 2h | Dashboard |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Dashboard query performance | Medium | Cache report, refresh periodically |
| Many database queries | Medium | Batch queries, parallel execution |
| Chart rendering performance | Low | Use virtualization, limit data points |

### Data Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Incomplete data skews scores | Medium | Weight factors by data availability |
| Survey anonymity leak | High | Never show individual survey responses |
| Stale data misleads | Low | Show "last updated" timestamp |

### UX Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Information overload | Medium | Progressive disclosure, clear hierarchy |
| Scores feel judgmental | Medium | Frame as "health check" not "performance" |
| Dashboard not actionable | Medium | Link items to relevant detail views |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Dashboard load time | <2s | Performance logging |
| Daily active users | >50% | Analytics |
| Click-through to details | >30% | Track navigation |
| Meeting cadence improvement | 15% | Compare before/after |

---

## Dependencies

- Existing: TrackerDbManager, all entity models
- New: TeamHealthService, UI controls
- Charting: LiveCharts2 (if not already added)

---

## Future Enhancements

1. **Exportable Reports** - PDF/Excel export of dashboard
2. **Historical Comparison** - Compare health over time
3. **Custom Weights** - User-defined scoring weights
4. **Team Comparison** - Multi-team view (for skip-level managers)
5. **Alerts Integration** - Tie to Proactive Insights system
6. **Goal Setting** - Set target health scores

---

**Document End**
