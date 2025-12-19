# Tracker UI Redesign - Phase 3 Summary

## Completed Work (This Session)

### 1. Filtering System - All Pulse/Circle Pages ✅
- **Projects Page**: `ProjectStatusFilter`, `FilteredProjects`, `ApplyProjectFilters()`
- **Tasks Page**: `TaskStatusFilter`, `FilteredTasks`, `ApplyTaskFilters()`
- **Feedback Page**: `FeedbackSearchText`, `SelectedFeedbackFilterMember`, `FilteredFeedbacks`
- **Goals Page**: `GoalSearchText`, `SelectedGoalFilterMember`, `FilteredGoals`
- All stat cards are now clickable filters with visual feedback

### 2. Notes Redesign ✅
- **New `NoteLinkedEntityType` enum**: None, TeamMember, Project, OneOnOne, OKR, KeyResult, KPI, Task, Goal, Feedback
- **Updated `QuickNote` model**: Polymorphic linking via `LinkedEntityType` + `LinkedEntityId`
- **New properties**: `Title`, `DisplayTitle`, `LinkedToDisplay`, `HasLinkedEntity`, `CreatedDisplay`
- **Updated `QuickNotesControl`**: Master-detail layout with filter bar
- **Sample data**: 18 varied notes linked to different entity types

### 3. Bug Fixes ✅
- Fixed task statistics not displaying (Open/Overdue/Completed counts)
- Fixed binding errors for `FeedbackSearchText`, `GoalSearchText`, etc.
- Removed debug logging after fixes confirmed
- Fixed duplicate method definitions

---

## Pending Redesign Work

### 1. 1:1 Dialog - MAJOR REWRITE NEEDED

**Current Issues:**
- 4 tabs (Agenda Items, Notes/Agenda, Tasks, Linked Items) mostly empty
- Linked Items tab now redundant with polymorphic notes
- Team member lookup is clunky (button-based, not autocomplete)
- Styling inconsistent with new design patterns
- Workflow unclear

**Proposed Changes:**
- Single scrollable panel instead of tabs
- Remove Linked Items tab entirely
- AutoSuggest team member picker (custom control for DeepEndControls)
- Inline agenda item editing with categories
- Clear sections: Previous Meeting → Agenda → Notes → Action Items

**Agenda Items Value for Managers:**
- Categories: Discussion, Follow-up, Career, Feedback, Blocker, Review
- Status: To Discuss, Discussed, Deferred
- Optional link to related entity (task, goal, OKR, project)
- Rollover from previous meetings
- Notes per item

### 2. Team Members Page - NEEDS RETHINKING

**Current State:**
- Basic card grid with stats
- Clickable filter cards implemented
- Detail panel shows member info

**Potential Improvements:**
- Better visualization of team health
- 1:1 cadence tracking (on track, overdue)
- Task load balancing view
- Goal progress per member
- Quick actions (schedule 1:1, assign task, give feedback)

### 3. Team Member Dialog - STYLING INCONSISTENT

**Issues from Screenshot:**
- Different styling than other dialogs
- Social media fields taking up space (how useful?)
- Missing tabs for related data in edit mode

### 4. Feedback Page - NEEDS VALUE ASSESSMENT

**Questions:**
- What makes feedback tracking USEFUL for a manager?
- Should it tie to performance reviews?
- How does it relate to Goals?
- Visibility: Should team members see their feedback?

### 5. Goals Page - NEEDS VALUE ASSESSMENT

**Questions:**
- How do Goals differ from OKRs?
- Are these personal development goals vs team OKRs?
- Should Goals have milestones? (they do)
- How do Goals tie to career progression?

### 6. AutoSuggest Control - NEW FOR DeepEndControls

**Requirements:**
- Type-ahead filtering
- Dropdown shows matches
- Keyboard navigation (up/down/enter)
- Custom item template support
- No ModernWPF.UI (memory issues, bugs)

---

## Architecture Notes

### Current ViewModel Structure
- `TrackerMainViewModel` - Shared across most pages
- `DashboardViewModel` - Separate for home dashboard
- `OkrsViewModel` - Separate for OKR page
- `QuickNotesViewModel` - Separate for notes

### Data Flow
- All data owned by User via `UserId` shadow property
- Sample data seeded after user setup
- `TrackerDataManager` as central data access point

### Messaging Pattern - TODO: Implement CommunityToolkit.Mvvm Messenger
**Decision:** Use `CommunityToolkit.Mvvm` package for cross-ViewModel communication.

**Implementation Tasks:**
1. Add NuGet package: `CommunityToolkit.Mvvm`
2. Create message types (e.g., `DataChangedMessage`, `RefreshRequestMessage`)
3. Register ViewModels as recipients
4. Send messages when data changes (after dialog saves, etc.)

**Use Cases:**
- Dialog saves data → Main view refreshes
- Task completed → Dashboard updates
- 1:1 scheduled → Team member stats update
- Any CRUD operation that affects multiple views

**Pattern:**
```csharp
// Message definition
public record DataChangedMessage(PropertyChangedEnum ChangedProperty);

// Sending (in dialog ViewModel after save)
WeakReferenceMessenger.Default.Send(new DataChangedMessage(PropertyChangedEnum.Tasks));

// Receiving (in main ViewModel)
public class TrackerMainViewModel : ObservableRecipient, IRecipient<DataChangedMessage>
{
    public void Receive(DataChangedMessage message)
    {
        if (message.ChangedProperty == PropertyChangedEnum.Tasks)
            _ = RefreshTasksAsync();
    }
}
```

---

## Files Changed This Session

### New Files
- `Tracker/Common/Enums/NoteLinkedEntityType.cs`

### Modified Files
- `Tracker/DataModels/QuickNote.cs` - Polymorphic linking
- `Tracker/Database/TrackerDbContext.cs` - QuickNote config
- `Tracker/Database/DatabaseSeeder.cs` - Sample notes
- `Tracker/ViewModels/QuickNotesViewModel.cs` - Full rewrite
- `Tracker/Controls/QuickNotesControl.xaml` - Master-detail layout
- `Tracker/Controls/QuickNotesControl.xaml.cs` - Updated
- `Tracker/ViewModels/TrackerMainViewModel.cs` - Filters for Feedback/Goals/Projects/Tasks

---

## Design Principles (from .cursorrules)

1. **Modularity & Reusability** - UserControls for reuse
2. **No Duplicative Code** - DRY principle
3. **No Duplicative Styles** - Consistent theming
4. **No Duplicative Data** - Single source of truth
5. **Accessibility** - Keyboard navigation, screen readers
6. **Usability** - Clear workflows, obvious actions
7. **Simplicity** - Don't over-engineer, but don't dumb down value

