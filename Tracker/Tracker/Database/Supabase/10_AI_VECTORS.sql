-- ============================================================================
-- TRACKER DATABASE - AI / VECTOR EMBEDDINGS
-- ============================================================================

-- ============================================================================
-- VECTOR_EMBEDDINGS
-- Store embeddings for semantic search across all content
-- ============================================================================
CREATE TABLE vector_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Source entity
    entity_type VARCHAR(50) NOT NULL,  -- note, meeting_note, feedback, task, goal, etc.
    entity_id UUID NOT NULL,
    
    -- Content that was embedded
    content_hash VARCHAR(64) NOT NULL,  -- SHA256 of content, to detect changes
    content_preview VARCHAR(500),  -- First 500 chars for display
    
    -- Embedding vector (1536 dimensions for OpenAI ada-002, 3072 for text-embedding-3-large)
    embedding vector(1536),  -- Using OpenAI ada-002 dimension
    
    -- Metadata for filtering
    metadata JSONB,
    
    -- Embedding model info
    model_name VARCHAR(100) NOT NULL DEFAULT 'text-embedding-ada-002',
    model_version VARCHAR(50),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Unique constraint: one embedding per entity
    UNIQUE (entity_type, entity_id)
);

-- Indexes
CREATE INDEX idx_vector_embeddings_org ON vector_embeddings(organization_id);
CREATE INDEX idx_vector_embeddings_entity ON vector_embeddings(entity_type, entity_id);

-- HNSW index for fast similarity search
-- m: max connections per node, ef_construction: size of dynamic candidate list
CREATE INDEX idx_vector_embeddings_vector ON vector_embeddings 
    USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Trigger
CREATE TRIGGER vector_embeddings_updated_at
    BEFORE UPDATE ON vector_embeddings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- AI_CONVERSATIONS
-- Store AI chat history for context
-- ============================================================================
CREATE TABLE ai_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- User
    team_member_id UUID NOT NULL REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Conversation details
    title VARCHAR(200),
    
    -- Context entity (what was the user looking at when they started?)
    context_entity_type VARCHAR(50),
    context_entity_id UUID,
    
    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_ai_conversations_org ON ai_conversations(organization_id);
CREATE INDEX idx_ai_conversations_member ON ai_conversations(team_member_id);
CREATE INDEX idx_ai_conversations_recent ON ai_conversations(team_member_id, updated_at DESC) 
    WHERE is_deleted = false;

-- Trigger
CREATE TRIGGER ai_conversations_updated_at
    BEFORE UPDATE ON ai_conversations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- AI_MESSAGES
-- Individual messages in AI conversations
-- ============================================================================
CREATE TABLE ai_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES ai_conversations(id) ON DELETE CASCADE,
    
    -- Message content
    role VARCHAR(20) NOT NULL,  -- user, assistant, system
    content TEXT NOT NULL,
    
    -- Token usage
    prompt_tokens INTEGER,
    completion_tokens INTEGER,
    total_tokens INTEGER,
    
    -- Model info
    model_name VARCHAR(100),
    
    -- References used to generate response
    referenced_entities JSONB,  -- [{type: 'note', id: '...'}, ...]
    
    -- Ordering
    message_order INTEGER NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_ai_messages_conversation ON ai_messages(conversation_id);
CREATE INDEX idx_ai_messages_order ON ai_messages(conversation_id, message_order);

-- ============================================================================
-- AI_INSIGHTS
-- Proactive AI-generated insights
-- ============================================================================
CREATE TABLE ai_insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Target (can be org-wide, team-specific, or person-specific)
    target_team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    target_team_member_id UUID REFERENCES team_members(id) ON DELETE CASCADE,
    
    -- Insight details
    insight_type VARCHAR(100) NOT NULL,  -- risk_alert, opportunity, trend, recommendation
    category VARCHAR(100) NOT NULL,  -- engagement, performance, retention, goal_progress
    
    title VARCHAR(200) NOT NULL,
    summary TEXT NOT NULL,
    details JSONB,  -- Structured data supporting the insight
    
    -- Priority/severity
    priority VARCHAR(20) NOT NULL DEFAULT 'medium',  -- low, medium, high, critical
    
    -- Action recommendations
    recommended_actions JSONB,
    
    -- Source entities that led to this insight
    source_entities JSONB,  -- [{type: 'metric', id: '...'}, ...]
    
    -- Validity period
    valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    valid_until TIMESTAMPTZ,
    
    -- Status
    is_dismissed BOOLEAN NOT NULL DEFAULT false,
    dismissed_at TIMESTAMPTZ,
    dismissed_by UUID REFERENCES users(id),
    dismiss_reason TEXT,
    
    -- Action taken
    is_actioned BOOLEAN NOT NULL DEFAULT false,
    actioned_at TIMESTAMPTZ,
    action_notes TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_ai_insights_org ON ai_insights(organization_id);
CREATE INDEX idx_ai_insights_team ON ai_insights(target_team_id);
CREATE INDEX idx_ai_insights_member ON ai_insights(target_team_member_id);
CREATE INDEX idx_ai_insights_active ON ai_insights(organization_id, valid_from, valid_until) 
    WHERE is_dismissed = false;
CREATE INDEX idx_ai_insights_type ON ai_insights(organization_id, insight_type, category);

-- ============================================================================
-- SEMANTIC SEARCH FUNCTION
-- Helper function to search embeddings by similarity
-- ============================================================================
CREATE OR REPLACE FUNCTION search_embeddings(
    p_organization_id UUID,
    p_query_embedding vector(1536),
    p_entity_types TEXT[] DEFAULT NULL,
    p_limit INTEGER DEFAULT 10,
    p_threshold FLOAT DEFAULT 0.7
)
RETURNS TABLE (
    entity_type VARCHAR(50),
    entity_id UUID,
    content_preview VARCHAR(500),
    similarity FLOAT,
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ve.entity_type,
        ve.entity_id,
        ve.content_preview,
        (1 - (ve.embedding <=> p_query_embedding))::FLOAT AS similarity,
        ve.metadata
    FROM vector_embeddings ve
    WHERE ve.organization_id = p_organization_id
      AND (p_entity_types IS NULL OR ve.entity_type = ANY(p_entity_types))
      AND (1 - (ve.embedding <=> p_query_embedding)) >= p_threshold
    ORDER BY ve.embedding <=> p_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql STABLE;

SELECT 'AI/Vector tables created successfully' AS status;
