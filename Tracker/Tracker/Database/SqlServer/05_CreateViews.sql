/*
 * Tracker Database Views - Performance Optimization
 * 
 * These views pre-calculate complex aggregations and joins for common queries.
 * Using these views instead of raw queries dramatically improves dashboard performance.
 */

USE [TrackerDB];
GO

-- =============================================================================
-- VIEW 1: Team Member Summary
-- Aggregates key metrics for each team member for dashboard display
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_TeamMemberSummary')
    DROP VIEW [dbo].[vw_TeamMemberSummary];
GO

CREATE VIEW [dbo].[vw_TeamMemberSummary]
AS
SELECT 
    tm.[Id] AS TeamMemberId,
    tm.[UserId],
    tm.[FirstName],
    tm.[LastName],
    tm.[Email],
    tm.[JobTitle],
    tm.[HireDate],
    tm.[IsActive],
    tm.[LastOneOnOneDate],
    tm.[OneOnOneCadence],
    
    -- One-on-One metrics
    (SELECT COUNT(*) 
     FROM [dbo].[OneOnOnes] o 
     WHERE o.[TeamMemberId] = tm.[Id] AND o.[IsDeleted] = 0) AS TotalMeetings,
    
    (SELECT COUNT(*) 
     FROM [dbo].[OneOnOnes] o 
     WHERE o.[TeamMemberId] = tm.[Id] 
       AND o.[Status] = 1 -- Completed
       AND o.[IsDeleted] = 0) AS CompletedMeetings,
    
    (SELECT TOP 1 o.[Date]
     FROM [dbo].[OneOnOnes] o
     WHERE o.[TeamMemberId] = tm.[Id] 
       AND o.[Status] = 1
       AND o.[IsDeleted] = 0
     ORDER BY o.[Date] DESC) AS LastCompletedMeetingDate,
    
    (SELECT COUNT(*) 
     FROM [dbo].[OneOnOnes] o 
     WHERE o.[TeamMemberId] = tm.[Id] 
       AND o.[Status] = 0 -- Scheduled
       AND o.[Date] >= GETDATE()
       AND o.[IsDeleted] = 0) AS UpcomingMeetings,
    
    -- Task metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[OwnerId] = tm.[Id] 
       AND t.[IsCompleted] = 0 
       AND t.[IsDeleted] = 0) AS OpenTasks,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[OwnerId] = tm.[Id] 
       AND t.[IsCompleted] = 0 
       AND t.[DueDate] < GETDATE()
       AND t.[IsDeleted] = 0) AS OverdueTasks,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[OwnerId] = tm.[Id] 
       AND t.[IsCompleted] = 1
       AND t.[IsDeleted] = 0) AS CompletedTasks,
    
    -- OKR metrics
    (SELECT COUNT(*) 
     FROM [dbo].[ObjectiveKeyResults] o 
     WHERE o.[OwnerId] = tm.[Id] 
       AND o.[EndDate] >= GETDATE()
       AND o.[IsDeleted] = 0) AS ActiveOkrs,
    
    -- Goal metrics
    (SELECT COUNT(*) 
     FROM [dbo].[IndividualGoals] g 
     WHERE g.[TeamMemberId] = tm.[Id] 
       AND g.[Status] <> 2 -- Not Completed
       AND g.[IsDeleted] = 0) AS ActiveGoals,
    
    -- Feedback metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Feedbacks] f 
     WHERE f.[TeamMemberId] = tm.[Id] 
       AND f.[IsDeleted] = 0) AS TotalFeedback,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Feedbacks] f 
     WHERE f.[TeamMemberId] = tm.[Id] 
       AND f.[Type] = 0 -- Positive
       AND f.[IsDeleted] = 0) AS PositiveFeedbackCount,
    
    -- Calculated fields
    CASE 
        WHEN tm.[LastOneOnOneDate] IS NULL THEN 1
        WHEN DATEDIFF(DAY, tm.[LastOneOnOneDate], GETDATE()) > tm.[OneOnOneCadence] THEN 1
        ELSE 0
    END AS IsOneOnOneOverdue,
    
    CASE 
        WHEN tm.[LastOneOnOneDate] IS NOT NULL 
        THEN DATEDIFF(DAY, tm.[LastOneOnOneDate], GETDATE())
        ELSE 9999
    END AS DaysSinceLastOneOnOne

FROM [dbo].[TeamMembers] tm
WHERE tm.[IsDeleted] = 0;
GO

-- =============================================================================
-- VIEW 2: OKR Progress Summary
-- Calculates completion percentage for each OKR based on Key Results
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_OkrProgress')
    DROP VIEW [dbo].[vw_OkrProgress];
GO

CREATE VIEW [dbo].[vw_OkrProgress]
AS
SELECT 
    okr.[ObjectiveId],
    okr.[UserId],
    okr.[OwnerId],
    okr.[Title],
    okr.[Description],
    okr.[StartDate],
    okr.[EndDate],
    okr.[TimePeriod],
    okr.[Year],
    okr.[Quarter],
    
    tm.[FirstName] + ' ' + tm.[LastName] AS OwnerName,
    
    -- Key Result aggregations
    (SELECT COUNT(*) 
     FROM [dbo].[KeyResults] kr 
     WHERE kr.[OkrId] = okr.[ObjectiveId] 
       AND kr.[IsDeleted] = 0) AS KeyResultCount,
    
    -- Weighted average progress
    (SELECT 
        CASE 
            WHEN SUM(kr.[Weight]) = 0 THEN 0
            ELSE CAST(SUM(
                CASE 
                    WHEN kr.[TargetValue] - kr.[StartingValue] = 0 THEN kr.[Weight] * 100
                    ELSE kr.[Weight] * (
                        (kr.[CurrentValue] - kr.[StartingValue]) / 
                        NULLIF((kr.[TargetValue] - kr.[StartingValue]), 0) * 100
                    )
                END
            ) / NULLIF(SUM(kr.[Weight]), 0) AS DECIMAL(5,2))
        END
     FROM [dbo].[KeyResults] kr 
     WHERE kr.[OkrId] = okr.[ObjectiveId] 
       AND kr.[IsDeleted] = 0) AS CompletionPercentage,
    
    -- Status based on progress and dates
    CASE 
        WHEN okr.[EndDate] < GETDATE() THEN 'Completed'
        WHEN (SELECT COUNT(*) FROM [dbo].[KeyResults] kr WHERE kr.[OkrId] = okr.[ObjectiveId] AND kr.[IsDeleted] = 0) = 0 THEN 'Not Started'
        WHEN (SELECT AVG(
                CAST((kr.[CurrentValue] - kr.[StartingValue]) AS FLOAT) / 
                NULLIF((kr.[TargetValue] - kr.[StartingValue]), 0) * 100
             ) 
             FROM [dbo].[KeyResults] kr 
             WHERE kr.[OkrId] = okr.[ObjectiveId] AND kr.[IsDeleted] = 0) >= 70 THEN 'On Track'
        WHEN (SELECT AVG(
                CAST((kr.[CurrentValue] - kr.[StartingValue]) AS FLOAT) / 
                NULLIF((kr.[TargetValue] - kr.[StartingValue]), 0) * 100
             ) 
             FROM [dbo].[KeyResults] kr 
             WHERE kr.[OkrId] = okr.[ObjectiveId] AND kr.[IsDeleted] = 0) >= 40 THEN 'At Risk'
        ELSE 'Behind'
    END AS Status,
    
    DATEDIFF(DAY, GETDATE(), okr.[EndDate]) AS DaysRemaining,
    
    CASE 
        WHEN okr.[EndDate] >= GETDATE() THEN 1
        ELSE 0
    END AS IsActive

FROM [dbo].[ObjectiveKeyResults] okr
LEFT JOIN [dbo].[TeamMembers] tm ON okr.[OwnerId] = tm.[Id]
WHERE okr.[IsDeleted] = 0;
GO

-- =============================================================================
-- VIEW 3: Project Status Dashboard
-- Aggregates project metrics including tasks, risks, and milestones
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ProjectDashboard')
    DROP VIEW [dbo].[vw_ProjectDashboard];
GO

CREATE VIEW [dbo].[vw_ProjectDashboard]
AS
SELECT 
    p.[ID] AS ProjectId,
    p.[UserId],
    p.[OwnerId],
    p.[Name],
    p.[Description],
    p.[Status],
    p.[StartDate],
    p.[EndDate],
    p.[Budget],
    p.[ActualCost],
    p.[PercentComplete],
    
    tm.[FirstName] + ' ' + tm.[LastName] AS OwnerName,
    
    -- Task metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[ProjectId] = p.[ID] 
       AND t.[IsDeleted] = 0) AS TotalTasks,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[ProjectId] = p.[ID] 
       AND t.[IsCompleted] = 1
       AND t.[IsDeleted] = 0) AS CompletedTasks,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] t 
     WHERE t.[ProjectId] = p.[ID] 
       AND t.[IsCompleted] = 0
       AND t.[DueDate] < GETDATE()
       AND t.[IsDeleted] = 0) AS OverdueTasks,
    
    -- Milestone metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Milestones] m 
     WHERE m.[ProjectId] = p.[ID] 
       AND m.[IsDeleted] = 0) AS TotalMilestones,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Milestones] m 
     WHERE m.[ProjectId] = p.[ID] 
       AND m.[IsCompleted] = 1
       AND m.[IsDeleted] = 0) AS CompletedMilestones,
    
    -- Risk metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Risks] r 
     WHERE r.[ProjectId] = p.[ID] 
       AND r.[Severity] >= 2 -- High or Critical
       AND r.[Status] <> 2 -- Not Mitigated
       AND r.[IsDeleted] = 0) AS HighRisks,
    
    -- Team size
    (SELECT COUNT(*) 
     FROM [dbo].[ProjectTeamMembers] ptm 
     WHERE ptm.[ProjectsID] = p.[ID]) AS TeamMemberCount,
    
    -- Calculated progress (if not manually set)
    CASE 
        WHEN p.[PercentComplete] IS NOT NULL THEN p.[PercentComplete]
        WHEN (SELECT COUNT(*) FROM [dbo].[Tasks] t WHERE t.[ProjectId] = p.[ID] AND t.[IsDeleted] = 0) = 0 THEN 0
        ELSE CAST(
            (SELECT COUNT(*) FROM [dbo].[Tasks] t WHERE t.[ProjectId] = p.[ID] AND t.[IsCompleted] = 1 AND t.[IsDeleted] = 0) * 100.0 /
            NULLIF((SELECT COUNT(*) FROM [dbo].[Tasks] t WHERE t.[ProjectId] = p.[ID] AND t.[IsDeleted] = 0), 0)
        AS DECIMAL(5,2))
    END AS CalculatedProgress,
    
    CASE 
        WHEN p.[EndDate] < GETDATE() AND p.[Status] NOT IN ('Completed', 'Done', 'Finished') THEN 1
        ELSE 0
    END AS IsOverdue,
    
    DATEDIFF(DAY, GETDATE(), p.[EndDate]) AS DaysRemaining

FROM [dbo].[Projects] p
LEFT JOIN [dbo].[TeamMembers] tm ON p.[OwnerId] = tm.[Id]
WHERE p.[IsDeleted] = 0;
GO

-- =============================================================================
-- VIEW 4: Task Overview
-- Comprehensive task information with owner details
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_TaskOverview')
    DROP VIEW [dbo].[vw_TaskOverview];
GO

CREATE VIEW [dbo].[vw_TaskOverview]
AS
SELECT 
    t.[Id] AS TaskId,
    t.[UserId],
    t.[OwnerId],
    t.[ProjectId],
    t.[ParentTaskId],
    t.[Description],
    t.[Notes],
    t.[DueDate],
    t.[CompletedDate],
    t.[IsCompleted],
    t.[Priority],
    t.[EstimatedHours],
    t.[ActualHours],
    t.[CreatedAt],
    
    tm.[FirstName] + ' ' + tm.[LastName] AS OwnerName,
    tm.[Email] AS OwnerEmail,
    
    p.[Name] AS ProjectName,
    p.[Status] AS ProjectStatus,
    
    -- Subtask metrics
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] st 
     WHERE st.[ParentTaskId] = t.[Id] 
       AND st.[IsDeleted] = 0) AS SubtaskCount,
    
    (SELECT COUNT(*) 
     FROM [dbo].[Tasks] st 
     WHERE st.[ParentTaskId] = t.[Id] 
       AND st.[IsCompleted] = 1
       AND st.[IsDeleted] = 0) AS CompletedSubtasks,
    
    CASE 
        WHEN t.[IsCompleted] = 1 THEN 'Completed'
        WHEN t.[DueDate] IS NULL THEN 'Open'
        WHEN t.[DueDate] < GETDATE() THEN 'Overdue'
        WHEN DATEDIFF(DAY, GETDATE(), t.[DueDate]) <= 3 THEN 'Due Soon'
        ELSE 'Open'
    END AS Status,
    
    CASE 
        WHEN t.[DueDate] IS NOT NULL AND t.[DueDate] < GETDATE() AND t.[IsCompleted] = 0 THEN 1
        ELSE 0
    END AS IsOverdue,
    
    CASE 
        WHEN t.[DueDate] IS NOT NULL 
        THEN DATEDIFF(DAY, GETDATE(), t.[DueDate])
        ELSE NULL
    END AS DaysUntilDue

FROM [dbo].[Tasks] t
LEFT JOIN [dbo].[TeamMembers] tm ON t.[OwnerId] = tm.[Id]
LEFT JOIN [dbo].[Projects] p ON t.[ProjectId] = p.[ID]
WHERE t.[IsDeleted] = 0;
GO

-- =============================================================================
-- VIEW 5: Upcoming One-on-Ones
-- Shows scheduled meetings with team member details
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_UpcomingOneOnOnes')
    DROP VIEW [dbo].[vw_UpcomingOneOnOnes];
GO

CREATE VIEW [dbo].[vw_UpcomingOneOnOnes]
AS
SELECT 
    o.[Id] AS OneOnOneId,
    o.[UserId],
    o.[TeamMemberId],
    o.[Date],
    o.[Duration],
    o.[Status],
    o.[Description],
    o.[HasGoogleCalendarEvent],
    o.[IsRecurring],
    
    tm.[FirstName] + ' ' + tm.[LastName] AS TeamMemberName,
    tm.[Email] AS TeamMemberEmail,
    tm.[JobTitle],
    
    -- Agenda item counts
    (SELECT COUNT(*) 
     FROM [dbo].[AgendaItems] a 
     WHERE a.[OneOnOneId] = o.[Id] 
       AND a.[IsDeleted] = 0) AS AgendaItemCount,
    
    (SELECT COUNT(*) 
     FROM [dbo].[AgendaItems] a 
     WHERE a.[OneOnOneId] = o.[Id] 
       AND a.[Category] = 1 -- Concerns
       AND a.[IsDeleted] = 0) AS ConcernCount,
    
    -- Task counts
    (SELECT COUNT(*) 
     FROM [dbo].[MeetingTasks] mt 
     WHERE mt.[OneOnOneId] = o.[Id] 
       AND mt.[IsDeleted] = 0) AS TaskCount,
    
    (SELECT COUNT(*) 
     FROM [dbo].[MeetingTasks] mt 
     WHERE mt.[OneOnOneId] = o.[Id] 
       AND mt.[IsCompleted] = 0
       AND mt.[IsDeleted] = 0) AS OpenTaskCount,
    
    DATEDIFF(DAY, GETDATE(), o.[Date]) AS DaysUntilMeeting,
    
    CASE 
        WHEN o.[Date] < GETDATE() AND o.[Status] = 0 THEN 1 -- Scheduled but in past
        ELSE 0
    END AS IsPastDue

FROM [dbo].[OneOnOnes] o
LEFT JOIN [dbo].[TeamMembers] tm ON o.[TeamMemberId] = tm.[Id]
WHERE o.[IsDeleted] = 0
  AND o.[Status] = 0 -- Scheduled
  AND o.[Date] >= CAST(GETDATE() AS DATE);
GO

-- =============================================================================
-- VIEW 6: KPI Dashboard
-- Shows current KPI values with status indicators
-- =============================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_KpiDashboard')
    DROP VIEW [dbo].[vw_KpiDashboard];
GO

CREATE VIEW [dbo].[vw_KpiDashboard]
AS
SELECT 
    kpi.[KpiId],
    kpi.[UserId],
    kpi.[OwnerId],
    kpi.[ParentKpiId],
    kpi.[Name],
    kpi.[Description],
    kpi.[Unit],
    kpi.[Category],
    kpi.[TargetValue],
    kpi.[CurrentValue],
    kpi.[ThresholdGreen],
    kpi.[ThresholdYellow],
    kpi.[IsComposite],
    
    tm.[FirstName] + ' ' + tm.[LastName] AS OwnerName,
    
    -- Status calculation
    CASE 
        WHEN kpi.[CurrentValue] IS NULL THEN 'No Data'
        WHEN kpi.[ThresholdGreen] IS NOT NULL AND kpi.[CurrentValue] >= kpi.[ThresholdGreen] THEN 'Green'
        WHEN kpi.[ThresholdYellow] IS NOT NULL AND kpi.[CurrentValue] >= kpi.[ThresholdYellow] THEN 'Yellow'
        ELSE 'Red'
    END AS Status,
    
    -- Percent of target
    CASE 
        WHEN kpi.[TargetValue] IS NOT NULL AND kpi.[TargetValue] <> 0
        THEN CAST((kpi.[CurrentValue] / NULLIF(kpi.[TargetValue], 0) * 100) AS DECIMAL(5,2))
        ELSE NULL
    END AS PercentOfTarget,
    
    -- Data source count
    (SELECT COUNT(*) 
     FROM [dbo].[KpiDataSources] ds 
     WHERE ds.[KpiId] = kpi.[KpiId] 
       AND ds.[IsDeleted] = 0) AS DataSourceCount

FROM [dbo].[KeyPerformanceIndicators] kpi
LEFT JOIN [dbo].[TeamMembers] tm ON kpi.[OwnerId] = tm.[Id]
WHERE kpi.[IsDeleted] = 0;
GO

PRINT 'Performance views created successfully!';
PRINT 'Views created: 6';
PRINT '  - vw_TeamMemberSummary: Aggregated team member metrics';
PRINT '  - vw_OkrProgress: OKR completion calculations';
PRINT '  - vw_ProjectDashboard: Project status with tasks/risks/milestones';
PRINT '  - vw_TaskOverview: Task details with owner information';
PRINT '  - vw_UpcomingOneOnOnes: Scheduled meetings with counts';
PRINT '  - vw_KpiDashboard: KPI values with status indicators';
GO
