# ProCohere.Avalonia Build Fix Plan

**Date:** 2026-01-23  
**Total Errors:** 43 (after clean build)  
**Root Cause:** Model property mismatches between code and database schema

---

## Error Summary by File

| File | Error Count | Root Cause |
|------|-------------|------------|
| `MeetingTemplateService.cs` | 12 | Missing `Category`, `IsSystem`, `IsShared` + wrong MeetingTemplateItem usage |
| `CircleViewModel.cs` | 15 | Guid? → Guid assignment + .HasValue/.Value on non-nullable |
| `DashboardService.cs` | 3 | Guid.HasValue/Value on non-nullable Guid |
| `GoalsService.cs` | 1 | ?? operator on non-nullable Guid |
| `MeetingNoteService.cs` | 3 | Missing `IsPrivate` property |
| `MeetingPrepItemService.cs` | 2 | Missing `DeletedAt`, `DeletedBy` |
| `ApplyTemplateDialog.axaml.cs` | 1 | Missing `Category` property |
| `EditGoalDialog.axaml.cs` | 4 | Guid.HasValue/Value on non-nullable |
| `EditMetricDialog.axaml.cs` | 1 | ToString() overload issue |

---

## Fix 1: MeetingTemplateDetail - Missing Properties

**File:** `Models/MeetingTemplateDetail.cs`

The service code expects `Category`, `IsSystem`, `IsShared` but the model has:
- `MeetingType` (not `Category`)
- `IsSystemTemplate` (not `IsSystem`)
- No `IsShared` property

**Fix Options:**
1. **Option A (Recommended):** Update service code to use existing properties
   - Replace `t.Category` → `t.MeetingType`
   - Replace `t.IsSystem` → `t.IsSystemTemplate`
   - Add `IsShared` to model (if needed) or remove references

2. **Option B:** Add alias properties to model
   ```csharp
   public string Category => MeetingType;
   public bool IsSystem => IsSystemTemplate;
   public bool IsShared { get; set; }
   ```

---

## Fix 2: MeetingTemplateItem - Missing DB Properties & BaseModel

**File:** `Models/MeetingTemplateDetail.cs` (contains `MeetingTemplateItem` class)

The `MeetingTemplateItem` class:
- Does NOT inherit from `BaseModel` (it's JSONB, not a table)
- Does NOT have `TemplateId`, `CreatedAt` columns (it's embedded JSON)

**Issue:** `MeetingTemplateService.cs` tries to use `From<MeetingTemplateItem>()` which requires BaseModel

**Fix:** The service is wrong - `MeetingTemplateItem` is JSONB data stored in `meeting_templates.default_agenda`, NOT a separate table. Remove all direct `From<MeetingTemplateItem>()` calls and work with the JSON properly.

```csharp
// WRONG - there is no meeting_template_items table
await client.From<MeetingTemplateItem>().Insert(...)

// RIGHT - serialize to JSON and update the template
template.DefaultAgendaJson = JsonSerializer.Serialize(items);
await client.From<MeetingTemplateDetail>().Update(template);
```

---

## Fix 3: GoalDetail.OwnerTeamMemberId - Nullable Mismatch

**Files:** 
- `Services/DashboardService.cs` (line 196)
- `Services/GoalsService.cs` (line 357)
- `Views/Dialogs/EditGoalDialog.axaml.cs` (lines 112, 114, 131, 133)
- `ViewModels/CircleViewModel.cs` (lines 1654, 1670)

Model has: `public Guid OwnerTeamMemberId { get; set; }` (non-nullable)  
Code uses: `.HasValue` and `.Value` (treats as nullable)

**Fix:** Update code to not use `.HasValue`/`.Value`:
```csharp
// BEFORE (wrong - OwnerTeamMemberId is not nullable)
if (goal.OwnerTeamMemberId.HasValue && memberDict.TryGetValue(goal.OwnerTeamMemberId.Value, out var owner))

// AFTER (correct)
if (goal.OwnerTeamMemberId != Guid.Empty && memberDict.TryGetValue(goal.OwnerTeamMemberId, out var owner))
```

---

## Fix 4: MeetingNote.IsPrivate - Missing Property

**File:** `Services/MeetingNoteService.cs` (lines 272, 280)

Code references `note.IsPrivate` but the property doesn't exist on `MeetingNote` model.

**Fix:** Either:
1. Add `IsPrivate` property to `MeetingNote` model (map to `is_private` column if exists)
2. Remove/replace references if the property shouldn't exist

---

## Fix 5: MeetingPrepItem - Missing Soft Delete Properties

**File:** `Services/MeetingPrepItemService.cs` (lines 389, 390)

Code references `item.DeletedAt` and `item.DeletedBy` but these don't exist on `MeetingPrepItem`.

**Fix:** Add to `MeetingPrepItem` model:
```csharp
[Column("deleted_at")]
public DateTime? DeletedAt { get; set; }

[Column("deleted_by")]
public Guid? DeletedBy { get; set; }
```

---

## Fix 6: EditMetricDialog.ToString() - Wrong Overload

**File:** `Views/Dialogs/EditMetricDialog.axaml.cs` (line 72)

Calling `ToString(format)` on a type that doesn't support format overload.

**Fix:** Check what type is being formatted and use appropriate conversion.

---

## Fix 7: CircleViewModel - Guid? to Guid Assignment (Test Data)

**File:** `ViewModels/CircleViewModel.cs` (lines 1446-1512)

Test data generation uses:
```csharp
TeamMemberId = members.FirstOrDefault()?.Id,  // Returns Guid? 
```

But `FeedbackDetail.TeamMemberId` is `Guid` (not nullable).

**Fix:** Use null coalescing:
```csharp
TeamMemberId = members.FirstOrDefault()?.Id ?? Guid.Empty,
```

---

## Recommended Fix Order

1. **Fix 1** - MeetingTemplateDetail/Item (biggest impact - 12+ errors)
2. **Fix 3** - Guid.HasValue/Value fixes (DashboardService, GoalsService, EditGoalDialog, CircleViewModel)
3. **Fix 7** - CircleViewModel Guid? assignments (7 errors)
4. **Fix 4** - MeetingNote.IsPrivate (3 errors)
5. **Fix 5** - MeetingPrepItem soft delete (2 errors)
6. **Fix 6** - EditMetricDialog.ToString (1 error)

---

## Files to Modify

| Priority | File | Changes | Error Count |
|----------|------|---------|-------------|
| 1 | `Services/MeetingTemplateService.cs` | Rewrite - use correct property names, fix MeetingTemplateItem | 12 |
| 2 | `ViewModels/CircleViewModel.cs` | Fix Guid? → Guid + HasValue/Value | 15 |
| 3 | `Services/DashboardService.cs` | Fix nullable Guid handling | 3 |
| 4 | `Views/Dialogs/EditGoalDialog.axaml.cs` | Fix nullable Guid handling | 4 |
| 5 | `Services/MeetingNoteService.cs` | Fix `IsPrivate` reference | 3 |
| 6 | `Services/MeetingPrepItemService.cs` | Add soft delete properties to model | 2 |
| 7 | `Services/GoalsService.cs` | Fix ?? operator on non-nullable | 1 |
| 8 | `Views/Dialogs/ApplyTemplateDialog.axaml.cs` | Change `Category` → `MeetingType` | 1 |
| 9 | `Views/Dialogs/EditMetricDialog.axaml.cs` | Fix ToString call | 1 |

---

## Estimated Effort

- **MeetingTemplateService (Fix 1):** ~30-45 minutes (needs careful review)
- **CircleViewModel (Fixes 3, 7):** ~20 minutes
- **Other files:** ~5-10 minutes each

**Total:** ~1.5-2 hours

---

## Notes for Next Session

1. **MeetingTemplateService is the big one** - it's using wrong property names AND treating JSONB as a separate table
2. Most other fixes are simple property name/type corrections
3. Build after each fix to verify progress
4. The old WPF Tracker project also has errors but we're ignoring those (it's being deprecated)
5. Some errors may be in generated/test data code (CircleViewModel) - consider if test data is needed
