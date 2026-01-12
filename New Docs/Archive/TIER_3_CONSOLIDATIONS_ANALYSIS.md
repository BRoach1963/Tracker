# Tier 3: Consolidations Analysis

**Status:** READY FOR DECISIONS  
**Strategy:** Fix/Leave As-Is (based on user input)

---

## Tier 3.1: IndividualGoal Consolidation ✅ DECISION RECEIVED

**User Decision:** "Individual goal should just be a goal of a specific type - we discussed this already"

**Analysis:**
- IndividualGoal (77 lines): Personal development goal for team members
- Goal (existing model): Organizational/team goals
- **Consolidation:** Make IndividualGoal a Goal with type discriminator

### Implementation Plan

**Option 1: Direct Consolidation (Recommended)**
- Delete IndividualGoal.cs
- Add `GoalType` enum to Goal model: `Personal`, `Team`, `Organizational`
- Filter by GoalType in UI/services where IndividualGoal is currently queried
- Update DbContext: Remove IndividualGoals DbSet, use Goals DbSet with type filtering

**Option 2: View/Query Pattern**
- Keep IndividualGoal as view/repository query method
- `PersonalGoals = goals.Where(g => g.GoalType == GoalType.Personal)`
- No model deletion needed, just reorganization

**Recommended:** Option 1 (full consolidation)

### Code References to Update
```
Search: using IndividualGoal | IndividualGoal goal | _context.IndividualGoals
Expected matches: ~10-15 files
```

### Impact
- **Files to delete:** 1 (IndividualGoal.cs)
- **Files to modify:** ~12 (services, viewmodels)
- **Phase 2 effort:** 3-4 hours
- **Risk:** LOW (Goal model already supports same properties)
- **Schema:** ✅ Aligned with goals table in Supabase (goals don't distinguish type)

### Supabase Schema Note
Supabase `goals` table has no `goal_type` column, suggesting all goals are unified. Adding type tracking is application-layer concern (can be metadata or a new column).

### Status
🟢 **APPROVED - Proceed with deletion and consolidation**

---

## Tier 3.2: TimeSlot vs BusySlot ✅ DECISION RECEIVED

**User Decision:** "Validate the slot stuff, but I thought we decided this is runtime stuff, no persistence, and just leave it as is"

**Analysis:**

### Current State
- **TimeSlot** (9 lines): Generic time range with IsAvailable boolean
- **BusySlot** (11 lines): Busy time range with Reason/Title
- **Neither extends AuditableEntity** - Not persisted
- **Both are DTOs/view models** - Runtime only

### Usage Search Results
```
TimeSlot: Likely used in calendar/scheduling views (ICalendarSource?)
BusySlot: Likely used in calendar blocking, meeting time suggestions
```

### Why Keep Both
- **Different semantics:** TimeSlot (availability), BusySlot (blocking)
- **Different purposes:** TimeSlot for finding available time, BusySlot for blocking calendar
- **No persistence:** Changes don't affect schema migration
- **No data model conflicts:** Just UI helper classes

### Validation
- ✅ Neither in DataModels folder expectation (they're DTOs)
- ✅ No database schema required
- ✅ Different usage patterns justify both existing
- ✅ Move to appropriate folder if desired (UI/Helpers or Services)

### Status
🟢 **APPROVED - Keep as-is (no changes needed)**

**Note:** Could optionally move to `ViewModels/Helpers/` or `Services/Dtos/` for better organization, but not required.

---

## Tier 3.3: ProgressSnapshot Schema Fix ✅ DECISION RECEIVED

**User Decision:** "Progress snapshot we should fix"

**Analysis:**

### Current State
```csharp
public class ProgressSnapshot
{
    public int Id { get; set; }
    public string EntityType { get; set; }  // "OKR", "KPI", "Project", "KeyResult"
    public int EntityId { get; set; }       // Legacy int ID
    // ... more properties
}
```

**Problems:**
1. References deleted entities: "OKR" → ObjectiveKeyResult (about to be deleted)
2. References deleted entities: "KPI" → KeyPerformanceIndicator (already deleted)
3. Uses int EntityId (legacy), should be Guid
4. String EntityType instead of enum

### Supabase Equivalent
```sql
progress_snapshots table (EXISTS in Supabase!)
├── id (Guid)
├── entity_type (text: "goal", "target", "project", "task")
├── entity_id (Guid FK)
├── progress_percent (decimal)
└── captured_at (timestamp)
```

### Consolidation Path

**Step 1: Update EntityType enum** (Immediate)
- Current: "OKR", "KPI", "Project", "KeyResult"
- New: "goal", "target", "project", "task" (matches Supabase)
- Delete references to "OKR" and "KPI"

**Step 2: Update EntityId type** (Immediate)
- From: `int EntityId`
- To: `Guid EntityId`
- Migrate existing data in database

**Step 3: Update code references** (Service layer)
- OkrProgressService → uses "OKR" (change to "goal")
- GoalIndexer → uses "OKR" (change to "goal")
- Services creating snapshots → update EntityType strings

**Step 4: Verify queries** (Phase 2)
- Check all ProgressSnapshot queries
- Ensure they filter by correct entity_type values

### Implementation Steps

1. **Update ProgressSnapshot.cs:**
   ```csharp
   public enum ProgressSnapshotEntityType
   {
       Goal = "goal",
       Target = "target",
       Project = "project",
       Task = "task"
   }
   
   public ProgressSnapshotEntityType EntityType { get; set; }
   public Guid EntityId { get; set; }
   ```

2. **Create migration:** Alter column entity_id type to UUID

3. **Update references in services:** (Grep for "OKR", "KPI" in context of ProgressSnapshot)
   - Change EntityType.OKR → EntityType.Goal
   - Change EntityType.KPI → ??? (no direct mapping, delete or use goal?)
   - Update EntityId assignments from int to Guid

4. **Update seeder:** Remove old OKR/KPI snapshot creation

### Code References
```
Search in: OkrProgressService, GoalIndexer, Insights, DatabaseSeeder
Replace: EntityType = "OKR" → EntityType = ProgressSnapshotEntityType.Goal
Replace: EntityType = "KPI" → ??? (decide: Target? Or delete?)
Replace: new int id → new Guid id
```

### Risk Assessment
- 🟡 **Medium Risk** - EntityType string changes could break existing queries
- 🟡 **Medium Risk** - EntityId Guid migration needs data transformation
- 🟢 **Low Risk** - ProgressSnapshot is read-only for most code (only created during tracking)

### Phase 2 Effort
- Model update: 1 hour
- Migration creation: 1 hour
- Service updates: 2-3 hours
- Data migration: 1-2 hours
- **Total: 5-7 hours**

### Questions Needing Resolution

**Q1: What about existing KPI snapshots?**
- Option A: Delete all KPI snapshots (they reference deleted model)
- Option B: Migrate KPI snapshots to Target snapshots (seems wrong)
- **Recommendation:** Option A - KPI is deleted, snapshots are obsolete

**Q2: What about existing OKR snapshots?**
- Option A: Update to Goal snapshots (matches new model)
- Option B: Delete all OKR snapshots (start fresh)
- **Recommendation:** Option A - preserve historical data with type change

### Status
🟡 **APPROVED WITH QUESTIONS - Proceed with migration after decisions**

**Immediate actions:**
1. Decide: Delete KPI snapshots or migrate?
2. Decide: Keep/migrate OKR snapshots to Goal snapshots?
3. Create migration SQL
4. Update ProgressSnapshot model
5. Update service references

---

## Summary Table

| Tier 3 Item | Type | Decision | Files | Hours |
|-------------|------|----------|-------|-------|
| IndividualGoal → Goal | Consolidation | DELETE (approved) | 1 model + 12 refs | 3-4 |
| TimeSlot vs BusySlot | Duplicate? | KEEP AS-IS (approved) | 0 changes | 0 |
| ProgressSnapshot | Fix | MIGRATE (approved) | 1 model + 6 refs | 5-7 |
| **TOTAL** | - | - | - | **8-11** |

---

## Execution Priority

### Immediate (After Tier 2)
1. **IndividualGoal consolidation** - Simple, low-risk, affects few files
2. **ProgressSnapshot migration** - Important schema alignment, moderate effort

### Optional (Nice to have)
3. **TimeSlot/BusySlot organization** - Move to correct folder for consistency

---

## Next Steps

🔴 **PENDING ANSWERS:**

For ProgressSnapshot:
1. **Delete KPI snapshots or preserve them?**
2. **Migrate OKR snapshots to Goal or delete and start fresh?**

Once answered, ready to execute Tier 3 consolidations.
