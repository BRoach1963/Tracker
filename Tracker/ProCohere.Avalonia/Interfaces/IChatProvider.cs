using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces;

/// <summary>
/// Interface for AI chat providers (Gemini).
/// Enables clean architecture with provider abstraction.
/// </summary>
public interface IChatProvider
{
    /// <summary>
    /// Display name of the provider (e.g., "Google Gemini").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider requires an internet connection.
    /// </summary>
    bool RequiresInternet { get; }

    /// <summary>
    /// Whether the provider is properly configured and available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets a response for a single prompt.
    /// </summary>
    /// <param name="prompt">The user's message</param>
    /// <param name="systemContext">Optional system context to guide the AI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The AI's response text</returns>
    Task<string> GetResponseAsync(string prompt, string? systemContext = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a response for a conversation with history.
    /// </summary>
    /// <param name="messages">The conversation history</param>
    /// <param name="systemContext">Optional system context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The AI's response text</returns>
    Task<string> GetResponseAsync(IEnumerable<ChatMessage> messages, string? systemContext = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a chat message in a conversation.
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string? Content { get; set; }

    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
}

/// <summary>
/// Supported AI providers (currently Gemini-only for cost efficiency).
/// </summary>
public enum AIProviderType
{
    /// <summary>Google Gemini (free tier available)</summary>
    Gemini
}