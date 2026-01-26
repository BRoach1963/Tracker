using System;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result from the invite team member dialog.
/// </summary>
public class InviteTeamMemberResult
{
    /// <summary>
    /// Email address to send invite to.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Job title for the new team member.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Manager to assign the new team member to.
    /// </summary>
    public Guid? ManagerTeamMemberId { get; set; }

    /// <summary>
    /// Optional personal message to include in invite.
    /// </summary>
    public string? PersonalMessage { get; set; }
}
