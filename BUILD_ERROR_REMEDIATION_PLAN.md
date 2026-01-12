# Build Error Remediation Plan

## Current State
- **Total Build Errors**: 1,482
- **Affected Files**: ~80 files

## Error Analysis Summary

### Error Types by Count
| Code | Count | Description |
|------|-------|-------------|
| CS1061 | 802 | Missing instance member (property/method) |
| CS0117 | 172 | Missing static member/definition |
| CS0103 | 166 | Undefined name/variable |
| CS0029 | 72 | Cannot convert type implicitly |
| CS0019 | 72 | Operator cannot be applied |
| CS0246 | 56 | Type/namespace not found |
| CS1503 | 42 | Argument type mismatch |
| CS0234 | 24 | Type not in namespace |
| CS0266 | 22 | Cannot convert (explicit cast needed) |
| CS0104 | 16 | Ambiguous reference |
| CS1501 | 14 | Wrong number of arguments |
| CS0023 | 12 | Operator cannot apply to operand |
| CS0452 | 6 | Type must be reference type |

### Top 15 Most Affected Files
| Errors | File |
|--------|------|
| 142 | CalendarSyncService.cs |
| 136 | TrackerMainViewModel.cs |
| 66 | KudosService.cs |
| 66 | GoalsViewModel.cs |
| 58 | DashboardViewModel.cs |
| 56 | ExcelExportService.cs |
| 54 | GoogleGmailService.cs |
| 44 | PredictiveAnalyticsService.cs |
| 32 | NewMetricViewModel.cs |
| 32 | GoogleCalendarService.cs |
| 30 | TeamMemberRepository.cs |
| 26 | GoalIndexer.cs |
| 26 | GoalProgressService.cs |
| 26 | FeedbackViewModel.cs |
| 24 | NewProjectViewModel.cs |

---

## Root Cause Categories

### Category 1: Legacy Types Removed (Must Create Compatibility Aliases or Delete Consumers)
These types no longer exist and were part of the old OKR/KPI model:
- `ObjectiveKeyResult` / `OneOnOne` / `IndividualTask` / `MeetingTask` / `KeyPerformanceIndicator`
- `KeyResultMeasurable` / `Measurable`
- `TrackerDbManager` (obsolete - replaced by `TrackerDataManager` + repositories)
- `KpiStatusEnum` (replaced by `MetricStatus`?)
- `OkrKpiGatherer` / `KpiGapAnalyzer`
- `SyncDirection` / `InsightSeverity.Warning`

### Category 2: Property/Field Renames on Data Models
These properties were renamed or removed as part of the migration to the new model:

#### Meeting Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `Date` | `ScheduledAt` or `StartAt` |
| `Duration` | Calculate from `StartAt` to `EndAt` |
| `StartTime` / `EndTime` | `StartAt` / `EndAt` |
| `TeamMember` | `Attendees` (collection) or `OrganizerTeamMember` |
| `TeamMemberName` | Via navigation |
| `UserId` | `OrganizationId` |
| `CalendarEventId` / `CalendarEventEtag` | Via `CalendarLinks` collection |
| `LinkedOkrs` / `LinkedKpis` / `LinkedTasks` | Via `LinkedGoals` / `LinkedTasks` collections |

#### Feedback Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `TeamMemberId` | `ToTeamMemberId` |
| `TeamMember` | `ToTeamMember` |
| `Date` | `CreatedAt` |
| `Title` | `Subject` or remove |
| `Type` | `FeedbackType` |
| `Context` | `Content` or `Notes` |

#### Goal/Strategic Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `ObjectiveId` | N/A (Goals are standalone) |
| `KeyResults` | Via `Targets` or `ChildGoals` |
| `GoalId` | `Id` |
| `Progress` | `CurrentValue` / `ProgressPercentage` |
| `CompletionPercentage` | `ProgressPercentage` |
| `CreatedByUser` | `Owner` navigation |
| `UserId` | `OrganizationId` |

#### Metric Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `KpiId` | N/A (Metrics standalone) |
| `MetricId` | `Id` |
| `Value` | `CurrentValue` |
| `PercentComplete` | `ProgressPercentage` |
| `LastUpdated` | `UpdatedAt` |
| `UserId` | `OrganizationId` |

#### Kudos Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `TeamMemberId` | `RecipientTeamMemberId` |
| `UserId` | `OrganizationId` |
| `Category` | `CategoryId` or enum |
| `CategoryDisplayName` | Computed/removed |
| `DeliveryStatus` / `DeliveryChannel` | Different structure |
| `DeliveredAt` | `SentAt` |
| `StatusDisplayName` | Computed |

#### Project Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `ID` | `Id` |
| `Progress` | `ProgressPercentage` |
| `Budget` | Removed or via custom field |
| `EndDate` | `DueDate` or `EndAt` |
| `DisplayValue` | Computed |

#### TrackerTask Model
| Old Property | New Property/Replacement |
|--------------|-------------------------|
| `OwnerName` | Via `Owner` navigation |
| `UserId` | `OrganizationId` |

### Category 3: OrganizationContext API Changes
- `OrganizationContext.Instance` → `OrganizationContext.Current`

### Category 4: TrackerDbContextFactory Ambiguity
- Two classes with same name: `Tracker.Classes.TrackerDbContextFactory` vs `Tracker.Database.TrackerDbContextFactory`
- Need to remove one or use fully qualified names

### Category 5: TrackerDataManager Missing Methods
These methods need to be added or consumers updated:
- `GetKPIs` / `KPIs` → Use `Metrics` instead
- `GetMeetings` / `Meetings` / `OneOnOnes` → Use `Meetings` 
- `Goals` → Use `StrategicGoals` or `DevelopmentGoals`
- `DeleteKPI` → `DeleteMetric`
- `DeleteOKR` → `DeleteStrategicGoal`
- `UpdateGoal` → Check method signature

### Category 6: Repository Changes
- ID types changed from `int` to `Guid`
- `UserId` filters replaced with `OrganizationId`
- Some navigation properties renamed

### Category 7: Enum Value Changes
| Enum | Missing Value | Replacement |
|------|---------------|-------------|
| `ReviewCycleStatus.Active` | ? |
| `InsightSeverity.Warning` | Check if exists |
| `InsightType.KpiOffTarget` | `MetricOffTarget`? |
| `InsightType.KudosSuggestion` | Check |
| `MetricFrequency.OnDemand` | Check |
| `PrepItemLinkType.Goal` | Check |
| `SnapshotEntityType.KeyResult/KPI/OKR` | Remove or rename |
| `SurveyStatus.Sent` | Check |
| `PropertyChangedEnum.StrategicGoals` | Check |
| `DataChangeType.StrategicGoals` | Check |

---

## Remediation Strategy

### Phase 1: Infrastructure & Quick Wins (Unblocks ~200 errors)
**Priority: HIGH - Do first as it unblocks other phases**

1. **Fix OrganizationContext.Instance → Current** (~16 occurrences)
   - Files: SurveyDataGatherer, SurveySentimentAnalyzer, SurveySyncService, TaskDataGatherer
   
2. **Fix TrackerDbContextFactory ambiguity** (~16 occurrences)
   - Delete `Tracker.Classes.TrackerDbContextFactory` or merge into Database version
   
3. **Add missing enum values** (~50 occurrences)
   - Add `Warning` to `InsightSeverity` if missing
   - Add missing values to other enums

### Phase 2: Repository Layer Fixes (~150 errors)
**Priority: HIGH - Required for data access**

1. **GoalRepository.cs** - Fix `CreatedByUser` → navigation property
2. **MetricRepository.cs** - Remove `UserId` references
3. **KudosRepository.cs** - Remove `UserId` references
4. **TeamMemberRepository.cs** - Fix DbContext.OneOnOnes, Tasks references
5. **TargetRepository.cs** - Fix `UserId`, `Weight`, `SortOrder` on TargetMeasurable
6. **ReviewCycleRepository.cs** - Fix `UserId`, int/Guid conversions
7. **ReviewTemplateRepository.cs** - Fix `UserId`, int/Guid conversions
8. **PulseSurveyRepository.cs** - Fix `UserId` references
9. **DevelopmentGoalRepository.cs** - Fix `UserId` references

### Phase 3: Data Model Property Mapping (~400 errors)
**Priority: MEDIUM - Core model alignment**

1. **Meeting model consumers** - Update all `Date`, `Duration`, `TeamMember` references
2. **Feedback model consumers** - Update `TeamMemberId`, `Date`, `Title`, `Type` references
3. **Goal model consumers** - Update `ObjectiveId`, `KeyResults`, `Progress` references
4. **Metric model consumers** - Update `KpiId`, `Value`, `PercentComplete` references
5. **Kudos model consumers** - Update property references
6. **Project model consumers** - Update `ID`, `Progress`, `Budget` references

### Phase 4: TrackerDataManager API Updates (~100 errors)
**Priority: MEDIUM**

1. Add/alias missing methods or update consumers:
   - `GetKPIs` / `KPIs` → `GetMetrics` / `Metrics`
   - `GetMeetings` → exists? Add if needed
   - `Goals` → `StrategicGoals` or `DevelopmentGoals`
   - `DeleteKPI` → `DeleteMetric`
   - `DeleteOKR` → `DeleteStrategicGoal`

### Phase 5: View/ViewModel Layer (~500 errors)
**Priority: MEDIUM - UI layer**

Focus files by error count:
1. TrackerMainViewModel.cs (136 errors)
2. GoalsViewModel.cs (66 errors)
3. DashboardViewModel.cs (58 errors)
4. NewMetricViewModel.cs (32 errors)
5. FeedbackViewModel.cs (26 errors)
6. NewProjectViewModel.cs (24 errors)

### Phase 6: Service Layer (~400 errors)
**Priority: MEDIUM**

Focus files by error count:
1. CalendarSyncService.cs (142 errors)
2. KudosService.cs (66 errors)
3. ExcelExportService.cs (56 errors)
4. GoogleGmailService.cs (54 errors)
5. PredictiveAnalyticsService.cs (44 errors)
6. GoogleCalendarService.cs (32 errors)
7. SearchService.cs (20 errors)
8. ReminderService.cs (20 errors)

### Phase 7: Delete Obsolete Code
**Priority: LOW - Clean up after fixes**

Consider deleting files that reference heavily deprecated types if not needed:
- Files referencing `ObjectiveKeyResult`, `KeyPerformanceIndicator`, `OneOnOne`
- `OkrKpiGatherer`, `KpiGapAnalyzer` if obsolete
- Any `TrackerDbManager` references (if fully replaced)

---

## Execution Approach

### Per-File Strategy
For each file:
1. Identify all error types in that file
2. Check if the file should be deleted (uses only obsolete types)
3. If keeping: apply property renames, type changes, API updates
4. Build and verify errors resolved

### Batch Strategy for Common Patterns
Use multi-replace for:
- `OrganizationContext.Instance` → `OrganizationContext.Current`
- `.UserId` → `.OrganizationId` (where appropriate)
- `Project.ID` → `Project.Id`
- `Meeting.Date` → `Meeting.ScheduledAt`
- `Meeting.TeamMember` → `Meeting.OrganizerTeamMember` or `Meeting.Attendees.First()`
- `Feedback.TeamMemberId` → `Feedback.ToTeamMemberId`
- `Feedback.Date` → `Feedback.CreatedAt`

---

## Next Steps

1. **User confirms approach**
2. **Phase 1**: Fix OrganizationContext + TrackerDbContextFactory + enums
3. **Phase 2**: Fix all repository files
4. **Phase 3-6**: Work through files by error count, highest first
5. **Phase 7**: Delete dead code

**Estimated effort**: Each phase may take 15-30 minutes depending on complexity.

---

## Files to Consider Deleting (if obsolete)

If these features are no longer used:
- `OkrKpiGatherer.cs`
- `KpiGapAnalyzer.cs`
- Any files that ONLY work with `ObjectiveKeyResult`, `KeyPerformanceIndicator`, `OneOnOne`
- `TrackerDbManager.cs` (if fully replaced)
