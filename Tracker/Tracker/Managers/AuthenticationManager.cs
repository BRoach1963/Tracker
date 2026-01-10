using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Services.Auth;
using Tracker.Services.Licensing;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages authentication state for the application.
    /// Coordinates between PostgreSQL local auth and provides a unified authentication interface.
    /// </summary>
    public class AuthenticationManager
    {
        #region Singleton

        private static readonly Lazy<AuthenticationManager> _instance =
            new(() => new AuthenticationManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AuthenticationManager Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly AuthService _authService;
        private readonly IFirmLicenseService _licenseService;
        private PostgresAuthContextFactory? _authFactory;
        private PostgresDbContextFactory? _userContextFactory;
        private DatabaseSettings? _settings;
        private string _jwtSecret = string.Empty;
        
        // Current firm info (populated after successful sign in)
        private SeatValidationResult? _currentSeatInfo;

        #endregion

        #region Events

        /// <summary>
        /// Fired when authentication state changes (sign in, sign out).
        /// </summary>
        public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Whether a user is currently signed in (either via Supabase or local auth).
        /// </summary>
        public bool IsSignedIn => _authService.IsSignedIn || Services.Backend.SupabaseService.Instance.IsSignedIn;

        /// <summary>
        /// The currently authenticated user.
        /// </summary>
        public AuthenticatedUser? CurrentUser => _authService.CurrentUser;

        /// <summary>
        /// The current user's ID.
        /// </summary>
        public Guid? CurrentUserId => _authService.CurrentUserId ?? 
            (Guid.TryParse(Services.Backend.SupabaseService.Instance.CurrentUser?.Id, out var supabaseId) ? supabaseId : null);

        /// <summary>
        /// Whether PostgreSQL authentication is configured.
        /// </summary>
        public bool IsPostgresConfigured => _settings?.Type == DatabaseType.PostgreSQL;

        /// <summary>
        /// Gets the PostgreSQL context factory for the current user.
        /// Throws if not authenticated.
        /// </summary>
        public PostgresDbContextFactory? UserContextFactory
        {
            get
            {
                if (!IsSignedIn || !CurrentUserId.HasValue || _settings == null)
                    return null;
                
                // Create factory if needed, or recreate if user changed
                if (_userContextFactory == null || _userContextFactory.UserId != CurrentUserId.Value)
                {
                    _userContextFactory?.Dispose();
                    _userContextFactory = new PostgresDbContextFactory(_settings, CurrentUserId.Value);
                }
                
                return _userContextFactory;
            }
        }

        /// <summary>
        /// Gets the current firm information (after successful sign in).
        /// </summary>
        public SeatValidationResult? CurrentSeatInfo => _currentSeatInfo;

        /// <summary>
        /// Gets the current firm name.
        /// </summary>
        public string? CurrentFirmName => _currentSeatInfo?.FirmName;

        /// <summary>
        /// Gets the current subscription tier.
        /// </summary>
        public string? CurrentTier => _currentSeatInfo?.Tier;

        #endregion

        #region Constructor

        private AuthenticationManager()
        {
            _logger = LoggingManager.GetComponentLogger("AuthManager");
            _authService = AuthService.Instance;
            _licenseService = new FirmLicenseService();
            
            // Forward auth events
            _authService.AuthStateChanged += (sender, e) => AuthStateChanged?.Invoke(this, e);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the authentication manager with PostgreSQL settings.
        /// </summary>
        /// <param name="settings">PostgreSQL database settings.</param>
        /// <param name="jwtSecret">Secret key for JWT signing (min 32 characters).</param>
        public void Initialize(DatabaseSettings settings, string jwtSecret)
        {
            if (settings.Type != DatabaseType.PostgreSQL)
            {
                _logger.Warn("AuthenticationManager initialized with non-PostgreSQL settings");
            }

            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
            {
                throw new ArgumentException("JWT secret must be at least 32 characters");
            }

            _settings = settings;
            _jwtSecret = jwtSecret;
            
            if (settings.Type == DatabaseType.PostgreSQL)
            {
                _authFactory = new PostgresAuthContextFactory(settings);
            }

            _logger.Info("AuthenticationManager initialized");
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Sign in with email and password.
        /// Flow: 1) Validate Supabase seat 2) Authenticate locally against PostgreSQL
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <returns>Authentication result with user info and tokens if successful.</returns>
        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            if (_settings?.Type != DatabaseType.PostgreSQL || _authFactory == null)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "PostgreSQL authentication not configured" 
                };
            }

            _logger.Info("Sign in attempt: {0}", email);

            // Step 1: Validate Supabase license seat
            _logger.Info("Checking license seat for: {0}", email);
            var seatResult = await _licenseService.ValidateSeatAsync(email, "tracker");
            
            if (!seatResult.IsValid)
            {
                _logger.Warn("License check failed for {0}: {1}", email, seatResult.ErrorMessage);
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = seatResult.ErrorMessage ?? "No valid license found for this email" 
                };
            }

            _logger.Info("License valid for {0}: Firm={1}, Tier={2}", 
                email, seatResult.FirmName, seatResult.Tier);

            // Step 2: Authenticate locally against PostgreSQL
            var result = await _authService.LoginAsync(
                email,
                password,
                async (e) => await _authFactory.LookupUserByEmailAsync(e),
                _jwtSecret);

            if (result.Success && result.User != null)
            {
                // Store seat info
                _currentSeatInfo = seatResult;
                
                // Update last login timestamp
                await _authFactory.UpdateLastLoginAsync(result.User.Id);
                
                // Create user context factory
                _userContextFactory?.Dispose();
                _userContextFactory = new PostgresDbContextFactory(_settings, result.User.Id);
                
                _logger.Info("Sign in successful: {0} (Firm: {1})", result.User.Email, seatResult.FirmName);
            }
            else
            {
                _logger.Warn("Sign in failed: {0} - {1}", email, result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Create a new account.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password (min 8 characters).</param>
        /// <param name="displayName">Optional display name.</param>
        /// <returns>Authentication result with user info and tokens if successful.</returns>
        public async Task<AuthResult> SignUpAsync(string email, string password, string? displayName = null)
        {
            if (_settings?.Type != DatabaseType.PostgreSQL || _authFactory == null)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "PostgreSQL authentication not configured" 
                };
            }

            _logger.Info("Sign up attempt: {0}", email);

            var result = await _authService.RegisterAsync(
                email,
                password,
                displayName,
                async (e, hash, name) => await _authFactory.CreateUserAsync(e, hash, name),
                _jwtSecret);

            if (result.Success && result.User != null)
            {
                // Create user context factory
                _userContextFactory?.Dispose();
                _userContextFactory = new PostgresDbContextFactory(_settings, result.User.Id);
                
                _logger.Info("Sign up successful: {0}", result.User.Email);
            }
            else
            {
                _logger.Warn("Sign up failed: {0} - {1}", email, result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        public void SignOut()
        {
            var email = CurrentUser?.Email ?? "unknown";
            
            _authService.SignOut();
            _userContextFactory?.Dispose();
            _userContextFactory = null;
            
            _logger.Info("User signed out: {0}", email);
        }

        /// <summary>
        /// Try to restore a saved session.
        /// </summary>
        /// <param name="accessToken">Saved access token.</param>
        /// <param name="refreshToken">Saved refresh token.</param>
        /// <returns>True if session was restored successfully.</returns>
        public async Task<bool> TryRestoreSessionAsync(string? accessToken, string? refreshToken)
        {
            if (_settings?.Type != DatabaseType.PostgreSQL || _authFactory == null)
            {
                return false;
            }

            var success = await _authService.TryRestoreSessionAsync(
                accessToken,
                refreshToken,
                _jwtSecret,
                async (userId) => await _authFactory.GetUserByIdAsync(userId));

            if (success && CurrentUserId.HasValue)
            {
                _userContextFactory?.Dispose();
                _userContextFactory = new PostgresDbContextFactory(_settings, CurrentUserId.Value);
                _logger.Info("Session restored for: {0}", CurrentUser?.Email);
            }

            return success;
        }

        /// <summary>
        /// Test the PostgreSQL connection.
        /// </summary>
        /// <returns>True if connection succeeds, false otherwise.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            if (_settings?.Type != DatabaseType.PostgreSQL || _authFactory == null)
            {
                return false;
            }

            try
            {
                using var context = _authFactory.CreateContext();
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Connection test failed");
                return false;
            }
        }

        /// <summary>
        /// Create a DbContext for the current authenticated user.
        /// </summary>
        /// <returns>A TrackerDbContext with RLS configured, or null if not authenticated.</returns>
        public TrackerDbContext? CreateUserContext()
        {
            return UserContextFactory?.CreateContext();
        }

        #endregion
    }
}
