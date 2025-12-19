# Continuation Prompt for Next Chat

Copy and paste this to start your next session:

---

## Context

I'm continuing work on the Tracker application - a manager's tool for tracking team members, 1:1 meetings, OKRs, KPIs, projects, tasks, feedback, and goals.

**Recent Completed Work:**
- OKR/KPI model redesign (Phase 1 & 2 complete)
- Notes redesign with polymorphic linking to any entity
- Filtering system on all main pages (clickable stat cards)
- Master-detail layouts on OKR, KPI, and Notes pages

**Key Design Documents:**
- `Tracker/Docs/Design/OKR-KPI-Model-Design.md` - Full OKR/KPI architecture
- `Tracker/Docs/Design/UI-Redesign-Phase3-Summary.md` - Recent changes and pending work

---

## Current Tasks

### 1. Implement CommunityToolkit.Mvvm Messenger (Infrastructure)
**Purpose:** Cross-ViewModel communication when data changes in one view affects another.

**Tasks:**
- Add NuGet package: `CommunityToolkit.Mvvm`
- Create message types (`DataChangedMessage`, etc.)
- Update ViewModels to inherit `ObservableRecipient` and implement `IRecipient<T>`
- Send messages after CRUD operations in dialogs
- Receive and refresh in main ViewModels

**Use Cases:**
- Dialog saves → Main view refreshes
- Task completed → Dashboard updates
- 1:1 scheduled → Team member stats update

### 2. 1:1 Meeting Dialog Redesign (Priority)
**Current Problems:**
- 4 tabs mostly empty (Agenda Items, Notes/Agenda, Tasks, Linked Items)
- Linked Items tab is now redundant (we have polymorphic notes)
- Team member lookup is clunky (needs autocomplete)
- Styling inconsistent with other screens

**Proposed Direction:**
- Single scrollable panel instead of tabs
- Sections: Previous Meeting → Agenda → Notes → Action Items
- Inline agenda item editing with categories and entity linking
- AutoSuggest team member picker

**Key Question:** What makes agenda items VALUABLE for a manager?

### 3. AutoSuggest Control for DeepEndControls
- Need custom autocomplete without ModernWPF.UI (buggy, memory issues)
- Type-ahead filtering, keyboard navigation, custom templates

### 4. Team Members Page Rethink
- How to visualize team health effectively?
- 1:1 cadence tracking
- Task load balancing
- Goal progress per member

### 5. Feedback & Goals Pages
- What makes feedback tracking useful?
- How do Goals differ from OKRs?
- Should Goals tie to career progression/performance reviews?

---

## Technical Context

- **Framework:** WPF (.NET 8), MVVM pattern
- **Database:** EF Core with SQLite/SQL Server
- **Key Managers:** `TrackerDbManager`, `TrackerDataManager`, `UserSettingsManager`
- **Ownership:** All entities tied to User via `UserId` shadow property

---

## Design Principles

1. Modularity & Reusability (UserControls)
2. No duplicative code, styles, or data
3. Accessibility & Usability
4. Simplicity without dumbing down value

---

Please start by:
1. Adding the `CommunityToolkit.Mvvm` NuGet package
2. Reviewing `Tracker/Docs/Design/UI-Redesign-Phase3-Summary.md` for full context
3. Then let's discuss the 1:1 dialog redesign approach before implementing
