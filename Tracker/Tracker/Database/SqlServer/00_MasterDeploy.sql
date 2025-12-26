/*
 * TRACKER DATABASE - MASTER DEPLOYMENT SCRIPT
 * SQL Server Edition
 * 
 * This master script executes all sub-scripts in the correct order
 * to create a fully optimized Tracker database on SQL Server.
 * 
 * PREREQUISITES:
 * 1. SQL Server 2016 or later (recommended: SQL Server 2019+)
 * 2. CREATE DATABASE permissions
 * 3. Sufficient disk space (minimum 500MB for initial database)
 * 
 * DEPLOYMENT STEPS:
 * 1. Create a new database manually or use this script
 * 2. Execute this master script
 * 3. Verify deployment success
 * 
 * PERFORMANCE OPTIMIZATIONS INCLUDED:
 * - 80+ strategic indexes on foreign keys, filtering columns, and composite queries
 * - 6 pre-calculated views for dashboard and reporting queries
 * - Row-level versioning for optimistic concurrency control
 * - Soft delete support with indexed IsDeleted columns
 * - Automatic audit field population
 * 
 * Author: Prickly Cactus Software
 * Version: 1.0
 * Last Updated: December 2025
 */

-- =============================================================================
-- STEP 1: CREATE DATABASE (if it doesn't exist)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TrackerDB')
BEGIN
    CREATE DATABASE [TrackerDB]
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    
    ALTER DATABASE [TrackerDB] SET RECOVERY SIMPLE; -- Simple recovery for smaller log files
    ALTER DATABASE [TrackerDB] SET AUTO_UPDATE_STATISTICS_ASYNC ON; -- Better query performance
    ALTER DATABASE [TrackerDB] SET PAGE_VERIFY CHECKSUM; -- Data integrity
    
    PRINT 'Database [TrackerDB] created successfully.';
END
ELSE
BEGIN
    PRINT 'Database [TrackerDB] already exists. Using existing database.';
END
GO

USE [TrackerDB];
GO

PRINT '=============================================================================';
PRINT 'TRACKER DATABASE DEPLOYMENT';
PRINT '=============================================================================';
PRINT 'Starting deployment at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
GO

-- =============================================================================
-- STEP 2: EXECUTE CORE TABLE SCRIPTS
-- =============================================================================
PRINT 'Creating core tables (Users, TeamMembers, OneOnOnes)...';
:r "01_CreateDatabase.sql"
PRINT 'Core tables created.';
PRINT '';
GO

PRINT 'Creating task tables...';
:r "02_CreateTasks.sql"
PRINT 'Task tables created.';
PRINT '';
GO

PRINT 'Creating OKR and project tables...';
:r "03_CreateOKRs.sql"
PRINT 'OKR and project tables created.';
PRINT '';
GO

PRINT 'Creating KPI, goals, and supporting tables...';
:r "04_CreateKPIsAndRemaining.sql"
PRINT 'Remaining tables created.';
PRINT '';
GO

-- =============================================================================
-- STEP 3: CREATE PERFORMANCE VIEWS
-- =============================================================================
PRINT 'Creating performance views...';
:r "05_CreateViews.sql"
PRINT 'Performance views created.';
PRINT '';
GO

-- =============================================================================
-- STEP 4: VERIFICATION
-- =============================================================================
PRINT '=============================================================================';
PRINT 'DEPLOYMENT VERIFICATION';
PRINT '=============================================================================';

DECLARE @TableCount INT;
DECLARE @ViewCount INT;
DECLARE @IndexCount INT;

SELECT @TableCount = COUNT(*) FROM sys.tables WHERE type = 'U';
SELECT @ViewCount = COUNT(*) FROM sys.views WHERE type = 'V';
SELECT @IndexCount = COUNT(*) FROM sys.indexes WHERE type IN (1,2) AND is_primary_key = 0 AND is_unique_constraint = 0;

PRINT 'Tables created: ' + CAST(@TableCount AS VARCHAR);
PRINT 'Views created: ' + CAST(@ViewCount AS VARCHAR);
PRINT 'Performance indexes created: ' + CAST(@IndexCount AS VARCHAR);
PRINT '';

-- Verify expected counts
IF @TableCount >= 27 AND @ViewCount >= 6 AND @IndexCount >= 80
BEGIN
    PRINT '✓ DEPLOYMENT SUCCESSFUL!';
    PRINT '';
    PRINT 'Database is ready for use.';
    PRINT 'Connection string: Server=YOUR_SERVER;Database=TrackerDB;Integrated Security=True;';
END
ELSE
BEGIN
    PRINT '⚠ WARNING: Deployment may be incomplete.';
    PRINT 'Expected: 27+ tables, 6+ views, 80+ indexes';
    PRINT 'Found: ' + CAST(@TableCount AS VARCHAR) + ' tables, ' + CAST(@ViewCount AS VARCHAR) + ' views, ' + CAST(@IndexCount AS VARCHAR) + ' indexes';
END

PRINT '';
PRINT 'Deployment completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '=============================================================================';
GO

-- =============================================================================
-- STEP 5: SAMPLE QUERIES TO TEST VIEWS
-- =============================================================================
/*
-- Test Team Member Summary View
SELECT TOP 10 * FROM vw_TeamMemberSummary ORDER BY LastName, FirstName;

-- Test OKR Progress View
SELECT * FROM vw_OkrProgress WHERE IsActive = 1 ORDER BY CompletionPercentage DESC;

-- Test Project Dashboard View
SELECT * FROM vw_ProjectDashboard WHERE Status NOT IN ('Completed', 'Done') ORDER BY DaysRemaining;

-- Test Task Overview
SELECT * FROM vw_TaskOverview WHERE IsOverdue = 1 ORDER BY DueDate;

-- Test Upcoming Meetings
SELECT * FROM vw_UpcomingOneOnOnes ORDER BY Date;

-- Test KPI Dashboard
SELECT * FROM vw_KpiDashboard ORDER BY Category, Name;
*/
