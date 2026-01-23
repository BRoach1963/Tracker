# 15 – AI Vector Store (pgvector)

This document defines the **authoritative implementation contract** for ProCohere’s Postgres-backed vector store.

The vector store is used for:
- semantic search over help documentation (RAG grounding)
- semantic search over user data (meetings, agenda items, prep items, tasks, goals, metrics, feedback)
- AI “suggest” experiences (agenda suggestions, prep suggestions) based on context
- insight generation (pattern detection and summarization)

This is a **security boundary**:
- the vector store must never widen visibility beyond what a user can already see
- RLS must enforce organization isolation and entity visibility
- indexing must be incremental and idempotent

---

## Design Goals

### 1) Multi-tenant correctness
Every row is scoped by `organization_id`. No cross-org reads or writes are permitted.

### 2) Visibility-preserving retrieval
Vector search is **never** an authorization mechanism. It is retrieval acceleration only.

### 3) Incremental indexing
Indexing must upsert by stable key (`organization_id`, `entity_type`, `entity_id`, `chunk_index`) and avoid duplicates.

### 4) Performance
Use ANN indexing on `embedding` (HNSW recommended), and keep the index small with a partial predicate (`is_deleted = false`).

---

## Schema: procohere.vector_embeddings

### Purpose
Stores chunked text and its embedding for semantic similarity search.

This table is intentionally generic:
- help-doc chunks and user-data chunks live in the same structure
- chunking is supported for long entities
- content is optional, but recommended so we can ground responses without re-hitting every source table

### Column Contract

| Column | Type | Null | Default | Meaning |
|---|---|---:|---|---|
| id | uuid | NO | gen_random_uuid() | Row identity |
| organization_id | uuid | NO | — | Tenant scope (FK → public.organizations.id) |
| entity_type | varchar(64) | NO | — | Canonical type (examples below) |
| entity_id | uuid | NO | — | Entity identity within its table |
| chunk_index | int | NO | 0 | 0-based ordinal within the entity |
| content_hash | varchar(64) | NO | — | Hash of normalized content |
| content_preview | varchar(500) | YES | — | Short excerpt for quick UI/debugging |
| content | text | YES | — | Stored chunk text |
| embedding | vector(768) | YES | — | pgvector embedding |
| embedding_dimensions | int | NO | 768 | Must remain 768 unless the model changes |
| model_name | varchar(100) | NO | text-embedding-004 | Embedding model identifier |
| model_version | varchar(50) | YES | — | Optional model version tag |
| metadata | jsonb | YES | — | Structured metadata (see below) |
| is_deleted | boolean | NO | false | Soft delete |
| created_at | timestamptz | NO | now() | Audit |
| updated_at | timestamptz | NO | now() | Audit |
| deleted_at | timestamptz | YES | — | Audit |
| deleted_by | uuid | YES | — | Audit |

### Canonical entity_type values

**Documentation**
- `help_doc`

**Core product entities**
- `team_member`
- `meeting`
- `meeting_agenda_item`
- `meeting_prep_item`
- `task`
- `goal`
- `metric`
- `feedback`

### metadata JSONB contract

Recommended keys:

| Key | Type | Meaning |
|---|---|---|
| source | string | `help_docs` or `user_data` |
| source_id | string | For docs: filename/slug; for user data: stable description |
| title | string | Title snapshot used for embedding context |
| updated_at_source | string | ISO timestamp of the entity at indexing time |
| visibility_hint | object | Optional hints (never used as auth) |
| chunk | object | `{ index, total, strategy }` |

---

## DDL (Tables, Constraints, Indexes, Triggers, RLS)

### Required Extension

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

### Table

```sql
CREATE TABLE IF NOT EXISTS procohere.vector_embeddings
(
  id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id       uuid NOT NULL REFERENCES public.organizations(id),

  entity_type           varchar(64) NOT NULL,
  entity_id             uuid NOT NULL,
  chunk_index           integer NOT NULL DEFAULT 0,

  content_hash          varchar(64) NOT NULL,
  content_preview       varchar(500) NULL,
  content               text NULL,

  embedding             vector(768) NULL,
  embedding_dimensions  integer NOT NULL DEFAULT 768,

  model_name            varchar(100) NOT NULL DEFAULT 'text-embedding-004',
  model_version         varchar(50) NULL,

  metadata              jsonb NULL,

  is_deleted            boolean NOT NULL DEFAULT false,
  created_at            timestamptz NOT NULL DEFAULT now(),
  updated_at            timestamptz NOT NULL DEFAULT now(),
  deleted_at            timestamptz NULL,
  deleted_by            uuid NULL REFERENCES public.users(id),

  CONSTRAINT vector_embeddings_unique_entity_chunk
    UNIQUE (organization_id, entity_type, entity_id, chunk_index),

  CONSTRAINT vector_embeddings_embedding_dimensions_chk
    CHECK (embedding_dimensions = 768),

  CONSTRAINT vector_embeddings_entity_type_chk
    CHECK (char_length(entity_type) > 0)
);
```

### Indexes

```sql
CREATE INDEX IF NOT EXISTS ix_vector_embeddings_org_entity
ON procohere.vector_embeddings (organization_id, entity_type, entity_id)
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_vector_embeddings_org_entity_type
ON procohere.vector_embeddings (organization_id, entity_type)
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_vector_embeddings_content_hash
ON procohere.vector_embeddings (organization_id, entity_type, entity_id, content_hash)
WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_vector_embeddings_embedding_hnsw
ON procohere.vector_embeddings
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64)
WHERE is_deleted = false;
```

### Triggers

```sql
CREATE TRIGGER tr_vector_embeddings_set_updated_at
BEFORE UPDATE ON procohere.vector_embeddings
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
```

---

## RLS Model

### Requirements
1. Organization isolation: `organization_id = get_current_organization_id()`
2. No visibility widening: vectors are readable only if the user can see the referenced entity
3. Writes should be constrained to the intended indexing path (client vs RPC vs service role)

### Helper function: entity visibility (contract)

```sql
CREATE OR REPLACE FUNCTION procohere.rls_can_see_vector_entity(
  p_entity_type varchar,
  p_entity_id uuid
)
RETURNS boolean
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
  IF p_entity_type = 'help_doc' THEN
    RETURN true;
  END IF;

  RETURN true;
END;
$$;
```

Replace the stub body with the real per-entity checks you already use in your RLS helpers.

### Table RLS

```sql
ALTER TABLE procohere.vector_embeddings ENABLE ROW LEVEL SECURITY;
ALTER TABLE procohere.vector_embeddings FORCE ROW LEVEL SECURITY;

CREATE POLICY vector_embeddings_select
ON procohere.vector_embeddings
FOR SELECT
TO authenticated
USING
(
  organization_id = procohere.get_current_organization_id()
  AND is_deleted = false
  AND procohere.rls_can_see_vector_entity(entity_type, entity_id)
);

CREATE POLICY vector_embeddings_write
ON procohere.vector_embeddings
FOR INSERT, UPDATE, DELETE
TO authenticated
USING
(
  organization_id = procohere.get_current_organization_id()
)
WITH CHECK
(
  organization_id = procohere.get_current_organization_id()
);
```

---

## Querying the Vector Store

### No named parameters in raw SQL
Postgres does not support `:named_parameters` in the SQL editor.

Invalid in Supabase SQL editor:

```sql
embedding <=> :query_embedding
```

Use a CTE (or positional `$1` parameters via a prepared statement).

### Example: top-K similarity search (CTE style)

```sql
WITH q AS (
  SELECT '[0.01, 0.02, 0.03]'::vector AS embedding
)
SELECT
  v.id,
  v.entity_type,
  v.entity_id,
  v.chunk_index,
  v.content_preview,
  1 - (v.embedding <=> q.embedding) AS similarity
FROM procohere.vector_embeddings v
CROSS JOIN q
WHERE v.organization_id = procohere.get_current_organization_id()
  AND v.is_deleted = false
ORDER BY v.embedding <=> q.embedding
LIMIT 10;
```

### Recommended: RPC wrapper for app usage

```sql
CREATE OR REPLACE FUNCTION procohere.vector_search(
  p_query_embedding vector(768),
  p_top_k integer DEFAULT 5,
  p_entity_types varchar[] DEFAULT NULL,
  p_min_similarity double precision DEFAULT 0.40
)
RETURNS TABLE
(
  id uuid,
  entity_type varchar,
  entity_id uuid,
  chunk_index integer,
  content_preview varchar,
  content text,
  similarity double precision
)
LANGUAGE sql
STABLE
AS $$
  SELECT
    v.id,
    v.entity_type,
    v.entity_id,
    v.chunk_index,
    v.content_preview,
    v.content,
    1 - (v.embedding <=> p_query_embedding) AS similarity
  FROM procohere.vector_embeddings v
  WHERE v.organization_id = procohere.get_current_organization_id()
    AND v.is_deleted = false
    AND (p_entity_types IS NULL OR v.entity_type = ANY (p_entity_types))
    AND (1 - (v.embedding <=> p_query_embedding)) >= p_min_similarity
  ORDER BY v.embedding <=> p_query_embedding
  LIMIT p_top_k;
$$;
```

---

## Indexing Rules

### Upsert key
`(organization_id, entity_type, entity_id, chunk_index)`

### Idempotency
Indexers must compute `content_hash`. If unchanged, skip re-embedding.

### Soft delete
Prefer soft delete for rebuild workflows.

---

End of AI Vector Store documentation.
