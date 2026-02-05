# Chronicle Note Links - Database Schema Proposal

## Context

We're building a "Chronicle" feature in ProCohere - a notes system where users can create notes and link them to various entities (goals, tasks, meetings, people, companies).

**Current Problem:** The existing `notes` table uses nullable FK columns, limiting each note to ONE link per entity type. This is too restrictive - meeting notes often discuss multiple goals, a strategy note might relate to several tasks, etc.

---

## Current Schema (Problematic)

```sql
CREATE TABLE procohere.notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id),
    team_id UUID REFERENCES procohere.teams(id),
    
    title TEXT,
    content TEXT,
    category TEXT,
    tags TEXT[],
    is_pinned BOOLEAN DEFAULT FALSE,
    is_archived BOOLEAN DEFAULT FALSE,
    
    -- LIMITATION: Can only link to ONE of each entity type
    linked_goal_id UUID REFERENCES procohere.goals(id),
    linked_task_id UUID REFERENCES procohere.tasks(id),
    linked_meeting_id UUID REFERENCES procohere.meetings(id),
    linked_person_id UUID REFERENCES procohere.persons(id),
    linked_company_id UUID REFERENCES procohere.companies(id),
    
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES auth.users(id)
);
```

### Why This Is Limiting

1. A note about "Q1 Strategy Meeting" can only link to ONE goal, even if it discussed THREE goals
2. Meeting notes often span multiple topics/entities
3. Forces users to duplicate notes or lose context
4. Not scalable - adding new entity types requires schema changes to the notes table

---

## Proposed Schema

### Step 1: Remove FK columns from notes table

```sql
CREATE TABLE procohere.notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id),
    team_id UUID REFERENCES procohere.teams(id),
    
    title TEXT,
    content TEXT,
    category TEXT,
    tags TEXT[],
    is_pinned BOOLEAN DEFAULT FALSE,
    is_archived BOOLEAN DEFAULT FALSE,
    
    -- NO linked_*_id columns - relationships live in note_links table
    
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES auth.users(id)
);
```

### Step 2: Create note_links join table

```sql
CREATE TABLE procohere.note_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    note_id UUID NOT NULL REFERENCES procohere.notes(id) ON DELETE CASCADE,
    
    -- Polymorphic link pattern: type + id
    entity_type TEXT NOT NULL,  -- 'goal', 'task', 'meeting', 'person', 'company'
    entity_id UUID NOT NULL,
    
    -- Audit fields
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by UUID REFERENCES auth.users(id),
    
    -- Prevent duplicate links (same note can't link to same entity twice)
    CONSTRAINT unique_note_entity UNIQUE (note_id, entity_type, entity_id),
    
    -- Validate entity_type values
    CONSTRAINT valid_entity_type CHECK (
        entity_type IN ('goal', 'task', 'meeting', 'person', 'company')
    )
);

-- Indexes for efficient querying
CREATE INDEX idx_note_links_note_id ON procohere.note_links(note_id);
CREATE INDEX idx_note_links_entity ON procohere.note_links(entity_type, entity_id);
```

### Step 3: RLS Policies for note_links

```sql
-- Enable RLS
ALTER TABLE procohere.note_links ENABLE ROW LEVEL SECURITY;

-- Users can see links for notes they own or have team access to
CREATE POLICY "Users can view their note links"
ON procohere.note_links FOR SELECT
USING (
    note_id IN (
        SELECT id FROM procohere.notes 
        WHERE user_id = auth.uid() 
        OR team_id IN (SELECT team_id FROM procohere.team_members WHERE user_id = auth.uid())
    )
);

-- Users can create links for notes they own
CREATE POLICY "Users can create links for their notes"
ON procohere.note_links FOR INSERT
WITH CHECK (
    note_id IN (
        SELECT id FROM procohere.notes 
        WHERE user_id = auth.uid()
    )
);

-- Users can delete links for notes they own
CREATE POLICY "Users can delete links for their notes"
ON procohere.note_links FOR DELETE
USING (
    note_id IN (
        SELECT id FROM procohere.notes 
        WHERE user_id = auth.uid()
    )
);
```

---

## Design Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| **Polymorphic pattern (entity_type + entity_id)** | Well-established pattern for linking to multiple table types. Single table handles all entity types. Adding a new entity type only requires updating the CHECK constraint. |
| **ON DELETE CASCADE** | When a note is deleted, its links are automatically cleaned up. No orphaned links. |
| **UNIQUE constraint on (note_id, entity_type, entity_id)** | Prevents accidentally linking the same entity twice to the same note. |
| **CHECK constraint on entity_type** | Ensures only valid entity types are stored. Catches typos and invalid data at the database level. |
| **No soft-delete on links** | Links are simple associations. When removed, they're gone. The parent note has soft-delete for recovery scenarios. |
| **Compound index on (entity_type, entity_id)** | Enables fast reverse lookups: "find all notes linked to this goal" |
| **No FK to individual entity tables** | One column can't reference multiple tables. Application layer validates entity existence before creating links. |
| **created_by field** | Audit trail for who created the link (useful for team scenarios). |

---

## Query Examples

### Get all links for a note
```sql
SELECT * FROM procohere.note_links WHERE note_id = $1;
```

### Get all notes linked to a specific goal
```sql
SELECT n.* FROM procohere.notes n
JOIN procohere.note_links nl ON nl.note_id = n.id
WHERE nl.entity_type = 'goal' AND nl.entity_id = $1
AND n.is_deleted = FALSE;
```

### Link a note to multiple entities (batch insert)
```sql
INSERT INTO procohere.note_links (note_id, entity_type, entity_id, created_by)
VALUES 
    ($1, 'goal', $2, auth.uid()),
    ($1, 'task', $3, auth.uid()),
    ($1, 'person', $4, auth.uid());
```

### Replace all links for a note (on save)
```sql
-- Delete existing links
DELETE FROM procohere.note_links WHERE note_id = $1;

-- Insert new links
INSERT INTO procohere.note_links (note_id, entity_type, entity_id, created_by)
VALUES 
    ($1, 'goal', $2, auth.uid()),
    ($1, 'task', $3, auth.uid());
```

### Count notes per entity
```sql
SELECT entity_type, entity_id, COUNT(*) as note_count
FROM procohere.note_links
GROUP BY entity_type, entity_id;
```

---

## Alternative Considered: Separate Join Tables

```sql
-- Alternative approach: One join table per entity type
CREATE TABLE procohere.note_goals (note_id UUID, goal_id UUID);
CREATE TABLE procohere.note_tasks (note_id UUID, task_id UUID);
CREATE TABLE procohere.note_meetings (note_id UUID, meeting_id UUID);
CREATE TABLE procohere.note_persons (note_id UUID, person_id UUID);
CREATE TABLE procohere.note_companies (note_id UUID, company_id UUID);
```

### Why We Rejected This

| Issue | Impact |
|-------|--------|
| More tables to maintain | 5+ tables instead of 1 |
| Schema changes for new entity types | Adding "note_projects" requires new table, new RLS policies, new indexes |
| Complex queries | "All links for a note" requires UNION across all 5 tables |
| More RLS policies | 5x the policy maintenance |
| Inconsistent patterns | Each table needs its own service methods |

---

## Questions for Validation

1. **Is the polymorphic pattern (entity_type + entity_id) appropriate here, or would separate join tables be cleaner despite the extra maintenance?**

2. **Should we add an `order` or `position` column to note_links for controlling display order of linked entities?**

3. **Is ON DELETE CASCADE the right choice, or should we soft-delete links when notes are soft-deleted?**

4. **Should we add a `link_context` or `relationship_type` field (e.g., "mentioned", "action item", "follow-up")?**

5. **Any concerns with the RLS policies - particularly the team access check in the SELECT policy?**

---

## Tech Stack Context

- **Database:** Supabase PostgreSQL with Row-Level Security (RLS)
- **Auth:** Supabase Auth (auth.uid() function available)
- **ORM:** None - using Dapper with direct SQL
- **Application:** .NET 8 desktop app (Avalonia UI)
