/*
 * Tracker Database Creation Script - Part 4
 * KPIs, Goals, Feedback, and Supporting Tables
 */

USE [TrackerDB];
GO

-- =============================================================================
-- SECTION 7: KPIs AND DATA SOURCES
-- =============================================================================

-- KeyPerformanceIndicators Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KeyPerformanceIndicators')
BEGIN
    CREATE TABLE [dbo].[KeyPerformanceIndicators] (
        [KpiId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [ParentKpiId] INT NULL, -- For composite KPIs
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [Unit] NVARCHAR(50) NULL,
        [Category] NVARCHAR(100) NULL,
        [TargetValue] DECIMAL(18,4) NULL,
        [CurrentValue] DECIMAL(18,4) NULL,
        [ThresholdGreen] DECIMAL(18,4) NULL,
        [ThresholdYellow] DECIMAL(18,4) NULL,
        [IsComposite] BIT NOT NULL DEFAULT 0,
        [AggregationMethod] INT NOT NULL DEFAULT 0, -- 0=Sum, 1=Average, 2=Min, 3=Max
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_KeyPerformanceIndicators] PRIMARY KEY CLUSTERED ([KpiId]),
        CONSTRAINT [FK_KPIs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_KPIs_TeamMembers] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TeamMembers]([Id]),
        CONSTRAINT [FK_KPIs_ParentKPI] FOREIGN KEY ([ParentKpiId]) REFERENCES [dbo].[KeyPerformanceIndicators]([KpiId])
    );
    
    -- Indexes for KPIs
    CREATE NONCLUSTERED INDEX [IX_KPIs_UserId_Category] ON [dbo].[KeyPerformanceIndicators]([UserId], [Category]) INCLUDE ([Name], [CurrentValue], [TargetValue]);
    CREATE NONCLUSTERED INDEX [IX_KPIs_Name] ON [dbo].[KeyPerformanceIndicators]([Name]);
    CREATE NONCLUSTERED INDEX [IX_KPIs_OwnerId] ON [dbo].[KeyPerformanceIndicators]([OwnerId]);
    CREATE NONCLUSTERED INDEX [IX_KPIs_IsComposite] ON [dbo].[KeyPerformanceIndicators]([IsComposite]) WHERE [IsComposite] = 1;
    CREATE NONCLUSTERED INDEX [IX_KPIs_ParentKpiId] ON [dbo].[KeyPerformanceIndicators]([ParentKpiId]) WHERE [ParentKpiId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_KPIs_IsDeleted] ON [dbo].[KeyPerformanceIndicators]([IsDeleted]);
END
GO

-- KpiDataSources Table
-- Data sources that feed KPI values (polymorphic: Project, TaskCollection, or KPI)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KpiDataSources')
BEGIN
    CREATE TABLE [dbo].[KpiDataSources] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [KpiId] INT NOT NULL,
        [SourceType] INT NOT NULL, -- 0=Project, 1=TaskCollection, 2=KPI
        [SourceId] INT NOT NULL, -- Polymorphic FK (not enforced at DB level)
        [Weight] DECIMAL(5,2) NOT NULL DEFAULT 1.0,
        [QueryCriteria] NVARCHAR(2000) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_KpiDataSources] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_KpiDataSources_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_KpiDataSources_KPIs] FOREIGN KEY ([KpiId]) REFERENCES [dbo].[KeyPerformanceIndicators]([KpiId]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_KpiDataSources_KpiId] ON [dbo].[KpiDataSources]([KpiId]);
    CREATE NONCLUSTERED INDEX [IX_KpiDataSources_SourceType_SourceId] ON [dbo].[KpiDataSources]([SourceType], [SourceId]);
    CREATE NONCLUSTERED INDEX [IX_KpiDataSources_UserId] ON [dbo].[KpiDataSources]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_KpiDataSources_IsDeleted] ON [dbo].[KpiDataSources]([IsDeleted]);
END
GO

-- OneOnOneLinkedKpis Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OneOnOneLinkedKpis')
BEGIN
    CREATE TABLE [dbo].[OneOnOneLinkedKpis] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OneOnOneId] INT NOT NULL,
        [KpiId] INT NOT NULL,
        [DiscussionNotes] NVARCHAR(2000) NULL,
        
        CONSTRAINT [PK_OneOnOneLinkedKpis] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OneOnOneLinkedKpis_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OneOnOneLinkedKpis_KPIs] FOREIGN KEY ([KpiId]) REFERENCES [dbo].[KeyPerformanceIndicators]([KpiId]),
        CONSTRAINT [UQ_OneOnOneLinkedKpis_Meeting_Kpi] UNIQUE ([OneOnOneId], [KpiId])
    );
    
    CREATE NONCLUSTERED INDEX [IX_OneOnOneLinkedKpis_KpiId] ON [dbo].[OneOnOneLinkedKpis]([KpiId]);
END
GO

-- =============================================================================
-- SECTION 8: GOALS AND FEEDBACK
-- =============================================================================

-- IndividualGoals Table
-- Personal development goals for team members
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IndividualGoals')
BEGIN
    CREATE TABLE [dbo].[IndividualGoals] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [TeamMemberId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [Category] INT NOT NULL DEFAULT 0, -- 0=Career, 1=Skill, 2=Performance, etc.
        [Status] INT NOT NULL DEFAULT 0, -- 0=NotStarted, 1=InProgress, 2=Completed, etc.
        [TargetDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [Notes] NVARCHAR(2000) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_IndividualGoals] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_IndividualGoals_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_IndividualGoals_TeamMembers] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE CASCADE
    );
    
    -- Indexes for IndividualGoals
    CREATE NONCLUSTERED INDEX [IX_IndividualGoals_TeamMemberId_Status] ON [dbo].[IndividualGoals]([TeamMemberId], [Status]) INCLUDE ([TargetDate], [Category]);
    CREATE NONCLUSTERED INDEX [IX_IndividualGoals_UserId_Category_Status] ON [dbo].[IndividualGoals]([UserId], [Category], [Status]);
    CREATE NONCLUSTERED INDEX [IX_IndividualGoals_TargetDate] ON [dbo].[IndividualGoals]([TargetDate]) WHERE [Status] <> 2 AND [TargetDate] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_IndividualGoals_IsDeleted] ON [dbo].[IndividualGoals]([IsDeleted]);
END
GO

-- GoalMilestones Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoalMilestones')
BEGIN
    CREATE TABLE [dbo].[GoalMilestones] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [GoalId] INT NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [TargetDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
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
        
        CONSTRAINT [PK_GoalMilestones] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_GoalMilestones_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_GoalMilestones_Goals] FOREIGN KEY ([GoalId]) REFERENCES [dbo].[IndividualGoals]([Id]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_GoalMilestones_GoalId] ON [dbo].[GoalMilestones]([GoalId]) INCLUDE ([IsCompleted], [TargetDate]);
    CREATE NONCLUSTERED INDEX [IX_GoalMilestones_UserId] ON [dbo].[GoalMilestones]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_GoalMilestones_IsDeleted] ON [dbo].[GoalMilestones]([IsDeleted]);
END
GO

-- Feedbacks Table
-- Feedback given to team members
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Feedbacks')
BEGIN
    CREATE TABLE [dbo].[Feedbacks] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [TeamMemberId] INT NOT NULL,
        [OneOnOneId] INT NULL,
        [Title] NVARCHAR(200) NULL,
        [Content] NVARCHAR(4000) NOT NULL,
        [Type] INT NOT NULL DEFAULT 0, -- 0=Positive, 1=Constructive, 2=Developmental
        [Date] DATETIME2 NOT NULL,
        [Context] NVARCHAR(500) NULL,
        [IsPrivate] BIT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Feedbacks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Feedbacks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Feedbacks_TeamMembers] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Feedbacks_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE SET NULL
    );
    
    -- Indexes for Feedbacks
    CREATE NONCLUSTERED INDEX [IX_Feedbacks_TeamMemberId_Date] ON [dbo].[Feedbacks]([TeamMemberId], [Date] DESC) INCLUDE ([Type], [Title]);
    CREATE NONCLUSTERED INDEX [IX_Feedbacks_UserId_Type] ON [dbo].[Feedbacks]([UserId], [Type]) INCLUDE ([Date], [TeamMemberId]);
    CREATE NONCLUSTERED INDEX [IX_Feedbacks_Date] ON [dbo].[Feedbacks]([Date] DESC);
    CREATE NONCLUSTERED INDEX [IX_Feedbacks_IsDeleted] ON [dbo].[Feedbacks]([IsDeleted]);
END
GO

-- =============================================================================
-- SECTION 9: NOTES AND REMINDERS
-- =============================================================================

-- QuickNotes Table
-- Quick notes and journal entries with flexible linking
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuickNotes')
BEGIN
    CREATE TABLE [dbo].[QuickNotes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Title] NVARCHAR(200) NULL,
        [Content] NVARCHAR(4000) NOT NULL,
        [Category] INT NOT NULL DEFAULT 0, -- 0=General, 1=Meeting, 2=Project, 3=TeamMember, etc.
        [Tags] NVARCHAR(500) NULL,
        [IsPinned] BIT NOT NULL DEFAULT 0,
        [IsArchived] BIT NOT NULL DEFAULT 0,
        
        -- Legacy FK fields (still supported for backwards compatibility)
        [TeamMemberId] INT NULL,
        [ProjectId] INT NULL,
        [OneOnOneId] INT NULL,
        
        -- New polymorphic linking (preferred method)
        [LinkedEntityType] INT NOT NULL DEFAULT 0, -- 0=None, 1=TeamMember, 2=Project, 3=OneOnOne, 4=Task, 5=OKR, 6=KPI
        [LinkedEntityId] INT NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_QuickNotes] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_QuickNotes_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_QuickNotes_TeamMembers] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_QuickNotes_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]) ON DELETE SET NULL,
        CONSTRAINT [FK_QuickNotes_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE SET NULL
    );
    
    -- Indexes for QuickNotes (optimized for filtering and searching)
    CREATE NONCLUSTERED INDEX [IX_QuickNotes_UserId_Category_CreatedAt] ON [dbo].[QuickNotes]([UserId], [Category], [CreatedAt] DESC) WHERE [IsArchived] = 0;
    CREATE NONCLUSTERED INDEX [IX_QuickNotes_LinkedEntity] ON [dbo].[QuickNotes]([LinkedEntityType], [LinkedEntityId]) WHERE [LinkedEntityType] <> 0 AND [LinkedEntityId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_QuickNotes_IsPinned] ON [dbo].[QuickNotes]([IsPinned], [CreatedAt] DESC) WHERE [IsPinned] = 1 AND [IsArchived] = 0;
    CREATE NONCLUSTERED INDEX [IX_QuickNotes_CreatedAt] ON [dbo].[QuickNotes]([CreatedAt] DESC) INCLUDE ([UserId], [Title], [Category]);
    CREATE NONCLUSTERED INDEX [IX_QuickNotes_IsDeleted] ON [dbo].[QuickNotes]([IsDeleted]);
END
GO

-- Reminders Table
-- Notifications and alerts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reminders')
BEGIN
    CREATE TABLE [dbo].[Reminders] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NULL,
        [DueDateTime] DATETIME2 NOT NULL,
        [Status] INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Completed, 2=Dismissed, 3=Snoozed
        [Type] INT NOT NULL DEFAULT 0, -- 0=General, 1=Meeting, 2=Task, 3=Goal
        [SnoozeUntil] DATETIME2 NULL,
        
        -- Links to related entities
        [OneOnOneId] INT NULL,
        [TeamMemberId] INT NULL,
        [TaskId] INT NULL,
        [GoalId] INT NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Reminders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Reminders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Reminders_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reminders_TeamMembers] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reminders_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reminders_Goals] FOREIGN KEY ([GoalId]) REFERENCES [dbo].[IndividualGoals]([Id]) ON DELETE CASCADE
    );
    
    -- Indexes for Reminders
    CREATE NONCLUSTERED INDEX [IX_Reminders_UserId_DueDateTime_Status] ON [dbo].[Reminders]([UserId], [DueDateTime], [Status]) WHERE [Status] = 0;
    CREATE NONCLUSTERED INDEX [IX_Reminders_Type] ON [dbo].[Reminders]([Type]) INCLUDE ([UserId], [DueDateTime], [Status]);
    CREATE NONCLUSTERED INDEX [IX_Reminders_IsDeleted] ON [dbo].[Reminders]([IsDeleted]);
END
GO

-- =============================================================================
-- SECTION 10: CHANGE TRACKING
-- =============================================================================

-- ChangeTrackingEntries Table
-- Records all data modifications for offline sync capabilities
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChangeTrackingEntries')
BEGIN
    CREATE TABLE [dbo].[ChangeTrackingEntries] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] INT NOT NULL,
        [Operation] INT NOT NULL, -- 0=Insert, 1=Update, 2=Delete
        [EntityJson] NVARCHAR(MAX) NULL, -- JSON snapshot of entity
        [ChangedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ChangedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsSynced] BIT NOT NULL DEFAULT 0,
        [SyncedAt] DATETIME2 NULL,
        [SyncError] NVARCHAR(1000) NULL,
        
        CONSTRAINT [PK_ChangeTrackingEntries] PRIMARY KEY CLUSTERED ([Id])
    );
    
    -- Indexes for Change Tracking (optimized for sync operations)
    CREATE NONCLUSTERED INDEX [IX_ChangeTracking_IsSynced_ChangedAt] ON [dbo].[ChangeTrackingEntries]([IsSynced], [ChangedAt]) WHERE [IsSynced] = 0;
    CREATE NONCLUSTERED INDEX [IX_ChangeTracking_EntityType_EntityId] ON [dbo].[ChangeTrackingEntries]([EntityType], [EntityId]);
    CREATE NONCLUSTERED INDEX [IX_ChangeTracking_ChangedAt] ON [dbo].[ChangeTrackingEntries]([ChangedAt] DESC);
END
GO

PRINT 'Database schema created successfully!';
PRINT 'Total tables: 27';
PRINT 'Performance indexes: Optimized';
PRINT 'Ready for data seeding.';
GO
