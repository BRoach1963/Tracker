using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for Supabase authentication with Windows Credential Manager integration.
/// Supports "Remember Me" functionality for auto-login on subsequent launches.
/// </summary>
public class AuthService
{
    #region Singleton

    private static readonly Lazy<AuthService> _instance =
        new(() => new AuthService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static AuthService Instance => _instance.Value;

    #endregion

    #region Fields

    private Supabase.Client? _client;
    private bool _isInitialized;
    private readonly ICredentialService _credentialService;

    #endregion

    #region Properties

    public bool IsInitialized => _isInitialized;
    public bool IsSignedIn => _client?.Auth.CurrentUser != null;
    public User? CurrentUser => _client?.Auth.CurrentUser;
    public Session? CurrentSession => _client?.Auth.CurrentSession;
    public string? AccessToken => _client?.Auth.CurrentSession?.AccessToken;
    
    /// <summary>
    /// The current user's profile from the database.
    /// </summary>
    public UserProfile? CurrentProfile { get; private set; }

    /// <summary>
    /// Gets the Supabase client for use by other services.
    /// </summary>
    public Supabase.Client? GetSupabaseClient() => _client;

    #endregion

    #region Events

    public event EventHandler<User?>? AuthStateChanged;
    
    /// <summary>
    /// Raised when user profile is loaded or updated.
    /// </summary>
    public event EventHandler<UserProfile?>? ProfileChanged;

    #endregion

    #region Initialization

    private AuthService()
    {
        _credentialService = new WindowsCredentialService();
    }

    /// <summary>
    /// Initializes the Supabase client. Does NOT attempt to restore session.
    /// Call TryAutoLoginAsync() separately to attempt auto-login.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _client = new Supabase.Client(
                SupabaseConfig.ProjectUrl,
                SupabaseConfig.AnonKey,
                options);

            await _client.InitializeAsync();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize Supabase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to auto-login using stored credentials from Windows Credential Manager.
    /// </summary>
    /// <returns>True if auto-login succeeded, false if manual login is required.</returns>
    public async Task<bool> TryAutoLoginAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        if (!_credentialService.HasStoredSession())
        {
            return false;
        }

        try
        {
            var (accessToken, refreshToken) = _credentialService.GetStoredSession();

            if (string.IsNullOrEmpty(refreshToken))
            {
                _credentialService.ClearSession();
                return false;
            }

            // Try to restore the session using the refresh token
            var session = await _client!.Auth.SetSession(accessToken!, refreshToken);

            if (session?.User != null)
            {
                // Update stored tokens with new ones from refresh
                if (!string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
                }

                AuthStateChanged?.Invoke(this, session.User);
                return true;
            }

            // Session restore failed - clear stored credentials
            _credentialService.ClearSession();
            return false;
        }
        catch (Exception)
        {
            // Token likely expired or revoked - clear and require manual login
            _credentialService.ClearSession();
            return false;
        }
    }

    #endregion

    #region Authentication

    public async Task<(bool Success, string? Error)> SignInAsync(string email, string password, bool persistSession = false)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            var session = await _client!.Auth.SignIn(email, password);

            if (session?.User != null)
            {
                if (persistSession && !string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    // Store in Windows Credential Manager for auto-login
                    _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
                }
                else
                {
                    // User didn't check "Remember Me" - clear any stored session
                    _credentialService.ClearSession();
                }

                AuthStateChanged?.Invoke(this, session.User);
                return (true, null);
            }

            return (false, "Sign in failed. Please check your credentials.");
        }
        catch (GotrueException ex)
        {
            return (false, GetFriendlyAuthError(ex));
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> SignUpAsync(string email, string password, string? displayName = null)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            var session = await _client!.Auth.SignUp(email, password, new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    ["display_name"] = displayName ?? email.Split('@')[0]
                }
            });

            if (session?.User != null)
            {
                // Always persist session for new sign-ups (they can disable later in settings)
                if (!string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
                }
                AuthStateChanged?.Invoke(this, session.User);
                return (true, null);
            }

            return (false, "Sign up failed. Please try again.");
        }
        catch (GotrueException ex)
        {
            return (false, GetFriendlyAuthError(ex));
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task SignOutAsync()
    {
        if (_client?.Auth != null)
        {
            await _client.Auth.SignOut();
        }
        // Clear stored credentials on logout
        _credentialService.ClearSession();
        AuthStateChanged?.Invoke(this, null);
    }

    /// <summary>
    /// Updates the current user's metadata (profile information).
    /// </summary>
    public async Task<bool> UpdateUserMetadataAsync(Dictionary<string, object> metadata)
    {
        if (_client?.Auth?.CurrentUser == null)
        {
            throw new InvalidOperationException("User is not signed in.");
        }

        try
        {
            var attributes = new UserAttributes
            {
                Data = metadata
            };

            var user = await _client.Auth.Update(attributes);
            
            if (user != null)
            {
                AuthStateChanged?.Invoke(this, user);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update user metadata: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Profile Management

    /// <summary>
    /// Loads the current user's profile from the database.
    /// </summary>
    /// <returns>The user profile, or null if not found.</returns>
    public async Task<UserProfile?> LoadUserProfileAsync()
    {
        if (_client?.Auth?.CurrentUser == null)
        {
            System.Diagnostics.Debug.WriteLine("LoadUserProfileAsync: No current user");
            return null;
        }

        try
        {
            var authId = _client.Auth.CurrentUser.Id;
            System.Diagnostics.Debug.WriteLine($"LoadUserProfileAsync: Querying for auth ID: {authId}");
            
            var userId = Guid.Parse(authId);
            
            var result = await _client.From<UserProfile>()
                .Where(p => p.SupabaseAuthId == userId)
                .Single();

            CurrentProfile = result;
            ProfileChanged?.Invoke(this, result);
            
            System.Diagnostics.Debug.WriteLine($"Profile loaded: {result?.DisplayName ?? "null"}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load profile: {ex.GetType().Name}: {ex.Message}");
            LastProfileError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Last error from profile loading - for debugging.
    /// </summary>
    public string? LastProfileError { get; private set; }

    /// <summary>
    /// Updates the current user's profile in the database.
    /// </summary>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="jobTitle">Job title</param>
    /// <param name="company">Company name</param>
    /// <param name="phone">Phone number</param>
    /// <returns>True if update succeeded.</returns>
    public async Task<(bool Success, string? Error)> UpdateUserProfileAsync(
        string? firstName,
        string? lastName,
        string? jobTitle,
        string? company,
        string? phone)
    {
        if (_client?.Auth?.CurrentUser == null)
        {
            return (false, "Not signed in.");
        }

        try
        {
            var userId = Guid.Parse(_client.Auth.CurrentUser.Id);
            
            // Build display name from first/last
            var displayName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = _client.Auth.CurrentUser.Email?.Split('@')[0] ?? "User";
            }

            // Update the profile in the users table
            var updatedProfiles = await _client.From<UserProfile>()
                .Where(p => p.SupabaseAuthId == userId)
                .Set(p => p.FirstName!, firstName ?? string.Empty)
                .Set(p => p.LastName!, lastName ?? string.Empty)
                .Set(p => p.DisplayName!, displayName)
                .Set(p => p.JobTitle!, jobTitle ?? string.Empty)
                .Set(p => p.Company!, company ?? string.Empty)
                .Set(p => p.Phone!, phone ?? string.Empty)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Update();

            // Reload the profile to get the updated data
            await LoadUserProfileAsync();
            
            System.Diagnostics.Debug.WriteLine($"Profile updated: {displayName}");
            return (true, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update profile: {ex.Message}");
            return (false, $"Failed to update profile: {ex.Message}");
        }
    }

    #endregion

    #region Helpers

    private static string GetFriendlyAuthError(GotrueException ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (message.Contains("invalid login") || message.Contains("invalid_credentials"))
        {
            return "Invalid email or password.";
        }
        if (message.Contains("email not confirmed"))
        {
            return "Please verify your email address before signing in.";
        }
        if (message.Contains("too many requests") || message.Contains("rate limit"))
        {
            return "Too many attempts. Please wait a moment and try again.";
        }
        if (message.Contains("user already registered"))
        {
            return "An account with this email already exists.";
        }
        if (message.Contains("password"))
        {
            return "Password must be at least 6 characters.";
        }

        return "Authentication failed. Please try again.";
    }

    #endregion

    #region Data Queries

    /// <summary>
    /// Gets counts of data items for the current user.
    /// </summary>
    public async Task<DataCounts> GetDataCountsAsync()
    {
        var counts = new DataCounts();
        var errors = new List<string>();
        
        if (_client == null || CurrentProfile == null)
        {
            LastDataCountError = "Client or profile is null";
            return counts;
        }

        System.Diagnostics.Debug.WriteLine($"GetDataCountsAsync: User ID = {CurrentProfile.Id}");

        // Get team members managed by this user
        try
        {
            var teamMembersResult = await _client.From<TeamMemberSimple>()
                .Where(t => t.ManagerUserId == CurrentProfile.Id)
                .Get();
            counts.TeamMembers = teamMembersResult.Models?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"Team members: {counts.TeamMembers}");
        }
        catch (Exception ex)
        {
            errors.Add($"TeamMembers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"TeamMembers error: {ex.Message}");
        }

        // Get meetings created by this user
        try
        {
            var meetingsResult = await _client.From<MeetingSimple>()
                .Where(m => m.CreatedByUserId == CurrentProfile.Id)
                .Get();
            counts.Meetings = meetingsResult.Models?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"Meetings: {counts.Meetings}");
        }
        catch (Exception ex)
        {
            errors.Add($"Meetings: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Meetings error: {ex.Message}");
        }

        // Get goals created by this user
        try
        {
            var goalsResult = await _client.From<GoalSimple>()
                .Where(g => g.CreatedByUserId == CurrentProfile.Id)
                .Get();
            counts.Goals = goalsResult.Models?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"Goals: {counts.Goals}");
        }
        catch (Exception ex)
        {
            errors.Add($"Goals: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Goals error: {ex.Message}");
        }

        // Get tasks created by this user
        try
        {
            var tasksResult = await _client.From<TaskSimple>()
                .Where(t => t.CreatedByUserId == CurrentProfile.Id)
                .Get();
            counts.Tasks = tasksResult.Models?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"Tasks: {counts.Tasks}");
        }
        catch (Exception ex)
        {
            errors.Add($"Tasks: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Tasks error: {ex.Message}");
        }

        // Get projects created by this user
        try
        {
            var projectsResult = await _client.From<ProjectSimple>()
                .Where(p => p.CreatedByUserId == CurrentProfile.Id)
                .Get();
            counts.Projects = projectsResult.Models?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"Projects: {counts.Projects}");
        }
        catch (Exception ex)
        {
            errors.Add($"Projects: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Projects error: {ex.Message}");
        }

        LastDataCountError = errors.Count > 0 ? string.Join("; ", errors) : null;
        return counts;
    }
    
    /// <summary>
    /// Last error from data count queries - for debugging.
    /// </summary>
    public string? LastDataCountError { get; private set; }

    #endregion
}

/// <summary>
/// Simple data counts for the current user.
/// </summary>
public class DataCounts
{
    public int TeamMembers { get; set; }
    public int Meetings { get; set; }
    public int Goals { get; set; }
    public int Tasks { get; set; }
    public int Projects { get; set; }
}

/// <summary>
/// Minimal model for querying team_members table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("team_members")]
public class TeamMemberSimple : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("manager_user_id")]
    public Guid? ManagerUserId { get; set; }

    [Supabase.Postgrest.Attributes.Column("first_name")]
    public string? FirstName { get; set; }

    [Supabase.Postgrest.Attributes.Column("last_name")]
    public string? LastName { get; set; }
}

/// <summary>
/// Minimal model for querying meetings table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("meetings")]
public class MeetingSimple : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Minimal model for querying goals table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("goals")]
public class GoalSimple : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Minimal model for querying tasks table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("tasks")]
public class TaskSimple : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Supabase.Postgrest.Attributes.Column("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Minimal model for querying projects table.
/// </summary>
[Supabase.Postgrest.Attributes.Table("projects")]
public class ProjectSimple : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    [Supabase.Postgrest.Attributes.Column("name")]
    public string? Name { get; set; }
}
