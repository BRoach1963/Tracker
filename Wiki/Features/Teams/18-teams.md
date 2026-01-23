# Teams (Lightweight, Multi-Team Membership Model)

## Purpose
Teams are **named, persistent groups** inside an organization. They model how real companies work (Platform, Store Ops, Legal, Leadership), without turning Pro Cohere into a project management or HR system.

Teams complement (not replace):
- **Manager hierarchy** (`team_members.manager_team_member_id`) → reporting chain
- **Projects** → time-scoped work containers
- **Meetings** → ad-hoc or scheduled gatherings

Teams exist to answer:
- “Who is in this working group?”
- “Who should see team-scoped items by default?”
- “What group does this meeting/goal/note relate to?”

---

## Current Schema Status
You already have:

### `procohere.teams`
- `id`, `organization_id`
- `name`, `description`
- `parent_team_id` (optional hierarchy)
- `lead_team_member_id` (optional leader)
- soft delete + timestamps

### `procohere.team_members`
- includes `manager_team_member_id` for the reporting chain

### Missing Piece (the actual gap)
To support “a team member can be on multiple teams,” you need a join table:

- `procohere.team_memberships` (**to add**)

Without this table, Teams can be named but cannot represent real membership across multiple teams.

---

## What We Need To Add (DB)
### 1) `procohere.team_memberships` table
A many-to-many join between `teams` and `team_members`.

**Core rules**
- A team member can belong to many teams.
- A team can have many team members.
- Membership should be soft-deletable.
- Ensure only one active membership row per `(team_id, team_member_id)`.

**Suggested membership role**
- `member`
- `lead`
- `viewer`

Note: you already have `teams.lead_team_member_id`. The membership `role` lets you represent additional leads (or future patterns) without schema churn.

---

## Supabase SQL (DDL + Indexes + RLS)
> This script assumes existing helpers:
> - `procohere.get_current_organization_id()`
> - `procohere.get_current_team_member_id()`

```sql
BEGIN;

-- -------------------------------------------------------------------------------------
-- Table: procohere.team_memberships
-- -------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS procohere.team_memberships
(
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id  uuid NOT NULL,

  team_id          uuid NOT NULL,
  team_member_id   uuid NOT NULL,

  role             text NOT NULL DEFAULT 'member',

  is_deleted       boolean NOT NULL DEFAULT false,
  created_at       timestamp with time zone NOT NULL DEFAULT now(),
  deleted_at       timestamp with time zone NULL,
  deleted_by       uuid NULL
);

ALTER TABLE procohere.team_memberships
  DROP CONSTRAINT IF EXISTS team_memberships_role_chk;

ALTER TABLE procohere.team_memberships
  ADD CONSTRAINT team_memberships_role_chk
  CHECK (role IN ('member','lead','viewer'));

-- One active membership per team+member
CREATE UNIQUE INDEX IF NOT EXISTS ux_team_memberships_unique_active
  ON procohere.team_memberships (team_id, team_member_id)
  WHERE is_deleted = false;

-- Lookup members of a team
CREATE INDEX IF NOT EXISTS ix_team_memberships_team
  ON procohere.team_memberships (team_id)
  WHERE is_deleted = false;

-- Lookup teams for a member (scoped)
CREATE INDEX IF NOT EXISTS ix_team_memberships_member
  ON procohere.team_memberships (organization_id, team_member_id)
  WHERE is_deleted = false;

-- -------------------------------------------------------------------------------------
-- Foreign Keys
-- -------------------------------------------------------------------------------------
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_team_memberships_team'
  ) THEN
    ALTER TABLE procohere.team_memberships
      ADD CONSTRAINT fk_team_memberships_team
      FOREIGN KEY (team_id)
      REFERENCES procohere.teams (id);
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_team_memberships_team_member'
  ) THEN
    ALTER TABLE procohere.team_memberships
      ADD CONSTRAINT fk_team_memberships_team_member
      FOREIGN KEY (team_member_id)
      REFERENCES procohere.team_members (id);
  END IF;
END $$;

-- -------------------------------------------------------------------------------------
-- RLS
-- -------------------------------------------------------------------------------------
ALTER TABLE procohere.team_memberships ENABLE ROW LEVEL SECURITY;

-- Read membership rows if:
--  - you're the member, OR
--  - you're the team lead
DROP POLICY IF EXISTS team_memberships_select ON procohere.team_memberships;
CREATE POLICY team_memberships_select
ON procohere.team_memberships
FOR SELECT
USING (
  organization_id = procohere.get_current_organization_id()
  AND is_deleted = false
  AND (
    team_member_id = procohere.get_current_team_member_id()
    OR EXISTS (
      SELECT 1
      FROM procohere.teams t
      WHERE t.id = team_id
        AND t.is_deleted = false
        AND t.lead_team_member_id = procohere.get_current_team_member_id()
    )
  )
);

-- Write membership rows if you're the team lead
DROP POLICY IF EXISTS team_memberships_write ON procohere.team_memberships;
CREATE POLICY team_memberships_write
ON procohere.team_memberships
FOR ALL
USING (
  organization_id = procohere.get_current_organization_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.teams t
    WHERE t.id = team_id
      AND t.is_deleted = false
      AND t.lead_team_member_id = procohere.get_current_team_member_id()
  )
)
WITH CHECK (
  organization_id = procohere.get_current_organization_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.teams t
    WHERE t.id = team_id
      AND t.is_deleted = false
      AND t.lead_team_member_id = procohere.get_current_team_member_id()
  )
);

COMMIT;
```

---

## How Teams Should Be Handled (Functional + UI Guidance)
This mirrors the “Projects” handling style: lightweight, real-world, and not overbuilt.

### Team lifecycle
- Teams are **persistent**.
- Teams are rarely deleted in real orgs; they’re **renamed** or **archived**.
- Prefer `is_deleted` only for true removal; use “archived” concept later if needed.

### Who can create/manage teams
Recommended rule set (simple):
- Only org admins can create teams (or allow any manager role if you prefer).
- Only the team lead (or admin) can:
  - add/remove members
  - set lead
  - update name/description

If you don’t have admin role enforcement at DB level yet, start with “team lead can manage membership” (as in the RLS above).

### Team membership semantics
- A team member can be on many teams.
- Membership is separate from manager hierarchy.
- Team membership controls:
  - team-scoped views (team sync meetings, team notes, etc.)
  - suggested attendees (optional)
  - AI context and “suggest agenda/prep”

### Team naming (guardrails for UX)
- Teams are **identity-based**: “Platform”, “Legal”, “District Managers”.
- Projects are **time-based**: “Q2 Website Redesign”.
- UI should not show completion/progress for teams.
- If a user tries to create a team named like a project, UI can gently suggest creating a Project.

---

## Standard Queries (what the app needs)
### 1) Get teams the current member belongs to
```sql
select t.*
from procohere.teams t
join procohere.team_memberships tm
  on tm.team_id = t.id
 and tm.is_deleted = false
where t.is_deleted = false;
```

### 2) Get members for a team
```sql
select tm.*, v.display_name, v.email, v.job_title
from procohere.team_memberships tm
join procohere.v_team_members v
  on v.id = tm.team_member_id
where tm.team_id = :team_id
  and tm.is_deleted = false;
```

### 3) “Team picker” for meeting creation
- If meeting type = “team sync”, pick a team and default attendees from membership.
- Still allow manual add/remove of attendees after defaulting.

---

## Interaction With Projects
Projects and Teams overlap, but they are not the same:

- A **Project** can optionally be linked to a Team (future enhancement).
- A **Project status meeting** can default attendees from **project members**.
- A **Team sync meeting** can default attendees from **team members**.

In practice:
- Teams keep working relationships stable.
- Projects come and go, and people rotate in/out.

---

## AI / Vector Store Implications (future-safe)
Adding Teams gives the AI a clean, named entity to attach context to:
- “Suggest agenda items for the Platform team sync”
- “Summarize open risks discussed by the Store Ops team”
- “Draft prep items for this team meeting”

Teams should be included in the vectorization plan as an entity type once embeddings are active.

---

## What Claude Needs To Implement In UI
### Team UI components
- Team list (my teams)
- Team detail (name/description/lead)
- Team members roster management (add/remove)

### Membership UI rules
- Only show “Manage members” UI if current team member is the team lead (or admin once implemented).
- Membership can be soft-deleted (remove) and re-added.

### Meeting creation integration (minimal)
- Add meeting type: “Team Sync”
- If selected:
  - prompt for team
  - default attendees from `team_memberships`
  - allow edits to attendees

---

## Summary
- Teams already exist.
- To fully model “people are on multiple named teams,” you must add `team_memberships`.
- Keep Teams lightweight and persistent; use Projects for time-scoped work.
- This unlocks correct UX, correct AI context, and real-world organizational modeling.
