# ProCohere Projects — Implementation Plan

This document outlines the work required to implement the full Projects specification. **Do not start until approved.**

---

## Summary of Specifications Received

1. **Project Creation Flow** — Modal-based creation, minimal fields, post-create opens detail flyout
2. **Project Linking UX** — Work opts into projects (not the reverse), single-project linking only
3. **Project Completion Semantics** — Manual completion, no cascading effects on linked items
4. **Project vs Chronicle Boundary** — Projects organize intent, Chronicle records reality
5. **Projects in Briefing Rules** — Projects appear only as quiet labels on work items
6. **Owner Reassignment & Orphan Handling** — Ownership transfer, orphan recovery by admins

---

## Phase 1: Project Creation Flow

### 1.1 Create Project Modal (NEW)
**Files to create/modify:**
- `Views/Dialogs/CreateProjectDialog.axaml` — New modal dialog
- `Views/Dialogs/CreateProjectDialog.axaml.cs` — Code-behind
- `ViewModels/Dialogs/CreateProjectDialogViewModel.cs` — VM with validation

**Requirements:**
- Modal title: "Create Project"
- Required field: Project Name (inline validation: non-empty)
- Optional fields: Description, Due Date
- Implicit defaults set server-side (owner, status, org)
- On success: close modal, open Project Detail flyout for new project

**Backend:**
- Already exists: `rpc_create_project`
- Repository method: `ProjectsRepository.CreateAsync()`

### 1.2 Update ProjectsView Entry Points
**Files to modify:**
- `Views/ProjectsView.axaml` — Change "New Project" button to open modal (not flyout)
- `ViewModels/ProjectsViewModel.cs` — Add `ShowCreateDialogCommand` that opens modal

### 1.3 Post-Create Behavior
**Files to modify:**
- `ViewModels/ProjectsViewModel.cs` — After create succeeds, call `SelectProjectCommand` on new project

---

## Phase 2: Project Linking UX

### 2.1 Add "Project" Section to Entity Flyouts
**Files to modify:**
- `Views/TaskDetailView.axaml` — Add Project section with link/change/remove
- `Views/GoalDetailView.axaml` — Add Project section
- `Views/MetricDetailView.axaml` — Add Project section
- `Views/ChronicleNoteDetailView.axaml` — Add Project section (if exists)

**Pattern per flyout:**
```
Project
• Not linked               [Add to project]
— OR —
• Customer Onboarding     [Change] [Remove]
```

### 2.2 Project Selector Popover (NEW)
**Files to create:**
- `Views/Controls/ProjectSelectorPopover.axaml` — Lightweight selector
- `Views/Controls/ProjectSelectorPopover.axaml.cs` — Code-behind
- `ViewModels/ProjectSelectorViewModel.cs` — Search, list projects

**Requirements:**
- Search input
- List of user-visible projects (RLS-respected)
- Status badges (Active/Paused/Completed)
- Selection immediately creates link, closes popover

### 2.3 Linking Backend Integration
**Files to modify:**
- `ViewModels/TaskDetailViewModel.cs` — Add `ProjectId` property, `LinkToProjectCommand`, `UnlinkProjectCommand`
- `ViewModels/GoalDetailViewModel.cs` — Same pattern
- `ViewModels/MetricDetailViewModel.cs` — Same pattern

**Backend:**
- Already exists: `rpc_add_project_link`, `rpc_remove_project_link`
- Repository methods exist in `ProjectsRepository`

### 2.4 Update Entity Models
**Files to modify:**
- `Models/Task.cs` — Add `ProjectId?` and `ProjectTitle?` (for display)
- `Models/Goal.cs` — Same
- `Models/Metric.cs` — Same

---

## Phase 3: Project Completion Semantics

### 3.1 Status Dropdown in Project Detail Flyout
**Files to modify:**
- `Views/ProjectDetailView.axaml` — Add status dropdown (Active/Paused/Completed)
- `ViewModels/ProjectDetailViewModel.cs` — Bind `Status`, add `ChangeStatusCommand`

**Requirements:**
- Only owner can change status
- No wizard, no confirmation
- Immediate state change

### 3.2 Visual Treatment of Completed Projects
**Files to modify:**
- `Views/ProjectsView.axaml` — Style completed project cards as subdued

**CSS/Styles:**
- Muted colors for completed status
- Status badge visible

### 3.3 Ensure NO Cascading Effects
**Verification:**
- Confirm `rpc_update_project` does NOT touch linked items
- Already correct per schema review

---

## Phase 4: Project vs Chronicle Boundary

### 4.1 Chronicle Note Linking
**Files to modify:**
- `Views/ChronicleNoteView.axaml` — Add Project section (same pattern as Phase 2)
- `ViewModels/ChronicleNoteViewModel.cs` — Add linking commands

### 4.2 Project Detail Shows Chronicle Notes
**Files to modify:**
- `Views/ProjectDetailView.axaml` — Add "Notes" card showing linked Chronicle notes (titles only)
- `ViewModels/ProjectDetailViewModel.cs` — Fetch linked notes, display read-only list

**Requirements:**
- Titles only (no bodies)
- Clicking opens Chronicle note in its own flyout
- No inline editing

### 4.3 Boundary Enforcement (Code Review)
**Verification checklist:**
- ❌ No auto-creating Chronicle notes from Project changes
- ❌ No descriptions longer than a paragraph in Projects
- ❌ No Project activity feeds
- ❌ No Chronicle notes renaming Projects

---

## Phase 5: Projects in Briefing Rules

### 5.1 Add Project Label to Briefing Task Cards
**Files to modify:**
- `Views/BriefingView.axaml` — Add small project label to task cards
- `ViewModels/BriefingViewModel.cs` — Ensure tasks include `ProjectTitle` in fetch

**Requirements:**
- Label is secondary, non-interactive
- Visually quiet
- No navigation on click

### 5.2 Ensure NO Project Sections in Briefing
**Verification:**
- ❌ No "Projects due today" section
- ❌ No grouping by project
- ❌ No filtering by project
- ❌ No project progress indicators

---

## Phase 6: Owner Reassignment & Orphan Handling

### 6.1 Owner Reassignment UI
**Files to modify:**
- `Views/ProjectDetailView.axaml` — Show owner field, "Change owner" action (owner only)
- `ViewModels/ProjectDetailViewModel.cs` — Add `TransferOwnershipCommand`

**Selector:**
- Reuse `ProjectSelectorPopover` pattern for team member selection
- Or create `TeamMemberSelectorPopover`

**Backend:**
- ✅ Already deployed: `rpc_transfer_project_ownership`
- Add repository method: `ProjectsRepository.TransferOwnershipAsync()`

### 6.2 Orphaned Project Display
**Files to modify:**
- `Views/ProjectsView.axaml` — Show "Unassigned" with warning icon for orphaned projects
- `Views/ProjectDetailView.axaml` — Disable management actions, show message

**Requirements:**
- Orphaned = owner is inactive/deleted
- Project remains visible and readable
- All actions disabled except for admins

### 6.3 Admin Reclaim Flow
**Files to modify:**
- `Views/ProjectDetailView.axaml` — Show "Assign owner" for admins on orphaned projects
- `ViewModels/ProjectDetailViewModel.cs` — Admin-specific reclaim logic

**Backend:**
- May need new RPC: `rpc_admin_assign_project_owner` (bypasses owner check)
- Or extend `rpc_transfer_project_ownership` with admin override

### 6.4 Block Self-Removal If Owner
**Files to modify:**
- Team member removal flow (wherever that lives)
- Add validation: cannot leave org if you own projects

**Error message:**
> "You must assign a new owner for your projects before leaving."

---

## Phase 7: Polish & Verification

### 7.1 Empty State
**Files to modify:**
- `Views/ProjectsView.axaml` — Empty state with "Create your first project" CTA

### 7.2 Error Handling
**Requirements:**
- Inline validation errors in modals
- Backend permission errors surfaced clearly
- No retry loops, no partial saves

### 7.3 Final Boundary Tests
**Manual verification:**
- [ ] Projects never auto-complete based on linked work
- [ ] Linking does not change ownership/status/priority
- [ ] Briefing shows projects only as labels
- [ ] Chronicle and Projects remain separate surfaces
- [ ] Orphaned projects are recoverable

---

## Estimated Scope

| Phase | New Files | Modified Files | Complexity |
|-------|-----------|----------------|------------|
| 1. Creation Flow | 3 | 2 | Medium |
| 2. Linking UX | 3 | 8+ | High |
| 3. Completion | 0 | 2 | Low |
| 4. Chronicle Boundary | 0 | 4 | Medium |
| 5. Briefing Rules | 0 | 2 | Low |
| 6. Owner/Orphan | 1 | 3 | Medium |
| 7. Polish | 0 | 2 | Low |

**Total:** ~7 new files, ~20+ modified files

---

## Dependencies & Prerequisites

1. **`rpc_transfer_project_ownership`** — ✅ Already deployed to Supabase
2. **Entity models need `ProjectId`** — Tasks, Goals, Metrics need nullable FK
3. **Project links table** — ✅ Already exists (`procohere.project_links`)
4. **Admin role detection** — Need way to check if current user is org admin

---

## Recommended Order

1. **Phase 1** (Creation) — Foundation for all testing
2. **Phase 3** (Completion) — Simple, validates status model
3. **Phase 6.1** (Owner Reassignment) — Uses new RPC
4. **Phase 2** (Linking) — Most complex, depends on models
5. **Phase 4** (Chronicle) — Extends linking to notes
6. **Phase 5** (Briefing) — Display-only, low risk
7. **Phase 6.2-6.4** (Orphan) — Edge cases, can be last
8. **Phase 7** (Polish) — Final pass

---

## Open Questions

1. **Team Member Selector** — Reuse existing component or create new popover?
2. **Admin Detection** — Is there an existing `IsAdmin` property or RPC?
3. **Chronicle Notes** — Does `ChronicleNoteDetailView` exist, or is it embedded in Chronicle?
4. **Briefing Task Cards** — What's the current card component? Need to find it.

---

*Awaiting approval before implementation begins.*
