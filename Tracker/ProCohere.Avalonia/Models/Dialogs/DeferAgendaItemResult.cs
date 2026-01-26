using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the DeferAgendaItemDialog.
/// </summary>
public class DeferAgendaItemResult
{
    public required Guid AgendaItemId { get; init; }
    public required Guid AnchorTeamMemberId { get; init; }
    public required int ExpirationDays { get; init; }
    public string? Note { get; init; }
}
