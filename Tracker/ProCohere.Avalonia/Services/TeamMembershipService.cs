using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing team memberships.
/// Handles the many-to-many relationship between teams and team members.
/// </summary>
public class TeamMembershipService
{
    #region Singleton

    private static readonly Lazy<TeamMembershipService> _instance =
        new(() => new TeamMembershipService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static TeamMembershipService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "team_membership.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    #region Error Tracking

    /// <summary>
    /// Last error message from membership operations.
    /// </summary>
    public string? LastError { get; private set; }

    #endregion

    private TeamMembershipService() { }

    /// <summary>
    /// Gets all teams that a specific team member belongs to.
    /// </summary>
    /// <param name="teamMemberId">The team member's ID.</param>
    /// <returns>List of teams the member belongs to.</returns>
    public async Task<List<Team>> GetTeamsForMemberAsync(Guid teamMemberId)
    {
        Log($"GetTeamsForMemberAsync starting for member={teamMemberId}...");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return new List<Team>();
        }

        try
        {
            // Step 1: Get membership records for this member
            var memberships = await client.From<TeamMembership>()
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var membershipList = memberships.Models ?? new List<TeamMembership>();
            Log($"Found {membershipList.Count} memberships for member");

            if (membershipList.Count == 0)
                return new List<Team>();

            // Step 2: Get the teams for these memberships
            var teamIds = membershipList.Select(m => m.TeamId.ToString()).Distinct().ToList();
            var teams = await client.From<Team>()
                .Filter("id", Operator.In, teamIds)
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("name", Ordering.Ascending)
                .Get();

            var teamList = teams.Models ?? new List<Team>();
            Log($"Loaded {teamList.Count} teams");

            return teamList;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return new List<Team>();
        }
    }

    /// <summary>
    /// Gets the current user's teams (teams they belong to).
    /// </summary>
    /// <returns>List of teams the current user belongs to.</returns>
    public async Task<List<Team>> GetMyTeamsAsync()
    {
        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.TeamMember == null)
        {
            LastError = "Not authenticated or no team member session";
            return new List<Team>();
        }

        return await GetTeamsForMemberAsync(session.TeamMember.Id);
    }

    /// <summary>
    /// Gets all members of a specific team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <returns>List of team memberships with member details populated.</returns>
    public async Task<List<TeamMembership>> GetMembersForTeamAsync(Guid teamId)
    {
        Log($"GetMembersForTeamAsync starting for team={teamId}...");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return new List<TeamMembership>();
        }

        try
        {
            // Step 1: Get membership records for this team
            var memberships = await client.From<TeamMembership>()
                .Filter("team_id", Operator.Equals, teamId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var membershipList = memberships.Models ?? new List<TeamMembership>();
            Log($"Found {membershipList.Count} memberships for team");

            if (membershipList.Count == 0)
                return new List<TeamMembership>();

            // Step 2: Get the team members for these memberships
            var memberIds = membershipList.Select(m => m.TeamMemberId.ToString()).Distinct().ToList();
            var members = await client.From<TeamMemberDetail>()
                .Filter("id", Operator.In, memberIds)
                .Filter("is_active", Operator.Equals, "true")
                .Order("first_name", Ordering.Ascending)
                .Get();

            var memberDict = (members.Models ?? new List<TeamMemberDetail>())
                .ToDictionary(m => m.Id);

            // Step 3: Enrich memberships with member details
            foreach (var membership in membershipList)
            {
                if (memberDict.TryGetValue(membership.TeamMemberId, out var member))
                {
                    // Convert TeamMemberDetail to TeamMember for the navigation property
                    // Note: TeamMemberDetail (from view) doesn't have all fields - just copy what's available
                    membership.Member = new TeamMember
                    {
                        Id = member.Id,
                        OrganizationId = membership.OrganizationId, // Use from membership, not member detail
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        // DisplayName is not in TeamMemberDetail view
                        Email = member.Email,
                        JobTitle = member.JobTitle,
                        IsActive = member.IsActive
                    };
                }
            }

            Log($"Enriched {membershipList.Count} memberships with member details");
            return membershipList;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return new List<TeamMembership>();
        }
    }

    /// <summary>
    /// Adds a member to a team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <param name="teamMemberId">The team member's ID.</param>
    /// <param name="role">The membership role (member, lead, viewer). Defaults to 'member'.</param>
    /// <returns>The created membership, or null if failed.</returns>
    public async Task<TeamMembership?> AddMemberToTeamAsync(Guid teamId, Guid teamMemberId, string role = TeamMembership.RoleMember)
    {
        Log($"AddMemberToTeamAsync: team={teamId}, member={teamMemberId}, role={role}");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return null;
        }

        // Validate role
        if (role != TeamMembership.RoleMember && role != TeamMembership.RoleLead && role != TeamMembership.RoleViewer)
        {
            LastError = $"Invalid role: {role}. Must be 'member', 'lead', or 'viewer'.";
            Log($"ERROR: {LastError}");
            return null;
        }

        try
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                TeamId = teamId,
                TeamMemberId = teamMemberId,
                Role = role,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await client.From<TeamMembership>()
                .Insert(membership);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Created membership: id={created.Id}");
                return created;
            }

            LastError = "Insert returned no data";
            Log($"ERROR: {LastError}");
            return null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Removes a member from a team (soft delete).
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <param name="teamMemberId">The team member's ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> RemoveMemberFromTeamAsync(Guid teamId, Guid teamMemberId)
    {
        Log($"RemoveMemberFromTeamAsync: team={teamId}, member={teamMemberId}");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return false;
        }

        try
        {
            // Get current user ID for deleted_by
            var currentUserId = AuthService.Instance.CurrentUser?.Id;
            Guid? deletedByUserId = null;
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var parsedUserId))
            {
                deletedByUserId = parsedUserId;
            }

            // Soft delete the membership
            var result = await client.From<TeamMembership>()
                .Filter("team_id", Operator.Equals, teamId.ToString())
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt!, DateTime.UtcNow)
                .Set(m => m.DeletedBy!, deletedByUserId)
                .Update();

            var success = result.Models?.Count > 0;
            Log($"RemoveMemberFromTeamAsync result: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates a member's role in a team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <param name="teamMemberId">The team member's ID.</param>
    /// <param name="newRole">The new role.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateMembershipRoleAsync(Guid teamId, Guid teamMemberId, string newRole)
    {
        Log($"UpdateMembershipRoleAsync: team={teamId}, member={teamMemberId}, role={newRole}");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return false;
        }

        // Validate role
        if (newRole != TeamMembership.RoleMember && newRole != TeamMembership.RoleLead && newRole != TeamMembership.RoleViewer)
        {
            LastError = $"Invalid role: {newRole}. Must be 'member', 'lead', or 'viewer'.";
            Log($"ERROR: {LastError}");
            return false;
        }

        try
        {
            var result = await client.From<TeamMembership>()
                .Filter("team_id", Operator.Equals, teamId.ToString())
                .Filter("team_member_id", Operator.Equals, teamMemberId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Set(m => m.Role, newRole)
                .Update();

            var success = result.Models?.Count > 0;
            Log($"UpdateMembershipRoleAsync result: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user is the lead of a specific team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <returns>True if the current user is the team lead.</returns>
    public async Task<bool> IsCurrentUserTeamLeadAsync(Guid teamId)
    {
        var session = AuthService.Instance.CurrentSession_ProCohere;
        if (session?.TeamMember == null)
            return false;

        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
            return false;

        try
        {
            // Check if team's lead_team_member_id matches current user
            var team = await client.From<Team>()
                .Filter("id", Operator.Equals, teamId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (team?.LeadTeamMemberId == session.TeamMember.Id)
                return true;

            // Also check membership role
            var membership = await client.From<TeamMembership>()
                .Filter("team_id", Operator.Equals, teamId.ToString())
                .Filter("team_member_id", Operator.Equals, session.TeamMember.Id.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("role", Operator.Equals, TeamMembership.RoleLead)
                .Single();

            return membership != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets team member details for team members, useful for auto-populating meeting attendees.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <returns>List of team member details.</returns>
    public async Task<List<TeamMemberDetail>> GetTeamMemberDetailsAsync(Guid teamId)
    {
        Log($"GetTeamMemberDetailsAsync for team={teamId}...");
        
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<TeamMemberDetail>();
        }

        try
        {
            // Get memberships
            var memberships = await client.From<TeamMembership>()
                .Filter("team_id", Operator.Equals, teamId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var membershipList = memberships.Models ?? new List<TeamMembership>();
            if (membershipList.Count == 0)
                return new List<TeamMemberDetail>();

            // Get member details
            var memberIds = membershipList.Select(m => m.TeamMemberId.ToString()).Distinct().ToList();
            var members = await client.From<TeamMemberDetail>()
                .Filter("id", Operator.In, memberIds)
                .Filter("is_active", Operator.Equals, "true")
                .Order("first_name", Ordering.Ascending)
                .Get();

            return members.Models ?? new List<TeamMemberDetail>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return new List<TeamMemberDetail>();
        }
    }
}
