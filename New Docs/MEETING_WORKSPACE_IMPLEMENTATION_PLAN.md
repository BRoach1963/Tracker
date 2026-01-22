# Meeting Creation + Agenda Preparation Workspace

## Implementation Plan

**Created:** January 22, 2026  
**Status:** Planning  
**Project:** ProCohere Avalonia

---

## Executive Summary

This document outlines the implementation of the Meeting Creation + Agenda Preparation workspace. The core principle is that **agenda items are first-class discussion artifacts** that can:

- Be generated from existing work (tasks, goals, metrics, feedback)
- Spawn new work (tasks, goals, metrics, feedback)

**Non-negotiable:** Agenda items must never degrade into "just text notes."

---

## Core Product Principles

| Principle | Description |
|-----------|-------------|
| **Continuous Flow** | Meeting creation and agenda preparation must feel like one continuous flow |
| **No Save-Reopen Dance** | Users must not feel forced to "create → save → reopen" to prepare an agenda |
| **Connectors, Not Notes** | Agenda items are connectors to real work, not freeform notes |
| **Same Model, Different Lens** | Same meeting data model across Me / Circle; only the lens differs |
| **Personal-First in Me** | In Me context, agenda creation is personal-first by default |

---

## Me Meeting Flyout: Purpose & Scope

### The Anchor Concept

The Me meeting flyout is **not**:
- A meeting editor
- A meeting transcript
- A team coordination view
- The calendar

**Its job is:** "Help me personally prepare for and follow through on this meeting."

Think of it as: **"My meeting inbox"** — where meetings show up as work that requires my attention.

### What Belongs in Me Flyout

| Category | Items | Status |
|----------|-------|--------|
| **Keep (Strong Yes)** | Prep tab as default | ✅ |
| | My Prep Items list | ✅ |
| | Prep status indicator ("No prep yet" → "Prep in progress") | ✅ |
| | Add Prep Item | ✅ |
| **Add Soon** | Add prep item from existing task/goal/metric | Phase 5 |
| | Subtle indication when prep item spawned a task | Phase 8 |
| | Carry-forward affordance | Phase 8 |
| | Post-meeting outcomes (lightweight) | Phase 8 |
| **De-emphasize/Move** | Full team-wide agenda | → Meeting Workspace |
| | People management | → Meeting Workspace |
| | Rich meeting admin | → Meeting Workspace |
| | Team notes | → Meeting Workspace |

### Edit Meeting De-emphasis

**Problem:** "Edit Meeting" visually competes with Prep actions. In Me context, editing meeting logistics is secondary; Prep is primary.

**Solution Options (pick one):**

1. **Secondary button styling** (muted colors, smaller)
2. **Kebab menu only** — Edit lives in context menu
3. **Conditional prominence** — Only show prominently if:
   - User is organizer, AND
   - Meeting is > 2 hours away

**Recommended:** Option 2 (kebab menu) for cleanest experience.

```
┌──────────────────────────────────────────────────────┐
│ 1:1 with Sarah Chen                              [⋮] │  ← Kebab contains "Edit Meeting"
│ Today, 2:00 PM · 30 min                              │
├──────────────────────────────────────────────────────┤
│ [Prep] [Attendees]                                   │
├──────────────────────────────────────────────────────┤
```

### Prep Items Must Show Connections

**Risk:** If prep items look like plain checkboxes, they degrade into "personal notes with a better name."

**Solution:** Visual indicators showing linkage to real work.

```
┌──────────────────────────────────────────────────────┐
│ MY PREP ITEMS                                    [+] │
├──────────────────────────────────────────────────────┤
│ ○ Discuss blocked auth bug                           │
│   ☐ → Task: "Fix auth bug"                          │  ← Linked indicator
├──────────────────────────────────────────────────────┤
│ ○ Review Q1 goal progress                            │
│   🎯 → Goal: "Increase NPS by 10"                   │  ← Linked indicator
├──────────────────────────────────────────────────────┤
│ ○ Ask about vacation schedule                        │
│   (no link)                                          │  ← Freeform is fine too
└──────────────────────────────────────────────────────┘
```

**Visual Pattern:**
| Linkage | Display |
|---------|---------|
| Linked to Task | `☐ → Task: "title"` (muted, smaller text) |
| Linked to Goal | `🎯 → Goal: "title"` |
| Linked to Metric | `📊 → Metric: "title"` |
| Spawned a Task | `✓ Created: "task title"` (after meeting) |
| No link | No indicator (freeform is valid) |

### Post-Meeting Outcomes (Lightweight)

The flyout is pre-meeting focused, but must acknowledge outcomes exist. After the meeting:

```
┌──────────────────────────────────────────────────────┐
│ 1:1 with Sarah Chen (Past)                       [⋮] │
│ Yesterday, 2:00 PM · 30 min                          │
├──────────────────────────────────────────────────────┤
│ [Outcomes] [Prep]                                    │  ← Outcomes becomes primary tab
├──────────────────────────────────────────────────────┤
│ OUTCOMES                                             │
├──────────────────────────────────────────────────────┤
│ ✓ Discussed blocked auth bug                         │
│   → Created task: "Unblock auth with DevOps"        │
├──────────────────────────────────────────────────────┤
│ ✓ Reviewed Q1 goal progress                          │
│   (discussed, no action)                             │
├──────────────────────────────────────────────────────┤
│ ↪ Ask about vacation schedule                        │
│   Carried forward to next 1:1                        │
└──────────────────────────────────────────────────────┘
```

**Tab Logic:**
| Meeting State | Default Tab | Available Tabs |
|---------------|-------------|----------------|
| Future (> 2 hrs) | Prep | Prep, Attendees |
| Soon (< 2 hrs) | Prep | Prep, Attendees |
| Past (< 24 hrs) | Outcomes | Outcomes, Prep |
| Past (> 24 hrs) | Outcomes | Outcomes, Prep |

### Questions This Flyout Answers

**Before the meeting:**
- What do I need to think about?
- What do I need to bring up?
- What existing work needs discussion?

**After the meeting (lightweight):**
- What did I commit to?
- What needs to be carried forward?
- What turned into real work?

---

## Styling Constraints (Non-Negotiable)

- ✅ Use existing theme resources only
- ✅ Preserve existing spacing, typography, button styles
- ✅ Agenda panel must visually match existing flyouts
- ❌ No new color palette
- ❌ No bright badges ("skittles")
- ❌ No character personas

---

## Phase 1: Data Model & Infrastructure

### 1.1 Agenda Item Model

**File:** `Models/AgendaItem.cs`

```csharp
public class AgendaItem : BaseModel
{
    // Core Properties
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("meeting_id")]
    public Guid MeetingId { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("sort_order")]
    public int SortOrder { get; set; }
    
    [Column("is_private")]
    public bool IsPrivate { get; set; } // Personal prep flag
    
    [Column("status")]
    public string Status { get; set; } = "open"; // open, discussed, converted, carried_forward
    
    [Column("carry_forward_from_meeting_id")]
    public Guid? CarryForwardFromMeetingId { get; set; }
    
    // Source Tracking (where this came from)
    [Column("source_type")]
    public string? SourceType { get; set; } // manual, task, goal, metric, feedback, template, ai_suggestion
    
    [Column("source_id")]
    public Guid? SourceId { get; set; }
    
    [Column("source_snapshot")]
    public string? SourceSnapshot { get; set; } // JSON - captures state at time of linking
    
    // Metadata
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
```

### 1.2 Agenda Source Type Enum

**File:** `Models/AgendaSourceType.cs`

```csharp
public enum AgendaSourceType
{
    Manual,      // Freeform user entry
    Task,        // Linked from existing task
    Goal,        // Linked from existing goal
    Metric,      // Linked from existing metric
    Feedback,    // Linked from existing feedback
    Template,    // Generated from scaffold
    AISuggestion // Generated by AI
}
```

### 1.3 Agenda Status Enum

**File:** `Models/AgendaStatus.cs`

```csharp
public enum AgendaStatus
{
    Open,           // Not yet discussed
    Discussed,      // Discussed in meeting
    Converted,      // Converted to task/goal/etc
    CarriedForward  // Moved to next meeting
}
```

### 1.4 Agenda Outcome Link Model

**File:** `Models/AgendaOutcomeLink.cs`

Tracks work spawned FROM an agenda item.

```csharp
public class AgendaOutcomeLink : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("agenda_item_id")]
    public Guid AgendaItemId { get; set; }
    
    [Column("linked_entity_type")]
    public string LinkedEntityType { get; set; } = string.Empty; // task, goal, metric, feedback
    
    [Column("linked_entity_id")]
    public Guid LinkedEntityId { get; set; }
    
    [Column("link_type")]
    public string LinkType { get; set; } = string.Empty; // created_from, linked_to
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### 1.5 AI Suggested Agenda Item

**File:** `Models/AISuggestedAgendaItem.cs`

```csharp
public class AISuggestedAgendaItem
{
    public string Title { get; set; } = string.Empty;
    public string? Rationale { get; set; }
    public AgendaSourceType? SuggestedSourceType { get; set; }
    public Guid? SuggestedSourceId { get; set; }
    public double Confidence { get; set; } // Internal scoring
}
```

### 1.6 Meeting Draft Support

Add to existing `MeetingDetail.cs`:

```csharp
[Column("is_draft")]
public bool IsDraft { get; set; } = true;

// Draft auto-created when:
// - Title has 3+ characters, OR
// - At least one attendee selected
```

### 1.7 Repository Layer

**Files to create:**
- `Services/Data/Repositories/AgendaItemRepository.cs`
- `Services/Data/Repositories/AgendaOutcomeLinkRepository.cs`

**Update:**
- `Services/Data/Repositories/MeetingRepository.cs` - Add draft handling

---

## Phase 2: Meeting Workspace View Structure

### 2.1 Layout Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Meeting Workspace                             │
├────────────────────────────────┬────────────────────────────────┤
│                                │                                 │
│   LEFT PANEL (60%)             │   RIGHT PANEL (40%)            │
│   Meeting Details              │   Agenda Workspace             │
│                                │                                 │
│   ┌──────────────────────┐    │   ┌────────────────────────┐   │
│   │ Title                │    │   │ AGENDA (N items)   [+] │   │
│   └──────────────────────┘    │   ├────────────────────────┤   │
│                                │   │                        │   │
│   ┌──────────────────────┐    │   │  Agenda Item 1         │   │
│   │ Date/Time/Duration   │    │   │  🔗 Task: "Fix bug"    │   │
│   └──────────────────────┘    │   │                        │   │
│                                │   │  Agenda Item 2         │   │
│   ┌──────────────────────┐    │   │  🎯 Goal: "NPS +10"    │   │
│   │ Meeting Type ▼       │    │   │                        │   │
│   └──────────────────────┘    │   │  Agenda Item 3         │   │
│                                │   │  📋 Template           │   │
│   ┌──────────────────────┐    │   │                        │   │
│   │ Attendees            │    │   ├────────────────────────┤   │
│   │ [Avatar] [Avatar]    │    │   │ [Structure] [Prepare]  │   │
│   └──────────────────────┘    │   └────────────────────────┘   │
│                                │                                 │
│   ┌──────────────────────┐    │                                 │
│   │ Location (optional)  │    │                                 │
│   └──────────────────────┘    │                                 │
│                                │                                 │
└────────────────────────────────┴────────────────────────────────┘
```

### 2.2 Files to Create

```
Views/
├── MeetingWorkspaceView.axaml
├── MeetingWorkspaceView.axaml.cs

ViewModels/
├── MeetingWorkspaceViewModel.cs
├── AgendaItemViewModel.cs
```

### 2.3 Navigation Entry Points

| Entry Point | Action |
|-------------|--------|
| Me View "New Meeting" button | Opens empty workspace |
| Circle View "New Meeting" button | Opens empty workspace |
| Click existing meeting card | Opens workspace with meeting loaded |
| Meeting card context menu → Edit | Opens workspace with meeting loaded |

---

## Phase 3: Meeting Details Panel (Left)

### 3.1 Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Title | TextBox | Yes (to finalize) | Inline edit, large font |
| Date | DatePicker | Yes (to finalize) | |
| Time | TimePicker | Yes (to finalize) | |
| Duration | ComboBox | No | 15m, 30m, 45m, 1h, 1.5h, 2h |
| Meeting Type | ComboBox | No | 1:1, Team, Review, Planning (existing enum) |
| Attendees | Multi-select | Yes for non-personal | Use existing TeamMember picker |
| Location | TextBox | No | Optional |
| Organizer | Badge | Read-only | Shows current user role |

### 3.2 Auto-Draft Behavior

```csharp
// Debounced persistence (500ms after changes)
private async Task AutoSaveAsync()
{
    if (!_isDirty) return;
    
    // Create draft if doesn't exist
    if (Meeting.Id == Guid.Empty && HasMinimalData())
    {
        Meeting.Id = Guid.NewGuid();
        Meeting.IsDraft = true;
        await _meetingRepository.CreateAsync(Meeting);
    }
    else if (Meeting.Id != Guid.Empty)
    {
        await _meetingRepository.UpdateAsync(Meeting);
    }
    
    _isDirty = false;
}

private bool HasMinimalData()
{
    return (Title?.Length >= 3) || (Attendees.Count > 0);
}
```

### 3.3 Draft Indicator

Show subtle "Draft" badge until meeting is finalized.

---

## Phase 4: Agenda Workspace Panel (Right)

### 4.1 Empty State

```
┌──────────────────────────────────┐
│                                  │
│   No agenda yet.                 │
│                                  │
│   Add items manually, start      │
│   with a simple structure, or    │
│   get help preparing.            │
│                                  │
│   [+ Add agenda item]            │
│   [☰ Use suggested structure]    │
│   [✨ Help me prepare]           │
│                                  │
└──────────────────────────────────┘
```

### 4.2 Agenda List (With Items)

```
┌──────────────────────────────────┐
│ AGENDA (3 items)            [+]  │
├──────────────────────────────────┤
│ ⚫ Discuss blocked task          │
│   🔗 Task: "Fix auth bug"        │
│   🔒 Private                     │
├──────────────────────────────────┤
│ ⚫ Review Q1 goal progress       │
│   🎯 Goal: "Increase NPS"        │
├──────────────────────────────────┤
│ ○ General updates                │
│   📋 Template suggestion         │
└──────────────────────────────────┘
│ [☰ Structure] [✨ Prepare]       │
└──────────────────────────────────┘
```

### 4.3 Visual Indicators

| Indicator | Icon | Meaning |
|-----------|------|---------|
| Source: Task | ☐ (checkbox) | Linked from task |
| Source: Goal | 🎯 (target) | Linked from goal |
| Source: Metric | 📊 (chart) | Linked from metric |
| Source: Feedback | 💬 (comment) | Linked from feedback |
| Source: Template | 📋 (list) | Generated from scaffold |
| Source: AI | ✨ (sparkle) | AI suggestion |
| Private | 🔒 (lock) | Personal prep only |
| Carry-forward | ↪️ (arrow) | From previous meeting |
| Status: Open | ○ (empty circle) | Not discussed |
| Status: Discussed | ⚫ (filled circle) | Discussed |
| Status: Converted | ✓ (checkmark) | Spawned work |

### 4.4 Linkage Display in Me Flyout

Prep items in the Me flyout must visually show their connection to real work. This prevents them from degrading into "personal notes with a better name."

**Pattern: Inline linkage indicator (muted, below title)**

```
┌──────────────────────────────────────────────────────┐
│ ○ Discuss blocked auth bug                           │
│   ☐ → Task: "Fix auth bug"                    [Due] │
├──────────────────────────────────────────────────────┤
│ ○ Review Q1 goal progress                            │
│   🎯 → Goal: "Increase NPS by 10"             [45%] │
├──────────────────────────────────────────────────────┤
│ ○ Ask about vacation schedule                        │
│   (freeform - no link)                               │
└──────────────────────────────────────────────────────┘
```

**Linkage indicator styling:**
- Font size: 11px (smaller than title)
- Color: `BrushTextTertiary` (muted)
- Icon + arrow + entity type + title snippet
- Optional metadata badge (due date, progress %)

**Implementation in PrepItemViewModel:**

```csharp
public class PrepItemViewModel : ViewModelBase
{
    // ... existing properties
    
    public bool HasSourceLink => SourceType != null && SourceId != null;
    
    public string SourceLinkIcon => SourceType switch
    {
        "task" => "☐",
        "goal" => "🎯", 
        "metric" => "📊",
        "feedback" => "💬",
        _ => null
    };
    
    public string SourceLinkDisplay => SourceType switch
    {
        "task" => $"→ Task: \"{SourceTitle}\"",
        "goal" => $"→ Goal: \"{SourceTitle}\"",
        "metric" => $"→ Metric: \"{SourceTitle}\"",
        "feedback" => $"→ Feedback: \"{SourceTitle}\"",
        _ => null
    };
    
    public string SourceMetadataBadge { get; set; } // e.g., "Due Jan 25" or "45%"
}

---

## Phase 5: Add Agenda Item Flow

### 5.1 Chooser Dialog

```
┌─────────────────────────────┐
│ Add agenda item             │
├─────────────────────────────┤
│                             │
│   ○ Freeform                │
│     Create a custom item    │
│                             │
│   ○ From existing work...   │
│     Link to task, goal, etc │
│                             │
└─────────────────────────────┘
```

### 5.2 Freeform Mode

```
┌─────────────────────────────┐
│ Add freeform item           │
├─────────────────────────────┤
│                             │
│ Title *                     │
│ ┌─────────────────────────┐ │
│ │                         │ │
│ └─────────────────────────┘ │
│                             │
│ Description (optional)      │
│ ┌─────────────────────────┐ │
│ │                         │ │
│ │                         │ │
│ └─────────────────────────┘ │
│                             │
│ [✓] Private (only I see)    │
│     Default ON in Me context│
│                             │
│ [Cancel]        [Add item]  │
└─────────────────────────────┘
```

### 5.3 From Existing Mode (Tabbed Picker)

```
┌─────────────────────────────┐
│ Link from existing          │
├─────────────────────────────┤
│ [Tasks] [Goals] [Metrics]   │
│ [Feedback]                  │
├─────────────────────────────┤
│                             │
│ ☐ Fix authentication bug    │
│   Due: Jan 25 · High        │
│                             │
│ ☐ Update dashboard layout   │
│   Due: Jan 30 · Medium      │
│                             │
│ ☐ Review pull request #42   │
│   Due: Jan 23 · Low         │
│                             │
├─────────────────────────────┤
│ [Cancel]     [Add selected] │
└─────────────────────────────┘
```

### 5.4 Source Linking Logic

When linking from existing entity:

```csharp
var agendaItem = new AgendaItem
{
    Id = Guid.NewGuid(),
    MeetingId = _meeting.Id,
    Title = GenerateTitleFromEntity(entity),
    SourceType = entity switch
    {
        TaskDetail => "task",
        GoalDetail => "goal",
        MetricDetail => "metric",
        FeedbackDetail => "feedback",
        _ => "manual"
    },
    SourceId = entity.Id,
    SourceSnapshot = JsonSerializer.Serialize(new
    {
        entity.Title,
        Status = entity.Status,
        DueDate = (entity as TaskDetail)?.DueDate,
        Value = (entity as MetricDetail)?.CurrentValue
    }),
    IsPrivate = IsInMeContext,
    SortOrder = AgendaItems.Count,
    CreatedBy = _currentUserId
};
```

---

## Phase 6: Suggested Structure (Template Scaffold)

### 6.1 Template Definitions

```csharp
public static class AgendaTemplates
{
    public static List<AgendaTemplateSection> GetSections(MeetingType type) => type switch
    {
        MeetingType.OneOnOne => new()
        {
            new("Wins & highlights", "What's going well? Celebrate successes."),
            new("Blockers & challenges", "What's getting in the way?"),
            new("Goals & metrics check", "Progress on key objectives."),
            new("Feedback", "Give and receive feedback."),
            new("Next actions", "What needs to happen next?")
        },
        
        MeetingType.Team => new()
        {
            new("Updates", "What's new since last meeting?"),
            new("Risks & blockers", "Issues requiring attention."),
            new("Decisions needed", "Items requiring group decision."),
            new("Goal progress", "Status on team objectives."),
            new("Follow-ups", "Action items and owners.")
        },
        
        MeetingType.Review => new()
        {
            new("Targets recap", "What were we trying to achieve?"),
            new("What's working", "Successes and wins."),
            new("What's not working", "Gaps and challenges."),
            new("Decisions", "Changes or actions to take."),
            new("Action items", "Next steps with owners.")
        },
        
        MeetingType.Planning => new()
        {
            new("Objectives", "What are we trying to accomplish?"),
            new("Constraints", "Limitations and boundaries."),
            new("Options to consider", "Possible approaches."),
            new("Plan & owners", "Decisions and assignments.")
        },
        
        _ => new()
        {
            new("Discussion points", "Topics to cover."),
            new("Decisions", "Items requiring decision."),
            new("Action items", "Next steps.")
        }
    };
}

public record AgendaTemplateSection(string Title, string Description);
```

### 6.2 Confirmation Flow

```
┌─────────────────────────────┐
│ Add suggested structure?    │
├─────────────────────────────┤
│                             │
│ This will add 5 sections    │
│ for a 1:1 meeting:          │
│                             │
│ • Wins & highlights         │
│ • Blockers & challenges     │
│ • Goals & metrics check     │
│ • Feedback                  │
│ • Next actions              │
│                             │
│ You can edit or remove      │
│ any section afterward.      │
│                             │
│ [Cancel]     [Add sections] │
└─────────────────────────────┘
```

### 6.3 Enablement Logic

```csharp
public bool CanUseScaffold => Meeting.Type != null;
```

### 6.4 Created Items Behavior

- `SourceType = Template`
- Subtle visual styling (lighter text opacity)
- Show small "Suggested" label
- Fully editable and deletable

---

## Phase 7: AI Agenda Generation

### 7.1 Enablement Logic

```csharp
public bool CanUseAI => 
    Meeting.Type != null && 
    Meeting.Attendees.Count > 0;
```

If not enabled, show helper text:
> "Select a meeting type and add attendees to get AI suggestions."

### 7.2 AI Service Contract

**File:** `Services/IAgendaAIService.cs`

```csharp
public interface IAgendaAIService
{
    Task<List<AISuggestedAgendaItem>> GenerateSuggestionsAsync(
        MeetingDetail meeting,
        List<TaskDetail> relevantTasks,
        List<GoalDetail> relevantGoals,
        List<MetricDetail> relevantMetrics,
        List<FeedbackDetail> recentFeedback,
        CancellationToken ct = default);
}
```

### 7.3 AI Response Format

```csharp
public class AISuggestedAgendaItem
{
    public string Title { get; set; } = string.Empty;
    public string? Rationale { get; set; } // "Why this matters"
    public AgendaSourceType? SuggestedSourceType { get; set; }
    public Guid? SuggestedSourceId { get; set; }
    public double Confidence { get; set; } // 0.0 - 1.0, internal only
}
```

### 7.4 Review UI

```
┌─────────────────────────────┐
│ Suggested agenda items      │
├─────────────────────────────┤
│                             │
│ [+] Review blocked task     │
│     "Auth bug is overdue    │
│      and blocking release"  │
│     [Edit] [Dismiss]        │
│                             │
│ [+] Check Q1 goal progress  │
│     "Goal at 45% with 2     │
│      weeks remaining"       │
│     [Edit] [Dismiss]        │
│                             │
│ [+] Discuss team feedback   │
│     "Recent feedback about  │
│      communication"         │
│     [Edit] [Dismiss]        │
│                             │
├─────────────────────────────┤
│ [Add all]    [Add selected] │
└─────────────────────────────┘
```

### 7.5 Stub Implementation

```csharp
public class AgendaAIService : IAgendaAIService
{
    public async Task<List<AISuggestedAgendaItem>> GenerateSuggestionsAsync(
        MeetingDetail meeting,
        List<TaskDetail> tasks,
        List<GoalDetail> goals,
        List<MetricDetail> metrics,
        List<FeedbackDetail> feedback,
        CancellationToken ct = default)
    {
        // Stub: Return suggestions based on meeting type
        var suggestions = new List<AISuggestedAgendaItem>();
        
        // Add overdue/blocked tasks
        foreach (var task in tasks.Where(t => t.IsOverdue || t.Status == "blocked").Take(2))
        {
            suggestions.Add(new AISuggestedAgendaItem
            {
                Title = $"Discuss: {task.Title}",
                Rationale = task.IsOverdue ? "This task is overdue" : "This task is blocked",
                SuggestedSourceType = AgendaSourceType.Task,
                SuggestedSourceId = task.Id,
                Confidence = 0.9
            });
        }
        
        // Add goals needing attention
        foreach (var goal in goals.Where(g => g.Progress < 50).Take(2))
        {
            suggestions.Add(new AISuggestedAgendaItem
            {
                Title = $"Review progress: {goal.Title}",
                Rationale = $"Currently at {goal.Progress}%",
                SuggestedSourceType = AgendaSourceType.Goal,
                SuggestedSourceId = goal.Id,
                Confidence = 0.8
            });
        }
        
        return suggestions;
    }
}
```

### 7.6 AI Integration Notes

- Default added items to `IsPrivate = true`
- If AI can't confidently link to an entity, create as freeform
- Never invent links - only suggest if entity exists
- Hook ready for real AI integration (OpenAI, etc.)

---

## Phase 8: Agenda Item Actions (Outcomes)

### 8.1 Item Context Menu

```
┌─────────────────────────────┐
│ ✏️  Edit                     │
├─────────────────────────────┤
│ ➕ Create task from this    │
│ 🔗 Link to existing task    │
├─────────────────────────────┤
│ ✓  Mark as discussed        │
│ ↪️  Carry forward            │
├─────────────────────────────┤
│ 🗑️  Delete                   │
└─────────────────────────────┘
```

### 8.2 Create Task from Agenda Item

**Flow:**
1. Click "Create task from this"
2. Task creation dialog opens (use existing pattern)
3. Title pre-filled from agenda item title
4. On save:
   - Create task
   - Create `AgendaOutcomeLink` with `LinkType = created_from`
   - Update agenda item status to `Converted`
   - Show linked task indicator on agenda item

```csharp
public async Task CreateTaskFromAgendaItemAsync(AgendaItem agendaItem)
{
    var task = await _taskCreationDialog.ShowAsync(new TaskCreationContext
    {
        PrefilledTitle = agendaItem.Title,
        PrefilledDescription = agendaItem.Description,
        SourceMeetingId = agendaItem.MeetingId
    });
    
    if (task != null)
    {
        // Create outcome link
        var link = new AgendaOutcomeLink
        {
            Id = Guid.NewGuid(),
            AgendaItemId = agendaItem.Id,
            LinkedEntityType = "task",
            LinkedEntityId = task.Id,
            LinkType = "created_from"
        };
        await _agendaOutcomeLinkRepository.CreateAsync(link);
        
        // Update agenda item
        agendaItem.Status = "converted";
        await _agendaItemRepository.UpdateAsync(agendaItem);
    }
}
```

### 8.3 Link to Existing Task

**Flow:**
1. Click "Link to existing task"
2. Task picker dialog opens
3. User selects task
4. Create `AgendaOutcomeLink` with `LinkType = linked_to`
5. Show linked task indicator on agenda item

### 8.4 Carry Forward

**Flow:**
1. Click "Carry forward"
2. Set `Status = carried_forward`
3. Store current meeting ID for reference

**On next 1:1 creation with same attendee:**
- Check for carried-forward items
- Offer to import them: "You have 2 items carried forward from your last 1:1. Import them?"

```csharp
public async Task CarryForwardAsync(AgendaItem item)
{
    item.Status = "carried_forward";
    await _agendaItemRepository.UpdateAsync(item);
    
    // Visual feedback
    ShowToast("Item will carry forward to your next meeting with this person.");
}

public async Task<List<AgendaItem>> GetCarriedForwardItemsAsync(
    Guid attendeeId, 
    Guid excludeMeetingId)
{
    return await _agendaItemRepository.GetByAttendeeWithStatusAsync(
        attendeeId, 
        "carried_forward",
        excludeMeetingId);
}
```

### 8.5 Post-Meeting Outcomes View (Me Flyout)

After the meeting, the Me flyout shifts to show outcomes. This is a **lightweight** view — not a full meeting transcript.

**Outcomes Tab Content:**

```
┌──────────────────────────────────────────────────────┐
│ OUTCOMES                                             │
├──────────────────────────────────────────────────────┤
│ ✓ Discussed blocked auth bug                         │
│   → Created: "Unblock auth with DevOps"         [→] │  ← Click to open task
├──────────────────────────────────────────────────────┤
│ ✓ Reviewed Q1 goal progress                          │
│   (discussed, no action)                             │
├──────────────────────────────────────────────────────┤
│ ↪ Ask about vacation schedule                        │
│   Carried forward to next 1:1                        │
├──────────────────────────────────────────────────────┤
│ — Ask about team offsite                             │
│   (not discussed)                                    │
└──────────────────────────────────────────────────────┘
```

**Outcome States:**

| State | Icon | Meaning |
|-------|------|---------|
| Discussed | ✓ | Was discussed in meeting |
| Discussed + Created | ✓ + link | Spawned a task/goal/etc |
| Carried Forward | ↪ | Moved to next meeting |
| Not Discussed | — | Didn't get to it |

**Outcome Actions Available:**

| Action | When Available |
|--------|----------------|
| Mark as Discussed | Always (if not already) |
| Create Task from This | If not already converted |
| Carry Forward | If not discussed |
| Link to Existing | Always |

**ViewModel Support:**

```csharp
public class PrepItemViewModel : ViewModelBase
{
    // ... existing properties
    
    // Outcome-specific
    public bool IsDiscussed => Status == "discussed" || Status == "converted";
    public bool HasSpawnedWork => OutcomeLinks.Any();
    public string SpawnedWorkDisplay => HasSpawnedWork 
        ? $"→ Created: \"{OutcomeLinks.First().Title}\""
        : null;
    public bool IsCarriedForward => Status == "carried_forward";
    public bool WasNotDiscussed => Status == "open" && MeetingIsPast;
    
    public string OutcomeIcon => Status switch
    {
        "discussed" => "✓",
        "converted" => "✓",
        "carried_forward" => "↪",
        _ when MeetingIsPast => "—",
        _ => "○"
    };
}
```

---

## Phase 9: Agenda List Rendering

### 9.1 Sorting

| Context | Sort Order |
|---------|------------|
| Me View | Personal items first, then team items, then by sort order |
| Circle View | By sort order (manual drag) |

### 9.2 Drag-and-Drop Reorder

- Items reorderable via drag handle
- Update `SortOrder` on drop
- Persist immediately

### 9.3 Item Display Template

```xml
<DataTemplate x:DataType="vm:AgendaItemViewModel">
    <Border Classes="agenda-item" Margin="0,0,0,6">
        <Grid ColumnDefinitions="Auto,*,Auto">
            <!-- Status indicator -->
            <Border Grid.Column="0" Width="8" Height="8" CornerRadius="4"
                    Background="{Binding StatusColor}" 
                    VerticalAlignment="Center" Margin="0,0,10,0"/>
            
            <!-- Content -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="{Binding Title}" FontWeight="Medium"/>
                
                <!-- Source indicator -->
                <StackPanel Orientation="Horizontal" Spacing="6" Margin="0,2,0,0"
                            IsVisible="{Binding HasSource}">
                    <PathIcon Data="{Binding SourceIcon}" Width="12" Height="12"/>
                    <TextBlock Text="{Binding SourceDisplay}" FontSize="11" 
                               Foreground="{DynamicResource BrushTextTertiary}"/>
                </StackPanel>
            </StackPanel>
            
            <!-- Badges -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4">
                <PathIcon Data="{StaticResource LockIcon}" Width="12" Height="12"
                          IsVisible="{Binding IsPrivate}"
                          Foreground="{DynamicResource BrushTextTertiary}"/>
                <PathIcon Data="{StaticResource ArrowIcon}" Width="12" Height="12"
                          IsVisible="{Binding IsCarriedForward}"
                          Foreground="{DynamicResource BrushTextTertiary}"/>
            </StackPanel>
        </Grid>
    </Border>
</DataTemplate>
```

---

## Phase 10: Persistence & Exit Handling

### 10.1 Auto-Save Behavior

| Item | Save Trigger |
|------|--------------|
| Meeting details | Debounced 500ms after change |
| Agenda items | Immediate on add/edit/delete |
| Draft flag | Cleared on explicit Save or when all required fields complete |

### 10.2 Exit Handling

```csharp
public async Task<bool> CanCloseAsync()
{
    if (!IsDirty) return true;
    
    if (HasMeaningfulData()) // Title + agenda items, or attendees
    {
        var result = await _dialogService.ShowConfirmationAsync(
            "Save meeting?",
            "You have unsaved changes.",
            new[] { "Save", "Discard", "Cancel" });
        
        return result switch
        {
            "Save" => await SaveAndCloseAsync(),
            "Discard" => await DiscardDraftAsync(),
            _ => false // Cancel - stay on page
        };
    }
    
    // No meaningful data - discard quietly
    await DiscardDraftAsync();
    return true;
}

private bool HasMeaningfulData()
{
    return (Meeting.Title?.Length >= 3) || 
           (Meeting.Attendees.Count > 0) ||
           (AgendaItems.Count > 0);
}

private async Task<bool> DiscardDraftAsync()
{
    if (Meeting.IsDraft && Meeting.Id != Guid.Empty)
    {
        // Delete draft meeting and all agenda items
        await _meetingRepository.DeleteAsync(Meeting.Id);
    }
    return true;
}
```

---

## Phase 11: Integration Points

### 11.1 Me View Integration

**Me Meeting Flyout (existing, enhanced):**
- Default view for meeting cards in Me
- Personal prep focus — NOT meeting admin
- Edit Meeting in kebab menu (de-emphasized)
- Shows prep items with linkage indicators
- Post-meeting: switches to Outcomes tab

| Action | Behavior |
|--------|----------|
| Click meeting card | Opens flyout (prep-focused) |
| Kebab → Edit Meeting | Opens Meeting Workspace |
| Kebab → Open Full View | Opens Meeting Workspace |
| "New Meeting" button | Opens Meeting Workspace (creation mode) |

**Flyout Tabs by Meeting State:**
| State | Tabs | Default |
|-------|------|---------|
| Future | Prep, Attendees | Prep |
| Past | Outcomes, Prep | Outcomes |

### 11.2 Meeting Workspace (Full View)

The Meeting Workspace is the full editing experience — used for:
- Creating new meetings
- Editing meeting logistics (time, attendees, location)
- Managing team-wide agenda
- Rich meeting administration

**When to use Workspace vs Flyout:**
| Task | Use |
|------|-----|
| Personal prep | Flyout |
| Add my prep item | Flyout |
| Review my outcomes | Flyout |
| Edit meeting time/attendees | Workspace |
| Build team agenda | Workspace |
| Create new meeting | Workspace |

### 11.3 Circle View Integration

- Add "New Meeting" button in header
- Same workspace UI as Me
- Default `IsPrivate = false` for agenda items
- Team-wide agenda visibility by default
- No personal flyout (meetings in Circle are team-focused)

### 11.4 Existing Meeting Edit

- Load meeting + agenda items from database
- Same workspace UI
- Show "Edit Meeting" vs "New Meeting" in header
- All agenda functionality works identically

---

## File Structure

```
ProCohere.Avalonia/
├── Models/
│   ├── AgendaItem.cs                    # Core agenda item model
│   ├── AgendaOutcomeLink.cs             # Link to spawned work
│   ├── AgendaSourceType.cs              # Enum: manual, task, goal, etc.
│   ├── AgendaStatus.cs                  # Enum: open, discussed, etc.
│   └── AISuggestedAgendaItem.cs         # AI response model
│
├── ViewModels/
│   ├── MeetingWorkspaceViewModel.cs     # Main workspace VM
│   ├── AgendaItemViewModel.cs           # Single agenda item VM
│   ├── PrepItemViewModel.cs             # Enhanced for Me flyout (linkage + outcomes)
│   ├── AddAgendaItemDialogViewModel.cs  # Add item dialog VM
│   └── AISuggestionsViewModel.cs        # AI review panel VM
│
├── Views/
│   ├── MeetingWorkspaceView.axaml       # Main workspace view (full edit)
│   ├── MeetingWorkspaceView.axaml.cs
│   ├── MeView.axaml                     # Enhanced meeting flyout (existing)
│   ├── MeView.axaml.cs                  # Flyout panel building (existing)
│   └── Dialogs/
│       ├── AddAgendaItemDialog.axaml    # Freeform/picker chooser
│       ├── EntityPickerDialog.axaml     # Task/goal/metric picker
│       └── ScaffoldConfirmDialog.axaml  # Template confirmation
│
├── Services/
│   ├── IAgendaService.cs                # Agenda business logic interface
│   ├── AgendaService.cs                 # Agenda business logic
│   ├── IAgendaAIService.cs              # AI generation interface
│   └── AgendaAIService.cs               # AI generation (stub initially)
│
└── Services/Data/Repositories/
    ├── AgendaItemRepository.cs          # Agenda item CRUD
    └── AgendaOutcomeLinkRepository.cs   # Outcome link CRUD
```

### Me Flyout vs Meeting Workspace Files

| Component | Me Flyout (Personal Prep) | Meeting Workspace (Full Edit) |
|-----------|---------------------------|-------------------------------|
| View | `MeView.axaml` (flyout panel) | `MeetingWorkspaceView.axaml` |
| ViewModel | `MeViewModel` (existing) | `MeetingWorkspaceViewModel` |
| Prep Items | `PrepItemViewModel` | `AgendaItemViewModel` |
| Scope | My prep + outcomes | Team agenda + meeting admin |

---

## Implementation Order

| Phase | Description | Estimated Effort | Dependencies |
|-------|-------------|------------------|--------------|
| 1 | Data models + repository stubs | 2 hours | None |
| 2 | MeetingWorkspaceView shell (layout only) | 2 hours | Phase 1 |
| 3 | Meeting Details panel (left) | 3 hours | Phase 2 |
| 4 | Agenda panel empty state + list rendering | 2 hours | Phase 2 |
| 5 | Add Agenda Item (freeform + from existing) | 4 hours | Phase 4 |
| 6 | Template scaffold | 2 hours | Phase 5 |
| 7 | AI suggestions (stub) | 3 hours | Phase 5 |
| 8 | Agenda item actions (create task, link) | 4 hours | Phase 5 |
| 9 | Persistence + exit handling | 2 hours | Phase 3-8 |
| 10 | Navigation integration (Me/Circle) | 2 hours | Phase 9 |

**Total Estimated:** ~26 hours

---

## Database Schema (Supabase)

### agenda_items table

```sql
CREATE TABLE agenda_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    description TEXT,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_private BOOLEAN NOT NULL DEFAULT false,
    status TEXT NOT NULL DEFAULT 'open',
    carry_forward_from_meeting_id UUID REFERENCES meetings(id),
    
    -- Source tracking
    source_type TEXT, -- manual, task, goal, metric, feedback, template, ai_suggestion
    source_id UUID,
    source_snapshot JSONB,
    
    -- Metadata
    created_by UUID REFERENCES team_members(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES team_members(id)
);

-- RLS Policies
ALTER TABLE agenda_items ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view agenda items for meetings they attend"
    ON agenda_items FOR SELECT
    USING (
        meeting_id IN (
            SELECT meeting_id FROM meeting_attendees 
            WHERE team_member_id = current_team_member_id()
        )
        AND (NOT is_private OR created_by = current_team_member_id())
    );

CREATE POLICY "Users can create agenda items for meetings they attend"
    ON agenda_items FOR INSERT
    WITH CHECK (
        meeting_id IN (
            SELECT meeting_id FROM meeting_attendees 
            WHERE team_member_id = current_team_member_id()
        )
    );

CREATE POLICY "Users can update their own agenda items"
    ON agenda_items FOR UPDATE
    USING (created_by = current_team_member_id());

CREATE POLICY "Users can delete their own agenda items"
    ON agenda_items FOR DELETE
    USING (created_by = current_team_member_id());
```

### agenda_outcome_links table

```sql
CREATE TABLE agenda_outcome_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agenda_item_id UUID NOT NULL REFERENCES agenda_items(id) ON DELETE CASCADE,
    linked_entity_type TEXT NOT NULL, -- task, goal, metric, feedback
    linked_entity_id UUID NOT NULL,
    link_type TEXT NOT NULL, -- created_from, linked_to
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- RLS Policies
ALTER TABLE agenda_outcome_links ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view links for their agenda items"
    ON agenda_outcome_links FOR SELECT
    USING (
        agenda_item_id IN (
            SELECT id FROM agenda_items 
            WHERE created_by = current_team_member_id()
        )
    );

CREATE POLICY "Users can create links for their agenda items"
    ON agenda_outcome_links FOR INSERT
    WITH CHECK (
        agenda_item_id IN (
            SELECT id FROM agenda_items 
            WHERE created_by = current_team_member_id()
        )
    );
```

---

## Definition of Done

### Meeting Workspace (Full Edit)
- [ ] Meeting creation + agenda is one continuous experience
- [ ] Manual, scaffold, and AI agenda items coexist
- [ ] Agenda items can link to existing work (tasks, goals, metrics, feedback)
- [ ] Agenda items can spawn new work (tasks initially)
- [ ] Architecture supports future expansion without refactor
- [ ] Styling matches existing Pro Cohere patterns
- [ ] Works in both Me and Circle contexts
- [ ] Draft handling is seamless
- [ ] Exit handling prevents data loss

### Me Meeting Flyout (Personal Prep)
- [ ] "Edit Meeting" is de-emphasized (kebab menu)
- [ ] Prep tab is default for future meetings
- [ ] Prep items show linkage to source entities (task/goal/metric)
- [ ] Linkage indicators are subtle but clear (not competing with title)
- [ ] Outcomes tab is default for past meetings
- [ ] Outcomes show: discussed, spawned work, carried forward, not discussed
- [ ] Carry-forward creates item for next meeting with same attendee
- [ ] Flyout feels like "my meeting inbox" — not meeting admin

---

## Future Enhancements (Not in Scope)

- Agenda item templates (saved per meeting series)
- Recurring meeting series with automatic agenda carry-forward
- Real AI integration (OpenAI/Claude)
- Goal/metric/feedback creation from agenda items (only task initially)
- Agenda item voting/prioritization
- Collaborative real-time agenda editing
- Meeting summary generation
