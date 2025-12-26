/*
 * Tracker Database Creation Script - Part 2
 * Tasks, Agenda Items, and Project Management Tables
 */

USE [TrackerDB];
GO

-- =============================================================================
-- SECTION 3: TASK TABLES
-- =============================================================================

-- IndividualTasks Table
-- Tasks that can be standalone or belong to projects
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE [dbo].[Tasks] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [ProjectId] INT NULL,
        [ParentTaskId] INT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [Notes] NVARCHAR(2000) NULL,
        [DueDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [IsCompleted] BIT NOT NULL DEFAULT 0,
        [Priority] INT NOT NULL DEFAULT 0,
        [EstimatedHours] DECIMAL(10,2) NULL,
        [ActualHours] DECIMAL(10,2) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Tasks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Tasks_TeamMembers] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TeamMembers]([Id]),
        CONSTRAINT [FK_Tasks_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([ID]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tasks_ParentTask] FOREIGN KEY ([ParentTaskId]) REFERENCES [dbo].[Tasks]([Id])
    );
    
    -- Indexes for Tasks (heavily optimized for filtering and sorting)
    CREATE NONCLUSTERED INDEX [IX_Tasks_UserId_IsCompleted] ON [dbo].[Tasks]([UserId], [IsCompleted]) INCLUDE ([OwnerId], [DueDate], [Priority], [Description]);
    CREATE NONCLUSTERED INDEX [IX_Tasks_OwnerId_IsCompleted_DueDate] ON [dbo].[Tasks]([OwnerId], [IsCompleted], [DueDate]) INCLUDE ([Description], [Priority]);
    CREATE NONCLUSTERED INDEX [IX_Tasks_ProjectId] ON [dbo].[Tasks]([ProjectId]) WHERE [ProjectId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_Tasks_ParentTaskId] ON [dbo].[Tasks]([ParentTaskId]) WHERE [ParentTaskId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_Tasks_DueDate_IsCompleted] ON [dbo].[Tasks]([DueDate], [IsCompleted]) WHERE [DueDate] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_Tasks_IsDeleted] ON [dbo].[Tasks]([IsDeleted]);
END
GO

-- MeetingTasks Table
-- Tasks created during 1:1 meetings
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeetingTasks')
BEGIN
    CREATE TABLE [dbo].[MeetingTasks] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OneOnOneId] INT NOT NULL,
        [OwnerId] INT NOT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [Notes] NVARCHAR(2000) NULL,
        [DueDate] DATETIME2 NULL,
        [CompletedDate] DATETIME2 NULL,
        [IsCompleted] BIT NOT NULL DEFAULT 0,
        [Priority] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_MeetingTasks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MeetingTasks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_MeetingTasks_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MeetingTasks_TeamMembers] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TeamMembers]([Id])
    );
    
    -- Indexes for MeetingTasks
    CREATE NONCLUSTERED INDEX [IX_MeetingTasks_OneOnOneId] ON [dbo].[MeetingTasks]([OneOnOneId]);
    CREATE NONCLUSTERED INDEX [IX_MeetingTasks_UserId_IsCompleted] ON [dbo].[MeetingTasks]([UserId], [IsCompleted]) INCLUDE ([DueDate], [OwnerId]);
    CREATE NONCLUSTERED INDEX [IX_MeetingTasks_DueDate_IsCompleted] ON [dbo].[MeetingTasks]([DueDate], [IsCompleted]) WHERE [DueDate] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_MeetingTasks_IsDeleted] ON [dbo].[MeetingTasks]([IsDeleted]);
END
GO

-- TaskCollections Table
-- Groups of tasks treated as single measurable units
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskCollections')
BEGIN
    CREATE TABLE [dbo].[TaskCollections] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_TaskCollections] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TaskCollections_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    
    CREATE NONCLUSTERED INDEX [IX_TaskCollections_UserId_Name] ON [dbo].[TaskCollections]([UserId], [Name]);
    CREATE NONCLUSTERED INDEX [IX_TaskCollections_IsDeleted] ON [dbo].[TaskCollections]([IsDeleted]);
END
GO

-- TaskCollectionItems Table
-- Links between TaskCollections and tasks
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskCollectionItems')
BEGIN
    CREATE TABLE [dbo].[TaskCollectionItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [CollectionId] INT NOT NULL,
        [TaskId] INT NOT NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_TaskCollectionItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TaskCollectionItems_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_TaskCollectionItems_Collections] FOREIGN KEY ([CollectionId]) REFERENCES [dbo].[TaskCollections]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskCollectionItems_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_TaskCollectionItems_Collection_Task] UNIQUE ([CollectionId], [TaskId])
    );
    
    CREATE NONCLUSTERED INDEX [IX_TaskCollectionItems_TaskId] ON [dbo].[TaskCollectionItems]([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskCollectionItems_UserId] ON [dbo].[TaskCollectionItems]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_TaskCollectionItems_IsDeleted] ON [dbo].[TaskCollectionItems]([IsDeleted]);
END
GO

-- =============================================================================
-- SECTION 4: AGENDA AND LINKED ITEMS
-- =============================================================================

-- AgendaItems Table
-- Topics, concerns, questions discussed in 1:1 meetings
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgendaItems')
BEGIN
    CREATE TABLE [dbo].[AgendaItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OneOnOneId] INT NOT NULL,
        [LinkedTaskId] INT NULL,
        [Category] INT NOT NULL DEFAULT 0,
        [Description] NVARCHAR(1000) NOT NULL,
        [Resolution] NVARCHAR(2000) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [Status] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_AgendaItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AgendaItems_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_AgendaItems_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AgendaItems_MeetingTasks] FOREIGN KEY ([LinkedTaskId]) REFERENCES [dbo].[MeetingTasks]([Id]) ON DELETE SET NULL
    );
    
    CREATE NONCLUSTERED INDEX [IX_AgendaItems_OneOnOneId_Category] ON [dbo].[AgendaItems]([OneOnOneId], [Category]) INCLUDE ([Status], [Description]);
    CREATE NONCLUSTERED INDEX [IX_AgendaItems_UserId_Category_Status] ON [dbo].[AgendaItems]([UserId], [Category], [Status]);
    CREATE NONCLUSTERED INDEX [IX_AgendaItems_IsDeleted] ON [dbo].[AgendaItems]([IsDeleted]);
END
GO

-- LinkedItems Table
-- Links from agenda items to other entities (Tasks, OKRs, KPIs, Projects)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LinkedItems')
BEGIN
    CREATE TABLE [dbo].[LinkedItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AgendaItemId] INT NOT NULL,
        [Type] INT NOT NULL, -- 0=Task, 1=OKR, 2=KPI, 3=Project
        [ItemId] INT NOT NULL,
        [Title] NVARCHAR(200) NULL,
        
        CONSTRAINT [PK_LinkedItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_LinkedItems_AgendaItems] FOREIGN KEY ([AgendaItemId]) REFERENCES [dbo].[AgendaItems]([Id]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_LinkedItems_AgendaItemId] ON [dbo].[LinkedItems]([AgendaItemId]);
    CREATE NONCLUSTERED INDEX [IX_LinkedItems_Type_ItemId] ON [dbo].[LinkedItems]([Type], [ItemId]);
END
GO

-- OneOnOneLinkedTasks Table
-- Links between OneOnOne meetings and existing IndividualTasks discussed
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OneOnOneLinkedTasks')
BEGIN
    CREATE TABLE [dbo].[OneOnOneLinkedTasks] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OneOnOneId] INT NOT NULL,
        [TaskId] INT NOT NULL,
        [DiscussionNotes] NVARCHAR(2000) NULL,
        
        CONSTRAINT [PK_OneOnOneLinkedTasks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OneOnOneLinkedTasks_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OneOnOneLinkedTasks_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]),
        CONSTRAINT [UQ_OneOnOneLinkedTasks_Meeting_Task] UNIQUE ([OneOnOneId], [TaskId])
    );
    
    CREATE NONCLUSTERED INDEX [IX_OneOnOneLinkedTasks_TaskId] ON [dbo].[OneOnOneLinkedTasks]([TaskId]);
END
GO

-- Continued in next file...
