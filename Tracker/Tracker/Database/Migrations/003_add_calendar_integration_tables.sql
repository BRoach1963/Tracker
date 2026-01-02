-- ============================================================================
-- Migration: 003_add_calendar_integration_tables.sql
-- Description: Adds tables for calendar integration (CalendarLinks and CalendarSyncTokens)
-- Date: 2024-12-29
-- 
-- This migration adds support for tracking calendar sync state between
-- Tracker meetings and external calendar providers (Google Calendar, Outlook).
--
-- For SQLite: Run via EF Core migrations or manually execute this script
-- For SQL Server: Run this script in SSMS or via SqlCmd
-- ============================================================================

-- ============================================================================
-- STEP 1: Create CalendarLinks table
-- Links Tracker OneOnOne meetings to external calendar events
-- ============================================================================

-- SQLite version (no IF NOT EXISTS for columns in SQLite, but CREATE TABLE IF NOT EXISTS works)
CREATE TABLE IF NOT EXISTS CalendarLinks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OneOnOneId INTEGER NOT NULL,
    ProviderId TEXT NOT NULL,
    ExternalEventId TEXT NOT NULL,
    ETag TEXT,
    LastSyncedAt TEXT NOT NULL DEFAULT (datetime('now')),
    LastSyncDirection TEXT NOT NULL DEFAULT 'Push',
    Status TEXT NOT NULL DEFAULT 'Synced',
    LastError TEXT,
    UserId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    CreatedBy TEXT NOT NULL DEFAULT '',
    LastModifiedAt TEXT NOT NULL DEFAULT (datetime('now')),
    LastModifiedBy TEXT NOT NULL DEFAULT '',
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT,
    DeletedBy TEXT,
    FOREIGN KEY (OneOnOneId) REFERENCES OneOnOnes(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT
);

-- Create indexes for CalendarLinks
CREATE INDEX IF NOT EXISTS IX_CalendarLinks_Meeting_Provider 
    ON CalendarLinks(OneOnOneId, ProviderId);
    
CREATE INDEX IF NOT EXISTS IX_CalendarLinks_Provider_ExternalId 
    ON CalendarLinks(ProviderId, ExternalEventId);
    
CREATE INDEX IF NOT EXISTS IX_CalendarLinks_UserId 
    ON CalendarLinks(UserId);

-- Unique constraint: one link per provider per meeting
CREATE UNIQUE INDEX IF NOT EXISTS UX_CalendarLinks_Meeting_Provider 
    ON CalendarLinks(OneOnOneId, ProviderId) 
    WHERE IsDeleted = 0;

-- ============================================================================
-- STEP 2: Create CalendarSyncTokens table
-- Stores delta sync tokens for incremental calendar synchronization
-- ============================================================================

CREATE TABLE IF NOT EXISTS CalendarSyncTokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderId TEXT NOT NULL,
    SyncToken TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UserId INTEGER NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create indexes for CalendarSyncTokens
CREATE INDEX IF NOT EXISTS IX_CalendarSyncTokens_Provider 
    ON CalendarSyncTokens(ProviderId);

-- Unique constraint: one token per provider per user
CREATE UNIQUE INDEX IF NOT EXISTS UX_CalendarSyncTokens_User_Provider 
    ON CalendarSyncTokens(UserId, ProviderId);

-- ============================================================================
-- SQL Server version (uncomment if using SQL Server)
-- ============================================================================
/*
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CalendarLinks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CalendarLinks] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OneOnOneId] INT NOT NULL,
        [ProviderId] NVARCHAR(20) NOT NULL,
        [ExternalEventId] NVARCHAR(500) NOT NULL,
        [ETag] NVARCHAR(500) NULL,
        [LastSyncedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastSyncDirection] NVARCHAR(10) NOT NULL DEFAULT 'Push',
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Synced',
        [LastError] NVARCHAR(2000) NULL,
        [UserId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(256) NOT NULL DEFAULT '',
        [LastModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(256) NOT NULL DEFAULT '',
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(256) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [FK_CalendarLinks_OneOnOnes] FOREIGN KEY ([OneOnOneId]) REFERENCES [dbo].[OneOnOnes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CalendarLinks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_CalendarLinks_Meeting_Provider] ON [dbo].[CalendarLinks]([OneOnOneId], [ProviderId]);
    CREATE INDEX [IX_CalendarLinks_Provider_ExternalId] ON [dbo].[CalendarLinks]([ProviderId], [ExternalEventId]);
    CREATE INDEX [IX_CalendarLinks_UserId] ON [dbo].[CalendarLinks]([UserId]);
    CREATE UNIQUE INDEX [UX_CalendarLinks_Meeting_Provider] ON [dbo].[CalendarLinks]([OneOnOneId], [ProviderId]) WHERE [IsDeleted] = 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CalendarSyncTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CalendarSyncTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProviderId] NVARCHAR(20) NOT NULL,
        [SyncToken] NVARCHAR(2000) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UserId] INT NOT NULL,
        CONSTRAINT [FK_CalendarSyncTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_CalendarSyncTokens_Provider] ON [dbo].[CalendarSyncTokens]([ProviderId]);
    CREATE UNIQUE INDEX [UX_CalendarSyncTokens_User_Provider] ON [dbo].[CalendarSyncTokens]([UserId], [ProviderId]);
END
GO
*/

-- ============================================================================
-- End of migration
-- ============================================================================
