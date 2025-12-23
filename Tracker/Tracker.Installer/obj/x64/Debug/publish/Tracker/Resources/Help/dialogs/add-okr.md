# Add/Edit OKR Dialog

This dialog creates and manages Objectives & Key Results.

## Understanding the OKR Dialog

The dialog is designed around the OKR structure:
- **One Objective** (the qualitative goal)
- **Multiple Key Results** (measurable outcomes)

## Accessing the Dialog

### Add New
- **Pulse** → **OKRs** → **+ Add OKR**

### Edit Existing
- Click OKR → **Edit**
- Right-click → **Edit**

## Objective Section

### Core Fields

| Field | Required | Description |
|-------|----------|-------------|
| **Title** | Yes | The objective statement |
| **Description** | Optional | Extended context |
| **Owner** | Yes | Accountable person |
| **Time Period** | Yes | Q1, Q2, Q3, Q4, or Annual |
| **Year** | Yes | Target year |
| **Start Date** | Auto | Period start |
| **End Date** | Auto | Period end |

### Writing Good Objectives

**Format**: Start with a verb, be inspirational

✅ **Good Objectives**:
- "Become the market leader in customer satisfaction"
- "Transform our engineering culture to embrace DevOps"
- "Achieve operational excellence in product delivery"

❌ **Bad Objectives**:
- "Get more sales" (too vague)
- "Increase NPS by 10 points" (this is a Key Result)
- "Do stuff" (meaningless)

### Time Period Selection

| Period | Typical Dates |
|--------|---------------|
| Q1 | Jan 1 - Mar 31 |
| Q2 | Apr 1 - Jun 30 |
| Q3 | Jul 1 - Sep 30 |
| Q4 | Oct 1 - Dec 31 |
| Annual | Jan 1 - Dec 31 |
| Custom | You set dates |

## Key Results Section

### Adding Key Results
1. Click **+ Add Key Result**
2. Fill in details
3. Repeat for 2-5 Key Results

### Key Result Fields

| Field | Required | Description |
|-------|----------|-------------|
| **Description** | Yes | What you're measuring |
| **Start Value** | Yes | Where you are now |
| **Target Value** | Yes | Goal to achieve |
| **Current Value** | Yes | Latest measurement |
| **Unit** | Optional | Measurement type |
| **Weight** | Optional | Relative importance |

### Writing Good Key Results

**Format**: Specific, measurable, achievable

✅ **Good Key Results**:
- "Increase NPS from 7.0 to 8.5"
- "Reduce customer churn from 5% to 2%"
- "Launch 3 new product features by end of quarter"

❌ **Bad Key Results**:
- "Improve satisfaction" (not measurable)
- "Work harder" (not specific)
- "Increase revenue 500%" (not achievable)

### SMART Criteria
- **S**pecific: Clear definition
- **M**easurable: Quantifiable
- **A**chievable: Realistic
- **R**elevant: Aligned to objective
- **T**ime-bound: Within period

### Linking Key Results

Key Results can pull values from:

**KPIs**:
1. Click **Link to KPI**
2. Select KPI
3. Current value auto-updates

**Projects**:
1. Click **Link to Project**
2. Select project
3. Progress % becomes value

**Manual**:
- Update Current Value directly
- Track manually

### Weight Assignment
Distribute 100% across Key Results:
- Reflects relative importance
- Affects overall progress calculation
- Default: Equal weight

Example:
- KR1: 40% weight
- KR2: 35% weight
- KR3: 25% weight
- Total: 100%

## Progress & Status

### Automatic Progress
Overall progress = Weighted average of Key Results

Each KR progress = (Current - Start) / (Target - Start) × 100

### Status Calculation

| Progress | Status | Color |
|----------|--------|-------|
| 70-100% | On Track | 🟢 |
| 40-69% | At Risk | 🟡 |
| 0-39% | Off Track | 🔴 |

### Status Override
Force a different status:
1. Check **Override Status**
2. Select status manually
3. Useful when calculation doesn't reflect reality

## Dialog Actions

| Button | Action |
|--------|--------|
| **Save** | Save and close |
| **Cancel** | Discard changes |
| **Delete** | Remove OKR (edit mode) |

## Best Practices

### OKR Quantity
- 3-5 OKRs per quarter maximum
- 2-4 Key Results per Objective
- Quality over quantity

### Regular Updates
- Update Key Result values weekly
- Check status bi-weekly
- Formal review monthly

### Stretch Goals
- Aim for 70-80% achievement
- 100% every time = not ambitious enough
- But don't make them impossible

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+S` | Save |
| `Escape` | Cancel |
| `Tab` | Next field |
