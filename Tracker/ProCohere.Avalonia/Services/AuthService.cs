using System;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>
    /// Client configured for public schema - used for auth and licensing operations.
    /// </summary>
    private Supabase.Client? _publicClient;
    
    /// <summary>
    /// Client configured for procohere schema - used for app data operations.
    /// </summary>
    private Supabase.Client? _procohereClient;
    
    private bool _isInitialized;
    private readonly ICredentialService _credentialService;

    #endregion

    #region Properties

    public bool IsInitialized => _isInitialized;
    public bool IsSignedIn => _publicClient?.Auth.CurrentUser != null;
    public User? CurrentUser => _publicClient?.Auth.CurrentUser;
    public Session? CurrentSession => _publicClient?.Auth.CurrentSession;
    public string? AccessToken => _publicClient?.Auth.CurrentSession?.AccessToken;
    
    /// <summary>
    /// The current user's profile from the database.
    /// </summary>
    public UserProfile? CurrentProfile { get; private set; }

    /// <summary>
    /// The current user's session data including team member and role info.
    /// Populated after successful login via GetUserSessionAsync.
    /// </summary>
    public ProCohereUserSessionDto? CurrentSession_ProCohere { get; private set; }

    /// <summary>
    /// The current user's team member record. Shortcut to CurrentSession_ProCohere?.TeamMember.
    /// </summary>
    public TeamMemberDto? CurrentTeamMember => CurrentSession_ProCohere?.TeamMember;

    /// <summary>
    /// The current user's role. Shortcut to CurrentSession_ProCohere?.Role.
    /// </summary>
    public RoleDto? CurrentRole => CurrentSession_ProCohere?.Role;

    /// <summary>
    /// Gets the public schema Supabase client (for auth and licensing operations).
    /// </summary>
    public Supabase.Client? GetSupabaseClient() => _publicClient;
    
    /// <summary>
    /// Gets the procohere schema Supabase client (for app data operations).
    /// </summary>
    public Supabase.Client? GetProCohereClient() => _procohereClient;

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
    /// Initializes the Supabase clients. Does NOT attempt to restore session.
    /// Call TryAutoLoginAsync() separately to attempt auto-login.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            // Public schema client - for auth and licensing operations
            var publicOptions = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
                // Schema defaults to "public"
            };

            _publicClient = new Supabase.Client(
                SupabaseConfig.ProjectUrl,
                SupabaseConfig.AnonKey,
                publicOptions);

            await _publicClient.InitializeAsync();

            // ProCohere schema client - for app data operations
            var procohereOptions = new SupabaseOptions
            {
                AutoRefreshToken = false,  // Only public client manages auth
                AutoConnectRealtime = false,
                Schema = "procohere"
            };

            _procohereClient = new Supabase.Client(
                SupabaseConfig.ProjectUrl,
                SupabaseConfig.AnonKey,
                procohereOptions);

            await _procohereClient.InitializeAsync();

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
            var session = await _publicClient!.Auth.SetSession(accessToken!, refreshToken);

            if (session?.User != null)
            {
                // Update stored tokens with new ones from refresh
                if (!string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
                {
                    _credentialService.StoreSession(session.AccessToken, session.RefreshToken);
                }

                // Sync auth to procohere client so it can make authenticated requests
                await SyncAuthToProCohereClientAsync();

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

    /// <summary>
    /// Syncs the auth session from the public client to the procohere client.
    /// Must be called after any successful authentication (signin, signup, session restore).
    /// </summary>
    private async Task SyncAuthToProCohereClientAsync()
    {
        var session = _publicClient?.Auth.CurrentSession;
        if (session != null && _procohereClient != null)
        {
            await _procohereClient.Auth.SetSession(session.AccessToken!, session.RefreshToken!);
            System.Diagnostics.Debug.WriteLine("Auth session synced to procohere client");
        }
    }

    public async Task<(bool Success, string? Error)> SignInAsync(string email, string password, bool persistSession = false)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            var session = await _publicClient!.Auth.SignIn(email, password);

            if (session?.User != null)
            {
                // Sync auth to procohere client so it can make authenticated requests
                await SyncAuthToProCohereClientAsync();

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
            var session = await _publicClient!.Auth.SignUp(email, password, new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    ["display_name"] = displayName ?? email.Split('@')[0]
                }
            });

            if (session?.User != null)
            {
                // Sync auth to procohere client so it can make authenticated requests
                await SyncAuthToProCohereClientAsync();

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
        if (_publicClient?.Auth != null)
        {
            await _publicClient.Auth.SignOut();
        }
        // Clear stored credentials and session data on logout
        _credentialService.ClearSession();
        ClearSessionData();
        AuthStateChanged?.Invoke(this, null);
    }

    /// <summary>
    /// Updates the current user's metadata (profile information).
    /// </summary>
    public async Task<bool> UpdateUserMetadataAsync(Dictionary<string, object> metadata)
    {
        if (_publicClient?.Auth?.CurrentUser == null)
        {
            throw new InvalidOperationException("User is not signed in.");
        }

        try
        {
            var attributes = new UserAttributes
            {
                Data = metadata
            };

            var user = await _publicClient.Auth.Update(attributes);
            
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
        if (_publicClient?.Auth?.CurrentUser == null)
        {
            System.Diagnostics.Debug.WriteLine("LoadUserProfileAsync: No current user");
            return null;
        }

        try
        {
            var authId = _publicClient.Auth.CurrentUser.Id;
            System.Diagnostics.Debug.WriteLine($"LoadUserProfileAsync: Querying for user ID: {authId}");
            
            var userId = Guid.Parse(authId);
            
            // In new schema, users.id = auth.users.id (no separate supabase_auth_id)
            var result = await _publicClient.From<UserProfile>()
                .Where(p => p.Id == userId)
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
        if (_publicClient?.Auth?.CurrentUser == null)
        {
            return (false, "Not signed in.");
        }

        try
        {
            var userId = Guid.Parse(_publicClient.Auth.CurrentUser.Id);
            
            // Build display name from first/last
            var displayName = $"{firstName} {lastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = _publicClient.Auth.CurrentUser.Email?.Split('@')[0] ?? "User";
            }

            // Update the profile in the users table (id = auth.uid in new schema)
            var updatedProfiles = await _publicClient.From<UserProfile>()
                .Where(p => p.Id == userId)
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

    #region Product Access

    /// <summary>
    /// Checks if the current user has active access to a specific product.
    /// This verifies both: user has a seat AND organization has active license.
    /// </summary>
    /// <param name="productCode">The product code (e.g., "procohere")</param>
    /// <returns>True if user has active access to the product.</returns>
    public async Task<bool> HasProductAccessAsync(string productCode = "procohere")
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProCohere", "auth.log");
        void Log(string msg) => File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        
        Log($"HasProductAccessAsync called with productCode={productCode}");
        Log($"CurrentUser: {_publicClient?.Auth?.CurrentUser?.Id ?? "NULL"}");
        
        if (_publicClient?.Auth?.CurrentUser == null)
        {
            Log("ERROR: No current user - returning false");
            return false;
        }

        try
        {
            Log($"Calling RPC: user_has_active_product_access(product_code={productCode})");
            
            // Call the helper function we defined in the public schema
            var result = await _publicClient.Rpc("user_has_active_product_access", new { product_code = productCode });
            
            Log($"RPC result type: {result?.GetType().FullName ?? "NULL"}");
            Log($"RPC result Content: {result?.Content ?? "NULL"}");
            
            // BaseResponse has a Content property with the actual JSON result
            if (result?.Content != null)
            {
                var content = result.Content.Trim();
                Log($"Content trimmed: '{content}'");
                
                // The RPC returns just "true" or "false" as JSON
                if (bool.TryParse(content, out var hasAccess))
                {
                    Log($"Parsed as boolean: {hasAccess}");
                    return hasAccess;
                }
                
                // Try parsing as JSON boolean in case it's wrapped
                if (content == "true" || content == "\"true\"")
                {
                    Log("Matched true string");
                    return true;
                }
            }
            
            Log("Could not parse result as boolean - returning false");
            return false;
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log($"Stack: {ex.StackTrace}");
            // On error, deny access to be safe
            return false;
        }
    }

    /// <summary>
    /// Gets the user's role for a specific product (admin, user, viewer).
    /// </summary>
    /// <param name="productCode">The product code (e.g., "procohere")</param>
    /// <returns>The role name, or null if no access.</returns>
    public async Task<string?> GetProductRoleAsync(string productCode = "procohere")
    {
        if (_publicClient?.Auth?.CurrentUser == null)
        {
            return null;
        }

        try
        {
            var result = await _publicClient.Rpc("get_user_product_role", new { product_code = productCode });
            return result?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetProductRoleAsync failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves the signed-in user's session payload including product access, team member, and role data.
    /// This is the primary method to call after authentication to get all session context.
    /// </summary>
    /// <param name="productKey">The product key (e.g., "procohere")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user session DTO with access status, team member, and role info.</returns>
    public async Task<ProCohereUserSessionDto> GetUserSessionAsync(string productKey = "procohere", CancellationToken cancellationToken = default)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProCohere", "auth.log");
        void Log(string msg) => File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] [GetUserSession] {msg}\n");

        Log($"GetUserSessionAsync called with productKey={productKey}");

        if (string.IsNullOrWhiteSpace(productKey))
            throw new ArgumentException("Product key is required.", nameof(productKey));

        if (_publicClient?.Auth?.CurrentUser == null)
        {
            Log("ERROR: No current user");
            return new ProCohereUserSessionDto
            {
                HasAccess = false,
                Error = "Not authenticated"
            };
        }

        try
        {
            Log("Calling RPC: get_user_session (procohere schema)");
            // Use procohere client for procohere schema RPC
            var result = await _procohereClient!.Rpc("get_user_session", new { p_product_key = productKey });
            
            Log($"RPC result Content: {result?.Content ?? "NULL"}");

            if (result?.Content == null)
            {
                Log("ERROR: RPC returned null content");
                return new ProCohereUserSessionDto
                {
                    HasAccess = false,
                    Error = "Session RPC returned no data"
                };
            }

            // Parse the JSON response
            var session = System.Text.Json.JsonSerializer.Deserialize<ProCohereUserSessionDto>(
                result.Content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (session == null)
            {
                Log("ERROR: Failed to deserialize session");
                return new ProCohereUserSessionDto
                {
                    HasAccess = false,
                    Error = "Failed to parse session data"
                };
            }

            Log($"Session parsed: HasAccess={session.HasAccess}, TeamMember={session.TeamMember?.FullName ?? "NULL"}, Role={session.Role?.Name ?? "NULL"}");

            // Store the session for later use
            CurrentSession_ProCohere = session;

            return session;
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return new ProCohereUserSessionDto
            {
                HasAccess = false,
                Error = $"Session lookup failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Clears the stored session data. Called on sign out.
    /// </summary>
    private void ClearSessionData()
    {
        CurrentSession_ProCohere = null;
        CurrentProfile = null;
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
        
        if (_procohereClient == null || CurrentProfile == null)
        {
            LastDataCountError = "Client or profile is null";
            return counts;
        }

        System.Diagnostics.Debug.WriteLine($"GetDataCountsAsync: User ID = {CurrentProfile.Id}");

        // Get team members managed by this user
        try
        {
            var teamMembersResult = await _procohereClient.From<TeamMemberSimple>()
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
            var meetingsResult = await _procohereClient.From<MeetingSimple>()
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
            var goalsResult = await _procohereClient.From<GoalSimple>()
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
            var tasksResult = await _procohereClient.From<TaskSimple>()
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
            var projectsResult = await _procohereClient.From<ProjectSimple>()
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
