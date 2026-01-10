using System.IO;
using System.Security.Cryptography;
using System.Text;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Gotrue.Interfaces;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend.Models;

using AuthState = Supabase.Gotrue.Constants.AuthState;

namespace Tracker.Services.Backend
{
    /// <summary>
    /// Service for interacting with Supabase backend.
    /// Handles authentication, profile management, and subscription status.
    /// </summary>
    public class SupabaseService
    {
        #region Singleton

        private static readonly Lazy<SupabaseService> _instance =
            new(() => new SupabaseService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SupabaseService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private Supabase.Client? _client;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Fired when authentication state changes.
        /// </summary>
        public event EventHandler<AuthState>? AuthStateChanged;

        /// <summary>
        /// Fired when user profile is updated.
        /// </summary>
        public event EventHandler<UserProfile?>? ProfileChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the service is initialized and ready.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Whether a user is currently signed in.
        /// </summary>
        public bool IsSignedIn => _client?.Auth.CurrentUser != null;

        /// <summary>
        /// The current Supabase user (from auth).
        /// </summary>
        public User? CurrentUser => _client?.Auth.CurrentUser;

        /// <summary>
        /// The current user's profile (from database).
        /// </summary>
        public UserProfile? CurrentProfile { get; private set; }

        /// <summary>
        /// The current user's subscription.
        /// </summary>
        public UserSubscription? CurrentSubscription { get; private set; }

        /// <summary>
        /// The current session access token.
        /// </summary>
        public string? AccessToken => _client?.Auth.CurrentSession?.AccessToken;

        #endregion

        #region Constructor

        private SupabaseService()
        {
            _logger = LoggingManager.GetComponentLogger("Supabase");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the Supabase client.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _logger.Info("Initializing Supabase client...");

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false // We don't need realtime for now
                };

                _client = new Supabase.Client(
                    SupabaseConfig.ProjectUrl,
                    SupabaseConfig.AnonKey,
                    options);

                await _client.InitializeAsync();

                // Listen for auth state changes
                _client.Auth.AddStateChangedListener(OnAuthStateChanged);

                _isInitialized = true;
                _logger.Info("Supabase client initialized successfully");

                // Try to restore session from stored token
                await TryRestoreSessionAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize Supabase client");
                throw;
            }
        }

        private void OnAuthStateChanged(object sender, AuthState state)
        {
            _logger.Info("Auth state changed: {0}", state);
            AuthStateChanged?.Invoke(this, state);

            if (state == AuthState.SignedOut)
            {
                CurrentProfile = null;
                CurrentSubscription = null;
                ProfileChanged?.Invoke(this, null);
            }
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Signs up a new user with email and password.
        /// Note: Caller must call UserSettingsManager.Instance.SwitchToUser() after successful signup
        /// to initialize user-specific settings.
        /// </summary>
        public async Task<(bool Success, string? Error)> SignUpAsync(
            string email, 
            string password, 
            string? displayName = null)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Signing up user: {0}", email);

                var session = await _client!.Auth.SignUp(email, password, new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        ["display_name"] = displayName ?? email.Split('@')[0]
                    }
                });

                if (session?.User != null)
                {
                    _logger.Info("User signed up successfully: {0}", session.User.Id);
                    
                    // Clear any previous user's data to ensure fresh state
                    CurrentProfile = null;
                    CurrentSubscription = null;
                    
                    await SaveSessionAsync();
                    await LoadUserDataAsync();
                    
                    // If no subscription exists for new user, create a fresh free subscription
                    if (CurrentSubscription == null)
                    {
                        await CreateFreeSubscriptionAsync(session.User.Id);
                    }
                    
                    return (true, null);
                }

                return (false, "Sign up failed. Please try again.");
            }
            catch (GotrueException ex)
            {
                _logger.Warn("Sign up failed: {0}", ex.Message);
                return (false, GetFriendlyAuthError(ex));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Sign up error");
                return (false, "An unexpected error occurred. Please try again.");
            }
        }

        /// <summary>
        /// Signs in a user with email and password.
        /// Note: Caller must call UserSettingsManager.Instance.SwitchToUser() after successful sign-in
        /// to load user-specific settings.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <param name="persistSession">If true, saves session tokens for automatic login on next launch.</param>
        public async Task<(bool Success, string? Error)> SignInAsync(string email, string password, bool persistSession = false)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("=== SIGN IN ATTEMPT ===");
                _logger.Info("Email: {0}", email);
                _logger.Info("Password length: {0} chars", password?.Length ?? 0);
                _logger.Info("Persist session: {0}", persistSession);
                _logger.Info("Supabase URL: {0}", SupabaseConfig.ProjectUrl);
                _logger.Info("Client initialized: {0}", _client != null);

                var session = await _client!.Auth.SignIn(email, password);

                _logger.Info("SignIn call completed");
                _logger.Info("Session null: {0}", session == null);
                _logger.Info("Session.User null: {0}", session?.User == null);

                if (session?.User != null)
                {
                    _logger.Info("SUCCESS! User ID: {0}", session.User.Id);
                    _logger.Info("User email: {0}", session.User.Email);
                    
                    try
                    {
                        _logger.Info("Starting post-login processing...");
                        
                        // Only save session tokens if user opted in to "Keep me signed in"
                        if (persistSession)
                        {
                            _logger.Info("  - SaveSessionAsync (user opted in)...");
                            await SaveSessionAsync();
                        }
                        else
                        {
                            _logger.Info("  - Skipping session save (user did not opt in)");
                            ClearStoredSession(); // Clear any previous tokens
                        }
                        
                        _logger.Info("  - LoadUserDataAsync...");
                        await LoadUserDataAsync();
                        
                        _logger.Info("  - UpdateLastLoginAsync...");
                        await UpdateLastLoginAsync();
                        
                        _logger.Info("  - RegisterInstallationAsync...");
                        await RegisterInstallationAsync();
                        
                        _logger.Info("Post-login processing complete!");
                    }
                    catch (Exception postLoginEx)
                    {
                        _logger.Warn("Post-login processing failed: {0}", postLoginEx.Message);
                        _logger.Warn("Stack trace: {0}", postLoginEx.StackTrace);
                        // Don't fail the login - user is authenticated, just log the issue
                    }
                    
                    return (true, null);
                }

                _logger.Warn("Sign in returned null session for: {0}", email);
                return (false, "Sign in failed. Please check your credentials.");
            }
            catch (GotrueException ex)
            {
                _logger.Warn("=== GOTRUE EXCEPTION ===");
                _logger.Warn("Type: {0}", ex.GetType().FullName);
                _logger.Warn("Message: {0}", ex.Message);
                _logger.Warn("Stack: {0}", ex.StackTrace);
                return (false, GetFriendlyAuthError(ex));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "=== GENERAL EXCEPTION ===");
                _logger.Warn("Type: {0}", ex.GetType().FullName);
                _logger.Warn("Message: {0}", ex.Message);
                _logger.Warn("Stack: {0}", ex.StackTrace);
                if (ex.InnerException != null)
                {
                    _logger.Warn("Inner: {0} - {1}", ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                return (false, $"Sign in failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Signs out the current user.
        /// Note: Caller must call UserSettingsManager.Instance.ResetToAnonymous() after sign out
        /// to reset settings to anonymous mode.
        /// </summary>
        public async Task SignOutAsync()
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Signing out user");
                await _client!.Auth.SignOut();
                ClearStoredSession();
                ClearKeepMeSignedInPreference(); // Clear the preference so user must opt-in again
                CurrentProfile = null;
                CurrentSubscription = null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Sign out error");
            }
        }

        /// <summary>
        /// Sends a password reset email.
        /// </summary>
        public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Sending password reset email to: {0}", email);
                
                // The redirect URL is configured in Supabase Dashboard:
                // Authentication -> Email Templates -> Reset Password
                // Uses the redirect URL: https://www.pricklycactussoftware.com/password-reset
                var options = new Supabase.Gotrue.ResetPasswordForEmailOptions(email)
                {
                    RedirectTo = "https://www.pricklycactussoftware.com/password-reset"
                };
                
                await _client!.Auth.ResetPasswordForEmail(options);
                return (true, null);
            }
            catch (GotrueException ex)
            {
                _logger.Warn("Password reset failed: {0}", ex.Message);
                return (false, GetFriendlyAuthError(ex));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Password reset error");
                return (false, "Failed to send reset email. Please try again.");
            }
        }

        /// <summary>
        /// Updates the current user's email address.
        /// Supabase will send a confirmation email to the new address.
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateEmailAsync(string newEmail)
        {
            EnsureInitialized();

            if (CurrentUser == null)
                return (false, "Not signed in");

            try
            {
                _logger.Info("Updating email for user {0} to {1}", CurrentUser.Id, newEmail);

                var attrs = new UserAttributes { Email = newEmail };
                await _client!.Auth.Update(attrs);

                return (true, null);
            }
            catch (GotrueException ex)
            {
                _logger.Warn("Email update failed: {0}", ex.Message);
                return (false, GetFriendlyAuthError(ex));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Email update error");
                return (false, "Failed to update email. Please try again.");
            }
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Loads the current user's profile and subscription from the database.
        /// </summary>
        public async Task LoadUserDataAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                _logger.Debug("Loading user data for auth id: {0}", CurrentUser.Id);

                // Load profile from users table - match by supabase_auth_id
                var profileResult = await _client!.From<UserProfile>()
                    .Where(p => p.SupabaseAuthId == Guid.Parse(CurrentUser.Id))
                    .Single();

                CurrentProfile = profileResult;
                _logger.Info("User profile loaded: {0}", CurrentProfile?.DisplayName ?? "null");

                // Load subscription
                var subResult = await _client.From<UserSubscription>()
                    .Where(s => s.UserId == CurrentUser.Id)
                    .Single();

                CurrentSubscription = subResult;

                _logger.Info("User data loaded. Tier: {0}", CurrentSubscription?.TierString ?? "unknown");
                ProfileChanged?.Invoke(this, CurrentProfile);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load user data");
            }
        }

        /// <summary>
        /// Updates the current user's profile.
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateProfileAsync(UserProfile profile)
        {
            if (CurrentUser == null) return (false, "Not signed in");

            try
            {
                _logger.Info("Updating profile for auth id: {0}", CurrentUser.Id);

                profile.UpdatedAt = DateTime.UtcNow;

                await _client!.From<UserProfile>()
                    .Where(p => p.SupabaseAuthId == Guid.Parse(CurrentUser.Id))
                    .Update(profile);

                CurrentProfile = profile;
                ProfileChanged?.Invoke(this, profile);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to update profile");
                return (false, "Failed to update profile. Please try again.");
            }
        }

        /// <summary>
        /// Updates the user's last login timestamp.
        /// </summary>
        private async Task UpdateLastLoginAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                await _client!.From<UserProfile>()
                    .Where(p => p.SupabaseAuthId == Guid.Parse(CurrentUser.Id))
                    .Set(p => p.LastLoginAt!, DateTime.UtcNow)
                    .Update();
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to update last login: {0}", ex.Message);
            }
        }

        #endregion

        #region Installation Tracking

        /// <summary>
        /// Registers this app installation with the backend.
        /// </summary>
        public async Task RegisterInstallationAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                var deviceId = GetDeviceId();
                var deviceName = Environment.MachineName;
                var osVersion = Environment.OSVersion.ToString();
                var appVersion = VersionHelper.GetVersion();

                _logger.Debug("Registering installation: {0}", deviceId);

                // Check if installation exists
                var existing = await _client!.From<AppInstallation>()
                    .Where(i => i.UserId == CurrentUser.Id && i.DeviceId == deviceId)
                    .Single();

                if (existing != null)
                {
                    // Update existing
                    existing.LastSeenAt = DateTime.UtcNow;
                    existing.AppVersion = appVersion;
                    existing.IsActive = true;

                    await _client.From<AppInstallation>()
                        .Where(i => i.Id == existing.Id)
                        .Update(existing);
                }
                else
                {
                    // Create new
                    var installation = new AppInstallation
                    {
                        UserId = CurrentUser.Id!,
                        DeviceId = deviceId,
                        DeviceName = deviceName,
                        OsVersion = osVersion,
                        AppVersion = appVersion,
                        ActivatedAt = DateTime.UtcNow,
                        LastSeenAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _client.From<AppInstallation>().Insert(installation);
                }

                _logger.Info("Installation registered successfully");
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to register installation: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Gets a unique device identifier (hashed for privacy).
        /// </summary>
        private static string GetDeviceId()
        {
            // Combine machine name and user for a semi-unique ID
            var raw = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
            
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(hash)[..32];
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Tries to restore a session from stored credentials.
        /// This enables "Keep me signed in" - if a user has opted in and has stored tokens,
        /// they will be automatically signed in on subsequent launches.
        /// </summary>
        private async Task TryRestoreSessionAsync()
        {
            try
            {
                // Check if user opted in to "Keep me signed in"
                if (!IsKeepMeSignedInEnabled())
                {
                    _logger.Info("User has not opted in to 'Keep me signed in' - login required");
                    ClearStoredSession(); // Clear any stale tokens
                    return;
                }
                
                var storedRefreshToken = SecureTokenStorage.GetRefreshToken();
                var storedAccessToken = SecureTokenStorage.GetAccessToken();
                
                if (string.IsNullOrEmpty(storedRefreshToken))
                {
                    _logger.Info("No stored refresh token found - login required");
                    return;
                }

                _logger.Info("Found stored tokens and user opted in - attempting session restore...");

                Supabase.Gotrue.Session? session = null;
                
                // Try to restore with both tokens if available
                if (!string.IsNullOrEmpty(storedAccessToken))
                {
                    _logger.Debug("Restoring with access + refresh tokens");
                    try
                    {
                        session = await _client!.Auth.SetSession(storedAccessToken, storedRefreshToken);
                    }
                    catch (Exception setEx)
                    {
                        _logger.Debug("SetSession failed, will try refresh: {0}", setEx.Message);
                    }
                }
                
                // If SetSession didn't work, try refreshing
                if (session?.User == null)
                {
                    _logger.Debug("Trying RefreshSession...");
                    try
                    {
                        // First set just the refresh token, then refresh
                        session = await _client!.Auth.RefreshSession();
                    }
                    catch (Exception refEx)
                    {
                        _logger.Debug("RefreshSession failed: {0}", refEx.Message);
                    }
                }
                
                if (session?.User != null)
                {
                    _logger.Info("Session restored successfully!");
                    _logger.Info("  User: {0}", session.User.Email);
                    _logger.Info("  ID: {0}", session.User.Id);
                    
                    // Switch to user-specific settings
                    UserSettingsManager.Instance.SwitchToUser(session.User.Id, isNewAccount: false);
                    
                    // Save the new tokens (they may have been refreshed)
                    if (!string.IsNullOrEmpty(session.RefreshToken))
                    {
                        SecureTokenStorage.SaveRefreshToken(session.RefreshToken);
                    }
                    if (!string.IsNullOrEmpty(session.AccessToken))
                    {
                        SecureTokenStorage.SaveAccessToken(session.AccessToken);
                    }
                    
                    try
                    {
                        await LoadUserDataAsync();
                        await RegisterInstallationAsync();
                    }
                    catch (Exception postEx)
                    {
                        _logger.Warn("Post-restore processing failed: {0}", postEx.Message);
                        // Don't fail the restore - user is authenticated
                    }
                }
                else
                {
                    _logger.Warn("Session restore returned null session - clearing stored tokens");
                    ClearStoredSession();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to restore session: {0}", ex.Message);
                _logger.Debug("Stack: {0}", ex.StackTrace);
                ClearStoredSession();
            }
        }

        /// <summary>
        /// Saves the current session for later restoration.
        /// This enables "Windows SSO" by storing tokens encrypted for the current Windows user.
        /// </summary>
        private async Task SaveSessionAsync()
        {
            try
            {
                var session = _client?.Auth.CurrentSession;
                if (session != null)
                {
                    // Save both tokens for proper session restoration
                    if (!string.IsNullOrEmpty(session.RefreshToken))
                    {
                        SecureTokenStorage.SaveRefreshToken(session.RefreshToken);
                        _logger.Debug("Refresh token saved");
                    }
                    if (!string.IsNullOrEmpty(session.AccessToken))
                    {
                        SecureTokenStorage.SaveAccessToken(session.AccessToken);
                        _logger.Debug("Access token saved");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to save session: {0}", ex.Message);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears stored session data.
        /// </summary>
        private void ClearStoredSession()
        {
            SecureTokenStorage.ClearTokens();
            _logger.Debug("Stored session cleared");
        }

        /// <summary>
        /// Checks if the user has opted in to "Keep me signed in".
        /// This preference is stored per Windows user.
        /// </summary>
        private bool IsKeepMeSignedInEnabled()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var settingsPath = Path.Combine(appData, "Tracker", "keep_signed_in.txt");
                
                if (File.Exists(settingsPath))
                {
                    var content = File.ReadAllText(settingsPath);
                    return content.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("Failed to read keep-me-signed-in preference: {0}", ex.Message);
            }
            
            return false; // Default to not keeping signed in
        }

        /// <summary>
        /// Clears the "Keep me signed in" preference file.
        /// Called during sign-out so user must opt-in again on next login.
        /// </summary>
        private void ClearKeepMeSignedInPreference()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var settingsPath = Path.Combine(appData, "Tracker", "keep_signed_in.txt");
                
                if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                    _logger.Debug("Cleared keep-me-signed-in preference");
                }
            }
            catch (Exception ex)
            {
                _logger.Debug("Failed to clear keep-me-signed-in preference: {0}", ex.Message);
            }
        }

        #endregion

        #region Avatar Upload

        /// <summary>
        /// Uploads an avatar image from a file path.
        /// </summary>
        public async Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return (false, "File not found", null);

            try
            {
                var imageData = await File.ReadAllBytesAsync(filePath);
                return await UploadAvatarAsync(imageData);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to read avatar file: {0}", filePath);
                return (false, "Failed to read image file.", null);
            }
        }

        /// <summary>
        /// Uploads an avatar image for the current user.
        /// </summary>
        public async Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(byte[] imageData)
        {
            if (CurrentUser == null) return (false, "Not signed in", null);

            try
            {
                _logger.Info("UploadAvatarAsync called. Image size: {0} bytes", imageData.Length);
                
                if (imageData.Length > SupabaseConfig.MaxAvatarSizeBytes)
                {
                    _logger.Warn("Avatar image too large: {0} bytes (max: {1})", imageData.Length, SupabaseConfig.MaxAvatarSizeBytes);
                    return (false, "Image is too large. Maximum size is 500KB.", null);
                }

                var fileName = $"{CurrentUser.Id}/avatar.jpg";

                _logger.Info("Uploading avatar to: {0}/{1}", SupabaseConfig.AvatarBucket, fileName);

                await _client!.Storage
                    .From(SupabaseConfig.AvatarBucket)
                    .Upload(imageData, fileName, new Supabase.Storage.FileOptions
                    {
                        ContentType = "image/jpeg",
                        Upsert = true
                    });
                
                _logger.Info("Avatar uploaded successfully to storage");

                // Update profile with avatar URL using targeted update
                if (CurrentProfile != null && CurrentUser != null)
                {
                    try
                    {
                        _logger.Info("Updating avatar_url in database to: {0}", fileName);
                        
                        // Do a targeted update of just the avatar_url field
                        await _client!.From<UserProfile>()
                            .Where(p => p.SupabaseAuthId == Guid.Parse(CurrentUser.Id))
                            .Set(p => p.AvatarUrl!, fileName)
                            .Set(p => p.UpdatedAt, DateTime.UtcNow)
                            .Update();
                        
                        // Update local copy
                        CurrentProfile.AvatarUrl = fileName;
                        _logger.Info("Profile avatar_url updated successfully");
                    }
                    catch (Exception updateEx)
                    {
                        _logger.Exception(updateEx, "Failed to update avatar_url in database");
                        // Still return success since the file uploaded - avatar is accessible via URL
                    }
                }
                else
                {
                    _logger.Warn("CurrentProfile or CurrentUser is null, cannot update avatar URL in database");
                }

                var fullUrl = $"{SupabaseConfig.ProjectUrl}/storage/v1/object/public/{SupabaseConfig.AvatarBucket}/{fileName}";
                _logger.Info("Avatar full URL: {0}", fullUrl);
                return (true, null, fullUrl);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to upload avatar");
                return (false, $"Failed to upload avatar: {ex.Message}", null);
            }
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Updates the current user's subscription.
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateSubscriptionAsync(UserSubscription subscription)
        {
            if (CurrentUser == null) return (false, "Not signed in");

            try
            {
                _logger.Info("Updating subscription for user: {0}", CurrentUser.Id);

                subscription.UpdatedAt = DateTime.UtcNow;

                await _client!.From<UserSubscription>()
                    .Where(s => s.UserId == CurrentUser.Id)
                    .Update(subscription);

                CurrentSubscription = subscription;
                
                // Notify listeners that subscription changed
                SubscriptionChanged?.Invoke(this, subscription);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to update subscription");
                return (false, "Failed to update subscription. Please try again.");
            }
        }

        /// <summary>
        /// Logs a subscription event for auditing.
        /// </summary>
        public async Task LogSubscriptionEventAsync(string eventType, Dictionary<string, object>? eventData = null)
        {
            if (CurrentUser == null || CurrentSubscription == null) return;

            try
            {
                var eventDataJson = eventData != null 
                    ? System.Text.Json.JsonSerializer.Serialize(eventData) 
                    : "{}";

                // Insert into subscription_events table via RPC or direct insert
                // For now, we'll log locally - full implementation would insert to Supabase
                _logger.Info("Subscription event: {0} - {1}", eventType, eventDataJson);

                // TODO: When subscription_events table is ready, insert there
                // await _client!.From<SubscriptionEvent>().Insert(new SubscriptionEvent { ... });
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to log subscription event: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Fired when subscription is updated.
        /// </summary>
        public event EventHandler<UserSubscription?>? SubscriptionChanged;

        /// <summary>
        /// Creates a fresh free subscription for a new user.
        /// </summary>
        private async Task CreateFreeSubscriptionAsync(string userId)
        {
            try
            {
                _logger.Info("Creating free subscription for new user: {0}", userId);

                var now = DateTime.UtcNow;
                var newSubscription = new UserSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    TierString = "free",
                    StatusString = "active",
                    BillingCadence = "monthly",
                    PriceCents = 0,
                    OriginalPriceCents = 0,
                    DiscountPercent = 0,
                    AiRequestsThisMonth = 0,
                    AiBudgetUsedCents = 0,
                    UsageResetAt = now.AddMonths(1),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _client!.From<UserSubscription>().Insert(newSubscription);
                CurrentSubscription = newSubscription;

                _logger.Info("Free subscription created successfully for user: {0}", userId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to create free subscription for new user");
            }
        }

        #endregion

        #region AI Usage Tracking

        /// <summary>
        /// Records AI usage for billing/budget tracking.
        /// </summary>
        public async Task RecordAiUsageAsync(int requestCount, int budgetCents)
        {
            if (CurrentUser == null || CurrentSubscription == null) return;

            try
            {
                CurrentSubscription.AiRequestsThisMonth += requestCount;
                CurrentSubscription.AiBudgetUsedCents += budgetCents;

                await _client!.From<UserSubscription>()
                    .Where(s => s.UserId == CurrentUser.Id)
                    .Set(s => s.AiRequestsThisMonth, CurrentSubscription.AiRequestsThisMonth)
                    .Set(s => s.AiBudgetUsedCents, CurrentSubscription.AiBudgetUsedCents)
                    .Update();
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to record AI usage: {0}", ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Supabase service not initialized. Call InitializeAsync first.");
            }
        }

        private static string GetFriendlyAuthError(GotrueException ex)
        {
            var message = ex.Message.ToLower();

            if (message.Contains("invalid login"))
                return "Invalid email or password. Please try again.";
            
            if (message.Contains("email not confirmed"))
                return "Please check your email and confirm your account.";
            
            if (message.Contains("user already registered"))
                return "An account with this email already exists. Try signing in instead.";
            
            if (message.Contains("password"))
                return "Password must be at least 6 characters.";
            
            if (message.Contains("rate limit"))
                return "Too many attempts. Please wait a moment and try again.";

            return ex.Message;
        }

        #endregion
    }
}

