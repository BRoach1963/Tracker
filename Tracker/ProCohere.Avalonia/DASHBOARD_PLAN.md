# Pro Cohere Dashboard Implementation Plan

## Current State

### Data Available (Confirmed Working)
| Entity | Count | Table | RLS Status |
|--------|-------|-------|------------|
| Team Members | 8 | `team_members` | ✅ Working |
| Goals | 4 | `goals` | ✅ Working |
| Tasks | 8 | `tasks` | ✅ Working |
| Projects | 3 | `projects` | ✅ Working |
| Meetings | 0 | `meetings` | ⚠️ Needs `meeting_attendees` RLS fix |
| Metrics | 0 | `metrics` | Not seeded |
| Targets | 0 | `targets` | Not seeded |

### User Context
- **User ID:** `b0000000-0000-0000-0000-000000000000`
- **Auth ID:** `bb54d81c-5ca5-45b5-8502-927bbf23d7d4`
- **Role:** `manager` (default in users.role column)

---

## Correct Terminology

| ❌ Old/Wrong | ✅ Correct | Database Table |
|--------------|-----------|----------------|
| KPIs | Metrics | `metrics` |
| OKRs | Goals | `goals` |
| Key Results | Targets | `targets` |
| 1:1s | Meetings (type=one_on_one) | `meetings` |

---

## Phase 1: Manager Dashboard

### Layout Structure

```
┌─────────────────────────────────────────────────────────────┐
│  Today                                         [Refresh] 📅  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ 👥 Team  │ │ ✅ Tasks │ │ 🎯 Goals │ │ 📁 Active│       │
│  │    8     │ │   75%    │ │   50%    │ │ Projects │       │
│  │ members  │ │ complete │ │ on track │ │    3     │       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘       │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Team Overview                              Upcoming Tasks  │
│  ┌─────────────────────────────────┐  ┌──────────────────┐ │
│  │ Name      │ Tasks │ Goals │ ... │  │ □ Task 1 - Due   │ │
│  │───────────┼───────┼───────┼─────│  │ □ Task 2 - Due   │ │
│  │ Sarah J.  │   2   │   1   │     │  │ □ Task 3 - Due   │ │
│  │ Marcus C. │   3   │   0   │     │  │ □ Task 4 - Due   │ │
│  │ Emily R.  │   1   │   1   │     │  │                  │ │
│  │ ...       │       │       │     │  │                  │ │
│  └─────────────────────────────────┘  └──────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### KPI Cards (Top Row)
| Card | Label | Value Source | Color Logic |
|------|-------|--------------|-------------|
| Team Size | "Team Members" | `team_members` count | Always neutral |
| Tasks Done | "Tasks Complete" | % of tasks with status=completed | Green >75%, Amber 50-75%, Red <50% |
| Goals On Track | "Goals On Track" | % of goals with status=on_track | Green >75%, Amber 50-75%, Red <50% |
| Active Projects | "Active Projects" | `projects` where status=in_progress | Always neutral |

### Team Overview Table
| Column | Source | Notes |
|--------|--------|-------|
| Avatar/Initials | Computed from first_name, last_name | Colored circle |
| Name | `first_name` + `last_name` | Full name |
| Job Title | `job_title` | From team_members |
| Open Tasks | `open_task_count` | Pre-computed field |
| Active Goals | `active_goal_count` | Pre-computed field |
| Last Meeting | `last_meeting_date` | "3d ago", "Never", etc. |

### Upcoming Tasks Section
- Tasks due within next 7 days
- Sorted by due_date ascending
- Shows: checkbox, title, due date, assignee name
- Max 5-10 items with "View All" link

---

## Files to Create/Modify

### New Files
| File | Purpose |
|------|---------|
| `Models/TeamMemberDetail.cs` | Full team member model for dashboard |
| `Models/TaskDetail.cs` | Task with owner info |
| `Models/GoalDetail.cs` | Goal with status/progress |
| `Services/DashboardService.cs` | Dashboard data queries |

### Files to Modify
| File | Changes |
|------|---------|
| `ViewModels/TodayViewModel.cs` | Replace placeholder with full dashboard VM |
| `Views/TodayView.axaml` | Replace placeholder with dashboard UI |
| `Models/UserProfile.cs` | Add `Role` property |

---

## Data Models (Postgrest)

### TeamMemberDetail
```csharp
[Table("team_members")]
public class TeamMemberDetail : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("first_name")] public string FirstName { get; set; }
    [Column("last_name")] public string LastName { get; set; }
    [Column("job_title")] public string? JobTitle { get; set; }
    [Column("email")] public string Email { get; set; }
    [Column("avatar_url")] public string? AvatarUrl { get; set; }
    [Column("open_task_count")] public int OpenTaskCount { get; set; }
    [Column("active_goal_count")] public int ActiveGoalCount { get; set; }
    [Column("last_meeting_date")] public DateTime? LastMeetingDate { get; set; }
    [Column("manager_user_id")] public Guid? ManagerUserId { get; set; }
    [Column("is_active")] public bool IsActive { get; set; }
}
```

### TaskDetail
```csharp
[Table("tasks")]
public class TaskDetail : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("title")] public string Title { get; set; }
    [Column("status")] public string Status { get; set; }
    [Column("priority")] public string? Priority { get; set; }
    [Column("due_date")] public DateTime? DueDate { get; set; }
    [Column("owner_team_member_id")] public Guid? OwnerTeamMemberId { get; set; }
    [Column("created_by_user_id")] public Guid CreatedByUserId { get; set; }
}
```

### GoalDetail
```csharp
[Table("goals")]
public class GoalDetail : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("title")] public string Title { get; set; }
    [Column("status")] public string Status { get; set; }
    [Column("progress_percent")] public int? ProgressPercent { get; set; }
    [Column("owner_team_member_id")] public Guid? OwnerTeamMemberId { get; set; }
    [Column("created_by_user_id")] public Guid CreatedByUserId { get; set; }
}
```

---

## Role-Based Views (Future)

### Manager View (Phase 1 - Current Focus)
- Team health overview
- Team members list with status
- Tasks assigned to team
- Goals owned by team

### Team Member View (Phase 2 - Later)
- Personal task list
- My goals and progress
- Upcoming meetings with manager
- Quick actions (add task, update goal)

---

## SQL Fixes Needed

### Fix Meetings RLS
```sql
GRANT SELECT ON meeting_attendees TO authenticated;

CREATE POLICY "Users can read meeting attendees" ON meeting_attendees
    FOR SELECT
    USING (true); -- Or more restrictive based on meeting ownership
```

---

## Implementation Order

1. ✅ Database connectivity verified
2. ✅ Create data models (`TeamMemberDetail`, `TaskDetail`, `GoalDetail`, `DashboardStats`)
3. ✅ Create `DashboardService.cs` with query methods
4. ✅ Build `TodayViewModel.cs` with:
   - Stats properties (team count, task %, goal %, project count)
   - TeamMembers collection
   - UpcomingTasks collection
   - LoadDataAsync() method
5. ✅ Build `TodayView.axaml` with:
   - KPI cards row
   - Team overview table
   - Upcoming tasks list
6. ⬜ Test and iterate
7. ✅ Remove debug database card

---

## Status

**Plan Created:** January 16, 2026  
**Implementation Started:** January 16, 2026  
**Status:** Phase 1 complete - ready for testing  
**Next Step:** Test with real data, iterate on UI
