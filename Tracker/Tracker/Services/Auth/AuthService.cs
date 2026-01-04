using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tracker.Logging;

namespace Tracker.Services.Auth
{
    /// <summary>
    /// Result of an authentication operation.
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public AuthenticatedUser? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Represents an authenticated user.
    /// </summary>
    public class AuthenticatedUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Authentication service for PostgreSQL-based auth.
    /// Replaces Supabase auth with self-contained JWT authentication.
    /// </summary>
    public class AuthService
    {
        #region Singleton

        private static readonly Lazy<AuthService> _instance =
            new(() => new AuthService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AuthService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private AuthenticatedUser? _currentUser;
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime? _tokenExpiresAt;

        // JWT configuration - in production, load from secure config
        private const int AccessTokenExpirationMinutes = 60; // 1 hour
        private const int RefreshTokenExpirationDays = 7;
        private const int BcryptWorkFactor = 12;

        #endregion

        #region Events

        /// <summary>
        /// Fired when authentication state changes.
        /// </summary>
        public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Whether a user is currently signed in.
        /// </summary>
        public bool IsSignedIn => _currentUser != null && _accessToken != null && _tokenExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// The currently authenticated user.
        /// </summary>
        public AuthenticatedUser? CurrentUser => _currentUser;

        /// <summary>
        /// The current user's ID (for database queries).
        /// </summary>
        public Guid? CurrentUserId => _currentUser?.Id;

        /// <summary>
        /// The current access token.
        /// </summary>
        public string? AccessToken => _accessToken;

        #endregion

        #region Constructor

        private AuthService()
        {
            _logger = LoggingManager.GetComponentLogger("Auth");
        }

        #endregion

        #region Password Hashing

        /// <summary>
        /// Hash a password using BCrypt.
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
        }

        /// <summary>
        /// Verify a password against a BCrypt hash.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Password verification failed");
                return false;
            }
        }

        #endregion

        #region JWT Token Generation

        /// <summary>
        /// Generate a JWT access token for a user.
        /// </summary>
        private string GenerateAccessToken(AuthenticatedUser user, string jwtSecret)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.DisplayName ?? user.Email),
                new("user_id", user.Id.ToString()) // Custom claim for easy access
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generate a refresh token.
        /// </summary>
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Validate a JWT token and extract claims.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token, string jwtSecret)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Token validation failed");
                return null;
            }
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Login with email and password.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <param name="dbLookupFunc">Function to lookup user by email from database.</param>
        /// <param name="jwtSecret">Secret key for JWT signing.</param>
        /// <returns>Authentication result with tokens if successful.</returns>
        public async Task<AuthResult> LoginAsync(
            string email, 
            string password,
            Func<string, Task<(Guid? id, string? email, string? displayName, string? passwordHash)?>> dbLookupFunc,
            string jwtSecret)
        {
            try
            {
                _logger.Info("Login attempt for: {0}", email);

                // Look up user in database
                var userRecord = await dbLookupFunc(email);
                
                if (userRecord == null || !userRecord.Value.id.HasValue)
                {
                    _logger.Warn("Login failed: User not found - {0}", email);
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };
                }

                // Verify password
                if (string.IsNullOrEmpty(userRecord.Value.passwordHash) || 
                    !VerifyPassword(password, userRecord.Value.passwordHash))
                {
                    _logger.Warn("Login failed: Invalid password - {0}", email);
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };
                }

                // Create authenticated user
                var user = new AuthenticatedUser
                {
                    Id = userRecord.Value.id.Value,
                    Email = userRecord.Value.email ?? email,
                    DisplayName = userRecord.Value.displayName,
                    LastLoginAt = DateTime.UtcNow
                };

                // Generate tokens
                var accessToken = GenerateAccessToken(user, jwtSecret);
                var refreshToken = GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes);

                // Store in memory
                _currentUser = user;
                _accessToken = accessToken;
                _refreshToken = refreshToken;
                _tokenExpiresAt = expiresAt;

                _logger.Info("Login successful: {0} (ID: {1})", email, user.Id);

                // Fire event
                AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(AuthState.SignedIn, user));

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Login failed with exception");
                return new AuthResult { Success = false, ErrorMessage = "An error occurred during login" };
            }
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="password">User's password.</param>
        /// <param name="displayName">User's display name.</param>
        /// <param name="dbCreateFunc">Function to create user in database. Returns created user ID or null on failure.</param>
        /// <param name="jwtSecret">Secret key for JWT signing.</param>
        /// <returns>Authentication result with tokens if successful.</returns>
        public async Task<AuthResult> RegisterAsync(
            string email,
            string password,
            string? displayName,
            Func<string, string, string?, Task<Guid?>> dbCreateFunc,
            string jwtSecret)
        {
            try
            {
                _logger.Info("Registration attempt for: {0}", email);

                // Validate inputs
                if (string.IsNullOrWhiteSpace(email))
                {
                    return new AuthResult { Success = false, ErrorMessage = "Email is required" };
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                {
                    return new AuthResult { Success = false, ErrorMessage = "Password must be at least 8 characters" };
                }

                // Hash password
                var passwordHash = HashPassword(password);

                // Create user in database
                var userId = await dbCreateFunc(email, passwordHash, displayName);

                if (!userId.HasValue)
                {
                    _logger.Warn("Registration failed: Could not create user - {0}", email);
                    return new AuthResult { Success = false, ErrorMessage = "Email already in use or registration failed" };
                }

                // Create authenticated user
                var user = new AuthenticatedUser
                {
                    Id = userId.Value,
                    Email = email,
                    DisplayName = displayName,
                    LastLoginAt = DateTime.UtcNow
                };

                // Generate tokens
                var accessToken = GenerateAccessToken(user, jwtSecret);
                var refreshToken = GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes);

                // Store in memory
                _currentUser = user;
                _accessToken = accessToken;
                _refreshToken = refreshToken;
                _tokenExpiresAt = expiresAt;

                _logger.Info("Registration successful: {0} (ID: {1})", email, userId.Value);

                // Fire event
                AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(AuthState.SignedIn, user));

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Registration failed with exception");
                return new AuthResult { Success = false, ErrorMessage = "An error occurred during registration" };
            }
        }

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        public void SignOut()
        {
            var previousUser = _currentUser;
            
            _currentUser = null;
            _accessToken = null;
            _refreshToken = null;
            _tokenExpiresAt = null;

            _logger.Info("User signed out: {0}", previousUser?.Email ?? "unknown");

            // Fire event
            AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(AuthState.SignedOut, null));
        }

        /// <summary>
        /// Restore session from stored tokens.
        /// </summary>
        public async Task<bool> TryRestoreSessionAsync(
            string? storedAccessToken,
            string? storedRefreshToken,
            string jwtSecret,
            Func<Guid, Task<AuthenticatedUser?>> dbGetUserFunc)
        {
            if (string.IsNullOrEmpty(storedAccessToken))
            {
                return false;
            }

            try
            {
                var principal = ValidateToken(storedAccessToken, jwtSecret);
                if (principal == null)
                {
                    // Token expired or invalid - could try refresh token here
                    return false;
                }

                // Extract user ID from token
                var userIdClaim = principal.FindFirst("user_id") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return false;
                }

                // Get user from database to ensure they still exist and are active
                var user = await dbGetUserFunc(userId);
                if (user == null)
                {
                    return false;
                }

                // Restore session
                _currentUser = user;
                _accessToken = storedAccessToken;
                _refreshToken = storedRefreshToken;
                // TODO: Extract actual expiration from token
                _tokenExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes);

                _logger.Info("Session restored for: {0}", user.Email);

                // Fire event
                AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs(AuthState.SignedIn, user));

                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to restore session");
                return false;
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Authentication state.
    /// </summary>
    public enum AuthState
    {
        SignedOut,
        SignedIn,
        TokenRefreshed
    }

    /// <summary>
    /// Event args for auth state changes.
    /// </summary>
    public class AuthStateChangedEventArgs : EventArgs
    {
        public AuthState State { get; }
        public AuthenticatedUser? User { get; }

        public AuthStateChangedEventArgs(AuthState state, AuthenticatedUser? user)
        {
            State = state;
            User = user;
        }
    }

    #endregion
}
