# DAPPER MIGRATION PLAN - FULL EXECUTION ROADMAP

**Start Date:** January 12, 2026  
**Target:** Complete migration in 7-10 days  
**Strategy:** FULL MIGRATION ONLY - No hybrid approach, no partial implementations  
**Success Metric:** 76 build errors → 0 errors + app runs with zero EF Core references  
**Commitment:** Complete removal of Entity Framework Core - every ViewModel, Service, and Manager uses Dapper repositories

---

## PHASE 0: SETUP INFRASTRUCTURE (4-6 hours)

**Goal:** Create the Dapper scaffolding without touching existing code yet.

### 0.1 Create Base Interfaces & Abstractions

**New files to create:**
```
/Services/Data/IRepository.cs          - Base generic interface
/Services/Data/IUnitOfWork.cs          - Transaction/batch management
/Services/Data/DapperConnectionFactory.cs - Connection management
/Services/Data/Mappings/                - Auto-mapping configuration
```

### 0.2 Create Connection Management

```csharp
// Services/Data/DapperConnectionFactory.cs
public interface IDapperConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly DatabaseSettings _settings;
    
    public DapperConnectionFactory(DatabaseSettings settings) => _settings = settings;
    
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_settings.GetConnectionString());
    }
}
```

### 0.3 Register Dependencies

**Modify Program.cs / DI container:**
```csharp
services.AddScoped<IDapperConnectionFactory, DapperConnectionFactory>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### 0.4 Create Generic Base Repository

```csharp
// Services/Data/BaseRepository.cs
public abstract class BaseRepository<T> where T : class
{
    protected readonly IDapperConnectionFactory _connectionFactory;
    protected readonly ILogger<BaseRepository<T>> _logger;
    
    protected BaseRepository(IDapperConnectionFactory connectionFactory, ILogger<BaseRepository<T>> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    // GetById(Guid id)
    // Create(T entity)
    // Update(T entity)
    // Delete(Guid id)  - soft delete
    // GetAll()
}
```

**Effort:** 4-6 hours

---

## PHASE 1: MIGRATE CORE ENTITIES (12-16 hours)

**Migrate in this order (highest impact first):**

### 1.1 Migrate User Repository
```
/Repositories/UserRepository.cs
- GetUserById(Guid id)
- GetUserByEmail(string email)
- GetUserBySupabaseId(Guid supabaseId)
- CreateUser(User user)
- UpdateUser(User user)
- DeleteUser(Guid id)  [soft delete]
```

**Impact:** AuthService, UserSettingsManager, and multiple ViewModels depend on this.

### 1.2 Migrate TeamMember Repository
```
/Repositories/TeamMemberRepository.cs
- GetTeamMemberById(Guid id)
- GetTeamMembersByOrganization(Guid orgId)
- GetTeamMembersByManager(Guid managerId)
- CreateTeamMember(TeamMember member)
- UpdateTeamMember(TeamMember member)
- DeleteTeamMember(Guid id)
```

**Impact:** Core entity, used everywhere (1:1 meetings, feedback, goals).

### 1.3 Migrate Meeting Repository
```
/Repositories/MeetingRepository.cs
- GetMeetingById(Guid id)
- GetMeetingsByTeamMember(Guid teamMemberId, DateRange range)
- CreateMeeting(Meeting meeting)
- UpdateMeeting(Meeting meeting)
- GetUpcomingMeetings(Guid orgId, int days)
- GetMeetingsByStatus(Guid orgId, MeetingStatus status)
```

**Impact:** High - meetings are central to app.

### 1.4 Migrate Task Repository
```
/Repositories/TaskRepository.cs
- GetTaskById(Guid id)
- GetTasksByOwner(Guid ownerId)
- GetTasksByProject(Guid projectId)
- GetTasksByGoal(Guid goalId)
- GetTasksByStatus(Guid orgId, TaskStatus status)
- CreateTask(TrackerTask task)
- UpdateTask(TrackerTask task)
- CompleteTask(Guid id, DateTime completedAt)
```

**Impact:** High - work items are core.

### 1.5 Migrate Goal Repository
```
/Repositories/GoalRepository.cs
- GetGoalById(Guid id)
- GetGoalsByOwner(Guid teamMemberId)
- GetGoalsByOrganization(Guid orgId)
- GetGoalsByStatus(Guid orgId, GoalStatus status)
- CreateGoal(Goal goal)
- UpdateGoal(Goal goal)
- CalculateProgress(Guid goalId)  [aggregation]
```

**Impact:** Medium-high - OKR management.

### 1.6 Migrate Metric Repository
```
/Repositories/MetricRepository.cs
- GetMetricById(Guid id)
- GetMetricsByOwner(Guid teamMemberId)
- GetMetricsByOrganization(Guid orgId)
- CreateMetric(Metric metric)
- UpdateMetric(Metric metric)
- RecordMetricValue(Guid metricId, decimal value, DateTime timestamp)
- GetMetricHistory(Guid metricId, DateRange range)
```

**Impact:** Medium - analytics/dashboards.

**Effort per repo:** 2-3 hours  
**Total Phase 1:** 12-16 hours

---

## PHASE 2: MIGRATE SUPPORTING ENTITIES (8-10 hours)

These are lower-impact but still needed:

### 2.1 Migrate Feedback Repository
```
/Repositories/FeedbackRepository.cs
- GetFeedbackById(Guid id)
- GetFeedbackForTeamMember(Guid teamMemberId, DateRange range)
- GetFeedbackFromTeamMember(Guid fromId, DateRange range)
- CreateFeedback(Feedback feedback)
- GetFeedbackStats(Guid teamMemberId)
```

### 2.2 Migrate Project Repository
```
/Repositories/ProjectRepository.cs
- GetProjectById(Guid id)
- GetProjectsByOrganization(Guid orgId)
- GetProjectsByOwner(Guid ownerId)
- CreateProject(Project project)
- UpdateProject(Project project)
```

### 2.3 Migrate Notes Repository
```
/Repositories/NotesRepository.cs
- GetNoteById(Guid id)
- GetNotesByTeamMember(Guid teamMemberId)
- GetNotesByMeeting(Guid meetingId)
- CreateNote(Note note)
- UpdateNote(Note note)
- SearchNotes(string query, Guid orgId)
```

### 2.4 Migrate Development Goals & Performance Reviews

```
/Repositories/DevelopmentGoalRepository.cs
/Repositories/PerformanceReviewRepository.cs
```

### 2.5 Migrate Pulse Surveys

```
/Repositories/PulseSurveyRepository.cs
```

**Effort per repo:** 1-2 hours  
**Total Phase 2:** 8-10 hours

---

## PHASE 3: MIGRATE VIEWMODELS (16-20 hours)

**Dependency Injection into ViewModels:**

Before:
```csharp
public class OneOnOneViewModel : BaseViewModel
{
    private readonly TrackerDbContext _context;
    
    public OneOnOneViewModel()
    {
        _context = new TrackerDbContext();
    }
}
```

After:
```csharp
public class OneOnOneViewModel : BaseViewModel
{
    private readonly IMeetingRepository _meetings;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly ITaskRepository _tasks;
    
    public OneOnOneViewModel(
        IMeetingRepository meetings,
        ITeamMemberRepository teamMembers,
        ITaskRepository tasks)
    {
        _meetings = meetings;
        _teamMembers = teamMembers;
        _tasks = tasks;
    }
}
```

### 3.1 Start with High-Error ViewModels (Map build errors → ViewModels)

**From 76 errors, identify which ViewModels have the most errors:**
- OneOnOneViewModel (currently deleted, but needed)
- MeasurableViewModel
- InsightPanelViewModel
- etc.

**Strategy:** Migrate top 5 ViewModels that account for 50% of errors.

### 3.2 Update Service Classes

Any service using `TrackerDbContext` directly → inject repositories instead.

Example:
```csharp
// OLD
public class TrackerDataManager
{
    private readonly TrackerDbContext _context;
    
    public Task<Guid> AddMeetingAsync(Meeting meeting)
    {
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();
        return meeting.Id;
    }
}

// NEW
public class TrackerDataManager
{
    private readonly IMeetingRepository _meetings;
    
    public async Task<Guid> AddMeetingAsync(Meeting meeting)
    {
        return await _meetings.CreateAsync(meeting);
    }
}
```

**Effort:** 16-20 hours (mostly mechanical)

---

## PHASE 4: DELETE OLD CODE & VERIFY (6-8 hours)

### 4.1 Remove EF Core References
- Delete TrackerDbContext.cs
- Remove Microsoft.EntityFrameworkCore from .csproj
- Remove all `using Tracker.Database` statements

### 4.2 Remove Old Data Manager Methods
- TrackerDataManager now just delegates to repositories
- Or delete it entirely if no longer needed

### 4.3 Run Build & Fix Remaining Errors

Expected result: **0 build errors**

### 4.4 Test Suite
- Unit test repositories
- Integration test ViewModels with mocked repositories

**Effort:** 6-8 hours

---

## PHASE 5: SHIP & ITERATE (2-4 hours)

### 5.1 Deploy to Development
### 5.2 Manual Testing
### 5.3 Performance Validation
### 5.4 Documentation

---

## TOTAL EFFORT ESTIMATE

| Phase | Task | Hours | Days |
|-------|------|-------|------|
| 0 | Infrastructure Setup | 5 | 0.5 |
| 1 | Core Repositories (6 repos) | 15 | 2 |
| 2 | Supporting Repositories (6 repos) | 12 | 1.5 |
| 3 | ViewModel Migration (ALL of them) | 25 | 3 |
| 4 | Complete EF Removal & Cleanup | 8 | 1 |
| 5 | Ship & Iterate | 3 | 0.5 |
| **TOTAL** | | **68 hours** | **~8 days** |

**Timeline:** 7-10 days of focused, continuous work to complete FULL migration with zero technical debt

---

## ALTERNATIVE: FASTER PATH (HYBRID FIRST - 24-48 hours)

**NOT AN OPTION.** You chose FULL MIGRATION to avoid exactly this kind of mess. Hybrid approaches create:
- Confused code with mixed patterns
- Multiple places to debug the same problem
- Technical debt that compounds
- Exactly what happened the last time

Instead: FULL MIGRATION, done right, once.

---

## EXECUTION CHECKLIST - FULL MIGRATION

**Day 1 - Morning (4 hours):**
- [ ] Phase 0: Create infrastructure files
- [ ] Set up DI container
- [ ] Create BaseRepository template
- [ ] NO EXCEPTIONS, COMPLETE SETUP

**Day 1 - Afternoon (6 hours):**
- [ ] Create UserRepository (COMPLETE)
- [ ] Create TeamMemberRepository (COMPLETE)
- [ ] Create MeetingRepository (COMPLETE)
- [ ] Test with simple unit tests

**Day 2 - Morning (6 hours):**
- [ ] Create TaskRepository (COMPLETE)
- [ ] Create GoalRepository (COMPLETE) 
- [ ] Create MetricRepository (COMPLETE)
- [ ] All 6 core repositories DONE

**Day 2 - Afternoon (6 hours):**
- [ ] Create FeedbackRepository
- [ ] Create ProjectRepository
- [ ] Create NotesRepository

**Day 3 - Morning (6 hours):**
- [ ] Create DevelopmentGoalRepository
- [ ] Create PerformanceReviewRepository
- [ ] Create PulseSurveyRepository
- [ ] ALL 12 REPOSITORIES COMPLETE

**Day 3-4 - ViewModel Migration (16 hours):**
- [ ] Migrate OneOnOneViewModel
- [ ] Migrate MeasurableViewModel
- [ ] Migrate InsightPanelViewModel
- [ ] Migrate remaining ViewModels systematically
- [ ] Update all Services to use repositories
- [ ] NO ViewModels using DbContext

**Day 4-5 - Final Cleanup (12 hours):**
- [ ] Delete TrackerDbContext.cs
- [ ] Remove all EF migrations
- [ ] Remove EntityFrameworkCore NuGet packages
- [ ] Remove all `using Tracker.Database` statements
- [ ] Build: 76 errors → 0 errors
- [ ] Run: App starts successfully

**Day 5 - Final Verification (4 hours):**
- [ ] Manual testing of core flows
- [ ] Performance validation
- [ ] Documentation updates
- [ ] SHIP

---

## DON'T DO THIS:

❌ Try to keep both EF and Dapper running (mixing patterns)
❌ Leave some ViewModels on EF, others on Dapper (confusion)
❌ Keep TrackerDbContext "just in case" (technical debt)
❌ Use hybrid approach to "speed things up" (creates mess like before)
❌ Refactor data models during migration (separate concern)

## DO THIS:

✅ COMPLETE removal of EF Core in 7-10 days
✅ Every ViewModel uses repositories consistently
✅ Every repository uses Dapper consistently
✅ Zero DbContext references in final code
✅ One clean architecture, not two  

---

## START HERE (Next 4 hours):

Phase 0 - Infrastructure Foundation (Must be 100% complete before moving to Phase 1):

1. Create `/Services/Data/` folder structure
2. Create `IRepository.cs` and `BaseRepository.cs`
3. Create `DapperConnectionFactory.cs`
4. Create `IUnitOfWork.cs` (for transaction handling)
5. Create `UserRepository.cs` as template example
6. Update `Program.cs` to register all dependencies
7. Build solution - should compile with no errors
8. Create unit test stubs for repositories

**NO SHORTCUTS.** This foundation must be solid because all 12 repositories will be built on it.

Once Phase 0 compiles clean, we move to Phase 1 at full speed (1-2 hours per repository).

