# Foreign Key Relationships Audit

## Complete FK Dependency Map

### Root Entity (No FKs)
1. **TeamMember** - No foreign keys (root entity)

### Level 1 Dependencies (depend on TeamMember only)
2. **IndividualTask** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
3. **Project** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
4. **ObjectiveKeyResult (OKR)** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
5. **KeyPerformanceIndicator (KPI)** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property)
6. **OneOnOne** - `TeamMemberId` → `TeamMember.Id` (NULLABLE, shadow property)

### Level 2 Dependencies (depend on Level 1)
7. **Milestone** - `ProjectId` → `Project.ID` (REQUIRED, explicit property)
8. **Risk** - `ProjectId` → `Project.ID` (REQUIRED, explicit property)
9. **ActionItem** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property) - nested in OneOnOne
10. **FollowUpItem** - `OwnerId` → `TeamMember.Id` (REQUIRED, shadow property) - nested in OneOnOne
11. **KPI (nested in OKR)** - `OkrId` → `OKR.ObjectiveId` (NULLABLE, set automatically by EF Core)

### Level 3 Dependencies (depend on Level 2)
12. **DiscussionPoint** - `ActionItemId` → `ActionItem.Id` (NULLABLE, explicit property)
13. **Concern** - `ActionItemId` → `ActionItem.Id` (NULLABLE, explicit property)

### Junction Tables (many-to-many relationships)
14. **OneOnOneLinkedTask** - `OneOnOneId` → `OneOnOne.Id`, `TaskId` → `IndividualTask.Id`
15. **OneOnOneLinkedOkr** - `OneOnOneId` → `OneOnOne.Id`, `OkrId` → `OKR.ObjectiveId`
16. **OneOnOneLinkedKpi** - `OneOnOneId` → `OneOnOne.Id`, `KpiId` → `KPI.KpiId`
17. **ProjectTeamMembers** - Auto-generated join table (many-to-many)

## Seeding Order (Correct Dependency Order)

1. ✅ **TeamMembers** - Save first (root entity)
2. ✅ **Tasks** - Depend on TeamMembers (OwnerId FK)
3. ✅ **Projects** - Depend on TeamMembers (OwnerId FK)
   - ✅ **Milestones** - Nested in Projects, ProjectId set after Projects saved
   - ✅ **Risks** - Nested in Projects, ProjectId set after Projects saved
4. ✅ **OKRs** - Depend on TeamMembers (OwnerId FK) and Projects (ProjectId FK)
   - ✅ **KPIs (nested)** - Depend on TeamMembers (OwnerId FK) and OKRs (OkrId FK)
5. ✅ **Standalone KPIs** - Depend on TeamMembers (OwnerId FK)
6. ✅ **OneOnOnes** - Depend on TeamMembers (TeamMemberId FK)
   - ✅ **ActionItems** - Nested in OneOnOnes, OwnerId FK set
   - ✅ **FollowUpItems** - Nested in OneOnOnes, OwnerId FK set
7. ✅ **Link Items to Meetings** - Junction tables created after all entities exist

## Shadow Properties vs Explicit Properties

### Shadow Properties (set via Entry.Property)
- `Tasks.OwnerId`
- `Projects.OwnerId`
- `OKRs.OwnerId`
- `KPIs.OwnerId`
- `OneOnOnes.TeamMemberId`
- `ActionItems.OwnerId`
- `FollowUpItems.OwnerId`

### Explicit Properties (set directly)
- `Milestones.ProjectId`
- `Risks.ProjectId`
- `OKRs.ProjectId`
- `DiscussionPoints.ActionItemId`
- `Concerns.ActionItemId`

## Verification Checklist

- ✅ TeamMembers saved first with valid IDs
- ✅ All OwnerId shadow properties set after AddRange
- ✅ ProjectId on Milestones/Risks set after Projects saved
- ✅ ProjectId on OKRs verified/set
- ✅ Nested KPIs OwnerId set, OkrId set automatically
- ✅ OneOnOnes TeamMemberId set
- ✅ Nested ActionItems/FollowUpItems OwnerId set
- ✅ Junction tables created after all entities exist

## Potential Issues Fixed

1. ✅ Milestones/Risks ProjectId now set explicitly after Projects saved
2. ✅ OKR ProjectId verified/set if missing
3. ✅ All shadow properties set AFTER AddRange (entities tracked)
4. ✅ All nested entities handled correctly

