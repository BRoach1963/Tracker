# ProCohere Avalonia - Priority Backlog

> **Last Updated:** February 5, 2026  
> **Status:** ✅ FEATURE COMPLETE - All core functionality implemented  
> **Remaining:** Polish, accessibility, performance, platform testing (all deferred)

---

## 🎉 FEATURE COMPLETE SUMMARY

**ProCohere Avalonia is feature complete as of February 5, 2026.**

All core functionality has been implemented:

| Area | Status |
|------|--------|
| **Auth** | ✅ Login, password reset, profile editing, avatar upload |
| **Briefing View** | ✅ Daily briefing, AI insights, action items |
| **Me View** | ✅ Personal dashboard, calendar, tasks, goals, meetings |
| **Circle View** | ✅ Team management, relationships, 1:1s, feedback, kudos |
| **Pulse View** | ✅ Goals, metrics, tasks overview |
| **Projects View** | ✅ Full project management, work items, phases |
| **Chronicle View** | ✅ Notes, entity linking, search |
| **Reports View** | ✅ Multiple report types, Excel/PDF/CSV export |
| **Settings** | ✅ Theme, notifications, preferences |
| **AI Chat** | ✅ Gemini integration, function calling, context awareness |
| **System Tray** | ✅ Minimize to tray, notifications, exit confirmation |
| **Help System** | ✅ F1 help window, search, topic navigation |
| **All Dialogs** | ✅ 31 dialogs for CRUD operations |

---

## 🔮 DEFERRED ITEMS (Polish/Enhancement)

These items are NOT required for launch. All are polish, performance, or platform work:

| # | Item | Type | Notes |
|---|------|------|-------|
| 32 | Admin Window | DEFERRED | Moving to web platform |
| 33 | Setup Wizard | DEFERRED | Not needed |
| 35-36 | Accessibility Epic | DEFERRED | Keyboard nav + screen readers (see detailed plan below) |
| 37 | Animations & Transitions | POLISH | Smooth micro-interactions |
| 38 | Real-time Sync | ENHANCEMENT | Supabase Realtime - manual refresh works |
| 39 | Offline Mode | ENHANCEMENT | Local caching - requires internet is fine |
| 40 | Virtualized Lists | PERFORMANCE | Only needed at scale |
| 41 | Lazy Loading | PERFORMANCE | Only needed at scale |
| 42 | macOS Testing | PLATFORM | Windows is primary |
| 43 | Linux Testing | PLATFORM | Windows is primary |

---

## 📚 HELP DOCUMENTATION (Separate Effort)

User guides can be added post-launch. App is usable without them.
One guide complete: [INSIGHTS_USER_GUIDE.md](Docs/INSIGHTS_USER_GUIDE.md)

---

## 🐛 KNOWN BUGS

See [Bugs/PROCOHERE_BUGS.md](Bugs/PROCOHERE_BUGS.md) for 4 minor UI polish bugs in Circle view.

---

## ✅ COMPLETED ITEMS (1-34)

### ✅ 1. Register Insight Analyzers on Startup
**Completed:** February 4, 2026  
**Location:** [App.axaml.cs](Tracker/ProCohere.Avalonia/App.axaml.cs) lines 296-310  
**Implementation:** Created `InitializeInsightAnalyzers()` method, called from `InitializeAIServices()`, registers all 6 analyzers with InsightEngine.Instance  
**Result:** Insights fully functional in Briefing view

---

### ✅ 2. AddGoalDialog
**Completed:** February 4, 2026  
**Location:** [Dialogs/AddGoalDialog.axaml](Tracker/ProCohere.Avalonia/Dialogs/AddGoalDialog.axaml)  
**Implementation:** Full MVVM dialog with title, description, dates, parent goal picker, health status, owner assignment. Wired to CircleView event handler.  
**Result:** Can create goals from Circle view team member detail

---

### ✅ 3. AddMetricDialog
**Completed:** February 4, 2026  
**Location:** [Dialogs/AddMetricDialog.axaml](Tracker/ProCohere.Avalonia/Dialogs/AddMetricDialog.axaml)  
**Implementation:** Full metric creation with name, description, unit, type, target/baseline, frequency, owner. Wired via AppDialogService.  
**Result:** Can create metrics from Pulse view

---

### ✅ 4. AddFeedbackDialog
**Completed:** February 4, 2026  
**Location:** [Dialogs/AddFeedbackDialog.axaml](Tracker/ProCohere.Avalonia/Dialogs/AddFeedbackDialog.axaml)  
**Implementation:** Feedback creation with type (Positive/Constructive/Recognition), rating, anonymous option, recipient selection. Wired to CircleViewModel.  
**Result:** Can give feedback to team members

---

### ✅ 5. Initialize System Tray
**Completed:** February 4, 2026  
**Location:** [App.axaml.cs](Tracker/ProCohere.Avalonia/App.axaml.cs) lines 211, 267  
**Implementation:** `InitializeSystemTray(desktop)` called after MainWindow.Show() in both auto-login and manual login paths. Wires up ShowWindowRequested and ExitRequested handlers.  
**Result:** App minimizes to tray, tray menu functional with exit confirmation

---

### ✅ 6. Wire Soft Delete Confirmations
**Completed:** February 4, 2026  
**Files Updated:**
- [CircleViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/CircleViewModel.cs) - DeleteGoalAsync lines 1580-1631
- [TeamMemberDetailsDialogViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/TeamMemberDetailsDialogViewModel.cs) - DeleteNoteAsync lines 177-207
- [CircleView.axaml.cs](Tracker/ProCohere.Avalonia/Views/CircleView.axaml.cs) - DialogService wiring line 65

**Implementation:** Added ConfirmationService.ShowDestructiveConfirmationAsync before all delete operations. All other ViewModels already had confirmations.  
**Result:** All delete operations confirm before executing

---

### ✅ 7. F1 Help Window
**Completed:** ALREADY COMPLETE (verified Feb 4, 2026)  
**Location:** 
- [HelpWindow.axaml](Tracker/ProCohere.Avalonia/Views/HelpWindow.axaml)
- [MainWindow.axaml](Tracker/ProCohere.Avalonia/Views/MainWindow.axaml) line 30 (F1 keybinding)
- [HelpService.cs](Tracker/ProCohere.Avalonia/Services/HelpService.cs)
- [HelpWindowFactory.cs](Tracker/ProCohere.Avalonia/Services/HelpWindowFactory.cs)

**Status:** Fully implemented with search, topic tree, content display, F1 keybinding to open help  
**Result:** F1 opens help window with context-aware content

---

### ✅ 8. Password Reset Flow
**Completed:** February 4, 2026  
**Files Updated:**
- [AuthService.cs](Tracker/ProCohere.Avalonia/Services/AuthService.cs) lines 373-397 - ResetPasswordForEmailAsync method
- [LoginViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/LoginViewModel.cs) - Added password reset mode with toggle, email validation, success messages
- [LoginWindow.axaml](Tracker/ProCohere.Avalonia/Views/LoginWindow.axaml) - Two-panel toggle UI (login/reset)

**Implementation:** Toggle mode between login and password reset panels, Supabase Auth.ResetPasswordForEmail integration, email validation, color-coded success/error borders, auto-dismiss success message after 3 seconds  
**Result:** "Forgot password?" switches to reset mode, sends email, shows confirmation

---

### ✅ 9. Profile Picture Upload
**Completed:** February 4, 2026 (Enhanced)  
**Files Updated:**
- [EditAccountDialogViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/EditAccountDialogViewModel.cs) - Added IsUploadingAvatar, enhanced validation
- [EditAccountDialog.axaml](Tracker/ProCohere.Avalonia/Views/EditAccountDialog.axaml) - Added animated spinner overlay
- [AuthService.cs](Tracker/ProCohere.Avalonia/Services/AuthService.cs) - UploadAvatarAsync already complete

**Implementation:** Avatar upload was already functional. Enhancements: file size (5MB) and extension (JPG/PNG/GIF/WebP) validation before upload, animated rotating spinner during upload, file size display in KB, color-coded status messages, disabled buttons during upload  
**Result:** Can upload profile pictures with progress indication and validation feedback

---

### ✅ 10. Task-Goal Linking
**Completed:** February 4, 2026  
**Files Updated:**
- [TaskDetail.cs](Tracker/ProCohere.Avalonia/Models/TaskDetail.cs) - Added GoalId column mapping and GoalName property
- [AddTaskResult.cs](Tracker/ProCohere.Avalonia/Models/Dialogs/AddTaskResult.cs) - Added GoalId property
- [AddTaskDialogViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/Dialogs/AddTaskDialogViewModel.cs) - Added Goals collection, SelectedGoal, LoadGoalsAsync
- [AddTaskDialog.axaml](Tracker/ProCohere.Avalonia/Views/Dialogs/AddTaskDialog.axaml) - Added goal picker ComboBox
- [AddTaskDialog.axaml.cs](Tracker/ProCohere.Avalonia/Views/Dialogs/AddTaskDialog.axaml.cs) - Wired LoadGoalsAsync on dialog open
- [TaskService.cs](Tracker/ProCohere.Avalonia/Services/TaskService.cs) - Added goalId parameter to CreateTaskAsync
- [AppDialogService.cs](Tracker/ProCohere.Avalonia/Services/AppDialogService.cs) - Pass GoalId when creating/updating tasks
- [TaskDetailFlyout.axaml](Tracker/ProCohere.Avalonia/Views/Controls/TaskDetailFlyout.axaml) - Added Goal section to display linked goal
- [DashboardService.cs](Tracker/ProCohere.Avalonia/Services/DashboardService.cs) - Added EnrichTasksWithGoalNames to populate GoalName
- [20260204_add_goal_id_to_tasks.sql](Tracker/supabase/migrations/20260204_add_goal_id_to_tasks.sql) - **SCHEMA MIGRATION CREATED**

**Implementation:** Added goal picker to task creation/edit dialog using GoalsService.GetLinkableGoalsAsync. Tasks now store goal_id directly in database. Goal name displayed in task detail flyout. DashboardService enriches tasks with goal names on load.

**⚠️ SCHEMA CHANGE REQUIRED:** Run migration `20260204_add_goal_id_to_tasks.sql` to add `goal_id UUID` column to tasks table with foreign key to goals(id) and index.

**Result:** Tasks can be linked to goals during creation/editing, goal name shows in task detail view

---

## 🔴 CRITICAL PRIORITY - Core Functionality Gaps

---

### ✅ 11. Task-Meeting Linking
**Completed:** Previously implemented  
**Implementation:** 
- TaskDetail has `SourceType` and `SourceId` columns for meeting/agenda_item provenance
- TaskService.GetTasksForMeetingAsync() returns all linked tasks
- Tasks display their source in TaskDetailFlyout
**Result:** Tasks created from meetings link back via source tracking

---

### ✅ 12. Recurring Tasks
**Completed:** Previously implemented  
**Implementation:**
- AddTaskDialog.axaml has recurrence checkbox and pattern picker
- AddTaskDialogViewModel has IsRecurringEnabled, RecurrencePatternIndex, RecurrenceInterval, RecurrenceEndDate
- TaskDetail has IsRecurring, RecurrencePattern, RecurrenceInterval, RecurrenceEndDate, ParentRecurringTaskId
- Migration: 20260204_add_task_recurrence.sql
**Result:** Tasks can repeat daily/weekly/monthly

---

### ✅ 13. Recurring Meetings
**Completed:** Previously implemented  
**Implementation:**
- MeetingDetailsPanel.axaml has recurrence checkbox and pattern picker
- EditMeetingDialogViewModel has IsRecurringEnabled, RecurrencePatternIndex, ParseRecurrenceRule(), BuildRecurrenceRule()
- MeetingDetail has RecurrenceRule (RRULE format), RecurrenceDisplay property
**Result:** Can create weekly 1:1 series with recurrence patterns

---

### ✅ 14. Meeting Prep View
**Completed:** Previously implemented  
**Implementation:**
- PrepItemsPanel.axaml in Views/Controls/Meeting/
- EditMeetingDialog.axaml includes PrepItemsPanel in Prep tab
- MeetingPrepItemService for data operations
**Result:** See prep items when opening meeting

---

### ✅ 15. Standalone Goals View
**Completed:** Previously implemented  
**Implementation:**
- GoalsView.axaml with GoalsViewModel
- Full hierarchy view with filtering, health status
- Accessible via Goals navigation item (Ctrl+key)
**Result:** Can view/manage all goals in one place

---

### ✅ 16. Standalone Metrics View
**Completed:** Previously implemented  
**Implementation:**
- MetricsView.axaml with MetricsViewModel
- Full metrics browse with trends, history
- Accessible via Metrics navigation item
**Result:** Can view metric trends and history

---

## 🟡 MEDIUM PRIORITY - Advanced Features

### ✅ 17. Calendar Integration (Google)
**Completed:** February 4, 2026  
**Status:** Core implementation complete (OAuth credentials needed)  
**Implementation:**
- Database migration: `20260204_add_meeting_calendar_sync.sql`
- MeetingDetail model: Added calendar_event_id, calendar_provider, calendar_link_id, last_synced_at, sync_status
- GoogleCalendarService: OAuth flow, CreateEventAsync, UpdateEventAsync, DeleteEventAsync
- CalendarIntegrationService: ConnectGoogleAsync, DisconnectGoogleAsync, GetGoogleIntegrationAsync
- MeetingService: SyncToGoogleCalendarAsync, UnsyncFromGoogleCalendarAsync, auto-sync on create/update
- NuGet: Google.Apis.Calendar.v3, Google.Apis.Auth installed

**Remaining:** Replace placeholder OAuth credentials (YOUR_CLIENT_ID/SECRET) in GoogleCalendarService.cs with real Google Cloud Console credentials

**Acceptance:** Meetings sync to/from Google Calendar ✅ (infrastructure ready, needs OAuth setup)

---

### ✅ 18. Calendar Integration (Microsoft/Outlook)
**Completed:** February 2026  
**Implementation:**
- MicrosoftCalendarService.cs with full OAuth flow via Microsoft Graph API
- CalendarIntegrationService: ConnectMicrosoftAsync, DisconnectMicrosoftAsync, GetMicrosoftIntegrationAsync
- MeetingService: SyncToMicrosoftCalendarAsync, UnsyncFromMicrosoftCalendarAsync
- appsettings.json: MicrosoftCalendar section for OAuth credentials
- NuGet: Microsoft.Graph, Azure.Identity installed
**Result:** Meetings sync to/from Microsoft 365 Calendar ✅

---

### ✅ 19. Kudos System
**Completed:** February 2026  
**Implementation:**
- KudosService.cs (ProCohere.Avalonia) with CreateKudosAsync, GetKudosReceivedAsync
- AddKudosDialog.axaml with AddKudosDialogViewModel
- CircleViewModel integration: loads kudos for member detail
- Integration with MessageService for Slack/Teams delivery
**Result:** Can send kudos with Slack/Teams notifications ✅

---

### ✅ 20. Slack Quick Message
**Completed:** February 2026  
**Implementation:**
- SlackService.cs singleton with SendDirectMessageAsync
- QuickMessageDialog.axaml with provider selection dropdown
- QuickMessageDialogViewModel with Slack integration
- MessageService routes "Slack" provider to SlackService.Instance
- AppDialogService.ShowQuickMessageDialogAsync() wired up
**Result:** Can send Slack message from team member detail ✅

---

### ✅ 21. Microsoft Teams Quick Message
**Completed:** February 2026  
**Implementation:**
- TeamsService.cs singleton implementing IMessageService
- Reuses MicrosoftCalendarService authentication (shared Graph client)
- QuickMessageDialog supports Teams as provider option
- MessageService routes "Teams" provider to TeamsService.Instance
**Result:** Can send Teams message from team member detail ✅

---

### ✅ 22. Pulse Survey Creation
**Completed:** Previously implemented  
**Implementation:**
- CreateSurveyDialog.axaml with full survey builder UI
- CreateSurveyDialogViewModel with question management
- AppDialogService.ShowCreateSurveyDialogAsync()
**Result:** Can create surveys with multiple question types

---

### ✅ 23. Pulse Survey Distribution
**Completed:** Previously implemented  
**Implementation:**
- SurveyService with SendSurveyAsync and schedule functionality
- PulseView integration for sending surveys
**Result:** Surveys sent to team members

---

### ✅ 24. Pulse Survey Analytics
**Completed:** Previously implemented  
**Implementation:**
- SurveyAnalyticsDialog.axaml with charts and response breakdown
- SurveyAnalyticsViewModel with data loading
- SurveyService.GetSurveyAnalyticsAsync()
**Result:** Can see survey response trends

---

### ✅ 25. Predictive Analytics - Trend Analyzer
**Completed:** Previously implemented  
**Implementation:**
- TrendAnalyzer.cs service with Analyze(), ProjectValue()
- Used by MetricsService.GetTrendAnalysisAsync()
- Integrated into MetricsViewModel for trend display
**Result:** Shows metric trend predictions

---

### ✅ 26. Predictive Analytics - Trajectory Predictor
**Completed:** Previously implemented  
**Implementation:**
- TrajectoryPredictor.cs service with PredictGoalTrajectoryAsync()
- Used by GoalsService for completion probability
- Integrated into goal detail views
**Result:** Predicts goal completion probability

---

### ✅ 27. Predictive Analytics - What-If Simulator
**Completed:** Previously implemented  
**Implementation:**
- WhatIfSimulator.cs service
- WhatIfDialogViewModel for scenario modeling
- Integrated into goals/metrics views
**Result:** Can model "what if we hit X by Y date"

---

### ✅ 28. Reports View
**Completed:** February 5, 2026  
**Implementation:**
- ReportsView.axaml with tab-based navigation (Overview, Goals, Metrics, Tasks, Meetings, Team)
- ReportsViewModel with LiveCharts2 integration
- ReportService for data aggregation with date range filtering
- LiveCharts2 NuGet package for charts
**Result:** Can view analytics with charts and export data

---

### ✅ 29. Excel Export
**Completed:** February 5, 2026  
**Implementation:**
- ExcelExportService.cs singleton with EPPlus 7.5.2
- Export methods: ExportTeamMembersAsync, ExportMeetingsAsync, ExportTasksAsync, ExportGoalsAsync, ExportMetricsAsync, ExportProjectsAsync
- ExportAllDataAsync creates multi-sheet workbook with Summary page
- ReportsViewModel: ExportAllDataAsync, ExportCurrentTabAsync commands
- ReportsView: Export dropdown menu (Export Current Tab, Export All Data)
- File picker integration with suggested filenames
- Toast notifications on success/failure
**Result:** Can export team/goals/metrics/tasks/meetings/projects to Excel ✅

---

### ✅ 30. PDF Export
**Completed:** February 5, 2026  
**Implementation:**
- PdfExportService.cs singleton with QuestPDF 2024.12.2
- Export methods: ExportTeamMembersAsync, ExportMeetingsAsync, ExportTasksAsync, ExportGoalsAsync, ExportMetricsAsync, ExportProjectsAsync
- ExportAllDataAsync creates multi-page PDF with title page and entity sections
- Professional styling: Blue-500 primary color, branded headers, footers with page numbers
- ReportsViewModel: ExportAllDataToPdfAsync, ExportCurrentTabToPdfAsync commands
- ReportsView: Nested export menu (Excel/.xlsx and PDF/.pdf submenus)
- File picker integration with suggested filenames
- Toast notifications on success/failure
**Result:** Can export reports to PDF with professional formatting ✅

---

### ✅ 31. CSV Export
**Completed:** February 5, 2026  
**Implementation:**
- CsvExportService.cs singleton with proper CSV escaping (quotes, commas, newlines)
- Export methods: ExportTeamMembersAsync, ExportMeetingsAsync, ExportTasksAsync, ExportGoalsAsync, ExportMetricsAsync, ExportProjectsAsync
- ExportAllDataAsync creates ZIP file with all CSVs + README.txt summary
- Uses System.IO.Compression for ZIP creation (no external NuGet)
- ReportsViewModel: ExportAllDataToCsvAsync, ExportCurrentTabToCsvAsync commands
- ReportsView: Nested export menu (Excel/.xlsx, PDF/.pdf, CSV/.csv submenus)
- File picker integration with .csv and .zip filters
- Toast notifications on success/failure
**Result:** Can export individual reports to CSV or all data to ZIP ✅

---

## 🟢 LOW PRIORITY - Nice to Have

### ⏸️ 32. Admin Window
**Status:** DEFERRED - Moving to web  
**Reason:** Admin functionality will be implemented in a separate web admin portal rather than the desktop app. Supabase dashboard can be used in the interim.

---

### ⏸️ 33. Setup Wizard
**Status:** DEFERRED - Not needed  
**Reason:** ProCohere.Avalonia uses Supabase Auth with simple login. No first-run wizard required - users sign up/login and immediately access the app. Organization setup handled at database level.

---

### ✅ 34. About Dialog
**Status:** COMPLETE (Feb 5, 2026)  
**Files Created:**
- [AboutDialog.axaml](Tracker/ProCohere.Avalonia/Views/Dialogs/AboutDialog.axaml)
- [AboutDialogViewModel.cs](Tracker/ProCohere.Avalonia/ViewModels/Dialogs/AboutDialogViewModel.cs)
- [AppDialogService.cs](Tracker/ProCohere.Avalonia/Services/AppDialogService.cs) - ShowAboutDialogAsync

**Implementation:** Pro Cohere logo + version, Prickly Cactus logo, © 2026 Prickly Cactus Software, Website/Support links, accessible from Account menu

---

### 🔮 35-36. ACCESSIBILITY EPIC (DEFERRED)
**Status:** DEFERRED - Comprehensive plan documented below  
**Impact:** Keyboard-only and screen reader users cannot fully use app  
**Merged:** Items #35 (Keyboard Navigation) + #36 (Screen Reader Support)

#### Why Deferred
- Requires touching ALL 31 dialogs + 7 views + windows
- Each file needs comprehensive review
- Better to do as dedicated accessibility sprint

#### Per-File Checklist
| Category | Item | Implementation |
|----------|------|----------------|
| **Keyboard** | Escape closes | `<KeyBinding Gesture="Escape" Command="{Binding CloseCommand}"/>` |
| **Keyboard** | Enter submits | `<KeyBinding Gesture="Enter" Command="{Binding SaveCommand}"/>` (form dialogs) |
| **Keyboard** | Tab order | `TabIndex` on all focusable controls, logical flow |
| **Keyboard** | Focusable controls | `IsTabStop="True"` on interactive, `IsTabStop="False"` on decorative |
| **Keyboard** | Arrow navigation | Lists/grids respond to arrow keys |
| **Screen Reader** | Control names | `AutomationProperties.Name="Save Goal"` on buttons |
| **Screen Reader** | Form labels | `AutomationProperties.LabeledBy` or `AutomationProperties.Name` on TextBoxes |
| **Screen Reader** | Help text | `AutomationProperties.HelpText` for icons/non-obvious controls |
| **Screen Reader** | Live regions | `AutomationProperties.LiveSetting` for dynamic status messages |
| **Visual** | Focus indicators | Ensure FocusAdorner visible (global style) |
| **Visual** | Min touch targets | Buttons at least 32x32 (ideally 44x44) |
| **Other** | Error announcements | Validation errors accessible to screen readers |

#### Implementation Phases

**Phase 1: Global Foundation**
- Add global FocusAdorner style in App.axaml
- Consider creating shared Escape/Enter attached behavior

**Phase 2: Dialogs (31 files)**
1. AboutDialog
2. AddFeedbackDialog
3. AddGoalDialog
4. AddKudosDialog
5. AddMetricDialog
6. AddNoteDialog
7. ConfirmationDialog
8. CreateProjectDialog
9. CreateSurveyDialog
10. DeferAgendaItemDialog
11. EditAccountDialog
12. EditAgendaItemDialog
13. EditGoalDialog
14. EditMeetingDialog
15. EditMetricDialog
16. EditProjectDialog
17. EditTeamMemberDialog
18. EntityPickerDialog
19. GoalDetailsDialog
20. GoalPickerDialog
21. InviteTeamMemberDialog
22. MetricDetailsDialog
23. QuickMessageDialog
24. RecordOutcomeDialog
25. ReportsConfigDialog
26. ScheduleMeetingDialog
27. SurveyAnalyticsDialog
28. SurveyResponseDialog
29. TaskDetailsDialog
30. TeamMemberDetailsDialog
31. WorkItemDetailsDialog

**Phase 3: Main Views (7 files)**
- BriefingView
- MeView
- CircleView
- ProjectsView
- PulseView
- ChronicleView
- ReportsView

**Phase 4: Windows & Other**
- MainWindow (already has keyboard shortcuts)
- LoginWindow
- HelpWindow
- SettingsView

#### Acceptance Criteria
- [ ] Escape closes all dialogs
- [ ] Enter submits all form dialogs
- [ ] Tab navigates logically through all controls
- [ ] Focus indicator visible on all interactive elements
- [ ] All buttons have AutomationProperties.Name
- [ ] All form fields have accessible labels
- [ ] Screen reader can announce all UI elements
- [ ] App passes basic accessibility audit

---

### 37. Animations & Transitions
**Status:** Minimal  
**Impact:** Feels static  
**Task:** Add smooth transitions, micro-interactions

**Acceptance:** Polished feel with smooth animations

---

### 38. Real-time Sync (Supabase Realtime)
**Status:** Not implemented  
**Impact:** No live updates  
**Task:** Add Supabase realtime subscriptions

**Acceptance:** Changes from other users appear live

---

### 39. Offline Mode
**Status:** Not implemented  
**Impact:** Requires internet  
**Task:** Add local caching and sync queue

**Acceptance:** Can work offline, syncs when online

---

### 40. Performance - Virtualized Lists
**Status:** Partial  
**Impact:** Slow with large datasets  
**Task:** Ensure all lists use VirtualizingStackPanel

**Acceptance:** Smooth scrolling with 1000+ items

---

### 41. Performance - Lazy Loading
**Status:** Partial  
**Impact:** Slow initial load  
**Task:** Load data on-demand

**Acceptance:** Fast startup time

---

### 42. macOS Testing
**Status:** Not done  
**Impact:** May have bugs on macOS  
**Task:** Test and fix macOS-specific issues

**Acceptance:** Runs smoothly on macOS

---

### 43. Linux Testing
**Status:** Not done  
**Impact:** May have bugs on Linux  
**Task:** Test and fix Linux-specific issues

**Acceptance:** Runs smoothly on Linux

---

## 📚 HELP DOCUMENTATION NEEDED

### Priority 1 - Core User Guides
1. **Getting Started Guide** - Onboarding, basic concepts
2. **Briefing Guide** - Daily briefing, insights, actions
3. **Circle Guide** - Team management, relationships, 1:1s
4. **Me Guide** - Personal dashboard, calendar, tasks
5. **Pulse Guide** - Projects, goals, metrics, tasks
6. **Meetings Guide** - OneOnOne setup, agendas, outcomes

### Priority 2 - Feature Deep Dives
7. **Goals & Metrics Guide** - Goal lifecycle, health, metrics tracking
8. **Chronicle Guide** - Note taking, entity linking
9. **Projects Guide** - Project management, work items
10. **Settings Guide** - Account, preferences, theme
11. **AI Chat Guide** - Using the AI assistant
12. ✅ **Insights Guide** - COMPLETE ([INSIGHTS_USER_GUIDE.md](Docs/INSIGHTS_USER_GUIDE.md))

### Priority 3 - Reference
13. **Keyboard Shortcuts Reference** - All hotkeys
14. **Troubleshooting Guide** - Common issues/solutions
15. **FAQ** - Frequently asked questions
16. **Glossary** - Terminology reference

### Priority 4 - Advanced
17. **Admin Guide** - Organization management (when admin features exist)
18. **Integrations Guide** - Calendar, Slack, Teams setup
19. **API Guide** - Developer documentation (future)
20. **Video Tutorial Scripts** - Step-by-step walkthroughs

---

## 📊 PROGRESS SUMMARY

| Category | Complete | In Progress | Not Started | Total |
|----------|----------|-------------|-------------|-------|
| **Core CRUD** | 90% | 5% | 5% | 100% |
| **UI Polish** | 85% | 10% | 5% | 100% |
| **Auth/Account** | 95% | 5% | 0% | 100% |
| **Platform Work** | 5% | 0% | 95% | 100% |
| **WPF Migration** | 100% | 0% | 0% | 100% |
| **Advanced Features** | 10% | 5% | 85% | 100% |
| **Reports/Export** | 0% | 0% | 100% | 100% |
| **Admin** | 0% | 0% | 100% | 100% |
| **Help Docs** | 5% | 0% | 95% | 100% |

**Overall: ~68% Complete**

### Recent Progress (Feb 4, 2026)
✅ Items #1-10 completed (Core CRUD dialogs, System Tray, Confirmations, Password Reset, Avatar Upload, Task-Goal Linking)

---

## 🎯 RECOMMENDED ATTACK ORDER

### ✅ Sprint 1: Make It Work (COMPLETE - Feb 4, 2026)
1. ✅ Register Insight Analyzers (#1)
2. ✅ Initialize System Tray (#5)
3. ✅ AddGoalDialog (#2)
4. ✅ AddMetricDialog (#3)
5. ✅ AddFeedbackDialog (#4)
6. ✅ Wire Soft Delete Confirmations (#6)
7. ✅ F1 Help Window (#7 - was already complete)
8. ✅ Password Reset (#8)
9. ✅ Profile Picture Upload (#9)

**Result:** Core features fully functional ✅

---

### Sprint 2: Expand Core Features (NEXT)
10. Task-Goal Linking (#10)
11. Task-Meeting Linking (#11)
12. Recurring Tasks (#12)
13. Recurring Meetings (#13)
14. Meeting Prep View (#14)

**Goal:** Feature completeness for core workflows

---

### Sprint 3: Standalone Views
15. Standalone Goals View (#15)
16. Standalone Metrics View (#16)

**Goal:** Dedicated management screens

---

### Sprint 4: Help Users (Documentation)
- Getting Started Guide
- Briefing Guide
- Circle Guide
- Me Guide
- Pulse Guide
- Meetings Guide
- Goals & Metrics Guide
- Chronicle Guide

**Goal:** Users can self-serve

---

### Sprint 5: Advanced Features
17-27. Calendar integration, Kudos, Surveys, Predictive Analytics

**Goal:** Differentiation features

---

### Sprint 6: Polish & Performance
32-43. Animations, keyboard nav, accessibility, performance

**Goal:** Production-ready quality

---

## 📝 NOTES

- **System Tray, Toasts, Reminders, Help Service** - All built, just need wiring
- **InsightEngine** - Complete but not registered on startup (5-minute fix)
- **Chronicle View** - Exists (470 lines XAML), just needs testing
- **Projects** - Fully implemented
- **AI Services** - Chat, context, functions all exist

**The app is much further along than the roadmap suggested!**

Most "missing" items are either:
1. Not wired/registered (quick fixes)
2. Dialog creation (1-2 hours each)
3. Documentation (time-consuming but straightforward)

**Next recommended action:** Fix the 5 Critical items (#1-5) to unlock core functionality.
