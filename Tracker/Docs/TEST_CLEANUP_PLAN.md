# Test Cleanup Plan

**Created:** December 17, 2025  
**Status:** Pending  
**Estimated Effort:** 4-6 hours

---

## Overview

The test project (`Tracker.Tests`) has fallen behind the main application code. There are currently **~727 build errors** preventing tests from running.

---

## Root Cause Analysis

| Error Code | Count | Cause | Fix Complexity |
|------------|-------|-------|----------------|
| **CS0246** | ~1238 | Missing `using` statements | 🟢 Easy - Add using directives |
| **CS0234** | ~114 | Namespace/type moved or renamed | 🟡 Medium - Update references |
| **CS0103** | ~102 | Variable/type doesn't exist | 🟡 Medium - Update to new APIs |

### Most Common Missing Types

| Type | Count | Root Cause | Fix |
|------|-------|------------|-----|
| `Fact`, `FactAttribute` | 856 | Missing `using Xunit;` | Add using statement |
| `Theory`, `TheoryAttribute` | 40 | Missing `using Xunit;` | Add using statement |
| `InlineData`, `InlineDataAttribute` | 216 | Missing `using Xunit;` | Add using statement |
| `IAsyncLifetime` | 16 | Missing `using Xunit;` | Add using statement |
| `Collection`, `CollectionAttribute` | 32 | Missing `using Xunit;` | Add using statement |
| `TeamMember` | 16 | Missing `using Tracker.DataModels;` | Add using statement |
| `TrackerDbContext` | 6 | Moved namespace | Update using |
| `ReminderType` | 3+ | Enum renamed/removed | Update to new enum |
| `NoteCategory` | 4 | Enum renamed/removed | Update to new enum |
| `FeedbackType` | 4 | Enum renamed/removed | Update to new enum |
| `GoalStatus` | 4 | Enum renamed/removed | Update to new enum |
| `GoalCategory` | 4 | Enum renamed/removed | Update to new enum |

---

## Fix Strategy

### Phase 1: Global Usings (5 min) 🟢 Quick Win

Add a `GlobalUsings.cs` file to fix 90%+ of errors:

```csharp
// Tracker.Tests/GlobalUsings.cs
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Tracker.DataModels;
global using Tracker.Common.Enums;
global using Tracker.Database;
global using Tracker.Tests.Infrastructure;
```

### Phase 2: Update Enum References (30 min) 🟡 Medium

Map old enum names to current ones:

| Old Name | New Name | Location |
|----------|----------|----------|
| `ReminderType` | Check if exists or removed | `ReminderTests.cs` |
| `NoteCategory` | `QuickNoteCategory` (if renamed) | `QuickNoteTests.cs` |
| `FeedbackType` | `FeedbackTypeEnum` | `FeedbackTests.cs` |
| `GoalStatus` | `GoalStatusEnum` | `IndividualGoalTests.cs` |
| `GoalCategory` | `GoalCategoryEnum` | `IndividualGoalTests.cs` |

### Phase 3: Update API Changes (1-2 hours) 🔴 Needs Review

Some tests reference old method signatures or removed properties. Each will need individual review:

**Files likely needing updates:**
- `TrackerDbManagerTests.cs` - DB API changes
- `OneOnOneViewModelTests.cs` - ViewModel changes
- `TeamMemberViewModelTests.cs` - ViewModel changes
- `ReminderServiceTests.cs` - Service changes

### Phase 4: Delete Obsolete Tests (30 min)

Some features may have been removed. Delete tests for:
- Removed entities
- Deprecated features
- Renamed/replaced functionality

---

## Test Files by Priority

### High Priority (Core functionality)
- [ ] `Database/TrackerDbManagerTests.cs` - Critical data layer
- [ ] `Database/TrackerDbContextTests.cs` - EF Core setup
- [ ] `ViewModels/OneOnOneViewModelTests.cs` - Core feature
- [ ] `ViewModels/TeamMemberViewModelTests.cs` - Core feature

### Medium Priority (Supporting features)
- [ ] `DataModels/*.cs` - All model tests
- [ ] `Services/*.cs` - Service layer tests
- [ ] `Converters/ConverterTests.cs` - UI converters
- [ ] `Commands/CommandTests.cs` - ICommand tests

### Lower Priority (Can skip initially)
- [ ] `Integration/*.cs` - Need more setup
- [ ] `Help/HelpServiceTests.cs` - Nice to have

---

## Execution Checklist

```
[ ] 1. Create GlobalUsings.cs with common imports
[ ] 2. Attempt build - see remaining errors
[ ] 3. Fix enum renames in DataModel tests
[ ] 4. Fix namespace changes in Database tests
[ ] 5. Update ViewModel tests for API changes
[ ] 6. Delete tests for removed features
[ ] 7. Run tests - fix failures
[ ] 8. Document any tests still needing work
```

---

## Expected Outcome

After cleanup:
- Tests should **build** successfully
- **Core tests** (Database, ViewModels) should pass
- Some tests may be marked `[Skip]` pending further work
- Test coverage baseline established for going forward

---

## Commands for Cleanup Session

```powershell
# Build tests to see current errors
dotnet build Tracker.Tests\Tracker.Tests.csproj

# Run tests after fixing
dotnet test Tracker.Tests\Tracker.Tests.csproj --verbosity normal

# Run specific test file
dotnet test Tracker.Tests\Tracker.Tests.csproj --filter "FullyQualifiedName~TeamMemberTests"

# Run with coverage (after tests pass)
dotnet test Tracker.Tests\Tracker.Tests.csproj --collect:"XPlat Code Coverage"
```

---

## Notes

- The main app builds and runs fine - this is test-only tech debt
- Going forward, new changes should include test updates (per .cursorrules)
- Consider running tests in CI/CD to catch drift early


