# Tracker PostgreSQL Database Schema

**Generated:** January 5, 2026  
**Database:** PostgreSQL 18  
**Schema:** public  
**Total Tables:** 48 (47 EF Core + 1 auth)

---

## Table of Contents

1. [Common Patterns](#common-patterns)
2. [Core Tables](#core-tables)
3. [Team & User Management](#team--user-management)
4. [One-on-Ones & Meetings](#one-on-ones--meetings)
5. [Tasks & Projects](#tasks--projects)
6. [Goals & OKRs](#goals--okrs)
7. [KPIs & Metrics](#kpis--metrics)
8. [Feedback & Kudos](#feedback--kudos)
9. [Performance Reviews](#performance-reviews)
10. [Pulse Surveys](#pulse-surveys)
11. [Notes & Reminders](#notes--reminders)
12. [Calendar & Sync](#calendar--sync)
13. [Templates](#templates)
14. [Enums Reference](#enums-reference)
15. [Foreign Key Reference](#foreign-key-reference)

---

## Common Patterns

### Standard Audit Columns (ALL EF Core tables)

Every EF Core table has these columns. They are **ALL NOT NULL**:

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `CreatedAt` | timestamptz | NO | `NOW() AT TIME ZONE 'UTC'` | Auto-set |
| `CreatedBy` | varchar(100) | NO | - | Must provide |
| `LastModifiedAt` | timestamptz | NO | `NOW() AT TIME ZONE 'UTC'` | Auto-set |
| `LastModifiedBy` | varchar(100) | NO | - | Must provide |
| `RowVersion` | bytea | YES | - | Concurrency token |
| `IsDeleted` | boolean | NO | - | Soft delete flag |
| `DeletedAt` | timestamptz | YES | - | |
| `DeletedBy` | varchar(100) | YES | - | |

### Common Foreign Keys

- `UserId` → `Users.Id` (the logged-in user who owns/created the record)
- `OrganizationId` → `Organization.Id` (tenant/org scope, usually nullable)
- `TeamMemberId` → `TeamMembers.Id` (the team member this relates to)

---

## Core Tables

### Organization

The tenant/company table. Usually one per deployment.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | uuid | NO | **PK** |
| `Name` | text | NO | |
| `Slug` | text | YES | URL-friendly identifier |
| `IsActive` | boolean | NO | |
| `SubscriptionTier` | text | NO | e.g., 'Enterprise', 'Professional' |
| `MaxUsers` | integer | YES | |
| `MaxTeamMembers` | integer | YES | |
| + Audit columns | | | |

---

## Team & User Management

### Users (EF Core)

Application users (managers) who log in.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK**, auto-increment |
| `OrganizationId` | uuid | YES | FK → Organization |
| `SupabaseUserId` | uuid | YES | Link to Supabase auth |
| `Username` | varchar(200) | NO | |
| `Email` | varchar(200) | NO | |
| `DisplayName` | varchar(200) | NO | **Required** |
| `IsActive` | boolean | NO | |
| `IsAdmin` | boolean | NO | |
| `Role` | text | NO | e.g., 'Admin', 'Manager', 'User' |
| + Audit columns | | | |

### users (Auth - lowercase)

Supabase/local auth table for login credentials.

| Column | Type | Nullable | Default |
|--------|------|----------|---------|
| `id` | uuid | NO | `gen_random_uuid()` |
| `email` | text | NO | |
| `display_name` | text | YES | |
| `password_hash` | text | YES | |
| `created_at` | timestamp | YES | `now()` |
| `last_login_at` | timestamp | YES | |

### TeamMembers

People being managed (direct reports).

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `FirstName` | varchar(100) | NO | |
| `LastName` | varchar(100) | NO | |
| `NickName` | varchar(50) | NO | Can be empty string |
| `Email` | varchar(200) | NO | |
| `CellPhone` | varchar(20) | NO | Can be empty string |
| `JobTitle` | varchar(100) | NO | |
| `BirthDay` | timestamptz | NO | |
| `HireDate` | timestamptz | NO | |
| `TerminationDate` | timestamptz | NO | Use `0001-01-01` for active |
| `IsActive` | boolean | NO | |
| `ManagerId` | integer | NO | 0 if no manager |
| `ProfileImage` | bytea | NO | Use `\x` for empty |
| `LinkedInProfile` | varchar(500) | NO | Can be empty |
| `FacebookProfile` | varchar(500) | NO | Can be empty |
| `InstagramProfile` | varchar(500) | NO | Can be empty |
| `XProfile` | varchar(500) | NO | Can be empty |
| `Specialty` | integer | NO | **Enum** |
| `SkillLevel` | integer | NO | **Enum** |
| `Role` | integer | NO | **Enum** |
| `LastOneOnOneDate` | timestamptz | YES | |
| `OpenTaskCount` | integer | NO | Computed/cached |
| `ActiveGoalCount` | integer | NO | Computed/cached |
| `UpcomingMeetingCount` | integer | NO | Computed/cached |
| `NextOneOnOneDate` | timestamptz | YES | |
| `OrganizationId` | uuid | YES | FK → Organization |
| `UserId` | integer | NO | FK → Users (owner) |
| `UserId1` | integer | YES | Secondary user FK |
| + Audit columns | | | |

### ManagerHistory

Tracks manager changes over time.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | uuid | NO | **PK** |
| `OrganizationId` | uuid | NO | FK |
| `TeamMemberId` | integer | NO | FK → TeamMembers |
| `ManagerUserId` | integer | NO | |
| `ManagerSupabaseId` | uuid | YES | |
| `StartDate` | timestamptz | NO | |
| `EndDate` | timestamptz | YES | |
| `Reason` | text | YES | |
| `Notes` | text | YES | |
| `ManagerId` | integer | YES | FK → Users |
| + Audit columns | | | |

---

## One-on-Ones & Meetings

### OneOnOnes

The core meeting table.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `ManagerUserId` | integer | YES | |
| `Description` | varchar(500) | NO | |
| `TeamMemberId` | integer | NO | FK → TeamMembers |
| `Date` | timestamptz | NO | |
| `StartTime` | interval | NO | e.g., '14:00' |
| `EndTime` | interval | NO | e.g., '14:30' |
| `Duration` | interval | NO | e.g., '30 minutes' |
| `IsRecurring` | boolean | NO | |
| `Status` | integer | NO | **Enum** |
| `Agenda` | varchar(4000) | NO | Can be empty |
| `Notes` | varchar(4000) | NO | Can be empty |
| `Feedback` | varchar(4000) | NO | Can be empty |
| `GoogleCalendarEventId` | varchar(200) | YES | |
| `CalendarEventId` | varchar(200) | YES | |
| `TeamsMeetingUrl` | text | YES | |
| `TeamsMeetingId` | text | YES | |
| `GoogleMeetUrl` | text | YES | |
| `CalendarEventEtag` | text | YES | |
| `LastSyncedAt` | timestamptz | YES | |
| `SyncStatus` | text | NO | Can be empty |
| `IsSyncedToGoogle` | boolean | NO | |
| `ManagerId` | integer | YES | FK → Users |
| `UserId` | integer | NO | FK → Users (owner) |
| + Audit columns | | | |

### AgendaItems

Items on a meeting agenda.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Description` | varchar(1000) | NO | |
| `Category` | integer | NO | **Enum** |
| `Priority` | integer | NO | **Enum** |
| `Resolution` | varchar(2000) | NO | Can be empty |
| `IsCompleted` | boolean | NO | |
| `LinkedTaskId` | integer | YES | FK → MeetingTasks |
| `OneOnOneId` | integer | NO | FK → OneOnOnes |
| `UserId` | integer | NO | FK → Users |
| + Audit columns | | | |

### MeetingTasks

Tasks created from meetings.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Description` | varchar(1000) | NO | |
| `DueDate` | timestamptz | NO | |
| `IsCompleted` | boolean | NO | |
| `Notes` | varchar(2000) | NO | Can be empty |
| `OwnerId` | integer | NO | FK → TeamMembers |
| `OneOnOneId` | integer | NO | FK → OneOnOnes |
| `UserId` | integer | NO | FK → Users |
| + Audit columns | | | |

### LinkedItems

Links agenda items to other entities.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `AgendaItemId` | integer | NO | FK → AgendaItems |
| `Type` | integer | NO | **Enum** |
| `ItemId` | integer | NO | |
| `Title` | varchar(200) | NO | |

### OneOnOneLinkedTasks / OneOnOneLinkedOkrs / OneOnOneLinkedKpis

Junction tables for linking meetings to other entities.

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OneOnOneId` | integer | NO |
| `TaskId` / `OkrId` / `KpiId` | integer | NO |
| `DiscussionNotes` | varchar(2000) | NO |
| + Partial audit columns | | |

---

## Tasks & Projects

### Tasks

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Description` | varchar(1000) | NO | |
| `IsCompleted` | boolean | NO | |
| `DueDate` | timestamptz | NO | |
| `Notes` | varchar(2000) | NO | Can be empty |
| `OwnerId` | integer | NO | FK → TeamMembers |
| `ProjectId` | integer | YES | FK → Projects |
| `ParentTaskId` | integer | YES | FK → Tasks (subtasks) |
| `UserId` | integer | NO | FK → Users |
| + Audit columns | | | |

### Projects

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `ID` | integer | NO | **PK** (note: uppercase) |
| `OrganizationId` | uuid | YES | FK |
| `Name` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | Can be empty |
| `StartDate` | timestamptz | NO | |
| `EndDate` | timestamptz | YES | |
| `Status` | varchar(50) | NO | **String**, not enum! |
| `OwnerId` | integer | NO | FK → TeamMembers |
| `Budget` | numeric | NO | |
| `UserId` | integer | NO | FK → Users |
| + Audit columns | | | |

### Milestones

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `ID` | integer | NO | **PK** (uppercase) |
| `OrganizationId` | uuid | YES | FK |
| `ProjectId` | integer | NO | FK → Projects |
| `Name` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `TargetDate` | timestamptz | NO | |
| `IsAchieved` | boolean | NO | |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### Risks

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `ID` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `ProjectId` | integer | NO | FK → Projects |
| `Name` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `Severity` | integer | NO | **Enum** |
| `MitigationStrategy` | varchar(4000) | NO | |
| `IdentifiedDate` | timestamptz | YES | |
| `IsMitigated` | boolean | NO | |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### ProjectDependencies

| Column | Type | Nullable |
|--------|------|----------|
| `ID` | integer | NO |
| `OrganizationId` | uuid | YES |
| `Name` | varchar(200) | NO |
| `ProjectId` | integer | NO |
| `DependentProjectID` | integer | NO |
| `RequiredProjectID` | integer | NO |
| `Description` | varchar(2000) | NO |
| `ExpectedCompletionDate` | timestamptz | YES |
| `UserId` | integer | NO |
| + Audit columns | | |

### ProjectTeamMembers (Junction)

| Column | Type | Nullable |
|--------|------|----------|
| `ProjectID` | integer | NO |
| `TeamMembersId` | integer | NO |

### TaskCollections / TaskCollectionItems

Grouping tasks into collections.

---

## Goals & OKRs

### IndividualGoals

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `TeamMemberId` | integer | NO | FK → TeamMembers |
| `Title` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `Category` | integer | NO | **Enum** |
| `Status` | integer | NO | **Enum** |
| `TargetDate` | timestamptz | YES | |
| `ProgressPercent` | integer | NO | 0-100 |
| `Notes` | varchar(2000) | NO | Can be empty |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### GoalMilestones

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `GoalId` | integer | NO |
| `Description` | varchar(500) | NO |
| `IsCompleted` | boolean | NO |
| `CompletedDate` | timestamptz | YES |
| `SortOrder` | integer | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### ObjectiveKeyResults (OKRs)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `ObjectiveId` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Title` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `OwnerId` | integer | NO | FK → TeamMembers |
| `StartDate` | timestamptz | NO | |
| `EndDate` | timestamptz | NO | |
| `TimePeriod` | integer | NO | **Enum** |
| `Year` | integer | NO | |
| `ProjectId` | integer | YES | FK → Projects |
| `StatusOverride` | integer | YES | **Enum** |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### KeyResults

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `OkrId` | integer | NO | FK → ObjectiveKeyResults |
| `Title` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `TargetValue` | numeric | NO | |
| `CurrentValue` | numeric | NO | |
| `StartingValue` | numeric | NO | |
| `Unit` | varchar(50) | NO | |
| `Weight` | numeric | NO | Default 1.0 |
| `SortOrder` | integer | NO | |
| `TargetDirection` | integer | NO | **Enum** |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### KeyResultMeasurables

Links key results to data sources.

---

## KPIs & Metrics

### KeyPerformanceIndicators

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `KpiId` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Name` | varchar(200) | NO | |
| `Description` | varchar(2000) | NO | |
| `Value` | double | NO | Current value |
| `TargetValue` | double | NO | |
| `Unit` | varchar(50) | NO | |
| `Category` | varchar(100) | NO | **String** |
| `OwnerId` | integer | NO | FK → TeamMembers |
| `LastUpdated` | timestamptz | NO | |
| `TargetDirection` | integer | NO | **Enum** |
| `Frequency` | integer | NO | **Enum** |
| `IsComposite` | boolean | NO | |
| `ParentKpiId` | integer | YES | Self-FK |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### KpiDataSources

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `KpiId` | integer | NO |
| `SourceType` | integer | NO |
| `SourceId` | integer | YES |
| `AggregationType` | integer | NO |
| `Weight` | numeric | NO |
| `QueryCriteria` | varchar(2000) | YES |
| `SortOrder` | integer | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### ProgressSnapshots

Historical tracking.

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `EntityType` | varchar(20) | NO |
| `EntityId` | integer | NO |
| `SnapshotDate` | timestamptz | NO |
| `CurrentValue` | numeric | NO |
| `TargetValue` | numeric | NO |
| `Progress` | numeric | NO |
| `UserId` | integer | NO |
| `CreatedAt` | timestamptz | NO |

---

## Feedback & Kudos

### Feedbacks

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `TeamMemberId` | integer | NO | FK → TeamMembers |
| `Date` | timestamptz | NO | |
| `Type` | integer | NO | **Enum** (0=Positive, 1=Constructive) |
| `Title` | varchar(200) | NO | |
| `Content` | varchar(4000) | NO | |
| `Context` | varchar(500) | NO | |
| `OneOnOneId` | integer | YES | FK → OneOnOnes |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### Kudos

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `UserId` | integer | NO | FK → Users (sender) |
| `TeamMemberId` | integer | NO | FK → TeamMembers (recipient) |
| `Title` | varchar(200) | YES | |
| `Message` | varchar(2000) | NO | |
| `Category` | varchar(50) | NO | **String** |
| `LinkedTaskId` | integer | YES | FK |
| `LinkedOkrId` | integer | YES | FK |
| `LinkedMeetingId` | integer | YES | FK |
| `DeliveryChannel` | varchar(50) | NO | Can be empty |
| `DeliveryStatus` | varchar(50) | NO | Can be empty |
| `DeliveredAt` | timestamptz | YES | |
| `DeliveryError` | varchar(1000) | YES | |
| `ScheduledFor` | timestamptz | YES | |
| `IsPublic` | boolean | NO | |
| `MentionInMeetingPrep` | boolean | NO | |
| + Audit columns | | | |

---

## Performance Reviews

### ReviewTemplates

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `Name` | varchar(200) | NO |
| `Description` | varchar(2000) | NO |
| `ReviewType` | integer | NO |
| `IsDefault` | boolean | NO |
| `IsActive` | boolean | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### ReviewTemplateSections

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `ReviewTemplateId` | integer | NO |
| `Title` | varchar(200) | NO |
| `Description` | varchar(1000) | NO |
| `SortOrder` | integer | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### ReviewTemplateQuestions

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `ReviewTemplateSectionId` | integer | NO |
| `Text` | varchar(500) | NO |
| `QuestionType` | integer | NO |
| `SortOrder` | integer | NO |
| `IsRequired` | boolean | NO |
| `RatingMin` | integer | NO |
| `RatingMax` | integer | NO |
| `RatingLabels` | varchar(500) | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### PerformanceReviewCycles

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `Name` | varchar(200) | NO |
| `Description` | varchar(2000) | NO |
| `ReviewTemplateId` | integer | NO |
| `Status` | integer | NO |
| `SelfReviewStartDate` | timestamptz | YES |
| `SelfReviewDueDate` | timestamptz | YES |
| `ManagerReviewStartDate` | timestamptz | YES |
| `ManagerReviewDueDate` | timestamptz | YES |
| `CalibrationDate` | timestamptz | YES |
| `ShareDate` | timestamptz | YES |
| `UserId` | integer | NO |
| + Audit columns | | |

### PerformanceReviews

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `PerformanceReviewCycleId` | integer | NO |
| `TeamMemberId` | integer | NO |
| `Status` | integer | NO |
| `OverallRating` | integer | YES |
| `ManagerSummary` | varchar(4000) | NO |
| `SelfAssessmentSummary` | varchar(4000) | NO |
| `SelfReviewSubmittedAt` | timestamptz | YES |
| `ManagerReviewSubmittedAt` | timestamptz | YES |
| `SharedAt` | timestamptz | YES |
| `DiscussionDate` | timestamptz | YES |
| `OneOnOneId` | integer | YES |
| `UserId` | integer | NO |
| + Audit columns | | |

### PerformanceReviewSections / PerformanceReviewAnswers

Section and answer data for reviews.

---

## Pulse Surveys

### PulseSurveys

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `Title` | varchar(200) | NO |
| `Description` | varchar(2000) | NO |
| `Status` | integer | NO |
| `SentDate` | timestamptz | YES |
| `DueDate` | timestamptz | YES |
| `ClosedDate` | timestamptz | YES |
| `IsAnonymous` | boolean | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### PulseSurveyQuestions

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `PulseSurveyId` | integer | NO |
| `Text` | varchar(500) | NO |
| `QuestionType` | integer | NO |
| `SortOrder` | integer | NO |
| `RatingMin` | integer | NO |
| `RatingMax` | integer | NO |
| `RatingMinLabel` | varchar(100) | NO |
| `RatingMaxLabel` | varchar(100) | NO |
| `Category` | varchar(100) | NO |
| `IsRequired` | boolean | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### PulseSurveyResponses / PulseSurveyAnswers

Response and answer tables.

---

## Notes & Reminders

### QuickNotes

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Title` | varchar(200) | NO | |
| `Content` | varchar(4000) | NO | |
| `Category` | integer | NO | **Enum** |
| `LinkedEntityType` | integer | NO | Default 0 |
| `LinkedEntityId` | integer | YES | |
| `TeamMemberId` | integer | YES | FK |
| `ProjectId` | integer | YES | FK |
| `OneOnOneId` | integer | YES | FK |
| `IsPinned` | boolean | NO | |
| `IsArchived` | boolean | NO | |
| `Tags` | varchar(500) | NO | Comma-separated |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

### Reminders

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | integer | NO | **PK** |
| `OrganizationId` | uuid | YES | FK |
| `Type` | integer | NO | **Enum** |
| `Status` | integer | NO | **Enum** |
| `Title` | varchar(200) | NO | |
| `Message` | varchar(1000) | NO | |
| `DueDateTime` | timestamptz | NO | |
| `SnoozedUntil` | timestamptz | YES | |
| `OneOnOneId` | integer | YES | FK |
| `TeamMemberId` | integer | YES | FK |
| `TaskId` | integer | YES | FK |
| `GoalId` | integer | YES | FK |
| `IsRecurring` | boolean | NO | |
| `RecurrenceIntervalDays` | integer | YES | |
| `UserId` | integer | NO | FK |
| + Audit columns | | | |

---

## Calendar & Sync

### CalendarLinks

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `OneOnOneId` | integer | NO |
| `ProviderId` | varchar(20) | NO |
| `ExternalEventId` | varchar(500) | NO |
| `ETag` | varchar(500) | YES |
| `LastSyncedAt` | timestamptz | NO |
| `LastSyncDirection` | varchar(10) | NO |
| `Status` | varchar(20) | NO |
| `LastError` | varchar(2000) | YES |
| `UserId` | integer | NO |
| + Audit columns | | |

### CalendarSyncTokens

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `ProviderId` | varchar(20) | NO |
| `SyncToken` | varchar(2000) | NO |
| `UpdatedAt` | timestamptz | NO |
| `UserId` | integer | NO |

### ChangeTrackingEntries

For offline sync tracking.

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `EntityType` | varchar(100) | NO |
| `EntityId` | integer | NO |
| `ChangeType` | integer | NO |
| `EntityJson` | varchar(8000) | NO |
| `ChangedAt` | timestamptz | NO |
| `ChangedBy` | varchar(100) | NO |
| `IsSynced` | boolean | NO |
| `SyncedAt` | timestamptz | YES |
| `SyncError` | varchar(1000) | YES |

---

## Templates

### MeetingTemplates

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `OrganizationId` | uuid | YES |
| `Name` | varchar(100) | NO |
| `Description` | varchar(500) | NO |
| `SuggestedDurationMinutes` | integer | NO |
| `IsSystemTemplate` | boolean | NO |
| `SortOrder` | integer | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

### MeetingTemplateItems

| Column | Type | Nullable |
|--------|------|----------|
| `Id` | integer | NO |
| `MeetingTemplateId` | integer | NO |
| `Description` | varchar(500) | NO |
| `Category` | integer | NO |
| `Priority` | integer | NO |
| `SortOrder` | integer | NO |
| `UserId` | integer | NO |
| + Audit columns | | |

---

## Enums Reference

Many columns are stored as integers representing enum values. Here are the known patterns:

### Common Status Enums

| Value | Meaning |
|-------|---------|
| 0 | NotStarted / Pending / Draft |
| 1 | InProgress / Active |
| 2 | Completed / Closed |
| 3 | OnHold / Cancelled |

### Feedback.Type

| Value | Meaning |
|-------|---------|
| 0 | Positive |
| 1 | Constructive |

### Priority

| Value | Meaning |
|-------|---------|
| 0 | Low |
| 1 | Medium |
| 2 | High |
| 3 | Critical |

### TeamMember Skill Level

| Value | Meaning |
|-------|---------|
| 0 | Entry |
| 1 | Mid |
| 2 | Senior |
| 3 | Lead |

### TeamMember Role

| Value | Meaning |
|-------|---------|
| 0 | TeamMember |
| 1 | Manager |
| 2 | Admin |

### QuickNote Category / LinkedEntityType

| Value | Meaning |
|-------|---------|
| 0 | General / None |
| 1 | TeamMember |
| 2 | Project |
| 3 | OneOnOne |
| 4 | Task |
| 5 | Goal |

### OKR TimePeriod

| Value | Meaning |
|-------|---------|
| 0 | Q1 |
| 1 | Q2 |
| 2 | Q3 |
| 3 | Q4 |
| 4 | Annual |

### KPI TargetDirection

| Value | Meaning |
|-------|---------|
| 0 | Higher is better |
| 1 | Lower is better |
| 2 | Target value |

### KPI Frequency

| Value | Meaning |
|-------|---------|
| 0 | Daily |
| 1 | Weekly |
| 2 | Monthly |
| 3 | Quarterly |

---

## Foreign Key Reference

### Most Common FKs

| Column | References |
|--------|------------|
| `UserId` | Users.Id |
| `OrganizationId` | Organization.Id |
| `TeamMemberId` | TeamMembers.Id |
| `ProjectId` | Projects.ID |
| `OneOnOneId` | OneOnOnes.Id |
| `OwnerId` (on Tasks, Projects, etc.) | TeamMembers.Id |
| `ManagerId` (on OneOnOnes) | Users.Id |

### Full FK List

```
AgendaItems.LinkedTaskId → MeetingTasks.Id
AgendaItems.OneOnOneId → OneOnOnes.Id
AgendaItems.UserId → Users.Id
CalendarLinks.OneOnOneId → OneOnOnes.Id
CalendarLinks.UserId → Users.Id
CalendarSyncTokens.UserId → Users.Id
Feedbacks.OneOnOneId → OneOnOnes.Id
Feedbacks.TeamMemberId → TeamMembers.Id
Feedbacks.UserId → Users.Id
GoalMilestones.GoalId → IndividualGoals.Id
GoalMilestones.UserId → Users.Id
IndividualGoals.TeamMemberId → TeamMembers.Id
IndividualGoals.UserId → Users.Id
KeyPerformanceIndicators.OwnerId → TeamMembers.Id
KeyPerformanceIndicators.ParentKpiId → KeyPerformanceIndicators.KpiId
KeyPerformanceIndicators.UserId → Users.Id
KeyResultMeasurables.KeyResultId → KeyResults.Id
KeyResultMeasurables.UserId → Users.Id
KeyResults.OkrId → ObjectiveKeyResults.ObjectiveId
KeyResults.UserId → Users.Id
KpiDataSources.KpiId → KeyPerformanceIndicators.KpiId
KpiDataSources.UserId → Users.Id
Kudos.TeamMemberId → TeamMembers.Id
LinkedItems.AgendaItemId → AgendaItems.Id
ManagerHistory.ManagerId → Users.Id
ManagerHistory.OrganizationId → Organization.Id
ManagerHistory.TeamMemberId → TeamMembers.Id
MeetingTasks.OneOnOneId → OneOnOnes.Id
MeetingTasks.OwnerId → TeamMembers.Id
MeetingTasks.UserId → Users.Id
MeetingTemplateItems.MeetingTemplateId → MeetingTemplates.Id
MeetingTemplateItems.UserId → Users.Id
MeetingTemplates.UserId → Users.Id
Milestones.ProjectId → Projects.ID
Milestones.UserId → Users.Id
ObjectiveKeyResults.OwnerId → TeamMembers.Id
ObjectiveKeyResults.ProjectId → Projects.ID
ObjectiveKeyResults.UserId → Users.Id
OneOnOneLinkedKpis.KpiId → KeyPerformanceIndicators.KpiId
OneOnOneLinkedKpis.OneOnOneId → OneOnOnes.Id
OneOnOneLinkedOkrs.OkrId → ObjectiveKeyResults.ObjectiveId
OneOnOneLinkedOkrs.OneOnOneId → OneOnOnes.Id
OneOnOneLinkedTasks.OneOnOneId → OneOnOnes.Id
OneOnOneLinkedTasks.TaskId → Tasks.Id
OneOnOnes.ManagerId → Users.Id
OneOnOnes.OrganizationId → Organization.Id
OneOnOnes.TeamMemberId → TeamMembers.Id
OneOnOnes.UserId → Users.Id
PerformanceReviewAnswers.PerformanceReviewSectionId → PerformanceReviewSections.Id
PerformanceReviewAnswers.ReviewTemplateQuestionId → ReviewTemplateQuestions.Id
PerformanceReviewCycles.ReviewTemplateId → ReviewTemplates.Id
PerformanceReviewCycles.UserId → Users.Id
PerformanceReviewSections.PerformanceReviewId → PerformanceReviews.Id
PerformanceReviewSections.ReviewTemplateSectionId → ReviewTemplateSections.Id
PerformanceReviews.OneOnOneId → OneOnOnes.Id
PerformanceReviews.PerformanceReviewCycleId → PerformanceReviewCycles.Id
PerformanceReviews.TeamMemberId → TeamMembers.Id
PerformanceReviews.UserId → Users.Id
ProjectDependencies.DependentProjectID → Projects.ID
ProjectDependencies.ProjectId → Projects.ID
ProjectDependencies.RequiredProjectID → Projects.ID
ProjectDependencies.UserId → Users.Id
ProjectTeamMembers.ProjectID → Projects.ID
ProjectTeamMembers.TeamMembersId → TeamMembers.Id
Projects.OwnerId → TeamMembers.Id
Projects.UserId → Users.Id
PulseSurveyAnswers.PulseSurveyQuestionId → PulseSurveyQuestions.Id
PulseSurveyAnswers.PulseSurveyResponseId → PulseSurveyResponses.Id
PulseSurveyQuestions.PulseSurveyId → PulseSurveys.Id
PulseSurveyQuestions.UserId → Users.Id
PulseSurveyResponses.PulseSurveyId → PulseSurveys.Id
PulseSurveyResponses.TeamMemberId → TeamMembers.Id
PulseSurveyResponses.UserId → Users.Id
PulseSurveys.UserId → Users.Id
QuickNotes.OneOnOneId → OneOnOnes.Id
QuickNotes.ProjectId → Projects.ID
QuickNotes.TeamMemberId → TeamMembers.Id
QuickNotes.UserId → Users.Id
Reminders.GoalId → IndividualGoals.Id
Reminders.OneOnOneId → OneOnOnes.Id
Reminders.TaskId → Tasks.Id
Reminders.TeamMemberId → TeamMembers.Id
Reminders.UserId → Users.Id
ReviewTemplateQuestions.ReviewTemplateSectionId → ReviewTemplateSections.Id
ReviewTemplateQuestions.UserId → Users.Id
ReviewTemplateSections.ReviewTemplateId → ReviewTemplates.Id
ReviewTemplateSections.UserId → Users.Id
ReviewTemplates.UserId → Users.Id
Risks.ProjectId → Projects.ID
Risks.UserId → Users.Id
TaskCollectionItems.CollectionId → TaskCollections.Id
TaskCollectionItems.TaskId → Tasks.Id
TaskCollectionItems.UserId → Users.Id
TaskCollections.UserId → Users.Id
Tasks.OwnerId → TeamMembers.Id
Tasks.ParentTaskId → Tasks.Id
Tasks.ProjectId → Projects.ID
Tasks.UserId → Users.Id
TeamMembers.OrganizationId → Organization.Id
TeamMembers.UserId → Users.Id
TeamMembers.UserId1 → Users.Id
Users.OrganizationId → Organization.Id
```

---

## Important Notes for Seeding

1. **All audit columns are NOT NULL** - Must provide `CreatedBy`, `LastModifiedBy`, `IsDeleted`
2. **Enum columns are integers** - Not strings (except `Projects.Status`, `Kudos.Category`, `KPI.Category`)
3. **ProfileImage is bytea** - Use `'\x'` for empty
4. **Interval columns** - Use format like `'30 minutes'` or `'14:00'`
5. **TerminationDate** - Use `'0001-01-01'` for active employees
6. **Projects.ID uses uppercase** - Note `Projects.ID` not `Projects.Id`
7. **OneOnOnes.ManagerId** - References Users.Id, NOT TeamMembers.Id
8. **Empty strings for NOT NULL varchars** - Use `''` not NULL
