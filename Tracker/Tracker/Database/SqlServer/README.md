# Tracker Database - SQL Server Deployment Scripts

## Overview

This directory contains production-ready SQL Server deployment scripts for the Tracker application database. These scripts create a **fully optimized, enterprise-ready database** with strategic indexes, pre-calculated views, and performance optimizations.

## 📋 What's Included

### Deployment Scripts

1. **00_MasterDeploy.sql** - Master deployment script (run this one)
2. **01_CreateDatabase.sql** - Core tables (Users, TeamMembers, OneOnOnes, Templates)
3. **02_CreateTasks.sql** - Task management tables
4. **03_CreateOKRs.sql** - OKR and project management tables
5. **04_CreateKPIsAndRemaining.sql** - KPIs, goals, feedback, notes, reminders
6. **05_CreateViews.sql** - 6 performance-optimized views

### Database Structure

- **27 Tables** covering all Tracker features
- **80+ Strategic Indexes** for query performance
- **6 Performance Views** for dashboard and reporting
- **Full audit trail** support (CreatedAt, ModifiedAt, soft deletes)
- **Row versioning** for optimistic concurrency

## 🚀 Quick Start

### Prerequisites

- SQL Server 2016 or later (recommended: SQL Server 2019+)
- SQL Server Management Studio (SSMS) or Azure Data Studio
- CREATE DATABASE permissions
- Minimum 500MB disk space

### Deployment Steps

#### Option 1: Using SSMS (Recommended)

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open **00_MasterDeploy.sql**
4. Verify the server name in the connection
5. Click **Execute** (F5)
6. Wait for "✓ DEPLOYMENT SUCCESSFUL!" message

#### Option 2: Using Command Line (sqlcmd)

```bash
cd "C:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Database\SqlServer"
sqlcmd -S YOUR_SERVER_NAME -i 00_MasterDeploy.sql
```

#### Option 3: Using PowerShell

```powershell
$ServerName = "YOUR_SERVER_NAME"
$ScriptPath = "C:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Database\SqlServer\00_MasterDeploy.sql"

Invoke-Sqlcmd -ServerInstance $ServerName -InputFile $ScriptPath -Verbose
```

### Verification

After deployment, verify success:

```sql
USE TrackerDB;

-- Check table count (should be 27+)
SELECT COUNT(*) AS TableCount FROM sys.tables WHERE type = 'U';

-- Check view count (should be 6+)
SELECT COUNT(*) AS ViewCount FROM sys.views WHERE type = 'V';

-- Check index count (should be 80+)
SELECT COUNT(*) AS IndexCount 
FROM sys.indexes 
WHERE type IN (1,2) AND is_primary_key = 0 AND is_unique_constraint = 0;

-- Test a view
SELECT TOP 10 * FROM vw_TeamMemberSummary;
```

## 📊 Database Schema

### Core Tables

| Table | Purpose | Key Indexes |
|-------|---------|-------------|
| Users | Logged-in managers | Username (Unique), IsActive |
| TeamMembers | Employees being tracked | UserId, IsActive, Name, Email |
| OneOnOnes | Meeting records | Date, TeamMemberId, UserId+Status+Date |
| Tasks | Individual tasks | OwnerId+IsCompleted, DueDate, ProjectId |
| Projects | Project management | UserId+Status, Name, EndDate |
| ObjectiveKeyResults | OKRs | UserId+Year+TimePeriod, EndDate, OwnerId |
| KeyResults | Key results for OKRs | OkrId+SortOrder |
| KeyPerformanceIndicators | KPIs | UserId+Category, Name, OwnerId |
| IndividualGoals | Personal goals | TeamMemberId+Status, Category |
| Feedbacks | Performance feedback | TeamMemberId+Date, UserId+Type |
| QuickNotes | Notes and journal | UserId+Category+CreatedAt, LinkedEntity |

### Performance Views

| View | Purpose | Use Case |
|------|---------|----------|
| vw_TeamMemberSummary | Aggregated metrics per team member | Dashboard, team overview |
| vw_OkrProgress | OKR completion calculations | OKR dashboard, progress tracking |
| vw_ProjectDashboard | Project status with tasks/risks | Project management screens |
| vw_TaskOverview | Task details with owners | Task lists, filtering |
| vw_UpcomingOneOnOnes | Scheduled meetings with prep data | Calendar view, reminders |
| vw_KpiDashboard | KPI values with status | KPI monitoring, reporting |

## ⚡ Performance Optimizations

### Index Strategy

1. **Foreign Key Indexes** - Every FK has a supporting index
2. **Filtering Indexes** - Indexed on IsDeleted, IsActive, IsCompleted
3. **Composite Indexes** - Multi-column indexes for common WHERE clauses
4. **Covering Indexes** - Include frequently accessed columns
5. **Filtered Indexes** - WHERE clauses for partial indexes on common filters

### Examples of Optimized Queries

```sql
-- ✓ OPTIMIZED: Uses IX_Tasks_UserId_IsCompleted
SELECT * FROM Tasks 
WHERE UserId = 1 AND IsCompleted = 0;

-- ✓ OPTIMIZED: Uses IX_OneOnOnes_TeamMemberId_Date
SELECT * FROM OneOnOnes 
WHERE TeamMemberId = 5 
ORDER BY Date DESC;

-- ✓ OPTIMIZED: Uses IX_TeamMembers_IsActive_UserId (covering)
SELECT FirstName, LastName, LastOneOnOneDate 
FROM TeamMembers 
WHERE IsActive = 1 AND UserId = 1;

-- ✓ OPTIMIZED: Uses view with pre-calculated aggregations
SELECT * FROM vw_TeamMemberSummary 
WHERE OpenTasks > 0;
```

## 🔧 Configuration

### Connection String

For the Tracker application, use this connection string format:

```
Server=YOUR_SERVER_NAME;Database=TrackerDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;
```

### Recommended SQL Server Settings

```sql
-- Enable query optimization
ALTER DATABASE TrackerDB SET AUTO_UPDATE_STATISTICS_ASYNC ON;

-- Enable page compression for better storage
ALTER TABLE Tasks REBUILD WITH (DATA_COMPRESSION = PAGE);
ALTER TABLE OneOnOnes REBUILD WITH (DATA_COMPRESSION = PAGE);

-- Set max degree of parallelism (for multi-core servers)
EXEC sp_configure 'max degree of parallelism', 4;
RECONFIGURE;
```

## 📈 Maintenance

### Regular Maintenance Tasks

```sql
-- Update statistics weekly
EXEC sp_updatestats;

-- Rebuild fragmented indexes monthly
ALTER INDEX ALL ON Tasks REBUILD;
ALTER INDEX ALL ON OneOnOnes REBUILD;
ALTER INDEX ALL ON ObjectiveKeyResults REBUILD;

-- Check database size
EXEC sp_spaceused;

-- View index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

### Backup Strategy

```sql
-- Full backup (weekly)
BACKUP DATABASE TrackerDB 
TO DISK = 'C:\Backups\TrackerDB_Full.bak'
WITH INIT, COMPRESSION;

-- Differential backup (daily)
BACKUP DATABASE TrackerDB 
TO DISK = 'C:\Backups\TrackerDB_Diff.bak'
WITH DIFFERENTIAL, COMPRESSION;
```

## 🔍 Troubleshooting

### Common Issues

**Issue: "Database already exists" error**
```sql
-- Option 1: Use existing database
USE TrackerDB;
-- Then run individual scripts 01-05

-- Option 2: Drop and recreate (⚠️ DELETES ALL DATA)
DROP DATABASE TrackerDB;
-- Then run 00_MasterDeploy.sql
```

**Issue: Slow query performance**
```sql
-- Check missing indexes
SELECT 
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX IX_' + OBJECT_NAME(mid.object_id) + '_' + 
    REPLACE(REPLACE(REPLACE(ISNULL(mid.equality_columns,''),', ','_'),']',''),'[','') AS create_index_statement,
    mid.*
FROM sys.dm_db_missing_index_groups mig
JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY improvement_measure DESC;
```

**Issue: Row versioning errors**
```sql
-- Check if row versioning is enabled
SELECT name, is_read_committed_snapshot_on, snapshot_isolation_state
FROM sys.databases
WHERE name = 'TrackerDB';

-- Enable if needed
ALTER DATABASE TrackerDB SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE TrackerDB SET READ_COMMITTED_SNAPSHOT ON;
```

## 📝 Migration from SQLite

If migrating from SQLite to SQL Server:

1. Export data from SQLite using Entity Framework migrations
2. Deploy this SQL Server schema
3. Import data using SSIS, BCP, or custom migration script
4. Update Tracker application connection settings
5. Test thoroughly before switching production

## 🎯 Best Practices

### Query Writing

✅ **DO**
- Use the pre-built views for dashboards and reports
- Filter on indexed columns (UserId, IsDeleted, IsActive)
- Use EXISTS instead of IN for subqueries
- Leverage covering indexes with INCLUDE columns

❌ **DON'T**
- Use SELECT * in production queries
- Ignore soft deletes (always filter `WHERE IsDeleted = 0`)
- Create indexes without analyzing query plans first
- Modify view definitions without testing impact

### Application Integration

```csharp
// Update DatabaseSettings in Tracker application
var settings = new DatabaseSettings
{
    Type = DatabaseType.SqlServer,
    ServerName = "YOUR_SERVER_NAME",
    DatabaseName = "TrackerDB",
    IntegratedSecurity = true
};

var context = new TrackerDbContext(settings);
```

## 📞 Support

For issues or questions:
- Check the Tracker documentation
- Review query execution plans
- Contact: support@pricklycactus.com

## 📄 License

Copyright © 2025 Prickly Cactus Software
All rights reserved.

---

**Version:** 1.0  
**Last Updated:** December 2025  
**Compatible With:** Tracker v1.0+, SQL Server 2016+
