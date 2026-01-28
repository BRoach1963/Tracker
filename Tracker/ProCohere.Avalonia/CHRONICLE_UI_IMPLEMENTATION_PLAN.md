# Chronicle UI Implementation Plan

## Overview

Build a responsive card-based notes UI (Windows Explorer style grid) that integrates with the existing `ChronicleViewModel`. The UI should be modular, MVVM-compliant, and follow established patterns in the codebase.

---

## Existing Assets (Already Built)

| Component | Location | Status |
|-----------|----------|--------|
| `ChronicleViewModel` | `ViewModels/ChronicleViewModel.cs` | ✅ Complete (~578 lines) |
| `Note` model | `Models/Note.cs` | ✅ Complete |
| `NotesService` | `Services/NotesService.cs` | ✅ Complete |
| `NoteCategory` enum | `Models/NoteCategory.cs` | ✅ Complete |
| `NoteDetailFlyout` | `Views/Controls/NoteDetailFlyout.axaml` | ✅ Complete (353 lines) |
| MainWindow placeholder | `Views/MainWindow.axaml` | 🔄 Needs replacement |

---

## Implementation Phases

### Phase 1: Core Card Grid View
**Goal:** Replace MainWindow placeholder with functional card grid

#### Files to Create:
1. **`Views/ChronicleView.axaml`** - Main Chronicle view
2. **`Views/ChronicleView.axaml.cs`** - Code-behind for DataContext wiring

#### Features:
- Header with title, subtitle, stats (Total/Pinned counts)
- Search bar with real-time filtering
- Sub-tab bar (Notes / Reports - future)
- Responsive WrapPanel card grid
- Empty state when no notes
- Loading spinner

#### ViewModel Bindings Used:
- `Notes`, `PinnedNotes` collections
- `SearchQuery`, `SearchCommand`
- `TotalCount`, `PinnedCount`
- `IsLoading`, `HasError`, `ErrorMessage`
- `LoadNotesCommand`

---

### Phase 2: Note Card Component
**Goal:** Create reusable card control for displaying notes

#### Files to Create:
1. **`Views/Controls/NoteCard.axaml`** - Card template
2. **`Views/Controls/NoteCard.axaml.cs`** - Code-behind

#### Card Layout:
```
┌─────────────────────────────────────┐
│ 📌 Pinned          Category Badge   │  <- Header row
├─────────────────────────────────────┤
│ Title (truncated)                   │
│ Content preview (2-3 lines)...      │
├─────────────────────────────────────┤
│ 🎯 ✅ 📅 👤 🏢    Jan 15, 2026     │  <- Footer: badges + date
└─────────────────────────────────────┘
```

#### Features:
- Fixed min-width, flexible height (responsive)
- Pin indicator (top-left corner or badge)
- Category badge (top-right)
- Title with ellipsis
- Content preview (2-3 lines, faded truncation)
- Entity link badges with icons:
  - 🎯 Goal (linked_goal_id)
  - ✅ Task (linked_task_id)
  - 📅 Meeting (linked_meeting_id)
  - 👤 Person (linked_team_member_id)
  - 🏢 Project (linked_project_id)
- Hover effect (border color change, slight lift)
- Click to select
- Right-click context menu (Edit, Pin, Delete)

#### Computed Properties on Note Model (already exist):
- `DisplayTitle` - title or first 50 chars
- `ContentPreview` - first 200 chars
- `HasLinks`, `LinkCount`

---

### Phase 3: Card Grid Layout & Responsiveness
**Goal:** Windows Explorer-style flowing grid

#### Implementation:
- Use `ItemsControl` with `WrapPanel` as ItemsPanel
- Set card MinWidth (280px) and MaxWidth (400px)
- Cards flow and wrap based on container width
- Separate sections for Pinned vs Regular notes

#### XAML Structure:
```xml
<ScrollViewer>
    <StackPanel>
        <!-- Pinned Section (if any) -->
        <TextBlock Text="📌 Pinned" IsVisible="{Binding PinnedNotes.Count}"/>
        <ItemsControl ItemsSource="{Binding PinnedNotes}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel Orientation="Horizontal"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <controls:NoteCard/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <!-- All Notes Section -->
        <TextBlock Text="Notes"/>
        <ItemsControl ItemsSource="{Binding Notes}">
            <!-- Same pattern -->
        </ItemsControl>
    </StackPanel>
</ScrollViewer>
```

---

### Phase 4: Note Editor Panel
**Goal:** Side panel or overlay for creating/editing notes

#### Files to Create:
1. **`Views/Controls/NoteEditorFlyout.axaml`** - Editor panel
2. **`Views/Controls/NoteEditorFlyout.axaml.cs`** - Code-behind

#### Features:
- Slide-in panel (right side) or modal dialog
- Title input (TextBox)
- Content editor (multiline TextBox, eventually rich text)
- Category dropdown
- Tags input (comma-separated or chips)
- Entity linking section (Phase 5)
- Save / Cancel buttons

#### ViewModel Bindings:
- `EditingNote` - the note being edited
- `IsNoteEditorOpen`
- `SaveNoteCommand`, `CloseNoteEditorCommand`
- `CreateNewNoteCommand`, `EditNoteCommand`

---

### Phase 5: Entity Linking UI (Badges + Picker)
**Goal:** Display linked entities as badges, allow linking via picker

#### Part A: Badge Display (in NoteCard)
- Show icon badges for linked entities
- Tooltip on hover shows entity name (requires service lookup)
- Future: Click badge to navigate to entity

#### Part B: Entity Linking Picker
**File:** `Views/Dialogs/EntityLinkingDialog.axaml`

- Tabbed interface: Goals | Tasks | Meetings | People | Projects
- Search within each tab
- Multi-select checkboxes
- Currently linked items pre-checked
- Returns list of `LinkedEntityInfo` objects

**Note:** This is schema-agnostic. Whether links come from FK columns or join table, the UI just needs:
```csharp
public class LinkedEntityInfo
{
    public string EntityType { get; set; }  // "goal", "task", etc.
    public Guid EntityId { get; set; }
    public string DisplayName { get; set; }  // For tooltip/display
    public string Icon { get; set; }         // PathIcon data or emoji
}
```

---

### Phase 6: Polish & Interactions
**Goal:** Smooth UX and edge cases

#### Features:
- Keyboard navigation (Tab through cards, Enter to open)
- Context menu (right-click: Edit, Pin/Unpin, Delete)
- Drag-to-reorder pinned notes (stretch goal)
- Animation: Cards fade in on load
- Auto-focus search on Ctrl+F
- Confirmation dialog for delete (already implemented in VM)

---

## File Structure Summary

```
Views/
├── ChronicleView.axaml            # NEW - Main view
├── ChronicleView.axaml.cs         # NEW - Code-behind
├── Controls/
│   ├── NoteCard.axaml             # NEW - Card component
│   ├── NoteCard.axaml.cs          # NEW
│   ├── NoteDetailFlyout.axaml     # EXISTS - Read-only detail view
│   ├── NoteEditorFlyout.axaml     # NEW - Edit/create panel
│   └── NoteEditorFlyout.axaml.cs  # NEW
├── Dialogs/
│   ├── EntityLinkingDialog.axaml  # NEW - Multi-select picker
│   └── EntityLinkingDialog.axaml.cs # NEW
```

---

## ViewModel Additions Needed

The existing `ChronicleViewModel` is comprehensive. Minor additions may include:

```csharp
// For entity linking display (may already exist via NotesService)
[ObservableProperty]
private ObservableCollection<LinkedEntityInfo> _selectedNoteLinks = new();

// For entity picker
[RelayCommand]
private async Task OpenEntityLinkingDialogAsync()
{
    // Open dialog, get selections, update EditingNote
}
```

---

## Styling Guidelines

Follow established patterns from `GoalCard.axaml`:

1. **Card Border:** `BrushSurface` background, `BrushBorder` border, 8px radius
2. **Hover State:** `BrushPrimary` border, `BrushSurfaceHover` background
3. **Text:** `BrushTextPrimary` for title, `BrushTextSecondary` for preview
4. **Badges:** Use existing `.health-badge` style patterns
5. **Icons:** Use PathIcon with Material Design paths (consistent with rest of app)

---

## Implementation Order

| Phase | Deliverable | Dependencies |
|-------|-------------|--------------|
| 1 | ChronicleView.axaml (shell + grid) | None |
| 2 | NoteCard.axaml | Phase 1 |
| 3 | Responsive layout tuning | Phase 2 |
| 4 | NoteEditorFlyout.axaml | Phase 2 |
| 5 | Entity linking badges + dialog | Phase 4 |
| 6 | Polish & keyboard nav | All above |

---

## Testing Checklist

- [ ] Empty state displays when no notes
- [ ] Notes load on navigation to Chronicle tab
- [ ] Search filters cards in real-time
- [ ] Pinned section shows when pinned notes exist
- [ ] Card click opens detail flyout
- [ ] Create new note opens editor
- [ ] Edit note populates editor correctly
- [ ] Save creates/updates note in collection
- [ ] Delete shows confirmation, removes card
- [ ] Pin/Unpin moves card between sections
- [ ] Entity badges display for linked items
- [ ] Responsive: cards reflow on resize
- [ ] Keyboard: Tab navigation works
- [ ] Theme: Light/Dark modes work

---

## Open Questions (Deferred)

1. **Rich text editor?** - Start with plain TextBox, can upgrade later
2. **Category management?** - Use predefined list, custom categories later
3. **Drag reorder?** - Stretch goal, not MVP
4. **Reports tab?** - Placeholder only for now

---

## Ready to Start

**Phase 1** can begin immediately. Say "Phase 1 - go" when ready.
