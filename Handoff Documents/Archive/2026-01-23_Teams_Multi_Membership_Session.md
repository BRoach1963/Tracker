# Multi-Team Membership Implementation - January 23, 2026

## Session Summary
Implemented multi-team membership support in ProCohere.Avalonia, allowing team members to belong to multiple teams.

**Final Status: ✅ BUILD SUCCEEDED - All changes compile**

---

## What Was Built

### 1. TeamMembership Model
**File**: [Models/Team.cs](ProCohere.Avalonia/Models/Team.cs)

Added `TeamMembership` class mapping to `procohere.team_memberships` table:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `OrganizationId` | `Guid` | FK to organizations |
| `TeamId` | `Guid` | FK to teams |
| `TeamMemberId` | `Guid` | FK to team_members |
| `Role` | `string` | 'member', 'lead', or 'viewer' |
| `IsDeleted` | `bool` | Soft delete flag |
| `CreatedAt` | `DateTime` | Creation time |
| `DeletedAt` | `DateTime?` | When deleted |
| `DeletedBy` | `Guid?` | Who deleted |

Constants: `TeamMembership.RoleMember`, `TeamMembership.RoleLead`, `TeamMembership.RoleViewer`

### 2. TeamMembershipService
**File**: [Services/TeamMembershipService.cs](ProCohere.Avalonia/Services/TeamMembershipService.cs)

New singleton service with methods:

| Method | Purpose |
|--------|---------|
| `GetMyTeamsAsync()` | Get teams current user belongs to |
| `GetTeamsForMemberAsync(Guid)` | Get teams for any member |
| `GetMembersForTeamAsync(Guid)` | Get members of a team |
| `AddMemberToTeamAsync(Guid, Guid, string)` | Add member to team |
| `RemoveMemberFromTeamAsync(Guid, Guid)` | Soft delete membership |
| `UpdateMembershipRoleAsync(Guid, Guid, string)` | Change member role |
| `IsCurrentUserTeamLeadAsync(Guid)` | Check if user is team lead |
| `GetTeamMemberDetailsAsync(Guid)` | Get member details for attendee population |

### 3. TeamService Updates
**File**: [Services/TeamService.cs](ProCohere.Avalonia/Services/TeamService.cs)

Added team management methods:

| Method | Purpose |
|--------|---------|
| `GetAllTeamsAsync()` | Get all teams with member counts |
| `GetTeamDetailAsync(Guid)` | Get team with members populated |
| `CreateTeamAsync(string, string?, Guid?, Guid?)` | Create new team |
| `UpdateTeamAsync(Guid, string, string?, Guid?)` | Update team name/description/lead |
| `DeleteTeamAsync(Guid)` | Soft delete team |

### 4. Meeting Creation Integration
**Files**: 
- [Views/Dialogs/EditMeetingDialog.axaml](ProCohere.Avalonia/Views/Dialogs/EditMeetingDialog.axaml)
- [Views/Dialogs/EditMeetingDialog.axaml.cs](ProCohere.Avalonia/Views/Dialogs/EditMeetingDialog.axaml.cs)

Added team picker for Team Meetings:
- New `TeamPickerComboBox` dropdown in Team Attendees section
- Shows teams the user belongs to with member counts
- When a team is selected, automatically selects all team members as attendees
- Still allows manual add/remove after auto-population

### 5. Documentation
**File**: [Wiki/Database/06s-teams-tables.md](Wiki/Database/06s-teams-tables.md)

Updated to include:
- `team_memberships` table documentation
- C# model reference
- Service usage examples
- Important notes on teams vs hierarchy vs projects

---

## Key Architecture Decisions

### 1. Membership via Join Table (NOT property on team_members)
- A team member can belong to **0..N teams**
- A team can have **0..N members**
- Membership is tracked in `procohere.team_memberships`, NOT as a column on `team_members`

### 2. Team Names Stored Only in teams.name
- Team name lives ONLY in `procohere.teams.name`
- Never store team name elsewhere (not denormalized to meetings or members)

### 3. Attendees ≠ Team Membership
- **Attendees** are meeting-scoped (who is invited to THIS meeting)
- **Team membership** is persistent (who belongs to the team)
- For team meetings, attendees are initially populated from membership but can be edited

### 4. Roles: member/lead/viewer
- `lead` - Can manage team membership
- `member` - Regular team member
- `viewer` - Can view team content but limited participation

---

## Database Schema (Must Be Applied)

The `team_memberships` table must exist in the database. See [Wiki/Features/Teams/18-teams.md](Wiki/Features/Teams/18-teams.md) for the full DDL including:
- Table creation
- Constraints (role check, unique active membership)
- Indexes
- RLS policies

---

## What's NOT Done Yet

### UI Not Yet Built
1. **Teams List View** - Page showing "My Teams" list
2. **Team Detail View** - Page showing team info, members roster
3. **Manage Members UI** - Add/remove members from team (only lead can do this)
4. **Create Team Dialog** - Dialog for creating new teams

### Not Integrated
1. Goals scoped to teams
2. Notes scoped to teams
3. AI context using team data

---

## Next Steps

1. **Verify database schema** - Ensure `procohere.team_memberships` table exists with correct schema
2. **Test meeting creation** - Create a Team Meeting and verify team picker works
3. **Build Teams UI** - Create TeamsView, TeamsViewModel, TeamDetailView

---

## Files Changed

| File | Change |
|------|--------|
| `Models/Team.cs` | Added `TeamMembership` class |
| `Services/TeamMembershipService.cs` | **NEW** - Service for membership operations |
| `Services/TeamService.cs` | Added team management methods |
| `Views/Dialogs/EditMeetingDialog.axaml` | Added team picker UI |
| `Views/Dialogs/EditMeetingDialog.axaml.cs` | Added team picker logic |
| `Wiki/Database/06s-teams-tables.md` | Updated documentation |
