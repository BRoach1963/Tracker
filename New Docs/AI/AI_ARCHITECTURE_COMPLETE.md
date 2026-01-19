# Tracker AI Architecture - Complete Technical Guide

## Overview

Tracker uses a sophisticated AI architecture that combines **Retrieval Augmented Generation (RAG)**, **semantic search via vector embeddings**, and **function calling** to provide intelligent, contextual assistance through the **Oracle** AI assistant.

This document provides a complete technical understanding of how AI is utilized throughout the application.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [AI Assistant (Oracle)](#ai-assistant-oracle)
3. [Embedding & Vector System](#embedding--vector-system)
4. [RAG (Retrieval Augmented Generation)](#rag-retrieval-augmented-generation)
5. [Data Indexing Pipeline](#data-indexing-pipeline)
6. [AI Providers & Chat Services](#ai-providers--chat-services)
7. [Function Calling (Tool Use)](#function-calling-tool-use)
8. [Insight Engine](#insight-engine)
9. [Data Flow Diagrams](#data-flow-diagrams)
10. [Database Schema](#database-schema)
11. [Service Architecture](#service-architecture)
12. [Configuration & Settings](#configuration--settings)
13. [File Reference](#file-reference)

---

## Architecture Overview

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              USER INTERFACE                                │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                    HelpBotWindow / HelpBotControl                   │  │
│  │                         (Oracle Chat UI)                            │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                            VIEWMODEL LAYER                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                        HelpBotViewModel                              │  │
│  │  • Manages chat messages                                            │  │
│  │  • Coordinates RAG pipeline                                         │  │
│  │  • Handles function call results                                    │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            │                         │                         │
            ▼                         ▼                         ▼
┌───────────────────┐    ┌───────────────────┐    ┌───────────────────────┐
│ HelpBotContext    │    │  SmartContext     │    │  AI Chat Provider     │
│ Service           │    │  Builder          │    │  (Gemini/OpenAI)      │
│ • System prompt   │    │ • Query-based     │    │ • LLM API calls       │
│ • Doc retrieval   │    │   data search     │    │ • Function calling    │
│ • User data       │    │ • Semantic search │    │ • Response generation │
└───────────────────┘    └───────────────────┘    └───────────────────────┘
            │                         │                         │
            ▼                         ▼                         │
┌───────────────────────────────────────────────┐              │
│              VECTOR STORE LAYER               │              │
│  ┌─────────────────────────────────────────┐  │              │
│  │  SQLite VectorStore (Local/Offline)     │  │              │
│  │  PostgreSQL VectorStore (Multi-tenant)  │  │              │
│  └─────────────────────────────────────────┘  │              │
└───────────────────────────────────────────────┘              │
                      │                                        │
                      ▼                                        ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                         DATA INDEXERS                                      │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐         │
│  │ TeamMember  │ │  Meeting    │ │   Task      │ │   Goal      │         │
│  │  Indexer    │ │  Indexer    │ │  Indexer    │ │  Indexer    │ ...     │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘         │
└───────────────────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                      EMBEDDING SERVICE                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │              Google Gemini text-embedding-004                        │  │
│  │                    (768 dimensions)                                  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

### Key Principles

1. **RAG (Retrieval Augmented Generation)**: AI responses are grounded in user's actual data and documentation
2. **Semantic Search**: Vector embeddings enable finding conceptually similar content, not just keyword matches
3. **Multi-tenant Ready**: PostgreSQL vector store supports organization-scoped data with RLS
4. **Offline Fallback**: SQLite vector store works without internet for cached data
5. **Function Calling**: AI can execute actions (create meetings, tasks, etc.) on behalf of the user

---

## AI Assistant (Oracle)

### Identity

The AI assistant is named **Oracle**. This name appears in:
- Welcome messages: "Hi! I'm Oracle, your AI assistant."
- System prompts identifying the assistant's role

### Core Capabilities

Oracle can:
1. **Answer questions** about team data, meetings, tasks, OKRs, KPIs, projects
2. **Search semantically** across all indexed data
3. **Create entities** via function calling (meetings, tasks, OKRs, KPIs, feedback, etc.)
4. **Provide insights** about team patterns, upcoming events, at-risk items
5. **Generate predictions** about OKR/KPI trajectories

### Implementation Files

| File | Purpose |
|------|---------|
| `ViewModels/HelpBotViewModel.cs` | Main ViewModel for chat interface |
| `Views/HelpBotWindow.xaml` | Standalone chat window |
| `Controls/HelpBotControl.xaml` | Embeddable chat control |
| `Services/HelpBotContextService.cs` | Builds system context and RAG |

### System Prompt Structure

The system prompt sent with every Oracle request includes:

```
You are Oracle, the AI assistant for Tracker. Tracker is a team management app with these features:
- Team Members: profiles of direct reports
- 1:1 Meetings: scheduled meetings with agenda items and notes
- Tasks: work items with due dates and priorities
- Projects: multi-task initiatives  
- OKRs: Objectives & Key Results for goal tracking
- KPIs: Key Performance Indicators for metrics
- Goals: individual development goals
- Feedback: performance feedback records

[Current date/time context for relative date parsing]

[User's actual data summary - team members, upcoming meetings, active tasks, etc.]

[Relevant documentation chunks from RAG search]
```

---

## Embedding & Vector System

### What Are Embeddings?

Embeddings are high-dimensional vectors (arrays of floats) that represent the semantic meaning of text. Similar concepts have similar vectors, enabling "find things like this" searches.

### Embedding Model

**Google Gemini text-embedding-004**
- **Dimensions**: 768
- **Provider**: Google Generative AI API
- **Cost**: Free tier available

### EmbeddingService

**Location**: `Services/AI/EmbeddingService.cs`

```csharp
public class EmbeddingService : IDisposable
{
    private const string EmbeddingModel = "text-embedding-004";
    
    /// <summary>
    /// Gets the embedding vector for a single text.
    /// </summary>
    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets embeddings for multiple texts in a batch (more efficient).
    /// </summary>
    public async Task<List<float[]?>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculates cosine similarity between two embedding vectors.
    /// Returns value between -1 and 1, where 1 means identical.
    /// </summary>
    public static float CosineSimilarity(float[] a, float[] b);
}
```

### Vector Stores

Tracker supports multiple vector storage backends:

#### 1. SQLite VectorStore (Local/Offline)

**Location**: `Services/AI/VectorStore.cs`

- **Storage**: `%LocalAppData%\Tracker\vectors.db`
- **Use case**: Single-user, offline-capable
- **Search**: In-memory cosine similarity calculation

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
```

#### 2. PostgreSQL VectorStore (Multi-tenant)

**Location**: `Services/AI/PostgresVectorStore.cs`

- **Storage**: Supabase PostgreSQL with pgvector extension
- **Use case**: Multi-tenant, cloud-synced
- **Search**: Native pgvector HNSW index for fast ANN search

Features:
- Row Level Security (RLS) for organization isolation
- HNSW indexing for approximate nearest neighbor search
- Native vector operations via pgvector

### IVectorStore Interface

**Location**: `Services/AI/IVectorStore.cs`

```csharp
public interface IVectorStore : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    bool IsInitialized { get; }
    
    // Storage
    Task<Guid> StoreAsync(string entityType, string entityId, string content, 
                          float[] embedding, int chunkIndex = 0, 
                          Dictionary<string, object>? metadata = null);
    Task StoreBatchAsync(IEnumerable<VectorStoreEntry> entries);
    
    // Search
    Task<List<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 10,
                                                string[]? entityTypes = null, 
                                                float minSimilarity = 0.5f);
    
    // Deletion
    Task DeleteEntityAsync(string entityType, string entityId);
}
```

---

## RAG (Retrieval Augmented Generation)

### What is RAG?

RAG improves AI responses by:
1. **Retrieving** relevant context based on the user's question
2. **Augmenting** the prompt with this context
3. **Generating** a response grounded in actual data

### RAG Pipeline in Tracker

```
┌──────────────────┐
│   User Question  │
│ "When is Sarah's │
│   birthday?"     │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ EmbeddingService │  Generate embedding for question
│ GetEmbeddingAsync│  [0.12, -0.34, 0.56, ...]
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   VectorStore    │  Search for similar embeddings
│   SearchAsync    │  (cosine similarity > 0.5)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Relevant Chunks  │  "Sarah Martinez\nBirthday: March 15..."
│ from indexed     │  "Team member profile: Sarah..."
│ data & docs      │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────────────────────┐
│                  Combined Prompt                      │
│                                                       │
│  System: You are Oracle... [User's data summary]     │
│                                                       │
│  Relevant information:                               │
│  - Sarah Martinez, Birthday: March 15                │
│  - Team member profile: Sarah...                     │
│                                                       │
│  User: When is Sarah's birthday?                     │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────┐
│  Gemini API      │  Generate response using context
│  generateContent │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ "Sarah's birthday│
│  is March 15th." │
└──────────────────┘
```

### HelpBotContextService

**Location**: `Services/HelpBotContextService.cs`

Key methods:

```csharp
public class HelpBotContextService
{
    /// <summary>
    /// Ensures the documentation is indexed for semantic search.
    /// Call once at startup.
    /// </summary>
    public async Task InitializeAsync();
    
    /// <summary>
    /// Builds the system context (instructions + user data).
    /// Sent with every request as the system instruction.
    /// </summary>
    public async Task<string> BuildSystemContextAsync();
    
    /// <summary>
    /// Gets relevant documentation AND data for a user question using semantic search.
    /// </summary>
    public async Task<string> GetRelevantDocsAsync(string question, int topK = 5);
}
```

### SmartContextBuilder

**Location**: `Services/SmartContextBuilder.cs`

Searches vectorized user data (not documentation) based on query intent:

```csharp
public class SmartContextBuilder
{
    /// <summary>
    /// Searches vectorized data for relevant context based on the question
    /// </summary>
    public async Task<string> GetDataContextForQueryAsync(string question, int topK = 5);
}
```

---

## Data Indexing Pipeline

### Overview

Data indexing converts entity records (team members, meetings, tasks, etc.) into vector embeddings for semantic search.

### Indexing Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        DataIndexer.IndexAllDataAsync()              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
        ▼                           ▼                           ▼
┌───────────────┐         ┌───────────────┐         ┌───────────────┐
│ TeamMember    │         │   Meeting     │         │    Task       │
│   Indexer     │         │   Indexer     │         │   Indexer     │
└───────┬───────┘         └───────┬───────┘         └───────┬───────┘
        │                         │                         │
        ▼                         ▼                         ▼
┌───────────────────────────────────────────────────────────────────┐
│                     For each entity:                               │
│  1. Fetch entity from database                                    │
│  2. Build rich text representation                                │
│  3. Generate embedding via EmbeddingService                       │
│  4. Store in VectorStore with metadata                            │
└───────────────────────────────────────────────────────────────────┘
```

### Entity Indexers

All indexers extend `EntityIndexerBase`:

| Indexer | Entity Type | Content Indexed |
|---------|-------------|-----------------|
| `TeamMemberIndexer` | `team_member` | Name, title, hire date, birthday, email, phone |
| `MeetingIndexer` | `meeting` | Title, date, attendee, agenda items, notes |
| `TaskIndexer` | `task` | Title, description, owner, due date, status |
| `GoalIndexer` | `goal` | OKR title, key results, progress, owner |
| `PulseSurveyIndexer` | `pulse_survey` | Survey responses, sentiment |

### EntityIndexerBase

**Location**: `Services/AI/EntityIndexerBase.cs`

```csharp
public abstract class EntityIndexerBase
{
    /// <summary>
    /// Template method for indexing all entities of a type.
    /// Handles reset, logging, filtering, and exception handling.
    /// </summary>
    public async Task<int> IndexAllAsync(DateTime? sinceTime = null);
    
    /// <summary>
    /// Sets a custom IVectorStore to use instead of the legacy singleton.
    /// </summary>
    public void SetVectorStore(IVectorStore vectorStore);
    
    // Abstract methods implemented by subclasses
    protected abstract Task<IEnumerable<object>> FetchEntitiesAsync();
    protected abstract Task IndexSingleEntityAsync(object entity);
}
```

### Example: TeamMemberIndexer

```csharp
protected override async Task IndexSingleEntityAsync(object entity)
{
    var member = (TeamMember)entity;
    
    // Build rich text representation
    var sb = new StringBuilder();
    sb.AppendLine($"{member.FullName}");
    sb.AppendLine($"Job Title: {member.JobTitle}");
    sb.AppendLine($"Hire Date: {member.HireDate:MMMM d, yyyy}");
    sb.AppendLine($"Birthday: {member.Birthday:MMMM d}");
    sb.AppendLine($"Email: {member.Email}");
    sb.AppendLine($"Status: {(member.IsActive ? "Active" : "Inactive")}");

    var content = sb.ToString();

    // Metadata for filtering
    var metadata = new Dictionary<string, object>
    {
        ["type"] = "team_member",
        ["id"] = member.Id,
        ["name"] = member.FullName,
        ["is_active"] = member.IsActive
    };

    await IndexEntityAsync($"team_member_{member.Id}", content, metadata);
}
```

### DocumentIndexer

**Location**: `Services/AI/DocumentIndexer.cs`

Indexes help documentation (Markdown files) for RAG:

1. **Chunking**: Splits documents into ~500 char chunks with 50 char overlap
2. **Embedding**: Generates embeddings for each chunk
3. **Storage**: Stores in VectorStore with document ID

```csharp
public class DocumentIndexer
{
    private const int MaxChunkSize = 500;
    private const int ChunkOverlap = 50;
    
    public async Task<bool> EnsureIndexedAsync(CancellationToken cancellationToken = default);
    public async Task<bool> ReindexAllAsync(CancellationToken cancellationToken = default);
}
```

### Incremental Indexing

The system supports incremental indexing to avoid re-indexing unchanged data:

```csharp
// Index only entities modified since last run
var stats = await DataIndexer.Instance.IndexAllDataAsync();
// stats.TotalIndexed = number of entities indexed
// stats.Duration = time taken
```

---

## AI Providers & Chat Services

### Supported Providers

| Provider | Model | Notes |
|----------|-------|-------|
| **Google Gemini** | gemini-2.5-pro | Default, free tier available |
| OpenAI | GPT-4 | Uses API credits |
| Anthropic | Claude | Uses API credits |

### ChatProviderFactory

**Location**: `Services/ChatProviderFactory.cs`

```csharp
public enum AIProviderType
{
    Gemini,    // Google Gemini (default)
    OpenAI,    // OpenAI GPT models
    Anthropic  // Anthropic Claude models
}

public class ChatProviderFactory
{
    public AIProviderType SelectedProvider { get; set; }
    public async Task<IChatProvider> GetProviderAsync();
}
```

### IChatProvider Interface

**Location**: `Interfaces/IChatProvider.cs`

```csharp
public interface IChatProvider
{
    string ProviderName { get; }
    bool RequiresInternet { get; }
    bool IsAvailable { get; }
    
    Task<string> GetResponseAsync(string prompt, string? systemContext = null, 
                                   CancellationToken cancellationToken = default);
    Task<string> GetResponseAsync(IEnumerable<ChatMessage> messages, string? systemContext = null,
                                   CancellationToken cancellationToken = default);
}
```

### GeminiChatService

**Location**: `Services/GeminiChatService.cs`

The primary chat provider implementation:

```csharp
public class GeminiChatService : IChatProvider
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string DefaultModel = "gemini-2.5-pro";
    
    // Supports function calling via tools/function_declarations
    // Handles streaming responses
    // Tracks usage for budget monitoring
}
```

### API Request Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    GeminiChatService.GetResponseAsync()             │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│  1. Check budget limits (AIUsageTracker)                            │
│  2. Truncate system context if > 4000 chars                         │
│  3. Build request with messages + system instruction + tools        │
│  4. POST to Gemini API                                              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────┴───────────────┐
                    │                               │
           Text Response                   Function Call
                    │                               │
                    ▼                               ▼
            Return text to           ┌─────────────────────────────┐
            HelpBotViewModel         │ AIFunctionService.Execute   │
                                     │ Return result, call again   │
                                     └─────────────────────────────┘
```

---

## Function Calling (Tool Use)

### Overview

Oracle can execute actions through **function calling** - the AI decides when to call a function based on user intent, and Tracker executes it.

### Available Functions

| Function | Description | Parameters |
|----------|-------------|------------|
| `create_meeting` | Schedule a 1:1 meeting | team_member_name, date, notes |
| `create_task` | Create a task | description, owner_name, due_date |
| `create_kpi` | Create a KPI | name, target_value, unit, current_value |
| `create_okr` | Create an OKR | title, key_results, owner_name |
| `create_feedback` | Record feedback | team_member_name, type, content |
| `create_project` | Create a project | name, description |
| `create_goal` | Create a goal | title, description, target_date |
| `create_note` | Create a note | title, content, linked_to |
| `search_team_members` | Search team | query |
| `get_upcoming_meetings` | List meetings | days_ahead |
| `get_projects` | List projects | status_filter |
| `get_notes` | Search notes | query |
| `get_insights` | Get AI insights | count |
| `dismiss_insight` | Dismiss insight | insight_id |

### AIFunctionService

**Location**: `Services/AI/AIFunctionService.cs`

```csharp
public class AIFunctionService
{
    /// <summary>
    /// Executes a function call from the AI.
    /// </summary>
    public async Task<string> ExecuteFunctionAsync(string functionName, JsonElement arguments)
    {
        return functionName switch
        {
            "create_meeting" => await CreateMeetingAsync(arguments),
            "create_task" => await CreateTaskAsync(arguments),
            "create_kpi" => await CreateKPIAsync(arguments),
            // ... etc
            _ => $"Unknown function: {functionName}"
        };
    }
}
```

### Function Call Flow

```
User: "Schedule a 1:1 with Sarah for next Tuesday at 2pm"
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Gemini API analyzes intent                        │
│                    Returns: FunctionCall                             │
│                    {                                                 │
│                      "name": "create_meeting",                      │
│                      "args": {                                      │
│                        "team_member_name": "Sarah",                 │
│                        "date": "2026-01-21 14:00",                  │
│                        "notes": "1:1 Meeting"                       │
│                      }                                              │
│                    }                                                 │
└─────────────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    AIFunctionService.ExecuteFunctionAsync()         │
│                    - Finds Sarah in database                        │
│                    - Parses date                                    │
│                    - Creates Meeting entity                         │
│                    - Returns: "✓ Created 1:1 with Sarah Martinez    │
│                               on Tuesday, Jan 21 at 2:00 PM"        │
└─────────────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Second Gemini API call                           │
│                    - Includes function result                       │
│                    - Generates natural language confirmation        │
│                    Returns: "Done! I've scheduled your 1:1 with     │
│                             Sarah for next Tuesday at 2pm."         │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Insight Engine

### Overview

The Insight Engine generates proactive insights by analyzing user data through rule-based analyzers and AI enhancement.

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         InsightEngine                                │
└─────────────────────────────────────────────────────────────────────┘
                    │
        ┌───────────┼───────────┬───────────┬───────────┐
        │           │           │           │           │
        ▼           ▼           ▼           ▼           ▼
┌─────────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
│ Meeting     │ │Personal │ │Action   │ │Goal     │ │Survey   │
│ Cadence     │ │Date     │ │Item     │ │Trajectory│ │Sentiment│
│ Analyzer    │ │Analyzer │ │Staleness│ │Analyzer │ │Analyzer │
└─────────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
```

### Insight Types

| Analyzer | What it detects |
|----------|-----------------|
| `MeetingCadenceAnalyzer` | Team members overdue for 1:1s |
| `PersonalDateAnalyzer` | Upcoming birthdays, work anniversaries |
| `ActionItemStalenessAnalyzer` | Stale tasks, overdue items |
| `GoalTrajectoryAnalyzer` | OKRs at risk of missing targets |
| `MetricGapAnalyzer` | KPIs below target |
| `SurveySentimentAnalyzer` | Declining pulse survey sentiment |

### IInsightAnalyzer Interface

**Location**: `Services/AI/Insights/IInsightAnalyzer.cs`

```csharp
public interface IInsightAnalyzer
{
    string Name { get; }
    bool IsEnabled { get; set; }
    Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default);
}
```

### AI-Enhanced Insights

**Location**: `Services/AI/Insights/AIInsightGenerator.cs`

Uses AI to generate richer insights from data patterns:

```csharp
public class AIInsightGenerator
{
    /// <summary>
    /// Generates AI-powered insights from team data.
    /// </summary>
    public async Task<List<Insight>> GenerateInsightsAsync(TeamDataContext dataContext);
    
    /// <summary>
    /// Generates an AI-powered daily briefing summary.
    /// </summary>
    public async Task<string> GenerateBriefingSummaryAsync(DailyBriefing briefing);
    
    /// <summary>
    /// Generates AI-powered meeting prep notes.
    /// </summary>
    public async Task<string> GenerateMeetingPrepAsync(TeamMember member, ...);
}
```

---

## Data Flow Diagrams

### Complete AI Request Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           User types question                                │
│                    "What tasks does Sarah have this week?"                   │
└──────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                         HelpBotViewModel.SendToAIAsync()                      │
│                                                                               │
│  1. Wait for context initialization if in progress                           │
│  2. Get system context (cached or build new)                                 │
│  3. RAG: Search for relevant docs                                            │
│  4. RAG: Search for relevant data                                            │
└──────────────────────────────────────────────────────────────────────────────┘
                                        │
                   ┌────────────────────┼────────────────────┐
                   │                    │                    │
                   ▼                    ▼                    ▼
        ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
        │ HelpBotContext   │  │  SmartContext    │  │   System         │
        │ .GetRelevantDocs │  │  .GetDataContext │  │   Context        │
        │                  │  │                  │  │   (cached)       │
        │ Embed question   │  │ Embed question   │  │                  │
        │ Search vectors   │  │ Search data vecs │  │ Core instructions│
        │ Return docs      │  │ Return data      │  │ User data summary│
        └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
                 │                     │                     │
                 └─────────────────────┼─────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                            Build Combined Prompt                              │
│                                                                               │
│  System Instruction:                                                          │
│    [Core instructions]                                                        │
│    [User data summary]                                                        │
│                                                                               │
│  User Message:                                                                │
│    [Relevant docs: ...]                                                       │
│    [Relevant data: Sarah Martinez has tasks...]                              │
│    === USER QUESTION ===                                                      │
│    What tasks does Sarah have this week?                                     │
└──────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                       GeminiChatService.GetResponseAsync()                    │
│                                                                               │
│  POST https://generativelanguage.googleapis.com/v1beta/models/               │
│       gemini-2.5-pro:generateContent                                          │
│                                                                               │
│  Body: { contents: [...], systemInstruction: {...}, tools: [...] }           │
└──────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                              Gemini API Response                              │
│                                                                               │
│  "Based on your data, Sarah Martinez has the following tasks this week:      │
│   1. Update Q1 roadmap (Due: Wednesday)                                      │
│   2. Review performance metrics (Due: Friday)                                │
│   3. Submit budget proposal (Due: Thursday)"                                 │
└──────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                    Display response in chat UI                               │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### vector_embeddings Table (PostgreSQL)

```sql
CREATE TABLE vector_embeddings (
    -- Identity
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id       UUID NOT NULL REFERENCES organizations(id),
    
    -- Entity Reference
    entity_type           VARCHAR(50) NOT NULL,  -- "team_member", "meeting", etc.
    entity_id             UUID NOT NULL,
    chunk_index           INTEGER NOT NULL DEFAULT 0,
    
    -- Content
    content_hash          VARCHAR(64) NOT NULL,
    content_preview       VARCHAR(500),
    content               TEXT,
    
    -- Embedding
    embedding             VECTOR(768),  -- pgvector type
    embedding_dimensions  INTEGER NOT NULL DEFAULT 768,
    
    -- Model Info
    model_name            VARCHAR(100) NOT NULL DEFAULT 'text-embedding-004',
    model_version         VARCHAR(50),
    
    -- Metadata
    metadata              JSONB,
    
    -- Audit
    created_at            TIMESTAMPTZ DEFAULT NOW(),
    updated_at            TIMESTAMPTZ DEFAULT NOW(),
    is_deleted            BOOLEAN DEFAULT false,
    deleted_at            TIMESTAMPTZ,
    deleted_by            UUID,
    
    -- Constraints
    UNIQUE(organization_id, entity_type, entity_id, chunk_index)
);

-- HNSW index for fast similarity search
CREATE INDEX idx_vector_embeddings_embedding 
ON vector_embeddings USING hnsw (embedding vector_cosine_ops);

-- Partial indexes for entity type filtering
CREATE INDEX idx_vector_embeddings_entity_type 
ON vector_embeddings(organization_id, entity_type) 
WHERE is_deleted = false;
```

### VectorEmbedding C# Model

**Location**: `Tracker.Core/DataModels/VectorEmbedding.cs`

```csharp
[Table("vector_embeddings")]
public class VectorEmbedding : AuditableEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("entity_type")]
    public string EntityType { get; set; }

    [Column("entity_id")]
    public Guid EntityId { get; set; }

    [Column("chunk_index")]
    public int ChunkIndex { get; set; }

    [Column("embedding")]
    public float[]? EmbeddingVector { get; set; }

    [Column("embedding_dimensions")]
    public int EmbeddingDimensions { get; set; } = 768;

    [Column("model_name")]
    public string ModelName { get; set; } = "text-embedding-004";
    
    // ... additional properties
}
```

---

## Service Architecture

### Singleton Services

| Service | Responsibility |
|---------|----------------|
| `EmbeddingService.Instance` | Generate embeddings via Gemini API |
| `VectorStore.Instance` | Local SQLite vector storage |
| `DataIndexer.Instance` | Coordinate entity indexing |
| `DocumentIndexer.Instance` | Index help documentation |
| `HelpBotContextService.Instance` | Build RAG context |
| `SmartContextBuilder.Instance` | Query-based data search |
| `AIFunctionService.Instance` | Execute function calls |
| `InsightEngine.Instance` | Generate insights |
| `AIInsightGenerator.Instance` | AI-enhanced insights |
| `ChatProviderFactory.Instance` | Create chat providers |
| `AIUsageTracker.Instance` | Track API usage/budget |

### Dependency Graph

```
HelpBotViewModel
    ├── HelpBotContextService
    │       ├── DocumentIndexer
    │       │       ├── EmbeddingService
    │       │       └── VectorStore
    │       ├── VectorStore
    │       └── DataIndexer
    │               ├── TeamMemberIndexer
    │               ├── MeetingIndexer
    │               ├── TaskIndexer
    │               ├── GoalIndexer
    │               └── PulseSurveyIndexer
    │                       └── EmbeddingService
    ├── SmartContextBuilder
    │       ├── EmbeddingService
    │       └── VectorStore
    ├── GeminiChatService
    │       ├── AIUsageTracker
    │       └── AIFunctionService
    └── InsightEngine
            └── AIInsightGenerator
```

---

## Configuration & Settings

### User Settings (AI Section)

**Location**: `%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`

```json
{
  "AI": {
    "GeminiApiKey": "AIza...",
    "GeminiModel": "gemini-2.5-pro",
    "OpenAIApiKey": "",
    "AnthropicApiKey": "",
    "MaxResponseTokens": 1024,
    "EnableInsights": true,
    "BudgetLimitPerMonth": 10.00,
    "BudgetWarningThreshold": 0.8
  }
}
```

### AIUsageTracker

**Location**: `Services/AI/AIUsageTracker.cs`

Tracks API usage and enforces budget limits:

```csharp
public class AIUsageTracker
{
    public decimal BudgetLimitPerMonth { get; set; }
    public decimal BudgetWarningThreshold { get; set; }
    public decimal BudgetUsedPercent { get; }
    public bool IsWarningThresholdReached { get; }
    
    public (bool canProceed, string message) CheckCanMakeRequest();
    public void RecordRequest(int promptTokens, int completionTokens);
    public string GetUsageSummary();
}
```

---

## File Reference

### Core AI Services

| File | Description |
|------|-------------|
| `Services/AI/EmbeddingService.cs` | Generate vector embeddings |
| `Services/AI/VectorStore.cs` | Local SQLite vector storage |
| `Services/AI/PostgresVectorStore.cs` | Multi-tenant PostgreSQL storage |
| `Services/AI/IVectorStore.cs` | Vector store interface |
| `Services/AI/VectorStoreFactory.cs` | Create vector stores |
| `Services/AI/DataIndexer.cs` | Coordinate data indexing |
| `Services/AI/DocumentIndexer.cs` | Index documentation |
| `Services/AI/EntityIndexerBase.cs` | Base class for entity indexers |
| `Services/AI/TeamMemberIndexer.cs` | Index team members |
| `Services/AI/MeetingIndexer.cs` | Index meetings |
| `Services/AI/TaskIndexer.cs` | Index tasks |
| `Services/AI/GoalIndexer.cs` | Index OKRs/goals |
| `Services/AI/PulseSurveyIndexer.cs` | Index pulse surveys |
| `Services/AI/AIFunctionService.cs` | Execute function calls |
| `Services/AI/AIUsageTracker.cs` | Track API usage |

### Chat Providers

| File | Description |
|------|-------------|
| `Services/GeminiChatService.cs` | Google Gemini provider |
| `Services/ChatProviderFactory.cs` | Provider factory |
| `Interfaces/IChatProvider.cs` | Provider interface |

### Context Building

| File | Description |
|------|-------------|
| `Services/HelpBotContextService.cs` | RAG context service |
| `Services/SmartContextBuilder.cs` | Query-based data search |

### Insight Engine

| File | Description |
|------|-------------|
| `Services/AI/Insights/InsightEngine.cs` | Insight coordinator |
| `Services/AI/Insights/AIInsightGenerator.cs` | AI-enhanced insights |
| `Services/AI/Insights/IInsightAnalyzer.cs` | Analyzer interface |
| `Services/AI/Insights/Analyzers/*.cs` | Individual analyzers |

### UI Components

| File | Description |
|------|-------------|
| `ViewModels/HelpBotViewModel.cs` | Chat ViewModel |
| `Views/HelpBotWindow.xaml` | Chat window |
| `Controls/HelpBotControl.xaml` | Embeddable chat |

### Data Models

| File | Description |
|------|-------------|
| `Tracker.Core/DataModels/VectorEmbedding.cs` | Embedding entity |
| `Tracker.Core/DataModels/Insight.cs` | Insight entity |

---

## Summary

Tracker's AI architecture is built on three pillars:

1. **RAG (Retrieval Augmented Generation)**: Every Oracle response is grounded in the user's actual data and documentation through semantic search

2. **Vector Embeddings**: Text is converted to vectors using Gemini's text-embedding-004 model, enabling conceptual similarity search across all indexed entities

3. **Function Calling**: Oracle can take actions (create meetings, tasks, etc.) through a structured function calling interface

This architecture enables Oracle to be:
- **Contextual**: Answers are based on real user data
- **Accurate**: RAG reduces hallucinations
- **Actionable**: Can execute tasks on behalf of users
- **Scalable**: Multi-tenant ready with PostgreSQL/pgvector

---

*Document created: January 19, 2026*
*Last updated: January 19, 2026*
