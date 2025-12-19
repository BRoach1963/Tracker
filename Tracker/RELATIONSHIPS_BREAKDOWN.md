# Entity Relationships Breakdown

## Current Architecture

### **NO User/Account Entity Exists**
- There is **no User or Account table** in the database
- Authentication is handled via `UserSettingsManager.Instance.CurrentUser` (just a string username)
- Audit fields (`CreatedBy`, `LastModifiedBy`) are **strings**, not foreign keys to a User table
- **All data is owned by TeamMembers, NOT by a logged-in user account**

---

## Entity Ownership & Relationships

### 1. **TeamMember** (Root Entity)
- **No foreign keys** - This is the root entity
- **Owned by:** N/A (root entity)
- **Owns:** Everything else references TeamMember

### 2. **Tasks (IndividualTask)**
- **Owner:** `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
- **Owned by:** A TeamMember (not the logged-in user)
- **Relationships:**
  - Can be linked to OneOnOne meetings via `OneOnOneLinkedTask` junction table

### 3. **Projects**
- **Owner:** `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
- **Owned by:** A TeamMember (not the logged-in user)
- **Relationships:**
  - Contains `Milestones` (FK: `ProjectId` → `Project.ID`)
  - Contains `Risks` (FK: `ProjectId` → `Project.ID`)
  - Many-to-many with `TeamMembers` (via join table)
  - Many-to-many with `OKRs` (via navigation property)

### 4. **OKRs (ObjectiveKeyResult)**
- **Owner:** `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
- **Project:** `ProjectId` → `Project.ID` (REQUIRED, explicit property)
- **Owned by:** A TeamMember (not the logged-in user)
- **Relationships:**
  - Contains nested `KPIs` (FK: `OkrId` → `OKR.ObjectiveId`)
  - Linked to Projects (many-to-many)
  - Can be linked to OneOnOne meetings via `OneOnOneLinkedOkr` junction table

### 5. **KPIs (KeyPerformanceIndicator)**
- **Owner:** `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
- **OKR (if nested):** `OkrId` → `OKR.ObjectiveId` (NULLABLE, explicit property)
- **Owned by:** A TeamMember (not the logged-in user)
- **Relationships:**
  - Can be nested in OKRs
  - Can be standalone (not linked to OKR)
  - Can be linked to OneOnOne meetings via `OneOnOneLinkedKpi` junction table

### 6. **1:1s (OneOnOne)**
- **TeamMember:** `TeamMemberId` → `TeamMember.Id` (NULLABLE but should be set, shadow property)
- **Owned by:** A TeamMember (the person the meeting is with, not the logged-in user)
- **Relationships:**
  - Contains `ActionItems` (standalone entities, FK: `OwnerId` → `TeamMember.Id`)
  - Contains `FollowUpItems` (standalone entities, FK: `OwnerId` → `TeamMember.Id`)
  - Contains `DiscussionPoints` (FK: `ActionItemId` → `ActionItem.Id`)
  - Contains `Concerns` (FK: `ActionItemId` → `ActionItem.Id`)
  - Can link to Tasks via `OneOnOneLinkedTask` junction table
  - Can link to OKRs via `OneOnOneLinkedOkr` junction table
  - Can link to KPIs via `OneOnOneLinkedKpi` junction table

---

## Critical Issue: No User-Level Ownership

### The Problem
**You stated:** "All of these are owned by the user who is logged in"

**Current Reality:**
- ❌ There is **NO User/Account entity** in the database
- ❌ Everything is owned by **TeamMembers**, not by a logged-in user
- ❌ Multiple users could see/edit each other's data if sharing the same database
- ❌ No data isolation between different logged-in users

### What This Means
1. **If you want user-level ownership**, you need to:
   - Create a `User` or `Account` entity
   - Add `UserId` foreign key to ALL entities (TeamMember, Task, Project, OKR, KPI, OneOnOne, etc.)
   - Filter all queries by `UserId` to show only the logged-in user's data

2. **Current FK constraint failures** are likely NOT related to missing User ownership because:
   - All FK relationships are to `TeamMember`, which we're creating correctly
   - The issue is likely in how we're setting shadow properties or the order of operations

### Recommendation
**If user-level ownership is required:**
1. Add a `User` entity (Id, Username, Email, etc.)
2. Add `UserId` FK to all entities
3. Update seeding to create a default user and assign all sample data to that user
4. Update all queries to filter by `UserId`

**If user-level ownership is NOT required** (single-user app):
- Current architecture is fine
- The FK constraint failures are a separate issue related to shadow property handling

---

## Current FK Constraint Issue

The FK constraint failures are happening because:
1. Shadow properties (`OwnerId`, `TeamMemberId`) need to be set **after** `AddRange` but **before** `SaveChangesAsync`
2. Entities need to reference **tracked** TeamMember instances from the DbContext
3. The order of operations matters (save OneOnOnes first, then ActionItems/FollowUpItems separately)

This is **NOT** related to missing User ownership - it's a technical EF Core shadow property issue.

