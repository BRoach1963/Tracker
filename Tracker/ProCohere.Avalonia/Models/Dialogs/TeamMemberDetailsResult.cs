using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the team member details dialog.
/// Contains only the fields a manager can edit.
/// </summary>
public class TeamMemberDetailsResult
{
    /// <summary>
    /// The team member ID being edited.
    /// </summary>
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Job title (org-specific, manager can edit).
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Manager assignment (manager can edit).
    /// </summary>
    public Guid? ManagerTeamMemberId { get; set; }

    /// <summary>
    /// Hire date (org-specific, manager can edit).
    /// </summary>
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// Whether the team member is active (manager can edit).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the team member should be deactivated (soft delete).
    /// </summary>
    public bool IsDeactivated { get; set; }
}
