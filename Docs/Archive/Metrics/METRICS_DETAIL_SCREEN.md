
# Metrics – Detail Screen Specification

## Purpose
The Metric Detail screen is the authoritative place to understand, update, and discuss a single metric over time.

---

## Layout Sections

### Header
- Metric name
- Signal state
- Owner
- Cadence
- Linked goal (if any)

---

### Current Status
- Latest value
- Target (if defined)
- Direction indicator
- Last updated date

---

### Trend
- Small inline trend visualization (last 6 updates max)
- Emphasis on direction, not precision

---

### Update Panel
- Value input (type-specific)
- Optional note field
- Save action

---

### History
- Chronological list of updates
- Each entry shows value, date, and note

---

### Related Context
- Linked goal summary
- Tasks created from this metric
- Meetings where this metric was discussed

---

## Behavioral Rules
- Updates are explicit user actions
- History is append-only
- No inline editing of past values
