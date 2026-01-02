# Tracker AI Integration
## Technical Specification Document

**Document Version:** 1.0  
**Date:** December 27, 2025  
**Status:** Production Implementation

---

## Executive Summary

Tracker implements a sophisticated AI-powered assistant called "Oracle" that provides natural language interaction with user data. The system uses a Hybrid RAG (Retrieval Augmented Generation) architecture combining semantic search over documentation and user data with Google Gemini 2.5 Pro for natural language understanding and function execution.

**Key Capabilities:**
- Natural language queries about user data (team members, meetings, tasks, OKRs, KPIs, projects)
- Semantic search across documentation and entity data
- Function calling for creating and modifying data (meetings, tasks, KPIs, OKRs, etc.)
- Budget tracking and cost management
- Incremental indexing for performance

---

## Data Analysis & Understanding

### How Oracle Understands Your Data

Oracle doesn't just store your data—it **understands** it. Through semantic vectorization, every piece of information in Tracker becomes queryable through natural language. This is the core differentiator of our AI integration.

### The Semantic Understanding Pipeline

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        RAW DATA ENTITIES                                 │
│  Team Members │ 1:1 Meetings │ Tasks │ OKRs │ KPIs │ Projects │ Goals  │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     RICH TEXT TRANSFORMATION                             │
│  Each entity is converted to a human-readable narrative:                │
│                                                                          │
│  TeamMember → "Sarah Chen, Senior Developer, hired March 15, 2023,      │
│               birthday July 22, email sarah@company.com, Active"         │
│                                                                          │
│  Meeting → "1:1 with Sarah Chen on Dec 20, 2024. Agenda: Career growth, │
│            Q4 goals review. Notes: Discussed promotion timeline..."      │
│                                                                          │
│  Task → "Complete API documentation, assigned to Sarah Chen,            │
│          due Jan 15, 2025, High priority, 60% complete"                  │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      VECTOR EMBEDDING (768 dimensions)                   │
│  Text → Gemini text-embedding-004 → [0.023, -0.156, 0.892, ...]        │
│                                                                          │
│  Vectors capture SEMANTIC MEANING, not just keywords:                   │
│  • "hire date" ≈ "start date" ≈ "when did they join"                   │
│  • "1:1" ≈ "one-on-one" ≈ "meeting with"                               │
│  • "overdue" ≈ "late" ≈ "past due date"                                │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      VECTOR DATABASE (SQLite)                            │
│  Enables similarity search across ALL your data simultaneously          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Types of Questions Oracle Can Answer

| Question Category | Example Questions | How It Works |
|------------------|-------------------|--------------|
| **Factual Lookups** | "When did Sarah start?" | Direct vector match on team member entity |
| **Cross-Entity** | "Who has meetings next week?" | Searches meetings, returns associated team members |
| **Aggregations** | "How many tasks are overdue?" | Context includes task data, LLM performs count |
| **Relationships** | "What's discussed in John's 1:1s?" | Finds meetings with John, extracts notes/agenda |
| **Comparisons** | "Which OKRs are behind schedule?" | Retrieves OKR data, LLM analyzes progress |
| **Trends** | "Who's been most active this month?" | LLM analyzes meeting/feedback frequency |
| **Recommendations** | "Who should I check in with?" | LLM reasons over meeting history gaps |
| **KPI Analysis** | "Which KPIs are off target?" | Retrieves KPI status, gap analysis, trends |
| **OKR Health** | "How are our Q4 OKRs tracking?" | Analyzes key results, progress, days remaining |
| **Survey Insights** | "What's the sentiment in our pulse surveys?" | Analyzes survey responses, ratings, feedback |

### Semantic Search vs. Keyword Search

Traditional keyword search would fail on these queries. Oracle succeeds because of semantic understanding:

| User Query | Keyword Search | Semantic Search (Oracle) |
|------------|---------------|--------------------------|
| "When did John join the team?" | ❌ No match for "join" | ✅ Matches "Hire Date: Jan 15, 2023" |
| "Anyone celebrating soon?" | ❌ No keyword match | ✅ Matches birthdays within 30 days |
| "What's Sarah working on?" | ❌ Too vague | ✅ Returns tasks, projects, OKRs for Sarah |
| "Upcoming conversations" | ❌ "conversations" ≠ "meetings" | ✅ Matches scheduled 1:1 meetings |
| "Team health check" | ❌ No direct match | ✅ Returns OKR progress, KPI status, overdue items |
| "What metrics need attention?" | ❌ No direct match | ✅ Finds off-target KPIs with gap analysis |
| "Survey feedback trends" | ❌ No direct match | ✅ Analyzes pulse survey responses & ratings |

### Data Context Building

When you ask a question, Oracle builds context in layers:

**Layer 1: Static Context (Always Included)**
- Team overview (names, titles, tenure)
- Upcoming meetings summary
- Active tasks summary
- OKR/KPI status
- Project status

**Layer 2: Semantic Search Results (Query-Specific)**
- Top 5 most relevant documentation chunks
- Top 5 most relevant data entities
- Combined and deduplicated

**Layer 3: Smart Filtering**
- Documentation vs. user data separated
- Metadata filtering (e.g., only active team members)
- Relevance threshold (min score 0.4-0.5)

### Example: Full Query Analysis

**User Question:** "What should I discuss with Sarah in our next 1:1?"

**Oracle's Analysis Process:**

1. **Embed Question** → 768-dimension vector representing "discuss Sarah 1:1"

2. **Semantic Search Results:**
   - Sarah's profile (hire date, role, recent activity)
   - Previous 1:1 notes with Sarah
   - Sarah's assigned tasks (especially overdue)
   - Sarah's OKR progress
   - Recent feedback about Sarah

3. **Context Sent to LLM:**
   ```
   Team Member: Sarah Chen, Senior Developer, hired March 2023
   
   Last 1:1 (Dec 10): Discussed promotion timeline, she wants to lead 
   the API project. Action item: Review her technical design doc.
   
   Current Tasks:
   - API documentation (overdue by 3 days)
   - Security audit preparation (due Dec 30)
   
   OKR Progress: "Improve API response time" - 65% complete
   
   Question: What should I discuss with Sarah in our next 1:1?
   ```

4. **LLM Response:**
   > "For your next 1:1 with Sarah, consider discussing:
   > 1. **API Documentation** - It's 3 days overdue. Check if she needs help.
   > 2. **Technical Design Doc** - You had an action item to review it.
   > 3. **Promotion Timeline** - Follow up on last meeting's discussion.
   > 4. **OKR Progress** - She's at 65% on API response time goal.
   > 5. **Security Audit** - Due Dec 30, ensure she's prepared."

### Data Freshness

Oracle's understanding stays current through incremental indexing:

| Trigger | Action |
|---------|--------|
| App Startup | Check for changes since last index |
| Data Modified | Entity re-indexed on next startup |
| Manual Refresh | User clicks "Refresh" in Help Bot |
| 24-hour threshold | Full re-index if stale |

---

## Architecture Overview

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              USER INTERFACE                                  │
│                         ┌─────────────────────┐                             │
│                         │   HelpBotControl    │                             │
│                         │   (Chat Interface)  │                             │
│                         └──────────┬──────────┘                             │
└────────────────────────────────────┼────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                             VIEWMODEL LAYER                                  │
│                         ┌──────────▼──────────┐                             │
│                         │  HelpBotViewModel   │                             │
│                         │  - Message history  │                             │
│                         │  - Loading states   │                             │
│                         │  - Context init     │                             │
│                         └──────────┬──────────┘                             │
└────────────────────────────────────┼────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                              SERVICE LAYER                                   │
│                                    │                                        │
│    ┌───────────────────────────────┼───────────────────────────────────┐   │
│    │                               │                                    │   │
│    │   ┌───────────────┐   ┌──────▼──────┐   ┌─────────────────────┐  │   │
│    │   │ SmartContext  │   │ HelpBot     │   │   GeminiChat        │  │   │
│    │   │ Builder       │◄──│ Context     │──►│   Service           │  │   │
│    │   │               │   │ Service     │   │   (API Client)      │  │   │
│    │   └───────┬───────┘   └──────┬──────┘   └───────────┬─────────┘  │   │
│    │           │                  │                      │            │   │
│    │           │           ┌──────▼──────┐               │            │   │
│    │           │           │ Document    │               │            │   │
│    │           │           │ Indexer     │               │            │   │
│    │           │           └──────┬──────┘               │            │   │
│    │           │                  │                      │            │   │
│    └───────────┼──────────────────┼──────────────────────┼────────────┘   │
│                │                  │                      │                 │
│    ┌───────────┼──────────────────┼──────────────────────┼───────────────┐│
│    │           │                  │                      │               ││
│    │   ┌───────▼───────┐   ┌──────▼──────┐   ┌──────────▼──────────┐   ││
│    │   │   Embedding   │   │   Vector    │   │   AIFunction        │   ││
│    │   │   Service     │◄──│   Store     │   │   Service           │   ││
│    │   │               │   │  (SQLite)   │   │   (CRUD Actions)    │   ││
│    │   └───────────────┘   └──────┬──────┘   └─────────────────────┘   ││
│    │                              │                                     ││
│    │   ┌──────────────────────────┼──────────────────────────────────┐ ││
│    │   │                   DATA INDEXERS                              │ ││
│    │   │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌───────────┐ │ ││
│    │   │  │TeamMember  │ │ Meeting    │ │   Task     │ │  Goal     │ │ ││
│    │   │  │ Indexer    │ │  Indexer   │ │  Indexer   │ │ Indexer   │ │ ││
│    │   │  └────────────┘ └────────────┘ └────────────┘ └───────────┘ │ ││
│    │   └──────────────────────────────────────────────────────────────┘ ││
│    │                        AI SUBSYSTEM                                 ││
│    └─────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │                       AIUsageTracker                                 │ │
│   │   - Request counting  - Cost estimation  - Budget enforcement       │ │
│   └─────────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                            EXTERNAL SERVICES                                 │
│                         ┌──────────▼──────────┐                             │
│                         │  Google Gemini API  │                             │
│                         │  - 2.5 Pro Model    │                             │
│                         │  - Embeddings API   │                             │
│                         └─────────────────────┘                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. HelpBotViewModel (`ViewModels/HelpBotViewModel.cs`)

**Purpose:** Primary orchestrator for the AI chat interface.

**Responsibilities:**
- Manage chat message history (ObservableCollection<ChatMessageViewModel>)
- Handle user input and command execution
- Initialize RAG system on startup
- Manage loading states and status messages
- Track usage statistics display

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Messages` | ObservableCollection<ChatMessageViewModel> | Chat history |
| `InputText` | string | Current user input |
| `IsLoading` | bool | Request in progress flag |
| `IsAvailable` | bool | API configured and available |
| `StatusMessage` | string | Current operation status |
| `UsageSummary` | string | Budget usage display |
| `BudgetUsedPercent` | decimal | Percentage of monthly budget consumed |

**Commands:**
| Command | Action |
|---------|--------|
| `SendCommand` | Submit user message to AI |
| `ClearCommand` | Clear chat history |
| `CancelCommand` | Cancel in-progress request |
| `RefreshContextCommand` | Force re-index of user data |

**Initialization Flow:**
```
1. Constructor called
2. Add welcome message
3. Initialize RAG (async):
   a. Index documentation (DocumentIndexer)
   b. Index user data (DataIndexer)
   c. Build system context (HelpBotContextService)
4. Ready for queries
```

---

### 2. GeminiChatService (`Services/GeminiChatService.cs`)

**Purpose:** HTTP client for Google Gemini API communication.

**Configuration:**
| Setting | Value | Description |
|---------|-------|-------------|
| Base URL | `generativelanguage.googleapis.com/v1beta` | Gemini REST API |
| Default Model | `gemini-2.5-pro` | Primary LLM |
| Max Tokens | 1024 | Response token limit |
| Timeout | 30 seconds | Request timeout |
| Max Request Size | 30,000 chars | Hard limit to prevent failures |

**API Integration:**
- Uses REST API (not SDK) for compatibility
- Supports function calling via tool declarations
- Automatic retry not implemented (handled by user)

**Function Calling Flow:**
```
1. Send request with tool declarations
2. If response contains FunctionCall:
   a. Execute function via AIFunctionService
   b. Add function result to conversation
   c. Make follow-up request for natural language response
3. Return final text response
```

**Error Handling:**
| Error | Response |
|-------|----------|
| 429 Too Many Requests | "You're sending too many messages. Please wait a minute." |
| 503 Service Unavailable | "The AI service is temporarily unavailable." |
| Invalid API Key | "API key is invalid. Please check your settings." |
| Request too large | "The request is too large. Try asking a simpler question." |

---

### 3. HelpBotContextService (`Services/HelpBotContextService.cs`)

**Purpose:** Builds context for AI requests using RAG.

**Context Limits:**
| Context Type | Max Size | Description |
|--------------|----------|-------------|
| System Context | 10,000 chars | Instructions + user data summary |
| Relevant Docs | 5,000 chars | RAG-retrieved documentation |
| User Data | 8,000 chars | Summary of team, meetings, tasks, etc. |

**System Context Structure:**
```
1. Core Instructions
   - AI persona definition ("Oracle")
   - Feature descriptions
   - Date/time context for relative date parsing
   
2. User Data Summary
   - Team Overview (up to 25 members)
   - Upcoming 1:1 Meetings (up to 15)
   - Active Tasks (up to 15)
   - Current OKRs (up to 10)
   - KPI Status (up to 10)
   - Active Projects (up to 10)
```

**RAG Search Process:**
```
1. Receive user question
2. Generate embedding for question (EmbeddingService)
3. Search VectorStore with minScore=0.4
4. Return top 5 relevant chunks
5. Combine with user data context
```

---

### 4. VectorStore (`Services/AI/VectorStore.cs`)

**Purpose:** Local SQLite-based vector database for semantic search.

**Storage Location:** `%LocalAppData%\Tracker\vectors.db`

**Schema:**
```sql
CREATE TABLE document_chunks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doc_id TEXT NOT NULL,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding BLOB NOT NULL,
    metadata TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(doc_id, chunk_index)
);

CREATE INDEX idx_doc_id ON document_chunks(doc_id);
```

**Embedding Storage:**
- Format: Raw bytes (4 bytes per float)
- Dimensions: 768 (text-embedding-004)
- Compression: None (SQLite handles storage)

**Search Algorithm:**
- Type: Brute-force cosine similarity (suitable for <100K vectors)
- Optimization: Candidates filtered by minScore before sorting
- Future: Consider approximate nearest neighbor for scale

**Key Methods:**
| Method | Description |
|--------|-------------|
| `StoreChunkAsync` | Store single document chunk |
| `StoreBatchAsync` | Store multiple chunks (transactional) |
| `SearchAsync` | Similarity search with top-K results |
| `AddAsync` | Store entity vector with metadata |
| `DeleteByMetadataAsync` | Remove vectors by metadata filter |

---

### 5. EmbeddingService (`Services/AI/EmbeddingService.cs`)

**Purpose:** Generate vector embeddings using Gemini API.

**Model:** `text-embedding-004`

**Specifications:**
| Parameter | Value |
|-----------|-------|
| Dimensions | 768 |
| Max Batch Size | 100 texts |
| Rate Limiting | 100ms delay between batches |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `GetEmbeddingAsync` | Single text → 768-dim vector |
| `GetEmbeddingsBatchAsync` | Multiple texts → vectors (batched) |
| `CosineSimilarity` | Calculate similarity score (0-1) |

**Cosine Similarity Formula:**
```
similarity = dot(a, b) / (||a|| * ||b||)
```

---

### 6. DocumentIndexer (`Services/AI/DocumentIndexer.cs`)

**Purpose:** Index help documentation for semantic search.

**Source:** `Resources/Help/*.md`

**Chunking Strategy:**
| Parameter | Value |
|-----------|-------|
| Max Chunk Size | 500 characters |
| Chunk Overlap | 50 characters |
| Min Chunk Size | 100 characters |

**Chunking Algorithm:**
```
1. Load markdown file
2. Split by headers (##, ###) for semantic boundaries
3. Further split large sections by paragraphs
4. Ensure chunk overlap for context continuity
5. Generate embedding for each chunk
6. Store in VectorStore with doc_id = filename
```

**Indexing Trigger:**
- On first startup (no vectors exist)
- On document count mismatch
- Manual reindex via RefreshContextCommand

---

### 7. DataIndexer (`Services/AI/DataIndexer.cs`)

**Purpose:** Coordinate indexing of all user data entities.

**Indexed Entity Types:**
| Entity | Indexer Class | Fields Indexed |
|--------|---------------|----------------|
| Team Members | TeamMemberIndexer | Name, title, hire date, birthday, email, phone, status |
| 1:1 Meetings | MeetingIndexer | Date, attendee, status, agenda, notes |
| Tasks | TaskIndexer | Description, owner, due date, status |
| OKRs | GoalIndexer | Title, owner, progress, status, key results, linked items, time period |
| KPIs | GoalIndexer | Name, owner, value, target, progress, status, category, gap analysis |
| Projects | GoalIndexer | Name, status, progress, description |
| Pulse Surveys | PulseSurveyIndexer | Title, questions, responses, ratings, feedback analysis |

**Incremental Indexing:**
- Tracks last indexed time in `%AppData%\Tracker\LastIndexed.txt`
- On subsequent runs, only indexes entities modified since last run
- Uses `LastModifiedAt` field from `AuditableEntity` base class
- Full re-index triggered if >24 hours since last index

**Index Progress Events:**
```csharp
public event EventHandler<IndexProgressEventArgs>? ProgressChanged;
```

---

### 8. Entity Indexers (TeamMemberIndexer, MeetingIndexer, TaskIndexer, GoalIndexer, PulseSurveyIndexer)

**Purpose:** Transform domain entities into searchable text representations.

**Base Class:** `EntityIndexerBase`

**Common Pattern:**
```csharp
protected override async Task IndexSingleEntityAsync(object entity)
{
    // 1. Extract entity data
    // 2. Build rich text representation
    // 3. Create metadata dictionary
    // 4. Call IndexEntityAsync(id, content, metadata)
}
```

**Team Member Text Format:**
```
John Smith
Job Title: Senior Developer
Hire Date: January 15, 2023
Birthday: March 5
Email: john.smith@company.com
Phone: (555) 123-4567
Status: Active
```

**Metadata Structure:**
```json
{
    "type": "team_member",
    "id": 42,
    "name": "John Smith",
    "is_active": true
}
```

---

### 9. SmartContextBuilder (`Services/SmartContextBuilder.cs`)

**Purpose:** Build query-specific context from vectorized data.

**Search Parameters:**
| Parameter | Value |
|-----------|-------|
| Top K | 5 results |
| Min Score | 0.5 (higher than doc search) |
| Max Context | 3,000 characters |

**Data Filtering:**
- Excludes documentation chunks (checks metadata for `type`)
- Only returns user data entities

---

### 10. AIFunctionService (`Services/AI/AIFunctionService.cs`)

**Purpose:** Execute AI-requested data operations.

**Available Functions:**
| Function | Parameters | Action |
|----------|------------|--------|
| `create_meeting` | team_member_name, date, notes | Create 1:1 meeting |
| `create_task` | description, owner_name, due_date | Create task |
| `create_kpi` | name, target_value, unit, current_value | Create KPI |
| `create_okr` | title, start_date, end_date, description | Create OKR |
| `create_feedback` | team_member_name, type, content | Create feedback |
| `create_project` | name, description, status | Create project |
| `create_goal` | team_member_name, title, target_date | Create goal |
| `create_note` | title, content | Create quick note |
| `search_team_members` | query | Search team members |
| `get_upcoming_meetings` | days | Get meetings in date range |
| `get_projects` | status | Get projects by status |
| `get_notes` | query | Search notes |

**Function Declaration Format (for Gemini):**
```json
{
    "name": "create_meeting",
    "description": "Creates a new 1:1 meeting with a team member.",
    "parameters": {
        "type": "object",
        "properties": {
            "team_member_name": {
                "type": "string",
                "description": "Name of the team member"
            },
            "date": {
                "type": "string",
                "description": "Date and time in standard format"
            }
        },
        "required": ["team_member_name", "date"]
    }
}
```

**Error Handling:**
- Team member not found → returns available member names
- Invalid date format → returns error with expected format
- Database errors → logged and returned as user message

---

### 11. AIUsageTracker (`Services/AI/AIUsageTracker.cs`)

**Purpose:** Track API usage and enforce budget limits.

**Pricing Model (Gemini 2.5 Pro):**
| Type | Price | Estimated chars/token |
|------|-------|----------------------|
| Input | $1.25 per 1M tokens | ~4 chars |
| Output | $5.00 per 1M tokens | ~4 chars |

**Calculated Rates:**
| Type | Cost per Character |
|------|-------------------|
| Input | $0.00000031 |
| Output | $0.00000125 |

**Usage Storage:** `%LocalAppData%\Tracker\ai_usage.json`

**Usage Data Structure:**
```json
{
    "month": "2025-12",
    "requestCount": 150,
    "totalInputChars": 450000,
    "totalOutputChars": 75000,
    "estimatedCost": 0.2334,
    "lastRequestTime": "2025-12-27T14:30:00Z"
}
```

**Budget Enforcement:**
| Setting | Default | Description |
|---------|---------|-------------|
| MonthlyBudget | $5.00 | Maximum monthly spend |
| BudgetWarningPercent | 80% | Warning threshold |
| EnforceBudgetLimit | true | Block requests when exceeded |

**Budget States:**
| State | Action |
|-------|--------|
| Under budget | Normal operation |
| Warning threshold | Display warning in UI |
| Budget exceeded | Block AI requests (if enforcement enabled) |

---

## Data Flow Diagrams

### Query Processing Flow

```
User Input: "When did John start?"
        │
        ▼
┌───────────────────┐
│ HelpBotViewModel  │
│ SendToAIAsync()   │
└────────┬──────────┘
         │
         ▼
┌───────────────────────────────────────────────────────┐
│ 1. Get Relevant Documentation                          │
│    HelpBotContextService.GetRelevantDocsAsync()       │
│    - Embed question                                    │
│    - Search VectorStore (docs)                         │
│    - Return top 5 chunks                               │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ 2. Get Relevant User Data                              │
│    SmartContextBuilder.GetDataContextForQueryAsync()  │
│    - Embed question                                    │
│    - Search VectorStore (entities)                     │
│    - Filter by metadata (type != documentation)        │
│    - Return top 5 data matches                         │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ 3. Build Enhanced Prompt                               │
│    combinedContext = relevantDocs + relevantData      │
│    enhancedQuestion = context + "\n\nQuestion: " + q  │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ 4. Send to Gemini API                                  │
│    GeminiChatService.GetResponseAsync()               │
│    - Build request with system context                 │
│    - Add tool declarations                             │
│    - POST to Gemini API                                │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ 5. Process Response                                    │
│    - Check for function calls → execute               │
│    - Record usage in AIUsageTracker                    │
│    - Return text response                              │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────┐
│ Add to Messages       │
│ Display in UI         │
└───────────────────────┘

Response: "John started on January 15, 2023."
```

### Function Calling Flow

```
User Input: "Schedule a 1:1 with Sarah for next Tuesday at 2pm"
        │
        ▼
┌───────────────────────────────────────────────────────┐
│ Gemini API Response                                    │
│ {                                                      │
│   "functionCall": {                                    │
│     "name": "create_meeting",                          │
│     "args": {                                          │
│       "team_member_name": "Sarah",                     │
│       "date": "2024-12-31 14:00"                       │
│     }                                                  │
│   }                                                    │
│ }                                                      │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ AIFunctionService.ExecuteFunctionAsync()              │
│ 1. Parse function name and arguments                   │
│ 2. Match to create_meeting handler                     │
│ 3. Find team member "Sarah" in database                │
│ 4. Create OneOnOne entity                              │
│ 5. Save to database                                    │
│ 6. Return: "✓ Created 1:1 meeting with Sarah..."      │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
┌───────────────────────────────────────────────────────┐
│ Follow-up Gemini Request                               │
│ - Add function result to conversation                  │
│ - Request natural language confirmation                │
└────────────────────────┬──────────────────────────────┘
                         │
                         ▼
Response: "I've scheduled your 1:1 meeting with Sarah
          for Tuesday, December 31st at 2:00 PM."
```

---

## Configuration

### User Settings (AI Section)

**Storage:** `%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`

```json
{
    "AI": {
        "IsEnabled": true,
        "GeminiApiKey": "AIza...",
        "GeminiModel": "gemini-2.5-pro",
        "MaxResponseTokens": 1024,
        "MonthlyBudget": 5.00,
        "BudgetWarningPercent": 80,
        "EnforceBudgetLimit": true
    }
}
```

### Required API Setup

1. **Obtain Gemini API Key:**
   - Go to Google AI Studio (https://aistudio.google.com/)
   - Create or select project
   - Generate API key
   - Copy key to Settings → AI → API Key

2. **Verify Connectivity:**
   - Open Help Bot (F1 or FAB button)
   - Send test message
   - Check for successful response

---

## Performance Characteristics

### Startup Time Impact

| Operation | Typical Time | Notes |
|-----------|--------------|-------|
| VectorStore init | <100ms | SQLite open |
| Documentation indexing | 2-5s | First run only |
| Data indexing (full) | 1-3s | First run only |
| Data indexing (incremental) | <500ms | Subsequent runs |
| System context build | <200ms | Database queries |

### Query Response Time

| Phase | Typical Time |
|-------|--------------|
| Embedding generation | 200-400ms |
| Vector search | 50-100ms |
| Context building | <100ms |
| Gemini API call | 1-3s |
| **Total** | **1.5-4s** |

### Storage Requirements

| Data | Typical Size |
|------|--------------|
| vectors.db | 5-50 MB |
| ai_usage.json | <1 KB |
| Per entity embedding | ~3 KB |

---

## Security Considerations

### API Key Storage
- Stored in user-specific settings file
- Not encrypted at rest (relies on OS-level file permissions)
- Never transmitted except to Gemini API

### Data Privacy
- User data never leaves local machine except for:
  - Embedding generation (text → vector)
  - Query context sent to Gemini
- No data stored on Google servers (API doesn't retain)
- Budget tracking is local only

### Recommendations
- Consider encrypting API key in settings
- Implement option to disable data context in queries
- Add data minimization option for sensitive environments

---

## Error Handling Matrix

| Error Scenario | Detection | User Message | Recovery |
|---------------|-----------|--------------|----------|
| No API key | `IsAvailable` check | "API key not configured..." | Redirect to settings |
| Rate limited | HTTP 429 | "Too many messages..." | Auto-retry hint |
| Budget exceeded | `IsBudgetExceeded` | "Monthly budget reached..." | Increase budget |
| Network error | HttpRequestException | "Trouble connecting..." | Retry button |
| Empty response | Null/empty text | "Couldn't generate response..." | Retry |
| Large request | >30K chars | "Request too large..." | Simplify question |
| Function error | Exception in handler | "Error: {message}" | Log for debugging |

---

## Future Enhancement Opportunities

### Short-term (1-2 Sprints)
1. **Streaming Responses** - Display tokens as they arrive
2. **Conversation Memory** - Persist chat history across sessions
3. **Custom Prompts** - User-defined system instructions
4. **Export Chat** - Save conversations as markdown

### Medium-term (3-6 Months)
1. **Approximate Nearest Neighbor** - Scale to larger datasets
2. **Multi-model Support** - Add Claude, OpenAI options
3. **Voice Input** - Whisper integration for speech-to-text
4. **Scheduled Reports** - AI-generated weekly summaries

### Long-term (6+ Months)
1. **Fine-tuned Model** - Custom model for Tracker domain
2. **Team AI** - Shared context across team members
3. **Proactive Insights** - AI-initiated suggestions
4. **Natural Language Queries** - "Show me Sarah's OKR progress" → auto-generate chart

---

## Appendix A: File Reference

| File | Purpose |
|------|---------|
| `ViewModels/HelpBotViewModel.cs` | Chat interface ViewModel |
| `Views/HelpBotWindow.xaml` | Chat window UI |
| `Controls/HelpBotControl.xaml` | Embedded chat control |
| `Services/GeminiChatService.cs` | Gemini API client |
| `Services/HelpBotContextService.cs` | RAG context builder |
| `Services/SmartContextBuilder.cs` | Query-specific data retrieval |
| `Services/AI/VectorStore.cs` | SQLite vector database |
| `Services/AI/EmbeddingService.cs` | Embedding generation |
| `Services/AI/DocumentIndexer.cs` | Help doc indexing |
| `Services/AI/DataIndexer.cs` | User data index coordinator |
| `Services/AI/TeamMemberIndexer.cs` | Team member indexing |
| `Services/AI/MeetingIndexer.cs` | Meeting indexing |
| `Services/AI/TaskIndexer.cs` | Task indexing |
| `Services/AI/GoalIndexer.cs` | OKR/KPI/Project indexing (enhanced with gap analysis) |
| `Services/AI/PulseSurveyIndexer.cs` | Pulse survey & response indexing |
| `Services/AI/AIFunctionService.cs` | Function execution |
| `Services/AI/AIUsageTracker.cs` | Budget tracking |
| `Interfaces/IChatProvider.cs` | Chat provider interface |

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| RAG | Retrieval Augmented Generation - combining search with LLM |
| Embedding | Vector representation of text for similarity search |
| Chunking | Splitting documents into smaller searchable pieces |
| Vector Store | Database optimized for similarity search |
| Function Calling | LLM capability to invoke predefined functions |
| Cosine Similarity | Measure of angle between vectors (0-1) |
| Top-K | Return K most similar results |
| System Context | Instructions sent with every request |
| KPI | Key Performance Indicator - measurable metric to track progress |
| OKR | Objectives and Key Results - goal-setting framework |
| Key Result | Measurable outcome that belongs to an OKR |

---

**Document End**

*This document is maintained alongside the Tracker codebase. For the latest implementation details, refer to the source files listed in Appendix A.*
