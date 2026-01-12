# DAPPER MIGRATION - MASTER STATUS & HANDOFF DOCUMENT

**Date:** January 12, 2026  
**Status:** Phase 0 COMPLETE - Ready for Phase 1  
**Total Effort:** 7-10 days remaining (Phases 1-5)  
**Architecture:** FULL MIGRATION (no hybrid)

---

## 🎯 CURRENT STATE

### ✅ Phase 0: Complete
- `IRepository.cs` - Generic CRUD interface (100 lines)
- `BaseRepository.cs` - Base implementation (400 lines, manageable)
- `DapperConnectionFactory.cs` - PostgreSQL connection (70 lines)
- `IUnitOfWork.cs` - Transaction interface (50 lines)
- `UserRepository.cs` - First concrete repository (180 lines, template)
- `ServiceConfiguration.cs` - DI registration (updated)
- `Tracker.csproj` - Dapper + Configuration NuGet added

**Files Created:** 5 new  
**Files Modified:** 2 (ServiceConfiguration.cs, Tracker.csproj)  
**Files Temporarily Stubbed:** 1 (AuthenticationManager.cs - ~50 lines of EF code commented)  

### ⏳ Phase 1-5: Not Started
- All remaining work ahead

---

## 📋 REPOSITORY SEGREGATION STRATEGY

### GOLD STANDARD: Must Create (12 repositories)

**TIER 1 - CRITICAL (6 repositories):**
These are the highest-impact, most-used entities.

| Repository | Entity | Key Methods | Est. Lines | Dependencies |
|------------|--------|-------------|-----------|--------------|
| UserRepository | User | GetBySupabaseId, GetByEmail, GetByOrganization, EmailExists | 180 | None |
| TeamMemberRepository | TeamMember | GetByOrganization, GetByManager, GetActive, GetByUserId | 200 | User |
| MeetingRepository | Meeting | GetByTeamMember, GetByDateRange, GetUpcoming, GetByStatus | 220 | TeamMember |
| TaskRepository | TrackerTask | GetByOwner, GetByProject, GetByGoal, GetByStatus, CompleteTask | 240 | TeamMember, Goal |
| GoalRepository | Goal | GetByOwner, GetByOrganization, GetByStatus, CalculateProgress | 200 | TeamMember |
| MetricRepository | Metric | GetByOwner, GetByOrganization, RecordValue, GetHistory | 200 | TeamMember |

**Subtotal:** 6 repos, ~1,240 lines total

**TIER 2 - IMPORTANT (6 repositories):**
Secondary entities, but necessary for complete data access.

| Repository | Entity | Key Methods | Est. Lines | Dependencies |
|------------|--------|-------------|-----------|--------------|
| FeedbackRepository | Feedback | GetForTeamMember, GetFromTeamMember, GetStats | 150 | TeamMember |
| ProjectRepository | Project | GetByOrganization, GetByOwner, GetTeamMembers | 140 | Organization, TeamMember |
| QuickNoteRepository | QuickNote | GetByTeamMember, GetByMeeting, SearchNotes | 140 | TeamMember, Meeting |
| DevelopmentGoalRepository | DevelopmentGoal | GetByTeamMember, GetByStatus, GetMilestones | 160 | TeamMember |
| PerformanceReviewRepository | PerformanceReview | GetByTeamMember, GetByReviewCycle, GetStatus | 160 | TeamMember |
| PulseSurveyRepository | PulseSurvey | GetByOrganization, GetByTeamMember, GetResponses | 180 | TeamMember |

**Subtotal:** 6 repos, ~930 lines total

**GRAND TOTAL:** 12 repositories, ~2,170 lines spread across 12 files = **~180 lines per file (all manageable)**

---

## 🗑️ DO NOT CREATE (48 tables)

These should NEVER have repositories (use raw SQL when needed):

**Infrastructure (Never Model):**
- activity_log - Query via raw SQL for audits
- user_sessions - Session management, no repository needed
- user_roles, roles - Authorization layer, handle at auth level
- notification_preferences - User settings, query directly
- announcement_reads - Tracking only, query directly

**Analytics (Raw SQL Only):**
- organization_snapshots - Historical, use SELECT only
- team_member_snapshots - Historical, use SELECT only
- team_snapshots - Historical, use SELECT only
- progress_snapshots - Historical, use SELECT only
- metric_history - Historical, use SELECT only

**AI & Embeddings (Not now):**
- vector_embeddings - AI features not in scope
- ai_conversations - AI features not in scope
- ai_insights - AI features not in scope

**Junctions (Don't model separately):**
- team_memberships - Query via TeamMemberRepository
- project_members - Query via ProjectRepository
- meeting_attendees - Query via MeetingRepository
- survey_responses, survey_answers - Query via PulseSurveyRepository

**Settings & Preferences (Handle via services):**
- reminder_preferences - Service-level handling
- calendar_links - Service-level handling
- announcement_reads - No repository
- manager_history - Historical, no repository
- review_cycles - Part of PerformanceReviewRepository

**Result:** Only 12 repositories. Clean. Manageable. Done.

---

## 🏗️ CODE FILE SIZE LIMITS (EDICT)

**Maximum:** 300 lines per repository file (including XML docs, spacing)  
**Rationale:** 
- Easy to read in VS Code without scrolling excessively
- Fast to understand, modify, test
- Single responsibility maintained

**Actual Expected Sizes:**
- UserRepository: 180 lines ✓
- TeamMemberRepository: 200 lines ✓
- MeetingRepository: 220 lines ✓
- TaskRepository: 240 lines (might hit 250) ⚠️ Watch this one
- GoalRepository: 200 lines ✓
- MetricRepository: 200 lines ✓

**If a file approaches 300 lines:**
1. Extract specialized queries to a separate `*QueryHelper.cs` file
2. Move complex aggregations to a separate `*Analytics.cs` file
3. Example: MeetingRepository (250 lines) + MeetingQueryHelper (50 lines specialized queries)

**BaseRepository.cs is exception:** 400 lines is acceptable because:
- It's used by ALL repositories (shared foundation)
- Fully documented
- Split into logical sections (#region marks)
- Never touched after creation

---

## 📅 PHASE BREAKDOWN

### Phase 1: Core Repositories (48 hours / 2 days)
**Goal:** Create 6 Tier-1 repositories

**Step 1.1 (6 hours):** UserRepository + TeamMemberRepository
- UserRepository: Template already exists (UserRepository.cs)
- TeamMemberRepository: Similar complexity
- Build + test both

**Step 1.2 (6 hours):** MeetingRepository + TaskRepository  
- More complex (joins, aggregations)
- MeetingRepository has date range queries
- TaskRepository has status filtering

**Step 1.3 (6 hours):** GoalRepository + MetricRepository
- Similar complexity to Task
- Progress calculations
- History queries

**Checkpoint:** Build clean, all 6 repos compile, DI registered

---

### Phase 2: Supporting Repositories (30 hours / 1.5 days)
**Goal:** Create 6 Tier-2 repositories

**Step 2.1 (6 hours):** FeedbackRepository + ProjectRepository
**Step 2.2 (6 hours):** QuickNoteRepository + DevelopmentGoalRepository
**Step 2.3 (6 hours):** PerformanceReviewRepository + PulseSurveyRepository

**Checkpoint:** All 12 repositories done, fully registered in DI

---

### Phase 3: ViewModel Migration (40 hours / 2.5 days)
**Goal:** Update ALL ViewModels to use repositories

**Not starting until Phase 1+2 complete.**

**High-error ViewModels first (identify via build output):**
- OneOnOneViewModel
- MeasurableViewModel  
- InsightPanelViewModel
- (others as identified)

**Pattern:**
```csharp
// OLD
var context = TrackerDbManager.Instance.GetContext();
var users = context.Users.Where(...).ToList();

// NEW
var users = await _userRepository.GetWhereSqlAsync("...", params);
```

---

### Phase 4: Delete EF Code (8 hours)
**Goal:** Complete removal of Entity Framework

- Delete TrackerDbContext.cs
- Remove all EF migrations
- Remove EntityFrameworkCore packages
- Remove all `using Tracker.Database` statements
- Stub out remaining EF references

---

### Phase 5: Ship & Verify (4 hours)
**Goal:** App runs, all tests pass

- Manual testing
- Performance validation
- Documentation update

---

## 🚨 WHEN TO STOP & HANDOFF

**If any of these occur during execution:**

1. **Build breaks unexpectedly** - Document current state, handoff to new thread
2. **A repository exceeds 300 lines** - Stop, document design issue, handoff
3. **ViewModel migrations reveal major architectural problem** - Stop, document, handoff
4. **Thread context becomes too long** - Stop before context fills, handoff
5. **More than 30 minutes debugging a single issue** - Stop, document, handoff

**Handoff Template:**
```
## HANDOFF - [Issue/Reason]
**Current Status:** [What's done, what's in progress]
**Problem:** [What went wrong or got complex]
**Files Affected:** [List of touched files]
**Next Steps:** [What needs to happen next]
**Context:** [Key decisions/learnings for next person]
```

---

## 📍 KEY DOCUMENTS (Single Place)

**This File:**
- `/New Docs/DAPPER_MIGRATION_STATUS.md` (THIS FILE)
- One-stop reference for entire migration

**Phase 0 Documentation:**
- `/New Docs/DAPPER_MIGRATION_PLAN.md` - Original plan, kept for reference

**Schema Reference:**
- `/New Docs/SupaBase SQL Scrips/SCHEMA_DIAGRAM_REFERENCE.md` - Table relationships

**Infrastructure Code:**
- `/Services/Data/` - All Dapper infrastructure
  - `IRepository.cs`
  - `BaseRepository.cs`
  - `DapperConnectionFactory.cs`
  - `IUnitOfWork.cs`
  - `/Repositories/` - All concrete repositories

**DI Registration:**
- `/Infrastructure/ServiceConfiguration.cs` - Where repositories are registered

---

## 🎯 CLEAR OBJECTIVES BY PHASE END

### Phase 0 ✅
- [x] Foundation built
- [x] Build passes
- [x] UserRepository template exists
- [x] DI configured

### Phase 1 (Next)
- [ ] 6 core repositories fully implemented
- [ ] All Tier-1 queries working
- [ ] Build passes
- [ ] No compile errors for repositories
- [ ] Ready for Phase 2

### Phase 2
- [ ] 12 repositories complete
- [ ] All data access patterns established
- [ ] Build passes
- [ ] Ready for ViewModel migration

### Phase 3
- [ ] All ViewModels use repositories
- [ ] No DbContext references in ViewModels
- [ ] Services updated
- [ ] ~70% of build errors resolved

### Phase 4
- [ ] TrackerDbContext deleted
- [ ] EF migrations removed
- [ ] All EF packages removed
- [ ] Build errors ~90% resolved

### Phase 5
- [ ] 0 build errors
- [ ] App runs
- [ ] Gold standard: Only 12 models, clean architecture

---

## ⚠️ RULES GOING FORWARD

1. **No file > 300 lines** (BaseRepository exception)
2. **One repository per file** (no mixing)
3. **All repositories inherit from BaseRepository<T>**
4. **All repositories have interface (IXxxRepository)**
5. **DI registration happens in ServiceConfiguration.cs immediately**
6. **Build must pass after each Phase step**
7. **No ViewModels updated until Phase 3**
8. **No EF code deleted until Phase 4**

---

## 🚀 READY TO PROCEED?

**Next action:** Start Phase 1, Step 1.1 (UserRepository + TeamMemberRepository)

**Time estimate:** 6 hours  
**Expected result:** Both repositories done, build clean, 2/12 complete

**If proceeding:** Confirm, and I'll start Phase 1.1 immediately.

**If pausing:** I'll create a fresh thread handoff with current status preserved here.

---

**Document Last Updated:** January 12, 2026, 21:00 UTC  
**Status Snapshot:** Phase 0 complete, 5 files created, infrastructure ready  
**Next Phase:** Phase 1 (core repositories)
