using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for team member data operations.
/// Provides simplified, AI-friendly methods for team member information.
/// </summary>
public interface ITeamDataService
{
    /// <summary>
    /// Searches for team members by name, role, or department.
    /// </summary>
    /// <param name="query">Search term for name, role, or department</param>
    /// <returns>List of matching team members</returns>
    Task<List<TeamMemberDetail>> SearchTeamMembersAsync(string? query = null);

    /// <summary>
    /// Gets team member details by email.
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>Team member details or null if not found</returns>
    Task<TeamMemberDetail?> GetTeamMemberByEmailAsync(string email);

    /// <summary>
    /// Gets all team members in the organization.
    /// </summary>
    /// <param name="includeInactive">Include inactive members</param>
    /// <returns>List of team members</returns>
    Task<List<TeamMemberDetail>> GetTeamMembersAsync(bool includeInactive = false);

    /// <summary>
    /// Gets team members who report to the current user.
    /// </summary>
    /// <returns>List of direct reports</returns>
    Task<List<TeamMemberDetail>> GetDirectReportsAsync();
}