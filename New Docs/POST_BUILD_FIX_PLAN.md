# Post-Build Fix Plan - Phase 4

**Date:** January 15, 2026  
**Status:** BUILD SUCCESSFUL ✅ (0 errors, 0 warnings)  
**Previous State:** 1,482 errors → 0 errors  
**Next Focus:** Runtime stability, testing, documentation, then Avalonia migration

---

## 🎯 Executive Summary

The Dapper migration Phase 3B is **COMPLETE**. The project now compiles cleanly with zero errors and zero warnings. The remaining work falls into these categories:

1. **Runtime Testing & Validation** - Ensure the app actually works
2. **Repository Coverage Completion** - Missing repository methods
3. **Service Layer Cleanup** - Remove dead code, consolidate patterns
4. **Documentation Sync** - Update docs to match reality
5. **Test Infrastructure** - Create unit tests (none exist currently)
6. **XAML/UI Work** - LOW PRIORITY (Avalonia migration planned)

---

## 📋 Priority 1: Runtime Testing & Validation

**Why First:** Code compiles but may crash at runtime due to:
- Null reference issues (we fixed compile-time, not runtime)
- Repository methods returning wrong data
- Missing data mappings

### Tasks

| # | Task | File/Area | Risk | Est. Time |
|---|------|-----------|------|-----------|
| 1.1 | Launch app and test login flow | AuthenticationManager | HIGH | 30 min |
| 1.2 | Test dashboard data loading | DashboardViewModel, TrackerDataManager | HIGH | 30 min |
| 1.3 | Test team member CRUD | TeamMemberRepository, TeamMemberViewModel | HIGH | 30 min |
| 1.4 | Test meeting CRUD | MeetingRepository, MeetingViewModel | HIGH | 30 min |
| 1.5 | Test goal CRUD | GoalRepository, GoalViewModel | MEDIUM | 30 min |
| 1.6 | Test task CRUD | TaskRepository, TasksViewModel | MEDIUM | 30 min |
| 1.7 | Test metric CRUD | MetricRepository, MetricViewModel | MEDIUM | 30 min |
| 1.8 | Test feedback CRUD | FeedbackRepository, FeedbackViewModel | LOW | 20 min |
| 1.9 | Test quick notes | QuickNoteRepository, QuickNotesViewModel | LOW | 20 min |
| 1.10 | Test pulse surveys | PulseSurveyRepository, PulseSurveysViewModel | LOW | 20 min |

**Total Estimated Time:** ~5 hours of manual testing

---

## 📋 Priority 2: Repository Method Completion

**Why Second:** Some repository methods may be stubs or incomplete.

### Review These Repositories

| Repository | Status | Missing Methods | Notes |
|------------|--------|-----------------|-------|
| MeetingRepository | ✅ | FindMeetingByCalendarEventIdAsync (simplified) | Uses 1 arg now |
| KudosRepository | ✅ | GetRecentKudosAsync (simplified) | Uses days param |
| QuickNoteRepository | ✅ | GetQuickNotesAsync (no args) | Verified |
| TeamMemberRepository | ⚠️ | Check GetTeamMembersWithoutRecentOneOnOneAsync | May need testing |
| GoalRepository | ⚠️ | Check progress calculation methods | May need testing |
| TargetRepository | ⚠️ | Check target-measurable relationships | May need testing |

### Action Items

1. Audit each repository for stub implementations
2. Add missing SQL queries
3. Test with real Supabase data

---

## 📋 Priority 3: Service Layer Cleanup

**Why Third:** Dead code and inconsistent patterns remain.

### Services to Review

| Service | Issue | Action |
|---------|-------|--------|
| CalendarSyncService | Simplified sync methods | Verify calendar integration works |
| KudosService | Fixed return types | Test kudos delivery |
| ReminderService | Uses new repository pattern | Test reminders fire |
| SurveySyncService | Fixed Guid/int conversion | Test survey sync |
| MeetingPrepService | Removed TrackerDbContext | Test meeting prep generation |

### Dead Code Removal Candidates

```
Services to potentially delete or consolidate:
- TrackerDbManager (if fully deprecated)
- Any EF Core specific code
- Legacy OKR/KPI specific services that weren't migrated
```

---

## 📋 Priority 4: Documentation Sync

**Why Fourth:** Docs are outdated after the massive refactor.

### Documentation Tasks

| Doc | Status | Action |
|-----|--------|--------|
| BUILD_ERROR_REMEDIATION_PLAN.md | ❌ Outdated | Archive or delete - errors are fixed |
| DAPPER_MIGRATION_STATUS.md | ⚠️ Outdated | Update to show Phase 3B complete |
| New Docs/Dapper/*.md | ⚠️ Review | Verify architecture docs match code |
| copilot-instructions.md | ✅ Current | No changes needed |
| MASTER_REFACTOR_PLAN.md | ⚠️ Outdated | Update execution status |

### New Docs Needed

1. **RUNTIME_TESTING_CHECKLIST.md** - Manual test cases
2. **AVALONIA_MIGRATION_PLAN.md** - Future UI framework migration

---

## 📋 Priority 5: Test Infrastructure

**Why Fifth:** No automated tests exist. Critical for preventing regressions.

### Test Project Setup

```
Tests/
├── Tracker.Tests/
│   ├── Repositories/
│   │   ├── TeamMemberRepositoryTests.cs
│   │   ├── MeetingRepositoryTests.cs
│   │   ├── GoalRepositoryTests.cs
│   │   └── ...
│   ├── Services/
│   │   ├── KudosServiceTests.cs
│   │   └── ...
│   └── ViewModels/
│       └── ...
```

### Test Categories

| Category | Priority | Coverage Target |
|----------|----------|-----------------|
| Repository unit tests | HIGH | 80% |
| Service integration tests | MEDIUM | 60% |
| ViewModel tests | LOW | 40% |

---

## 📋 Priority 6: XAML/UI Work (LOW - Avalonia Migration Pending)

**Why Last:** Planning to migrate from WPF to Avalonia for cross-platform support. Current WPF XAML fixes are low value.

### Current XAML State

- ✅ All XAML compiles without errors
- ✅ Converters updated (GoalStatusToColorConverter, etc.)
- ✅ Namespaces fixed (dtos:PrepItem, etc.)

### DO NOT Invest Time In

- WPF-specific styling improvements
- Complex WPF control customization
- WPF animation work
- Performance optimization of WPF rendering

### Pre-Avalonia Prep Work (Optional)

| Task | Benefit |
|------|---------|
| Extract view logic to ViewModels | Easier Avalonia port |
| Minimize code-behind | Avalonia uses similar MVVM |
| Document custom controls | Know what needs recreation |
| List WPF-specific dependencies | Identify migration blockers |

---

## 📊 Summary & Recommended Execution Order

```
Week 1:
├── Day 1-2: Runtime Testing (Priority 1)
│   └── Fix any runtime crashes discovered
├── Day 3: Repository Audit (Priority 2)
│   └── Add missing implementations
└── Day 4-5: Service Cleanup (Priority 3)
    └── Remove dead code

Week 2:
├── Day 1: Documentation Update (Priority 4)
├── Day 2-3: Test Infrastructure Setup (Priority 5)
└── Day 4-5: Initial Unit Tests

Week 3+:
├── Continue test coverage
├── Plan Avalonia migration
└── XAML work only if blocking features (Priority 6)
```

---

## 🏁 Success Criteria

| Milestone | Criteria |
|-----------|----------|
| Phase 4A Complete | App launches, all CRUD operations work |
| Phase 4B Complete | 50% repository test coverage |
| Phase 4C Complete | Documentation current, dead code removed |
| Ready for Avalonia | All business logic in ViewModels/Services, minimal code-behind |

---

## Files Modified in Phase 3B (Reference)

<details>
<summary>Click to expand - 50+ files touched</summary>

### Models
- Target.cs (TargetDirection fix)
- Kudos.cs (CompanyValues, Category, CategoryDisplayName)
- Meeting.cs (TeamsMeetingUrl, GoogleMeetUrl aliases)
- Goal.cs (Progress alias)
- AgendaItem.cs (IsDeleted, Description, Category)
- QuickNote.cs (Tags, TeamMemberId, LinkedEntityId)
- PulseSurvey.cs (RatingMax, SubmittedAt)
- Insight.cs (Details, RecommendedActions, SourceEntities)

### Services
- ReminderService.cs (logging fixes)
- KudosService.cs (logging, return types)
- CalendarSyncService.cs (method signatures)
- SurveySyncService.cs (type fixes)
- All MeetingPrep/Gatherers/*.cs (logging, type fixes)
- All AI/Insights/Analyzers/*.cs (logging, type fixes)
- SlackDeliveryProvider.cs (enum handling)

### ViewModels
- TeamMemberViewModel.cs (Task types, void assignments)
- QuickNotesViewModel.cs (Guid comparisons, void assignments)
- PulseSurveysViewModel.cs (Survey type consolidation)
- SetupWizardViewModel.cs (TrackerDbManager removal)
- SendKudosViewModel.cs (using statements)
- GoalViewModel.cs (nullable handling)
- MeasurableViewModel.cs (enum to string)
- ReportsViewModel.cs (type conversions)

### XAML
- OkrCard.xaml (converter rename)
- OkrsControl.xaml (converter rename)
- KpisControl.xaml (enum rename)
- MeetingPrepPanel.xaml (namespace fix)

</details>

---

*Last Updated: January 15, 2026*
