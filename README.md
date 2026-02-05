# ProCohere – Engineering Documentation

> **Status:** ✅ FEATURE COMPLETE (February 5, 2026)

ProCohere is a cross-platform desktop application for team management, goal tracking, and productivity. Built with Avalonia UI targeting Windows, macOS, and Linux.

---

## Quick Links

| Document | Purpose |
|----------|---------|
| [PROCOHERE_PRIORITY_BACKLOG.md](PROCOHERE_PRIORITY_BACKLOG.md) | **START HERE** - Feature status, deferred items, accessibility plan |
| [Bugs/PROCOHERE_BUGS.md](Bugs/PROCOHERE_BUGS.md) | Known bugs (4 minor UI issues) |
| [Wiki/Database/README.md](Wiki/Database/README.md) | Database schema documentation |
| [Wiki/ProCohere.Avalonia/README.md](Wiki/ProCohere.Avalonia/README.md) | Application architecture |

---

## Project Structure

```
Tracker/
├── PROCOHERE_PRIORITY_BACKLOG.md  # Feature status & roadmap
├── Bugs/                           # Bug tracking
├── Docs/                           # User guides & reference
│   ├── GOOGLE_CALENDAR_SETUP.md   # Calendar integration guide
│   ├── INSIGHTS_USER_GUIDE.md     # AI insights guide
│   └── Archive/                    # Historical design docs
├── Wiki/                           # Technical documentation
│   ├── Database/                   # Schema, RLS, migrations
│   ├── ProCohere.Avalonia/         # App architecture
│   ├── Features/                   # Feature specifications
│   └── CodeBase/                   # Code patterns
├── Tracker/                        # Source code
│   ├── ProCohere.Avalonia/        # Main Avalonia app
│   ├── Tracker.Core/              # Shared core library
│   └── supabase/                  # Database migrations
└── Handoff Documents/Archive/      # Historical session notes
```

---

## Technology Stack

- **UI Framework:** Avalonia 11.x (cross-platform)
- **Language:** C# / .NET 8
- **Database:** Supabase PostgreSQL with Row Level Security
- **ORM:** Dapper
- **AI:** Google Gemini (gemini-1.5-flash)
- **Testing:** xUnit, Moq

---

## Features (All Complete ✅)

- **Authentication:** Login, password reset, profile management
- **Team Management:** Team members, relationships, 1:1 meetings
- **Goal Tracking:** Goals, metrics, targets, health status
- **Project Management:** Projects, work items, phases
- **Task Management:** Tasks, assignments, due dates
- **Notes:** Chronicle with entity linking
- **Reports:** Multiple report types with Excel/PDF/CSV export
- **AI Assistant:** Chat with function calling, daily insights
- **System Tray:** Minimize to tray, notifications

---

## Deferred Items (Not Required for Launch)

See [PROCOHERE_PRIORITY_BACKLOG.md](PROCOHERE_PRIORITY_BACKLOG.md) for details:
- Accessibility (keyboard nav, screen readers)
- Animations & transitions
- Real-time sync
- Offline mode
- Performance optimization
- macOS/Linux testing
