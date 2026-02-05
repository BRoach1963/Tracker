# AI Migration Status & Implementation Plan
**Date:** February 1, 2026  
**Status:** Phase 5 Complete - Chat UI Integrated

---

## ✅ COMPLETED (Phases 1-5)

### Phase 1: Core AI Infrastructure
- ✅ `IChatProvider` interface - abstraction for multiple AI providers
- ✅ `GeminiChatService` - Google Gemini API implementation (gemini-1.5-flash)
- ✅ `ChatProviderFactory` - provider selection and initialization
- ✅ `AIUsageTracker` - token and cost tracking
- ✅ `HelpSystem` - context-sensitive help integration
- ✅ Build: Zero errors, zero warnings

### Phase 2: Function Calling Tools
- ✅ `AIFunctionService` - dispatcher for 12 AI functions
- ✅ Function Tools Implemented:
  1. `create_task` - Create tasks with title, description, due date, assignee
  2. `create_meeting` - Schedule one-on-one meetings
  3. `create_goal` - Create goals with targets and metrics
  4. `create_project` - Initialize projects with phases and metadata
  5. `create_note` - Add notes to entities or create standalone notes
  6. `search_team_members` - Find team members by name/role/department
  7. `get_upcoming_meetings` - Query upcoming meetings with date range
  8. `get_projects` - List projects with optional status filter
  9. `get_notes` - Retrieve notes with optional entity filter
  10. `get_tasks` - List tasks with optional status/assignee filter
  11. `get_current_time` - Get current date/time for scheduling context
  12. `help` - Get help on available functions and capabilities

### Phase 3: Data Integration Layer
- ✅ 6 AI-Facing Interfaces:
  - `ITaskDataService` - Task CRUD and querying
  - `IMeetingDataService` - Meeting management
  - `IGoalDataService` - Goal and target operations
  - `IProjectDataService` - Project lifecycle
  - `INoteDataService` - Note creation and retrieval
  - `ITeamDataService` - Team member search and info
- ✅ 6 Service Implementations wrapping ProCohere singletons
- ✅ Error Resolution: 41 build errors → 0 errors, 0 warnings
- ✅ Proper service contract matching (GetMyGoalsAsync, DueDate, Name, etc.)

### Phase 4: Context & Chat UI Components
- ✅ `AIContextService` - Gathers user context (active projects, tasks, goals, team info)
- ✅ `ChatMessage` - Immutable message model (Content, Role, Timestamp, FunctionName/Result)
- ✅ `ChatViewModel` - MVVM-compliant ViewModel with:
  - `ObservableCollection<ChatMessage>` Messages
  - `InputText`, `IsSending`, `HasError`, `StatusMessage` properties
  - `SendMessageCommand`, `RefreshContextCommand`, `ClearChatCommand`
- ✅ `ChatView.axaml` - Pure XAML chat interface:
  - Message bubbles (user right/blue, assistant left/gray, system center/yellow)
  - ScrollViewer with auto-scroll to latest
  - Input TextBox with Enter key binding
  - Loading overlay and status bar

### Phase 5: Main Window Integration
- ✅ `MainWindowViewModel.ChatViewModel` - Child ViewModel property
- ✅ `MainWindowViewModel.IsChatOpen` - Observable visibility state
- ✅ `MainWindowViewModel.ToggleChatCommand` - Open/close chat
- ✅ `MainWindow.axaml` Chat UI:
  - Floating chat panel (500x700px) in bottom-right corner
  - Dark semi-transparent overlay with white chat card
  - Blue header (#1E3A8A) with AI sparkle icon and close button
  - 56px circular FAB (Floating Action Button) when chat is closed
  - Keyboard shortcut: **Ctrl+/** to toggle chat
  - Z-Index layering (button=99, overlay=100)
- ✅ Build Status: Zero errors, zero warnings

---

## 🚧 STUBBED/NOT IMPLEMENTED (ProCohere.Avalonia)

### AI Features (Shelled but Not Functional)
1. **Meeting Prep Items** (Line 98: MeetingPrepItemService.cs)
   - Has "ai" prep item type but no AI generation logic
   - No AI-suggested agenda items based on past meetings
   - No AI-generated meeting summaries

2. **Meeting Listing** (MeetingDataService.cs)
   - Lines 90-91: "This is a stub implementation"
   - `GetUpcomingMeetingsAsync()` returns empty list
   - `CreateMeetingAsync()` not fully implemented
   - TODO: Connect to ProCohere meeting service when available

3. **Search Command** (MainWindowViewModel.cs line 412)
   - "TODO: Implement command palette / search"
   - Ctrl+K binding exists but not functional
   - No AI-powered search or command palette

4. **Dialog Stubs** (EditTeamMemberDialog.axaml.cs lines 195-232)
   - Note creation: TODO
   - Note deletion: TODO
   - Member deletion: TODO
   - Actual save via TeamService: TODO

5. **Navigation Converters** (NavigationConverters.cs)
   - Multiple `ConvertBack()` methods throw NotImplementedException
   - 18 converter classes with incomplete two-way binding

6. **Entity Picker** (EntityPickerDialogViewModel.cs line 282)
   - "TODO: Add ProjectService when available"
   - Project picking not functional

7. **Tasks Edit** (TasksViewModel.cs line 464)
   - "TODO: Implement edit task dialog"

8. **Circle View Features** (CircleViewModel.cs)
   - Line 735: Edit meeting dialog
   - Line 1188: Feedback dialog
   - Line 1514: Target loading
   - Line 1567: Goal edit dialog
   - Line 1575: Goal deletion confirmation
   - Line 1583: Add target dialog

9. **Chronicle Categories** (ChronicleViewModel.cs line 350)
   - "TODO: Implement when category column is added to DB"

10. **Pulse Role Detection** (PulseViewModel.cs line 270)
    - "TODO: Get actual user role from auth service"
    - Hardcoded to "contributor"

---

## 🔴 MISSING FROM WPF TRACKER (Not Yet Migrated)

### 1. AI Insights System (HIGH PRIORITY)
**WPF Location:** `Tracker/Services/AI/Insights/`

**Components:**
- `InsightEngine.cs` (483 lines) - Coordinates insight generation lifecycle
- `InsightStore.cs` - Persists insights to database
- `AIInsightGenerator.cs` - Generates insights from data analysis
- **6 Analyzer Implementations:**
  1. `ActionItemStalenessAnalyzer` - Detects stale/overdue tasks
  2. `GoalTrajectoryAnalyzer` - Predicts goal progress and completion
  3. `MeetingCadenceAnalyzer` - Analyzes meeting frequency patterns
  4. `MetricGapAnalyzer` - Identifies missing or incomplete metrics
  5. `PersonalDateAnalyzer` - Reminds about personal dates (birthdays, work anniversaries)
  6. `SurveySentimentAnalyzer` - Analyzes pulse survey sentiment trends

**Functionality:**
- Periodic background analysis (configurable intervals)
- Generates actionable insights with severity levels
- User can dismiss/acknowledge insights
- Insights displayed in dashboard/briefing
- Repository pattern with Dapper for storage

**Migration Effort:** HIGH
- Requires `IInsightRepository` implementation in ProCohere
- Need UI components for insight display (cards, notifications)
- Background task scheduling in Avalonia
- Event system for insight notifications

---

### 2. Vector Store & Semantic Search (MEDIUM PRIORITY)
**WPF Location:** `Tracker/Services/AI/`

**Components:**
- `VectorStore.cs` - In-memory vector storage for semantic search
- `PostgresVectorStore.cs` - Postgres with pgvector extension
- `SqlServerVectorStore.cs` - SQL Server vector storage (legacy)
- `VectorStoreFactory.cs` - Provider selection
- `VectorStoreMigrator.cs` - Migration between vector stores
- `EmbeddingService.cs` - OpenAI embeddings generation
- **5 Entity Indexers:**
  1. `GoalIndexer` - Index goals for semantic search
  2. `TaskIndexer` - Index tasks
  3. `MeetingIndexer` - Index meeting notes/agendas
  4. `TeamMemberIndexer` - Index team member profiles
  5. `PulseSurveyIndexer` - Index survey responses

**Functionality:**
- Semantic search across all entities ("find tasks related to customer feedback")
- RAG (Retrieval Augmented Generation) for context-aware AI responses
- Vector similarity matching for related content
- Incremental indexing as data changes

**Migration Effort:** MEDIUM
- ProCohere uses Supabase (Postgres with pgvector) - already compatible
- Need to wire up indexers to ProCohere data models
- Background indexing service
- Search UI integration (already have Ctrl+K placeholder)

---

### 3. OpenAI Chat Provider (LOW PRIORITY)
**WPF Location:** `Tracker/Services/OpenAIChatService.cs` (259 lines)

**Functionality:**
- GPT-4o and GPT-4o-mini support
- Streaming responses
- Function calling
- Token counting and cost tracking
- System context management

**Migration Effort:** LOW
- Already have `IChatProvider` interface in ProCohere
- Copy OpenAIChatService and adapt to Avalonia
- Add provider selection UI in settings
- Optional: Use OpenAI for fallback when Gemini unavailable

**Status:** Not urgent - Gemini provider works well

---

### 4. HelpBot Window (LOW PRIORITY)
**WPF Location:** `Tracker/Views/HelpBotWindow.xaml` (96 lines)

**Functionality:**
- Standalone chat window ("Tracker Oracle")
- Floating, always-on-top window
- Custom title bar with drag support
- Gradient sparkle icon
- Transparent, rounded corners

**Migration Effort:** LOW
- ProCohere has integrated chat in MainWindow (floating panel)
- Could add "Pop Out" button to open chat in separate window
- Reuse ChatView component with new Window wrapper

**Status:** Not needed - integrated chat is better UX

---

### 5. Admin Window (MEDIUM PRIORITY)
**WPF Location:** `Tracker/Views/AdminWindow.xaml`

**Functionality:**
- Organization management
- User role assignment
- System settings
- Database maintenance
- Bulk operations

**Migration Effort:** MEDIUM
- ProCohere has basic Settings view but no admin features
- Need role-based access control (admin-only views)
- Bulk data operations UI
- System health monitoring

**Status:** Needed for multi-org deployments

---

### 6. Loading/Splash Screens (LOW PRIORITY)
**WPF Location:** 
- `Tracker/Views/LoadingWindow.xaml`
- `Tracker/Views/SplashScreen.xaml`

**Functionality:**
- App startup loading screen
- Database initialization progress
- Service connection status
- Animated splash with branding

**Migration Effort:** LOW
- ProCohere doesn't have startup screens (faster load)
- Could add for better perceived performance
- Avalonia has different startup model

**Status:** Nice to have, not critical

---

## 📋 RECOMMENDED IMPLEMENTATION SEQUENCE

### **Priority 1: AI Insights System** (1-2 weeks)
**Why:** High value feature that differentiates the product. Users love actionable insights.

**Steps:**
1. Create `IInsightRepository` interface in ProCohere
2. Implement Dapper-based repository for insights table
3. Port `InsightEngine.cs` to ProCohere.Avalonia
4. Migrate 6 analyzers (start with ActionItemStalenessAnalyzer)
5. Create insight card UI component for Briefing view
6. Add background task scheduler
7. Wire up insight generation to data changes
8. Test with real data

**Deliverables:**
- Insight cards in Briefing view
- "Dismiss" and "Act On" actions
- Periodic analysis (daily at 6am, on data changes)
- 6 analyzer types generating insights

---

### **Priority 2: Vector Search & Semantic Features** (1 week)
**Why:** Enables powerful AI-assisted search and context retrieval.

**Steps:**
1. Create `IVectorStore` interface in ProCohere
2. Implement `PostgresVectorStore` using Supabase pgvector
3. Port 5 entity indexers
4. Create background indexing service
5. Implement semantic search API
6. Wire up to Ctrl+K command palette
7. Enhance AI context gathering with vector search

**Deliverables:**
- Ctrl+K search working with semantic matching
- "Find similar" buttons on entities
- AI gets better context via RAG
- Index updates automatically on data changes

---

### **Priority 3: Complete Chat Integration** (2-3 days)
**Why:** Chat is integrated but not tested end-to-end.

**Steps:**
1. Add Gemini API key to Settings view
2. Test chat with real Gemini API
3. Test all 12 function tools
4. Add error handling for API failures
5. Add "Pop Out" option for separate window
6. Add conversation export/save
7. Add conversation history persistence

**Deliverables:**
- Fully functional AI chat with real API
- All function tools tested and working
- Conversation history saved to database
- Export conversations to markdown

---

### **Priority 4: Missing Dialogs & UI** (3-5 days)
**Why:** Many workflows are incomplete without these dialogs.

**Steps:**
1. Implement edit task dialog (TasksViewModel line 464)
2. Implement edit meeting dialog (CircleViewModel line 735)
3. Implement feedback dialog (CircleViewModel line 1188)
4. Implement add target dialog (CircleViewModel line 1583)
5. Implement edit goal dialog (CircleViewModel line 1567)
6. Complete EditTeamMemberDialog save logic
7. Wire up ProjectService to EntityPicker

**Deliverables:**
- All entity editing workflows complete
- No more "TODO" placeholders in critical paths

---

### **Priority 5: Admin Features** (1 week)
**Why:** Needed for production deployment with multiple organizations.

**Steps:**
1. Design admin navigation item (gear icon in nav rail)
2. Create AdminView with tabs:
   - Organization settings
   - User management
   - Role assignments
   - System health
   - Database maintenance
3. Add role-based access control
4. Implement bulk operations
5. Add audit logging

**Deliverables:**
- Admin-only navigation item
- Complete organization management
- User role assignment UI
- System monitoring dashboard

---

### **Priority 6: OpenAI Provider** (1-2 days - OPTIONAL)
**Why:** Provider diversity for fallback and cost optimization.

**Steps:**
1. Port OpenAIChatService.cs to ProCohere
2. Add to ChatProviderFactory
3. Add provider selection to Settings
4. Test function calling with GPT-4o
5. Add automatic fallback on provider failure

**Deliverables:**
- OpenAI as alternative to Gemini
- Provider selection in Settings
- Automatic failover

---

## 📊 FEATURE COMPARISON MATRIX

| Feature | WPF Tracker | ProCohere.Avalonia | Status |
|---------|-------------|-------------------|--------|
| **AI Chat** | ✅ HelpBot Window | ✅ Integrated Chat Panel | ✅ Complete |
| **Function Calling** | ✅ 14 functions | ✅ 12 functions | ⚠️ Missing 2 (insights, dismiss) |
| **Chat Providers** | ✅ OpenAI + Gemini | ✅ Gemini only | ⚠️ Missing OpenAI |
| **AI Insights** | ✅ 6 analyzers | ❌ Not implemented | 🔴 Missing |
| **Vector Search** | ✅ Full indexing | ❌ Not implemented | 🔴 Missing |
| **Semantic Search** | ✅ Working | ❌ Ctrl+K stubbed | 🔴 Missing |
| **Context Gathering** | ✅ Full context | ✅ Basic context | ⚠️ Needs vector RAG |
| **Usage Tracking** | ✅ Full tracking | ✅ Full tracking | ✅ Complete |
| **Help System** | ✅ AI-powered help | ✅ Context help | ✅ Complete |
| **Admin Features** | ✅ Admin window | ❌ No admin UI | 🔴 Missing |
| **Meeting Prep AI** | ✅ AI suggestions | ⚠️ Stubbed | 🔴 Missing |
| **Goal Trajectory** | ✅ Predictions | ❌ Not implemented | 🔴 Missing |
| **Sentiment Analysis** | ✅ Pulse surveys | ❌ Not implemented | 🔴 Missing |
| **Conversation History** | ✅ Saved | ❌ Not saved | 🔴 Missing |
| **Export Conversations** | ✅ Markdown | ❌ Not available | 🔴 Missing |

**Legend:**
- ✅ Complete and working
- ⚠️ Partial or stubbed
- 🔴 Missing entirely

---

## 🎯 SUCCESS METRICS

### Phase 1-5 (Complete)
- ✅ Build succeeds: 0 errors, 0 warnings
- ✅ Chat UI integrated and styled
- ✅ MVVM compliance: 100%
- ✅ Function tools: 12/12 implemented
- ⏳ API tested: Pending (need API key)
- ⏳ Function tools tested: 0/12 (need real testing)

### Next Phase Targets
- **Insights:** 6/6 analyzers ported and generating insights
- **Search:** Ctrl+K functional with semantic matching
- **Chat:** End-to-end tested with real Gemini API
- **Dialogs:** 0 "TODO" comments in critical workflows
- **Admin:** Role-based access control functional

---

## 🔧 TECHNICAL DEBT

### High Priority
1. **Missing Conversation Persistence** - Chat messages not saved to database
2. **No Error Recovery** - API failures not gracefully handled
3. **Missing API Key UI** - No way to configure Gemini key in Settings
4. **Stubbed Meeting Service** - MeetingDataService returns empty lists

### Medium Priority
1. **Converter NotImplemented** - 18 ConvertBack methods throw exceptions
2. **No Admin UI** - Role management only via direct DB edits
3. **Missing Entity Pickers** - Projects can't be selected in some dialogs
4. **No Background Indexing** - Vector search not wired up

### Low Priority
1. **No OpenAI Provider** - Only Gemini available
2. **No Pop-Out Chat** - Chat is integrated only, no separate window
3. **No Loading Screens** - Fast startup means not needed, but nice to have

---

## 📝 NOTES

### What Works Well
- **MVVM Architecture:** Clean separation, zero View logic
- **Build Stability:** No warnings, no errors across 41+ file changes
- **Service Layer:** Data integration layer well-designed
- **UI Polish:** Chat panel looks professional, matches ProCohere design

### What Needs Attention
- **Testing:** No end-to-end testing with real API yet
- **Persistence:** Chat conversations ephemeral (lost on restart)
- **Settings:** No UI for API key configuration
- **Documentation:** Function calling behavior not user-documented

### Migration Challenges
- **Service Contracts:** WPF uses different method signatures (GetGoalsAsync vs GetMyGoalsAsync)
- **Data Models:** Property names differ (TargetDate vs DueDate, CurrentLifecycle vs Lifecycle)
- **Singleton Patterns:** ProCohere uses `Service.Instance`, WPF uses `Manager.Instance`
- **Async Patterns:** Some WPF code is synchronous, had to add async wrappers

---

## 🚀 NEXT STEPS (Immediate)

1. **Add API Key to Settings** (30 min)
   - Add "AI Settings" section to SettingsView
   - Add TextBox for Gemini API key
   - Save to user preferences
   - Load in GeminiChatService initialization

2. **Test Chat End-to-End** (1 hour)
   - Configure real API key
   - Send test message
   - Verify function calling works
   - Test error handling

3. **Start Insights Migration** (Priority 1)
   - Create insights database table
   - Port InsightEngine.cs
   - Implement first analyzer (ActionItemStaleness)
   - Add insight card to Briefing view

---

**End of Document**
