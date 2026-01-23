# 06a – Meetings Domain Tables

This document covers all tables related to meetings in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 12

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | meetings | ✅ MeetingDetail.cs |
| 2 | meeting_series | ❓ TBD |
| 3 | meeting_attendees | ✅ MeetingAttendee.cs |
| 4 | meeting_agenda_items | ✅ MeetingAgendaItem.cs |
| 5 | meeting_agenda_item_links | ✅ MeetingAgendaItemLink.cs |
| 6 | meeting_agenda_scaffolds | ✅ MeetingAgendaScaffold.cs |
| 7 | meeting_agenda_scaffold_items | ✅ MeetingAgendaScaffoldItem.cs |
| 8 | meeting_prep_items | ✅ MeetingPrepItem.cs |
| 9 | meeting_prep_item_links | ✅ MeetingPrepItemLink.cs |
| 10 | meeting_notes | ✅ MeetingNote.cs |
| 11 | meeting_summaries | ❓ TBD |
| 12 | meeting_templates | ✅ MeetingTemplateDetail.cs |

---

## procohere.meetings

**Purpose**  
Represents a scheduled or completed meeting instance.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| title | text | NO | |
| description | text | YES | |
| meeting_type | text | NO | 'one_on_one', 'team', 'all_hands', etc. |
| status | text | NO | 'scheduled', 'in_progress', 'completed', 'cancelled' |
| scheduled_at | timestamptz | YES | |
| started_at | timestamptz | YES | |
| ended_at | timestamptz | YES | |
| duration_minutes | integer | YES | |
| location | text | YES | |
| video_link | text | YES | |
| recurrence_rule | text | YES | |
| parent_meeting_id | uuid | YES | FK → meetings (self-reference) |
| meeting_series_id | uuid | YES | FK → meeting_series |
| created_by | uuid | NO | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingDetail.cs` ✅ Verified match

**RLS:** Owner (created_by), attendees, and management chain.

---

## procohere.meeting_series

**Purpose**  
Defines recurrence metadata for recurring meetings.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| title | text | NO | |
| meeting_type | text | NO | 'one_on_one', 'team', etc. |
| created_by | uuid | NO | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Disabled – access controlled via meetings.

---

## procohere.meeting_attendees

**Purpose**  
Join table linking team members to meetings with attendance metadata.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| team_member_id | uuid | NO | FK → team_members |
| role | text | NO | 'organizer', 'attendee', 'optional' |
| response_status | text | NO | 'pending', 'accepted', 'declined', 'tentative' |
| attended | boolean | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingAttendee` (in MeetingDetail.cs) ✅ Verified match

**RLS:** Inherited from meeting visibility.

---

## procohere.meeting_agenda_items

**Purpose**  
Individual discussion items on meeting agendas with rich conversation tracking.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| added_by | uuid | NO | FK → team_members |
| title | text | NO | |
| description | text | YES | |
| status | text | NO | 'open', 'discussed', 'action_created', 'deferred', 'dropped' |
| sort_order | integer | NO | |
| is_private | boolean | NO | |
| is_completed | boolean | NO | |
| completed_at | timestamptz | YES | |
| display_title | varchar | YES | Editable display title independent of linked entity |
| shared_context | text | YES | Shared framing visible to all attendees |
| private_context | text | YES | Creator-only thinking space |
| talking_points | jsonb | YES | [{id, text, discussed, order}] |
| outcome_type | varchar | YES | 'discussed', 'decision', 'deferred', 'blocked' |
| outcome_summary | text | YES | |
| visibility_scope | varchar | YES | 'meeting' or 'personal' |
| linked_entity_title_snapshot | varchar | YES | Cached linked entity title |
| discussed_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingAgendaItem` (in MeetingDetail.cs) ✅ Verified match (after fix)

**RLS:** Meeting visibility OR creator visibility.

---

## procohere.meeting_agenda_item_links

**Purpose**  
Links agenda items to other entities (goals, tasks, metrics). Simple junction table.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_agenda_item_id | uuid | NO | FK → meeting_agenda_items |
| link_kind | text | NO | Type of link relationship |
| entity_type | text | NO | 'task', 'goal', 'metric', etc. |
| entity_id | uuid | NO | FK to linked entity |
| created_at | timestamptz | NO | |

**Note:** This table has NO soft-delete columns (is_deleted, deleted_at, deleted_by) or updated_at.

**Model:** None (links managed via MeetingAgendaItem properties or service)

**RLS:** Inherited from agenda item visibility.

---

## procohere.meeting_agenda_scaffolds

**Purpose**  
Pre-built agenda structures that can be applied to meetings.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_type | text | NO | |
| name | text | NO | |
| scope | text | NO | 'system', 'organization', 'personal' |
| created_by | uuid | YES | FK → team_members |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Organization-scoped.

---

## procohere.meeting_agenda_scaffold_items

**Purpose**  
Individual items within an agenda scaffold template.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| scaffold_id | uuid | NO | FK → meeting_agenda_scaffolds |
| title | text | NO | |
| description | text | YES | |
| sort_order | integer | NO | |
| default_is_private | boolean | NO | |
| target_kind | text | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Inherited from parent scaffold.

---

## procohere.meeting_prep_items

**Purpose**  
Pre-meeting preparation items supporting personal, assigned, and team-wide visibility.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| requested_by_team_member_id | uuid | NO | FK → team_members (creator) |
| assigned_to_team_member_id | uuid | YES | FK → team_members (assignee) |
| title | text | NO | |
| body | text | YES | |
| assignee_notes | text | YES | |
| assignee_notes_updated_at | timestamptz | YES | |
| visibility_scope | text | NO | 'personal', 'assigned', 'meeting' |
| status | text | NO | 'open', 'in_progress', 'done', 'dismissed' |
| status_updated_at | timestamptz | NO | |
| status_updated_by_team_member_id | uuid | YES | FK → team_members |
| overridden_status | boolean | NO | |
| due_at | timestamptz | YES | |
| completed_at | timestamptz | YES | |
| completed_by_team_member_id | uuid | YES | FK → team_members |
| sort_order | integer | NO | |
| carry_forward | boolean | NO | |
| carried_from_prep_item_id | uuid | YES | FK → self |
| source_type | text | NO | 'manual', 'scaffold', 'ai', 'carry_forward' |
| source_snapshot | jsonb | YES | |
| linked_entity_type | varchar | YES | 'task', 'goal', 'metric', 'project' |
| linked_entity_id | uuid | YES | |
| linked_entity_title_snapshot | varchar | YES | |
| prep_prompt | text | YES | |
| prep_response | text | YES | |
| prepared_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |

**Note:** This table does NOT have `deleted_at` or `deleted_by` columns.

**Model:** `MeetingPrepItem.cs` ✅ Verified match (after fix)

**RLS:** Organization isolation enforced. App layer handles visibility_scope logic.

---

## procohere.meeting_prep_item_links

**Purpose**  
Links prep items to other entities for context.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_prep_item_id | uuid | NO | FK → meeting_prep_items |
| link_kind | text | NO | |
| entity_type | text | NO | |
| entity_id | uuid | NO | |
| created_at | timestamptz | NO | |

**Note:** This table does NOT have soft-delete columns.

**Model:** None (not currently used in app)

**RLS:** Inherited from prep item visibility.

---

## procohere.meeting_notes

**Purpose**  
Notes captured during or after meetings.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| author_id | uuid | NO | FK → team_members |
| content | text | NO | |
| is_shared | boolean | NO | false = private to author |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingNote.cs` ✅ Verified match (after fix)

**RLS:** Forced RLS; visible via meeting access or author if private.

---

## procohere.meeting_summaries

**Purpose**  
AI-generated meeting summaries.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| meeting_id | uuid | NO | FK → meetings |
| summary | text | NO | |
| key_decisions | jsonb | YES | |
| action_items | jsonb | YES | |
| topics_discussed | jsonb | YES | |
| sentiment | text | YES | |
| generated_by | text | YES | AI model identifier |
| is_approved | boolean | NO | |
| approved_by | uuid | YES | FK → team_members |
| approved_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** None (not currently used in app)

**RLS:** Inherited from meeting visibility.

---

## procohere.meeting_templates

**Purpose**  
Reusable meeting templates with default agendas.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| created_by | uuid | NO | FK → team_members |
| name | text | NO | |
| description | text | YES | |
| meeting_type | text | NO | 'one_on_one', 'team', 'project', 'custom' |
| default_duration | integer | YES | minutes |
| default_agenda | jsonb | YES | agenda items as JSON |
| is_system_template | boolean | NO | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MeetingTemplateDetail.cs` ✅ Verified match (after fix)

**RLS:** Organization-scoped; system templates visible to all.

---
