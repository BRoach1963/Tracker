using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the edit goal dialog.
/// Only includes fields that exist in the procohere.goals table.
/// </summary>
public class EditGoalResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GoalType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? OwnerTeamMemberId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
}
