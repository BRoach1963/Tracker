# ProCohere RLS Context for AI Assistants

> **Purpose**: This document provides essential context for AI coding assistants working on the ProCohere application. Include this in any new thread/session working with data access.

---

## Database Overview

You are working against the **ProCohere PostgreSQL database** (Supabase-hosted).

This database is fully protected by **Row Level Security (RLS)** and all authorization is enforced in the database, **not in the application**.

---

## Core Assumptions (NEVER Violate)

1. You will **never bypass RLS**
2. You will **never re-implement authorization logic in C#**
3. If data is returned, it is **already authorized**
4. If data is missing, the database is **correctly denying access**

---

## Identity Model (Critical)

| Concept | Details |
|---------|---------|
| `auth.users.id` | The canonical user identity (Supabase auth) |
| `public.users.id` | Same UUID as `auth.users.id` |
| Team membership | Each authenticated user maps to exactly one `procohere.team_members` row per organization |

### Database Context Resolution

The database resolves context automatically via:
- `get_current_organization_id()` → Current org
- `get_current_team_member_id()` → Current team member

**You do NOT pass `organization_id` or `team_member_id` from the app unless explicitly required for writes.**

Reads rely on RLS context only.

---

## Management Visibility Rules (Guaranteed by DB)

A user can see:
- ✅ Their own data
- ✅ Data owned by direct and indirect reports
- ✅ Shared or non-private team data when explicitly allowed

This includes:
- Tasks assigned to subordinates
- Goals owned by subordinates
- Metrics owned by subordinates
- Feedback involving the user or their reports
- Meetings they are an attendee of
- Meeting artifacts scoped to meetings they attend

### Visibility Functions

Visibility is computed centrally via:
```sql
rls_is_visible_team_member(team_member_id)
get_rls_visible_team_member_ids(...)
```

**⚠️ You must NEVER attempt to calculate hierarchy or visibility in C#.**

---

## RLS Enforcement Model

All core tables have:
```sql
ENABLE ROW LEVEL SECURITY
FORCE ROW LEVEL SECURITY
```

### RLS-Protected Tables (Safe to Query Directly)

| Table | Notes |
|-------|-------|
| `tasks` | Assigned tasks visible to assignee + manager chain |
| `meetings` | Visible if attendee |
| `meeting_attendees` | Scoped to visible meetings |
| `meeting_agenda_items` | Scoped to visible meetings |
| `meeting_notes` | Scoped to visible meetings |
| `notes` | Owner + visibility rules |
| `feedback` | Involving user or their reports |
| `goals` | Owner + manager chain visibility |
| `targets` | Tied to goal visibility |
| `metrics` | Owner + manager chain visibility |
| `metric_values` | Tied to metric visibility |

---

## Application Query Rules

### When Querying (SELECT)

✅ **DO:**
- Use simple SELECT statements
- Filter only by **business meaning**, not security
- Example: "tasks assigned to me", not "tasks I am allowed to see"
- Assume RLS already removed unauthorized rows

❌ **DON'T:**
- JOIN in ways that attempt to bypass ownership
- Add WHERE clauses for security filtering
- Pass `organization_id` for reads

### When Inserting or Updating

✅ **DO:**
- Provide only required ownership fields (e.g., `created_by`)
- Let the database reject invalid writes automatically

❌ **DON'T:**
- Manually check permissions before writes
- Add retry logic for permission errors

---

## Error Interpretation Rules

| Scenario | Meaning | Action |
|----------|---------|--------|
| Empty result set | RLS working correctly | Handle as empty state in UI |
| Permission error | Write not allowed | Show user-friendly error, don't retry |
| Missing data | User lacks visibility | Don't add fallback queries |

**Never:**
- Log or expose internal authorization details
- Treat empty results as bugs
- Add "check access" retry logic

---

## What to Ask (If Needed)

If data is missing or unexpected, ask:
- "What is the expected ownership or visibility rule for this entity?"
- "Is this record owned, shared, or private?"

**Never ask:**
- "Should we loosen RLS?"
- "Can we pass organization_id manually?"
- "Can we add a bypass flag?"

---

## Final Rule

> **Treat the database as the final authority.**

The app's job is:
1. **Authenticate** the user
2. **Request data** (simple queries)
3. **Display results** (whatever comes back)
4. **Handle empty states gracefully**

If RLS blocks something, the fix is in the database, not in C#.

---

## Quick Reference for Common Patterns

### Loading User's Tasks
```csharp
// CORRECT - simple query, RLS handles visibility
var tasks = await _taskRepository.GetAllAsync();

// WRONG - don't filter by org/team in code
var tasks = await _taskRepository.GetByOrganizationAsync(orgId); // NO!
```

### Loading Meeting Agenda Items
```csharp
// CORRECT - just query by meeting, RLS ensures access
var items = await _agendaRepository.GetByMeetingIdAsync(meetingId);

// WRONG - don't check if user is attendee first
if (await IsUserAttendee(meetingId)) { ... } // NO!
```

### Creating a New Task
```csharp
// CORRECT - provide business fields, DB handles ownership
var task = new Task {
    Title = "Review report",
    AssignedToTeamMemberId = teamMemberId,
    CreatedBy = currentUserId  // Only ownership field needed
};
await _taskRepository.CreateAsync(task);

// WRONG - don't pre-check permissions
if (await CanAssignTo(teamMemberId)) { ... } // NO!
```

---

## Version

- **Last Updated**: January 20, 2026
- **Applies To**: ProCohere.Avalonia, Tracker.Core data access
