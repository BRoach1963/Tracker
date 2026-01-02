# Feature 02: Auto-Generated Meeting Prep
## Technical Specification

**Feature ID:** F-002  
**Priority:** P0 (Highest)  
**Estimated Effort:** 2 sprints  
**Status:** Planning

---

## Executive Summary

Automatically generate comprehensive meeting preparation materials for upcoming 1:1s. The system analyzes all relevant data about the team member and surfaces a contextualized agenda with talking points, eliminating the 10-15 minutes managers typically spend preparing for each meeting.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want auto-generated prep for each 1:1 so I don't spend time reviewing manually | P0 |
| US-002 | As a manager, I want to see overdue items for the person so I can address blockers | P0 |
| US-003 | As a manager, I want previous meeting action items so I can follow up | P0 |
| US-004 | As a manager, I want OKR/KPI status for the person so I can discuss progress | P1 |
| US-005 | As a manager, I want recent pulse survey ratings so I can check in on concerns | P1 |
| US-006 | As a manager, I want personal dates (birthday, anniversary) so I can recognize them | P1 |
| US-007 | As a manager, I want one-click to add suggestions to the actual meeting agenda | P1 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      MEETING PREP GENERATION SYSTEM                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        MeetingPrepService                             │   │
│  │                                                                        │   │
│  │   GeneratePrepAsync(OneOnOne meeting)                                 │   │
│  │        │                                                               │   │
│  │        ▼                                                               │   │
│  │   ┌─────────────────────────────────────────────────────────────┐    │   │
│  │   │              DATA GATHERERS (Parallel)                       │    │   │
│  │   │                                                               │    │   │
│  │   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐    │    │   │
│  │   │  │ Previous    │ │ Tasks &     │ │ OKRs & KPIs         │    │    │   │
│  │   │  │ Meetings    │ │ Overdue     │ │ Progress            │    │    │   │
│  │   │  └─────────────┘ └─────────────┘ └─────────────────────┘    │    │   │
│  │   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐    │    │   │
│  │   │  │ Survey      │ │ Feedback    │ │ Personal Dates      │    │    │   │
│  │   │  │ Responses   │ │ History     │ │ (Bday/Anniversary)  │    │    │   │
│  │   │  └─────────────┘ └─────────────┘ └─────────────────────┘    │    │   │
│  │   └─────────────────────────────────────────────────────────────┘    │   │
│  │                              │                                         │   │
│  │                              ▼                                         │   │
│  │   ┌─────────────────────────────────────────────────────────────┐    │   │
│  │   │              PREP COMPILER                                   │    │   │
│  │   │   - Prioritize items by urgency                              │    │   │
│  │   │   - Format into structured MeetingPrep                       │    │   │
│  │   │   - Optionally call Gemini for AI suggestions                │    │   │
│  │   └─────────────────────────────────────────────────────────────┘    │   │
│  │                              │                                         │   │
│  │                              ▼                                         │   │
│  │                       MeetingPrep Model                                │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        UI COMPONENTS                                   │   │
│  │                                                                        │   │
│  │   ┌─────────────────────┐     ┌────────────────────────────────┐     │   │
│  │   │ MeetingPrepPanel    │     │ Quick Actions                  │     │   │
│  │   │ (Flyout/Dialog)     │     │ - Add to Agenda                │     │   │
│  │   │                     │     │ - Dismiss Item                 │     │   │
│  │   │ • Priority Items    │     │ - View Details                 │     │   │
│  │   │ • Follow-ups        │     │ - Ask Oracle                   │     │   │
│  │   │ • OKR Status        │     └────────────────────────────────┘     │   │
│  │   │ • Personal Notes    │                                             │   │
│  │   └─────────────────────┘                                             │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. MeetingPrep Data Model

```csharp
public class MeetingPrep
{
    public int MeetingId { get; set; }
    public TeamMember TeamMember { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    // Header info
    public string MeetingTitle { get; set; }     // "1:1 with Sarah Chen"
    public DateTime ScheduledDate { get; set; }
    public int DaysSinceLastMeeting { get; set; }
    
    // Sections (each with priority flag)
    public List<PrepSection> Sections { get; set; }
    
    // AI-generated summary (optional)
    public string AiSuggestedAgenda { get; set; }
    
    // Quick stats
    public int OverdueItemCount { get; set; }
    public int OpenActionItemCount { get; set; }
    public int OkrsAtRiskCount { get; set; }
}

public class PrepSection
{
    public PrepSectionType Type { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }            // MDI icon name
    public SectionPriority Priority { get; set; }
    public List<PrepItem> Items { get; set; }
}

public enum PrepSectionType
{
    Urgent,           // ⚠️ Needs immediate attention
    FollowUp,         // 📋 Previous meeting action items
    TaskStatus,       // ✅ Current tasks
    GoalProgress,     // 🎯 OKRs and KPIs
    SurveyFeedback,   // 📊 Recent pulse survey responses
    Recognition,      // 🎂 Birthday/Anniversary
    RecentFeedback,   // 💬 Feedback given/received
    Suggested         // 💡 AI-suggested topics
}

public enum SectionPriority
{
    Critical,   // Red - must discuss
    High,       // Orange - should discuss
    Normal,     // Blue - good to discuss
    Low         // Gray - if time permits
}

public class PrepItem
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Subtext { get; set; }         // e.g., "5 days overdue"
    public ItemPriority Priority { get; set; }
    
    // For deep-linking
    public string EntityType { get; set; }
    public int? EntityId { get; set; }
    
    // Actions
    public bool CanAddToAgenda { get; set; }
    public bool CanDismiss { get; set; }
}
```

### 2. MeetingPrepService (`Services/MeetingPrepService.cs`)

```csharp
public class MeetingPrepService
{
    private readonly TrackerDbManager _db;
    private readonly GeminiChatService _ai;
    
    public async Task<MeetingPrep> GeneratePrepAsync(OneOnOne meeting)
    {
        var prep = new MeetingPrep
        {
            MeetingId = meeting.Id,
            TeamMember = meeting.TeamMember,
            ScheduledDate = meeting.MeetingDate,
            GeneratedAt = DateTime.Now
        };
        
        // Gather data in parallel
        var tasks = new[]
        {
            GetPreviousMeetingDataAsync(meeting),
            GetTaskDataAsync(meeting.TeamMember),
            GetOkrKpiDataAsync(meeting.TeamMember),
            GetSurveyDataAsync(meeting.TeamMember),
            GetFeedbackDataAsync(meeting.TeamMember),
            GetPersonalDatesAsync(meeting.TeamMember)
        };
        
        await Task.WhenAll(tasks);
        
        // Compile into sections
        prep.Sections = CompileSections(tasks.Select(t => t.Result));
        
        // Optional AI enhancement
        if (Settings.EnableAiSuggestions)
        {
            prep.AiSuggestedAgenda = await GenerateAiSuggestionsAsync(prep);
        }
        
        return prep;
    }
}
```

### 3. Data Gatherers

#### Previous Meeting Gatherer
```csharp
private async Task<PrepSection> GetPreviousMeetingDataAsync(OneOnOne meeting)
{
    var section = new PrepSection
    {
        Type = PrepSectionType.FollowUp,
        Title = "Follow-ups from Last Meeting",
        Icon = "ClipboardCheck",
        Priority = SectionPriority.High
    };
    
    // Get last completed meeting with this person
    var lastMeeting = await _db.GetLastCompletedMeetingAsync(
        meeting.TeamMember.Id, 
        meeting.Id
    );
    
    if (lastMeeting == null)
    {
        section.Items.Add(new PrepItem 
        { 
            Title = "First 1:1 with this team member",
            Description = "Consider discussing: Role expectations, working style, goals"
        });
        return section;
    }
    
    section.Items.Add(new PrepItem
    {
        Title = $"Last met: {lastMeeting.MeetingDate:MMM d}",
        Subtext = $"{(DateTime.Today - lastMeeting.MeetingDate.Date).Days} days ago"
    });
    
    // Extract action items from notes
    var actionItems = ExtractActionItems(lastMeeting.Notes);
    foreach (var item in actionItems.Where(a => !a.IsComplete))
    {
        section.Items.Add(new PrepItem
        {
            Title = item.Text,
            Subtext = "Open action item",
            Priority = ItemPriority.High,
            CanAddToAgenda = true
        });
    }
    
    return section;
}
```

#### Task Status Gatherer
```csharp
private async Task<PrepSection> GetTaskDataAsync(TeamMember member)
{
    var section = new PrepSection
    {
        Type = PrepSectionType.TaskStatus,
        Title = "Tasks",
        Icon = "CheckboxMarked"
    };
    
    var tasks = await _db.GetTasksForMemberAsync(member.Id);
    
    // Overdue tasks - Critical priority
    var overdue = tasks.Where(t => t.DueDate < DateTime.Today && !t.IsComplete);
    foreach (var task in overdue.OrderBy(t => t.DueDate).Take(5))
    {
        var daysOverdue = (DateTime.Today - task.DueDate.Value).Days;
        section.Items.Add(new PrepItem
        {
            Title = task.Description,
            Subtext = $"⚠️ {daysOverdue} days overdue",
            Priority = ItemPriority.Critical,
            CanAddToAgenda = true,
            EntityType = "Task",
            EntityId = task.Id
        });
    }
    
    // Due this week
    var thisWeek = tasks.Where(t => 
        t.DueDate >= DateTime.Today && 
        t.DueDate <= DateTime.Today.AddDays(7) && 
        !t.IsComplete
    );
    foreach (var task in thisWeek.Take(3))
    {
        section.Items.Add(new PrepItem
        {
            Title = task.Description,
            Subtext = $"Due {task.DueDate:MMM d}",
            Priority = ItemPriority.Normal,
            CanAddToAgenda = true
        });
    }
    
    // Set section priority based on content
    section.Priority = overdue.Any() ? SectionPriority.Critical : SectionPriority.Normal;
    
    return section;
}
```

#### OKR/KPI Gatherer
```csharp
private async Task<PrepSection> GetOkrKpiDataAsync(TeamMember member)
{
    var section = new PrepSection
    {
        Type = PrepSectionType.GoalProgress,
        Title = "Goals & Metrics",
        Icon = "Target"
    };
    
    // OKRs owned by this member
    var okrs = await _db.GetOkrsForOwnerAsync(member.Id);
    foreach (var okr in okrs.Where(o => o.IsActive).Take(3))
    {
        var statusIcon = okr.Status switch
        {
            ObjectiveStatusEnum.OnTrack => "🟢",
            ObjectiveStatusEnum.AtRisk => "🟡",
            ObjectiveStatusEnum.OffTrack => "🔴",
            _ => "⚪"
        };
        
        section.Items.Add(new PrepItem
        {
            Title = $"{statusIcon} {okr.Title}",
            Subtext = $"{okr.CompletionPercentage:F0}% complete • {okr.DaysRemaining} days left",
            Priority = okr.Status == ObjectiveStatusEnum.OffTrack 
                ? ItemPriority.Critical 
                : ItemPriority.Normal,
            CanAddToAgenda = true,
            EntityType = "OKR",
            EntityId = okr.ObjectiveId
        });
    }
    
    // KPIs owned by this member
    var kpis = await _db.GetKpisForOwnerAsync(member.Id);
    foreach (var kpi in kpis.Where(k => k.Status != KpiStatusEnum.OnTarget).Take(3))
    {
        section.Items.Add(new PrepItem
        {
            Title = kpi.Name,
            Subtext = $"{kpi.Value:N0}/{kpi.TargetValue:N0} {kpi.Unit} ({kpi.PercentComplete:F0}%)",
            Priority = kpi.Status == KpiStatusEnum.OffTarget 
                ? ItemPriority.High 
                : ItemPriority.Normal,
            EntityType = "KPI",
            EntityId = kpi.KpiId
        });
    }
    
    return section;
}
```

#### Survey Response Gatherer
```csharp
private async Task<PrepSection> GetSurveyDataAsync(TeamMember member)
{
    var section = new PrepSection
    {
        Type = PrepSectionType.SurveyFeedback,
        Title = "Recent Survey Responses",
        Icon = "ChartBar"
    };
    
    // Get responses from last 30 days
    var responses = await _db.GetSurveyResponsesForMemberAsync(
        member.Id, 
        DateTime.Today.AddDays(-30)
    );
    
    foreach (var response in responses.Take(3))
    {
        // Find any low ratings
        var lowRatings = response.Answers
            .Where(a => a.Question.QuestionType == SurveyQuestionType.Rating 
                     && a.RatingValue <= 3)
            .ToList();
        
        if (lowRatings.Any())
        {
            foreach (var answer in lowRatings)
            {
                section.Items.Add(new PrepItem
                {
                    Title = answer.Question.QuestionText,
                    Subtext = $"Rated {answer.RatingValue}/5 on {response.SubmittedAt:MMM d}",
                    Priority = answer.RatingValue <= 2 
                        ? ItemPriority.Critical 
                        : ItemPriority.High,
                    Description = "Consider checking in about this area"
                });
            }
        }
    }
    
    // Note: Respect anonymity settings
    if (!responses.Any())
    {
        section.Items.Add(new PrepItem
        {
            Title = "No recent survey responses",
            Priority = ItemPriority.Low
        });
    }
    
    return section;
}
```

#### Personal Dates Gatherer
```csharp
private async Task<PrepSection> GetPersonalDatesAsync(TeamMember member)
{
    var section = new PrepSection
    {
        Type = PrepSectionType.Recognition,
        Title = "Recognition Opportunities",
        Icon = "Gift"
    };
    
    var today = DateTime.Today;
    
    // Birthday check
    if (member.Birthday.HasValue)
    {
        var nextBirthday = new DateTime(
            today.Year, 
            member.Birthday.Value.Month, 
            member.Birthday.Value.Day
        );
        if (nextBirthday < today) 
            nextBirthday = nextBirthday.AddYears(1);
        
        var daysUntil = (nextBirthday - today).Days;
        if (daysUntil <= 7)
        {
            section.Items.Add(new PrepItem
            {
                Title = daysUntil == 0 
                    ? "🎂 Birthday TODAY!" 
                    : $"🎂 Birthday in {daysUntil} days",
                Subtext = nextBirthday.ToString("MMMM d"),
                Priority = daysUntil == 0 ? ItemPriority.High : ItemPriority.Normal
            });
        }
    }
    
    // Work anniversary check
    if (member.HireDate.HasValue)
    {
        var nextAnniversary = new DateTime(
            today.Year,
            member.HireDate.Value.Month,
            member.HireDate.Value.Day
        );
        if (nextAnniversary < today)
            nextAnniversary = nextAnniversary.AddYears(1);
        
        var daysUntil = (nextAnniversary - today).Days;
        var years = nextAnniversary.Year - member.HireDate.Value.Year;
        
        if (daysUntil <= 7)
        {
            section.Items.Add(new PrepItem
            {
                Title = daysUntil == 0
                    ? $"🎉 {years} Year Anniversary TODAY!"
                    : $"🎉 {years} Year Anniversary in {daysUntil} days",
                Subtext = $"Joined {member.HireDate.Value:MMMM d, yyyy}",
                Priority = daysUntil == 0 ? ItemPriority.High : ItemPriority.Normal
            });
        }
    }
    
    return section;
}
```

### 4. AI Suggestions Generator (Optional)

```csharp
private async Task<string> GenerateAiSuggestionsAsync(MeetingPrep prep)
{
    var prompt = BuildAiPrompt(prep);
    
    var response = await _ai.GetResponseAsync(
        $"Generate 3-5 specific, actionable agenda items for this 1:1. Be concise.\n\n{prompt}",
        maxTokens: 300
    );
    
    return response;
}

private string BuildAiPrompt(MeetingPrep prep)
{
    var sb = new StringBuilder();
    sb.AppendLine($"Preparing for 1:1 with {prep.TeamMember.FullName}");
    sb.AppendLine($"Role: {prep.TeamMember.JobTitle}");
    sb.AppendLine($"Tenure: {prep.TeamMember.TenureDisplay}");
    sb.AppendLine();
    
    foreach (var section in prep.Sections.Where(s => s.Items.Any()))
    {
        sb.AppendLine($"## {section.Title}");
        foreach (var item in section.Items.Take(5))
        {
            sb.AppendLine($"- {item.Title}: {item.Subtext}");
        }
        sb.AppendLine();
    }
    
    return sb.ToString();
}
```

### 5. UI Components

#### MeetingPrepPanel.xaml

```xml
<UserControl x:Class="Tracker.Controls.MeetingPrepPanel">
    <Grid>
        <!-- Header -->
        <Border Background="{DynamicResource PrimaryBrush}" CornerRadius="8,8,0,0">
            <StackPanel Margin="16">
                <TextBlock Text="📋 Meeting Prep" FontSize="18" FontWeight="Bold"/>
                <TextBlock Text="{Binding MeetingTitle}" FontSize="24" FontWeight="Bold"/>
                <TextBlock Text="{Binding ScheduledDateDisplay}" Opacity="0.8"/>
            </StackPanel>
        </Border>
        
        <!-- Stats Bar -->
        <Border Background="{DynamicResource SurfaceAltBrush}">
            <UniformGrid Columns="3" Margin="16,8">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="{Binding OverdueItemCount}" FontSize="24" FontWeight="Bold"/>
                    <TextBlock Text="Overdue" FontSize="11"/>
                </StackPanel>
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="{Binding OpenActionItemCount}" FontSize="24" FontWeight="Bold"/>
                    <TextBlock Text="Action Items" FontSize="11"/>
                </StackPanel>
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="{Binding OkrsAtRiskCount}" FontSize="24" FontWeight="Bold"/>
                    <TextBlock Text="OKRs at Risk" FontSize="11"/>
                </StackPanel>
            </UniformGrid>
        </Border>
        
        <!-- Sections -->
        <ScrollViewer>
            <ItemsControl ItemsSource="{Binding Sections}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <local:PrepSectionControl Section="{Binding}"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
        
        <!-- AI Suggestions (if enabled) -->
        <Expander Header="💡 AI Suggested Topics" IsExpanded="False">
            <TextBlock Text="{Binding AiSuggestedAgenda}" TextWrapping="Wrap"/>
        </Expander>
    </Grid>
</UserControl>
```

#### Integration Points

**Option A: Button in Meeting Dialog**
- Add "📋 View Prep" button to OneOnOneDialog
- Opens MeetingPrepPanel as flyout

**Option B: Automatic on Meeting Open**
- When opening a future meeting, show prep panel alongside
- Split view: Prep on left, meeting form on right

**Option C: Dashboard Widget**
- "Today's Meetings" card shows prep buttons for each
- Click opens full prep panel

---

## Data Flow

### Generation Flow
```
User clicks "View Prep" on Meeting
              │
              ▼
    MeetingPrepService.GeneratePrepAsync()
              │
              ├──▶ GetPreviousMeetingDataAsync() ─┐
              ├──▶ GetTaskDataAsync() ────────────┼──▶ Parallel
              ├──▶ GetOkrKpiDataAsync() ──────────┤
              ├──▶ GetSurveyDataAsync() ──────────┤
              ├──▶ GetFeedbackDataAsync() ────────┤
              └──▶ GetPersonalDatesAsync() ───────┘
                              │
                              ▼
                    CompileSections()
                    Sort by priority
                              │
                              ▼
              (Optional) GenerateAiSuggestionsAsync()
                              │
                              ▼
                    Return MeetingPrep
                              │
                              ▼
                    Display in MeetingPrepPanel
```

### Add to Agenda Flow
```
User clicks "Add to Agenda" on PrepItem
              │
              ▼
    MeetingPrepViewModel.AddToAgendaCommand
              │
              ▼
    meeting.Agenda += item.Title
              │
              ▼
    Mark item as added (visual feedback)
```

---

## Configuration

### User Settings
```json
{
    "MeetingPrep": {
        "IsEnabled": true,
        "AutoShowOnMeetingOpen": true,
        "EnableAiSuggestions": false,
        "ShowOverdueTasksMaxDays": 30,
        "ShowCompletedActionItems": false,
        "IncludeSurveyResponses": true,
        "SurveyLookbackDays": 30,
        "MaxItemsPerSection": 5
    }
}
```

---

## Implementation Plan

### Phase 1: Core Service (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create MeetingPrep data models | 2h | None |
| Create MeetingPrepService skeleton | 2h | Models |
| Implement PreviousMeetingGatherer | 3h | Service |
| Implement TaskDataGatherer | 3h | Service |
| Implement OkrKpiGatherer | 3h | Service |
| Implement PersonalDatesGatherer | 2h | Service |
| Implement SurveyDataGatherer | 3h | Service |
| Implement FeedbackGatherer | 2h | Service |
| Section compiler & prioritization | 3h | All gatherers |

### Phase 2: UI Components (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| PrepSectionControl | 4h | Models |
| PrepItemControl | 3h | Models |
| MeetingPrepPanel | 6h | Section/Item controls |
| MeetingPrepViewModel | 4h | MeetingPrepService |
| Integration with OneOnOneDialog | 3h | Panel |
| Add to Agenda functionality | 2h | ViewModel |
| AI suggestions (optional) | 4h | GeminiChatService |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Slow generation with large data | Medium | Parallel data gathering, caching |
| Action item extraction inaccurate | Low | Simple pattern matching, manual override |
| AI suggestions cost adds up | Low | Default off, only on-demand |

### Data Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Survey anonymity compromise | Critical | Never show individual responses if anonymous |
| Stale data if not re-generated | Low | Always generate fresh, no caching |

### UX Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Information overload | Medium | Section collapsing, priority ordering |
| Prep feels redundant if well-prepared | Low | Make optional, quick dismiss |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Prep generation time | <2s | Performance logging |
| Feature usage rate | >50% of meetings | Track "View Prep" clicks |
| Items added to agenda | >1 per meeting avg | Track "Add to Agenda" usage |
| Meeting prep time saved | 10+ min/meeting | User survey |

---

## Dependencies

- Existing: TrackerDbManager, OneOnOne model, GeminiChatService
- New: MeetingPrep models, MeetingPrepService
- UI: OneOnOneDialog modification

---

## Future Enhancements

1. **Prep Templates** - Save preferred prep structure
2. **Historical Prep** - View past meeting preps
3. **Collaborative Notes** - Prep items visible to both parties (not applicable - team members don't use app)
4. **Calendar Preview** - Show prep in calendar tooltip
5. **Email Summary** - Send prep as email before meeting

---

**Document End**
