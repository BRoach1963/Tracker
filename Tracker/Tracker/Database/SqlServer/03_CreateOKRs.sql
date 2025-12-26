/*
 * Tracker Database Creation Script - Part 3
 * Projects, OKRs, KPIs, and Performance Management
 */

USE [TrackerDB];
GO

-- =============================================================================
-- SECTION 5: PROJECT MANAGEMENT
-- =============================================================================

-- Projects Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE [dbo].[Projects] (
        [ID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [Status] NVARCHAR(50) NULL,
        [StartDate] DATETIME2 NULL,
        [EndDate] DATETIME2 NULL,
        [Budget] DECIMAL(18,2) NULL,
        [ActualCost] DECIMAL(18,2) NULL,
        [PercentComplete] DECIMAL(5,2) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Projects] PRIMARY KEY CLUSTERED ([ID]),
        CONSTRAINT [FK_Projects_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Projects_TeamMembers] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TeamMembers]([Id])
    );
    
    -- Indexes for Projects
    CREATE NONCLUSTERED INDEX [IX_Projects_UserId_Status] ON [dbo].[Projects]([UserId], [Status]) INCLUDE ([Name], [EndDate], [PercentComplete]);
    CREATE NONCLUSTERED INDEX [IX_Projects_Name] ON [dbo].[Projects]([Name]) INCLUDE ([Status], [OwnerId]);
    CREATE NONCLUSTERED INDEX [IX_Projects_EndDate] ON [dbo].[Projects]([EndDate]) WHERE [EndDate] IS NOT NULL AND [Status] <> 'Completed';
    CREATE NONCLUSTERED INDEX [IX_Projects_IsDeleted] ON [dbo].[Projects]([IsDeleted]);
END
GO

-- ProjectTeamMembers junction table (many-to-many)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectTeamMembers')
BEGIN
    CREATE TABLE [dbo].[ProjectTeamMembers] (
        [ProjectsID] INT NOT NULL,
        [TeamMembersId] INT NOT NULL,
        
        CONSTRAINT [PK_ProjectTeamMembers] PRIMARY KEY ([ProjectsID], [TeamMembersId]),
        CONSTRAINT [FK_ProjectTeamMembers_Projects] FOREIGN KEY ([ProjectsID]) REFERENCES [dbo].[Projects]([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectTeamMembers_TeamMembers] FOREIGN KEY ([TeamMembersId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_ProjectTeamMembers_TeamMembersId] ON [dbo].[ProjectTeamMembers]([TeamMembersId]);
END
GO

-- Milestones Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Milestones')
BEGIN
    CREATE TABLE [dbo].[Milestones] (
        [ID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ProjectId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [TargetDate] DATETIME2 NOT NULL,
        [ActualDate] DATETIME2 NULL,
        [IsCompleted] BIT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Milestones] PRIMARY KEY CLUSTERED ([ID]),
        CONSTRAINT [FK_Milestones_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Milestones_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_Milestones_ProjectId_TargetDate] ON [dbo].[Milestones]([ProjectId], [TargetDate]) INCLUDE ([IsCompleted]);
    CREATE NONCLUSTERED INDEX [IX_Milestones_TargetDate_IsCompleted] ON [dbo].[Milestones]([TargetDate], [IsCompleted]) WHERE [IsCompleted] = 0;
    CREATE NONCLUSTERED INDEX [IX_Milestones_UserId] ON [dbo].[Milestones]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_Milestones_IsDeleted] ON [dbo].[Milestones]([IsDeleted]);
END
GO

-- Risks Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Risks')
BEGIN
    CREATE TABLE [dbo].[Risks] (
        [ID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ProjectId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [Severity] INT NOT NULL DEFAULT 0, -- 0=Low, 1=Medium, 2=High, 3=Critical
        [Probability] INT NOT NULL DEFAULT 0,
        [MitigationStrategy] NVARCHAR(4000) NULL,
        [Status] INT NOT NULL DEFAULT 0, -- 0=Identified, 1=Monitoring, 2=Mitigated, 3=Realized
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Risks] PRIMARY KEY CLUSTERED ([ID]),
        CONSTRAINT [FK_Risks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Risks_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_Risks_ProjectId_Severity] ON [dbo].[Risks]([ProjectId], [Severity] DESC) INCLUDE ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Risks_UserId] ON [dbo].[Risks]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_Risks_IsDeleted] ON [dbo].[Risks]([IsDeleted]);
END
GO

-- ProjectDependencies Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectDependencies')
BEGIN
    CREATE TABLE [dbo].[ProjectDependencies] (
        [ID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ProjectId] INT NOT NULL, -- The project that has dependencies
        [DependentProjectID] INT NOT NULL, -- The project that depends on something
        [RequiredProjectID] INT NOT NULL, -- The project that is required
        [Name] NVARCHAR(200) NULL,
        [Description] NVARCHAR(2000) NULL,
        [Type] INT NOT NULL DEFAULT 0, -- 0=FinishToStart, 1=StartToStart, etc.
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_ProjectDependencies] PRIMARY KEY CLUSTERED ([ID]),
        CONSTRAINT [FK_ProjectDependencies_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_ProjectDependencies_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]),
        CONSTRAINT [FK_ProjectDependencies_Dependent] FOREIGN KEY ([DependentProjectID]) REFERENCES [dbo].[Projects]([ID]),
        CONSTRAINT [FK_ProjectDependencies_Required] FOREIGN KEY ([RequiredProjectID]) REFERENCES [dbo].[Projects]([ID])
    );
    
    CREATE NONCLUSTERED INDEX [IX_ProjectDependencies_ProjectId] ON [dbo].[ProjectDependencies]([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectDependencies_DependentProjectID] ON [dbo].[ProjectDependencies]([DependentProjectID]);
    CREATE NONCLUSTERED INDEX [IX_ProjectDependencies_UserId] ON [dbo].[ProjectDependencies]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectDependencies_IsDeleted] ON [dbo].[ProjectDependencies]([IsDeleted]);
END
GO

-- =============================================================================
-- SECTION 6: OKRs AND KEY RESULTS
-- =============================================================================

-- ObjectiveKeyResults Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ObjectiveKeyResults')
BEGIN
    CREATE TABLE [dbo].[ObjectiveKeyResults] (
        [ObjectiveId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [ProjectId] INT NULL, -- Optional legacy link to project
        [Title] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NOT NULL,
        [TimePeriod] INT NOT NULL DEFAULT 0, -- 0=Annual, 1=Quarterly, 2=Monthly
        [Quarter] INT NULL,
        [Year] INT NOT NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_ObjectiveKeyResults] PRIMARY KEY CLUSTERED ([ObjectiveId]),
        CONSTRAINT [FK_ObjectiveKeyResults_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_ObjectiveKeyResults_TeamMembers] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TeamMembers]([Id]),
        CONSTRAINT [FK_ObjectiveKeyResults_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]) ON DELETE SET NULL
    );
    
    -- Indexes for OKRs
    CREATE NONCLUSTERED INDEX [IX_OKRs_UserId_Year_TimePeriod] ON [dbo].[ObjectiveKeyResults]([UserId], [Year], [TimePeriod]) INCLUDE ([OwnerId], [EndDate]);
    CREATE NONCLUSTERED INDEX [IX_OKRs_EndDate] ON [dbo].[ObjectiveKeyResults]([EndDate]) INCLUDE ([UserId], [OwnerId], [Title]);
    CREATE NONCLUSTERED INDEX [IX_OKRs_OwnerId_EndDate] ON [dbo].[ObjectiveKeyResults]([OwnerId], [EndDate]);
    CREATE NONCLUSTERED INDEX [IX_OKRs_ProjectId] ON [dbo].[ObjectiveKeyResults]([ProjectId]) WHERE [ProjectId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_OKRs_IsDeleted] ON [dbo].[ObjectiveKeyResults]([IsDeleted]);
END
GO

-- KeyResults Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KeyResults')
BEGIN
    CREATE TABLE [dbo].[KeyResults] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OkrId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [StartingValue] DECIMAL(18,4) NOT NULL DEFAULT 0,
        [TargetValue] DECIMAL(18,4) NOT NULL,
        [CurrentValue] DECIMAL(18,4) NOT NULL DEFAULT 0,
        [Unit] NVARCHAR(50) NULL,
        [Weight] DECIMAL(5,2) NOT NULL DEFAULT 1.0,
        [SortOrder] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_KeyResults] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_KeyResults_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_KeyResults_OKRs] FOREIGN KEY ([OkrId]) REFERENCES [dbo].[ObjectiveKeyResults]([ObjectiveId]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_KeyResults_OkrId_SortOrder] ON [dbo].[KeyResults]([OkrId], [SortOrder]) INCLUDE ([Title], [CurrentValue], [TargetValue]);
    CREATE NONCLUSTERED INDEX [IX_KeyResults_UserId] ON [dbo].[KeyResults]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_KeyResults_IsDeleted] ON [dbo].[KeyResults]([IsDeleted]);
END
GO

-- KeyResultMeasurables Table
-- Links between Key Results and their measurement sources (KPI, Project, TaskCollection)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KeyResultMeasurables')
BEGIN
    CREATE TABLE [dbo].[KeyResultMeasurables] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [KeyResultId] INT NOT NULL,
        [MeasurableType] INT NOT NULL, -- 0=KPI, 1=Project, 2=TaskCollection
        [MeasurableId] INT NOT NULL, -- Polymorphic FK (not enforced at DB level)
        [Weight] DECIMAL(5,2) NOT NULL DEFAULT 1.0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_KeyResultMeasurables] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_KeyResultMeasurables_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_KeyResultMeasurables_KeyResults] FOREIGN KEY ([KeyResultId]) REFERENCES [dbo].[KeyResults]([Id]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_KeyResultMeasurables_KeyResultId] ON [dbo].[KeyResultMeasurables]([KeyResultId]);
    CREATE NONCLUSTERED INDEX [IX_KeyResultMeasurables_Type_Id] ON [dbo].[KeyResultMeasurables]([MeasurableType], [MeasurableId]);
    CREATE NONCLUSTERED INDEX [IX_KeyResultMeasurables_UserId] ON [dbo].[KeyResultMeasurables]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_KeyResultMeasurables_IsDeleted] ON [dbo].[KeyResultMeasurables]([IsDeleted]);
END
GO

-- OneOnOneLinkedOkrs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OneOnOneLinkedOkrs')
BEGIN
    CREATE TABLE [dbo].[OneOnOneLinkedOkrs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OneOnOneId] INT NOT NULL,
        [OkrId] INT NOT NULL,
        [DiscussionNotes] NVARCHAR(2000) NULL,
        
        CONSTRAINT [PK_OneOnOneLinkedOkrs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OneOnOneLinkedOkrs_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OneOnOneLinkedOkrs_OKRs] FOREIGN KEY ([OkrId]) REFERENCES [dbo].[ObjectiveKeyResults]([ObjectiveId]),
        CONSTRAINT [UQ_OneOnOneLinkedOkrs_Meeting_Okr] UNIQUE ([OneOnOneId], [OkrId])
    );
    
    CREATE NONCLUSTERED INDEX [IX_OneOnOneLinkedOkrs_OkrId] ON [dbo].[OneOnOneLinkedOkrs]([OkrId]);
END
GO

-- Continued in next file...
