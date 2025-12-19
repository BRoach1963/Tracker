# User Ownership Architecture

## New Entity: User (Manager/Logged-In User)

### Purpose
- Represents the logged-in manager/user who owns ALL data
- Windows username maps to User entity
- In enterprise DB, determines data isolation per user

---

## Ownership Model

### 1. **User** (Root Entity)
- **Owns:** Everything
- **Properties:** Id, Username, Email, DisplayName, IsActive

### 2. **TeamMember**
- **Owned by:** `UserId` → `User.Id` (REQUIRED)
- **Purpose:** Members of the User's team
- **Note:** TeamMember.Owner (TeamMember) is for task assignment, NOT ownership

### 3. **Projects**
- **Owned by:** `UserId` → `User.Id` (REQUIRED)
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who manages the project
- **Contains:**
  - Milestones (FK: `ProjectId`)
  - Risks (FK: `ProjectId`)
  - Many-to-many with TeamMembers

### 4. **OKRs (ObjectiveKeyResult)**
- **Owned by:** `UserId` → `User.Id` (REQUIRED)
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who owns the OKR
- **Belongs to:** `ProjectId` → `Project.ID` (REQUIRED - OKRs MUST belong to a Project)
- **Contains:** KPIs (FK: `OkrId`)

### 5. **KPIs (KeyPerformanceIndicator)**
- **Owned by:** `UserId` → `User.Id` (REQUIRED)
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who owns the KPI
- **Belongs to:** `OkrId` → `OKR.ObjectiveId` (NULLABLE - KPIs CAN be standalone OR nested in OKR)

### 6. **Tasks (IndividualTask)**
- **Owned by:** `UserId` → `User.Id` (REQUIRED)
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who the task is assigned to
- **Purpose:** "I (User) gave this task to TeamMember"

### 7. **1:1s (OneOnOne)**
- **Owned by:** `UserId` → `User.Id` (REQUIRED) - the manager/user conducting the meeting
- **With:** `TeamMemberId` → `TeamMember.Id` (REQUIRED) - who the meeting is with
- **Purpose:** "My (User's) 1:1 meeting with TeamMember"
- **Contains:**
  - ActionItems (standalone, FK: `OwnerId` → `TeamMember.Id`)
  - FollowUpItems (standalone, FK: `OwnerId` → `TeamMember.Id`)
  - DiscussionPoints (FK: `ActionItemId` → `ActionItem.Id`)
  - Concerns (FK: `ActionItemId` → `ActionItem.Id`)

### 8. **ActionItems**
- **Owned by:** `UserId` → `User.Id` (REQUIRED) - belongs to User's 1:1
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who owns the action item
- **Linked to:** `ActionItemId` → `ActionItem.Id` (NULLABLE) - for DiscussionPoints/Concerns

### 9. **FollowUpItems**
- **Owned by:** `UserId` → `User.Id` (REQUIRED) - belongs to User's 1:1
- **Assigned to:** `OwnerId` → `TeamMember.Id` (REQUIRED) - who owns the follow-up

### 10. **DiscussionPoints**
- **Owned by:** `UserId` → `User.Id` (REQUIRED) - belongs to User's 1:1
- **Linked to:** `ActionItemId` → `ActionItem.Id` (NULLABLE)

### 11. **Concerns**
- **Owned by:** `UserId` → `User.Id` (REQUIRED) - belongs to User's 1:1
- **Linked to:** `ActionItemId` → `ActionItem.Id` (NULLABLE)

---

## Key Relationships Summary

### What 1:1s Own:
- ActionItems (standalone entities, not FK-linked)
- FollowUpItems (standalone entities, not FK-linked)
- DiscussionPoints (FK: `ActionItemId`)
- Concerns (FK: `ActionItemId`)
- Links to Tasks/OKRs/KPIs (via junction tables)

### What Tasks Own:
- Nothing (they're assigned to TeamMembers)

### What Projects Own:
- Milestones (FK: `ProjectId`)
- Risks (FK: `ProjectId`)
- OKRs (many-to-many relationship)
- TeamMembers (many-to-many relationship)

---

## Implementation Plan

1. **Create User Entity**
   - Id, Username, Email, DisplayName, IsActive
   - Inherits from AuditableEntity

2. **Add UserId FK to ALL entities:**
   - TeamMember
   - Project
   - IndividualTask
   - ObjectiveKeyResult
   - KeyPerformanceIndicator
   - OneOnOne
   - ActionItem
   - FollowUpItem
   - DiscussionPoint
   - Concern
   - Milestone
   - Risk
   - ProjectDependency

3. **Update DbContext:**
   - Add DbSet<User>
   - Configure User entity
   - Add UserId FK configuration to all entities

4. **Update Seeding:**
   - Create default User from Windows username
   - Assign all sample data to that User

5. **Update Queries:**
   - Filter ALL queries by UserId
   - Get current User from UserSettingsManager

6. **Update Login:**
   - Create/Get User on login
   - Store User.Id in UserSettingsManager

