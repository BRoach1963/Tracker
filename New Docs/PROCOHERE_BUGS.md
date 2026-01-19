# ProCohere Avalonia - Bugs to Fix

## Active Bugs

### BUG-001: Team Page Card Selection Scroll Issue
**Reported**: 2026-01-18  
**Status**: Open  
**Priority**: Medium  
**Area**: Circle/Team Page

**Description**:  
When clicking on a team member card that is low in the list, the detail panel expands and the card list shrinks. This pushes the selected card out of view. The UI should scroll to keep the selected card visible after selection.

**Steps to Reproduce**:
1. Go to Circle (Team) page
2. Scroll down to a card near the bottom of the list
3. Click on that card to select it
4. Observe: The card disappears from view as the list shrinks to make room for the detail panel

**Expected Behavior**:  
After selecting a card, the list should automatically scroll to keep the selected card in view.

**Affected Files**:
- `ProCohere.Avalonia/Views/CircleView.axaml`
- Possibly `ProCohere.Avalonia/ViewModels/CircleViewModel.cs`

---

### BUG-002: No Visual Indicator on Selected Team Member Card
**Reported**: 2026-01-18  
**Status**: Open  
**Priority**: Medium  
**Area**: Circle/Team Page

**Description**:  
When a team member card is selected, there's no visual indicator showing which card is currently selected. User has no way to know which card's details are being shown in the panel.

**Expected Behavior**:  
Selected card should have a visible border highlight or background change to indicate selection state.

**Affected Files**:
- `ProCohere.Avalonia/Views/CircleView.axaml` (card styles)

---

### BUG-003: Manager "Reports" Badge Not Obviously Clickable
**Reported**: 2026-01-18  
**Status**: Open  
**Priority**: Medium  
**Area**: Circle/Team Page

**Description**:  
The "X reports ›" badge on manager cards has a very small chevron that doesn't clearly indicate it's an interactive element. Would not work well on mobile/touch interfaces.

**Expected Behavior**:  
Badge should look more like a button with clear visual affordance for clicking (icon, hover state, etc.)

**Affected Files**:
- `ProCohere.Avalonia/Views/CircleView.axaml` (card template)

---

### BUG-004: Manager Filter Breadcrumb Too Subtle
**Reported**: 2026-01-18  
**Status**: Open  
**Priority**: Low  
**Area**: Circle/Team Page

**Description**:  
When filtering by manager, the breadcrumb showing "Viewing team of [Name]" is too subtle and not immediately obvious.

**Expected Behavior**:  
Breadcrumb should be more prominent with clear "Show All" action.

**Affected Files**:
- `ProCohere.Avalonia/Views/CircleView.axaml` (breadcrumb section)

---

## Resolved Bugs

(None yet)
