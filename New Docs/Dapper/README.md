# Dapper & Supabase Data Access Documentation

**Start Here** if you're new to the Tracker codebase or need to understand how we access the database.

---

## What's in This Folder?

This folder contains comprehensive documentation for Tracker's data access layer using **Dapper** and **Supabase PostgreSQL**.

---

## Reading Order

Read these documents in order for the best understanding:

| # | Document | What You'll Learn |
|---|----------|-------------------|
| 1 | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) | Big picture: layers, why Dapper, folder structure |
| 2 | [02_CONNECTION_MANAGEMENT.md](02_CONNECTION_MANAGEMENT.md) | How database connections work |
| 3 | [03_REPOSITORY_PATTERN.md](03_REPOSITORY_PATTERN.md) | How repositories encapsulate SQL |
| 4 | [04_SUPABASE_AND_RLS.md](04_SUPABASE_AND_RLS.md) | Supabase setup, Row-Level Security |
| 5 | [05_AUTHENTICATION_FLOW.md](05_AUTHENTICATION_FLOW.md) | Login, signup, tokens |
| 6 | [06_ADDING_NEW_ENTITIES.md](06_ADDING_NEW_ENTITIES.md) | Step-by-step guide for new entities |
| 7 | [07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md) | Common issues and solutions |
| 8 | [08_QUICK_REFERENCE.md](08_QUICK_REFERENCE.md) | Cheatsheet for daily use |

---

## Quick Start

**If you just need to...**

| Task | Go To |
|------|-------|
| Understand the architecture | [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) |
| Add a new entity | [06_ADDING_NEW_ENTITIES.md](06_ADDING_NEW_ENTITIES.md) |
| Fix a connection issue | [07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md) |
| Look up query syntax | [08_QUICK_REFERENCE.md](08_QUICK_REFERENCE.md) |

---

## Key Concepts Summary

### Architecture

```
ViewModel → Service → Repository → Database
    ↓          ↓           ↓           ↓
 UI Logic   Business    SQL Only   Supabase
            Logic                  PostgreSQL
```

### Golden Rules

1. **SQL lives ONLY in repositories** - never in ViewModels or Services
2. **Soft delete always** - set `is_deleted = true`, never hard delete
3. **Always use `using`** - dispose connections properly
4. **Parameterize everything** - never concatenate SQL strings

### Technology Stack

- **Dapper** - SQL execution & object mapping
- **Npgsql** - PostgreSQL driver for .NET
- **Supabase** - Hosted PostgreSQL + Auth + RLS
- **BCrypt** - Password hashing

---

## The 12 Gold Standard Repositories

| Repository | Entity | Purpose |
|------------|--------|---------|
| `UserRepository` | User | Application users |
| `TeamMemberRepository` | TeamMember | Staff/employees |
| `MeetingRepository` | Meeting | All meeting types |
| `TaskRepository` | TrackerTask | Work items |
| `GoalRepository` | Goal | OKRs/objectives |
| `MetricRepository` | Metric | KPIs/measurements |
| `FeedbackRepository` | Feedback | Feedback records |
| `ProjectRepository` | Project | Project containers |
| `QuickNoteRepository` | QuickNote | Quick notes |
| `DevelopmentGoalRepository` | DevelopmentGoal | Personal development |
| `PerformanceReviewRepository` | PerformanceReview | Reviews |
| `PulseSurveyRepository` | PulseSurvey | Surveys |

---

## Need Help?

1. **Check [07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md)** - most common issues covered
2. **Look at existing code** - see how similar repositories solve problems
3. **Test SQL in Supabase** - SQL Editor lets you run queries directly
4. **Check Supabase logs** - Dashboard → Logs

---

## Related Documentation

- **Migration Status:** [../DAPPER_MIGRATION_STATUS.md](../DAPPER_MIGRATION_STATUS.md)
- **Coding Guidelines:** [../CODING_GUIDELINES.md](../CODING_GUIDELINES.md)
- **Schema Reference:** [../SupaBase SQL Scrips/SCHEMA_DIAGRAM_REFERENCE.md](../SupaBase%20SQL%20Scrips/SCHEMA_DIAGRAM_REFERENCE.md)

---

**Last Updated:** January 14, 2026
