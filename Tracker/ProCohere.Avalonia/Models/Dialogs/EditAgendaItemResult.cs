using ProCohere.Avalonia.Models;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the edit agenda item dialog.
/// </summary>
public class EditAgendaItemResult
{
    public bool WasSaved { get; set; }
    public bool IsDirty { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DisplayTitle { get; set; }
    public string? SharedContext { get; set; }
    public string? PrivateContext { get; set; }
    public string VisibilityScope { get; set; } = "meeting";
    public List<TalkingPoint> TalkingPoints { get; set; } = new();
}
