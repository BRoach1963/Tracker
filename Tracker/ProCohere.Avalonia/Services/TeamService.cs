using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for loading team members with hierarchy-aware visibility.
/// Uses the get_visible_team_member_ids RPC to respect role-based access.
/// </summary>
public class TeamService
{
    #region Singleton

    private static readonly Lazy<TeamService> _instance =
        new(() => new TeamService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static TeamService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "team.log");

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

    #region Cache

    private List<TeamMemberDetail>? _cachedMembers;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Clears the cached team members (e.g., on logout or refresh).
    /// </summary>
    public void ClearCache()
    {
        _cachedMembers = null;
        _cacheExpiry = DateTime.MinValue;
        Log("Cache cleared");
    }

    #endregion

    #region Error Tracking

    /// <summary>
    /// Last error message from team operations.
    /// </summary>
    public string? LastError { get; private set; }

    #endregion

    private TeamService() { }

    /// <summary>
    /// Gets visible team members for the current user with hierarchy information.
    /// Uses 2-step pattern: RPC for visibility + PostgREST for full records.
    /// </summary>
    /// <param name="forceRefresh">If true, bypasses cache.</param>
    /// <returns>List of team members enriched with hierarchy data.</returns>
    public async Task<List<TeamMemberDetail>> GetVisibleTeamMembersAsync(bool forceRefresh = false)
    {
        Log("GetVisibleTeamMembersAsync starting...");
        LastError = null;

        // Check cache
        if (!forceRefresh && _cachedMembers != null && DateTime.UtcNow < _cacheExpiry)
        {
            Log($"Returning cached data ({_cachedMembers.Count} members)");
            return _cachedMembers;
        }

        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated or no team member session";
            Log($"ERROR: {LastError}");
            return new List<TeamMemberDetail>();
        }

        var orgId = session.TeamMember.OrganizationId;
        var teamMemberId = session.TeamMember.Id;

        Log($"Loading visible members for org={orgId}, teamMember={teamMemberId}");

        try
        {
            // Step 1: Call RPC to get visible IDs with depth/relation
            Log("Step 1: Calling get_visible_team_member_ids RPC...");
            var rpcResult = await client.Rpc("get_visible_team_member_ids", new
            {
                p_organization_id = orgId,
                p_team_member_id = teamMemberId
            });

            if (rpcResult?.Content == null)
            {
                LastError = "RPC returned no data";
                Log($"ERROR: {LastError}");
                return new List<TeamMemberDetail>();
            }

            Log($"RPC response: {rpcResult.Content}");

            // Parse RPC result into visibility map
            var visibilityData = JsonSerializer.Deserialize<List<VisibilityRow>>(
                rpcResult.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<VisibilityRow>();

            Log($"RPC returned {visibilityData.Count} visible member IDs");

            if (visibilityData.Count == 0)
            {
                Log("No visible members returned");
                return new List<TeamMemberDetail>();
            }

            // Build lookup dictionary
            var visibilityMap = visibilityData.ToDictionary(
                v => v.TeamMemberId,
                v => (depth: v.Depth, relation: v.Relation)
            );

            // Step 2: Fetch full team member records for visible IDs
            Log("Step 2: Fetching full team member records...");
            var visibleIds = visibilityData.Select(v => v.TeamMemberId.ToString()).ToList();
            
            // PostgREST IN filter
            var members = await client.From<TeamMemberDetail>()
                .Filter("id", Operator.In, visibleIds)
                .Filter("is_active", Operator.Equals, "true")
                .Order("first_name", Ordering.Ascending)
                .Get();

            var memberList = members.Models ?? new List<TeamMemberDetail>();
            Log($"Fetched {memberList.Count} full member records");

            // Step 3: Merge visibility data into members
            Log("Step 3: Merging hierarchy data...");
            foreach (var member in memberList)
            {
                if (visibilityMap.TryGetValue(member.Id, out var visibility))
                {
                    member.HierarchyDepth = visibility.depth;
                    member.Relation = visibility.relation;
                }
            }

            // Step 4: Compute counts from visible set
            Log("Step 4: Computing counts...");
            ComputeHierarchyCounts(memberList);

            // Step 5: Set manager names
            Log("Step 5: Setting manager names...");
            var memberDict = memberList.ToDictionary(m => m.Id);
            foreach (var member in memberList)
            {
                if (member.ManagerTeamMemberId.HasValue &&
                    memberDict.TryGetValue(member.ManagerTeamMemberId.Value, out var manager))
                {
                    member.ManagerName = manager.FullName;
                }
            }

            // Update cache
            _cachedMembers = memberList;
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

            Log($"SUCCESS: Returning {memberList.Count} enriched members");
            return memberList;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            Log($"STACK: {ex.StackTrace}");
            return new List<TeamMemberDetail>();
        }
    }

    /// <summary>
    /// Computes DirectReportCount and TotalDescendantCount from the visible set.
    /// </summary>
    private void ComputeHierarchyCounts(List<TeamMemberDetail> members)
    {
        foreach (var member in members)
        {
            // Direct reports: members whose manager is this person AND are 'direct' relation
            member.DirectReportCount = members.Count(m =>
                m.ManagerTeamMemberId == member.Id &&
                (m.Relation == "direct" || m.Relation == "descendant" || m.Relation == "peer" || m.Relation == "self"));

            // Actually, simpler: count members whose ManagerTeamMemberId == this member's Id
            member.DirectReportCount = members.Count(m => m.ManagerTeamMemberId == member.Id);

            // Total descendants: harder to compute without full tree, but we can approximate
            // For now, just use direct report count. Full descendant count would need recursive lookup.
            member.TotalDescendantCount = member.DirectReportCount;
        }
    }

    /// <summary>
    /// Gets the current user's team member record from the visible set.
    /// </summary>
    public async Task<TeamMemberDetail?> GetCurrentUserAsync()
    {
        var members = await GetVisibleTeamMembersAsync();
        return members.FirstOrDefault(m => m.Relation == "self");
    }

    /// <summary>
    /// Gets the current user's manager from the visible set.
    /// </summary>
    public async Task<TeamMemberDetail?> GetMyManagerAsync()
    {
        var members = await GetVisibleTeamMembersAsync();
        return members.FirstOrDefault(m => m.Relation == "manager");
    }

    /// <summary>
    /// Gets the current user's direct reports from the visible set.
    /// </summary>
    public async Task<List<TeamMemberDetail>> GetMyDirectReportsAsync()
    {
        var members = await GetVisibleTeamMembersAsync();
        return members.Where(m => m.Relation == "direct").ToList();
    }

    /// <summary>
    /// Gets the current user's peers from the visible set.
    /// </summary>
    public async Task<List<TeamMemberDetail>> GetMyPeersAsync()
    {
        var members = await GetVisibleTeamMembersAsync();
        return members.Where(m => m.Relation == "peer").ToList();
    }

    #region RPC Response Models

    /// <summary>
    /// Row returned by get_visible_team_member_ids RPC.
    /// Maps to snake_case JSON from PostgreSQL.
    /// </summary>
    private class VisibilityRow
    {
        [System.Text.Json.Serialization.JsonPropertyName("team_member_id")]
        public Guid TeamMemberId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("depth")]
        public int Depth { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("relation")]
        public string Relation { get; set; } = string.Empty;
    }

    #endregion

    #region Team Management

    /// <summary>
    /// Gets all teams in the organization.
    /// </summary>
    /// <returns>List of all active teams.</returns>
    public async Task<List<Team>> GetAllTeamsAsync()
    {
        Log("GetAllTeamsAsync starting...");
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
            var result = await client.From<Team>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("name", Ordering.Ascending)
                .Get();

            var teams = result.Models ?? new List<Team>();
            Log($"Loaded {teams.Count} teams");

            // Populate member counts via membership service
            foreach (var team in teams)
            {
                var memberships = await TeamMembershipService.Instance.GetMembersForTeamAsync(team.Id);
                team.Members.Clear();
                foreach (var m in memberships)
                {
                    if (m.Member != null)
                        team.Members.Add(m.Member);
                }
            }

            return teams;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return new List<Team>();
        }
    }

    /// <summary>
    /// Gets a team by ID with members populated.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <returns>The team with members populated, or null if not found.</returns>
    public async Task<Team?> GetTeamDetailAsync(Guid teamId)
    {
        Log($"GetTeamDetailAsync for team={teamId}...");
        LastError = null;

        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return null;
        }

        try
        {
            var team = await client.From<Team>()
                .Filter("id", Operator.Equals, teamId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (team == null)
            {
                LastError = "Team not found";
                return null;
            }

            // Populate members
            var memberships = await TeamMembershipService.Instance.GetMembersForTeamAsync(teamId);
            team.Members.Clear();
            foreach (var m in memberships)
            {
                if (m.Member != null)
                    team.Members.Add(m.Member);
            }

            // Populate lead if set
            if (team.LeadTeamMemberId.HasValue)
            {
                var leadMembership = memberships.FirstOrDefault(m => m.TeamMemberId == team.LeadTeamMemberId.Value);
                team.Lead = leadMembership?.Member;
            }

            Log($"Loaded team '{team.Name}' with {team.Members.Count} members");
            return team;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="name">Team name (required).</param>
    /// <param name="description">Team description (optional).</param>
    /// <param name="leadTeamMemberId">Team lead's ID (optional).</param>
    /// <param name="parentTeamId">Parent team's ID for hierarchy (optional).</param>
    /// <returns>The created team, or null if failed.</returns>
    public async Task<Team?> CreateTeamAsync(string name, string? description = null, 
        Guid? leadTeamMemberId = null, Guid? parentTeamId = null)
    {
        Log($"CreateTeamAsync: name='{name}'");
        LastError = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            LastError = "Team name is required";
            return null;
        }

        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return null;
        }

        try
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = session.TeamMember.OrganizationId,
                Name = name.Trim(),
                Description = description?.Trim(),
                LeadTeamMemberId = leadTeamMemberId,
                ParentTeamId = parentTeamId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<Team>()
                .Insert(team);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Created team: id={created.Id}, name='{created.Name}'");

                // If a lead was specified, add them as a member with 'lead' role
                if (leadTeamMemberId.HasValue)
                {
                    await TeamMembershipService.Instance.AddMemberToTeamAsync(
                        created.Id, leadTeamMemberId.Value, TeamMembership.RoleLead);
                }

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
    /// Updates an existing team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <param name="name">New team name.</param>
    /// <param name="description">New description.</param>
    /// <param name="leadTeamMemberId">New lead (null to clear).</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> UpdateTeamAsync(Guid teamId, string name, string? description = null, 
        Guid? leadTeamMemberId = null)
    {
        Log($"UpdateTeamAsync: id={teamId}, name='{name}'");
        LastError = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            LastError = "Team name is required";
            return false;
        }

        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            Log($"ERROR: {LastError}");
            return false;
        }

        try
        {
            var result = await client.From<Team>()
                .Filter("id", Operator.Equals, teamId.ToString())
                .Set(t => t.Name, name.Trim())
                .Set(t => t.Description!, description?.Trim())
                .Set(t => t.LeadTeamMemberId!, leadTeamMemberId)
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Update();

            var success = result.Models?.Count > 0;
            Log($"UpdateTeamAsync result: {success}");
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
    /// Soft deletes a team.
    /// </summary>
    /// <param name="teamId">The team's ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteTeamAsync(Guid teamId)
    {
        Log($"DeleteTeamAsync: id={teamId}");
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

            var result = await client.From<Team>()
                .Filter("id", Operator.Equals, teamId.ToString())
                .Set(t => t.IsDeleted, true)
                .Set(t => t.DeletedAt!, DateTime.UtcNow)
                .Set(t => t.DeletedBy!, deletedByUserId)
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Update();

            var success = result.Models?.Count > 0;
            Log($"DeleteTeamAsync result: {success}");
            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion
}
