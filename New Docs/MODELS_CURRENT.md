# Domain Models – Current Canonical State

**Goal:** Treat Supabase-aligned models as canonical and aggressively retire legacy models.

## Canonical Models (New Schema)

- **People & Orgs**
  - TeamMember – active staff, linked to Supabase user and organization.
  - Organization / Firm – seat/licensing root.
- **Meetings & Agenda**
  - Meeting – unified meeting model (includes 1:1, staff meetings, etc.).
  - AgendaItem – meeting-scoped agenda row (Supabase `meeting_agenda_items`), with optional links to tasks/goals/metrics via RelatedEntityType/RelatedEntityId.
  - MeetingAgendaItem – legacy/experimental EF model for `meeting_agenda_items`; not used in main flows and scheduled for removal once AgendaItem is fully wired.
- **Work & Goals**
  - TrackerTask – task/work item (IndividualTask replacement).
  - Goal – unified goal/OKR objective model.
  - Target – key result/target attached to a Goal.
  - TargetMeasurable – polymorphic link from Target to Metric/Project/TaskCollection.
- **Metrics**
  - Metric – KPI replacement; tracks numeric performance vs targets.
  - MetricDataSource, MetricHistory – supporting models.
- **Surveys & Reviews**
  - PulseSurvey + related question/response models.
  - ReviewCycle, ReviewTemplate, Review, etc.
- **AI & Insights**
  - Embedding/Vector models (conceptual) tied to Supabase `ai_embeddings`.
  - Insight and snapshot models aligned with Supabase tables.

## Legacy Models To Retire (Keep Only for Migration)

These are covered in detail in:
- `Tracker/CONSOLIDATION_2_KPI_METRIC_ANALYSIS.md`
- `Tracker/CONSOLIDATION_3_MEASURABLE_ANALYSIS.md`
- `TIER_2_OBJECTIVE_KEYRESULT_CONSOLIDATION.md`
- `TIER_3_CONSOLIDATIONS_ANALYSIS.md`

**Key legacy types:**

- OneOnOne – replaced by Meeting (with MeetingType = OneOnOne).
- MeetingAgendaItem – superseded by AgendaItem as the canonical meeting agenda model; keep only until migrations are complete.
- KeyPerformanceIndicator – replaced by Metric.
- ObjectiveKeyResult – replaced by Goal + Target.
- KeyResult (missing as a model; use Target instead).
- KeyResultMeasurable – replaced by TargetMeasurable.
- IndividualGoal – merged into Goal with type discriminator/usage.
- ProgressSnapshot (legacy int-based entity references) – to be updated to Guid-based `goal/target/project/task` entity types.

## Model-Level Work Remaining

- Delete legacy model classes once their usage is removed from services/viewmodels:
  - KeyPerformanceIndicator, KeyResultMeasurable, ObjectiveKeyResult, IndividualGoal, OneOnOne-specific junction/link models.
- Ensure all remaining models:
  - Use Guid/UUID identifiers.
  - Map directly to Supabase tables.
  - Do **not** reference deleted enums or legacy IDs (e.g., KpiStatusEnum, KpiFrequencyEnum, int-based foreign keys).
- Align ProgressSnapshot with the Supabase schema (see `TIER_3_CONSOLIDATIONS_ANALYSIS.md`).

Treat this file as the canonical model map; any code that disagrees with this list is a refactor candidate.
