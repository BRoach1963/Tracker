# Agenda Items Implementation Validation

**Document Created:** January 20, 2026  
**Status:** Validation Analysis  
**Purpose:** Validate current implementation against specification requirements

---

## Executive Summary

This document validates the current ProCohere agenda items implementation against the three specification documents provided:
1. **Agenda Items & Outcomes** - Core model and scenarios
2. **Agenda Items: Model, Outcomes, UI, and Templates** - Detailed specification
3. **Carry Forward: Model, UI, Expiration, and AI Guidance** - Deferral behavior

### Overall Status: ⚠️ **Partially Implemented** (~60% coverage)

---

## 1. Core Agenda Item Model

### ✅ **IMPLEMENTED**

| Feature | Status | Location |
|---------|--------|----------|
| Agenda item entity | ✅ Complete | `MeetingAgendaItem` in [MeetingDetail.cs](../../../Tracker/ProCohere.Avalonia/Models/MeetingDetail.cs#L357) |
| Title/description | ✅ Complete | `Title`, `Description` properties |
| Sort order | ✅ Complete | `SortOrder` property |
| Privacy flag | ✅ Complete | `IsPrivate` property |
| Status workflow | ✅ Complete | `open`, `discussed`, `action_created`, `deferred`, `dropped` |
| Meeting association | ✅ Complete | `MeetingId` FK |
| Created by tracking | ✅ Complete | `AddedBy` FK |
| Soft delete | ✅ Complete | `IsDeleted`, `DeletedAt`, `DeletedBy` |

### ✅ **Linked Entity Support - IMPLEMENTED**

| Feature | Status | Location |
|---------|--------|----------|
| `linked_entity_type` column | ✅ Complete | Database + model |
| `linked_entity_id` column | ✅ Complete | Database + model |
| Task linking | ✅ Complete | When task created from agenda item |
| Goal linking | ✅ Partial | Schema supports, UI not connected |
| Metric linking | ✅ Partial | Schema supports, UI not connected |
| Milestone linking | ❌ Missing | No milestone entity yet |

**Spec Requirement:**
> "An agenda item may optionally link to a single primary entity: Task, Goal, Milestone, Metric, None (pure discussion)"

**Current Support:** Task ✅, Goal ⚠️ (schema only), Metric ⚠️ (schema only), Milestone ❌, None ✅

---

## 2. Outcomes Model

### ❌ **NOT IMPLEMENTED - Major Gap**

The specification defines 6 outcome types that should be producible from agenda items:

| Outcome Type | Status | Notes |
|--------------|--------|-------|
| Task Created | ✅ Complete | Fully implemented via `CreateTaskFromAgendaItemAsync` |
| Goal Created or Updated | ❌ Missing | No UI/service support |
| Follow-Up Scheduled | ❌ Missing | No follow-up meeting creation |
| Decision Recorded | ⚠️ Partial | Notes category exists (`NoteCategory.Decision`) but not integrated with agenda items |
| Feedback Captured | ⚠️ Partial | Feedback entity exists but not linked to agenda outcomes |
| Notes Only | ⚠️ Partial | Meeting notes exist but separate from agenda item outcomes |

**Spec Requirement:**
> "There is no standalone 'Action Item' object. Any outcome requiring ownership, tracking, or due dates must be represented as a Task."

**Current Implementation:** This is **correctly implemented** - tasks are the only actionable outcome type.

### Missing: Formal Outcomes Model

The spec implies an `agenda_item_outcomes` table or similar linking mechanism. Currently:
- No `agenda_item_outcomes` table exists
- Outcomes are implicit (task creation updates `linked_entity_type`)
- No structured recording of decisions, feedback, or notes at the agenda item level

---

## 3. Scenario Coverage

### Scenario 1: Metric Discussion with No Performance Issue ⚠️ Partial

| Requirement | Status |
|-------------|--------|
| Agenda item links to metric | ⚠️ Schema supports, UI missing |
| Notes capture context | ⚠️ Meeting notes exist, not agenda-item-specific |
| Feedback is recorded | ⚠️ Not integrated with agenda items |
| Decision recorded | ❌ Not integrated |
| Optional task only if needed | ✅ Works correctly |
| Sensitive notes permission-scoped | ⚠️ `is_private` exists on agenda items |

### Scenario 2: Metric Indicates Systemic Issue ⚠️ Partial

| Requirement | Status |
|-------------|--------|
| Agenda item links to metric | ⚠️ Schema supports, UI missing |
| Decision recorded | ❌ Not integrated |
| Tasks created | ✅ Works |
| Goal update optional | ❌ No goal update from agenda |

### Scenario 3: Task Status Green, Quality Red ⚠️ Partial

| Requirement | Status |
|-------------|--------|
| Agenda item links to metric/milestone | ⚠️ Partial (milestone not supported) |
| Decision recorded | ❌ Not integrated |
| Tasks created | ✅ Works |
| Follow-up scheduled | ❌ Not implemented |

### Scenario 4: Positive Feedback Only ⚠️ Partial

| Requirement | Status |
|-------------|--------|
| Agenda item captures recognition | ✅ Can be created |
| Feedback outcome recorded | ❌ No formal feedback outcome |
| No task created | ✅ Optional, works |

### Scenario 5: Goal Adjustment ⚠️ Partial

| Requirement | Status |
|-------------|--------|
| Agenda item links to goal | ⚠️ Schema supports, UI missing |
| Decision recorded | ❌ Not integrated |
| Goal updated | ❌ No goal update from agenda |
| Optional task for next steps | ✅ Works |

---

## 4. UI Behavior

### ✅ **IMPLEMENTED - Collapsed/Expanded State**

The current UI in [MeetingDetailFlyout.axaml](../../../Tracker/ProCohere.Avalonia/Views/Controls/MeetingDetailFlyout.axaml) provides:

| Feature | Status | Notes |
|---------|--------|-------|
| Checkbox display | ✅ Complete | `IsCompleted` binding |
| Title display | ✅ Complete | With strikethrough for completed |
| Status badge | ✅ Complete | Shows current status |
| Description (optional) | ✅ Complete | Progressive disclosure |
| Status change buttons | ✅ Complete | Open/Discussed/Defer/Drop |
| Create Task action | ✅ Complete | With linked entity indicator |

### ❌ **MISSING - Progressive Disclosure as Specified**

**Spec Requirement:**
> Collapsed State: Checkbox, Title, Optional subtle indicators for linked items, notes, or outcomes
> Expanded State (progressive disclosure): Linked entity summary (if present), Notes tab, Outcomes tab

**Current Implementation:**
- All content is always visible
- No expandable/collapsible behavior
- No Notes tab specific to agenda item
- No Outcomes tab

### ⚠️ **PARTIAL - Mockup Alignment**

The spec mockup shows:
```
▾ Sprint status
Linked: Velocity (last 6 weeks)

[ Notes | Outcomes ]

Notes:
Velocity dipped versus baseline. Root cause identified as temporary PTO increase.
Visibility: Manager + Individual

Outcomes:
Decision recorded
Feedback captured
+ Create task
+ Schedule follow-up
```

**Gap:** Current UI doesn't match this tabbed interface pattern.

---

## 5. Agenda Item Templates

### ⚠️ **PARTIAL - Schema Exists, Not in ProCohere.Avalonia**

| Feature | Status | Location |
|---------|--------|----------|
| `meeting_templates` table | ✅ Exists | Database schema |
| `meeting_template_items` table | ✅ Exists | Database schema |
| `MeetingTemplate` model | ⚠️ Only in Tracker.Core | Not in ProCohere.Avalonia |
| Template selection UI | ❌ Missing | No UI to apply templates |

**Spec Templates Defined:**
1. 1:1 Check-In ❌ Not implemented
2. Sprint / Team Status ❌ Not implemented
3. Planning Session ❌ Not implemented
4. Retrospective ❌ Not implemented
5. Ad-Hoc / Issue Review ❌ Not implemented

---

## 6. Carry Forward

### ✅ **PARTIALLY IMPLEMENTED**

| Feature | Status | Notes |
|---------|--------|-------|
| Deferral status (`deferred`) | ✅ Complete | Status value supported |
| Copy + Link pattern | ✅ Designed | See [PROCOHERE_DESIGN_DECISIONS.md](../PROCOHERE_DESIGN_DECISIONS.md#71-agenda-item-deferral-carry-forward) |
| `linked_entity_type='agenda_item'` for chain | ✅ Designed | Documented, not yet UI-implemented |
| Auto carry-forward | ❌ Not implemented | Design exists, no code |
| Expiration model | ❌ Not implemented | |
| Person-anchoring | ❌ Not implemented | |

### ❌ **MISSING - Carry Forward States**

**Spec Requirement:**
> Carry-forward item states: Pending, Surfaced, Resolved, Converted, Expired

**Current Implementation:** Only the basic `deferred` status exists. No carry-forward lifecycle.

### ❌ **MISSING - Expiration Rules**

**Spec Requirement:**
> Expires after two meeting opportunities with the anchor person, or expires after 30 days, whichever comes first.

**Current Implementation:** No expiration logic exists.

### ❌ **MISSING - Person Anchoring**

**Spec Requirement:**
> In v1, all carry-forward items are anchored to a single individual.

**Current Implementation:** No anchor person concept. Carry-forward would just be meeting-to-meeting with no person association.

### ❌ **MISSING - AI Assistance Guidelines**

**Spec Requirement:**
> AI may suggest carry-forward but never applies it automatically.
> AI must use non-judgmental, tentative language and present all suggestions as optional.

**Current Implementation:** No AI integration for carry-forward.

---

## 7. Permission/Visibility Scoping

### ✅ **PARTIAL - Basic Support Exists**

| Feature | Status | Notes |
|---------|--------|-------|
| `is_private` on agenda items | ✅ Exists | Database column |
| `is_shared` on meeting notes | ✅ Exists | Database column |
| RLS policies | ✅ Complete | Organization-level isolation |
| Manager visibility functions | ✅ Complete | `get_team_descendants`, `get_visible_team_member_ids` |

### ⚠️ **MISSING - Sensitive Notes Scoping**

**Spec Requirement:**
> Sensitive notes are permission-scoped (e.g., "Visibility: Manager + Individual")

**Current Implementation:** `is_private` is a boolean. No granular visibility control (e.g., "visible to X and Y only").

---

## 8. Summary: What Needs to Be Built

### High Priority (Core Functionality Gaps)

1. **Outcomes Model & UI**
   - Create formal outcomes tracking for agenda items
   - Add Decision, Feedback, and Notes outcome types
   - Build Outcomes tab in agenda item expanded view

2. **Linked Entity UI**
   - Add UI to link agenda items to Goals, Metrics (not just Tasks)
   - Display linked entity summary in agenda item

3. **Progressive Disclosure UI**
   - Implement collapsible/expandable agenda items
   - Add Notes and Outcomes tabs per spec mockup

4. **Carry Forward Lifecycle**
   - Implement carry-forward creation (copy + link)
   - Add carry-forward states (Pending, Surfaced, Resolved, Converted, Expired)
   - Implement expiration logic
   - Add person-anchoring

### Medium Priority (Templates & Polish)

5. **Meeting Templates in ProCohere.Avalonia**
   - Port `MeetingTemplate` model
   - Build template selection UI
   - Create default templates per spec

6. **Follow-Up Scheduling**
   - Add "Schedule follow-up" outcome type
   - Create follow-up meeting with linked agenda item

### Lower Priority (Enhancements)

7. **Granular Note Visibility**
   - Extend beyond `is_private` boolean
   - Support "Manager + Individual" style scoping

8. **AI Integration for Carry-Forward**
   - Suggestion logic
   - Non-judgmental language patterns

---

## 9. Recommended Implementation Order

1. **Phase 1: Outcomes Foundation**
   - Add `agenda_item_outcomes` or inline outcome tracking
   - Implement Decision and Feedback recording
   - Connect Notes to agenda items

2. **Phase 2: UI Improvements**
   - Progressive disclosure (collapse/expand)
   - Notes and Outcomes tabs
   - Linked entity picker for Goal/Metric

3. **Phase 3: Carry Forward**
   - Copy + link mechanics
   - Anchor person concept
   - Carry forward states

4. **Phase 4: Templates**
   - Port template model
   - Build template picker
   - Create standard templates

---

## Files Referenced

| File | Purpose |
|------|---------|
| [MeetingDetail.cs](../../../Tracker/ProCohere.Avalonia/Models/MeetingDetail.cs) | `MeetingAgendaItem` model |
| [MeetingAgendaItemService.cs](../../../Tracker/ProCohere.Avalonia/Services/MeetingAgendaItemService.cs) | Agenda item CRUD |
| [MeetingDetailFlyout.axaml](../../../Tracker/ProCohere.Avalonia/Views/Controls/MeetingDetailFlyout.axaml) | Agenda UI |
| [CircleViewModel.cs](../../../Tracker/ProCohere.Avalonia/ViewModels/CircleViewModel.cs) | Agenda commands |
| [NoteCategory.cs](../../../Tracker/ProCohere.Avalonia/Models/NoteCategory.cs) | Decision/Feedback categories |
| [Note.cs](../../../Tracker/ProCohere.Avalonia/Models/Note.cs) | Note model with linking |
| [PROCOHERE_DESIGN_DECISIONS.md](../PROCOHERE_DESIGN_DECISIONS.md) | Carry-forward design |
| [PROCOHERE_SCHEMA_FINAL.sql](../../../Database%20Documentation/ProCohere%20Schema/PROCOHERE_SCHEMA_FINAL.sql) | Database schema |
