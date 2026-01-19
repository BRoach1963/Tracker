# ProCohere Avalonia - Session Handoff Document
**Date**: January 18, 2026  
**Session Focus**: Circle View UI fixes - Calendar meetings, timezone handling, scrolling  

---

## 🎯 Executive Summary

This session focused on fixing critical bugs in the **Circle View** (Team/Goals/Feedback/Meetings tabs) of the ProCohere Avalonia application. The main issues were:

1. **Day/Week view meetings not showing** - Fixed timezone conversion issue
2. **Meeting times displaying incorrectly** (showing AM instead of PM) - Fixed
3. **Calendar positioning wrong** - Fixed offset calculation
4. **Tab content not scrolling** - Fixed Grid.Row placement
5. **Broader architecture discussions** - Timezone handling for national/international product, localization strategy

---

## 🏗️ Project Overview

### What is ProCohere?
A professional relationship management desktop app built with:
- **Framework**: Avalonia UI (.NET 8) - cross-platform WPF alternative
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **Database**: Supabase PostgreSQL (via Postgrest API)
- **Auth**: Supabase Auth with JWT tokens

### Project Structure
```
Tracker/
├── Tracker/
│   ├── ProCohere.Avalonia/     ← ACTIVE AVALONIA APP
│   │   ├── Models/             ← Data models (Postgrest attributes)
│   │   ├── ViewModels/         ← MVVM ViewModels
│   │   ├── Views/              ← AXAML views
│   │   ├── Services/           ← Business logic, API calls
│   │   ├── Converters/         ← Value converters
│   │   └── Themes/             ← Styles, colors
│   ├── Tracker/                ← OLD WPF APP (reference only)
│   └── Tracker.Core/           ← Shared core library
├── New Docs/                   ← Documentation
│   ├── Dapper/                 ← Data access architecture docs
│   ├── TIMEZONE_AND_LOCALIZATION.md  ← Created this session
│   └── PROCOHERE_BUGS.md       ← Bug tracking
└── .github/
    └── copilot-instructions.md ← MUST READ - project rules
```

---

## ✅ Issues Fixed This Session

### 1. Timezone/DateTime Conversion Bug (CRITICAL)

**Problem**: Meetings stored in UTC showed at wrong times. A noon meeting showed as "12:00 AM".

**Root Cause**: Supabase Postgrest returns `DateTime` with `Kind=Unspecified`. When you call `.ToLocalTime()` on `Unspecified` kind, .NET assumes it's already local and doesn't convert.

**Solution**: Added `ScheduledAtLocal` property to `MeetingDetail.cs`:

```csharp
public DateTime? ScheduledAtLocal
{
    get
    {
        if (!ScheduledAt.HasValue) return null;
        var dt = ScheduledAt.Value;
        // If Kind is Unspecified, treat as UTC since Supabase stores in UTC
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return dt.ToLocalTime();
    }
}
```

**Files Modified**:
- `Models/MeetingDetail.cs` - Added `ScheduledAtLocal`, `LocalDate` properties; updated ALL display properties to use them:
  - `StartTimeDisplay`
  - `EndTimeDisplay`
  - `TimeRangeDisplay`
  - `ScheduledText`
  - `ShortTimeDisplay`
  - `StartHour` / `StartMinute` (calendar positioning)
  - `DayOfWeekIndex`
  - `DateGroupDisplay`

- `ViewModels/CircleViewModel.cs` - Updated all meeting filters to use `LocalDate`:
  - `DayMeetings` property
  - `RefreshMeetingsView()` - Day/Week/Month filters
  - `RefreshWeekDays()` 
  - `RefreshCalendarDays()`

### 2. Calendar Positioning Bug

**Problem**: Meetings positioned from hour 0 (midnight) instead of hour 8 (8 AM calendar start).

**Solution**: Fixed `CalendarTopOffset` in `MeetingDetail.cs`:
```csharp
private const int CalendarStartHour = 8;
public double CalendarTopOffset => Math.Max(0, ((StartHour - CalendarStartHour) * 60) + StartMinute);
```

### 3. Canvas Positioning Not Working (Day/Week Views)

**Problem**: Even with correct `CalendarTopOffset` values, meetings were stuck at top of calendar.

**Root Cause**: In Avalonia, `Canvas.Top` set directly on an item inside a DataTemplate doesn't work - it needs to be on the ContentPresenter.

**Solution**: Added `ItemsControl.Styles` to set Canvas.Top on the ContentPresenter:
```xml
<ItemsControl.Styles>
    <Style Selector="ContentPresenter">
        <Setter Property="Canvas.Top" Value="{Binding CalendarTopOffset}"/>
    </Style>
</ItemsControl.Styles>
```

**Files Modified**: `Views/CircleView.axaml` - Both Day view and Week view ItemsControls

### 4. Tab Content Not Scrolling

**Problem**: Goals, Feedback, Meetings tabs had content that didn't scroll.

**Root Cause**: The tab content Grids were missing `Grid.Row="1"` so they weren't filling the available space in their parent Grid.

**Solution**: Added `Grid.Row="1"` to Goals, Feedback, and Meetings tab Grids in `CircleView.axaml`.

---

## 🐛 Known Bugs (Not Fixed This Session)

See `New Docs/PROCOHERE_BUGS.md` for full details:

| Bug | Description | Priority |
|-----|-------------|----------|
| BUG-001 | Team card scroll issue when selecting | Medium |
| BUG-002 | No visual indicator on selected card | Medium |
| BUG-003 | Manager "Reports" badge not obviously clickable | Medium |
| BUG-004 | Manager filter breadcrumb too subtle | Low |

---

## 🌍 Architecture Decisions Discussed

### Timezone Handling (Multi-Region Support)

**Current State**:
- ✅ Timestamps stored as `timestamptz` (UTC) in PostgreSQL
- ✅ Display converts to user's system timezone
- ✅ Cross-timezone meetings work (PST user schedules, EST user sees correct time)

**Future Improvements** (documented in `New Docs/TIMEZONE_AND_LOCALIZATION.md`):
1. **Phase 1**: Use user's stored `timezone` preference from database (not just system timezone)
2. **Phase 2**: Add `scheduled_timezone` column to meetings for recurrence and display context
3. **Phase 3**: Show timezone abbreviations (e.g., "9:00 AM PST")

### Localization (i18n)

**Recommendation**: Use ResX resources (familiar from WPF):
- Create `Resources/Strings.resx`, `Strings.es.resx`, etc.
- Use `{x:Static res:Strings.ButtonText}` in XAML
- Avalonia supports same pattern as WPF

---

## 📁 Key Files Reference

### Models
| File | Purpose |
|------|---------|
| `Models/MeetingDetail.cs` | Meeting with timezone-aware display properties |
| `Models/TeamMemberCard.cs` | Team member for Circle view |
| `Models/GoalDetail.cs` | Goal tracking |
| `Models/FeedbackDetail.cs` | Feedback entries |

### ViewModels
| File | Purpose |
|------|---------|
| `ViewModels/CircleViewModel.cs` | Team/Goals/Feedback/Meetings tabs |
| `ViewModels/TodayViewModel.cs` | Dashboard (Briefing) |
| `ViewModels/MainWindowViewModel.cs` | Navigation, auth state |

### Views
| File | Purpose |
|------|---------|
| `Views/CircleView.axaml` | Main Circle page (1585 lines) |
| `Views/TodayView.axaml` | Dashboard/Briefing page |
| `Views/SettingsView.axaml` | User settings |

### Services
| File | Purpose |
|------|---------|
| `Services/SupabaseService.cs` | Supabase client wrapper |
| `Services/AuthService.cs` | Authentication |

---

## 🎨 Design System

### Color Palette (from `Themes/Colors.axaml`)
| Name | Hex | Usage |
|------|-----|-------|
| Primary | `#3C6248` | Green - main actions |
| Secondary | `#2E3A5A` | Navy - headers |
| Highlight | `#D0AF5F` | Gold - accents |
| Surface | `#FFFFFF` | Card backgrounds |
| SurfaceAlt | `#F9FAFB` | Alternate backgrounds |

### Meeting Status Display
| Database Value | Display |
|----------------|---------|
| `scheduled` | "Scheduled" |
| `in_progress` | "In Progress" |
| `completed` | "Completed" |
| `cancelled` | "Cancelled" |

### Goal Status Colors
| Status | Background |
|--------|------------|
| `on_track` | Green |
| `at_risk` | Amber |
| `off_track` | Red |

---

## 🔧 Development Commands

```powershell
# Build ProCohere.Avalonia
cd "c:\Users\vbpro\source\repos\Tracker\Tracker\ProCohere.Avalonia"
dotnet build

# Run the app
Start-Process "bin\Debug\net8.0\ProCohere.Avalonia.exe"

# Kill existing instance before rebuild
Stop-Process -Name "ProCohere.Avalonia" -Force -ErrorAction SilentlyContinue
```

Or use VS Code tasks:
- `build-procohere` - Build ProCohere.Avalonia
- `build-all` - Build entire solution

---

## ⚠️ Critical Rules (from copilot-instructions.md)

1. **NEVER VIOLATE MVVM** - No business logic in Views
2. **NEVER TAKE SHORTCUTS** - Fix things properly
3. **Soft Delete Only** - Set `is_deleted = true`, never hard delete
4. **All IDs are GUIDs** - Supabase uses UUID
5. **SQL in Repositories Only** - Never in ViewModels or Services
6. **Update Documentation** - When changing data access code, update `/New Docs/Dapper/`

---

## 📋 Recommended Next Steps

### Immediate (P0)
1. Test the timezone fixes with meetings at various times
2. Verify Day/Week/Month views all show meetings correctly

### Short-term (P1)
1. Fix BUG-001: Auto-scroll to selected team member card
2. Fix BUG-002: Add visual selection indicator to cards
3. Implement user timezone preference (use stored `timezone` field)

### Medium-term (P2)
1. Add `scheduled_timezone` to meetings table
2. Extract UI strings to ResX resources for localization prep
3. Add language selector to Settings

---

## 🔗 Related Documentation

- `New Docs/TIMEZONE_AND_LOCALIZATION.md` - Full timezone/i18n architecture
- `New Docs/PROCOHERE_BUGS.md` - Bug tracking
- `New Docs/Dapper/` - Data access architecture
- `.github/copilot-instructions.md` - Project rules (MUST READ)
- `ProCohere.Avalonia/DASHBOARD_PLAN.md` - Briefing page implementation plan

---

## 💡 Context for Next Session

The Circle view is mostly working now. Key remaining work:
1. **UI polish** - Selection indicators, scroll behavior
2. **Timezone architecture** - Use user preference instead of system timezone
3. **Localization setup** - Extract strings to resources

The codebase follows MVVM strictly. Avalonia XAML is very similar to WPF. The main gotcha is the `DateTime.Kind=Unspecified` issue from Postgrest - always use `ScheduledAtLocal` for display, not raw `ScheduledAt`.
