# Tasks

Tasks are the atomic units of work in Tracker. They represent individual action items that need to be completed.

## Task Types

### Standalone Tasks
- Independent work items
- Not linked to any project
- Quick to create and track
- Ideal for ad-hoc requests

### Project Tasks
- Part of a larger initiative
- Contribute to project progress
- Share project context
- Inherit project deadlines

## Creating a Task

### Quick Add
1. Go to **Pulse** → **Tasks**
2. Click **+ Add Task**
3. Enter description
4. Set due date
5. Assign owner
6. Save

### Full Task Details

| Field | Description | Required |
|-------|-------------|----------|
| Description | What needs to be done | Yes |
| Due Date | When it's needed | Recommended |
| Owner | Who's responsible | Recommended |
| Priority | Urgency level | Optional |
| Status | Current state | Auto-set |
| Notes | Additional context | Optional |
| Project | Parent project | Optional |

## Task Status Workflow

```
┌─────────┐    ┌─────────────┐    ┌───────────┐
│ Not     │───▶│ In Progress │───▶│ Completed │
│ Started │    └─────────────┘    └───────────┘
└─────────┘           │
                      ▼
               ┌──────────┐
               │ On Hold  │
               └──────────┘
```

### Status Definitions

| Status | Meaning |
|--------|---------|
| **Not Started** | Created but work hasn't begun |
| **In Progress** | Actively being worked on |
| **On Hold** | Paused (blocked or deprioritized) |
| **Completed** | Done and verified |

## Priority Levels

| Priority | Use Case | Visual |
|----------|----------|--------|
| **Critical** | Urgent, time-sensitive | 🔴 Red |
| **High** | Important, needs attention | 🟠 Orange |
| **Medium** | Normal priority | 🟡 Yellow |
| **Low** | Nice to have | 🟢 Green |

## Due Dates & Reminders

### Setting Due Dates
- Click the date field
- Use the calendar picker
- Or type: "tomorrow", "next week", "Dec 15"

### Overdue Handling
- Tasks turn red when past due
- Appear in Dashboard alerts
- Counted in overdue metrics

### Recurring Tasks
For repeating work:
1. Create the task
2. Mark as recurring
3. Set frequency (daily, weekly, monthly)
4. New instance auto-creates on completion

## Task Views

### List View
Sortable table with all task details:
- Click column headers to sort
- Drag to reorder columns
- Multi-select for bulk actions

### Kanban Board
Visual card-based view:
- Columns by status
- Drag cards to change status
- Quick visual of work distribution

### Calendar View
Tasks plotted by due date:
- See workload distribution
- Identify scheduling conflicts
- Plan capacity

## Filtering & Searching

### Quick Filters
- **My Tasks**: Only tasks I own
- **Overdue**: Past due date
- **Due This Week**: Next 7 days
- **Completed**: Done tasks

### Advanced Filters
Combine multiple criteria:
- Owner
- Status
- Priority
- Due date range
- Project
- Keywords

## Task Relationships

### Linking to Agenda Items
Connect tasks to 1:1 discussions:
1. Open the task
2. Click "Link to Agenda"
3. Select a meeting and item
4. Track discussion history

### Linking to OKRs
Show task contribution to goals:
1. Open the task
2. Click "Link to OKR"
3. Select Key Result
4. Progress updates reflect

## Bulk Operations

Select multiple tasks to:
- **Change status** - Move to In Progress, Complete
- **Reassign** - New owner
- **Change priority** - Bulk reprioritize
- **Delete** - Remove selected
- **Export** - CSV download

## Task Metrics

Track task performance:
- **Completion rate**: Tasks done vs. created
- **On-time rate**: Completed by due date
- **Average cycle time**: Created to completed
- **Backlog size**: Open tasks

## Best Practices

### Writing Good Task Descriptions
✅ Good: "Review Q4 budget proposal and provide feedback by Friday"
❌ Bad: "Budget stuff"

Tips:
- Start with an action verb
- Be specific about deliverable
- Include context if needed
- Set realistic due dates

### Task Hygiene
- Review tasks weekly
- Close completed tasks promptly
- Update status as work progresses
- Don't let tasks go stale

### Delegation Best Practices
- Ensure owner has capacity
- Provide context and resources
- Set clear expectations
- Follow up appropriately

## Integration with Other Features

| Feature | Integration |
|---------|-------------|
| **Projects** | Tasks roll up to project progress |
| **1:1s** | Discuss tasks in meetings |
| **OKRs** | Tasks contribute to Key Results |
| **Dashboard** | Task summary and alerts |
| **Reports** | Task completion analytics |
