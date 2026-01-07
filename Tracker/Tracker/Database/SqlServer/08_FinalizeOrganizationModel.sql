/*
 * TRACKER DATABASE - FINALIZE ORGANIZATION MODEL
 * SQL Server Edition
 * 
 * Run this script AFTER migrating existing data to set OrganizationId values.
 * This script:
 * - Adds NOT NULL constraints to OrganizationId columns
 * - Creates foreign key relationships to Organizations table
 * - Creates views for common queries
 * 
 * PREREQUISITE: All OrganizationId columns must be populated!
 * 
 * Author: Prickly Cactus Software
 * Version: 2.0 (Organization Model)
 * Last Updated: January 2025
 */

USE [TrackerDB];
GO

PRINT '=============================================='
PRINT 'Finalizing Organization Model...'
PRINT '=============================================='
GO

-- =============================================================================
-- SECTION 1: VERIFY DATA MIGRATION
-- =============================================================================

PRINT 'Checking for NULL OrganizationId values...'

DECLARE @HasNulls BIT = 0;
DECLARE @TableName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);
DECLARE @NullCount INT;

DECLARE check_cursor CURSOR FOR
SELECT t.name
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE c.name = 'OrganizationId'
  AND t.name NOT IN ('Organizations')
  AND c.is_nullable = 1;

OPEN check_cursor;
FETCH NEXT FROM check_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'SELECT @Count = COUNT(*) FROM [dbo].[' + @TableName + '] WHERE [OrganizationId] IS NULL';
    EXEC sp_executesql @SQL, N'@Count INT OUTPUT', @NullCount OUTPUT;
    
    IF @NullCount > 0
    BEGIN
        PRINT 'WARNING: ' + @TableName + ' has ' + CAST(@NullCount AS NVARCHAR) + ' rows with NULL OrganizationId';
        SET @HasNulls = 1;
    END
    
    FETCH NEXT FROM check_cursor INTO @TableName;
END

CLOSE check_cursor;
DEALLOCATE check_cursor;

IF @HasNulls = 1
BEGIN
    PRINT ''
    PRINT 'ERROR: Cannot finalize - there are tables with NULL OrganizationId values.'
    PRINT 'Please run data migration first to populate OrganizationId for all records.'
    PRINT ''
    -- Comment out the RETURN to continue anyway (for development)
    -- RETURN;
END
ELSE
BEGIN
    PRINT 'All OrganizationId values are populated. Proceeding with finalization...'
END
GO

-- =============================================================================
-- SECTION 2: ADD FOREIGN KEY CONSTRAINT TO USERS
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_Organizations')
BEGIN
    PRINT 'Adding FK_Users_Organizations...'
    
    -- First, add default org for any NULL values
    DECLARE @DefaultOrgId UNIQUEIDENTIFIER;
    SELECT TOP 1 @DefaultOrgId = Id FROM dbo.Organizations WHERE IsDeleted = 0;
    
    IF @DefaultOrgId IS NOT NULL
    BEGIN
        UPDATE dbo.Users SET OrganizationId = @DefaultOrgId WHERE OrganizationId IS NULL;
    END
    
    -- Make column NOT NULL (if no nulls remain)
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE OrganizationId IS NULL)
    BEGIN
        ALTER TABLE dbo.Users ALTER COLUMN OrganizationId UNIQUEIDENTIFIER NOT NULL;
        
        ALTER TABLE dbo.Users
        ADD CONSTRAINT FK_Users_Organizations 
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id);
        
        PRINT '  FK_Users_Organizations created.'
    END
END
GO

-- =============================================================================
-- SECTION 3: ADD FOREIGN KEY CONSTRAINT TO TEAM MEMBERS
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TeamMembers_Organizations')
BEGIN
    PRINT 'Adding FK_TeamMembers_Organizations...'
    
    DECLARE @DefaultOrgId UNIQUEIDENTIFIER;
    SELECT TOP 1 @DefaultOrgId = Id FROM dbo.Organizations WHERE IsDeleted = 0;
    
    IF @DefaultOrgId IS NOT NULL
    BEGIN
        UPDATE dbo.TeamMembers SET OrganizationId = @DefaultOrgId WHERE OrganizationId IS NULL;
    END
    
    IF NOT EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE OrganizationId IS NULL)
    BEGIN
        ALTER TABLE dbo.TeamMembers ALTER COLUMN OrganizationId UNIQUEIDENTIFIER NOT NULL;
        
        ALTER TABLE dbo.TeamMembers
        ADD CONSTRAINT FK_TeamMembers_Organizations 
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id);
        
        PRINT '  FK_TeamMembers_Organizations created.'
    END
END
GO

-- =============================================================================
-- SECTION 4: CREATE DASHBOARD VIEWS
-- =============================================================================

PRINT 'Creating dashboard views...'

-- View: Team Member Dashboard Summary
IF OBJECT_ID('dbo.vw_TeamMemberDashboard', 'V') IS NOT NULL
    DROP VIEW dbo.vw_TeamMemberDashboard;
GO

CREATE VIEW dbo.vw_TeamMemberDashboard
AS
SELECT 
    tm.Id,
    tm.OrganizationId,
    tm.CurrentManagerUserId,
    tm.FirstName,
    tm.LastName,
    tm.NickName,
    COALESCE(tm.NickName, tm.FirstName) + ' ' + ISNULL(tm.LastName, '') AS DisplayName,
    tm.Email,
    tm.JobTitle,
    tm.HireDate,
    tm.IsActive,
    tm.LastOneOnOneDate,
    tm.OneOnOneCadence,
    tm.OpenTaskCount,
    
    -- Calculated fields
    CASE 
        WHEN tm.LastOneOnOneDate IS NULL THEN -1
        ELSE DATEDIFF(DAY, tm.LastOneOnOneDate, GETUTCDATE())
    END AS DaysSinceLastOneOnOne,
    
    CASE 
        WHEN tm.LastOneOnOneDate IS NULL THEN 1
        WHEN DATEDIFF(DAY, tm.LastOneOnOneDate, GETUTCDATE()) > tm.OneOnOneCadence THEN 1
        ELSE 0
    END AS IsOverdueForMeeting,
    
    -- Tenure
    DATEDIFF(YEAR, tm.HireDate, GETUTCDATE()) AS YearsOfService,
    
    tm.CreatedAt,
    tm.LastModifiedAt

FROM dbo.TeamMembers tm
WHERE tm.IsDeleted = 0;
GO

PRINT '  Created vw_TeamMemberDashboard'

-- View: Meetings Due
IF OBJECT_ID('dbo.vw_MeetingsDue', 'V') IS NOT NULL
    DROP VIEW dbo.vw_MeetingsDue;
GO

CREATE VIEW dbo.vw_MeetingsDue
AS
SELECT 
    tm.Id AS TeamMemberId,
    tm.OrganizationId,
    tm.CurrentManagerUserId,
    COALESCE(tm.NickName, tm.FirstName) + ' ' + ISNULL(tm.LastName, '') AS TeamMemberName,
    tm.JobTitle,
    tm.LastOneOnOneDate,
    tm.OneOnOneCadence,
    
    -- When next meeting is due
    CASE 
        WHEN tm.LastOneOnOneDate IS NULL THEN CAST(GETUTCDATE() AS DATE)
        ELSE DATEADD(DAY, tm.OneOnOneCadence, CAST(tm.LastOneOnOneDate AS DATE))
    END AS NextMeetingDue,
    
    -- Days until due (negative = overdue)
    CASE 
        WHEN tm.LastOneOnOneDate IS NULL THEN 
            -1 * DATEDIFF(DAY, tm.HireDate, GETUTCDATE())
        ELSE 
            tm.OneOnOneCadence - DATEDIFF(DAY, tm.LastOneOnOneDate, GETUTCDATE())
    END AS DaysUntilDue,
    
    -- Priority (lower = more urgent)
    CASE 
        WHEN tm.LastOneOnOneDate IS NULL THEN 0
        WHEN DATEDIFF(DAY, tm.LastOneOnOneDate, GETUTCDATE()) > tm.OneOnOneCadence * 2 THEN 1
        WHEN DATEDIFF(DAY, tm.LastOneOnOneDate, GETUTCDATE()) > tm.OneOnOneCadence THEN 2
        ELSE 3
    END AS UrgencyPriority

FROM dbo.TeamMembers tm
WHERE tm.IsActive = 1 
  AND tm.IsDeleted = 0;
GO

PRINT '  Created vw_MeetingsDue'

-- View: Task Summary
IF OBJECT_ID('dbo.vw_TaskSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_TaskSummary;
GO

CREATE VIEW dbo.vw_TaskSummary
AS
SELECT 
    t.Id,
    t.OrganizationId,
    t.OwnerId AS TeamMemberId,
    t.ProjectId,
    t.Description AS Title,
    CASE t.IsCompleted WHEN 1 THEN 'completed' ELSE 'open' END AS Status,
    t.Priority,
    t.DueDate,
    t.CompletedDate,
    
    -- Team member info
    COALESCE(tm.NickName, tm.FirstName) + ' ' + ISNULL(tm.LastName, '') AS AssigneeName,
    
    -- Project info
    p.Name AS ProjectName,
    
    -- Calculated fields
    CASE 
        WHEN t.IsCompleted = 1 THEN 0
        WHEN t.DueDate IS NULL THEN 0
        WHEN t.DueDate < GETUTCDATE() THEN 1
        ELSE 0
    END AS IsOverdue,
    
    CASE 
        WHEN t.DueDate IS NULL THEN NULL
        ELSE DATEDIFF(DAY, GETUTCDATE(), t.DueDate)
    END AS DaysUntilDue,
    
    t.CreatedAt,
    t.LastModifiedAt

FROM dbo.Tasks t
INNER JOIN dbo.TeamMembers tm ON t.OwnerId = tm.Id
LEFT JOIN dbo.Projects p ON t.ProjectId = p.ID
WHERE t.IsDeleted = 0;
GO

PRINT '  Created vw_TaskSummary'

-- =============================================================================
-- SECTION 5: CREATE ORGANIZATION STATS PROCEDURE
-- =============================================================================

IF OBJECT_ID('dbo.sp_GetOrganizationStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOrganizationStats;
GO

CREATE PROCEDURE dbo.sp_GetOrganizationStats
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        @OrganizationId AS OrganizationId,
        (SELECT COUNT(*) FROM dbo.Users 
         WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND IsDeleted = 0) AS ActiveUsers,
        (SELECT COUNT(*) FROM dbo.TeamMembers 
         WHERE OrganizationId = @OrganizationId AND IsActive = 1 AND IsDeleted = 0) AS ActiveTeamMembers,
        (SELECT COUNT(*) FROM dbo.OneOnOnes 
         WHERE OrganizationId = @OrganizationId AND IsDeleted = 0) AS TotalOneOnOnes,
        (SELECT COUNT(*) FROM dbo.OneOnOnes 
         WHERE OrganizationId = @OrganizationId 
           AND [Date] >= DATEADD(MONTH, -1, GETUTCDATE())
           AND IsDeleted = 0) AS OneOnOnesLastMonth,
        (SELECT COUNT(*) FROM dbo.Tasks 
         WHERE OrganizationId = @OrganizationId AND IsDeleted = 0) AS TotalTasks,
        (SELECT COUNT(*) FROM dbo.Tasks 
         WHERE OrganizationId = @OrganizationId 
           AND IsCompleted = 0 
           AND IsDeleted = 0) AS OpenTasks,
        (SELECT COUNT(*) FROM dbo.VectorEmbeddings 
         WHERE OrganizationId = @OrganizationId) AS TotalEmbeddings;
END
GO

PRINT '  Created sp_GetOrganizationStats'

-- =============================================================================
-- SECTION 6: VERIFY SCHEMA
-- =============================================================================

PRINT ''
PRINT '=============================================='
PRINT 'Verifying Schema...'
PRINT '=============================================='

SELECT 
    t.name AS TableName,
    COUNT(c.name) AS ColumnCount,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = t.object_id AND name = 'OrganizationId'
    ) THEN 'Yes' ELSE 'No' END AS HasOrganizationId
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.name NOT LIKE 'sys%'
  AND t.name NOT LIKE '__EF%'
GROUP BY t.name, t.object_id
ORDER BY t.name;

PRINT ''
PRINT '=============================================='
PRINT 'Organization Model Finalization Complete!'
PRINT '=============================================='
GO
