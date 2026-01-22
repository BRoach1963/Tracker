# 06 – Repositories (Query Reference)

This document lists **all repositories** in Tracker.Core and their query methods.

Every repository follows the pattern:
- Interface: `I{Entity}Repository : IRepository<Entity>`
- Implementation: `{Entity}Repository : BaseRepository<Entity>, I{Entity}Repository`

---

## MeetingRepository

**File:** `Data/Repositories/MeetingRepository.cs`  
**Table:** `meetings`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByUserAsync(userId)` | Meetings where user is organizer or attendee |
| `GetByDateRangeAsync(start, end)` | Meetings within date range |
| `GetUpcomingByUserAsync(userId, fromDate)` | Future meetings for user |
| `GetPastByUserAsync(userId, upToDate)` | Past meetings for user |
| `GetByStatusAsync(status)` | Filter by status |
| `GetByOrganizerAsync(organizerId)` | Meetings organized by user |
| `GetByOneOnOneAsync(oneOnOneId)` | Meetings for a 1:1 relationship |
| `GetAttendeeIdsAsync(meetingId)` | Get all attendee user IDs |
| `CountByOrganizationInDateRangeAsync(orgId, start, end)` | Count for analytics |
| `GetByOrganizationAsync(orgId)` | All org meetings |
| `GetMeetingsAsync()` | All non-deleted meetings |
| `UpdateMeetingAsync(meeting)` | Full meeting update |
| `DeleteMeetingAsync(meetingId)` | Soft delete |
| `FindMeetingByCalendarEventIdAsync(eventId)` | Find by external calendar ID |
| `GetMeetingByIdAsync(meetingId)` | Single meeting lookup |
| `UpdateMeetingSyncDataAsync(meetingId, eventId, syncedAt)` | Update sync metadata |
| `GetMeetingsForTeamMemberAsync(teamMemberId)` | Meetings for team member |

### Key SQL Patterns

**User meetings with attendee join:**
```sql
SELECT DISTINCT m.* FROM meetings m
LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
  AND m.is_deleted = false
ORDER BY m.scheduled_at DESC
```

---

## GoalRepository

**File:** `Data/Repositories/GoalRepository.cs`  
**Table:** `goals`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOwnerAsync(ownerTeamMemberId)` | Goals owned by team member |
| `GetByOrganizationAsync(orgId)` | All org goals |
| `GetActiveGoalsAsync(orgId)` | Non-completed goals |
| `GetByParentGoalAsync(parentGoalId)` | Child goals |
| `GetRootGoalsAsync(orgId)` | Top-level goals (no parent) |
| `GetWithTargetsAsync(goalId)` | Goal with its key results |

---

## MetricRepository

**File:** `Data/Repositories/MetricRepository.cs`  
**Table:** `metrics`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOwnerAsync(ownerTeamMemberId)` | Metrics owned by team member |
| `GetByOrganizationAsync(orgId)` | All org metrics |
| `GetActiveMetricsAsync(orgId)` | Non-archived metrics |
| `GetByGoalAsync(goalId)` | Metrics linked to a goal |

---

## TargetRepository

**File:** `Data/Repositories/TargetRepository.cs`  
**Table:** `targets`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByGoalAsync(goalId)` | Key results for a goal |
| `GetActiveTargetsAsync(goalId)` | Non-completed key results |

---

## TaskRepository

**File:** `Data/Repositories/TaskRepository.cs`  
**Table:** `tasks`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOwnerAsync(ownerTeamMemberId)` | Tasks assigned to team member |
| `GetByProjectAsync(projectId)` | Project tasks |
| `GetByGoalAsync(goalId)` | Goal-linked tasks |
| `GetByMeetingAsync(meetingId)` | Meeting action items |
| `GetOverdueAsync(userId)` | Overdue tasks |
| `GetDueTodayAsync(userId)` | Tasks due today |
| `GetSubtasksAsync(parentTaskId)` | Child tasks |

---

## TeamMemberRepository

**File:** `Data/Repositories/TeamMemberRepository.cs`  
**Table:** `team_members`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOrganizationAsync(orgId)` | All org members |
| `GetByManagerAsync(managerUserId)` | Direct reports |
| `GetByLinkedUserAsync(userId)` | Team member with login |
| `GetActiveAsync(orgId)` | Non-terminated members |
| `SearchAsync(orgId, query)` | Name/email search |

---

## UserRepository

**File:** `Data/Repositories/UserRepository.cs`  
**Table:** `users`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByEmailAsync(email)` | Lookup by email |
| `GetByOrganizationAsync(orgId)` | All org users |
| `GetByAuthIdAsync(authId)` | Lookup by Supabase auth ID |

---

## ProjectRepository

**File:** `Data/Repositories/ProjectRepository.cs`  
**Table:** `projects`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOrganizationAsync(orgId)` | All org projects |
| `GetActiveAsync(orgId)` | Non-completed/archived projects |
| `GetByOwnerAsync(ownerTeamMemberId)` | Projects owned by member |

---

## FeedbackRepository

**File:** `Data/Repositories/FeedbackRepository.cs`  
**Table:** `feedback`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByRecipientAsync(toMemberId)` | Feedback received |
| `GetBySenderAsync(fromMemberId)` | Feedback given |
| `GetByTypeAsync(feedbackType)` | Filter by type |

---

## KudosRepository

**File:** `Data/Repositories/KudosRepository.cs`  
**Table:** `kudos`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByRecipientAsync(toMemberId)` | Kudos received |
| `GetBySenderAsync(fromMemberId)` | Kudos given |
| `GetPublicAsync(orgId)` | Public kudos for org |
| `GetRecentAsync(orgId, count)` | Recent kudos |

---

## QuickNoteRepository

**File:** `Data/Repositories/QuickNoteRepository.cs`  
**Table:** `quick_notes`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByTeamMemberAsync(teamMemberId)` | Notes about a person |
| `GetByCreatorAsync(createdByUserId)` | Notes created by user |
| `GetRecentAsync(userId, count)` | Recent notes |

---

## ReminderRepository

**File:** `Data/Repositories/ReminderRepository.cs`  
**Table:** `reminders`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOwnerAsync(ownerId)` | User's reminders |
| `GetPendingAsync(ownerId)` | Undismissed reminders |
| `GetDueAsync(beforeDate)` | Reminders due by date |

---

## InsightRepository

**File:** `Data/Repositories/InsightRepository.cs`  
**Table:** `insights`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByTeamMemberAsync(teamMemberId)` | AI insights for member |
| `GetUndismissedAsync(teamMemberId)` | Active insights |
| `GetByTypeAsync(insightType)` | Filter by type |

---

## PulseSurveyRepository

**File:** `Data/Repositories/PulseSurveyRepository.cs`  
**Table:** `pulse_surveys`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOrganizationAsync(orgId)` | Org surveys |
| `GetActiveAsync(orgId)` | Active surveys |
| `GetByRespondentAsync(teamMemberId)` | Surveys for member |

---

## MeetingTemplateRepository

**File:** `Data/Repositories/MeetingTemplateRepository.cs`  
**Table:** `meeting_templates`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOrganizationAsync(orgId)` | Org templates |
| `GetByTypeAsync(meetingType)` | Templates by meeting type |

---

## DevelopmentGoalRepository

**File:** `Data/Repositories/DevelopmentGoalRepository.cs`  
**Table:** `development_goals`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByTeamMemberAsync(teamMemberId)` | Member's dev goals |
| `GetActiveAsync(teamMemberId)` | Non-completed dev goals |

---

## TaskCollectionRepository

**File:** `Data/Repositories/TaskCollectionRepository.cs`  
**Table:** `task_collections`

### Interface Methods

| Method | Purpose |
|--------|---------|
| `GetByOwnerAsync(ownerTeamMemberId)` | Task lists for member |
| `GetWithItemsAsync(collectionId)` | Collection with tasks |

---

## Common Query Patterns

### Organization-Scoped
```sql
SELECT * FROM {table}
WHERE organization_id = @OrgId AND is_deleted = false
```

### Owner-Scoped
```sql
SELECT * FROM {table}
WHERE owner_team_member_id = @OwnerId AND is_deleted = false
```

### Date-Range Query
```sql
SELECT * FROM {table}
WHERE created_at >= @Start AND created_at <= @End AND is_deleted = false
```

### Search Query
```sql
SELECT * FROM team_members
WHERE organization_id = @OrgId
  AND (first_name ILIKE @Query OR last_name ILIKE @Query OR email ILIKE @Query)
  AND is_deleted = false
```

---

## Adding New Repository Methods

1. Add method to interface
2. Implement in repository class
3. Follow soft-delete pattern (`AND is_deleted = false`)
4. Use parameterized queries
5. Add structured logging
6. Update this document

---

## Invariants

1. All queries filter `is_deleted = false`
2. All queries use parameters (no string concatenation)
3. All methods are async
4. All methods log errors with context
5. No business logic in repositories

