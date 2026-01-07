/*
 * TRACKER DATABASE - VECTOR EMBEDDINGS SCHEMA
 * PostgreSQL Edition with pgvector
 * 
 * Creates the AI/semantic search infrastructure:
 * - vector_embeddings: Unified storage for all document embeddings
 * - document_chunks: Source text chunks for embeddings
 * 
 * Prerequisites:
 * - pgvector extension must be installed: CREATE EXTENSION vector;
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

-- Ensure pgvector extension is available
CREATE EXTENSION IF NOT EXISTS vector;

-- =============================================================================
-- VECTOR_EMBEDDINGS TABLE
-- =============================================================================
-- Unified storage for AI embeddings used in semantic search.
-- Uses pgvector for native vector operations and HNSW indexing.
-- 
-- Embedding dimension: 1536 (OpenAI text-embedding-3-small default)
-- Can be changed based on your embedding model.

CREATE TABLE IF NOT EXISTS vector_embeddings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope (required for RLS)
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Entity reference (what this embedding represents)
    entity_type VARCHAR(100) NOT NULL,  -- 'TeamMember', 'OneOnOne', 'Task', 'Note', etc.
    entity_id UUID NOT NULL,            -- The ID of the source entity
    
    -- Chunk info (for large documents split into chunks)
    chunk_index INT NOT NULL DEFAULT 0,  -- 0 = whole document, 1+ = chunk number
    chunk_count INT NOT NULL DEFAULT 1,  -- Total chunks for this entity
    
    -- Content
    content TEXT NOT NULL,              -- The text that was embedded
    content_hash VARCHAR(64),           -- SHA-256 hash for deduplication
    
    -- The embedding vector (1536 dimensions for OpenAI)
    embedding vector(1536) NOT NULL,
    
    -- Metadata
    embedding_model VARCHAR(100) NOT NULL DEFAULT 'text-embedding-3-small',
    embedding_version INT NOT NULL DEFAULT 1,
    
    -- Token tracking (for cost monitoring)
    token_count INT,
    
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraint to prevent duplicate embeddings for same content
    CONSTRAINT uq_vector_embeddings_entity_chunk 
        UNIQUE (organization_id, entity_type, entity_id, chunk_index)
);

-- =============================================================================
-- VECTOR INDEXES (HNSW for approximate nearest neighbor search)
-- =============================================================================
-- HNSW (Hierarchical Navigable Small World) provides fast approximate search
-- Parameters: m = connections per layer, ef_construction = quality during build

-- Main similarity search index using cosine distance
CREATE INDEX IF NOT EXISTS ix_vector_embeddings_embedding_cosine
    ON vector_embeddings USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Alternative: L2 distance index (uncomment if needed)
-- CREATE INDEX IF NOT EXISTS ix_vector_embeddings_embedding_l2
--     ON vector_embeddings USING hnsw (embedding vector_l2_ops)
--     WITH (m = 16, ef_construction = 64);

-- =============================================================================
-- STANDARD INDEXES
-- =============================================================================

CREATE INDEX IF NOT EXISTS ix_vector_embeddings_organization_id 
    ON vector_embeddings(organization_id);
    
CREATE INDEX IF NOT EXISTS ix_vector_embeddings_entity 
    ON vector_embeddings(entity_type, entity_id);
    
CREATE INDEX IF NOT EXISTS ix_vector_embeddings_entity_type 
    ON vector_embeddings(organization_id, entity_type);

CREATE INDEX IF NOT EXISTS ix_vector_embeddings_content_hash 
    ON vector_embeddings(content_hash) WHERE content_hash IS NOT NULL;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_vector_embeddings_modified_at ON vector_embeddings;
CREATE TRIGGER trg_vector_embeddings_modified_at
    BEFORE UPDATE ON vector_embeddings
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

-- =============================================================================
-- HELPER FUNCTIONS FOR VECTOR SEARCH
-- =============================================================================

-- Function to search for similar embeddings within an organization
-- Returns the top N most similar embeddings using cosine similarity
CREATE OR REPLACE FUNCTION search_similar_embeddings(
    p_organization_id UUID,
    p_query_embedding vector(1536),
    p_limit INT DEFAULT 10,
    p_entity_types TEXT[] DEFAULT NULL,  -- Optional filter by entity type
    p_min_similarity FLOAT DEFAULT 0.0   -- Minimum similarity threshold
)
RETURNS TABLE (
    id UUID,
    entity_type VARCHAR(100),
    entity_id UUID,
    chunk_index INT,
    content TEXT,
    similarity FLOAT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ve.id,
        ve.entity_type,
        ve.entity_id,
        ve.chunk_index,
        ve.content,
        1 - (ve.embedding <=> p_query_embedding) AS similarity
    FROM vector_embeddings ve
    WHERE ve.organization_id = p_organization_id
      AND (p_entity_types IS NULL OR ve.entity_type = ANY(p_entity_types))
      AND 1 - (ve.embedding <=> p_query_embedding) >= p_min_similarity
    ORDER BY ve.embedding <=> p_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;

-- Function to search with text (requires embedding to be pre-computed by app)
-- This is just a convenience wrapper
CREATE OR REPLACE FUNCTION search_by_vector(
    p_organization_id UUID,
    p_embedding FLOAT[],  -- Array of floats (will be cast to vector)
    p_limit INT DEFAULT 10,
    p_entity_types TEXT[] DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    entity_type VARCHAR(100),
    entity_id UUID,
    content TEXT,
    similarity FLOAT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.id,
        s.entity_type,
        s.entity_id,
        s.content,
        s.similarity
    FROM search_similar_embeddings(
        p_organization_id,
        p_embedding::vector(1536),
        p_limit,
        p_entity_types,
        0.0
    ) s;
END;
$$ LANGUAGE plpgsql;

-- Function to delete embeddings for a specific entity
CREATE OR REPLACE FUNCTION delete_entity_embeddings(
    p_organization_id UUID,
    p_entity_type VARCHAR(100),
    p_entity_id UUID
)
RETURNS INT AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM vector_embeddings
    WHERE organization_id = p_organization_id
      AND entity_type = p_entity_type
      AND entity_id = p_entity_id;
    
    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Function to get embedding stats for an organization
CREATE OR REPLACE FUNCTION get_embedding_stats(p_organization_id UUID)
RETURNS TABLE (
    entity_type VARCHAR(100),
    embedding_count BIGINT,
    total_tokens BIGINT,
    avg_chunk_size NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ve.entity_type,
        COUNT(*) AS embedding_count,
        COALESCE(SUM(ve.token_count), 0) AS total_tokens,
        AVG(LENGTH(ve.content))::NUMERIC AS avg_chunk_size
    FROM vector_embeddings ve
    WHERE ve.organization_id = p_organization_id
    GROUP BY ve.entity_type
    ORDER BY embedding_count DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- DOCUMENT_CHUNKS TABLE (optional - for tracking source chunks)
-- =============================================================================
-- If you need to store the original chunks before embedding,
-- or want to re-embed later without re-chunking.

CREATE TABLE IF NOT EXISTS document_chunks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Organization scope
    organization_id UUID NOT NULL REFERENCES organizations(id),
    
    -- Source entity
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    
    -- Chunk info
    chunk_index INT NOT NULL,
    chunk_count INT NOT NULL,
    
    -- Content
    content TEXT NOT NULL,
    content_hash VARCHAR(64),
    
    -- Metadata
    start_offset INT,  -- Character offset in original document
    end_offset INT,
    
    -- Status
    is_embedded BOOLEAN NOT NULL DEFAULT false,
    last_embedded_at TIMESTAMPTZ,
    
    -- Audit
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT uq_document_chunks_entity_chunk 
        UNIQUE (organization_id, entity_type, entity_id, chunk_index)
);

-- Indexes for document_chunks
CREATE INDEX IF NOT EXISTS ix_document_chunks_organization 
    ON document_chunks(organization_id);
CREATE INDEX IF NOT EXISTS ix_document_chunks_entity 
    ON document_chunks(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS ix_document_chunks_not_embedded 
    ON document_chunks(organization_id) WHERE NOT is_embedded;

-- Trigger for modified_at
DROP TRIGGER IF EXISTS trg_document_chunks_modified_at ON document_chunks;
CREATE TRIGGER trg_document_chunks_modified_at
    BEFORE UPDATE ON document_chunks
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_at_column();

COMMENT ON TABLE vector_embeddings IS 'AI embeddings for semantic search using pgvector';
COMMENT ON COLUMN vector_embeddings.embedding IS '1536-dimensional vector for OpenAI text-embedding-3-small';
COMMENT ON COLUMN vector_embeddings.content_hash IS 'SHA-256 hash for detecting content changes';
COMMENT ON TABLE document_chunks IS 'Source text chunks before embedding (optional tracking)';

\echo 'Vector embeddings schema created successfully.'
\echo 'Note: HNSW index uses cosine distance. For large datasets, consider adjusting m and ef_construction parameters.'
