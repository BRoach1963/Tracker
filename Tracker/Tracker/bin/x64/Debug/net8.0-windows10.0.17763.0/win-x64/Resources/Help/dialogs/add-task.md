# Add/Edit Task Dialog

This dialog creates and manages individual tasks.

## Accessing the Dialog

### Add New
- **Pulse** → **Tasks** → **+ Add Task**
- Within project → **+ Add Task**
- Keyboard: `Ctrl+N` on Tasks view

### Edit Existing
- Click task → **Edit**
- Right-click → **Edit**
- Double-click task

## Dialog Fields

### Core Fields

| Field | Required | Description |
|-------|----------|-------------|
| **Description** | Yes | What needs to be done |
| **Due Date** | Recommended | When it should be completed |
| **Owner** | Recommended | Who is responsible |
| **Priority** | Optional | Urgency level |
| **Status** | Auto | Current state |

### Description
Write clear, actionable task descriptions:
- Start with a verb
- Be specific about deliverable
- Include context if needed

**Examples**:
✅ "Review Q4 budget proposal and send feedback to Sarah"
❌ "Budget"

### Due Date
- Use calendar picker
- Or type: "tomorrow", "next Friday", "Dec 15"
- Leave blank for no deadline

### Owner
- Select from team members
- Can be yourself
- Tasks appear in their view

### Priority

| Level | Use Case | Visual |
|-------|----------|--------|
| **Critical** | Must do immediately | 🔴 |
| **High** | Important, needs attention | 🟠 |
| **Medium** | Normal priority | 🟡 |
| **Low** | Nice to have | 🟢 |

### Status

| Status | Description |
|--------|-------------|
| **Not Started** | Created, work not begun |
| **In Progress** | Actively working |
| **On Hold** | Paused/blocked |
| **Completed** | Done |

### Additional Fields

#### Notes
Add context, details, or instructions:
- Supporting information
- Links and references
- Acceptance criteria

#### Project
Optionally assign to a project:
1. Toggle "Part of Project"
2. Select project from dropdown
3. Task contributes to project progress

## Task Linking

### Link to 1:1 Agenda
Connect task to meeting discussion:
1. Click **Link to 1:1**
2. Select meeting
3. Task appears in meeting context

### Link to OKR
Show contribution to objectives:
1. Click **Link to OKR**
2. Select Key Result
3. Task completion updates OKR

## Dialog Actions

| Button | Action |
|--------|--------|
| **Save** | Save and close |
| **Save & New** | Save and create another |
| **Cancel** | Discard and close |
| **Delete** | Remove task (edit mode) |

## Quick Entry Tips

### Fastest Path
1. Open dialog (`Ctrl+N`)
2. Type description
3. Tab to set due date
4. Tab to select owner
5. Enter to save

### Bulk Creation
Use **Save & New** to rapidly create multiple tasks.

## Validation

- Description required (cannot be empty)
- Due date must be valid if set
- Owner must exist if selected

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+S` | Save |
| `Escape` | Cancel |
| `Enter` | Save (in some fields) |
| `Tab` | Next field |

## Best Practices

### Writing Tasks
- One action per task
- Clear completion criteria
- Appropriate granularity (not too big, not too small)

### Setting Due Dates
- Be realistic
- Include buffer for review
- Consider dependencies

### Assignment
- Ensure owner has capacity
- Provide context
- Set appropriate priority
