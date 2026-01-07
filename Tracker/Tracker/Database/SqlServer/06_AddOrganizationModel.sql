/*
 * TRACKER DATABASE - ORGANIZATION MODEL UPGRADE
 * SQL Server Edition
 * 
 * This script adds multi-tenant organization support to existing Tracker databases.
 * Run after the base schema (01-05) has been deployed.
 * 
 * Changes:
 * - Creates Organizations table
 * - Creates ManagerHistory table
 * - Adds OrganizationId to all existing tables
 * - Creates default organization for existing data
 * 
 * This is a NON-DESTRUCTIVE upgrade - existing data is preserved.
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

USE [TrackerDB];
GO

PRINT '=============================================='
PRINT 'Starting Organization Model Upgrade...'
PRINT '=============================================='
GO

-- =============================================================================
-- SECTION 1: ORGANIZATIONS TABLE
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Organizations')
BEGIN
    PRINT 'Creating Organizations table...'
    
    CREATE TABLE [dbo].[Organizations] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        
        -- Core fields
        [Name] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(200) NOT NULL,  -- URL-friendly identifier
        
        -- Status
        [IsActive] BIT NOT NULL DEFAULT 1,
        
        -- Subscription/billing
        [SubscriptionTier] NVARCHAR(50) NOT NULL DEFAULT 'free',  -- free, professional, enterprise
        [MaxUsers] INT NOT NULL DEFAULT 1,
        [MaxTeamMembers] INT NOT NULL DEFAULT 10,
        
        -- Supabase integration
        [SupabaseOrgId] NVARCHAR(100) NULL,
        
        -- Settings (JSON for flexibility)
        [Settings] NVARCHAR(MAX) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Organizations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Organizations_Slug] UNIQUE NONCLUSTERED ([Slug]),
        CONSTRAINT [CK_Organizations_SubscriptionTier] 
            CHECK ([SubscriptionTier] IN ('free', 'professional', 'enterprise'))
    );
    
    -- Indexes
    CREATE NONCLUSTERED INDEX [IX_Organizations_Slug] 
        ON [dbo].[Organizations]([Slug]) WHERE [IsDeleted] = 0;
    CREATE NONCLUSTERED INDEX [IX_Organizations_IsActive] 
        ON [dbo].[Organizations]([IsActive]) WHERE [IsDeleted] = 0;
    CREATE NONCLUSTERED INDEX [IX_Organizations_IsDeleted] 
        ON [dbo].[Organizations]([IsDeleted]);
    
    PRINT 'Organizations table created.'
END
ELSE
BEGIN
    PRINT 'Organizations table already exists.'
END
GO

-- =============================================================================
-- SECTION 2: UPDATE USERS TABLE
-- =============================================================================

PRINT 'Updating Users table...'

-- Add OrganizationId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [OrganizationId] UNIQUEIDENTIFIER NULL;
    PRINT '  Added OrganizationId column to Users.'
END

-- Add SupabaseUserId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'SupabaseUserId')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [SupabaseUserId] UNIQUEIDENTIFIER NULL;
    PRINT '  Added SupabaseUserId column to Users.'
END

-- Add Role column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Role')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Role] NVARCHAR(50) NOT NULL DEFAULT 'member';
    PRINT '  Added Role column to Users.'
END

-- Add Preferences column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Preferences')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Preferences] NVARCHAR(MAX) NULL;
    PRINT '  Added Preferences column to Users.'
END
GO

-- Add indexes for new columns (after all columns exist)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_OrganizationId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_OrganizationId] 
        ON [dbo].[Users]([OrganizationId]) WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_SupabaseUserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_SupabaseUserId] 
        ON [dbo].[Users]([SupabaseUserId]) WHERE [SupabaseUserId] IS NOT NULL;
END
GO

-- =============================================================================
-- SECTION 3: UPDATE TEAM MEMBERS TABLE
-- =============================================================================

PRINT 'Updating TeamMembers table...'

-- Add OrganizationId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TeamMembers') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE [dbo].[TeamMembers] ADD [OrganizationId] UNIQUEIDENTIFIER NULL;
    PRINT '  Added OrganizationId column to TeamMembers.'
END

-- Add CurrentManagerUserId column (tracks current manager for quick lookups)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TeamMembers') AND name = 'CurrentManagerUserId')
BEGIN
    ALTER TABLE [dbo].[TeamMembers] ADD [CurrentManagerUserId] UNIQUEIDENTIFIER NULL;
    PRINT '  Added CurrentManagerUserId column to TeamMembers.'
END

-- Add Notes column if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TeamMembers') AND name = 'Notes')
BEGIN
    ALTER TABLE [dbo].[TeamMembers] ADD [Notes] NVARCHAR(MAX) NULL;
    PRINT '  Added Notes column to TeamMembers.'
END
GO

-- Add index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TeamMembers_OrganizationId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_OrganizationId] 
        ON [dbo].[TeamMembers]([OrganizationId]) WHERE [IsDeleted] = 0;
END
GO

-- =============================================================================
-- SECTION 4: MANAGER HISTORY TABLE
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ManagerHistory')
BEGIN
    PRINT 'Creating ManagerHistory table...'
    
    CREATE TABLE [dbo].[ManagerHistory] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        
        -- Organization scope
        [OrganizationId] UNIQUEIDENTIFIER NOT NULL,
        
        -- The team member whose manager changed
        [TeamMemberId] INT NOT NULL,
        
        -- The manager during this period (references Users table)
        [ManagerUserId] UNIQUEIDENTIFIER NOT NULL,
        
        -- Period of management
        [StartDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [EndDate] DATETIME2 NULL,  -- NULL means current manager
        
        -- Reason for change
        [ChangeReason] NVARCHAR(500) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NULL,
        
        CONSTRAINT [PK_ManagerHistory] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ManagerHistory_Organizations] FOREIGN KEY ([OrganizationId]) 
            REFERENCES [dbo].[Organizations]([Id]),
        CONSTRAINT [FK_ManagerHistory_TeamMembers] FOREIGN KEY ([TeamMemberId]) 
            REFERENCES [dbo].[TeamMembers]([Id])
    );
    
    -- Indexes
    CREATE NONCLUSTERED INDEX [IX_ManagerHistory_OrganizationId] 
        ON [dbo].[ManagerHistory]([OrganizationId]);
    CREATE NONCLUSTERED INDEX [IX_ManagerHistory_TeamMemberId] 
        ON [dbo].[ManagerHistory]([TeamMemberId], [StartDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_ManagerHistory_ManagerUserId] 
        ON [dbo].[ManagerHistory]([ManagerUserId], [StartDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_ManagerHistory_Current] 
        ON [dbo].[ManagerHistory]([TeamMemberId]) WHERE [EndDate] IS NULL;
    
    PRINT 'ManagerHistory table created.'
END
GO

-- =============================================================================
-- SECTION 5: ADD ORGANIZATION ID TO REMAINING TABLES
-- =============================================================================

-- Helper procedure to add OrganizationId column to a table
IF OBJECT_ID('dbo.sp_AddOrganizationIdColumn', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AddOrganizationIdColumn;
GO

CREATE PROCEDURE dbo.sp_AddOrganizationIdColumn
    @TableName NVARCHAR(128)
AS
BEGIN
    DECLARE @SQL NVARCHAR(MAX);
    
    IF NOT EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.' + @TableName) 
        AND name = 'OrganizationId'
    )
    BEGIN
        SET @SQL = 'ALTER TABLE [dbo].[' + @TableName + '] ADD [OrganizationId] UNIQUEIDENTIFIER NULL;';
        EXEC sp_executesql @SQL;
        PRINT '  Added OrganizationId to ' + @TableName;
    END
END
GO

PRINT 'Adding OrganizationId to meeting tables...'
EXEC dbo.sp_AddOrganizationIdColumn 'OneOnOnes';
EXEC dbo.sp_AddOrganizationIdColumn 'MeetingTemplates';
EXEC dbo.sp_AddOrganizationIdColumn 'MeetingTemplateItems';
GO

PRINT 'Adding OrganizationId to task tables...'
EXEC dbo.sp_AddOrganizationIdColumn 'Tasks';
EXEC dbo.sp_AddOrganizationIdColumn 'MeetingTasks';
EXEC dbo.sp_AddOrganizationIdColumn 'TaskCollections';
EXEC dbo.sp_AddOrganizationIdColumn 'TaskCollectionItems';
EXEC dbo.sp_AddOrganizationIdColumn 'AgendaItems';
GO

PRINT 'Adding OrganizationId to project tables...'
EXEC dbo.sp_AddOrganizationIdColumn 'Projects';
EXEC dbo.sp_AddOrganizationIdColumn 'Milestones';
EXEC dbo.sp_AddOrganizationIdColumn 'Risks';
EXEC dbo.sp_AddOrganizationIdColumn 'ProjectDependencies';
GO

PRINT 'Adding OrganizationId to OKR/KPI tables...'
EXEC dbo.sp_AddOrganizationIdColumn 'ObjectiveKeyResults';
EXEC dbo.sp_AddOrganizationIdColumn 'KeyResults';
EXEC dbo.sp_AddOrganizationIdColumn 'KeyPerformanceIndicators';
EXEC dbo.sp_AddOrganizationIdColumn 'KpiDataSources';
GO

PRINT 'Adding OrganizationId to feedback/notes tables...'
EXEC dbo.sp_AddOrganizationIdColumn 'IndividualGoals';
EXEC dbo.sp_AddOrganizationIdColumn 'GoalMilestones';
EXEC dbo.sp_AddOrganizationIdColumn 'Feedbacks';
EXEC dbo.sp_AddOrganizationIdColumn 'QuickNotes';
GO

-- Clean up helper procedure
DROP PROCEDURE dbo.sp_AddOrganizationIdColumn;
GO

-- =============================================================================
-- SECTION 6: CREATE INDEXES FOR NEW ORGANIZATION COLUMNS
-- =============================================================================

PRINT 'Creating indexes on OrganizationId columns...'

-- Create indexes dynamically for all tables with OrganizationId
DECLARE @TableName NVARCHAR(128);
DECLARE @IndexName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);

DECLARE table_cursor CURSOR FOR
SELECT t.name
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE c.name = 'OrganizationId'
  AND t.name NOT IN ('Organizations', 'Users', 'TeamMembers', 'ManagerHistory')
  AND t.name NOT LIKE 'sys%';

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @IndexName = 'IX_' + @TableName + '_OrganizationId';
    
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = @IndexName)
    BEGIN
        -- Check if table has IsDeleted column
        IF EXISTS (
            SELECT * FROM sys.columns 
            WHERE object_id = OBJECT_ID('dbo.' + @TableName) 
            AND name = 'IsDeleted'
        )
        BEGIN
            SET @SQL = 'CREATE NONCLUSTERED INDEX [' + @IndexName + '] ON [dbo].[' + @TableName + ']([OrganizationId]) WHERE [IsDeleted] = 0;';
        END
        ELSE
        BEGIN
            SET @SQL = 'CREATE NONCLUSTERED INDEX [' + @IndexName + '] ON [dbo].[' + @TableName + ']([OrganizationId]);';
        END
        
        BEGIN TRY
            EXEC sp_executesql @SQL;
            PRINT '  Created index ' + @IndexName;
        END TRY
        BEGIN CATCH
            PRINT '  Warning: Could not create index ' + @IndexName + ' - ' + ERROR_MESSAGE();
        END CATCH
    END
    
    FETCH NEXT FROM table_cursor INTO @TableName;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;
GO

-- =============================================================================
-- SECTION 7: ADD FOREIGN KEY TO ORGANIZATIONS (OPTIONAL)
-- =============================================================================
-- Note: We make these optional/soft FKs because OrganizationId is nullable
-- during migration. After backfill, you can make them required.

PRINT ''
PRINT 'Organization foreign keys will be added after data migration.'
PRINT 'Run 07_FinalizeOrganizationModel.sql after backfilling OrganizationId values.'

-- =============================================================================
-- SECTION 8: UPDATE TRIGGERS FOR MODIFIED_AT
-- =============================================================================

-- Trigger for Organizations
IF OBJECT_ID('dbo.TR_Organizations_UpdateModifiedAt', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Organizations_UpdateModifiedAt;
GO

CREATE TRIGGER dbo.TR_Organizations_UpdateModifiedAt
ON dbo.Organizations
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE o
    SET LastModifiedAt = GETUTCDATE()
    FROM dbo.Organizations o
    INNER JOIN inserted i ON o.Id = i.Id;
END
GO

-- Trigger for ManagerHistory
IF OBJECT_ID('dbo.TR_ManagerHistory_UpdateModifiedAt', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_ManagerHistory_UpdateModifiedAt;
GO

CREATE TRIGGER dbo.TR_ManagerHistory_UpdateModifiedAt
ON dbo.ManagerHistory
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE m
    SET LastModifiedAt = GETUTCDATE()
    FROM dbo.ManagerHistory m
    INNER JOIN inserted i ON m.Id = i.Id;
END
GO

PRINT ''
PRINT '=============================================='
PRINT 'Organization Model Upgrade Complete!'
PRINT '=============================================='
PRINT ''
PRINT 'Next Steps:'
PRINT '1. Run 07_CreateVectorEmbeddings.sql to add AI/vector support'
PRINT '2. Migrate existing data to set OrganizationId values'
PRINT '3. Run 08_FinalizeOrganizationModel.sql to add NOT NULL constraints'
GO
