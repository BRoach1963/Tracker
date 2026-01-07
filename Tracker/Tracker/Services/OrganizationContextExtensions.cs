using Tracker.Logging;
using Tracker.Services.AI;
using Tracker.Services.Backend;

namespace Tracker.Services
{
    /// <summary>
    /// Extensions for setting up OrganizationContext during authentication.
    /// </summary>
    public static class OrganizationContextExtensions
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger("OrganizationContextExt");

        /// <summary>
        /// Sets up the organization context after Supabase authentication.
        /// 
        /// For single-user scenarios, the organization ID equals the user ID.
        /// For multi-tenant scenarios, queries Supabase for the user's organization.
        /// </summary>
        /// <param name="context">The organization context to set up</param>
        /// <param name="supabase">The Supabase service instance</param>
        /// <param name="isMultiTenant">Whether this is a multi-tenant (shared database) scenario</param>
        /// <returns>True if context was set successfully</returns>
        public static bool SetupFromSupabase(
            this OrganizationContext context,
            SupabaseService supabase,
            bool isMultiTenant = false)
        {
            if (!supabase.IsSignedIn || supabase.CurrentUser == null)
            {
                _logger.Warn("Cannot set up organization context - user not signed in");
                return false;
            }

            var user = supabase.CurrentUser;
            
            // Parse user ID
            if (!Guid.TryParse(user.Id, out var userId))
            {
                _logger.Error("Invalid user ID format from Supabase: {0}", user.Id);
                return false;
            }

            // For now, use user ID as organization ID in single-user mode
            // In multi-tenant mode, this would query for the user's organization
            var organizationId = userId;
            var organizationName = supabase.CurrentProfile?.DisplayName ?? "Personal";

            context.SetContext(
                organizationId: organizationId,
                userId: userId,
                userEmail: user.Email ?? "",
                organizationName: organizationName,
                isAdmin: true // In single-user mode, user is always admin
            );

            _logger.Info("Organization context set up from Supabase: org={0}, user={1}", 
                organizationId, userId);

            return true;
        }

        /// <summary>
        /// Sets up the organization context from local PostgreSQL auth result.
        /// </summary>
        public static bool SetupFromLocalAuth(
            this OrganizationContext context,
            Guid userId,
            string userEmail,
            Guid? organizationId = null,
            string? organizationName = null,
            bool isAdmin = false)
        {
            // If no org specified, use user ID (single-user mode)
            organizationId ??= userId;
            organizationName ??= "Personal";

            context.SetContext(
                organizationId: organizationId.Value,
                userId: userId,
                userEmail: userEmail,
                organizationName: organizationName,
                isAdmin: isAdmin
            );

            _logger.Info("Organization context set up from local auth: org={0}, user={1}", 
                organizationId, userId);

            return true;
        }

        /// <summary>
        /// Clears the organization context and any associated resources.
        /// Call this on sign-out.
        /// </summary>
        public static void ClearOnSignOut(this OrganizationContext context)
        {
            context.ClearContext();
            
            // Also clear the vector store from the data indexer
            DataIndexer.Instance.ClearVectorStore();
            
            _logger.Info("Organization context cleared on sign-out");
        }
    }
}
