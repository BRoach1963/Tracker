# 06g – AI Domain Tables

This document covers the AI-related tables in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 4

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | ai_conversations | ❌ No model |
| 2 | ai_messages | ❌ No model |
| 3 | ai_insights | ❌ No model |
| 4 | vector_embeddings | ✅ MeetingLinks.cs |

**Note:** AI features are planned but not yet implemented in ProCohere.Avalonia.

---

## procohere.ai_conversations

**Purpose**  
Tracks AI chat conversations per user, optionally tied to a context entity.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| team_member_id | uuid | NO | FK → team_members |
| title | text | YES | Conversation title |
| context_type | text | YES | 'meeting', 'goal', 'task', etc. |
| context_id | uuid | YES | FK to context entity |
| model_used | text | YES | AI model identifier |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**RLS:** Organization isolation + user ownership.

---

## procohere.ai_messages

**Purpose**  
Individual messages within AI conversations.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| conversation_id | uuid | NO | FK → ai_conversations |
| role | text | NO | 'user', 'assistant', 'system' |
| content | text | NO | Message content |
| tokens_used | integer | YES | Token count for billing |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**RLS:** Via conversation ownership.

---

## procohere.ai_insights

**Purpose**  
AI-generated insights surfaced to users (coaching tips, meeting prep suggestions, etc.).

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| team_member_id | uuid | YES | FK → team_members (about whom) |
| generated_for | uuid | NO | FK → team_members (recipient) |
| insight_type | text | NO | Type of insight |
| title | text | NO | |
| content | text | NO | |
| source_type | text | YES | Source entity type |
| source_id | uuid | YES | Source entity ID |
| relevance_score | numeric | YES | 0-1 relevance score |
| is_dismissed | boolean | NO | User dismissed this insight |
| dismissed_at | timestamptz | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** ❌ None - NOT USED YET

**RLS:** Organization isolation + recipient ownership.

---

## procohere.vector_embeddings

**Purpose**  
Stores vector embeddings for semantic search and AI features across various entity types.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| entity_type | text | NO | Type of entity embedded |
| entity_id | uuid | NO | FK to source entity |
| content_hash | text | NO | Hash of embedded content |
| embedding | vector(1536) | YES | OpenAI embedding vector |
| metadata | jsonb | YES | Additional context |
| model_version | text | YES | Model used for embedding |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |
| team_member_id | uuid | YES | FK → team_members |
| chunk_index | integer | YES | Chunk position for long content |
| chunk_text | text | YES | The actual text chunk |
| total_chunks | integer | YES | Total chunks for entity |

**Model:** ✅ `VectorEmbedding` in MeetingLinks.cs (17 columns)
