# KPIs (Key Performance Indicators)

KPIs are measurable metrics that track specific aspects of team or individual performance. Unlike OKRs which focus on achieving stretch goals, KPIs are about maintaining and monitoring ongoing performance.

## KPIs vs. OKRs

| Aspect | KPIs | OKRs |
|--------|------|------|
| **Purpose** | Monitor performance | Achieve stretch goals |
| **Nature** | Ongoing metrics | Time-bound objectives |
| **Target** | Maintain threshold | Reach ambitious target |
| **Success** | Stay on target | 70% is good |
| **Change** | Stable over time | New each quarter |

**Use Both**: KPIs feed into OKR Key Results for automatic progress tracking.

## Anatomy of a KPI

```
┌─────────────────────────────────────────────┐
│  Customer Satisfaction Score (NPS)          │
│  ─────────────────────────────────────────  │
│  Current: 72    Target: 80    Unit: score   │
│  Status: 🟡 Close to Target (90%)           │
│  Direction: Higher is Better                │
│  Owner: Sarah Johnson                       │
│  Last Updated: 2 days ago                   │
└─────────────────────────────────────────────┘
```

## Creating a KPI

### Step 1: Define the Metric
1. Navigate to **Pulse** → **KPIs**
2. Click **+ Add KPI**
3. Enter name and description

### Step 2: Set Values

| Field | Description | Example |
|-------|-------------|---------|
| **Current Value** | Where you are now | 72 |
| **Target Value** | Where you should be | 80 |
| **Unit** | Measurement type | score, %, $, hours |

### Step 3: Configure Behavior

| Field | Description |
|-------|-------------|
| **Target Direction** | Greater or Less is better |
| **Category** | Grouping (Customer, Revenue, etc.) |
| **Frequency** | How often to update |
| **Owner** | Who's responsible |

## Target Direction

### Greater or Equal (↑)
Higher values are better:
- Revenue
- Customer satisfaction
- Conversion rates
- Productivity metrics

### Less or Equal (↓)
Lower values are better:
- Bug counts
- Response time
- Churn rate
- Error rates
- Costs

## Update Frequency

| Frequency | Best For | Example |
|-----------|----------|---------|
| **On Demand** | Irregular metrics | Special projects |
| **Daily** | Operational metrics | Website uptime |
| **Weekly** | Team metrics | Sprint velocity |
| **Monthly** | Business metrics | Revenue |
| **Quarterly** | Strategic metrics | Market share |

## Status Calculation

### For "Greater or Equal" KPIs

| Value vs Target | Status |
|-----------------|--------|
| ≥ 100% of target | 🟢 On Target |
| 90-99% of target | 🟡 Close to Target |
| < 90% of target | 🔴 Off Target |

### For "Less or Equal" KPIs

| Value vs Target | Status |
|-----------------|--------|
| ≤ 100% of target | 🟢 On Target |
| 101-110% of target | 🟡 Close to Target |
| > 110% of target | 🔴 Off Target |

## Categories

Organize KPIs by domain:

### Customer
- Net Promoter Score (NPS)
- Customer Satisfaction (CSAT)
- Customer Retention Rate
- Churn Rate
- Support Ticket Volume

### Revenue
- Monthly Recurring Revenue (MRR)
- Average Revenue Per User (ARPU)
- Customer Lifetime Value (CLV)
- Sales Conversion Rate

### Quality
- Bug/Defect Count
- Code Coverage %
- Production Incidents
- Rework Rate
- Error Rate

### Efficiency
- Cycle Time
- Throughput
- Utilization Rate
- Time to Resolution

### People
- Employee Engagement Score
- Employee NPS (eNPS)
- Turnover Rate
- Time to Hire
- Training Hours

## Composite KPIs

Create parent KPIs calculated from children:

### When to Use
- Aggregate related metrics
- Create summary scores
- Roll up team → department → org

### Aggregation Methods

| Method | Description |
|--------|-------------|
| **Average** | Mean of all children |
| **Weighted Average** | With custom importance |
| **Sum** | Total of all children |
| **Minimum** | Lowest child value |
| **Maximum** | Highest child value |

### Example: Team Health Score

```
Team Health Score (Composite)
├── Employee Engagement (40%)
├── eNPS Score (30%)
└── Retention Rate (30%)

Calculated Value = Weighted Average
```

## Linking KPIs

### To OKR Key Results
KPIs can auto-populate Key Result values:
1. Create OKR with Key Result
2. On Key Result, select "Link to KPI"
3. Choose KPI
4. Key Result value updates automatically

### To Other KPIs
For composite calculations:
1. Create parent KPI
2. Mark as "Composite"
3. Add child KPIs
4. Set weights
5. Parent calculates automatically

### To Reports
Include KPIs in dashboards and reports:
- KPI Scorecard report
- Custom report widgets
- Dashboard overview

## Updating KPIs

### Manual Update
1. Open KPI
2. Change Current Value
3. Save
4. Timestamp auto-updates

### Quick Update
1. On KPI list, click value
2. Enter new number
3. Press Enter

### Bulk Update
1. Select multiple KPIs
2. Click **Bulk Update**
3. Enter new values
4. Save all

## KPI Views

### Scorecard View
All KPIs in a grid:
- Status at a glance
- Quick value updates
- Category grouping

### List View
Detailed table:
- All fields visible
- Sortable columns
- Filtering

### Dashboard Widget
Summary panel:
- Total KPIs by status
- Recent changes
- Alerts

## Best Practices

### Naming KPIs
✅ "Customer Satisfaction Score (CSAT)"
❌ "Satisfaction"

Include:
- Clear metric name
- Abbreviation if common
- Unit hint if helpful

### Setting Targets
1. **Use historical data**: What's realistic?
2. **Consider context**: Season, market conditions
3. **Make it achievable**: But not too easy
4. **Review regularly**: Adjust as needed

### Choosing What to Measure
Ask yourself:
- Can I influence this?
- Does it matter for my goals?
- Can I get reliable data?
- Will tracking it drive behavior?

### Quantity Guidelines
- **5-10 KPIs maximum** for most managers
- Too many = analysis paralysis
- Focus on what you can actually impact

### Review Cadence

| Activity | Frequency |
|----------|-----------|
| Update values | Per frequency set |
| Review status | Weekly |
| Adjust targets | Quarterly |
| Add/remove KPIs | Quarterly |

## Common KPIs by Role

### Engineering Manager
- Sprint velocity
- Bug escape rate
- Code review turnaround
- Deployment frequency
- Production incidents

### Sales Manager
- Pipeline value
- Conversion rate
- Average deal size
- Sales cycle length
- Quota attainment

### Customer Success Manager
- NPS / CSAT scores
- Retention rate
- Expansion revenue
- Time to value
- Support ticket volume

### Product Manager
- Feature adoption
- User engagement
- Time to ship
- Customer feedback score
- Roadmap delivery %

## Troubleshooting

### "My KPI shows wrong status"
- Check target direction setting
- Verify current and target values
- Review status threshold calculation

### "KPI isn't updating OKR"
- Confirm link exists
- Check KPI has been updated
- Verify Key Result source setting

### "Can't find historical values"
- KPIs track current value only
- Use Reports for historical trends
- Consider logging to external system
