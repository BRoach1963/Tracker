using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for team member operations.
/// Wraps DashboardService and other services with AI-friendly interface.
/// </summary>
public class TeamDataService : ITeamDataService
{
    private readonly DashboardService _dashboardService;

    public TeamDataService()
    {
        _dashboardService = DashboardService.Instance;
    }

    public async Task<List<TeamMemberDetail>> SearchTeamMembersAsync(string? query = null)
    {
        try
        {
            var allMembers = await GetTeamMembersAsync();
            
            if (string.IsNullOrEmpty(query))
                return allMembers;

            var searchTerm = query.ToLower();
            return allMembers.Where(member =>
                (member.FirstName?.ToLower().Contains(searchTerm) ?? false) ||
                (member.LastName?.ToLower().Contains(searchTerm) ?? false) ||
                (member.JobTitle?.ToLower().Contains(searchTerm) ?? false) ||
                (member.Email?.ToLower().Contains(searchTerm) ?? false) ||
                $"{member.FirstName} {member.LastName}".ToLower().Contains(searchTerm)
            ).ToList();
        }
        catch (Exception)
        {
            return new List<TeamMemberDetail>();
        }
    }

    public async Task<TeamMemberDetail?> GetTeamMemberByEmailAsync(string email)
    {
        try
        {
            var allMembers = await GetTeamMembersAsync();
            return allMembers.FirstOrDefault(m => 
                string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<TeamMemberDetail>> GetTeamMembersAsync(bool includeInactive = false)
    {
        try
        {
            // Load dashboard data to get team members
            var dashboardData = await _dashboardService.LoadDashboardDataAsync();
            
            if (dashboardData?.TeamMembers == null)
                return new List<TeamMemberDetail>();

            var members = dashboardData.TeamMembers.ToList();
            
            // Note: ProCohere doesn't seem to have an "active" flag, so we return all members
            // In the future, this could be enhanced with a proper team member status field
            
            return members;
        }
        catch (Exception)
        {
            return new List<TeamMemberDetail>();
        }
    }

    public async Task<List<TeamMemberDetail>> GetDirectReportsAsync()
    {
        try
        {
            // Get current user's profile
            var currentUser = AuthService.Instance.CurrentProfile;
            if (currentUser == null)
                return new List<TeamMemberDetail>();

            var allMembers = await GetTeamMembersAsync();
            
            // Note: ProCohere TeamMemberDetail doesn't have ManagerUserId property
            // For now, return empty list - this would need to be enhanced with
            // a proper manager-report relationship in the database
            return new List<TeamMemberDetail>();
        }
        catch (Exception)
        {
            return new List<TeamMemberDetail>();
        }
    }
}