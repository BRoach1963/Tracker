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
/// Service for managing projects in Supabase procohere schema.
/// Handles CRUD operations, member management, and entity linking.
/// </summary>
public class ProjectService
{
    #region Singleton

    private static readonly Lazy<ProjectService> _instance =
        new(() => new ProjectService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static ProjectService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "project_service.log");

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

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private ProjectService() { }

    #region Read Operations

    /// <summary>
    /// Gets all projects for the organization (excluding deleted).
    /// Ordered by status (active first), then by creation date descending.
    /// </summary>
    public async Task<List<Project>> GetAllProjectsAsync()
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Project>();
        }

        try
        {
            Log("Loading all projects");

            var result = await client.From<Project>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("status", Ordering.Ascending)
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Projects returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<Project>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetAllProjects ERROR: {ex.Message}");
            return new List<Project>();
        }
    }

    /// <summary>
    /// Gets projects filtered by status.
    /// </summary>
    public async Task<List<Project>> GetProjectsByStatusAsync(string status)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Project>();
        }

        try
        {
            Log($"Loading projects with status: {status}");

            var result = await client.From<Project>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("status", Operator.Equals, status)
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Projects returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<Project>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectsByStatus ERROR: {ex.Message}");
            return new List<Project>();
        }
    }

    /// <summary>
    /// Gets a single project by ID with members and links.
    /// </summary>
    public async Task<Project?> GetProjectByIdAsync(Guid projectId, bool includeMembers = true, bool includeLinks = true)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Getting project: {projectId}");

            var result = await client.From<Project>()
                .Filter("id", Operator.Equals, projectId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (result == null)
            {
                LastError = "Project not found";
                return null;
            }

            // Load members if requested
            if (includeMembers)
            {
                result.Members = await GetProjectMembersAsync(projectId);
            }

            // Load links if requested
            if (includeLinks)
            {
                result.Links = await GetProjectLinksAsync(projectId);
            }
            
            // Populate owner info
            await PopulateOwnerInfoAsync(client, result);

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectById ERROR: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Populates owner display name, initials, and orphan status for a project.
    /// </summary>
    private async Task PopulateOwnerInfoAsync(Supabase.Client client, Project project)
    {
        try
        {
            var owner = await client.From<TeamMemberDetail>()
                .Filter("id", Operator.Equals, project.OwnerTeamMemberId.ToString())
                .Single();
            
            if (owner != null)
            {
                project.OwnerDisplayName = owner.FullName ?? owner.Email ?? "Unknown";
                project.OwnerInitials = owner.Initials ?? "?";
                project.IsOrphaned = !owner.IsActive;
            }
            else
            {
                project.OwnerDisplayName = "Unknown";
                project.OwnerInitials = "?";
                project.IsOrphaned = true;
            }
        }
        catch (Exception ex)
        {
            Log($"PopulateOwnerInfo WARNING: {ex.Message}");
            project.OwnerDisplayName = "Unknown";
            project.OwnerInitials = "?";
            project.IsOrphaned = true;
        }
    }

    /// <summary>
    /// Gets all members of a project.
    /// </summary>
    public async Task<List<ProjectMember>> GetProjectMembersAsync(Guid projectId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<ProjectMember>();
        }

        try
        {
            Log($"Loading members for project: {projectId}");

            var result = await client.From<ProjectMember>()
                .Filter("project_id", Operator.Equals, projectId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("role", Ordering.Ascending)
                .Get();

            var members = result.Models ?? new List<ProjectMember>();

            // Optionally load team member details
            if (members.Any())
            {
                var teamMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
                foreach (var member in members)
                {
                    member.TeamMember = teamMembers.FirstOrDefault(tm => tm.Id == member.TeamMemberId);
                }
            }

            Log($"Members returned: {members.Count}");
            return members;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectMembers ERROR: {ex.Message}");
            return new List<ProjectMember>();
        }
    }

    /// <summary>
    /// Gets all links for a project.
    /// </summary>
    public async Task<List<ProjectLink>> GetProjectLinksAsync(Guid projectId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<ProjectLink>();
        }

        try
        {
            Log($"Loading links for project: {projectId}");

            var result = await client.From<ProjectLink>()
                .Filter("project_id", Operator.Equals, projectId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("entity_type", Ordering.Ascending)
                .Order("created_at", Ordering.Descending)
                .Get();

            Log($"Links returned: {result.Models?.Count ?? 0}");
            return result.Models ?? new List<ProjectLink>();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectLinks ERROR: {ex.Message}");
            return new List<ProjectLink>();
        }
    }

    /// <summary>
    /// Gets projects that a specific team member is part of.
    /// </summary>
    public async Task<List<Project>> GetProjectsForTeamMemberAsync(Guid teamMemberId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Project>();
        }

        try
        {
            Log($"Loading projects for team member: {teamMemberId}");

            // Get projects where user is owner or member
            var allProjects = await GetAllProjectsAsync();
            
            var memberProjects = new List<Project>();
            foreach (var project in allProjects)
            {
                // Check if owner
                if (project.OwnerTeamMemberId == teamMemberId)
                {
                    memberProjects.Add(project);
                    continue;
                }

                // Check if member
                var members = await GetProjectMembersAsync(project.Id);
                if (members.Any(m => m.TeamMemberId == teamMemberId))
                {
                    memberProjects.Add(project);
                }
            }

            Log($"Projects for team member: {memberProjects.Count}");
            return memberProjects;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectsForTeamMember ERROR: {ex.Message}");
            return new List<Project>();
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Creates a new project via RPC.
    /// </summary>
    public async Task<Project?> CreateProjectAsync(
        string name,
        string? description = null,
        string status = ProjectStatus.Active,
        DateTime? dueDate = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            LastError = "Project name is required";
            return null;
        }

        try
        {
            Log($"Creating project: {name}");

            var rpcResult = await client.Rpc("rpc_create_project", new
            {
                p_name = name.Trim(),
                p_description = description,
                p_status = status,
                p_due_date = dueDate?.ToString("yyyy-MM-dd")
            });

            Log($"Create project RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"CreateProject ERROR: {LastError}");
                return null;
            }

            // Parse the returned project from RPC result
            var project = ParseProjectFromRpcResult(rpcResult?.Content);
            if (project == null)
            {
                LastError = "Failed to parse project from RPC result";
                Log($"CreateProject ERROR: {LastError}");
                return null;
            }

            Log($"Project created: {project.Id}");
            return project;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateProject ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates a project via RPC.
    /// </summary>
    public async Task<Project?> UpdateProjectAsync(
        Guid projectId,
        string? name = null,
        string? description = null,
        string? status = null,
        DateTime? dueDate = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating project: {projectId}");

            var rpcResult = await client.Rpc("rpc_update_project", new
            {
                p_project_id = projectId,
                p_name = name?.Trim(),
                p_description = description,
                p_status = status,
                p_due_date = dueDate?.ToString("yyyy-MM-dd")
            });

            Log($"Update project RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"UpdateProject ERROR: {LastError}");
                return null;
            }

            // Parse the returned project from RPC result
            var project = ParseProjectFromRpcResult(rpcResult?.Content);
            if (project == null)
            {
                LastError = "Failed to parse project from RPC result";
                Log($"UpdateProject ERROR: {LastError}");
                return null;
            }

            Log($"Project updated: {project.Id}");
            return project;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateProject ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates project status.
    /// </summary>
    public async Task<Project?> UpdateProjectStatusAsync(Guid projectId, string status)
    {
        return await UpdateProjectAsync(projectId, status: status);
    }

    /// <summary>
    /// Transfers project ownership to a new team member via RPC.
    /// Only the current owner can transfer ownership.
    /// </summary>
    public async Task<bool> TransferOwnershipAsync(Guid projectId, Guid newOwnerTeamMemberId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Transferring ownership of project {projectId} to {newOwnerTeamMemberId}");

            var rpcResult = await client.Rpc(
                "rpc_transfer_project_ownership",
                new
                {
                    p_project_id = projectId,
                    p_new_owner_id = newOwnerTeamMemberId
                });

            // Check for errors
            if (!string.IsNullOrEmpty(rpcResult?.Content))
            {
                var content = rpcResult.Content;
                if (content.Contains("\"error\"") || content.Contains("not found") || 
                    content.Contains("permission") || content.Contains("denied"))
                {
                    LastError = "Failed to transfer ownership. You may not have permission.";
                    Log($"TransferOwnership ERROR: {content}");
                    return false;
                }
            }

            Log($"Project ownership transferred successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"TransferOwnership ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Soft-deletes a project via RPC.
    /// Also soft-deletes all members and links.
    /// </summary>
    public async Task<bool> DeleteProjectAsync(Guid projectId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting project: {projectId}");

            var rpcResult = await client.Rpc("rpc_delete_project", new
            {
                p_project_id = projectId
            });

            Log($"Delete project RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"DeleteProject ERROR: {LastError}");
                return false;
            }

            Log($"Project deleted: {projectId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteProject ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Member Management

    /// <summary>
    /// Adds a team member to a project via RPC.
    /// </summary>
    public async Task<ProjectMember?> AddProjectMemberAsync(
        Guid projectId,
        Guid teamMemberId,
        string role = ProjectMemberRole.Member)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Adding member {teamMemberId} to project {projectId} with role {role}");

            var rpcResult = await client.Rpc("rpc_add_project_member", new
            {
                p_project_id = projectId,
                p_team_member_id = teamMemberId,
                p_role = role
            });

            Log($"Add member RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"AddProjectMember ERROR: {LastError}");
                return null;
            }

            // Parse the returned member from RPC result
            var member = ParseProjectMemberFromRpcResult(rpcResult?.Content);
            if (member == null)
            {
                LastError = "Failed to parse member from RPC result";
                Log($"AddProjectMember ERROR: {LastError}");
                return null;
            }

            Log($"Member added: {member.Id}");
            return member;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"AddProjectMember ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Removes a team member from a project via RPC.
    /// </summary>
    public async Task<bool> RemoveProjectMemberAsync(Guid projectId, Guid teamMemberId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Removing member {teamMemberId} from project {projectId}");

            var rpcResult = await client.Rpc("rpc_remove_project_member", new
            {
                p_project_id = projectId,
                p_team_member_id = teamMemberId
            });

            Log($"Remove member RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"RemoveProjectMember ERROR: {LastError}");
                return false;
            }

            Log($"Member removed from project {projectId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveProjectMember ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates a member's role on a project.
    /// </summary>
    public async Task<ProjectMember?> UpdateMemberRoleAsync(Guid projectId, Guid teamMemberId, string newRole)
    {
        // The RPC uses upsert, so calling add with a new role will update
        return await AddProjectMemberAsync(projectId, teamMemberId, newRole);
    }

    #endregion

    #region Link Management

    /// <summary>
    /// Adds a link from a project to an entity via RPC.
    /// </summary>
    public async Task<ProjectLink?> AddProjectLinkAsync(
        Guid projectId,
        string entityType,
        Guid entityId,
        string? entityTitleSnapshot = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Adding link to project {projectId}: {entityType}/{entityId}");

            var rpcResult = await client.Rpc("rpc_add_project_link", new
            {
                p_project_id = projectId,
                p_entity_type = entityType,
                p_entity_id = entityId,
                p_entity_title_snapshot = entityTitleSnapshot
            });

            Log($"Add link RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"AddProjectLink ERROR: {LastError}");
                return null;
            }

            // Parse the returned link from RPC result
            var link = ParseProjectLinkFromRpcResult(rpcResult?.Content);
            if (link == null)
            {
                LastError = "Failed to parse link from RPC result";
                Log($"AddProjectLink ERROR: {LastError}");
                return null;
            }

            Log($"Link added: {link.Id}");
            return link;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"AddProjectLink ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Removes a link from a project via RPC.
    /// </summary>
    public async Task<bool> RemoveProjectLinkAsync(Guid projectId, string entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Removing link from project {projectId}: {entityType}/{entityId}");

            var rpcResult = await client.Rpc("rpc_remove_project_link", new
            {
                p_project_id = projectId,
                p_entity_type = entityType,
                p_entity_id = entityId
            });

            Log($"Remove link RPC result: {rpcResult?.Content ?? "NULL"}");

            if (rpcResult?.Content?.Contains("error") == true)
            {
                LastError = rpcResult.Content;
                Log($"RemoveProjectLink ERROR: {LastError}");
                return false;
            }

            Log($"Link removed from project {projectId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"RemoveProjectLink ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all projects linked to a specific entity.
    /// </summary>
    public async Task<List<Project>> GetProjectsForEntityAsync(string entityType, Guid entityId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<Project>();
        }

        try
        {
            Log($"Loading projects linked to {entityType}/{entityId}");

            // Get all links matching the entity
            var links = await client.From<ProjectLink>()
                .Filter("entity_type", Operator.Equals, entityType)
                .Filter("entity_id", Operator.Equals, entityId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            if (links.Models == null || !links.Models.Any())
            {
                return new List<Project>();
            }

            // Get the projects for those links
            var projectIds = links.Models.Select(l => l.ProjectId).Distinct().ToList();
            var projects = new List<Project>();

            foreach (var projectId in projectIds)
            {
                var project = await GetProjectByIdAsync(projectId, includeMembers: false, includeLinks: false);
                if (project != null)
                {
                    projects.Add(project);
                }
            }

            Log($"Projects for entity: {projects.Count}");
            return projects;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectsForEntity ERROR: {ex.Message}");
            return new List<Project>();
        }
    }

    #endregion

    #region Parsing Helpers

    private static Project? ParseProjectFromRpcResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            return JsonSerializer.Deserialize<Project>(json, options);
        }
        catch (Exception ex)
        {
            Log($"ParseProjectFromRpcResult ERROR: {ex.Message}");
            return null;
        }
    }

    private static ProjectMember? ParseProjectMemberFromRpcResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            return JsonSerializer.Deserialize<ProjectMember>(json, options);
        }
        catch (Exception ex)
        {
            Log($"ParseProjectMemberFromRpcResult ERROR: {ex.Message}");
            return null;
        }
    }

    private static ProjectLink? ParseProjectLinkFromRpcResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            return JsonSerializer.Deserialize<ProjectLink>(json, options);
        }
        catch (Exception ex)
        {
            Log($"ParseProjectLinkFromRpcResult ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Gets project signals (overdue tasks, goals needing attention) for multiple projects in one call.
    /// Uses procohere.get_project_signals_batch RPC to replace N+1 queries.
    /// </summary>
    /// <param name="projectIds">Project IDs to get signals for. Pass null for all visible projects.</param>
    /// <returns>List of signal results with project_id, overdue_task_count, goals_needing_attention.</returns>
    public async Task<List<ProjectSignalsBatchResult>> GetProjectSignalsBatchAsync(
        IEnumerable<Guid>? projectIds = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<ProjectSignalsBatchResult>();
        }

        try
        {
            var idsArray = projectIds?.ToArray();
            Log($"Getting project signals batch for {idsArray?.Length ?? 0} projects (null = all)");

            var rpcResult = await client.Rpc("get_project_signals_batch", new
            {
                p_project_ids = idsArray
            });

            if (rpcResult?.Content == null)
            {
                Log("RPC returned no content");
                return new List<ProjectSignalsBatchResult>();
            }

            Log($"RPC response length: {rpcResult.Content.Length}");

            var results = JsonSerializer.Deserialize<List<ProjectSignalsBatchResult>>(
                rpcResult.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<ProjectSignalsBatchResult>();

            Log($"Project signals batch returned: {results.Count} results");
            return results;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetProjectSignalsBatch ERROR: {ex.Message}");
            return new List<ProjectSignalsBatchResult>();
        }
    }

    #endregion
}
