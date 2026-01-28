
# Metrics – Product Specification

## Purpose
Metrics are recurring signals that answer the question: **Are we making progress?**
They provide quantitative or structured qualitative feedback that informs goals, meetings, and tasks without automating decisions.

---

## Definitions

### Metric
A repeatable, manually updated signal with a single owner, cadence, and direction.

### Goal
A directional outcome or intent supported by one or more metrics.

### Task
An action taken in response to metric insight or goal progress.

---

## Core Principles
- Metrics are **manual by design**
- Metrics are **recurring**, never one-off
- Metrics are **owned by a single individual**
- Metrics are **contextual**, not dashboard noise
- Metrics support **conversation, not enforcement**

---

## Required Fields
- Name
- Owner
- Measurement Type (Number, Percentage, Yes/No, Rating)
- Cadence (Weekly, Biweekly, Monthly)
- Direction (Higher / Lower / Neutral)

### Optional Fields
- Target
- Description
- Linked Goal

---

## Metric Updates
Each update includes:
- Value
- Timestamp
- Optional explanatory note

Updates are append-only and cannot be edited retroactively.

---

## Metric Ownership Rules
- Every metric has exactly **one owner**
- Ownership reflects accountability, not authorship
- Managers-of-managers may never edit metrics they do not own
- Metrics representing team performance are still owned by a single manager

---

## Signal States
Every metric is always in exactly one state:
- On Track
- At Risk
- Off Track
- Unknown

---

## Signal Calculation Inputs
Signals are derived from:
- Direction
- Target (if defined)
- Recent values (last 3–6 updates)
- Update freshness relative to cadence

---

## Signal Calculation Rules

### Step 1: Freshness Check
- Never updated → Signal = Unknown
- Last update outside cadence window:
  - Weekly → > 10 days
  - Biweekly → > 21 days
  - Monthly → > 40 days
→ Signal degrades to At Risk regardless of value

---

### Step 2: Target Evaluation (if target exists)

#### Higher Is Better
- On Track → value ≥ target
- At Risk → within 10% below target
- Off Track → more than 10% below target

#### Lower Is Better
- On Track → value ≤ target
- At Risk → within 10% above target
- Off Track → more than 10% above target

#### Neutral Metrics
- Target ignored
- Evaluated via trend only

---

### Step 3: Trend Evaluation
Based on the last 3 updates:
- Improving
- Stable (≤5% variance)
- Declining

---

### Step 4: Signal Adjustment via Trend
- Trend may downgrade but never upgrade a signal
- On Track + Declining → At Risk
- At Risk + Declining → Off Track

---

### Step 5: Final Resolution
Worst applicable state wins in this order:
1. Unknown
2. Off Track
3. At Risk
4. On Track

---

## Manager-of-Managers (MoM) Support

### Design Intent
Managers-of-managers consume metrics to detect patterns, risk, and drift across teams without micromanagement.

### MoM Visibility Rules
In Circle views, MoMs see:
- Metric name
- Owner (manager)
- Signal state
- Trend indicator
- Last update age

MoMs do not see by default:
- Full value history
- Raw numeric detail
- Update notes

---

### Cadence as a Signal
Missed updates automatically degrade metric signal visibility for MoMs, surfacing neglect without explicit alerts.

---

### Anti-Micromanagement Safeguards
- MoMs cannot update or override metrics they do not own
- MoMs may reference metrics in meetings and comments only

---

## Surface Behavior Summary

### Briefing
- Metrics appear only when:
  - Due for update
  - Off Track
  - Recently changed
- No charts or history
- Immediate action focus

### Me
- Shows owned and accountable metrics
- Displays trends, cadence status, and update affordances
- Emphasizes honesty and responsibility

### Circle
- Shows shared metrics grouped by goal or team
- Signal-first, value-second
- Optimized for alignment and discussion

---

## Non-Goals (v1)
- No automated ingestion
- No vanity dashboards
- No cross-metric rollups
