using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// User session DTO returned by the get_user_session RPC.
/// Contains access status, user info, team member, and role data.
/// </summary>
public sealed class ProCohereUserSessionDto
{
    [JsonPropertyName("has_access")]
    public bool HasAccess { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("user")]
    public PublicUserDto? User { get; set; }

    [JsonPropertyName("team_member")]
    public TeamMemberDto? TeamMember { get; set; }

    [JsonPropertyName("role")]
    public RoleDto? Role { get; set; }
}

/// <summary>
/// Public user info from auth.users / public.users.
/// Only safe fields exposed.
/// </summary>
public sealed class PublicUserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Team member info for the current user in their organization.
/// </summary>
public sealed class TeamMemberDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("role_id")]
    public Guid? RoleId { get; set; }

    [JsonPropertyName("manager_team_member_id")]
    public Guid? ManagerTeamMemberId { get; set; }

    [JsonPropertyName("linked_user_id")]
    public Guid? LinkedUserId { get; set; }

    /// <summary>
    /// Computed full name.
    /// </summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Role info for the team member.
/// </summary>
public sealed class RoleDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("permissions")]
    public JsonElement? Permissions { get; set; } // JSONB returned as nested JSON object

    [JsonPropertyName("is_system_role")]
    public bool IsSystemRole { get; set; }
}
