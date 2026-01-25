# MVVM Repair Plan for ProCohere.Avalonia

**Created:** January 25, 2026  
**Status:** IN PROGRESS  
**Priority:** HIGH - Critical MVVM violations across all dialogs

---

## Problem Statement

14 dialogs have **no ViewModels**, with business logic directly in code-behind. This violates the project's #1 critical rule: "NEVER VIOLATE MVVM".

Current state:
- `EditMeetingDialog.axaml.cs`: **1,416 lines** of business logic
- 20+ event handlers instead of commands
- DTOs defined inside view code-behind
- Direct service calls from views

---

## Guiding Principles

### ✅ Acceptable in Code-Behind (UI Glue)
- Dialog open/close mechanics (`ShowDialog`, `Close()`)
- Focus management (`TextBox.Focus()`)
- Visual tree manipulation (building calendar grids)
- Scroll handling
- Drag-and-drop visual feedback
- Animation triggers
- Window chrome (close button, drag to move)

### ❌ Must Be in ViewModel
- Service calls (Create/Update/Delete/Get)
- Data validation
- Business rules
- State management
- Command logic
- Observable collections
- DTOs/Result classes (in Models folder)

---

## Audit Results

| File | Lines | Has ViewModel | Severity |
|------|-------|---------------|----------|
| EditMeetingDialog.axaml.cs | 1,416 | ❌ | 🔴 CRITICAL |
| CircleView.axaml.cs | 538 | ✅ | 🟡 MEDIUM |
| MeView.axaml.cs | 523 | ✅ | 🟡 MEDIUM |
| AgendaItemCard.axaml.cs | 383 | ❌ | 🔴 HIGH |
| EditTeamMemberDialog.axaml.cs | 369 | ❌ | 🔴 HIGH |
| EntityPickerDialog.axaml.cs | 293 | ❌ | 🟠 MEDIUM |
| EditAccountDialog.axaml.cs | 286 | ❌ | 🟠 MEDIUM |
| EditMetricDialog.axaml.cs | 249 | ❌ | 🟠 MEDIUM |
| DateTimeSelector.axaml.cs | 242 | N/A | 🟢 Control |
| EditPrepItemDialog.axaml.cs | 235 | ❌ | 🟠 MEDIUM |
| EditGoalDialog.axaml.cs | 215 | ❌ | 🟠 MEDIUM |
| EditAgendaItemDialog.axaml.cs | 198 | ❌ | 🟠 MEDIUM |
| AddTaskDialog.axaml.cs | 182 | ❌ | 🟠 MEDIUM |
| ApplyTemplateDialog.axaml.cs | 173 | ❌ | 🟠 MEDIUM |
| MeetingDetailFlyout.axaml.cs | 161 | ❌ | 🟠 MEDIUM |
| TasksTabView.axaml.cs | 145 | ❌ | 🟡 LOW |
| RecordOutcomeDialog.axaml.cs | 142 | ❌ | 🟡 LOW |
| CarryForwardSuggestionsPanel.axaml.cs | 123 | ❌ | 🟡 LOW |
| DeferAgendaItemDialog.axaml.cs | 108 | ❌ | 🟡 LOW |
| GoalsTabView.axaml.cs | 100 | ❌ | 🟡 LOW |
| UpdateMetricValueDialog.axaml.cs | 83 | ❌ | 🟡 LOW |
| AddNoteDialog.axaml.cs | 47 | ❌ | 🟢 LOW |

---

## Phase 1: EditMeetingDialog (CRITICAL)

**Status:** 🔄 IN PROGRESS

### Files to Create
- [ ] `ViewModels/Dialogs/EditMeetingDialogViewModel.cs`
- [ ] `Models/Dialogs/EditMeetingResult.cs`
- [ ] `Models/Dialogs/DialogAgendaItem.cs`
- [ ] `Models/Dialogs/DialogPrepItem.cs`

### Commands to Implement
- [ ] `SaveCommand`
- [ ] `CancelCommand`
- [ ] `DeleteCommand`
- [ ] `AddAgendaItemCommand`
- [ ] `RemoveAgendaItemCommand`
- [ ] `EditAgendaItemCommand`
- [ ] `AddPrepItemCommand`
- [ ] `RemovePrepItemCommand`
- [ ] `EditPrepItemCommand`
- [ ] `AddAttendeeCommand`
- [ ] `RemoveAttendeeCommand`
- [ ] `LinkEntityCommand`
- [ ] `SetActiveTabCommand`

### ViewModel Regions (Standard Structure)
```
#region Fields
#region Observable Properties
#region Collections
#region Commands
#region Constructor
#region Public Methods (called by View for initialization)
#region Command Implementations
#region Private Helpers
#region Validation
```

### Target
- Code-behind: ~50-100 lines (dialog mechanics only)
- ViewModel: All business logic, validation, service calls

---

## Phase 2: High Priority Dialogs

**Status:** ⏳ PENDING

| Dialog | Est. Effort |
|--------|-------------|
| EditTeamMemberDialog (369 lines) | 2 hrs |
| EditAccountDialog (286 lines) | 1.5 hrs |
| EntityPickerDialog (293 lines) | 1.5 hrs |

---

## Phase 3: Medium Priority Dialogs

**Status:** ⏳ PENDING

| Dialog | Est. Effort |
|--------|-------------|
| EditMetricDialog | 1 hr |
| EditPrepItemDialog | 1 hr |
| EditGoalDialog | 1 hr |
| EditAgendaItemDialog | 1 hr |
| AddTaskDialog | 1 hr |
| ApplyTemplateDialog | 1 hr |
| MeetingDetailFlyout | 1 hr |

---

## Phase 4: Controls

**Status:** ⏳ PENDING

| Control | Notes |
|---------|-------|
| AgendaItemCard (383 lines) | Has service calls, needs ViewModel |
| CarryForwardSuggestionsPanel | Has service calls |
| DateTimeSelector | Pure UI control - keep as-is |

---

## Phase 5: Low Priority & Cleanup

**Status:** ⏳ PENDING

- TasksTabView
- GoalsTabView
- RecordOutcomeDialog
- DeferAgendaItemDialog
- UpdateMetricValueDialog
- AddNoteDialog
- CircleView/MeView audit (calendar building OK, verify no service calls)

---

## Architecture Decisions

### Dialog Result Pattern
Dialogs expose `public DialogResult? Result { get; private set; }` property. The ViewModel sets the result, view reads it after close.

### Tab Views
Embedded tab views share parent's ViewModel context via `DataContext` inheritance.

### Entity Picker
Single `EntityPickerDialogViewModel` with entity type configuration, not separate ViewModels per type.

---

## Progress Log

| Date | Phase | Action | Status |
|------|-------|--------|--------|
| 2026-01-25 | 1 | Begin EditMeetingDialog refactor | 🔄 |

---

## Definition of Done

For each dialog:
1. ✅ ViewModel created with proper regions
2. ✅ All service calls moved to ViewModel
3. ✅ All commands implemented (no event handlers for business logic)
4. ✅ DTOs moved to Models folder
5. ✅ Code-behind contains only UI glue
6. ✅ Builds without errors
7. ✅ Functionality verified manually
