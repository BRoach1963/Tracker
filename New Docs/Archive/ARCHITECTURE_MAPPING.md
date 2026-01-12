# Tracker Architecture Mapping: OKR → Goal → Target Evolution

## The Confusion Resolved

You've transitioned the codebase through multiple generations of goal-setting models. Here's the complete map:

---

## Old → New Terminology

| Old Term | New Model | Purpose | Repository Status |
|----------|-----------|---------|-------------------|
| **OKR** (Objective and Key Results) | **Goal** + **Target** | Strategic goal-setting framework | ⏳ Goal repo pending |
| **Objective** (part of OKR) | **Goal** | The qualitative "what" we want to achieve | ⏳ Goal repo pending |
| **Key Result** (part of OKR) | **Target** | The measurable "how" we measure success | ✅ TargetRepository |
| **KPI** (Key Performance Indicator) | **Metric** | Standalone performance measurement | ✅ MetricRepository |
| **Development Goal** | **DevelopmentGoal** | Personal career/skill development | ⏳ DevelopmentGoalRepository pending |

---

## The New Architecture

### 1. **Goal** (formerly Objective)
- **What it is**: The qualitative aim - "what do we want to achieve?"
- **Model**: `Goal.cs`
- **GoalType**:
  - `Organizational` - Company/enterprise-wide goals
  - `Team` - Team-level goals
  - `Personal` - Individual contributor goals (business goals, not career dev)
- **Key Properties**:
  - Title, Description
  - Type (Organizational/Team/Personal)
  - TimePeriod (Q1-Q4, Annual, Custom)
  - OwnerTeamMemberId (who owns this goal)
- **Progress**: Calculated from linked **Targets**
- **Repository Needed**: `GoalRepository` (NOT CREATED YET)
- **Example**: "Improve customer satisfaction" or "Launch new product features"

### 2. **Target** (formerly Key Result)
- **What it is**: The measurable outcome - "how do we measure success?"
- **Model**: `Target.cs`
- **Parent**: Belongs to a Goal (via GoalId FK)
- **Key Properties**:
  - Title, Description
  - StartingValue, TargetValue, CurrentValue
  - Unit (%, count, $, hours, etc.)
  - Progress calculated: `(CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100`
  - OkrStatus enum (OnTrack, AtRisk, OffTrack, Completed)
- **Can be linked to**: "Measurables" (Metrics, Projects, Task Collections)
- **Repository**: ✅ `TargetRepository` (COMPLETE)
- **Example for "Improve customer satisfaction" goal**:
  - Target 1: "Increase NPS from 42 to 65"
  - Target 2: "Reduce escalations from 25 to 10 per month"
  - Target 3: "Achieve 95% on-time delivery (currently 78%)"

### 3. **Metric** (formerly KPI)
- **What it is**: Standalone performance indicator - ongoing measurement
- **Model**: `Metric.cs`
- **Can exist independently**: Yes (doesn't require a Goal/Target parent)
- **Key Properties**:
  - Name, Description
  - Category (Sales, Engineering, Customer Success, etc.)
  - CurrentValue, TargetValue
  - Unit, Frequency
  - IsComposite (can be calculated from child metrics)
- **Can feed into**: Targets (via TargetMeasurable links)
- **Repository**: ✅ `MetricRepository` (COMPLETE)
- **Example**: "Customer satisfaction score", "Team velocity", "Bug resolution time"

### 4. **DevelopmentGoal** (Personal career/skill development)
- **What it is**: Individual career growth goals - separate from business goals
- **Model**: `DevelopmentGoal.cs`
- **Different from Goal.Personal**: This is ALWAYS individual/personal (person's career)
- **Key Properties**:
  - Category (SkillDevelopment, Certification, Promotion, etc.)
  - TargetDate, CompletedAt
  - Status (Draft, Active, Paused, Completed, Abandoned)
  - ProgressPercent (0-100)
  - SuccessCriteria
  - Milestones (DevelopmentGoalMilestone)
- **Example**: "Complete AWS certification", "Learn Kubernetes", "Improve public speaking skills"
- **Repository Needed**: `DevelopmentGoalRepository` (NOT CREATED YET)

---

## The Three Goal Levels

```
┌─────────────────────────────────────────────────┐
│  GOAL                                           │
│  (formerly Objective)                           │
│  "What do we want to achieve?"                  │
│  ✅ GoalType: Organizational, Team, Personal   │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │ TARGET 1 (Key Result #1)                 │  │
│  │ "Increase NPS from 42 to 65"             │  │
│  │   → Links to: Metrics (feedback data)    │  │
│  │   → Links to: Projects (initiatives)     │  │
│  │   → Links to: Task Collections           │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │ TARGET 2 (Key Result #2)                 │  │
│  │ "Achieve 95% on-time delivery"           │  │
│  │   → Links to: Metrics (delivery data)    │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘

┌──────────────────────────────────┐
│  METRIC (standalone KPI)         │
│  "Customer satisfaction score"   │
│  → Can be standalone OR          │
│  → Can feed into multiple Target │
│  → Can have DataSources          │
│  → Can be Composite (calc'd)     │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│ DEVELOPMENT GOAL (Career)        │
│ "Complete AWS certification"     │
│ → Milestones                     │
│ → Success criteria               │
│ → Progress tracking              │
└──────────────────────────────────┘
```

---

## Relationship Matrix

| From | To | Type | Example |
|------|----|----|---------|
| Goal | Target | Parent-Child (required) | Goal "Improve customer satisfaction" has 3 Targets |
| Target | Metric | Link (optional) | Target "Increase NPS" links to "NPS Score" Metric |
| Target | Project | Link (optional) | Target "Launch features" links to "Mobile App Project" |
| Target | TaskCollection | Link (optional) | Target links to grouped tasks |
| Metric | Metric | Parent-Child (composite) | "Overall Health Score" = average of 3 child metrics |
| Metric | MetricDataSource | Data feed | "NPS Score" fed by survey data source |
| Meeting | Metric | Discussion (new) | Meeting discusses progress on specific metrics (MeetingMetricLink) |
| DevelopmentGoal | DevelopmentGoalMilestone | Parent-Child | Career goal has progress milestones |

---

## Why Three Separate Models?

### 1. **Goals + Targets** (Strategic Framework)
- **Use when**: You need measurable business outcomes
- **Structure**: Parent-child (Goal contains Targets)
- **Timeline**: Usually quarterly or annual
- **Linked to**: Organizational initiatives

### 2. **Metrics** (Performance Measurement)
- **Use when**: You need ongoing performance tracking
- **Structure**: Can exist standalone OR feed into Targets
- **Timeline**: Can be daily, weekly, monthly, ongoing
- **Purpose**: Health checks, not just goal progress

### 3. **Development Goals** (Career Growth)
- **Use when**: Tracking personal/professional development
- **Structure**: Completely separate (not Goals)
- **Timeline**: Variable (weeks to years)
- **Purpose**: Career advancement, skill development

---

## Why You Need Three Repositories

```
GoalRepository (NEEDED)
├── GetGoalsAsync()
├── GetGoalsByTypeAsync(GoalType? type)
├── GetGoalByIdAsync(Guid id)
├── AddGoalAsync(Goal goal)
├── UpdateGoalAsync(Goal goal)
├── DeleteGoalAsync(Guid id)
└── GetGoalTargetsAsync(Guid goalId) → uses TargetRepository

TargetRepository (✅ COMPLETE)
├── GetTargetsAsync()
├── GetTargetsByStatusAsync(OkrStatus? status)
├── GetTargetByIdAsync(Guid id)
├── AddTargetAsync(Target target)
├── UpdateTargetAsync(Target target)
├── DeleteTargetAsync(Guid id)
├── LinkMeasurableToTargetAsync() → links to Metrics/Projects/TaskCollections
└── GetTargetMeasurablesAsync(Guid targetId)

MetricRepository (✅ COMPLETE)
├── GetMetricsAsync()
├── GetMetricByIdAsync(Guid id)
├── AddMetricAsync(Metric metric)
├── UpdateMetricAsync(Metric metric)
├── DeleteMetricAsync(Guid id)
├── GetMetricsByCategoryAsync(string category)
├── GetChildMetricsAsync(Guid parentMetricId) → composite metrics
├── LinkMetricToMeetingAsync() → MeetingMetricLink
└── GetMetricDataSourcesAsync(Guid metricId)

DevelopmentGoalRepository (NEEDED)
├── GetDevelopmentGoalsAsync()
├── GetDevelopmentGoalsByStatusAsync()
├── GetDevelopmentGoalByIdAsync(Guid id)
├── AddDevelopmentGoalAsync(DevelopmentGoal goal)
├── UpdateDevelopmentGoalAsync(DevelopmentGoal goal)
├── DeleteDevelopmentGoalAsync(Guid id)
├── GetTeamMemberDevelopmentGoalsAsync(Guid teamMemberId)
├── GetDevelopmentGoalMilestonesAsync(Guid goalId)
└── AddMilestoneAsync(Guid goalId, DevelopmentGoalMilestone milestone)
```

---

## Current Repository Status

✅ **COMPLETE**:
- TargetRepository (Key Results)
- MetricRepository (KPIs)
- MeetingRepository
- TrackerTaskRepository
- KudosRepository

⏳ **NEEDED**:
- **GoalRepository** (Objectives)
- **DevelopmentGoalRepository** (Career goals)
- ReviewCycleRepository
- ReviewTemplateRepository
- PerformanceReviewRepository
- And 9+ others...

---

## Key Insight

**You don't see a Goals repository yet because it hasn't been created.** The legacy codebase uses `ObjectiveKeyResult` (old model), but you've refactored to new models:
- `Goal` = new Objective
- `Target` = new Key Result
- `Metric` = new KPI (standalone)
- `DevelopmentGoal` = career development (separate from business Goals)

Each needs its own repository following the established pattern.
