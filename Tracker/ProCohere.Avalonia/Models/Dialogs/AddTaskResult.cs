using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the AddTaskDialog.
/// </summary>
public class AddTaskResult
{
    public Guid? Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Priority { get; init; }
    public string? Status { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssigneeId { get; init; }
    public bool IsDeleted { get; init; }
}
