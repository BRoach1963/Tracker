# AI Data Access Strategy
## Making the Help Bot Awesome with Limited Message Size

### Current Architecture (What We Have)
- ✅ **Documentation RAG**: Docs are vectorized and retrieved based on questions
- ✅ **Static System Context**: ~2000 char summary of all data sent with every request
- ✅ **Basic Data Summary**: Limited to 10 team members, 5 tasks, 5 meetings, etc.
- ❌ **Problem**: Can't answer detailed questions about specific employees, tasks, or historical data
- ❌ **Problem**: Data is truncated heavily to fit in context window

### Gemini Flash 1.5 Limits (Current Tier)
- **Input tokens**: ~1M tokens (~750K words) - **NOT the problem**
- **Output tokens**: 8K max
- **RPM**: 15 requests/minute
- **TPM**: 1M tokens/minute
- **Cost**: FREE tier

**KEY INSIGHT**: We actually have PLENTY of input capacity! The real constraint is:
1. **Response quality** degrades with massive context
2. **Processing time** increases with large context
3. **Cost management** (future when you upgrade)

---

## Strategy: Hybrid RAG for Data + Docs

### Phase 1: Quick Wins (Implement Now - 1-2 hours)
**Goal**: Better context relevance without vectorizing data yet

#### 1.1 Smart Context Detection
Detect what the user is asking about and load ONLY that data:

```
Question: "When did John start?"
→ Detect: Team member query for "John"
→ Load: Full details for John only (not all 50+ team members)
→ Context: 200 chars instead of 2000
```

#### 1.2 Query-Specific Data Loading
Instead of loading ALL data types, detect intent:

| Question Type | Load Only |
|--------------|-----------|
| "Team member X..." | That specific team member's full profile |
| "What tasks are due..." | Task list (not meetings, OKRs, etc.) |
| "Upcoming meetings..." | Meeting list only |
| "OKR status..." | OKR data only |

**Implementation**:
- Add `GetContextForQuery(string question)` method
- Use simple keyword detection (hire date → team member, task → tasks)
- Returns targeted context (500-1000 chars instead of 2000)

#### 1.3 Expand System Context Limit
Since Gemini Flash 1.5 supports 1M input tokens, we can be MUCH more generous:

- System context: **2000 → 10,000 chars**
- Relevant docs: **1200 → 5,000 chars**
- User data: **1200 → 8,000 chars**

This alone would solve most issues!

**Estimated Impact**: 80% of questions answered correctly
**Time to Implement**: 1-2 hours

---

### Phase 2: Vectorize User Data (Medium-term - 4-6 hours)

#### 2.1 Data Vectorization Architecture
Extend the existing vector store to include user data:

**Current**:
```
VectorStore
├── Documentation chunks (✅ implemented)
└── [Your data - NOT vectorized yet]
```

**Proposed**:
```
VectorStore
├── Documentation chunks (existing)
├── Team Member embeddings
├── Meeting embeddings  
├── Task embeddings
├── OKR embeddings
├── KPI embeddings
└── Project embeddings
```

#### 2.2 Embedding Strategy
Each entity becomes a searchable "document":

**Team Member Embedding**:
```
"John Smith (Software Engineer, Senior Level, Full Stack Specialist)
Hired: January 15, 2023
Birthday: March 5
Email: john.smith@company.com
Status: Active
Recent feedback: Strong performer in Q3 2024, leading the API redesign project.
Current OKRs: Improve API response time by 40%, reduce bug count by 25%
Upcoming 1:1: December 20, 2025"
```

**Task Embedding**:
```
"Task: Redesign authentication flow
Status: In Progress (60% complete)
Assigned to: Sarah Johnson  
Due: December 31, 2025
Priority: High
Project: Security Enhancement Initiative
Related OKR: Improve platform security score to 95%"
```

#### 2.3 Semantic Search Flow
When user asks a question:

1. **Embed the question** → vector
2. **Search ALL categories** (docs + team + tasks + meetings + etc.)
3. **Retrieve top 3-5 results** across all types
4. **Build dynamic context** from results only

**Example**:
```
Question: "Who's working on security and when did they start?"

Vector search returns:
1. Team Member: Sarah Johnson (hired Jan 2024, works on security)
2. Task: Security Enhancement Initiative (Sarah assigned)  
3. Project: Platform Security Upgrade (Sarah is lead)

Context sent to AI (auto-generated):
"Sarah Johnson, Software Engineer, hired January 2024, currently 
leading Security Enhancement Initiative. Task due Dec 31, 2025..."
```

#### 2.4 Background Indexing
On application startup (or nightly):

```csharp
DataIndexer.Instance.IndexAllDataAsync()
├── Index team members → vectors
├── Index meetings → vectors
├── Index tasks → vectors
├── Index OKRs → vectors
├── Index KPIs → vectors
└── Index projects → vectors

Time: ~10-30 seconds for 1000s of records
Re-index: Daily or on data changes
```

**Estimated Impact**: 95% of questions answered correctly with full detail
**Time to Implement**: 4-6 hours

---

### Phase 3: Advanced Optimization (Long-term - 8-10 hours)

#### 3.1 Multi-Hop Reasoning
Chain multiple queries for complex questions:

```
Question: "Show me everyone on John's team who has overdue tasks"

Step 1: Find John's team members
Step 2: Get their tasks  
Step 3: Filter by overdue
Step 4: Format response
```

#### 3.2 Temporal Context Windows
Keep recent context in memory:

- Last 7 days of activity
- Recently viewed team members
- Active projects only

#### 3.3 Intelligent Caching
Cache frequently asked data:

- Team roster (refresh hourly)
- Today's meetings (refresh every 15 min)
- Active tasks (refresh every 5 min)

#### 3.4 Function Calling (Gemini Pro feature)
Let AI call specific functions:

```javascript
AI detects: "Get John's hire date"
→ Calls: GetTeamMember("John")  
→ Returns: Full record
→ AI extracts: hire_date
```

**Estimated Impact**: 99% accuracy, handles complex queries
**Time to Implement**: 8-10 hours

---

## Recommended Implementation Order

### Week 1: Quick Wins
1. ✅ **Expand context limits** (2K → 10K chars) - 15 minutes
2. ✅ **Add hire dates to team data** (DONE!) - 15 minutes
3. **Smart context detection** - 2 hours
   - Detect team member names in questions
   - Load full profile for that person only
   - Detect question type (task/meeting/OKR)

### Week 2: Data Vectorization
1. **Extend VectorStore** for data types (Supabase/pgvector-backed) - 2 hours
2. **Create DataIndexer service** - 2 hours
3. **Index team members** - 1 hour
4. **Index meetings, tasks, goals, metrics, and targets** - 1 hour
5. **Test semantic search end-to-end via Supabase** - 1 hour

### Week 3: Integration & Polish
1. **Wire up semantic search** to HelpBot - 1 hour
2. **Add background indexing** - 1 hour
3. **Optimize search relevance** - 2 hours
4. **Add usage telemetry** - 1 hour

---

## Technical Implementation Sketch

### New Files Needed:
```
Services/
├── AI/
│   ├── DataIndexer.cs (NEW - indexes user data)
│   ├── DataEmbeddingService.cs (NEW - creates embeddings for entities)
│   └── SmartContextBuilder.cs (NEW - builds context based on query intent)
```

### Modified Files:
```
Services/
├── HelpBotContextService.cs (add smart detection)
├── AI/VectorStore.cs (add data collections, backed by Supabase/pgvector)
```

### Key Code Pattern:

```csharp
// DataIndexer.cs
public async Task IndexTeamMembersAsync()
{
    var members = await TrackerDbManager.Instance.GetTeamMembersAsync();
    
    foreach (var member in members)
    {
        // Create rich text representation
        var text = $@"{member.FullName} ({member.JobTitle})
            Hired: {member.HireDate:MMMM d, yyyy}
            Email: {member.Email}
            Birthday: {member.BirthDay:MMMM d}
            Status: {(member.IsActive ? "Active" : "Inactive")}";
        
        // Get embedding
        var embedding = await EmbeddingService.Instance.GetEmbeddingAsync(text);
        
        // Store in vector DB
        await VectorStore.Instance.AddAsync(
            id: $"member_{member.Id}",
            embedding: embedding,
            content: text,
            metadata: new { type = "team_member", id = member.Id }
        );
    }
}

// SmartContextBuilder.cs  
public async Task<string> GetSmartContextAsync(string question)
{
    // Embed question
    var questionEmbedding = await EmbeddingService.Instance.GetEmbeddingAsync(question);
    
    // Search across ALL data types (docs + team + tasks + etc.)
    var results = await VectorStore.Instance.SearchAsync(questionEmbedding, topK: 5);
    
    // Build context from results
    var context = new StringBuilder();
    foreach (var result in results)
    {
        context.AppendLine(result.Content);
    }
    
    return context.ToString();
}
```

---

## Cost & Performance Projections

### Storage Requirements:
- **Embeddings**: ~1.5KB per entity × 1000 entities = ~1.5MB (stored in Supabase Postgres with pgvector)
- **Vector DB**: Supabase/pgvector (managed Postgres) instead of local SQLite
- **Indexing time**: ~30 seconds for 1000 entities (network round-trips to Supabase included)

### API Costs (if you upgrade to paid):
- **Current**: Free tier (no cost)
- **With full vectorization**: Same (indexing is local)
- **Gemini API calls**: Same number of requests
- **Future Pro tier**: ~$0.10/1M tokens (still very cheap)

### Performance:
- **Current response time**: 2-4 seconds
- **With vectorization**: 1-3 seconds (faster - less data sent)
- **Context relevance**: 60% → 95%+

---

## Recommendation: Start with Phase 1

**Why**: 
- 80% of the benefit for 5% of the effort
- Test if expanded context limits solve your needs
- Learn what questions users actually ask
- Then decide if Phase 2 is needed

**Action Items**:
1. Increase context limits to 10K chars (15 min)
2. Add smart context detection (2 hours)
3. Test with real questions for 1 week
4. Decide on Phase 2 based on results

Would you like me to implement Phase 1 now?
