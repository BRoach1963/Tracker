
# Metrics – Product Specification

## Purpose
Metrics are recurring signals that answer the question: **Are we making progress?**
They provide quantitative or structured qualitative feedback that informs goals, meetings, and tasks without automating decisions.

---

## Definitions

### Metric
A repeatable, manually updated signal with an owner, cadence, and direction.

### Goal
A directional outcome or intent supported by one or more metrics.

### Task
An action taken in response to metric insight or goal progress.

---

## Core Principles
- Metrics are **manual by design**
- Metrics are **recurring**, never one-off
- Metrics are **owned**
- Metrics are **contextual**, not dashboard noise

---

## Required Fields
- Name
- Owner
- Measurement Type (Number, Percentage, Yes/No, Rating)
- Cadence (Weekly, Biweekly, Monthly)
- Direction (Higher/Lower/Neutral)

### Optional Fields
- Target
- Description
- Linked Goal

---

## Metric Updates
Each update includes:
- Value
- Timestamp
- Optional note explaining context

---

## Signals
Derived states:
- On Track
- At Risk
- Off Track

Signals are calculated from:
- Direction
- Target
- Trend

---

## Relationships
- Goals may have multiple metrics
- Metrics support a single goal (v1)
- Tasks may reference metrics

---

## Surface Behavior Summary

### Briefing
- Only shows metrics needing attention
- No charts or history
- Action-oriented

### Me
- Shows owned and accountable metrics
- Displays trends and update cadence

### Circle
- Shows shared metrics grouped by goal
- Emphasizes trend and signal

---

## Non-Goals
- No automatic metric ingestion (v1)
- No vanity dashboards
