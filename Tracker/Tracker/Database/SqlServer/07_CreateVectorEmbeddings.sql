/*
 * TRACKER DATABASE - VECTOR EMBEDDINGS TABLE
 * SQL Server Edition
 * 
 * Creates unified vector storage for AI/semantic search.
 * Uses VARBINARY for vector storage (SQL Server doesn't have native vector type).
 * 
 * Vector Operations:
 * - Vectors are stored as serialized float arrays in VARBINARY(MAX)
 * - Cosine similarity calculations are done in application code
 * - For high-performance scenarios, consider Azure Cognitive Search
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

USE [TrackerDB];
GO

PRINT '=============================================='
PRINT 'Creating Vector Embeddings Schema...'
PRINT '=============================================='
GO

-- =============================================================================
-- SECTION 1: VECTOR EMBEDDINGS TABLE
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VectorEmbeddings')
BEGIN
    PRINT 'Creating VectorEmbeddings table...'
    
    CREATE TABLE [dbo].[VectorEmbeddings] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        
        -- Organization scope (required for multi-tenant isolation)
        [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
        
        -- Entity reference (what this embedding represents)
        [EntityType] NVARCHAR(100) NOT NULL,  -- 'TeamMember', 'OneOnOne', 'Task', etc.
        [EntityId] NVARCHAR(100) NOT NULL,    -- The ID of the source entity (string for flexibility)
        
        -- Chunk info (for large documents split into chunks)
        [ChunkIndex] INT NOT NULL DEFAULT 0,   -- 0 = whole document, 1+ = chunk number
        [ChunkCount] INT NOT NULL DEFAULT 1,   -- Total chunks for this entity
        
        -- Content
        [Content] NVARCHAR(MAX) NOT NULL,      -- The text that was embedded
        [ContentHash] NVARCHAR(64) NULL,       -- SHA-256 hash for deduplication
        
        -- The embedding vector (serialized float array)
        -- 1536 dimensions * 4 bytes = 6144 bytes for OpenAI embeddings
        [Embedding] VARBINARY(MAX) NOT NULL,
        [EmbeddingDimensions] INT NOT NULL DEFAULT 1536,
        
        -- Metadata
        [EmbeddingModel] NVARCHAR(100) NOT NULL DEFAULT 'text-embedding-3-small',
        [EmbeddingVersion] INT NOT NULL DEFAULT 1,
        
        -- Token tracking (for cost monitoring)
        [TokenCount] INT NULL,
        
        -- Additional metadata (JSON)
        [Metadata] NVARCHAR(MAX) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_VectorEmbeddings] PRIMARY KEY NONCLUSTERED ([Id]),
        CONSTRAINT [FK_VectorEmbeddings_Organizations] FOREIGN KEY ([OrganizationId]) 
            REFERENCES [dbo].[Organizations]([Id]),
        CONSTRAINT [UQ_VectorEmbeddings_Entity_Chunk] 
            UNIQUE ([OrganizationId], [EntityType], [EntityId], [ChunkIndex])
    );
    
    -- Clustered index on OrganizationId for partition-like behavior
    CREATE CLUSTERED INDEX [IX_VectorEmbeddings_OrganizationId_Clustered]
        ON [dbo].[VectorEmbeddings]([OrganizationId], [EntityType], [CreatedAt] DESC);
    
    -- Index for entity lookups
    CREATE NONCLUSTERED INDEX [IX_VectorEmbeddings_Entity]
        ON [dbo].[VectorEmbeddings]([EntityType], [EntityId]);
    
    -- Index for content hash (deduplication)
    CREATE NONCLUSTERED INDEX [IX_VectorEmbeddings_ContentHash]
        ON [dbo].[VectorEmbeddings]([ContentHash]) WHERE [ContentHash] IS NOT NULL;
    
    -- Index for finding embeddings by type within org
    CREATE NONCLUSTERED INDEX [IX_VectorEmbeddings_OrgEntityType]
        ON [dbo].[VectorEmbeddings]([OrganizationId], [EntityType])
        INCLUDE ([EntityId], [ChunkIndex]);
    
    PRINT 'VectorEmbeddings table created.'
END
GO

-- =============================================================================
-- SECTION 2: DOCUMENT CHUNKS TABLE (Optional tracking)
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentChunks')
BEGIN
    PRINT 'Creating DocumentChunks table...'
    
    CREATE TABLE [dbo].[DocumentChunks] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        
        -- Organization scope
        [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
        
        -- Source entity
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] NVARCHAR(100) NOT NULL,
        
        -- Chunk info
        [ChunkIndex] INT NOT NULL,
        [ChunkCount] INT NOT NULL,
        
        -- Content
        [Content] NVARCHAR(MAX) NOT NULL,
        [ContentHash] NVARCHAR(64) NULL,
        
        -- Metadata
        [StartOffset] INT NULL,  -- Character offset in original document
        [EndOffset] INT NULL,
        
        -- Status
        [IsEmbedded] BIT NOT NULL DEFAULT 0,
        [LastEmbeddedAt] DATETIME2 NULL,
        
        -- Audit
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_DocumentChunks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_DocumentChunks_Organizations] FOREIGN KEY ([OrganizationId]) 
            REFERENCES [dbo].[Organizations]([Id]),
        CONSTRAINT [UQ_DocumentChunks_Entity_Chunk] 
            UNIQUE ([OrganizationId], [EntityType], [EntityId], [ChunkIndex])
    );
    
    -- Index for finding non-embedded chunks
    CREATE NONCLUSTERED INDEX [IX_DocumentChunks_NotEmbedded]
        ON [dbo].[DocumentChunks]([OrganizationId], [IsEmbedded])
        WHERE [IsEmbedded] = 0;
    
    CREATE NONCLUSTERED INDEX [IX_DocumentChunks_Entity]
        ON [dbo].[DocumentChunks]([EntityType], [EntityId]);
    
    PRINT 'DocumentChunks table created.'
END
GO

-- =============================================================================
-- SECTION 3: HELPER FUNCTIONS
-- =============================================================================

-- Function to convert float array to VARBINARY
IF OBJECT_ID('dbo.fn_FloatArrayToVarbinary', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_FloatArrayToVarbinary;
GO

-- Note: This is a placeholder. In practice, serialization happens in app code.
-- SQL Server CLR or app-side serialization is recommended.
PRINT 'Note: Vector serialization/deserialization should be done in application code.'
PRINT 'SQL Server does not have native vector operations.'
GO

-- =============================================================================
-- SECTION 4: STORED PROCEDURES FOR VECTOR OPERATIONS
-- =============================================================================

-- Procedure to store an embedding
IF OBJECT_ID('dbo.sp_StoreEmbedding', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StoreEmbedding;
GO

CREATE PROCEDURE dbo.sp_StoreEmbedding
    @OrganizationId UNIQUEIDENTIFIER,
    @EntityType NVARCHAR(100),
    @EntityId NVARCHAR(100),
    @ChunkIndex INT = 0,
    @ChunkCount INT = 1,
    @Content NVARCHAR(MAX),
    @Embedding VARBINARY(MAX),
    @EmbeddingDimensions INT = 1536,
    @EmbeddingModel NVARCHAR(100) = 'text-embedding-3-small',
    @TokenCount INT = NULL,
    @Metadata NVARCHAR(MAX) = NULL,
    @Id UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ContentHash NVARCHAR(64) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', @Content), 2);
    
    -- Check if embedding already exists
    SELECT @Id = Id 
    FROM dbo.VectorEmbeddings 
    WHERE OrganizationId = @OrganizationId 
      AND EntityType = @EntityType 
      AND EntityId = @EntityId 
      AND ChunkIndex = @ChunkIndex;
    
    IF @Id IS NOT NULL
    BEGIN
        -- Update existing
        UPDATE dbo.VectorEmbeddings
        SET Content = @Content,
            ContentHash = @ContentHash,
            Embedding = @Embedding,
            EmbeddingDimensions = @EmbeddingDimensions,
            EmbeddingModel = @EmbeddingModel,
            TokenCount = @TokenCount,
            Metadata = @Metadata,
            ChunkCount = @ChunkCount,
            LastModifiedAt = GETUTCDATE()
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        -- Insert new
        SET @Id = NEWID();
        
        INSERT INTO dbo.VectorEmbeddings (
            Id, OrganizationId, EntityType, EntityId, ChunkIndex, ChunkCount,
            Content, ContentHash, Embedding, EmbeddingDimensions, 
            EmbeddingModel, TokenCount, Metadata
        )
        VALUES (
            @Id, @OrganizationId, @EntityType, @EntityId, @ChunkIndex, @ChunkCount,
            @Content, @ContentHash, @Embedding, @EmbeddingDimensions,
            @EmbeddingModel, @TokenCount, @Metadata
        );
    END
END
GO

-- Procedure to delete embeddings for an entity
IF OBJECT_ID('dbo.sp_DeleteEntityEmbeddings', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteEntityEmbeddings;
GO

CREATE PROCEDURE dbo.sp_DeleteEntityEmbeddings
    @OrganizationId UNIQUEIDENTIFIER,
    @EntityType NVARCHAR(100),
    @EntityId NVARCHAR(100),
    @DeletedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.VectorEmbeddings
    WHERE OrganizationId = @OrganizationId
      AND EntityType = @EntityType
      AND EntityId = @EntityId;
    
    SET @DeletedCount = @@ROWCOUNT;
END
GO

-- Procedure to get embedding statistics
IF OBJECT_ID('dbo.sp_GetEmbeddingStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetEmbeddingStats;
GO

CREATE PROCEDURE dbo.sp_GetEmbeddingStats
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        EntityType,
        COUNT(*) AS EmbeddingCount,
        SUM(ISNULL(TokenCount, 0)) AS TotalTokens,
        AVG(LEN(Content)) AS AvgContentLength,
        SUM(DATALENGTH(Embedding)) / 1048576.0 AS EmbeddingStorageMB
    FROM dbo.VectorEmbeddings
    WHERE OrganizationId = @OrganizationId
    GROUP BY EntityType
    ORDER BY EmbeddingCount DESC;
END
GO

-- =============================================================================
-- SECTION 5: TRIGGERS
-- =============================================================================

-- Trigger for VectorEmbeddings modified_at
IF OBJECT_ID('dbo.TR_VectorEmbeddings_UpdateModifiedAt', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_VectorEmbeddings_UpdateModifiedAt;
GO

CREATE TRIGGER dbo.TR_VectorEmbeddings_UpdateModifiedAt
ON dbo.VectorEmbeddings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE v
    SET LastModifiedAt = GETUTCDATE()
    FROM dbo.VectorEmbeddings v
    INNER JOIN inserted i ON v.Id = i.Id;
END
GO

-- Trigger for DocumentChunks modified_at
IF OBJECT_ID('dbo.TR_DocumentChunks_UpdateModifiedAt', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_DocumentChunks_UpdateModifiedAt;
GO

CREATE TRIGGER dbo.TR_DocumentChunks_UpdateModifiedAt
ON dbo.DocumentChunks
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE d
    SET LastModifiedAt = GETUTCDATE()
    FROM dbo.DocumentChunks d
    INNER JOIN inserted i ON d.Id = i.Id;
END
GO

PRINT ''
PRINT '=============================================='
PRINT 'Vector Embeddings Schema Complete!'
PRINT '=============================================='
PRINT ''
PRINT 'Notes:'
PRINT '- Vectors are stored as VARBINARY (serialized float arrays)'
PRINT '- Similarity calculations must be done in application code'
PRINT '- For production at scale, consider Azure Cognitive Search'
PRINT '- Use sp_StoreEmbedding and sp_DeleteEntityEmbeddings for CRUD'
GO
