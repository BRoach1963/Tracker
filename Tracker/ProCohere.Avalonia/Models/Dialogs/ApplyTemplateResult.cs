using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the ApplyTemplateDialog.
/// </summary>
public class ApplyTemplateResult
{
    public required Guid TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required int ItemsAdded { get; init; }
}
