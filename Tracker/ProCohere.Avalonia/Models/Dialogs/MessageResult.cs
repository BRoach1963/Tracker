namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result of the QuickMessageDialog.
/// </summary>
public class MessageResult
{
    public bool Success { get; init; }
    public bool WasSent { get; init; }
    public bool WasCancelled { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RecipientEmail { get; init; }
    public string? MessageText { get; init; }

    /// <summary>
    /// Factory: Message sent successfully.
    /// </summary>
    public static MessageResult Sent(string recipientEmail, string messageText)
    {
        return new MessageResult
        {
            Success = true,
            WasSent = true,
            WasCancelled = false,
            RecipientEmail = recipientEmail,
            MessageText = messageText
        };
    }

    /// <summary>
    /// Factory: User cancelled the dialog.
    /// </summary>
    public static MessageResult Cancelled()
    {
        return new MessageResult
        {
            Success = false,
            WasSent = false,
            WasCancelled = true
        };
    }

    /// <summary>
    /// Factory: Send failed with error.
    /// </summary>
    public static MessageResult Failed(string error)
    {
        return new MessageResult
        {
            Success = false,
            WasSent = false,
            WasCancelled = false,
            ErrorMessage = error
        };
    }
}
