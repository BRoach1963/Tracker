namespace Tracker.Interfaces
{
    /// <summary>
    /// Interface for AI chat providers (Gemini, Groq, etc.)
    /// Allows swapping providers without changing consuming code.
    /// </summary>
    public interface IChatProvider
    {
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

        /// <summary>
        /// The name of this provider (e.g., "Gemini", "Groq")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Whether this provider requires an internet connection.
        /// </summary>
        bool RequiresInternet { get; }

        /// <summary>
        /// Whether this provider is currently available (has valid API key, etc.)
        /// </summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// Represents a message in a chat conversation.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// The role of the message sender ("user" or "assistant")
        /// </summary>
        public string Role { get; set; } = "user";

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of when the message was sent.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ChatMessage() { }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }

        public static ChatMessage User(string content) => new("user", content);
        public static ChatMessage Assistant(string content) => new("assistant", content);
    }
}

