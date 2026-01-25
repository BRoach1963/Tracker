# 07 – Functions Reference (Authoritative)

This document enumerates **all database functions that participate in security, visibility, identity resolution,
or cross-entity enforcement**.

If a function exists in the database and affects access or correctness, it must appear here.

---

## Conventions

- All functions are assumed to run under RLS unless explicitly SECURITY DEFINER.
- Functions used by RLS **must be stable, deterministic, and index-supported**.
- UI helper functions may return broader sets than RLS allows; this distinction is explicit.

---

## Identity & Session Functions

### get_current_organization_id()
Resolves the organization for the current authenticated session.

- Source: auth.uid()
- Returns NULL when unauthenticated or unprovisioned
- Used by: nearly all RLS policies

---

### get_current_team_member_id()
Resolves the team member row for the current session.

- Organization-scoped
- Returns NULL if user has no team_member mapping
- Soft-deleted team members must not resolve

---

## Hierarchy & Visibility Functions (RLS-Safe)

### rls_is_visible_team_member(target_team_member_id uuid)
Authoritative primitive for **team-member-based visibility**.

Returns true when:
- target is self
- target is a direct or indirect report

Must:
- be organization-scoped
- fail closed
- rely on hierarchy traversal defined in `14-hierarchy-model.md`

---

### rls_can_see_meeting(meeting_id uuid)
Determines whether the current session may see a meeting.

Returns true when:
- current team member is an attendee
- OR explicit meeting-owner logic allows it

Meeting type must not implicitly widen access.

---

### rls_is_meeting_owner(meeting_id uuid)
Returns true when the current team member created the meeting.

Used by:
- update/delete policies
- agenda/prep mutation checks

---

## Entity-Specific Visibility Helpers (If Present)

These helpers may exist for readability and performance.
If they exist in the database, they **must** obey the contracts in `06-tables.md`.

- rls_can_see_task(task_id uuid)
- rls_can_see_goal(goal_id uuid)
- rls_can_see_metric(metric_id uuid)
- rls_can_see_agenda_item(agenda_item_id uuid)

If these helpers do not exist, RLS policies must compose visibility using primitives only.

---

## UI Visibility Helpers (NOT RLS)

### get_ui_visible_team_member_ids(org_id uuid, team_member_id uuid)
Returns the set of team members visible in the UI.

Includes:
- self
- manager
- peers
- descendants

**Important:** This function is NOT used in RLS.
It intentionally returns a superset for UI convenience.

---

## Utility Functions

### set_updated_at()
Trigger helper to maintain updated_at timestamps.

Must:
- be deterministic
- not mutate unrelated rows
- not bypass RLS

---

## SECURITY DEFINER Functions

Any SECURITY DEFINER function must be listed explicitly here.

Rules:
- minimal surface
- explicit EXECUTE grants
- reviewed for RLS bypass risk

### Data Mutation RPCs (SECURITY DEFINER)

These functions bypass RLS to perform INSERT operations. They are necessary because the Supabase C# client v1.1.1 does not properly pass the Authorization header to Postgrest for direct table operations, causing `auth.uid()` to return NULL during INSERT/UPDATE/DELETE.

**Root Cause:** The Supabase C# client sets the session on the Auth module, but the Postgrest module doesn't inherit the Authorization header. SELECTs work because they use different RLS conditions.

#### Meeting Domain

| Function | Purpose | Created | Updated |
|----------|---------|---------|---------|
| `insert_meeting(...)` | Creates a meeting record | 2026-01-24 | |
| `insert_meeting_attendee(...)` | Creates a meeting attendee record | 2026-01-24 | |
| `insert_meeting_agenda_item(...)` | Creates an agenda item (with optional entity link) | 2026-01-24 | 2026-01-25 |
| `update_meeting_agenda_item(...)` | Updates an agenda item (calls link RPCs internally) | 2026-01-25 | 2026-01-25 |
| `delete_meeting_agenda_item(...)` | Deletes an agenda item (soft) | 2026-01-25 | |
| `upsert_meeting_agenda_item_reference_link(...)` | Creates/updates the reference link | 2026-01-25 | |
| `delete_meeting_agenda_item_reference_link(...)` | Removes the reference link | 2026-01-25 | |
| `insert_meeting_prep_item(...)` | Creates a prep item | 2026-01-24 | 2026-01-25 |
| `update_meeting_prep_item_as_requester(...)` | Updates prep item (requester role) | 2026-01-25 | |
| `update_meeting_prep_item_as_assignee(...)` | Updates prep item (assignee role) | 2026-01-25 | |
| `delete_meeting_prep_item(...)` | Deletes a prep item (soft) | 2026-01-25 | |
| `insert_meeting_note(...)` | Creates a meeting note | 2026-01-24 | 2026-01-25 |
| `update_meeting_note(...)` | Updates a meeting note | 2026-01-25 | |
| `delete_meeting_note(...)` | Deletes a meeting note (soft) | 2026-01-25 | |
| `insert_meeting_prep_item_link(...)` | Creates a prep item link | 2026-01-24 | |

**Views:**
| View | Purpose | Created |
|------|---------|---------|
| `v_meeting_agenda_items_with_links` | Agenda items + flattened reference link + links_json | 2026-01-25 |

#### insert_meeting

```sql
procohere.insert_meeting(
    p_id uuid,
    p_organization_id uuid,
    p_title text,
    p_meeting_type text,
    p_status text,
    p_scheduled_at timestamptz,
    p_duration_minutes integer,
    p_location text,
    p_video_link text,
    p_description text,
    p_created_by uuid
) RETURNS uuid
```

Validates:
- `p_organization_id` must equal `get_current_organization_id()`
- `p_created_by` must equal `get_current_team_member_id()`

#### insert_meeting_attendee

```sql
procohere.insert_meeting_attendee(
    p_id uuid,
    p_organization_id uuid,
    p_meeting_id uuid,
    p_team_member_id uuid,
    p_role text,
    p_response_status text
) RETURNS uuid
```

#### insert_meeting_agenda_item

```sql
procohere.insert_meeting_agenda_item(
    p_meeting_id uuid,
    p_title text,
    p_description text DEFAULT NULL,
    p_display_title text DEFAULT NULL,
    p_status text DEFAULT 'open',
    p_sort_order integer DEFAULT 0,
    p_is_private boolean DEFAULT false,
    p_visibility_scope varchar DEFAULT 'meeting',
    p_shared_context text DEFAULT NULL,
    p_private_context text DEFAULT NULL,
    p_talking_points text DEFAULT NULL,
    p_linked_entity_type varchar DEFAULT NULL,
    p_linked_entity_id uuid DEFAULT NULL,
    p_linked_entity_title_snapshot varchar DEFAULT NULL
) RETURNS uuid
```

Automatically creates a link record if `p_linked_entity_type` and `p_linked_entity_id` are provided.

#### update_meeting_agenda_item

```sql
procohere.update_meeting_agenda_item(
    p_id uuid,
    p_title text DEFAULT NULL,
    p_description text DEFAULT NULL,
    p_display_title text DEFAULT NULL,
    p_status text DEFAULT NULL,
    p_is_completed boolean DEFAULT NULL,
    p_shared_context text DEFAULT NULL,
    p_private_context text DEFAULT NULL,
    p_talking_points text DEFAULT NULL,
    p_outcome_type text DEFAULT NULL,
    p_outcome_summary text DEFAULT NULL,
    p_sort_order integer DEFAULT NULL,
    p_linked_entity_type varchar DEFAULT NULL,
    p_linked_entity_id uuid DEFAULT NULL,
    p_linked_entity_title_snapshot varchar DEFAULT NULL,
    p_clear_link boolean DEFAULT false
) RETURNS boolean
```

- Set `p_clear_link = true` to remove an existing entity link
- Provide `p_linked_entity_type`, `p_linked_entity_id`, `p_linked_entity_title_snapshot` to create/update a link

#### delete_meeting_agenda_item

```sql
procohere.delete_meeting_agenda_item(
    p_id uuid
) RETURNS boolean
```

Performs soft delete (sets `is_deleted = true`, `deleted_at`, `deleted_by`).

#### insert_meeting_prep_item

```sql
procohere.insert_meeting_prep_item(
    p_meeting_id uuid,
    p_title text,
    p_body text DEFAULT NULL,
    p_visibility_scope varchar DEFAULT 'personal',
    p_assigned_to_team_member_id uuid DEFAULT NULL,
    p_status text DEFAULT 'pending',
    p_sort_order integer DEFAULT 0,
    p_carry_forward boolean DEFAULT false,
    p_carried_from_prep_item_id uuid DEFAULT NULL,
    p_source_type text DEFAULT NULL,
    p_linked_entity_type varchar DEFAULT NULL,
    p_linked_entity_id uuid DEFAULT NULL,
    p_linked_entity_title_snapshot varchar DEFAULT NULL,
    p_due_at timestamptz DEFAULT NULL,
    p_prep_prompt text DEFAULT NULL,
    p_prep_response text DEFAULT NULL
) RETURNS uuid
```

Automatically creates a link record if `p_linked_entity_type` and `p_linked_entity_id` are provided.

#### update_meeting_prep_item_as_requester

```sql
procohere.update_meeting_prep_item_as_requester(
    p_id uuid,
    p_title text DEFAULT NULL,
    p_body text DEFAULT NULL,
    p_visibility_scope varchar DEFAULT NULL,
    p_assigned_to_team_member_id uuid DEFAULT NULL,
    p_status text DEFAULT NULL,
    p_sort_order integer DEFAULT NULL,
    p_due_at timestamptz DEFAULT NULL,
    p_prep_prompt text DEFAULT NULL
) RETURNS boolean
```

Use this RPC when the current user is the **requester** (creator) of the prep item.

#### update_meeting_prep_item_as_assignee

```sql
procohere.update_meeting_prep_item_as_assignee(
    p_id uuid,
    p_assignee_notes text DEFAULT NULL,
    p_status text DEFAULT NULL,
    p_prep_response text DEFAULT NULL
) RETURNS boolean
```

Use this RPC when the current user is the **assignee** of the prep item.

#### delete_meeting_prep_item

```sql
procohere.delete_meeting_prep_item(
    p_id uuid
) RETURNS boolean
```

Performs soft delete (sets `is_deleted = true`, `deleted_at`, `deleted_by`).

#### insert_meeting_note

```sql
procohere.insert_meeting_note(
    p_meeting_id uuid,
    p_content text,
    p_is_shared boolean DEFAULT false
) RETURNS uuid
```

Note: Uses `p_is_shared` (NOT `p_is_private`). The application must invert the `isPrivate` flag:
```csharp
new KeyValuePair<string, object>("p_is_shared", !isPrivate)
```

#### update_meeting_note

```sql
procohere.update_meeting_note(
    p_id uuid,
    p_content text DEFAULT NULL,
    p_is_shared boolean DEFAULT NULL
) RETURNS uuid
```

Note: Uses `p_is_shared`. Returns the note id on success.

#### delete_meeting_note

```sql
procohere.delete_meeting_note(
    p_id uuid
) RETURNS boolean
```

Performs soft delete (sets `is_deleted = true`, `deleted_at`, `deleted_by`).

---

### Agenda Item Link Management RPCs

Single-purpose RPCs for managing the reference link on agenda items. These are called internally by `update_meeting_agenda_item` but can also be called directly.

**Design Principles:**
- One reference link per agenda item (enforced by unique index)
- Keeps `meeting_agenda_items.linked_entity_title_snapshot` in sync with link table
- Entity types validated against `allowed_entity_types` lookup table
- Must be able to see the meeting to modify links

#### upsert_meeting_agenda_item_reference_link

```sql
procohere.upsert_meeting_agenda_item_reference_link(
    p_meeting_agenda_item_id uuid,
    p_entity_type text,
    p_entity_id uuid,
    p_entity_title_snapshot character varying DEFAULT NULL
) RETURNS boolean
```

**Behavior:**
- Creates or replaces the reference link for the agenda item
- Uses `ON CONFLICT ... DO UPDATE` for atomic upsert
- Updates `meeting_agenda_items.linked_entity_title_snapshot` to stay in sync
- Validates entity type against `allowed_entity_types.is_active = true`

**Validation:**
- Must be authenticated
- Agenda item must exist and not be deleted
- Must be able to see the meeting
- `p_entity_type` must be in `allowed_entity_types` with `is_active = true`
- Both `p_entity_type` and `p_entity_id` are required

**Error Messages:**
- `'Not authenticated'` - No valid session
- `'Agenda item id is required'` - NULL agenda item ID
- `'Entity type is required'` - NULL or empty entity type
- `'Entity id is required'` - NULL entity ID
- `'Agenda item not found or access denied'` - Item doesn't exist or wrong org
- `'Cannot access this meeting'` - RLS check failed
- `'Linked entity type "X" is not allowed'` - Entity type not active

**Client Usage:**
```csharp
await client.Rpc("upsert_meeting_agenda_item_reference_link", new Dictionary<string, object>
{
    ["p_meeting_agenda_item_id"] = agendaItemId,
    ["p_entity_type"] = "task",
    ["p_entity_id"] = taskId,
    ["p_entity_title_snapshot"] = taskTitle
});
```

#### delete_meeting_agenda_item_reference_link

```sql
procohere.delete_meeting_agenda_item_reference_link(
    p_agenda_item_id uuid
) RETURNS boolean
```

**Behavior:**
- Deletes the reference link for the agenda item
- Sets `meeting_agenda_items.linked_entity_title_snapshot` to NULL
- Returns `false` if agenda item not found or not owned by current user

**Client Usage:**
```csharp
await client.Rpc("delete_meeting_agenda_item_reference_link", new Dictionary<string, object>
{
    ["p_agenda_item_id"] = agendaItemId
});
```

---

### Entity Type Validation

#### validate_active_entity_type (Trigger Function)

```sql
procohere.validate_active_entity_type()
RETURNS trigger
```

**Purpose:** Enforces that only active entity types can be used in link tables.

**Trigger:**
```sql
CREATE TRIGGER trg_validate_active_entity_type
BEFORE INSERT OR UPDATE OF entity_type
ON procohere.meeting_agenda_item_links
FOR EACH ROW
EXECUTE FUNCTION procohere.validate_active_entity_type();
```

**Note:** This is a backup guard. The RPC validates `is_active` before insert for friendly error messages.

---

### Agenda Items Read View

#### v_meeting_agenda_items_with_links

```sql
CREATE OR REPLACE VIEW procohere.v_meeting_agenda_items_with_links AS
SELECT
    mai.*,
    l.entity_type AS reference_link_entity_type,
    l.entity_id AS reference_link_entity_id,
    l.entity_title_snapshot AS reference_link_entity_title_snapshot,
    (
      SELECT COALESCE(jsonb_agg(
        jsonb_build_object(
          'link_kind', lx.link_kind,
          'entity_type', lx.entity_type,
          'entity_id', lx.entity_id,
          'entity_title_snapshot', lx.entity_title_snapshot,
          'created_at', lx.created_at
        )
      ), '[]'::jsonb)
      FROM procohere.meeting_agenda_item_links lx
      WHERE lx.organization_id = mai.organization_id
        AND lx.meeting_agenda_item_id = mai.id
    ) AS links_json
FROM procohere.meeting_agenda_items mai
LEFT JOIN procohere.meeting_agenda_item_links l
  ON l.organization_id = mai.organization_id
 AND l.meeting_agenda_item_id = mai.id
 AND l.link_kind = 'reference';
```

**Purpose:** Returns agenda items with flattened reference link fields and a `links_json` column for future extensibility.

**Why use this view:**
1. **Performance** - One round-trip returns agenda items + links (no N+1 queries)
2. **Consistency** - UI always sees the same shape
3. **Simpler client code** - No join glue needed in C#

**Columns returned:**
- All columns from `meeting_agenda_items`
- `reference_link_entity_type` - The linked entity type (or NULL)
- `reference_link_entity_id` - The linked entity ID (or NULL)
- `reference_link_entity_title_snapshot` - The cached title (or NULL)
- `links_json` - JSONB array of all links for future multi-link support

---

## Legacy Overload Cleanup

The following legacy function overloads should be dropped to avoid confusion:

```sql
-- insert_meeting_agenda_item legacy overloads
DROP FUNCTION IF EXISTS procohere.insert_meeting_agenda_item(
  uuid, uuid, uuid, uuid, text, text, text, integer, boolean, character varying, character varying, uuid, character varying
);
DROP FUNCTION IF EXISTS procohere.insert_meeting_agenda_item(
  uuid, text, text, text, integer, boolean, text, text, text, jsonb, text, text
);

-- insert_meeting_note legacy overload
DROP FUNCTION IF EXISTS procohere.insert_meeting_note(
  uuid, uuid, uuid, uuid, text, boolean, character varying
);

-- insert_meeting_prep_item legacy overloads
DROP FUNCTION IF EXISTS procohere.insert_meeting_prep_item(
  uuid, uuid, uuid, uuid, text, text, character varying, uuid, boolean
);
DROP FUNCTION IF EXISTS procohere.insert_meeting_prep_item(
  uuid, uuid, text, text, text, text, integer, boolean, text, uuid, text
);

-- update_meeting_agenda_item legacy overload
DROP FUNCTION IF EXISTS procohere.update_meeting_agenda_item(
  uuid, text, text, text, integer, boolean, boolean, timestamptz, text, text, text, text, jsonb, text, text, timestamptz
);

-- update_meeting_prep_item_as_assignee legacy overload
DROP FUNCTION IF EXISTS procohere.update_meeting_prep_item_as_assignee(
  uuid, text, text, text, timestamptz
);

-- update_meeting_prep_item_as_requester legacy overload
DROP FUNCTION IF EXISTS procohere.update_meeting_prep_item_as_requester(
  uuid, text, text, uuid, text, text, integer, boolean, timestamptz, uuid, text
);
```

---

## Change Discipline

- Adding a function requires updating this document
- Changing a function used by RLS requires re-auditing policies
- Undocumented functions are defects
