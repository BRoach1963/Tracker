using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a vector embedding for semantic search.
/// Maps to Supabase vector_embeddings table.
/// </summary>
public class VectorEmbedding
{
    /// <summary>
    /// Unique identifier for this embedding.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this embedding belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Type of source entity (note, meeting_note, feedback, task, goal, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the source entity.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// SHA256 hash of content to detect changes.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// First 500 chars of content for display.
    /// </summary>
    [MaxLength(500)]
    public string? ContentPreview { get; set; }

    /// <summary>
    /// Embedding vector (stored as JSON array of floats).
    /// Note: In PostgreSQL this uses the vector type; in C# we store as JSON.
    /// </summary>
    public string? Embedding { get; set; }

    /// <summary>
    /// Metadata for filtering (stored as JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Name of the embedding model used.
    /// </summary>
    [MaxLength(100)]
    public string ModelName { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Version of the embedding model.
    /// </summary>
    [MaxLength(50)]
    public string? ModelVersion { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }
}

/// <summary>
/// Represents an AI chat conversation.
/// Maps to Supabase ai_conversations table.
/// </summary>
public class AiConversation
{
    /// <summary>
    /// Unique identifier for this conversation.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this conversation belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who started the conversation.
    /// </summary>
    [Required]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Conversation title.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Type of context entity when conversation started.
    /// </summary>
    [MaxLength(50)]
    public string? ContextEntityType { get; set; }

    /// <summary>
    /// ID of context entity when conversation started.
    /// </summary>
    public Guid? ContextEntityId { get; set; }

    /// <summary>
    /// Whether this conversation is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this conversation is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this conversation was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(TeamMemberId))]
    public virtual TeamMember? TeamMember { get; set; }

    public virtual ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

/// <summary>
/// Represents a message in an AI conversation.
/// Maps to Supabase ai_messages table.
/// </summary>
public class AiMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Conversation this message belongs to.
    /// </summary>
    [Required]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Role of the message sender (user, assistant, system).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Message content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Token count for this message.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Model used for generating the response.
    /// </summary>
    [MaxLength(100)]
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Latency in milliseconds.
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// Context references used (stored as JSON).
    /// </summary>
    public string? ContextReferences { get; set; }

    /// <summary>
    /// When this message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ConversationId))]
    public virtual AiConversation? Conversation { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this is a user message.
    /// </summary>
    [NotMapped]
    public bool IsUserMessage => Role == "user";

    /// <summary>
    /// Whether this is an assistant message.
    /// </summary>
    [NotMapped]
    public bool IsAssistantMessage => Role == "assistant";

    /// <summary>
    /// A truncated preview of the content.
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100 ? Content[..97] + "..." : Content;
}
