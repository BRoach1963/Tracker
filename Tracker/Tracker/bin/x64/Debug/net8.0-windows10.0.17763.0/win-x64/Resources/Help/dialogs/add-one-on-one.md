# Add/Edit 1:1 Meeting Dialog

This dialog is used to create and manage one-on-one meetings with team members.

## Accessing the Dialog

### Add New
- **Circle** → **1:1s** → **+ Add 1:1**
- From team member profile → **Schedule 1:1**

### Edit Existing
- Click 1:1 meeting → **Edit**
- Right-click → **Edit**

## Dialog Layout

The dialog uses a **two-column layout**:
- **Left Column**: Meeting details and settings
- **Right Column**: Agenda items and notes

## Left Column: Meeting Details

### Team Member Selection
Use the autocomplete field:
1. Start typing name
2. Select from suggestions
3. Or scroll dropdown

### Date & Time

| Field | Description |
|-------|-------------|
| **Date** | Meeting date (calendar picker) |
| **Start Time** | When meeting begins |
| **End Time** | When meeting ends |
| **Duration** | Auto-calculated |

### Meeting Settings

| Field | Options |
|-------|---------|
| **Status** | Scheduled, Completed, Cancelled |
| **Location** | Free text (office, video link, etc.) |
| **Recurring** | One-time or repeating |

### Quick Templates
Pre-fill with common meeting formats. The dropdown shows "Select a template..." when no template is selected.

**Available Templates**:
- **Weekly Check-in**: Standard recurring meeting structure
- **Career Development**: Focus on growth and goals
- **Performance Review**: Structured feedback discussion
- **Project Status**: Deep dive on project work
- **Custom**: Start from scratch

**To Apply**:
1. Select a template from the dropdown
2. Click **Apply**
3. Agenda items are added automatically
4. Customize as needed

## Right Column: Agenda & Notes

### Agenda Items

#### Adding Items
1. Click **+ Add Agenda Item**
2. Enter topic description
3. Select category (optional)
4. Set priority (optional)

#### Item Properties
- **Description**: What to discuss
- **Category**: Check-in, Priorities, Feedback, Career, etc.
- **Priority**: High, Medium, Low
- **Is Completed**: Check when resolved
- **Resolution**: What was decided

#### Linking Items
Connect agenda items to other entities for context. **You can link multiple items** to a single agenda topic:

1. Click the link icon (🔗) on any agenda item
2. Browse or search available items
3. Filter by type: Tasks, OKRs, KPIs, or Projects
4. Click an item to link it
5. Linked items appear as tags on the agenda item
6. **Click the link icon again** to add more links
7. Each linked item shows its type (e.g., "Task", "OKR")

**Why link multiple items?**
- Discuss related tasks together
- Connect an OKR with its supporting KPIs
- Review a project alongside its tasks
- Provide full context for complex topics

#### Removing Links
- Hover over a linked item tag
- Click the **×** to remove that link
- Other links remain intact

#### Deleting Items
- Click trash icon on the item
- Or select and press `Delete`

### Meeting Notes

#### Rich Text Editor
Format your notes:
- **Bold**: `Ctrl+B`
- **Italic**: `Ctrl+I`
- **Underline**: `Ctrl+U`
- Bullet lists
- Numbered lists
- Font size/family options

#### Auto-Save
Notes save automatically:
- Every few seconds
- On dialog close
- No manual save needed

## Dialog Workflow

### New Meeting
1. Select team member
2. Set date and time
3. Choose status (usually "Scheduled")
4. Add agenda items
5. Save

### Completing a Meeting
1. Open existing scheduled meeting
2. Add notes during/after meeting
3. Mark agenda items as completed
4. Add resolutions to items
5. Change status to "Completed"
6. Save

### Cancelling a Meeting
1. Open the meeting
2. Change status to "Cancelled"
3. Optionally add reason in notes
4. Save

## Validation

### Required Fields
- Team member must be selected
- Date must be set
- Time must be set

### Warnings
- Meeting in the past (for "Scheduled" status)
- No agenda items added
- Overlapping with another meeting

## Dialog Actions

| Button | Action |
|--------|--------|
| **Save** | Save changes and close |
| **Save & New** | Save and open new blank meeting |
| **Cancel** | Discard changes and close |
| **Delete** | Remove meeting (edit mode) |

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+S` | Save |
| `Escape` | Cancel |
| `Ctrl+Enter` | Save and close |
| `Tab` | Next field |

## Tips

### Preparation Best Practices
1. Open dialog day before meeting
2. Review previous meeting notes
3. Add 3-5 agenda items
4. Prioritize most important items first

### During the Meeting
1. Open dialog or have notes ready
2. Work through agenda items
3. Mark completed as you go
4. Add resolution notes
5. Capture action items

### After the Meeting
1. Complete any remaining notes
2. Mark all items as resolved/completed
3. Change status to "Completed"
4. Create follow-up tasks
5. Schedule next meeting if not recurring
