-- ============================================================================
-- ALTER TABLE: vector_embeddings - Add Missing Columns
-- Date: 2026-01-15
-- Purpose: Add columns that exist in the C# VectorEmbedding model but were 
--          missing from the original schema design
-- ============================================================================

-- Add chunk_index for supporting chunked documents (split long content)
ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS chunk_index INTEGER NOT NULL DEFAULT 0;

COMMENT ON COLUMN vector_embeddings.chunk_index IS 'Chunk index for long content that was split. Most entities have a single chunk (index 0).';

-- Add content for storing the original text that was embedded
-- Note: Schema has content_preview (500 chars), but model needs full content
ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS content TEXT;

COMMENT ON COLUMN vector_embeddings.content IS 'The original text content that was embedded. Stored for search result display and debugging.';

-- Add embedding_dimensions for tracking vector size
ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS embedding_dimensions INTEGER NOT NULL DEFAULT 1536;

COMMENT ON COLUMN vector_embeddings.embedding_dimensions IS 'Number of dimensions in the embedding vector. OpenAI ada-002: 1536, text-embedding-3-large: 3072.';

-- Add soft delete columns (model inherits AuditableEntity)
ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;

ALTER TABLE vector_embeddings 
ADD COLUMN IF NOT EXISTS deleted_by UUID;

COMMENT ON COLUMN vector_embeddings.is_deleted IS 'Soft delete flag.';
COMMENT ON COLUMN vector_embeddings.deleted_at IS 'When the embedding was soft deleted.';
COMMENT ON COLUMN vector_embeddings.deleted_by IS 'User who deleted the embedding.';

-- ============================================================================
-- Verification Query
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns 
-- WHERE table_schema = 'public' AND table_name = 'vector_embeddings'
-- ORDER BY ordinal_position;

-- ============================================================================
-- Updated Column Count: 12 original + 6 new = 18 columns
-- ============================================================================
