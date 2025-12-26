/*
 * Tracker Database Creation Script for SQL Server
 * 
 * This script creates a fully optimized database schema for the Tracker application
 * on SQL Server for networked/multi-user scenarios.
 * 
 * Features:
 * - All tables with proper constraints and relationships
 * - Optimized indexes for common query patterns
 * - Soft delete support (IsDeleted flag)
 * - Audit fields (CreatedAt, ModifiedAt, etc.)
 * - Row versioning for optimistic concurrency
 * 
 * Usage:
 * 1. Create a new database: CREATE DATABASE TrackerDB;
 * 2. Switch to it: USE TrackerDB;
 * 3. Execute this script
 * 
 * Author: Prickly Cactus Software
 * Version: 1.0
 * Last Updated: December 2025
 */

USE [TrackerDB];
GO

-- =============================================================================
-- SECTION 1: CORE TABLES
-- =============================================================================

-- Users Table
-- The logged-in manager who owns all data in the system
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Username] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [DisplayName] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [LastLogin] DATETIME2 NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Users_Username] UNIQUE NONCLUSTERED ([Username])
    );
    
    -- Indexes for Users
    CREATE NONCLUSTERED INDEX [IX_Users_IsActive] ON [dbo].[Users]([IsActive]) INCLUDE ([Username], [DisplayName]);
    CREATE NONCLUSTERED INDEX [IX_Users_IsDeleted] ON [dbo].[Users]([IsDeleted]);
END
GO

-- TeamMembers Table
-- The core entity representing employees/team members
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TeamMembers')
BEGIN
    CREATE TABLE [dbo].[TeamMembers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [NickName] NVARCHAR(50) NULL,
        [Email] NVARCHAR(200) NULL,
        [CellPhone] NVARCHAR(20) NULL,
        [JobTitle] NVARCHAR(100) NULL,
        [HireDate] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [LastOneOnOneDate] DATETIME2 NULL,
        [OneOnOneCadence] INT NOT NULL DEFAULT 14,
        [OpenTaskCount] INT NOT NULL DEFAULT 0,
        [LinkedInProfile] NVARCHAR(500) NULL,
        [FacebookProfile] NVARCHAR(500) NULL,
        [InstagramProfile] NVARCHAR(500) NULL,
        [XProfile] NVARCHAR(500) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TeamMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    
    -- Indexes for TeamMembers (optimized for common queries)
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_UserId] ON [dbo].[TeamMembers]([UserId]) INCLUDE ([FirstName], [LastName], [IsActive]);
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_Email] ON [dbo].[TeamMembers]([Email]);
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_Name] ON [dbo].[TeamMembers]([LastName], [FirstName]) INCLUDE ([Email], [JobTitle], [IsActive]);
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_IsDeleted] ON [dbo].[TeamMembers]([IsDeleted]);
    CREATE NONCLUSTERED INDEX [IX_TeamMembers_IsActive_UserId] ON [dbo].[TeamMembers]([IsActive], [UserId]) INCLUDE ([FirstName], [LastName], [HireDate], [LastOneOnOneDate], [OneOnOneCadence], [OpenTaskCount]);
END
GO

-- =============================================================================
-- SECTION 2: MEETING TABLES
-- =============================================================================

-- OneOnOnes Table
-- Meeting records between managers and team members
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OneOnOnes')
BEGIN
    CREATE TABLE [dbo].[OneOnOnes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [TeamMemberId] INT NULL,
        [Date] DATETIME2 NOT NULL,
        [Duration] INT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [Description] NVARCHAR(500) NULL,
        [Agenda] NVARCHAR(4000) NULL,
        [Notes] NVARCHAR(4000) NULL,
        [Feedback] NVARCHAR(4000) NULL,
        [GoogleCalendarEventId] NVARCHAR(200) NULL,
        [CalendarEventId] NVARCHAR(200) NULL,
        [HasGoogleCalendarEvent] BIT NOT NULL DEFAULT 0,
        [IsRecurring] BIT NOT NULL DEFAULT 0,
        [RecurrencePattern] NVARCHAR(100) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_OneOnOnes] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OneOnOnes_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_OneOnOnes_TeamMembers] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers]([Id]) ON DELETE SET NULL
    );
    
    -- Indexes for OneOnOnes
    CREATE NONCLUSTERED INDEX [IX_OneOnOnes_Date] ON [dbo].[OneOnOnes]([Date] DESC) INCLUDE ([TeamMemberId], [Status]);
    CREATE NONCLUSTERED INDEX [IX_OneOnOnes_TeamMemberId_Date] ON [dbo].[OneOnOnes]([TeamMemberId], [Date] DESC) WHERE [TeamMemberId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_OneOnOnes_UserId_Status_Date] ON [dbo].[OneOnOnes]([UserId], [Status], [Date]) INCLUDE ([TeamMemberId]);
    CREATE NONCLUSTERED INDEX [IX_OneOnOnes_IsDeleted] ON [dbo].[OneOnOnes]([IsDeleted]);
END
GO

-- MeetingTemplates Table
-- Reusable meeting templates for quick setup
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeetingTemplates')
BEGIN
    CREATE TABLE [dbo].[MeetingTemplates] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsDefault] BIT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_MeetingTemplates] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MeetingTemplates_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    
    CREATE NONCLUSTERED INDEX [IX_MeetingTemplates_UserId] ON [dbo].[MeetingTemplates]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_MeetingTemplates_IsDeleted] ON [dbo].[MeetingTemplates]([IsDeleted]);
END
GO

-- MeetingTemplateItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeetingTemplateItems')
BEGIN
    CREATE TABLE [dbo].[MeetingTemplateItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [MeetingTemplateId] INT NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [Category] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_MeetingTemplateItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MeetingTemplateItems_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_MeetingTemplateItems_Templates] FOREIGN KEY ([MeetingTemplateId]) REFERENCES [dbo].[MeetingTemplates]([Id]) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX [IX_MeetingTemplateItems_TemplateId_SortOrder] ON [dbo].[MeetingTemplateItems]([MeetingTemplateId], [SortOrder]);
    CREATE NONCLUSTERED INDEX [IX_MeetingTemplateItems_UserId] ON [dbo].[MeetingTemplateItems]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_MeetingTemplateItems_IsDeleted] ON [dbo].[MeetingTemplateItems]([IsDeleted]);
END
GO

-- Continued in next file...
