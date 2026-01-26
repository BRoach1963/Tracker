using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the edit metric dialog.
/// </summary>
public class EditMetricResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? BaselineValue { get; set; }
    public string? Unit { get; set; }
    public string? TargetDirection { get; set; }
    public string? Source { get; set; }
    public string? Scope { get; set; }
    public string? Frequency { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public string? Lifecycle { get; set; }
    public bool IsTeamVisible { get; set; }
    public bool IsOrgVisible { get; set; }
    public bool IsSensitive { get; set; }
}
