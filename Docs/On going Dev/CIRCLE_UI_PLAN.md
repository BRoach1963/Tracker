# Circle UI Plan: Hierarchical Team View

**Document Created:** January 17, 2026  
**Status:** Planning  
**Related:** [PROCOHERE_DESIGN_DECISIONS.md](PROCOHERE_DESIGN_DECISIONS.md)

---

## Overview

This document outlines the plan to make the Circle area hierarchy-aware, leveraging role-based visibility rules and the `manager_team_member_id` relationships in the database.

---

## Visibility Policy (Key Decision)

Visibility is **role-based**, not just "descendants". Different users see different scopes:

| Role | Who They See | Implementation |
|------|--------------|----------------|
| **Admin** (Brian) | Entire org (or entire product scope) | All active team_members in org |
| **Manager** (Troy) | Self + all descendants | `get_team_descendants(org, self, include_self=true)` |
| **IC** (Janet) | Self + manager + peers (same manager) | Self + manager + `WHERE manager_team_member_id = my.manager_team_member_id` |

**Key insight:** `get_team_descendants()` answers "who reports under me?" but Circle needs "who can I see?" which varies by role.

### Wrapper RPC (New)

Create a single wrapper that encapsulates visibility policy:

```sql
procohere.get_visible_team_member_ids(
    p_organization_id uuid,
    p_team_member_id uuid
) RETURNS TABLE (
    team_member_id uuid,
    depth int,           -- 0 = self, 1 = direct, 2+ = skip-level, -1 = manager
    relation text        -- 'self', 'manager', 'peer', 'direct', 'descendant'
)
```

This keeps visibility rules in the DB, not scattered in client code. Policy changes don't require app updates.

---

## Current State

### Files
| File | Lines | Description |
|------|-------|-------------|
| `CircleView.axaml` | ~1300 | Fully built UI with tabs (Team, Goals, Feedback, Meetings) |
| `CircleViewModel.cs` | ~1264 | Working but loads ALL team members (flat list, no hierarchy) |
| `DashboardService.cs` | ~444 | Loads data via Supabase Postgrest client |
| `TeamMemberDetail.cs` | ~236 | Model with computed properties, NO hierarchy fields |

### Current Behavior
- Loads all active team members in the organization (flat)
- No concept of "who can see whom"
- No manager/report relationships displayed
- Current user is filtered out of the list

---

## What Needs to Change

### 1. Database: Add Wrapper RPC

**File:** New SQL function

```sql
CREATE OR REPLACE FUNCTION procohere.get_visible_team_member_ids(
    p_organization_id uuid,
    p_team_member_id uuid
) RETURNS TABLE (
    team_member_id uuid,
    depth int,
    relation text
) AS $$
DECLARE
    v_is_admin boolean;
    v_manager_id uuid;
BEGIN
    -- Get caller's info
    SELECT manager_team_member_id INTO v_manager_id
    FROM procohere.team_members
    WHERE id = p_team_member_id AND organization_id = p_organization_id;
    
    -- TODO: Check if admin (via role or org_members table)
    v_is_admin := false; -- Placeholder
    
    IF v_is_admin THEN
        -- Admin: see entire org
        RETURN QUERY
        SELECT tm.id, 0, 'org'::text
        FROM procohere.team_members tm
        WHERE tm.organization_id = p_organization_id
          AND tm.is_active = true
          AND tm.is_deleted = false;
    ELSE
        -- Self
        RETURN QUERY SELECT p_team_member_id, 0, 'self'::text;
        
        -- Manager (if exists)
        IF v_manager_id IS NOT NULL THEN
            RETURN QUERY SELECT v_manager_id, -1, 'manager'::text;
        END IF;
        
        -- Peers (same manager, excluding self)
        IF v_manager_id IS NOT NULL THEN
            RETURN QUERY
            SELECT tm.id, 0, 'peer'::text
            FROM procohere.team_members tm
            WHERE tm.manager_team_member_id = v_manager_id
              AND tm.id != p_team_member_id
              AND tm.organization_id = p_organization_id
              AND tm.is_active = true
              AND tm.is_deleted = false;
        END IF;
        
        -- Descendants (if manager)
        RETURN QUERY
        SELECT d.team_member_id, d.depth, 
               CASE WHEN d.depth = 1 THEN 'direct'::text ELSE 'descendant'::text END
        FROM procohere.get_team_descendants(p_organization_id, p_team_member_id, false) d;
    END IF;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;
```

### 2. Model: Add Hierarchy Fields to `TeamMemberDetail`

```csharp
// Database column mapping
[Column("manager_team_member_id")]
public Guid? ManagerTeamMemberId { get; set; }

// FROM RPC (not computed client-side)
public int HierarchyDepth { get; set; }      // 0=self, 1=direct, 2+=skip, -1=manager
public string Relation { get; set; } = "self"; // 'self','manager','peer','direct','descendant'

// Computed from VISIBLE set (not global org)
public int DirectReportCount { get; set; }
public int TotalDescendantCount { get; set; }
public string ManagerName { get; set; } = string.Empty;

// Derived
public bool IsManager => DirectReportCount > 0;
public bool IsPeer => Relation == "peer";
public bool IsMyManager => Relation == "manager";
```

### 3. Service: Create `TeamService`

**File:** `Services/TeamService.cs`

**Two-step fetch (optimized):**
1. **RPC:** Get visible IDs + depth + relation (small payload)
2. **PostgREST:** Fetch `team_members WHERE id IN (...)` with only needed fields

```csharp
public async Task<List<TeamMemberDetail>> GetVisibleTeamMembersAsync()
{
    // Step 1: Get visible IDs from wrapper RPC
    var visibleIds = await client.Rpc("get_visible_team_member_ids", new {
        p_organization_id = orgId,
        p_team_member_id = currentTeamMemberId
    });
    
    // Step 2: Fetch full records for those IDs
    var members = await client.From<TeamMemberDetail>()
        .Filter("id", Operator.In, visibleIds.Select(v => v.team_member_id))
        .Get();
    
    // Step 3: Merge depth/relation from RPC into members
    // Step 4: Compute DirectReportCount from visible set
    
    return members;
}
```

**Caching:** Hierarchy doesn't change often - cache per session.

### 4. ViewModel: Add Hierarchy-Aware Logic

**New Properties:**
```csharp
// Current user's team member record
TeamMemberDetail? CurrentTeamMember { get; }

// Who does the current user report to? (from visible set)
TeamMemberDetail? MyManager { get; }

// Direct reports of current user (from visible set)
ObservableCollection<TeamMemberDetail> MyDirectReports { get; }

// View mode (start with 2, add more later if needed)
TeamViewMode ViewMode { get; set; }  // Flat, Tree

// Expanded/collapsed state for tree view
Dictionary<Guid, bool> ExpandedNodes { get; }
```

**Enum (simplified):**
```csharp
public enum TeamViewMode
{
    Flat,    // Grid of cards (default)
    Tree     // Hierarchical org chart style
}
// Note: "My Team" can be added later as filter on Flat/Tree, not a separate mode
```

### 5. UI Changes to CircleView.axaml

#### A. Add View Mode Toggle (2 modes)
```
┌──────────────────────────────┐
│ [Flat Grid] [Tree View]      │
└──────────────────────────────┘
```

#### B. Tree View Mode
- Expandable/collapsible nodes
- Indent based on `HierarchyDepth` (from RPC)
- Manager badge/icon for people with reports
- "X direct reports" count (from visible set)

#### C. Member Card Enhancement
- Show "Reports to: [Manager Name]"
- Show "X direct reports" if manager
- Visual distinction for peers vs descendants (optional)
- Badge: "Manager" vs "IC"

#### D. Flat View: Manager Click Filter
- Clicking a manager card toggles filter: "Show [Name]'s team"
- Same underlying visible set, just filters root
- Breadcrumb to clear filter

#### E. Detail Panel Enhancement
- Add "Team" tab for managers showing their reports
- Show org path: "Brian → Troy → Alice"

---

## Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  User logs in → AuthService gets current team_member record    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  TeamService.GetVisibleTeamMembersAsync()                       │
│  → Step 1: RPC: get_visible_team_member_ids(org_id, my_id)     │
│    Returns: { team_member_id, depth, relation } for each       │
│  → Step 2: PostgREST: team_members WHERE id IN (visible_ids)   │
│  → Step 3: Merge depth/relation into member records            │
│  → Step 4: Compute DirectReportCount from visible set          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  CircleViewModel receives enriched data                         │
│  → Sets MyManager, MyDirectReports from relation field         │
│  → Populates FilteredTeamMembers (respects view mode)          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  CircleView renders based on view mode                          │
│  → Flat: grid of cards (default)                               │
│  → Tree: indented list using HierarchyDepth from RPC           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Order

| Step | Task | Effort | Status |
|------|------|--------|--------|
| 1 | Create `get_visible_team_member_ids()` wrapper RPC | Medium | ✅ DONE |
| 2 | Add `manager_team_member_id`, `HierarchyDepth`, `Relation` to `TeamMemberDetail` | Small | ✅ DONE |
| 3 | Create `TeamService` with 2-step fetch pattern | Medium | ✅ DONE |
| 4 | Update `CircleViewModel` to use `TeamService` | Medium | ✅ DONE |
| 5 | Add view mode toggle to UI (Flat/Tree) | Small | 🔲 TODO |
| 6 | Build tree view rendering in XAML | Medium | 🔲 TODO |
| 7 | Enhance member cards with manager/reports info | Small | 🔲 TODO |
| 8 | Add manager click filter in flat view | Small | 🔲 TODO |
| 9 | Enhance detail panel with team tab | Small | 🔲 TODO |

---

## Step-by-Step Implementation (Testable Chunks)

### Chunk 1: Database Function ✅
**Goal:** Create `get_visible_team_member_ids()` and verify it works in Supabase.

**Files:** SQL only (run in Supabase SQL Editor)

**Test:** Run queries in Supabase:
```sql
-- Test as Brian (manager of managers)
SELECT * FROM procohere.get_visible_team_member_ids('org-uuid', 'brian-team-member-uuid');

-- Test as Troy (manager)
SELECT * FROM procohere.get_visible_team_member_ids('org-uuid', 'troy-team-member-uuid');

-- Test as Janet (IC)
SELECT * FROM procohere.get_visible_team_member_ids('org-uuid', 'janet-team-member-uuid');
```

**Success criteria:** Each role sees appropriate members with correct depth/relation.

---

### Chunk 2: Model Updates
**Goal:** Add hierarchy fields to `TeamMemberDetail` so it can hold RPC data.

**Files:** `Models/TeamMemberDetail.cs`

**Test:** Build succeeds. No runtime test yet.

**Success criteria:** Build passes with new properties.

---

### Chunk 3: TeamService
**Goal:** Create service that calls RPC + fetches full records.

**Files:** 
- `Services/TeamService.cs` (new)

**Test:** Add temporary logging, run app, check logs show visible members.

**Success criteria:** Logs show correct member IDs, depths, relations.

---

### Chunk 4: ViewModel Integration
**Goal:** CircleViewModel uses TeamService instead of DashboardService for team members.

**Files:** `ViewModels/CircleViewModel.cs`

**Test:** Run app, go to Circle, see team members loaded (should look same as before but with hierarchy data populated).

**Success criteria:** Circle loads, members display (flat view unchanged visually).

---

### Chunk 5: Member Cards - Show Hierarchy Info
**Goal:** Member cards show "Reports to: X" and "Y direct reports".

**Files:** `Views/CircleView.axaml`

**Test:** Run app, see hierarchy info on cards.

**Success criteria:** Cards show manager name and report count.

---

### Chunk 6: View Mode Toggle + Tree View
**Goal:** Add Flat/Tree toggle, implement tree indentation.

**Files:** 
- `Views/CircleView.axaml`
- `ViewModels/CircleViewModel.cs`

**Test:** Toggle between Flat and Tree, see indented hierarchy.

**Success criteria:** Tree view shows indented members based on depth.

---

### Chunk 7: Manager Click Filter (Flat View)
**Goal:** Click manager → filter to show their team.

**Files:** 
- `Views/CircleView.axaml`
- `ViewModels/CircleViewModel.cs`

**Test:** Click Troy → see only Troy's reports. Breadcrumb to clear.

**Success criteria:** Filter works, breadcrumb clears it.

---

### Chunk 8: Detail Panel Team Tab
**Goal:** For managers, add "Team" tab showing their direct reports.

**Files:** `Views/CircleView.axaml`

**Test:** Select a manager, see Team tab with their reports.

**Success criteria:** Team tab shows direct reports.

---

## Decisions Made

### 1. IC Visibility: Option C ✅
**ICs see: self + manager + peers (same manager)**

Rationale:
- Avoids "empty Circle" confusion
- Gives context for meetings/feedback ("who's on my team?")
- Still respects hierarchy boundaries
- Natural for collaboration

### 2. Default View Mode: Flat ✅
**Flat is default, Tree is opt-in**

Rationale:
- Better for quick scanning and searching
- Lighter UI
- Tree is there when you need structure

### 3. Click Manager in Flat View: Yes ✅
**Clicking a manager toggles "Show [Name]'s team" filter**

Rationale:
- Makes flat view navigable
- Same visible set, just filtered root
- Easy to clear with breadcrumb

### 4. View Modes: Start with 2 ✅
**Flat + Tree only. "My Team" deferred.**

Rationale:
- YAGNI - "My Team" is just Tree filtered to depth ≤ 1
- Add later if real usage shows need

---

## Technical Notes

### RPC Call Pattern (Supabase)

```csharp
// Step 1: Get visible IDs + metadata
var visibleResult = await client.Rpc("get_visible_team_member_ids", new {
    p_organization_id = orgId,
    p_team_member_id = currentTeamMemberId
});

// Parse result into lookup dictionary
var visibilityMap = visibleResult.ToDictionary(
    v => v.team_member_id,
    v => (depth: v.depth, relation: v.relation)
);
```

### Computing Counts from Visible Set

```csharp
// IMPORTANT: Count from visible set, not global org
foreach (var member in visibleMembers)
{
    member.DirectReportCount = visibleMembers
        .Count(m => m.ManagerTeamMemberId == member.Id && m.Relation == "direct");
    
    member.TotalDescendantCount = visibleMembers
        .Count(m => m.ManagerTeamMemberId == member.Id || 
                    (m.Relation == "descendant" && /* is under this member */));
}
```

### Tree View XAML Pattern

```xml
<!-- Indentation based on depth FROM RPC -->
<Border Margin="{Binding HierarchyDepth, Converter={StaticResource DepthToMarginConverter}}">
    <!-- Member card content -->
</Border>

<!-- Visual distinction for relation -->
<Border Classes.peer="{Binding IsPeer}"
        Classes.manager="{Binding IsMyManager}">
```

---

## Related Database Objects

| Object | Purpose | Status |
|--------|---------|--------|
| `procohere.team_members.manager_team_member_id` | FK to manager's team_member record | ✅ Exists |
| `procohere.get_team_descendants(org_id, manager_id, include_self)` | Returns descendant team_member_ids | ✅ Exists |
| `procohere.get_visible_team_member_ids(org_id, team_member_id)` | **Wrapper: returns visible IDs + depth + relation** | 🔲 TODO |

---

## Future Considerations

- **Org chart visualization** - Could add a graphical org chart view later
- **"My Team" view mode** - Add if usage shows need (just filtered Tree)
- **Cross-team visibility** - What if someone needs to see outside their hierarchy?
- **Delegation** - Manager temporarily delegates visibility to another person
- **Historical hierarchy** - Track reporting changes over time
- **Admin detection** - How to identify admins (role table? org_members flag?)
