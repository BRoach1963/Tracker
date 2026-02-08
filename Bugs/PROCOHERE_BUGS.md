# ProCohere Avalonia - Bugs to Fix

## Active Bugs

(None - all resolved!)

---

## Resolved Bugs

### ✅ BUG-001: Team Page Card Selection Scroll Issue
**Reported**: 2026-01-18  
**Resolved**: 2026-02-07  
**Fix**: Added `ScrollCardIntoView()` method in CircleView.axaml.cs that scrolls the selected card into view after a short delay (allows panel to expand first).

---

### ✅ BUG-002: No Visual Indicator on Selected Team Member Card
**Reported**: 2026-01-18  
**Resolved**: 2026-02-07  
**Fix**: Enhanced `.member-card.selected` style with green background (`BrushPrimarySubtle`), thicker border, and glow effect (`BoxShadow`). Also added smooth transitions for hover/selection states.

---

### ✅ BUG-003: Manager "Reports" Badge Not Obviously Clickable
**Reported**: 2026-01-18  
**Resolved**: 2026-02-07  
**Fix**: The `view-team-button` already had good hover states. Added `:pressed` state with scale transform for better click feedback.

---

### ✅ BUG-004: Manager Filter Breadcrumb Too Subtle
**Reported**: 2026-01-18  
**Resolved**: 2026-02-07  
**Fix**: The breadcrumb was already prominent with a colored banner, filter icon, and clear "Show All Team" button. No changes needed - verified it's visible and functional.
