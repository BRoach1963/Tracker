using Tracker.Logging;

namespace Tracker.Services
{
    /// <summary>
    /// Provides current organization and user context for the application.
    /// This is used to scope data access in multi-tenant scenarios.
    /// 
    /// The context is typically set during login based on the Supabase user's
    /// organization membership.
    /// 
    /// Usage:
    /// <code>
    /// // At login
    /// OrganizationContext.Current.SetContext(orgId, userId, userEmail);
    /// 
    /// // Throughout application
    /// if (OrganizationContext.Current.HasContext)
    /// {
    ///     var orgId = OrganizationContext.Current.OrganizationId;
    ///     var store = await VectorStoreFactory.CreateAsync(settings, orgId);
    /// }
    /// </code>
    /// </summary>
    public class OrganizationContext
    {
        private static readonly Lazy<OrganizationContext> _instance = 
            new(() => new OrganizationContext());

        private readonly ILogger _logger;
        private Guid? _organizationId;
        private Guid? _userId;
        private string? _userEmail;
        private string? _organizationName;
        private bool _isAdmin;

        /// <summary>
        /// Gets the singleton instance of OrganizationContext.
        /// </summary>
        public static OrganizationContext Current => _instance.Value;

        private OrganizationContext()
        {
            _logger = LoggingManager.GetComponentLogger("OrganizationContext");
        }

        /// <summary>
        /// Event raised when the organization context changes.
        /// </summary>
        public event EventHandler? ContextChanged;

        #region Properties

        /// <summary>
        /// Gets whether an organization context has been set.
        /// </summary>
        public bool HasContext => _organizationId.HasValue;

        /// <summary>
        /// Gets the current organization ID.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no context is set.</exception>
        public Guid OrganizationId => _organizationId 
            ?? throw new InvalidOperationException("Organization context not set. User must be logged in.");

        /// <summary>
        /// Gets the current organization ID, or null if not set.
        /// </summary>
        public Guid? OrganizationIdOrNull => _organizationId;

        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no context is set.</exception>
        public Guid UserId => _userId 
            ?? throw new InvalidOperationException("User context not set. User must be logged in.");

        /// <summary>
        /// Gets the current user ID, or null if not set.
        /// </summary>
        public Guid? UserIdOrNull => _userId;

        /// <summary>
        /// Gets the current user's email address.
        /// </summary>
        public string? UserEmail => _userEmail;

        /// <summary>
        /// Gets the current organization's display name.
        /// </summary>
        public string? OrganizationName => _organizationName;

        /// <summary>
        /// Gets whether the current user is an admin of the organization.
        /// </summary>
        public bool IsAdmin => _isAdmin;

        #endregion

        #region Context Management

        /// <summary>
        /// Sets the organization context. Called after successful authentication.
        /// </summary>
        /// <param name="organizationId">The organization ID</param>
        /// <param name="userId">The user ID (Supabase auth UID)</param>
        /// <param name="userEmail">The user's email address</param>
        /// <param name="organizationName">Optional organization display name</param>
        /// <param name="isAdmin">Whether user is org admin</param>
        public void SetContext(
            Guid organizationId, 
            Guid userId, 
            string userEmail,
            string? organizationName = null,
            bool isAdmin = false)
        {
            _organizationId = organizationId;
            _userId = userId;
            _userEmail = userEmail;
            _organizationName = organizationName;
            _isAdmin = isAdmin;

            _logger.Info(
                "Organization context set: org={0} ({1}), user={2} ({3}), admin={4}",
                organizationId, organizationName ?? "unnamed", userId, userEmail, isAdmin);

            OnContextChanged();
        }

        /// <summary>
        /// Updates the organization name without changing other context.
        /// </summary>
        public void SetOrganizationName(string name)
        {
            _organizationName = name;
            _logger.Debug("Organization name updated: {0}", name);
        }

        /// <summary>
        /// Updates the admin status without changing other context.
        /// </summary>
        public void SetAdminStatus(bool isAdmin)
        {
            _isAdmin = isAdmin;
            _logger.Debug("Admin status updated: {0}", isAdmin);
        }

        /// <summary>
        /// Clears the organization context. Called on logout.
        /// </summary>
        public void ClearContext()
        {
            var hadContext = HasContext;
            
            _organizationId = null;
            _userId = null;
            _userEmail = null;
            _organizationName = null;
            _isAdmin = false;

            if (hadContext)
            {
                _logger.Info("Organization context cleared");
                OnContextChanged();
            }
        }

        /// <summary>
        /// Creates a temporary context scope that reverts when disposed.
        /// Useful for impersonation or testing scenarios.
        /// </summary>
        public IDisposable CreateScope(
            Guid organizationId,
            Guid userId,
            string userEmail,
            string? organizationName = null,
            bool isAdmin = false)
        {
            return new ContextScope(this, organizationId, userId, userEmail, organizationName, isAdmin);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Ensures an organization context is set, throwing if not.
        /// </summary>
        /// <param name="operation">Description of the operation requiring context</param>
        public void RequireContext(string operation = "This operation")
        {
            if (!HasContext)
            {
                throw new InvalidOperationException(
                    $"{operation} requires an organization context. Please ensure the user is logged in.");
            }
        }

        /// <summary>
        /// Ensures the current user is an admin, throwing if not.
        /// </summary>
        /// <param name="operation">Description of the operation requiring admin</param>
        public void RequireAdmin(string operation = "This operation")
        {
            RequireContext(operation);
            
            if (!IsAdmin)
            {
                throw new UnauthorizedAccessException(
                    $"{operation} requires administrator privileges.");
            }
        }

        #endregion

        private void OnContextChanged()
        {
            ContextChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Nested Types

        /// <summary>
        /// Temporary context scope that restores previous context on dispose.
        /// </summary>
        private sealed class ContextScope : IDisposable
        {
            private readonly OrganizationContext _context;
            private readonly Guid? _previousOrgId;
            private readonly Guid? _previousUserId;
            private readonly string? _previousEmail;
            private readonly string? _previousOrgName;
            private readonly bool _previousIsAdmin;
            private bool _disposed;

            public ContextScope(
                OrganizationContext context,
                Guid organizationId,
                Guid userId,
                string userEmail,
                string? organizationName,
                bool isAdmin)
            {
                _context = context;
                
                // Save previous state
                _previousOrgId = context._organizationId;
                _previousUserId = context._userId;
                _previousEmail = context._userEmail;
                _previousOrgName = context._organizationName;
                _previousIsAdmin = context._isAdmin;

                // Set new context
                context.SetContext(organizationId, userId, userEmail, organizationName, isAdmin);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                // Restore previous state
                if (_previousOrgId.HasValue && _previousUserId.HasValue && _previousEmail != null)
                {
                    _context.SetContext(
                        _previousOrgId.Value,
                        _previousUserId.Value,
                        _previousEmail,
                        _previousOrgName,
                        _previousIsAdmin);
                }
                else
                {
                    _context.ClearContext();
                }
            }
        }

        #endregion
    }
}
