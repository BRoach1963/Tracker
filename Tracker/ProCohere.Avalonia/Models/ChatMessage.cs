using System;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a single message in an AI chat conversation.
/// Immutable value object for MVVM binding.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Message content/text.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Role: "user", "assistant", or "system"
    /// </summary>
    public string Role { get; init; } = "user";

    /// <summary>
    /// Timestamp when message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// True if this is a user message.
    /// </summary>
    public bool IsUser => Role == "user";

    /// <summary>
    /// True if this is an assistant message.
    /// </summary>
    public bool IsAssistant => Role == "assistant";

    /// <summary>
    /// True if this is a system message.
    /// </summary>
    public bool IsSystem => Role == "system";

    /// <summary>
    /// Optional: Function call information if the message triggered a function.
    /// </summary>
    public string? FunctionName { get; init; }

    /// <summary>
    /// Optional: Function call result if this message is a function response.
    /// </summary>
    public string? FunctionResult { get; init; }

    /// <summary>
    /// True if this message is still being streamed/generated.
    /// </summary>
    public bool IsStreaming { get; init; }

    /// <summary>
    /// Error message if the message failed to send/receive.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// True if this message has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}
