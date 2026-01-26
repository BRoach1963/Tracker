using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the RecordOutcomeDialog.
/// </summary>
public class RecordOutcomeResult
{
    public required Guid AgendaItemId { get; init; }
    public required string OutcomeType { get; init; }
    public required string Content { get; init; }
    public required string Visibility { get; init; }
}
