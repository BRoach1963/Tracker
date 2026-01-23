# ProCohere Avalonia Development Pipeline

> **Document Created:** January 22, 2026  
> **Project:** ProCohere.Avalonia (Cross-Platform Desktop App)  
> **Target:** Full feature parity with WPF Tracker + new capabilities

---

## Executive Summary

This document outlines the complete development pipeline for ProCohere.Avalonia, organized into prioritized phases. The goal is to achieve a fully functional, polished cross-platform application that matches and eventually exceeds the capabilities of the legacy WPF Tracker application.

**Current Status:** ~75% core infrastructure complete, UI scaffolding in place, CRUD operations partially implemented.

---

## Phase 1: Team Members - Complete CRUD Operations
**Priority:** CRITICAL | **Status:** In Progress

### 1.1 Team Member Management
| Task | Status | Notes |
|------|--------|-------|
| View team members list | ✅ Done | CircleView shows team list |
| View team member detail flyout | ✅ Done | TeamMemberDetailFlyout implemented |
| Add new team member | 🔄 Partial | Dialog exists, needs wiring |
| Edit team member | 🔄 Partial | EditTeamMemberDialog exists |
| Delete team member (soft delete) | ❌ Not Done | Need confirmation + soft delete |
| Archive/restore team member | ❌ Not Done | |
| Team member profile picture/avatar | 🔄 Partial | Display not working correctly |
| Team member relationship types | ❌ Not Done | Manager/Direct Report/Peer/Other |

### 1.2 Team Member Service Operations
| Task | Status | Notes |
|------|--------|-------|
| `GetAllTeamMembersAsync()` | ✅ Done | TeamService.cs |
| `GetTeamMemberByIdAsync()` | ✅ Done | |
| `CreateTeamMemberAsync()` | 🔄 Partial | Needs validation |
| `UpdateTeamMemberAsync()` | 🔄 Partial | |
| `DeleteTeamMemberAsync()` | ❌ Not Done | Soft delete implementation |
| Team member search/filter | ❌ Not Done | |

---

## Phase 2: Core Entity CRUD - Tasks, Meetings, Feedback, Goals, Metrics, Notes
**Priority:** CRITICAL | **Status:** Partially Complete

### 2.1 Tasks
| Task | Status | Notes |
|------|--------|-------|
| View tasks list (by status, due date) | ✅ Done | PulseView, MeView |
| Task detail flyout | ✅ Done | TaskDetailFlyout |
| Add new task | ✅ Done | AddTaskDialog |
| Edit task | 🔄 Partial | Need full edit capability |
| Delete task (soft delete) | ❌ Not Done | |
| Task status changes (Complete, etc.) | 🔄 Partial | |
| Task priority management | ❌ Not Done | |
| Task assignee management | ❌ Not Done | |
| Task-Meeting linking | ❌ Not Done | Link tasks to meetings |
| Task-Goal linking | ❌ Not Done | Link tasks to goals |
| Recurring tasks | ❌ Not Done | |
| Task due date reminders | ❌ Not Done | See Phase 6 |

### 2.2 Meetings
| Task | Status | Notes |
|------|--------|-------|
| View meetings list | ✅ Done | MeView calendar views |
| Meeting detail flyout | ✅ Done | MeetingDetailFlyout |
| Add new meeting | 🔄 Partial | Dialog exists |
| Edit meeting | ✅ Done | EditMeetingDialog |
| Delete meeting (soft delete) | ❌ Not Done | |
| Meeting types (OneOnOne, Team, etc.) | ✅ Done | |
| Agenda items management | ✅ Done | AgendaItemCard, AddAgendaItem |
| Record agenda item outcomes | ✅ Done | RecordOutcomeDialog |
| Defer agenda items | ✅ Done | DeferAgendaItemDialog |
| Meeting templates | 🔄 Partial | ApplyTemplateDialog exists |
| Meeting prep view | ❌ Not Done | WPF has MeetingPrepService |
| Meeting notes/summary | ❌ Not Done | |
| Recurring meetings | ❌ Not Done | |
| Meeting series management | ❌ Not Done | |

### 2.3 Feedback
| Task | Status | Notes |
|------|--------|-------|
| View feedback list (given/received) | ✅ Done | MeView, CircleView |
| Feedback detail flyout | 🔄 Partial | FeedbackCard exists |
| Add new feedback | ❌ Not Done | AddFeedbackDialog not in Avalonia |
| Edit feedback | ❌ Not Done | |
| Delete feedback (soft delete) | ❌ Not Done | |
| Feedback categories/types | ❌ Not Done | Praise, Constructive, etc. |
| Link feedback to meetings | ❌ Not Done | |
| Feedback acknowledgment | ❌ Not Done | |

### 2.4 Goals
| Task | Status | Notes |
|------|--------|-------|
| View goals list | ✅ Done | GoalsViewModel, MeView |
| Goal detail flyout | ✅ Done | GoalDetailFlyout |
| Add new goal | ❌ Not Done | Need AddGoalDialog |
| Edit goal | ✅ Done | EditGoalDialog, GoalEditorFlyout |
| Delete goal (soft delete) | ❌ Not Done | |
| Goal health status | ✅ Done | HealthChangeDialog |
| Goal lifecycle management | ✅ Done | LifecycleChangeDialog |
| Goal targets (Key Results) | 🔄 Partial | Need full CRUD |
| Goal progress tracking | 🔄 Partial | |
| Goal alignment (parent/child) | ❌ Not Done | |
| Goal-Task linking | ❌ Not Done | |
| Goal categories | ❌ Not Done | |

### 2.5 Metrics (KPIs)
| Task | Status | Notes |
|------|--------|-------|
| View metrics list | ✅ Done | MetricsViewModel |
| Metric detail view | 🔄 Partial | |
| Add new metric | ❌ Not Done | Need AddMetricDialog |
| Edit metric | ✅ Done | EditMetricDialog |
| Delete metric (soft delete) | ❌ Not Done | |
| Update metric values | ✅ Done | UpdateMetricValueDialog |
| Metric calculations | 🔄 Partial | IMetricsService |
| Metric trends/charts | ❌ Not Done | |
| Metric thresholds/alerts | ❌ Not Done | |
| Metric-Goal association | ❌ Not Done | |

### 2.6 Notes (Chronicle)
| Task | Status | Notes |
|------|--------|-------|
| View notes list | 🔄 Partial | ChronicleViewModel exists |
| Note detail flyout | ✅ Done | NoteDetailFlyout |
| Add new note | ❌ Not Done | |
| Edit note | ❌ Not Done | |
| Delete note (soft delete) | ❌ Not Done | |
| Note categories/tags | ❌ Not Done | |
| Note search | ❌ Not Done | |
| Link notes to entities | ❌ Not Done | Link to meetings, team members |
| Rich text editing | ❌ Not Done | |

---

## Phase 3: UI Completion & Polish
**Priority:** HIGH | **Status:** In Progress

### 3.1 Main Views
| View | Status | Notes |
|------|--------|-------|
| LoginWindow | ✅ Done | Auth flow complete |
| MainWindow | ✅ Done | Navigation, theme support |
| BriefingView | ✅ Done | Daily briefing |
| MeView | ✅ Done | Personal dashboard, calendar views |
| CircleView | ✅ Done | Team management |
| PulseView | ✅ Done | Tasks, activity feed |
| SettingsView | ✅ Done | User settings |
| SplashWindow | ✅ Done | App loading |

### 3.2 Views Needing Completion
| View | Status | Notes |
|------|--------|-------|
| ChronicleView | ❌ Not Started | ViewModel exists, needs XAML |
| GoalsView (standalone) | ❌ Not Started | Full goals management |
| MetricsView (standalone) | ❌ Not Started | Full metrics management |
| ReportsView | ❌ Not Started | See Phase 7 |
| AdminView | ❌ Not Started | Admin features if needed |

### 3.3 Dialogs Completion
| Dialog | Status | Notes |
|--------|--------|-------|
| AddTaskDialog | ✅ Done | |
| EditGoalDialog | ✅ Done | |
| EditMeetingDialog | ✅ Done | |
| EditMetricDialog | ✅ Done | |
| EditTeamMemberDialog | ✅ Done | |
| EditAccountDialog | ✅ Done | User profile |
| ApplyTemplateDialog | ✅ Done | Meeting templates |
| DeferAgendaItemDialog | ✅ Done | |
| RecordOutcomeDialog | ✅ Done | |
| UpdateMetricValueDialog | ✅ Done | |
| AddFeedbackDialog | ❌ Not Done | Need to create |
| AddGoalDialog | ❌ Not Done | Need to create |
| AddMetricDialog | ❌ Not Done | Need to create |
| AddNoteDialog | ❌ Not Done | Need to create |
| ConfirmationDialog | ❌ Not Done | Generic confirmation |
| MessageBoxDialog | ❌ Not Done | Generic messages |

### 3.4 UI Polish Tasks
| Task | Status | Notes |
|------|--------|-------|
| Consistent styling across all views | 🔄 In Progress | |
| Dark/Light theme support | ✅ Done | ThemeService |
| Loading states/spinners | 🔄 Partial | |
| Empty state messages | 🔄 Partial | |
| Error state handling | 🔄 Partial | |
| Keyboard navigation | ❌ Not Done | |
| Accessibility (screen readers) | ❌ Not Done | |
| Responsive layouts | 🔄 Partial | |
| Animation/transitions | ❌ Not Done | |
| Icon consistency | 🔄 In Progress | |

---

## Phase 4: Account & User Profile
**Priority:** HIGH | **Status:** Partially Complete

### 4.1 Authentication
| Task | Status | Notes |
|------|--------|-------|
| Login with email/password | ✅ Done | AuthService |
| Sign up flow | ✅ Done | |
| Password reset | ❌ Not Done | |
| Session persistence | ✅ Done | Credential storage |
| Auto-login on app start | ✅ Done | |
| Logout | ✅ Done | |
| Session expiry handling | 🔄 Partial | |
| Multi-factor authentication | ❌ Not Done | Future |

### 4.2 User Profile
| Task | Status | Notes |
|------|--------|-------|
| View profile | ✅ Done | EditAccountDialog |
| Edit profile (name, email) | 🔄 Partial | |
| Profile picture upload | ❌ Not Done | |
| Profile picture display | 🔄 Broken | Avatar display debugging |
| Timezone settings | ❌ Not Done | |
| Notification preferences | ❌ Not Done | |
| Language/locale settings | ❌ Not Done | |

### 4.3 User Settings
| Task | Status | Notes |
|------|--------|-------|
| Local settings storage | ✅ Done | LocalSettingsService |
| Theme preference | ✅ Done | |
| Default view preference | ❌ Not Done | |
| Calendar preferences | ❌ Not Done | |
| Reminder preferences | ❌ Not Done | |
| Export settings | ❌ Not Done | |
| Import settings | ❌ Not Done | |

### 4.4 Subscription & Licensing
| Task | Status | Notes |
|------|--------|-------|
| View current plan | ❌ Not Done | WPF has SubscriptionService |
| Plan upgrade flow | ❌ Not Done | UpgradePlanDialog in WPF |
| Usage limits display | ❌ Not Done | |
| Credit purchase | ❌ Not Done | For AI features |
| License activation | ❌ Not Done | For enterprise |

---

## Phase 5: Additional Avalonia Platform Work
**Priority:** MEDIUM | **Status:** Not Started

### 5.1 Cross-Platform Considerations
| Task | Status | Notes |
|------|--------|-------|
| Windows-specific features | 🔄 In Progress | |
| macOS compatibility testing | ❌ Not Done | |
| Linux compatibility testing | ❌ Not Done | |
| Platform-specific file paths | ❌ Not Done | |
| Platform-specific notifications | ❌ Not Done | |
| Keyboard shortcuts per platform | ❌ Not Done | Cmd vs Ctrl |

### 5.2 Data Synchronization
| Task | Status | Notes |
|------|--------|-------|
| Real-time data sync | ❌ Not Done | Supabase realtime |
| Offline mode | ❌ Not Done | Local caching |
| Conflict resolution | ❌ Not Done | |
| Background sync | ❌ Not Done | |

### 5.3 Performance Optimization
| Task | Status | Notes |
|------|--------|-------|
| Virtualized lists | ❌ Not Done | For large data sets |
| Lazy loading | 🔄 Partial | |
| Memory optimization | ❌ Not Done | |
| Startup time optimization | ❌ Not Done | |
| Database query optimization | ❌ Not Done | |

---

## Phase 6: WPF Feature Migration - Core Missing Features
**Priority:** MEDIUM-HIGH | **Status:** Not Started

### 6.1 System Tray Integration
| Task | Status | WPF Reference |
|------|--------|---------------|
| System tray icon | ❌ Not Done | SystemTrayService.cs |
| Tray context menu | ❌ Not Done | Show/Hide, Settings, Exit |
| Minimize to tray | ❌ Not Done | |
| Tray notifications | ❌ Not Done | |
| Reminders toggle in tray | ❌ Not Done | |

### 6.2 Reminder System
| Task | Status | WPF Reference |
|------|--------|---------------|
| Background reminder service | ❌ Not Done | ReminderService.cs |
| Meeting reminders | ❌ Not Done | X minutes before meeting |
| Task due date reminders | ❌ Not Done | |
| Goal check-in reminders | ❌ Not Done | |
| Engagement reminders | ❌ Not Done | "Talk to X, it's been 30 days" |
| Snooze functionality | ❌ Not Done | |
| Reminder scheduling | ❌ Not Done | |
| AddReminderDialog | ❌ Not Done | |

### 6.3 Toast Notifications
| Task | Status | WPF Reference |
|------|--------|---------------|
| Toast notification window | ❌ Not Done | TrackerToast.xaml |
| Toast stacking/queue | ❌ Not Done | NotificationManager.cs |
| Toast types (Info, Success, Warning, Error) | ❌ Not Done | |
| Toast animations | ❌ Not Done | Fade in/out, slide |
| Pause on hover | ❌ Not Done | |
| Click actions | ❌ Not Done | Navigate to related item |
| Native OS notifications | ❌ Not Done | Optional Windows/macOS |

### 6.4 Context-Sensitive Help System
| Task | Status | WPF Reference |
|------|--------|---------------|
| Help service | ❌ Not Done | HelpService.cs |
| Help window | ❌ Not Done | HelpWindow.xaml |
| Help topic registry | ❌ Not Done | HelpTopicRegistry.cs |
| HelpContext attributes | ❌ Not Done | [HelpContext("topic")] |
| Help attached properties | ❌ Not Done | HelpProperties.cs |
| F1 key binding | ❌ Not Done | Global help key |
| Markdown help content | ❌ Not Done | MarkdownRenderer.cs |
| Help search | ❌ Not Done | |
| Context resolution | ❌ Not Done | From focused element |
| Help topic cache (LRU) | ❌ Not Done | LruCache.cs |

---

## Phase 7: WPF Feature Migration - Advanced Features
**Priority:** MEDIUM | **Status:** Not Started

### 7.1 Calendar Integration
| Task | Status | WPF Reference |
|------|--------|---------------|
| Calendar sync manager | ❌ Not Done | CalendarSyncManager.cs |
| Google Calendar OAuth | ❌ Not Done | Google/ services |
| Google Calendar sync | ❌ Not Done | Read/write events |
| Outlook/Microsoft 365 OAuth | ❌ Not Done | Microsoft365/ services |
| Outlook Calendar sync | ❌ Not Done | Read/write events |
| Two-way sync | ❌ Not Done | |
| Sync conflict resolution | ❌ Not Done | |
| Apple Calendar (iCal) | ❌ Not Done | Future |

### 7.2 Help Bot / AI Assistant
| Task | Status | WPF Reference |
|------|--------|---------------|
| Help bot window | ❌ Not Done | HelpBotWindow.xaml |
| Help bot view model | ❌ Not Done | HelpBotViewModel.cs |
| AI chat interface | ❌ Not Done | Multi-provider support |
| RAG context building | ❌ Not Done | SmartContextBuilder.cs |
| Help bot context service | ❌ Not Done | HelpBotContextService.cs |
| Chat history | ❌ Not Done | |
| AI model selection | ❌ Not Done | OpenAI, Anthropic, Gemini |
| Token/credit tracking | ❌ Not Done | AIUsageTracker.cs |

### 7.3 AI/ML Features
| Task | Status | WPF Reference |
|------|--------|---------------|
| Insight engine | ❌ Not Done | InsightEngine.cs |
| AI insight generator | ❌ Not Done | AIInsightGenerator.cs |
| Vector store | ❌ Not Done | PostgresVectorStore.cs |
| Embedding service | ❌ Not Done | EmbeddingService.cs |
| Entity indexers | ❌ Not Done | GoalIndexer, MeetingIndexer, etc. |
| Insight analyzers | ❌ Not Done | Various in Analyzers/ |
| AI functions service | ❌ Not Done | AIFunctionService.cs |

### 7.4 Predictive Analytics
| Task | Status | WPF Reference |
|------|--------|---------------|
| Predictive analytics service | ❌ Not Done | PredictiveAnalyticsService.cs |
| Trend analyzer | ❌ Not Done | TrendAnalyzer.cs |
| Trajectory predictor | ❌ Not Done | TrajectoryPredictor.cs |
| Recommendation engine | ❌ Not Done | RecommendationEngine.cs |
| What-if simulator | ❌ Not Done | WhatIfSimulator.cs |
| Data sufficiency checker | ❌ Not Done | DataSufficiencyChecker.cs |
| Progress snapshots | ❌ Not Done | ProgressSnapshotService.cs |

### 7.5 Kudos / Recognition System
| Task | Status | WPF Reference |
|------|--------|---------------|
| Kudos service | ❌ Not Done | KudosService.cs |
| Send kudos dialog | ❌ Not Done | SendKudosDialog.xaml |
| Kudos delivery providers | ❌ Not Done | IKudosDeliveryProvider |
| Slack delivery | ❌ Not Done | SlackDeliveryProvider.cs |
| Teams delivery | ❌ Not Done | TeamsDeliveryProvider.cs |
| Email delivery | ❌ Not Done | |
| Kudos history | ❌ Not Done | |

### 7.6 Communication Integration
| Task | Status | WPF Reference |
|------|--------|---------------|
| Quick message dialog | ❌ Not Done | QuickMessageDialog.xaml |
| Slack integration | ❌ Not Done | Slack/ services |
| Microsoft Teams integration | ❌ Not Done | |
| Email integration | ❌ Not Done | |

### 7.7 Pulse Surveys
| Task | Status | WPF Reference |
|------|--------|---------------|
| Survey creation | ❌ Not Done | PulseSurvey models |
| Survey distribution | ❌ Not Done | |
| Survey responses | ❌ Not Done | |
| Survey analytics | ❌ Not Done | PulseSurveyIndexer.cs |
| Survey templates | ❌ Not Done | |

### 7.8 Projects Management
| Task | Status | WPF Reference |
|------|--------|---------------|
| Projects UI | ❌ Not Done | AddProjectDialog.xaml |
| Project-Task association | ❌ Not Done | |
| Project-Goal association | ❌ Not Done | |
| Project timelines | ❌ Not Done | |
| Project milestones | ❌ Not Done | |

---

## Phase 8: Reports & Export
**Priority:** MEDIUM | **Status:** Not Started

### 8.1 Reports Dialog/View
| Task | Status | WPF Reference |
|------|--------|---------------|
| Reports view/dialog | ❌ Not Done | ReportsDialog.xaml |
| Report templates | ❌ Not Done | |
| Report scheduling | ❌ Not Done | |
| Report sharing | ❌ Not Done | |

### 8.2 Export Functionality
| Task | Status | WPF Reference |
|------|--------|---------------|
| Excel export service | ❌ Not Done | ExcelExportService.cs |
| Export team member data | ❌ Not Done | |
| Export meetings history | ❌ Not Done | |
| Export tasks | ❌ Not Done | |
| Export goals/metrics | ❌ Not Done | |
| Export feedback history | ❌ Not Done | |
| PDF export | ❌ Not Done | |
| CSV export | ❌ Not Done | |

### 8.3 Report Types
| Report | Status | Notes |
|--------|--------|-------|
| Team engagement report | ❌ Not Done | |
| Goal progress report | ❌ Not Done | |
| Meeting summary report | ❌ Not Done | |
| Task completion report | ❌ Not Done | |
| Feedback summary report | ❌ Not Done | |
| Metric trends report | ❌ Not Done | |
| Activity timeline report | ❌ Not Done | |

---

## Phase 9: Administrative Features
**Priority:** LOW | **Status:** Not Started

### 9.1 Admin Window/View
| Task | Status | WPF Reference |
|------|--------|---------------|
| Admin window | ❌ Not Done | AdminWindow.xaml |
| User management | ❌ Not Done | |
| Organization settings | ❌ Not Done | |
| Team structure management | ❌ Not Done | |
| Permission management | ❌ Not Done | |

### 9.2 Setup & Onboarding
| Task | Status | WPF Reference |
|------|--------|---------------|
| Setup wizard | ❌ Not Done | SetupWizard.xaml |
| Initial configuration | ❌ Not Done | |
| Team import | ❌ Not Done | |
| Data migration wizard | ❌ Not Done | |

### 9.3 About & Info
| Task | Status | WPF Reference |
|------|--------|---------------|
| About dialog | ❌ Not Done | AboutDialog.xaml |
| Version info | ❌ Not Done | |
| License info | ❌ Not Done | |
| Check for updates | ❌ Not Done | |

---

## Phase 10: Quality & Polish
**Priority:** ONGOING | **Status:** In Progress

### 10.1 Testing
| Task | Status | Notes |
|------|--------|-------|
| Unit tests for services | ❌ Not Done | |
| Unit tests for ViewModels | ❌ Not Done | |
| Integration tests | ❌ Not Done | |
| UI automation tests | ❌ Not Done | |
| Cross-platform testing | ❌ Not Done | |

### 10.2 Documentation
| Task | Status | Notes |
|------|--------|-------|
| User manual | ❌ Not Done | Help content |
| API documentation | ❌ Not Done | Service interfaces |
| Architecture documentation | 🔄 Partial | New Docs/ folder |
| Deployment documentation | ❌ Not Done | |

### 10.3 Deployment
| Task | Status | Notes |
|------|--------|-------|
| Windows installer | ❌ Not Done | |
| macOS package | ❌ Not Done | |
| Linux package | ❌ Not Done | |
| Auto-update mechanism | ❌ Not Done | |
| Crash reporting | ❌ Not Done | |
| Telemetry (opt-in) | ❌ Not Done | |

---

## Known Issues & Bugs

| Issue | Priority | Status | Notes |
|-------|----------|--------|-------|
| Avatar display not working | Medium | Open | Debug image loading |
| Session expiry handling | Medium | Open | Refresh token flow |
| | | | |

---

## Dependencies & Prerequisites

### NuGet Packages Potentially Needed
- **Toast Notifications:** `Avalonia.Notification` or custom implementation
- **System Tray:** `Avalonia.Desktop` platform-specific APIs
- **Excel Export:** `ClosedXML` or `EPPlus`
- **PDF Export:** `QuestPDF` or similar
- **Charts:** `LiveChartsCore.SkiaSharpView.Avalonia`
- **Markdown:** Custom or `Markdig` with custom renderer
- **OAuth:** Platform HTTP listeners for callback

### External Services
- **Supabase:** Auth, Database, Storage, Realtime (already integrated)
- **Google APIs:** Calendar, OAuth
- **Microsoft Graph:** Calendar, Teams
- **Slack API:** Messaging, Kudos delivery
- **OpenAI/Anthropic/Google AI:** Chat, embeddings

---

## Appendix A: WPF Feature Inventory (Complete)

### WPF Managers (Tracker/Managers/)
- `AuthenticationManager.cs` - ✅ Covered by AuthService
- `CalendarSyncManager.cs` - ❌ Not in Avalonia
- `DialogManager.cs` - 🔄 Partial (no centralized manager)
- `NotificationManager.cs` - ❌ Not in Avalonia
- `ThemeManager.cs` - ✅ Covered by ThemeService
- `TrackerDataManager.cs` - ✅ Covered by various services
- `UserSettingsManager.cs` - ✅ Covered by LocalSettingsService

### WPF Services Not in Avalonia
- `SystemTrayService.cs` - System tray icon/menu
- `ReminderService.cs` - Background reminders
- `ExcelExportService.cs` - Excel reports
- `CalendarSyncManager.cs` - Google/Outlook sync
- `HelpBotContextService.cs` - AI chat context
- All services in `Services/AI/` - AI/ML features
- All services in `Services/Analytics/` - Predictive analytics
- All services in `Services/Recognition/` - Kudos system
- All services in `Services/Google/` - Google integration
- All services in `Services/Microsoft365/` - Office integration
- All services in `Services/Slack/` - Slack integration
- All services in `Services/Square/` - Payment processing
- All services in `Services/Subscription/` - Subscription management
- All services in `Services/Licensing/` - License management

### WPF Dialogs Not in Avalonia
- `AboutDialog.xaml` - App info
- `AddFeedbackDialog.xaml` - Create feedback
- `AddGoalDialog.xaml` - Create goal (have Edit only)
- `AddKeyResultDialog.xaml` - Goal targets
- `AddKPI.xaml` / `AddMeasurableDialog.xaml` - Create metrics
- `AddOneOnOneDialog.xaml` - Quick meeting creation
- `AddProjectDialog.xaml` - Project management
- `AddReminderDialog.xaml` - Custom reminders
- `ConfirmationDialog.xaml` - Generic confirm
- `DailyBriefingDialog.xaml` - Briefing popup
- `InputDialog.xaml` - Generic input
- `InsightsDialog.xaml` - AI insights
- `LinkAgendaItemDialog.xaml` - Agenda linking
- `ManualAuthCodeDialog.xaml` - OAuth fallback
- `MessageBoxDialog.xaml` - Generic messages
- `PurchaseCreditsDialog.xaml` - AI credits
- `QuickMessageDialog.xaml` - Send message
- `ReportsDialog.xaml` - Reports
- `SendKudosDialog.xaml` - Recognition
- `SettingsDialog.xaml` - Settings (have SettingsView)
- `SetupWizard.xaml` - Initial setup
- `TeamMemberDialog.xaml` - Team member (have Edit)
- `TemplatePreviewDialog.xaml` - Meeting template preview
- `UpgradePlanDialog.xaml` - Subscription upgrade

### WPF Views Not in Avalonia
- `AdminWindow.xaml` - Admin features
- `HelpBotWindow.xaml` - AI assistant
- `Toasts/TrackerToast.xaml` - Toast notifications

### WPF Help System (Complete)
- `Help/Attributes/HelpContextAttribute.cs` - View/control annotation
- `Help/Attributes/HelpProperties.cs` - XAML attached properties
- `Help/Models/HelpTopic.cs` - Topic model, HelpContext
- `Help/Models/HelpTopicRegistry.cs` - Topic registration
- `Help/Services/HelpService.cs` - Main help service
- `Help/Services/LruCache.cs` - Topic caching
- `Help/Services/MarkdownRenderer.cs` - Help content rendering
- `Help/ViewModels/HelpViewModel.cs` - Help window VM
- `Help/Views/HelpWindow.xaml` - Help display

---

## Appendix B: Quick Status Summary

| Category | Done | In Progress | Not Started | Total |
|----------|------|-------------|-------------|-------|
| Phase 1: Team Members | 4 | 2 | 4 | 10 |
| Phase 2: Core CRUD | 20 | 12 | 25 | 57 |
| Phase 3: UI Completion | 15 | 8 | 12 | 35 |
| Phase 4: Account/Profile | 8 | 4 | 12 | 24 |
| Phase 5: Platform Work | 0 | 2 | 12 | 14 |
| Phase 6: WPF Core | 0 | 0 | 35 | 35 |
| Phase 7: WPF Advanced | 0 | 0 | 45 | 45 |
| Phase 8: Reports | 0 | 0 | 18 | 18 |
| Phase 9: Admin | 0 | 0 | 12 | 12 |
| Phase 10: Quality | 0 | 2 | 14 | 16 |
| **TOTAL** | **47** | **30** | **189** | **266** |

**Completion: ~18% Done, ~11% In Progress, ~71% Not Started**

---

## Document History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-22 | Copilot | Initial pipeline document created |

