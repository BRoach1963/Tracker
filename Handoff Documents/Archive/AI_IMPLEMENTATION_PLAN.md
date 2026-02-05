# AI Features Implementation Plan
**Created:** February 2, 2026  
**Target:** ProCohere.Avalonia  
**Status:** Ready to Execute

---

## 🎯 MISSION

Implement the remaining AI features from WPF Tracker into ProCohere.Avalonia:
1. **AI Insights System** - Proactive analysis and recommendations
2. **Vector Search & Semantic Matching** - Intelligent search across all entities
3. **Chat Polish & Testing** - Complete and validate AI chat integration

---

## 📦 PHASE 1: AI INSIGHTS SYSTEM (Priority 1)
**Time Estimate:** 5-7 days  
**Value:** HIGH - Differentiating feature that drives user engagement

### What We're Building
An automated insight engine that analyzes user data and generates actionable recommendations:
- **6 Analyzers** detecting patterns and issues
- **Background processing** (daily + on-demand)
- **Insight cards** in Briefing view
- **Action workflows** (dismiss, act on, snooze)
- **Persistent storage** using Supabase

### Dependencies
- ✅ Supabase connection (already working)
- ✅ Task/Goal/Meeting/Metric services (already implemented)
- ✅ Briefing view (exists, needs insight cards)
- 🔄 Database migration (new `insights` table)

---

## 📋 PHASE 1 TASK BREAKDOWN

### **Task 1.1: Database Schema (2 hours)**

**Create Migration:**
```sql
-- File: Tracker/supabase/migrations/20260202_insights_table.sql

CREATE TABLE IF NOT EXISTS insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    user_id UUID NOT NULL REFERENCES team_members(id),
    
    -- Insight metadata
    insight_type TEXT NOT NULL,
    severity TEXT NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    
    -- Related entities (optional)
    entity_type TEXT,
    entity_id UUID,
    
    -- Status tracking
    status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'dismissed', 'acted_on', 'snoozed')),
    dismissed_at TIMESTAMPTZ,
    dismissed_by UUID REFERENCES team_members(id),
    snoozed_until TIMESTAMPTZ,
    acted_on_at TIMESTAMPTZ,
    
    -- Metadata
    analyzer_name TEXT NOT NULL,
    confidence_score FLOAT DEFAULT 1.0,
    metadata JSONB,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ,
    deleted_by UUID REFERENCES team_members(id)
);

-- Indexes
CREATE INDEX idx_insights_user ON insights(user_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_org ON insights(organization_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_status ON insights(status) WHERE NOT is_deleted;
CREATE INDEX idx_insights_entity ON insights(entity_type, entity_id) WHERE NOT is_deleted;
CREATE INDEX idx_insights_created ON insights(created_at DESC) WHERE NOT is_deleted;

-- RLS Policies
ALTER TABLE insights ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view their own insights"
    ON insights FOR SELECT
    USING (user_id = auth.uid() AND NOT is_deleted);

CREATE POLICY "Users can update their own insights"
    ON insights FOR UPDATE
    USING (user_id = auth.uid());

CREATE POLICY "System can insert insights"
    ON insights FOR INSERT
    WITH CHECK (true);

-- Updated trigger
CREATE TRIGGER update_insights_updated_at
    BEFORE UPDATE ON insights
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

**Acceptance:**
- ✅ Migration runs successfully
- ✅ RLS policies work correctly
- ✅ Indexes created

---

### **Task 1.2: Core Models (1 hour)**

**Files to Create:**

**`Models/Insight.cs`** (200 lines)
```csharp
namespace ProCohere.Avalonia.Models;

public class Insight
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    
    public InsightType Type { get; set; }
    public InsightSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    
    public InsightStatus Status { get; set; }
    public DateTime? DismissedAt { get; set; }
    public Guid? DismissedBy { get; set; }
    public DateTime? SnoozedUntil { get; set; }
    public DateTime? ActedOnAt { get; set; }
    
    public string AnalyzerName { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; } = 1.0f;
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Computed properties
    public bool IsActive => Status == InsightStatus.Active;
    public bool IsSnoozed => Status == InsightStatus.Snoozed && SnoozedUntil > DateTime.UtcNow;
    public string SeverityColor => Severity switch
    {
        InsightSeverity.Critical => "#DC2626",
        InsightSeverity.High => "#F59E0B",
        InsightSeverity.Medium => "#3B82F6",
        InsightSeverity.Low => "#10B981",
        _ => "#6B7280"
    };
}

public enum InsightType
{
    TaskOverdue,
    StaleActionItem,
    GoalOffTrack,
    GoalOnTrack,
    MeetingOverdue,
    MeetingUpcoming,
    MetricMissing,
    MetricDeclining,
    PersonalDate,
    SentimentDeclining,
    SentimentImproving
}

public enum InsightSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum InsightStatus
{
    Active,
    Dismissed,
    ActedOn,
    Snoozed
}
```

**Acceptance:**
- ✅ Models compile
- ✅ Enums match database constraints
- ✅ Computed properties work

---

### **Task 1.3: Insight Repository (3 hours)**

**Files to Create:**

**`Services/Insights/IInsightRepository.cs`** (50 lines)
```csharp
namespace ProCohere.Avalonia.Services.Insights;

public interface IInsightRepository
{
    Task<List<Insight>> GetActiveInsightsAsync(Guid userId);
    Task<Insight?> GetInsightByIdAsync(Guid id);
    Task<Guid> CreateInsightAsync(Insight insight);
    Task UpdateInsightAsync(Insight insight);
    Task DismissInsightAsync(Guid id, Guid userId);
    Task ActOnInsightAsync(Guid id);
    Task SnoozeInsightAsync(Guid id, DateTime until);
    Task<int> GetActiveCountAsync(Guid userId);
    Task CleanupOldInsightsAsync(int daysOld = 90);
}
```

**`Services/Insights/InsightRepository.cs`** (400 lines)
```csharp
using Postgrest;
using Supabase;

namespace ProCohere.Avalonia.Services.Insights;

public class InsightRepository : IInsightRepository
{
    private readonly ILogger _logger;
    
    public InsightRepository()
    {
        _logger = LoggingManager.GetComponentLogger("InsightRepository");
    }
    
    public async Task<List<Insight>> GetActiveInsightsAsync(Guid userId)
    {
        try
        {
            var client = SupabaseService.Instance.Client;
            var response = await client
                .From<InsightDto>()
                .Where(i => i.UserId == userId)
                .Where(i => i.IsDeleted == false)
                .Where(i => i.Status == "active")
                .Order("created_at", Ordering.Descending)
                .Get();
                
            return response.Models.Select(MapToInsight).ToList();
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Failed to get active insights");
            return new List<Insight>();
        }
    }
    
    public async Task<Guid> CreateInsightAsync(Insight insight)
    {
        try
        {
            var client = SupabaseService.Instance.Client;
            var dto = MapToDto(insight);
            
            var response = await client
                .From<InsightDto>()
                .Insert(dto);
                
            return response.Models.First().Id;
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Failed to create insight");
            throw;
        }
    }
    
    // ... remaining CRUD methods
}
```

**Acceptance:**
- ✅ All CRUD operations work
- ✅ RLS policies enforced
- ✅ Proper error handling

---

### **Task 1.4: Insight Engine Core (4 hours)**

**Files to Create:**

**`Services/Insights/IInsightAnalyzer.cs`** (30 lines)
```csharp
namespace ProCohere.Avalonia.Services.Insights;

public interface IInsightAnalyzer
{
    string Name { get; }
    IEnumerable<InsightType> SupportedInsightTypes { get; }
    bool IsEnabled { get; set; }
    Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default);
}
```

**`Services/Insights/InsightEngine.cs`** (500 lines - port from WPF)
```csharp
namespace ProCohere.Avalonia.Services.Insights;

public class InsightEngine : IDisposable
{
    private static InsightEngine? _instance;
    private readonly ILogger _logger;
    private readonly List<IInsightAnalyzer> _analyzers = new();
    private IInsightRepository? _repository;
    private bool _isRunning;
    
    public static InsightEngine Instance { get; }
    
    public async Task InitializeAsync(IInsightRepository repository)
    {
        _repository = repository;
        RegisterDefaultAnalyzers();
        await _repository.CleanupOldInsightsAsync();
    }
    
    private void RegisterDefaultAnalyzers()
    {
        RegisterAnalyzer(new ActionItemStalenessAnalyzer());
        RegisterAnalyzer(new GoalTrajectoryAnalyzer());
        RegisterAnalyzer(new MeetingCadenceAnalyzer());
        RegisterAnalyzer(new MetricGapAnalyzer());
        RegisterAnalyzer(new PersonalDateAnalyzer());
        RegisterAnalyzer(new SurveySentimentAnalyzer());
    }
    
    public async Task<List<Insight>> RunAnalyzersAsync(CancellationToken ct = default)
    {
        if (_isRunning) return new();
        
        _isRunning = true;
        var allInsights = new List<Insight>();
        
        try
        {
            foreach (var analyzer in _analyzers.Where(a => a.IsEnabled))
            {
                var insights = await analyzer.AnalyzeAsync(ct);
                
                foreach (var insight in insights)
                {
                    // Check for duplicates
                    var exists = await _repository.CheckDuplicateAsync(insight);
                    if (!exists)
                    {
                        await _repository.CreateInsightAsync(insight);
                        allInsights.Add(insight);
                        InsightGenerated?.Invoke(this, new InsightEventArgs(insight));
                    }
                }
            }
            
            InsightsUpdated?.Invoke(this, allInsights.Count);
        }
        finally
        {
            _isRunning = false;
        }
        
        return allInsights;
    }
    
    public event EventHandler<InsightEventArgs>? InsightGenerated;
    public event EventHandler<int>? InsightsUpdated;
}
```

**Acceptance:**
- ✅ Engine initializes
- ✅ Analyzers register correctly
- ✅ Events fire properly
- ✅ No duplicate insights created

---

### **Task 1.5: Analyzer #1 - Action Item Staleness (3 hours)**

**File:** `Services/Insights/Analyzers/ActionItemStalenessAnalyzer.cs` (150 lines)

**What it does:**
- Finds tasks overdue by >7 days → Critical
- Finds tasks overdue by 1-7 days → High
- Finds tasks with no due date but >14 days old → Medium
- Generates insight with action: "Review task with assignee"

**Implementation:**
```csharp
public class ActionItemStalenessAnalyzer : IInsightAnalyzer
{
    public string Name => "Action Item Staleness";
    public int StaleThresholdDays { get; set; } = 14;
    
    public async Task<List<Insight>> AnalyzeAsync(CancellationToken ct = default)
    {
        var insights = new List<Insight>();
        var today = DateTime.Today;
        var staleDate = today.AddDays(-StaleThresholdDays);
        
        var tasks = await TaskService.Instance.GetMyTasksAsync();
        
        foreach (var task in tasks.Where(t => !t.IsCompleted))
        {
            // Overdue tasks
            if (task.DueDate.HasValue && task.DueDate.Value < today)
            {
                var daysOverdue = (today - task.DueDate.Value).Days;
                var severity = daysOverdue > 7 
                    ? InsightSeverity.Critical 
                    : InsightSeverity.High;
                    
                insights.Add(new Insight
                {
                    Type = InsightType.TaskOverdue,
                    Severity = severity,
                    Title = $"Task overdue by {daysOverdue} days",
                    Description = $"\"{task.Name}\" was due on {task.DueDate:MMM d}",
                    EntityType = "task",
                    EntityId = task.Id,
                    AnalyzerName = Name,
                    UserId = AuthService.Instance.CurrentUser!.Id
                });
            }
            // Stale tasks (old but no due date)
            else if (task.CreatedDate <= staleDate)
            {
                var daysOld = (today - task.CreatedDate).Days;
                insights.Add(new Insight
                {
                    Type = InsightType.StaleActionItem,
                    Severity = InsightSeverity.Medium,
                    Title = $"Task has been open for {daysOld} days",
                    Description = $"\"{task.Name}\" might need attention",
                    EntityType = "task",
                    EntityId = task.Id,
                    AnalyzerName = Name,
                    UserId = AuthService.Instance.CurrentUser!.Id
                });
            }
        }
        
        return insights;
    }
}
```

**Test Cases:**
- ✅ Overdue task → Critical insight
- ✅ Old task no due date → Medium insight
- ✅ Completed task → No insight
- ✅ Recent task → No insight

---

### **Task 1.6: Analyzer #2 - Goal Trajectory (4 hours)**

**File:** `Services/Insights/Analyzers/GoalTrajectoryAnalyzer.cs` (250 lines)

**What it does:**
- Analyzes goal progress vs. time remaining
- Predicts on-track/off-track status
- Generates insights for goals needing attention

**Key Logic:**
```csharp
// Calculate progress rate
var daysElapsed = (today - goal.CreatedDate).Days;
var totalDays = (goal.EndDate - goal.CreatedDate).Days;
var expectedProgress = (daysElapsed / (double)totalDays) * 100;

// Compare to actual progress
var actualProgress = CalculateGoalProgress(goal);
var progressGap = actualProgress - expectedProgress;

if (progressGap < -20)
{
    // Goal is >20% behind schedule → Critical
    insights.Add(CreateOffTrackInsight(goal, progressGap));
}
else if (progressGap > 10)
{
    // Goal is >10% ahead → Positive reinforcement
    insights.Add(CreateOnTrackInsight(goal, progressGap));
}
```

**Acceptance:**
- ✅ Off-track goals detected
- ✅ On-track goals celebrated
- ✅ Progress calculation accurate

---

### **Task 1.7: Remaining Analyzers (6 hours total)**

**Analyzer #3: Meeting Cadence** (2 hours)
- Detects managers who haven't met with directs in >2 weeks
- Suggests scheduling next one-on-one

**Analyzer #4: Metric Gap** (2 hours)
- Finds goals without linked metrics
- Finds metrics without recent updates

**Analyzer #5: Personal Date** (1 hour)
- Reminds about upcoming birthdays/work anniversaries
- Simple date-based detection

**Analyzer #6: Survey Sentiment** (1 hour)
- Analyzes pulse survey trends
- Detects declining satisfaction scores

---

### **Task 1.8: Insight Card UI Component (3 hours)**

**File:** `Views/Components/InsightCard.axaml` (200 lines)

**Design:**
```xml
<Border Classes="insight-card" Classes.critical="{Binding IsCritical}">
    <Grid ColumnDefinitions="Auto,*,Auto">
        <!-- Severity indicator -->
        <Border Grid.Column="0" Width="4" Background="{Binding SeverityColor}"/>
        
        <!-- Content -->
        <StackPanel Grid.Column="1" Spacing="8" Margin="16">
            <TextBlock Text="{Binding Title}" FontWeight="SemiBold"/>
            <TextBlock Text="{Binding Description}" Opacity="0.7"/>
            <TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:MMM d, h:mm tt}'}" 
                       FontSize="12" Opacity="0.5"/>
        </StackPanel>
        
        <!-- Actions -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8" Margin="16">
            <Button Content="Act On" Command="{Binding ActOnCommand}"/>
            <Button Content="Dismiss" Classes="text-button" Command="{Binding DismissCommand}"/>
        </StackPanel>
    </Grid>
</Border>
```

**Acceptance:**
- ✅ Severity colors display correctly
- ✅ Actions work (act on, dismiss)
- ✅ Links to entity work
- ✅ Responsive layout

---

### **Task 1.9: Integrate into Briefing View (2 hours)**

**File:** `Views/Briefing/BriefingView.axaml`

**Add section:**
```xml
<!-- Insights Section -->
<Border Grid.Row="1" Classes="section-card" Margin="0,0,0,16">
    <StackPanel Spacing="16">
        <Grid ColumnDefinitions="*,Auto">
            <TextBlock Grid.Column="0" Text="Insights" FontSize="18" FontWeight="SemiBold"/>
            <TextBlock Grid.Column="1" Text="{Binding InsightCount}" Classes="badge"/>
        </Grid>
        
        <ItemsControl ItemsSource="{Binding Insights}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <components:InsightCard Margin="0,0,0,8"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <Button Content="View All Insights" Command="{Binding ViewAllInsightsCommand}"
                HorizontalAlignment="Left"/>
    </StackPanel>
</Border>
```

**ViewModel Changes:**
```csharp
public ObservableCollection<Insight> Insights { get; } = new();
public int InsightCount => Insights.Count;

private async Task LoadInsightsAsync()
{
    var insights = await InsightRepository.Instance.GetActiveInsightsAsync(
        AuthService.Instance.CurrentUser!.Id);
    
    Insights.Clear();
    foreach (var insight in insights.Take(5))
    {
        Insights.Add(insight);
    }
}
```

**Acceptance:**
- ✅ Insights display in Briefing
- ✅ Count badge shows correct number
- ✅ "View All" navigates to full list
- ✅ Real-time updates when new insights arrive

---

### **Task 1.10: Background Processing (3 hours)**

**File:** `Services/Insights/InsightScheduler.cs` (200 lines)

**Implementation:**
```csharp
public class InsightScheduler : IDisposable
{
    private Timer? _timer;
    
    public void Start()
    {
        // Run daily at 6 AM
        var now = DateTime.Now;
        var nextRun = now.Date.AddHours(6);
        if (nextRun < now) nextRun = nextRun.AddDays(1);
        
        var delay = nextRun - now;
        _timer = new Timer(async _ => await RunAnalysisAsync(), 
            null, delay, TimeSpan.FromDays(1));
    }
    
    private async Task RunAnalysisAsync()
    {
        var insights = await InsightEngine.Instance.RunAnalyzersAsync();
        // Notify user if critical insights found
        if (insights.Any(i => i.Severity == InsightSeverity.Critical))
        {
            ShowNotification($"{insights.Count} new insights need attention");
        }
    }
}
```

**Wire up in App.axaml.cs:**
```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    // ... existing startup code
    
    // Initialize insight system
    var repository = new InsightRepository();
    await InsightEngine.Instance.InitializeAsync(repository);
    
    // Start background processing
    InsightScheduler.Instance.Start();
}
```

**Acceptance:**
- ✅ Runs daily at 6 AM
- ✅ Can trigger manual analysis
- ✅ Notifications for critical insights
- ✅ Doesn't block UI

---

## 📦 PHASE 2: VECTOR SEARCH & SEMANTIC FEATURES (Priority 2)
**Time Estimate:** 3-5 days  
**Value:** MEDIUM-HIGH - Enables powerful AI-assisted search

### What We're Building
Semantic search across all entities using vector embeddings:
- **Supabase pgvector** integration
- **5 Entity Indexers** (Goals, Tasks, Meetings, Team Members, Notes)
- **Ctrl+K Command Palette** with semantic matching
- **RAG for AI Chat** - better context retrieval

### Dependencies
- ✅ Supabase (has pgvector extension)
- ✅ OpenAI/Gemini API (for embeddings)
- 🔄 Database migration (vector columns + indexes)

---

## 📋 PHASE 2 TASK BREAKDOWN

### **Task 2.1: Database Schema (3 hours)**

**Migration:** `20260203_vector_search.sql`

```sql
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Add embedding columns to existing tables
ALTER TABLE goals ADD COLUMN IF NOT EXISTS embedding vector(1536);
ALTER TABLE tasks ADD COLUMN IF NOT EXISTS embedding vector(1536);
ALTER TABLE meetings ADD COLUMN IF NOT EXISTS embedding vector(1536);
ALTER TABLE team_members ADD COLUMN IF NOT EXISTS embedding vector(1536);
ALTER TABLE notes ADD COLUMN IF NOT EXISTS embedding vector(1536);

-- Create vector indexes for fast similarity search
CREATE INDEX IF NOT EXISTS idx_goals_embedding ON goals 
    USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
    
CREATE INDEX IF NOT EXISTS idx_tasks_embedding ON tasks 
    USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
    
CREATE INDEX IF NOT EXISTS idx_meetings_embedding ON meetings 
    USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
    
CREATE INDEX IF NOT EXISTS idx_team_members_embedding ON team_members 
    USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
    
CREATE INDEX IF NOT EXISTS idx_notes_embedding ON notes 
    USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- RPC function for semantic search across all entities
CREATE OR REPLACE FUNCTION semantic_search(
    query_embedding vector(1536),
    match_threshold float DEFAULT 0.7,
    match_count int DEFAULT 10
)
RETURNS TABLE (
    entity_type text,
    entity_id uuid,
    title text,
    content text,
    similarity float
) AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            'goal' as entity_type,
            id as entity_id,
            name as title,
            description as content,
            1 - (embedding <=> query_embedding) as similarity
        FROM goals
        WHERE embedding IS NOT NULL
            AND NOT is_deleted
            AND 1 - (embedding <=> query_embedding) > match_threshold
        
        UNION ALL
        
        SELECT 
            'task' as entity_type,
            id as entity_id,
            name as title,
            description as content,
            1 - (embedding <=> query_embedding) as similarity
        FROM tasks
        WHERE embedding IS NOT NULL
            AND NOT is_deleted
            AND 1 - (embedding <=> query_embedding) > match_threshold
        
        UNION ALL
        
        SELECT 
            'meeting' as entity_type,
            id as entity_id,
            title as title,
            COALESCE(notes, '') as content,
            1 - (embedding <=> query_embedding) as similarity
        FROM meetings
        WHERE embedding IS NOT NULL
            AND NOT is_deleted
            AND 1 - (embedding <=> query_embedding) > match_threshold
        
        UNION ALL
        
        SELECT 
            'note' as entity_type,
            id as entity_id,
            title as title,
            content as content,
            1 - (embedding <=> query_embedding) as similarity
        FROM notes
        WHERE embedding IS NOT NULL
            AND NOT is_deleted
            AND 1 - (embedding <=> query_embedding) > match_threshold
    ) combined
    ORDER BY similarity DESC
    LIMIT match_count;
END;
$$ LANGUAGE plpgsql;
```

**Acceptance:**
- ✅ pgvector extension enabled
- ✅ Vector columns added to all tables
- ✅ Indexes created successfully
- ✅ RPC function works

---

### **Task 2.2: Embedding Service (2 hours)**

**File:** `Services/AI/EmbeddingService.cs` (150 lines)

```csharp
public class EmbeddingService
{
    private static readonly Lazy<EmbeddingService> _instance = new();
    public static EmbeddingService Instance => _instance.Value;
    
    private readonly HttpClient _httpClient = new();
    private string? _apiKey;
    
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        // Use Gemini embedding API
        var response = await _httpClient.PostAsync(
            "https://generativelanguage.googleapis.com/v1/models/embedding-001:embedContent",
            new StringContent(JsonSerializer.Serialize(new
            {
                content = new { parts = new[] { new { text } } }
            })));
            
        var result = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(
            await response.Content.ReadAsStreamAsync());
            
        return result.Embedding.Values;
    }
    
    public async Task<float[]> GetEmbeddingBatchAsync(List<string> texts)
    {
        // Batch processing for efficiency
    }
}
```

**Acceptance:**
- ✅ Can generate embeddings
- ✅ Handles API errors gracefully
- ✅ Batch processing works
- ✅ Caching prevents duplicate calls

---

### **Task 2.3: Vector Store Repository (3 hours)**

**File:** `Services/AI/VectorStoreRepository.cs` (300 lines)

```csharp
public class VectorStoreRepository
{
    public async Task<Guid> IndexGoalAsync(Goal goal)
    {
        var text = $"{goal.Name} {goal.Description}";
        var embedding = await EmbeddingService.Instance.GetEmbeddingAsync(text);
        
        var client = SupabaseService.Instance.Client;
        await client.Rpc("update_goal_embedding", new
        {
            goal_id = goal.Id,
            embedding_vector = embedding
        });
        
        return goal.Id;
    }
    
    public async Task<List<SearchResult>> SearchAsync(string query, int limit = 10)
    {
        var queryEmbedding = await EmbeddingService.Instance.GetEmbeddingAsync(query);
        
        var results = await client.Rpc("semantic_search", new
        {
            query_embedding = queryEmbedding,
            match_count = limit
        });
        
        return results.Select(r => new SearchResult
        {
            EntityType = r.EntityType,
            EntityId = r.EntityId,
            Title = r.Title,
            Content = r.Content,
            Similarity = r.Similarity
        }).ToList();
    }
}
```

**Acceptance:**
- ✅ Can index entities
- ✅ Search returns relevant results
- ✅ Similarity scores accurate
- ✅ Multi-entity search works

---

### **Task 2.4: Entity Indexers (4 hours)**

**5 Indexers to Create:**
1. `GoalIndexer.cs` - Index goals when created/updated
2. `TaskIndexer.cs` - Index tasks
3. `MeetingIndexer.cs` - Index meetings
4. `TeamMemberIndexer.cs` - Index team profiles
5. `NoteIndexer.cs` - Index notes

**Pattern:**
```csharp
public class GoalIndexer
{
    public async Task IndexAsync(Goal goal)
    {
        await VectorStoreRepository.Instance.IndexGoalAsync(goal);
    }
    
    public async Task IndexAllAsync()
    {
        var goals = await GoalsService.Instance.GetMyGoalsAsync();
        foreach (var goal in goals)
        {
            await IndexAsync(goal);
        }
    }
}
```

**Wire up to services:**
```csharp
// In GoalsService.cs
public async Task<Guid> CreateGoalAsync(Goal goal)
{
    var id = await base.CreateGoalAsync(goal);
    
    // Index for search
    _ = Task.Run(() => GoalIndexer.Instance.IndexAsync(goal));
    
    return id;
}
```

**Acceptance:**
- ✅ Auto-index on create/update
- ✅ Bulk indexing works
- ✅ Doesn't block UI
- ✅ Error handling

---

### **Task 2.5: Command Palette UI (4 hours)**

**File:** `Views/Dialogs/CommandPaletteDialog.axaml` (400 lines)

**Design:**
```xml
<Window Width="600" Height="500">
    <Grid RowDefinitions="Auto,*">
        <!-- Search Input -->
        <TextBox Grid.Row="0" 
                 Text="{Binding SearchQuery, Mode=TwoWay}"
                 Watermark="Search anything..."
                 FontSize="16"/>
        
        <!-- Results -->
        <ListBox Grid.Row="1" ItemsSource="{Binding Results}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <Grid ColumnDefinitions="Auto,*,Auto">
                        <PathIcon Grid.Column="0" Data="{Binding Icon}"/>
                        <StackPanel Grid.Column="1">
                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold"/>
                            <TextBlock Text="{Binding Description}" Opacity="0.7"/>
                        </StackPanel>
                        <TextBlock Grid.Column="2" Text="{Binding Similarity}" 
                                   Opacity="0.5"/>
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</Window>
```

**ViewModel:**
```csharp
public class CommandPaletteViewModel : ViewModelBase
{
    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            SetProperty(ref _searchQuery, value);
            _ = SearchAsync(value);
        }
    }
    
    private async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            return;
        }
        
        var results = await VectorStoreRepository.Instance.SearchAsync(query);
        Results.Clear();
        foreach (var result in results)
        {
            Results.Add(result);
        }
    }
}
```

**Acceptance:**
- ✅ Opens with Ctrl+K
- ✅ Search as you type
- ✅ Navigates to entity on select
- ✅ Shows similarity scores
- ✅ Fast (<300ms latency)

---

### **Task 2.6: RAG for AI Chat (2 hours)**

**Enhance AIContextService:**
```csharp
public async Task<string> GetCurrentContextAsync()
{
    var context = new StringBuilder();
    
    // ... existing context
    
    // Add relevant context via RAG
    var recentMessages = ChatViewModel.Messages.TakeLast(5)
        .Select(m => m.Content);
    var combinedQuery = string.Join(" ", recentMessages);
    
    var relevantDocs = await VectorStoreRepository.Instance.SearchAsync(
        combinedQuery, limit: 5);
    
    if (relevantDocs.Any())
    {
        context.AppendLine("\nRelevant Context:");
        foreach (var doc in relevantDocs)
        {
            context.AppendLine($"- {doc.Title}: {doc.Content}");
        }
    }
    
    return context.ToString();
}
```

**Acceptance:**
- ✅ AI gets relevant context
- ✅ Responses more accurate
- ✅ Can answer about historical data

---

## 📦 PHASE 3: CHAT POLISH & TESTING (Priority 3)
**Time Estimate:** 2-3 days  
**Value:** HIGH - Complete existing investment

### Task Breakdown

### **Task 3.1: Settings Integration (2 hours)**

**File:** `Views/SettingsView.axaml`

**Add AI Settings Tab:**
```xml
<TabControl>
    <!-- Existing tabs... -->
    
    <TabItem Header="AI Assistant">
        <StackPanel Spacing="16" Margin="24">
            <TextBlock Text="AI Configuration" FontSize="20" FontWeight="SemiBold"/>
            
            <!-- Gemini API Key -->
            <StackPanel Spacing="8">
                <TextBlock Text="Google Gemini API Key"/>
                <TextBox Text="{Binding GeminiApiKey, Mode=TwoWay}"
                         Watermark="Enter your API key..."
                         PasswordChar="*"/>
                <TextBlock Text="Get your key at: https://ai.google.dev"
                           FontSize="12" Opacity="0.7"/>
            </StackPanel>
            
            <!-- Model Selection -->
            <StackPanel Spacing="8">
                <TextBlock Text="Model"/>
                <ComboBox ItemsSource="{Binding AvailableModels}"
                          SelectedItem="{Binding SelectedModel}"/>
            </StackPanel>
            
            <!-- Test Connection -->
            <Button Content="Test Connection" Command="{Binding TestConnectionCommand}"/>
            <TextBlock Text="{Binding ConnectionStatus}" Foreground="Green"/>
        </StackPanel>
    </TabItem>
</TabControl>
```

**Acceptance:**
- ✅ API key saves to user preferences
- ✅ Test connection works
- ✅ Model selection persists

---

### **Task 3.2: Conversation Persistence (3 hours)**

**Database Migration:**
```sql
CREATE TABLE chat_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES team_members(id),
    title TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE chat_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES chat_conversations(id),
    role TEXT NOT NULL CHECK (role IN ('user', 'assistant', 'system')),
    content TEXT NOT NULL,
    function_name TEXT,
    function_result TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Repository:**
```csharp
public class ChatRepository
{
    public async Task SaveConversationAsync(List<ChatMessage> messages)
    {
        // Save to database
    }
    
    public async Task<List<ChatMessage>> LoadConversationAsync(Guid conversationId)
    {
        // Load from database
    }
}
```

**Acceptance:**
- ✅ Conversations saved on close
- ✅ Can load conversation history
- ✅ Export to markdown works

---

### **Task 3.3: End-to-End Testing (4 hours)**

**Test Scenarios:**

1. **Basic Chat** (30 min)
   - ✅ Send message, get response
   - ✅ Message history shows correctly
   - ✅ Error handling for API failures

2. **Function Calling** (2 hours)
   - ✅ create_task → task appears in TasksView
   - ✅ create_goal → goal appears in GoalsView
   - ✅ create_meeting → meeting scheduled
   - ✅ search_team_members → finds correct people
   - ✅ get_tasks → returns current tasks
   - ✅ All 12 functions tested

3. **Context Gathering** (30 min)
   - ✅ AI knows current user
   - ✅ AI knows active projects
   - ✅ AI knows open tasks
   - ✅ AI uses context in responses

4. **Edge Cases** (1 hour)
   - ✅ API key invalid → clear error
   - ✅ Network offline → graceful fallback
   - ✅ Rate limiting → backoff retry
   - ✅ Large conversations → pagination

---

## 🎯 SUCCESS METRICS

### Phase 1: AI Insights
- ✅ 6 analyzers running
- ✅ Insights display in Briefing
- ✅ Daily background processing works
- ✅ Users can dismiss/act on insights
- **Target:** Generate 5-10 insights per user per week

### Phase 2: Vector Search
- ✅ All 5 entity types indexed
- ✅ Ctrl+K search functional
- ✅ Search results relevant (>0.7 similarity)
- ✅ Search latency <300ms
- **Target:** 80%+ user satisfaction with search

### Phase 3: Chat Complete
- ✅ All 12 function tools tested
- ✅ API key configurable
- ✅ Conversations persist
- ✅ Zero critical bugs
- **Target:** 95%+ function call success rate

---

## 🚀 EXECUTION STRATEGY

### Week 1: AI Insights Foundation
- **Days 1-2:** Database + Core Models (Tasks 1.1-1.3)
- **Days 3-4:** Engine + First Analyzer (Tasks 1.4-1.5)
- **Day 5:** UI Integration (Tasks 1.8-1.9)

### Week 2: Insights Complete + Vector Start
- **Days 1-2:** Remaining Analyzers (Tasks 1.6-1.7)
- **Day 3:** Background Processing (Task 1.10)
- **Days 4-5:** Vector Database Setup (Tasks 2.1-2.2)

### Week 3: Vector Search Complete
- **Days 1-2:** Vector Store + Indexers (Tasks 2.3-2.4)
- **Days 3-4:** Command Palette UI (Task 2.5)
- **Day 5:** RAG Integration (Task 2.6)

### Week 4: Chat Polish & Testing
- **Days 1-2:** Settings + Persistence (Tasks 3.1-3.2)
- **Days 3-5:** Testing + Bug Fixes (Task 3.3)

---

## 📊 RISK ASSESSMENT

### HIGH RISK
1. **pgvector Performance** - Large datasets may be slow
   - Mitigation: Proper indexing, pagination
2. **Embedding API Costs** - Could get expensive
   - Mitigation: Caching, batch processing
3. **Insight Accuracy** - False positives annoy users
   - Mitigation: Confidence scores, user feedback

### MEDIUM RISK
1. **Database Migrations** - Schema changes on production
   - Mitigation: Test thoroughly, have rollback plan
2. **Background Processing** - Could impact performance
   - Mitigation: Rate limiting, off-peak scheduling

### LOW RISK
1. **UI Integration** - Well-defined patterns exist
2. **Testing** - Clear acceptance criteria

---

## 📝 COMPLETION CHECKLIST

### Phase 1: AI Insights
- [ ] Database migration deployed
- [ ] 6 analyzers implemented and tested
- [ ] Insight cards display in Briefing
- [ ] Background scheduler running
- [ ] Dismiss/Act workflows functional
- [ ] Zero build errors/warnings

### Phase 2: Vector Search
- [ ] pgvector extension enabled
- [ ] 5 entity indexers deployed
- [ ] Command palette (Ctrl+K) functional
- [ ] Search results accurate (>0.7 similarity)
- [ ] RAG enhancing AI responses
- [ ] Zero build errors/warnings

### Phase 3: Chat Complete
- [ ] API key configurable in Settings
- [ ] All 12 function tools tested
- [ ] Conversations persist across sessions
- [ ] Export to markdown works
- [ ] Error handling robust
- [ ] Zero critical bugs

---

**Ready to Execute:** All tasks defined, dependencies clear, acceptance criteria set.  
**Next Step:** Begin Phase 1, Task 1.1 - Database Schema
