# DAPPER MIGRATION - QUICK REFERENCE INDEX

**Everything is isolated and documented in ONE PLACE for easy navigation.**

---

## 📌 MASTER STATUS DOCUMENT
👉 **[DAPPER_MIGRATION_STATUS.md](DAPPER_MIGRATION_STATUS.md)**
- Complete migration plan
- Phase breakdown
- Repository segregation (12 to create, 48 to ignore)
- File size limits (EDICT: max 300 lines)
- When to handoff criteria
- All rules and objectives

**Read this first. It has everything.**

---

## 📋 ORIGINAL PLAN (Reference)
👉 **[DAPPER_MIGRATION_PLAN.md](DAPPER_MIGRATION_PLAN.md)**
- Initial strategy (kept for historical reference)
- Now superseded by DAPPER_MIGRATION_STATUS.md

---

## 🗂️ INFRASTRUCTURE CODE CREATED

Location: `Tracker\Tracker\Services\Data\`

| File | Purpose | Lines | Status |
|------|---------|-------|--------|
| `IRepository.cs` | Generic CRUD interface | 100 | ✅ Complete |
| `BaseRepository.cs` | Base implementation (shared by all) | 400 | ✅ Complete |
| `DapperConnectionFactory.cs` | PostgreSQL connection management | 70 | ✅ Complete |
| `IUnitOfWork.cs` | Transaction interface | 50 | ✅ Complete |
| `Repositories/UserRepository.cs` | First concrete repository (template) | 180 | ✅ Complete |

---

## 🎯 NEXT STEPS

**Phase 1 starts now: Create 6 core repositories**

Time: 18 hours (3 steps × 6 hours each)
Expected: UserRepository, TeamMemberRepository, MeetingRepository, TaskRepository, GoalRepository, MetricRepository

See DAPPER_MIGRATION_STATUS.md for detailed breakdown.

---

## 📊 SCHEMA REFERENCE

👉 **[SupaBase SQL Scrips/SCHEMA_DIAGRAM_REFERENCE.md](SupaBase%20SQL%20Scrips/SCHEMA_DIAGRAM_REFERENCE.md)**
- Complete 60-table PostgreSQL schema
- PK-FK relationships
- Reference for repository creation

---

## 💾 CURRENT PHASE STATUS

| Phase | Status | Files | Effort |
|-------|--------|-------|--------|
| 0 | ✅ COMPLETE | 5 created | 4 hours |
| 1 | ⏳ Next | 6 to create | 18 hours |
| 2 | ⏳ Next | 6 to create | 18 hours |
| 3 | 🔮 Future | ViewModels | 40 hours |
| 4 | 🔮 Future | Cleanup | 8 hours |
| 5 | 🔮 Future | Verify | 4 hours |

**Total Remaining:** ~88 hours = 7-10 days

---

## ✅ RULES (Binding)

1. No file > 300 lines (BaseRepository exception: 400)
2. One repository per file
3. All repositories: inherit BaseRepository<T>, have interface
4. DI register immediately after creation
5. Build must pass after each step
6. No ViewModels touched until Phase 3
7. No EF deleted until Phase 4

---

## 🚨 HANDOFF CRITERIA

**If any of these occur, STOP and handoff to new thread:**
1. Build breaks unexpectedly
2. A file exceeds 300 lines unintentionally
3. Architectural problem discovered
4. Thread context getting full
5. Single issue takes >30 min to debug

**Handoff location:** Update DAPPER_MIGRATION_STATUS.md with current state, create new thread with link.

---

## 🗺️ NAVIGATION

**You are here:** `/New Docs/DAPPER_MIGRATION_INDEX.md`

**Go to:**
- Complete status → DAPPER_MIGRATION_STATUS.md
- Phase details → Section in DAPPER_MIGRATION_STATUS.md
- Code → `/Services/Data/`
- Schema → `SupaBase SQL Scrips/SCHEMA_DIAGRAM_REFERENCE.md`
- DI config → `/Infrastructure/ServiceConfiguration.cs`

---

**Last Updated:** January 12, 2026  
**Status:** Phase 0 complete, Phase 1 ready to start  
**Next Action:** Confirm to start Phase 1.1 (UserRepository + TeamMemberRepository)
