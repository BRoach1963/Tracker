# Add/Edit KPI Dialog

This dialog creates and manages Key Performance Indicators.

## Accessing the Dialog

### Add New
- **Pulse** → **KPIs** → **+ Add KPI**

### Edit Existing
- Click KPI → **Edit**
- Right-click → **Edit**

## Dialog Fields

### Core Fields

| Field | Required | Description |
|-------|----------|-------------|
| **Name** | Yes | Short, descriptive name |
| **Description** | Optional | What this KPI measures |
| **Category** | Optional | Grouping (Customer, Revenue, etc.) |
| **Owner** | Recommended | Who's responsible |

### Measurement Fields

| Field | Description |
|-------|-------------|
| **Current Value** | Where you are now |
| **Target Value** | Goal to achieve |
| **Unit** | Measurement type (%, $, count, etc.) |
| **Target Direction** | Greater or Less is better |

### Target Direction

| Direction | Use When |
|-----------|----------|
| **Greater or Equal** | Higher is better (revenue, satisfaction) |
| **Less or Equal** | Lower is better (errors, costs, time) |

### Update Frequency

How often should this be measured?

| Frequency | Typical Use |
|-----------|-------------|
| **On Demand** | Irregular updates |
| **Daily** | Operational metrics |
| **Weekly** | Team performance |
| **Monthly** | Business metrics |
| **Quarterly** | Strategic KPIs |

## Categories

Organize KPIs by type:
- **Customer**: Satisfaction, NPS, retention
- **Revenue**: Sales, growth, deals
- **Quality**: Defects, errors, bugs
- **Efficiency**: Cycle time, throughput
- **People**: Engagement, retention, hiring
- **Custom**: Your own categories

## Composite KPIs

Create parent KPIs from children:

### Enable Composite Mode
1. Check **Is Composite**
2. Add child KPIs
3. Select aggregation method

### Aggregation Methods
- **Average**: Mean of child values
- **Sum**: Total of child values
- **Weighted Average**: With custom weights
- **Minimum**: Lowest child value
- **Maximum**: Highest child value

### Example: Team Health KPI
- Child: Employee NPS (weight: 40%)
- Child: Retention Rate (weight: 30%)
- Child: Training Hours (weight: 30%)
- Parent: Calculated from weighted average

## KPI Linking

### Link to Key Results
KPIs can feed OKR progress:
1. Open OKR dialog
2. On Key Result, click **Link to KPI**
3. KPI value auto-updates Key Result

### Link as Data Source
Projects and other entities can use KPIs:
- Project completion rates
- Task velocity
- Quality metrics

## Status Calculation

Status is automatic based on value vs. target:

### For "Greater or Equal" Direction

| Value vs Target | Status |
|-----------------|--------|
| ≥ 100% of target | 🟢 On Target |
| 90-99% of target | 🟡 Close |
| < 90% of target | 🔴 Off Target |

### For "Less or Equal" Direction

| Value vs Target | Status |
|-----------------|--------|
| ≤ target | 🟢 On Target |
| 101-110% of target | 🟡 Close |
| > 110% of target | 🔴 Off Target |

## Updating KPI Values

### Manual Update
1. Open KPI
2. Change **Current Value**
3. Save

### From Dialog
1. On KPI list, click **Update**
2. Enter new value
3. Timestamp auto-set

### Bulk Update
1. Select multiple KPIs
2. Click **Bulk Update**
3. Enter values
4. Save all

## Dialog Actions

| Button | Action |
|--------|--------|
| **Save** | Save and close |
| **Cancel** | Discard changes |
| **Delete** | Remove KPI (edit mode) |

## Best Practices

### Naming KPIs
- Be specific: "Customer Satisfaction Score (NPS)" not "Satisfaction"
- Include unit hint: "Response Time (hours)"
- Avoid abbreviations unless universal

### Setting Targets
- Base on historical data
- Consider improvement trajectory
- Make achievable but challenging
- Review and adjust quarterly

### Frequency Selection
- Match your ability to update
- Don't set "Daily" if you update monthly
- Regular updates = more useful data

### Limit Quantity
- Focus on 5-10 KPIs max
- Too many = none get attention
- Choose what you can actually influence

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+S` | Save |
| `Escape` | Cancel |
| `Tab` | Next field |
