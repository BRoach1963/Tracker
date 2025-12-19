# Feature Implementation Plan

## Status Overview

| Feature | Status | Complexity | Priority |
|---------|--------|------------|----------|
| Meeting History View | ✅ DONE | - | - |
| Action Item Rollover | ✅ DONE | - | - |
| Feedback History | 🔲 TODO | Medium | High |
| Individual Goals | 🔲 TODO | Medium | High |
| Meeting Templates | 🔲 TODO | Low | Medium |
| Quick Notes/Journal | 🔲 TODO | Low | Medium |
| Reminders/Notifications | 🔲 TODO | Medium | High |
| Search | 🔲 TODO | High | High |

---

## 1. Feedback History ⭐ Priority: High

**Purpose:** Track feedback given over time for performance reviews

### Data Model

```csharp
public class Feedback : AuditableEntity
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public TeamMember TeamMember { get; set; }
    
    public DateTime Date { get; set; }
    public FeedbackType Type { get; set; }  // Positive, Constructive, Recognition, Coaching
    public string Title { get; set; }
    public string Content { get; set; }
    public string Context { get; set; }     // Project, meeting, etc.
    
    // Optional link to 1:1 meeting where feedback was given
    public int? OneOnOneId { get; set; }
}

public enum FeedbackType
{
    Positive,
    Constructive,
    Recognition,
    Coaching,
    PerformanceReview
}
```

### Implementation Steps

1. **Create Data Model** (30 min)
   - `Feedback.cs` entity
   - `FeedbackType` enum
   - Add to `TrackerDbContext`

2. **Database Layer** (30 min)
   - Add `DbSet<Feedback>` to context
   - Configure entity in `OnModelCreating`
   - Add CRUD methods to `TrackerDbManager`

3. **ViewModel** (1 hr)
   - `FeedbackViewModel` for add/edit dialog
   - Add `FeedbackHistory` to `TeamMemberViewModel`

4. **UI Components** (2 hrs)
   - `FeedbackHistoryControl` - list view with filters
   - `AddFeedbackDialog` - add/edit feedback
   - Add to TeamMemberDialog (tab or section)

5. **Integration** (30 min)
   - Auto-populate from 1:1 meeting feedback field
   - Export for performance reviews

**Estimated Time: 4-5 hours**

---

## 2. Individual Goals ⭐ Priority: High

**Purpose:** Personal development goals beyond project OKRs

### Data Model

```csharp
public class IndividualGoal : AuditableEntity
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public TeamMember TeamMember { get; set; }
    
    public string Title { get; set; }
    public string Description { get; set; }
    public GoalCategory Category { get; set; }  // Career, Skill, Personal, Certification
    public GoalStatus Status { get; set; }      // NotStarted, InProgress, Completed, OnHold
    public DateTime? TargetDate { get; set; }
    public int ProgressPercent { get; set; }
    public string Notes { get; set; }
    
    // Milestones for tracking progress
    public List<GoalMilestone> Milestones { get; set; }
}

public class GoalMilestone : AuditableEntity
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public enum GoalCategory
{
    Career,
    SkillDevelopment,
    Certification,
    Leadership,
    Communication,
    Technical,
    Personal
}

public enum GoalStatus
{
    NotStarted,
    InProgress,
    Completed,
    OnHold,
    Cancelled
}
```

### Implementation Steps

1. **Create Data Models** (30 min)
   - `IndividualGoal.cs`, `GoalMilestone.cs`
   - `GoalCategory`, `GoalStatus` enums

2. **Database Layer** (45 min)
   - Add DbSets and configure relationships
   - Add CRUD methods

3. **ViewModel** (1.5 hrs)
   - `GoalViewModel` for add/edit
   - Goals collection in `TeamMemberViewModel`
   - Progress tracking logic

4. **UI Components** (2.5 hrs)
   - `GoalsControl` - list with progress bars
   - `AddGoalDialog` - add/edit with milestones
   - Goal progress visualization

5. **Integration** (30 min)
   - Link goals to 1:1 discussions
   - Add to dashboard summary

**Estimated Time: 5-6 hours**

---

## 3. Meeting Templates ⭐ Priority: Medium

**Purpose:** Reusable agenda templates for consistent meetings

### Data Model

```csharp
public class MeetingTemplate : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsDefault { get; set; }
    
    public List<TemplateAgendaItem> AgendaItems { get; set; }
}

public class TemplateAgendaItem : AuditableEntity
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string Description { get; set; }
    public AgendaItemCategory Category { get; set; }
    public Severity Priority { get; set; }
    public int SortOrder { get; set; }
}
```

### Implementation Steps

1. **Create Data Models** (20 min)
   - `MeetingTemplate.cs`, `TemplateAgendaItem.cs`

2. **Database Layer** (30 min)
   - Add DbSets and configure
   - CRUD methods

3. **ViewModel** (1 hr)
   - `MeetingTemplateViewModel`
   - Template selection in `OneOnOneViewModel`

4. **UI Components** (1.5 hrs)
   - Template manager in Settings
   - Template dropdown in Add 1:1 dialog
   - "Apply Template" button

5. **Seed Data** (20 min)
   - Create default templates (Weekly Check-in, Career Discussion, Performance Review)

**Estimated Time: 3-4 hours**

---

## 4. Quick Notes/Journal ⭐ Priority: Medium

**Purpose:** Ad-hoc notes about team members between formal meetings

### Data Model

```csharp
public class QuickNote : AuditableEntity
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public TeamMember TeamMember { get; set; }
    
    public DateTime Date { get; set; }
    public string Content { get; set; }
    public NoteType Type { get; set; }  // General, Observation, Reminder, Praise
    public bool IsPinned { get; set; }
    
    // Optional tags for organization
    public string Tags { get; set; }  // Comma-separated
}

public enum NoteType
{
    General,
    Observation,
    Reminder,
    Praise,
    Concern,
    Idea
}
```

### Implementation Steps

1. **Create Data Model** (15 min)
   - `QuickNote.cs`, `NoteType` enum

2. **Database Layer** (30 min)
   - Add DbSet and configure
   - CRUD methods with filtering

3. **ViewModel** (45 min)
   - `QuickNoteViewModel`
   - Notes collection in `TeamMemberViewModel`

4. **UI Components** (2 hrs)
   - `NotesPanel` - sticky-note style list
   - Quick-add inline form
   - Pin/unpin functionality
   - Filter by type/date

5. **Integration** (30 min)
   - Show recent notes in 1:1 prep
   - Add to dashboard

**Estimated Time: 3-4 hours**

---

## 5. Reminders/Notifications ⭐ Priority: High

**Purpose:** Alert when meetings are overdue or action items due

### Data Model

```csharp
public class Reminder : AuditableEntity
{
    public int Id { get; set; }
    public ReminderType Type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime TriggerDate { get; set; }
    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }
    
    // References
    public int? TeamMemberId { get; set; }
    public int? OneOnOneId { get; set; }
    public int? TaskId { get; set; }
}

public enum ReminderType
{
    MeetingOverdue,      // "Haven't met with X in Y days"
    TaskDue,             // "Task due tomorrow"
    TaskOverdue,         // "Task is overdue"
    GoalCheckIn,         // "Goal target date approaching"
    FollowUp             // Custom follow-up reminder
}
```

### Implementation Steps

1. **Create Data Model** (20 min)
   - `Reminder.cs`, `ReminderType` enum

2. **Database Layer** (30 min)
   - Add DbSet and configure
   - Methods to get active reminders

3. **Reminder Service** (2 hrs)
   - `ReminderService` - background checker
   - Generate reminders based on rules:
     - No 1:1 in X days (configurable)
     - Tasks due within X days
     - Goals approaching target date
   - Run on app startup and periodically

4. **ViewModel** (1 hr)
   - `ReminderViewModel`
   - `NotificationManager` integration

5. **UI Components** (2 hrs)
   - Notification bell icon in header
   - Notification dropdown/panel
   - Badge count
   - Toast notifications

6. **Settings** (30 min)
   - Configure reminder thresholds
   - Enable/disable notification types

**Estimated Time: 6-7 hours**

---

## 6. Search ⭐ Priority: High

**Purpose:** Find anything across meetings, tasks, notes, feedback

### Implementation Approach

Use SQLite FTS5 (Full-Text Search) or simple LIKE queries for MVP.

### Data Model

```csharp
// No new entity - search across existing entities
public class SearchResult
{
    public SearchResultType Type { get; set; }
    public int EntityId { get; set; }
    public string Title { get; set; }
    public string Snippet { get; set; }      // Highlighted match context
    public DateTime Date { get; set; }
    public string TeamMemberName { get; set; }
}

public enum SearchResultType
{
    OneOnOne,
    MeetingTask,
    AgendaItem,
    Feedback,
    Goal,
    QuickNote,
    IndividualTask,
    Project
}
```

### Implementation Steps

1. **Search Service** (2 hrs)
   - `SearchService` class
   - Search methods for each entity type
   - Combined search with ranking
   - Snippet generation with highlights

2. **Database Layer** (1 hr)
   - Optimized search queries
   - Consider FTS5 index for large datasets

3. **ViewModel** (1 hr)
   - `SearchViewModel`
   - Async search with debouncing
   - Result grouping by type

4. **UI Components** (2.5 hrs)
   - Global search box in header
   - Search results dropdown/panel
   - Keyboard shortcut (Ctrl+K)
   - Filter by type, date range, team member
   - Click to navigate to result

5. **Integration** (30 min)
   - Add to main window header
   - Highlight search terms in results

**Estimated Time: 7-8 hours**

---

## Implementation Order (Recommended)

### Phase 1: Core Features (Week 1)
1. **Feedback History** (4-5 hrs) - High value for performance reviews
2. **Individual Goals** (5-6 hrs) - Important for team development

### Phase 2: Productivity (Week 2)
3. **Reminders/Notifications** (6-7 hrs) - Keeps users engaged
4. **Meeting Templates** (3-4 hrs) - Saves time on recurring meetings

### Phase 3: Discovery (Week 3)
5. **Quick Notes/Journal** (3-4 hrs) - Low effort, good UX
6. **Search** (7-8 hrs) - Complex but very valuable

---

## Total Estimated Time

| Phase | Features | Hours |
|-------|----------|-------|
| Phase 1 | Feedback + Goals | 9-11 hrs |
| Phase 2 | Reminders + Templates | 9-11 hrs |
| Phase 3 | Notes + Search | 10-12 hrs |
| **Total** | All features | **28-34 hrs** |

---

## Quick Wins (Can Add Immediately)

These require minimal code changes:

1. **Meeting frequency indicator** - Show days since last 1:1 on team member card
2. **Task count badges** - Show open task count per team member
3. **Export to PDF** - Generate meeting summary reports
4. **Keyboard shortcuts** - Ctrl+N for new meeting, Ctrl+T for new task

---

## Architecture Notes

### Shared Components to Create
- `TagInput` control for tagging items
- `SearchBox` control with autocomplete
- `NotificationBell` control for header
- `ProgressIndicator` control for goals

### Database Considerations
- Add indexes on frequently searched columns
- Consider SQLite FTS5 for search
- Migration strategy for new tables

### Settings to Add
- Reminder thresholds (days before alert)
- Default meeting template
- Notification preferences
- Search result limits

