# Projects (Pro Cohere) — Functional Overview + Database Contract

This page defines **Projects** as we intend to implement them in Pro Cohere. It is written for developers (and Claude) so the UI can be implemented without guessing.

---

## What a Project is (and is not)

### Project (definition)
A **Project** is a **lightweight container for a manager-led effort** that:
- defines **who is involved** (membership)
- groups related items (goals, tasks, meetings, notes, metrics) via links

Projects exist to **organize work and conversations**. They are intentionally **not** a full project-management system.

### Guardrails (what we do NOT build)
Projects do **not** include:
- sprint/kanban boards
- time tracking
- dependencies / critical path
- story points / backlog management
- milestone planning workflows
- project-specific KPI systems

If a “progress measurement” is needed, we use existing **Goals / Metrics / Tasks**.

---

## Core behaviors

### Visibility / Access
A team member can see a project if they are:
- the **Project Owner**, or
- a **Project Member** (explicitly added)

This is enforced in the database via RLS.

### Membership
Membership has two purposes:
1) **Access** (who can see the project)
2) **Convenience** (default attendee list and assignee picklists)

Membership is intentionally simple:
- `member`
- `viewer`

Ownership is represented by `projects.owner_team_member_id`.

### Linking “stuff” to a Project
A project can be linked to **any entity** using a polymorphic join table:
- `project_links (project_id, entity_type, entity_id)`

This avoids scattering `project_id` columns across the schema and stays future-proof (e.g., Projects can later link to “projects” entities or new types without schema churn).

### Project status meetings
We support a meeting type such as `project_status` (UI concept). The database support is:
- meetings can optionally carry context: `context_type='project'` and `context_id=<project_id>`

When creating a project status meeting, the UI should:
1) pre-populate attendees from `project_members`
2) optionally suggest agenda items based on project-linked items (tasks/goals/notes/metrics)

---

## Database tables

### 1) `procohere.projects`
Stores the project itself.

Key fields:
- `organization_id`
- `owner_team_member_id`
- `title`, optional `description`
- coarse status: `active | on_hold | completed`
- optional scheduling hints: `start_date`, `target_date`
- soft delete: `is_deleted`, `deleted_at`, `deleted_by`
- archive: `is_archived`, `archived_at`
- timestamps: `created_at`, `updated_at`

### 2) `procohere.project_members`
Defines explicit membership and visibility.

Key fields:
- `project_id`
- `team_member_id`
- `role` (`member | viewer`)
- soft delete fields

### 3) `procohere.project_links`
Polymorphic links from project → entity.

Key fields:
- `project_id`
- `entity_type` (text)
- `entity_id` (uuid)
- optional `entity_title_snapshot`
- created metadata and soft delete fields

**Note:** We intentionally do not enforce foreign keys per `entity_type`. The application validates entity existence and access at runtime.

---

## Recommended `entity_type` values (initial)
These are the strings the UI should emit consistently:

- `goal`
- `task`
- `metric`
- `meeting`
- `note`
- `team_member`
- `project` (reserved for future, if/when we need project-to-project linking)
- `custom` (avoid unless we deliberately support it)

Consistency matters because queries and vectorization rely on it.

---

## Query patterns the UI will need

### List projects visible to me
Use `projects_select` RLS. The UI can query:
- `/rest/v1/projects?select=*&is_deleted=eq.false`

Optional filters:
- `status=eq.active`
- `is_archived=eq.false`

### Get project members
- `/rest/v1/project_members?select=*&project_id=eq.<projectId>&is_deleted=eq.false`

### Get linked entities
- `/rest/v1/project_links?select=*&project_id=eq.<projectId>&is_deleted=eq.false`

Then fetch per-entity lists (goals/tasks/metrics/etc.) by ID.
If you want to avoid N+1 later, we can add RPCs/views, but don’t block the MVP on that.

### Create a “project status meeting”
Insert a meeting with:
- `context_type='project'`
- `context_id=<projectId>`
and insert attendees from project_members.

---

## Implementation notes for Claude (UI)

### 1) Project creation flow
- Create project (title, description optional, status defaults to `active`)
- Owner is the current team_member
- Immediately add members (optional) via `project_members`
- Render project page that shows:
  - header + framing
  - linked goals/tasks/notes/meetings/metrics (sections)
  - “Create Project Status Meeting” action

### 2) Membership UX
Keep it simple:
- Add/remove members (soft delete rows)
- Role selection only if needed; otherwise default to `member`

### 3) Linking UX
When a user links an item (goal/task/meeting/note/metric) to a project:
- create a `project_links` row
- store a `entity_title_snapshot` for fast display and resilience

### 4) Don’t build PM UI
No boards, no “project progress percent.”
Projects are a container and a roster.

---

## Supabase SQL: DDL + RLS

The next section is the canonical SQL for creating Projects.

```sql
-- =====================================================================================
-- PROCOHERE: PROJECTS (DDL + RLS)
-- =====================================================================================
-- Creates:
--   procohere.projects
--   procohere.project_members
--   procohere.project_links
-- Adds optional meeting context columns if missing:
--   procohere.meetings.context_type
--   procohere.meetings.context_id
-- =====================================================================================

BEGIN;

-- ---------------------------------------------
-- Optional: meeting context support
-- ---------------------------------------------
ALTER TABLE procohere.meetings
  ADD COLUMN IF NOT EXISTS context_type text,
  ADD COLUMN IF NOT EXISTS context_id uuid;

CREATE INDEX IF NOT EXISTS ix_meetings_context
  ON procohere.meetings (organization_id, context_type, context_id)
  WHERE is_deleted = false;

-- ---------------------------------------------
-- Table: projects
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS procohere.projects
(
  id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id       uuid NOT NULL,
  owner_team_member_id  uuid NOT NULL,

  title                text NOT NULL,
  description          text NULL,

  status               text NOT NULL DEFAULT 'active',
  start_date           date NULL,
  target_date          date NULL,

  is_archived          boolean NOT NULL DEFAULT false,
  archived_at          timestamp with time zone NULL,

  is_deleted           boolean NOT NULL DEFAULT false,

  created_at           timestamp with time zone NOT NULL DEFAULT now(),
  updated_at           timestamp with time zone NOT NULL DEFAULT now(),

  deleted_at           timestamp with time zone NULL,
  deleted_by           uuid NULL
);

ALTER TABLE procohere.projects
  DROP CONSTRAINT IF EXISTS projects_status_chk;

ALTER TABLE procohere.projects
  ADD CONSTRAINT projects_status_chk
  CHECK (status IN ('active','on_hold','completed'));

CREATE INDEX IF NOT EXISTS ix_projects_org_owner
  ON procohere.projects (organization_id, owner_team_member_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_projects_org_status
  ON procohere.projects (organization_id, status)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_projects_org_archived
  ON procohere.projects (organization_id, is_archived, updated_at DESC)
  WHERE is_deleted = false;

DROP TRIGGER IF EXISTS tr_projects_set_updated_at ON procohere.projects;
CREATE TRIGGER tr_projects_set_updated_at
BEFORE UPDATE ON procohere.projects
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------
-- Table: project_members
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS procohere.project_members
(
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id  uuid NOT NULL,

  project_id       uuid NOT NULL,
  team_member_id   uuid NOT NULL,

  role             text NOT NULL DEFAULT 'member',

  is_deleted       boolean NOT NULL DEFAULT false,
  created_at       timestamp with time zone NOT NULL DEFAULT now(),
  deleted_at       timestamp with time zone NULL,
  deleted_by       uuid NULL
);

ALTER TABLE procohere.project_members
  DROP CONSTRAINT IF EXISTS project_members_role_chk;

ALTER TABLE procohere.project_members
  ADD CONSTRAINT project_members_role_chk
  CHECK (role IN ('member','viewer'));

CREATE INDEX IF NOT EXISTS ix_project_members_project
  ON procohere.project_members (project_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_project_members_member
  ON procohere.project_members (organization_id, team_member_id)
  WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS ux_project_members_unique_active
  ON procohere.project_members (project_id, team_member_id)
  WHERE is_deleted = false;

-- ---------------------------------------------
-- Table: project_links
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS procohere.project_links
(
  id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id        uuid NOT NULL,

  project_id             uuid NOT NULL,

  entity_type            text NOT NULL,
  entity_id              uuid NOT NULL,

  entity_title_snapshot  text NULL,

  created_by_team_member_id uuid NOT NULL,
  created_at             timestamp with time zone NOT NULL DEFAULT now(),

  is_deleted             boolean NOT NULL DEFAULT false,
  deleted_at             timestamp with time zone NULL,
  deleted_by             uuid NULL
);

CREATE INDEX IF NOT EXISTS ix_project_links_project
  ON procohere.project_links (project_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_project_links_entity
  ON procohere.project_links (organization_id, entity_type, entity_id)
  WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS ux_project_links_unique_active
  ON procohere.project_links (project_id, entity_type, entity_id)
  WHERE is_deleted = false;

-- ---------------------------------------------
-- Foreign keys (safe, non-polymorphic)
-- ---------------------------------------------
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'fk_projects_owner_team_member'
  ) THEN
    ALTER TABLE procohere.projects
      ADD CONSTRAINT fk_projects_owner_team_member
      FOREIGN KEY (owner_team_member_id)
      REFERENCES procohere.team_members (id);
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'fk_project_members_project'
  ) THEN
    ALTER TABLE procohere.project_members
      ADD CONSTRAINT fk_project_members_project
      FOREIGN KEY (project_id)
      REFERENCES procohere.projects (id);
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'fk_project_members_team_member'
  ) THEN
    ALTER TABLE procohere.project_members
      ADD CONSTRAINT fk_project_members_team_member
      FOREIGN KEY (team_member_id)
      REFERENCES procohere.team_members (id);
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'fk_project_links_project'
  ) THEN
    ALTER TABLE procohere.project_links
      ADD CONSTRAINT fk_project_links_project
      FOREIGN KEY (project_id)
      REFERENCES procohere.projects (id);
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'fk_project_links_created_by_team_member'
  ) THEN
    ALTER TABLE procohere.project_links
      ADD CONSTRAINT fk_project_links_created_by_team_member
      FOREIGN KEY (created_by_team_member_id)
      REFERENCES procohere.team_members (id);
  END IF;
END $$;

-- ---------------------------------------------
-- RLS
-- ---------------------------------------------
ALTER TABLE procohere.projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE procohere.project_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE procohere.project_links ENABLE ROW LEVEL SECURITY;

-- Projects: select if owner OR member
DROP POLICY IF EXISTS projects_select ON procohere.projects;
CREATE POLICY projects_select
ON procohere.projects
FOR SELECT
USING (
  organization_id = procohere.get_current_organization_id()
  AND is_deleted = false
  AND (
    owner_team_member_id = procohere.get_current_team_member_id()
    OR EXISTS (
      SELECT 1
      FROM procohere.project_members pm
      WHERE pm.project_id = id
        AND pm.is_deleted = false
        AND pm.team_member_id = procohere.get_current_team_member_id()
    )
  )
);

-- Projects: write owner only
DROP POLICY IF EXISTS projects_write ON procohere.projects;
CREATE POLICY projects_write
ON procohere.projects
FOR ALL
USING (
  organization_id = procohere.get_current_organization_id()
  AND owner_team_member_id = procohere.get_current_team_member_id()
)
WITH CHECK (
  organization_id = procohere.get_current_organization_id()
  AND owner_team_member_id = procohere.get_current_team_member_id()
);

-- Project members: select if can see project
DROP POLICY IF EXISTS project_members_select ON procohere.project_members;
CREATE POLICY project_members_select
ON procohere.project_members
FOR SELECT
USING (
  organization_id = procohere.get_current_organization_id()
  AND is_deleted = false
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.is_deleted = false
      AND (
        p.owner_team_member_id = procohere.get_current_team_member_id()
        OR EXISTS (
          SELECT 1
          FROM procohere.project_members pm2
          WHERE pm2.project_id = p.id
            AND pm2.is_deleted = false
            AND pm2.team_member_id = procohere.get_current_team_member_id()
        )
      )
  )
);

-- Project members: write owner only
DROP POLICY IF EXISTS project_members_write ON procohere.project_members;
CREATE POLICY project_members_write
ON procohere.project_members
FOR ALL
USING (
  organization_id = procohere.get_current_organization_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.owner_team_member_id = procohere.get_current_team_member_id()
      AND p.is_deleted = false
  )
)
WITH CHECK (
  organization_id = procohere.get_current_organization_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.owner_team_member_id = procohere.get_current_team_member_id()
      AND p.is_deleted = false
  )
);

-- Project links: select if can see project
DROP POLICY IF EXISTS project_links_select ON procohere.project_links;
CREATE POLICY project_links_select
ON procohere.project_links
FOR SELECT
USING (
  organization_id = procohere.get_current_organization_id()
  AND is_deleted = false
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.is_deleted = false
      AND (
        p.owner_team_member_id = procohere.get_current_team_member_id()
        OR EXISTS (
          SELECT 1
          FROM procohere.project_members pm
          WHERE pm.project_id = p.id
            AND pm.is_deleted = false
            AND pm.team_member_id = procohere.get_current_team_member_id()
        )
      )
  )
);

-- Project links: write owner only
DROP POLICY IF EXISTS project_links_write ON procohere.project_links;
CREATE POLICY project_links_write
ON procohere.project_links
FOR ALL
USING (
  organization_id = procohere.get_current_organization_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.owner_team_member_id = procohere.get_current_team_member_id()
      AND p.is_deleted = false
  )
)
WITH CHECK (
  organization_id = procohere.get_current_organization_id()
  AND created_by_team_member_id = procohere.get_current_team_member_id()
  AND EXISTS (
    SELECT 1
    FROM procohere.projects p
    WHERE p.id = project_id
      AND p.owner_team_member_id = procohere.get_current_team_member_id()
      AND p.is_deleted = false
  )
);

COMMIT;

```
