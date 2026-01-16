namespace Tracker.Core.Services.Backend
{
    /// <summary>
    /// Configuration for Supabase connection.
    /// These values are embedded in the application binary and are safe for client-side use.
    /// The anon key only allows operations permitted by Row Level Security policies.
    /// </summary>
    internal static class SupabaseConfig
    {
        /// <summary>
        /// Supabase project URL.
        /// </summary>
        internal const string ProjectUrl = "https://cftzoxucrzqljadyiijd.supabase.co";

        /// <summary>
        /// Supabase anon/public key (JWT format).
        /// This is safe to include in client apps - it only allows RLS-permitted operations.
        /// Note: Using legacy JWT format for .NET SDK compatibility. 
        /// Supabase is migrating to publishable keys - update when SDK supports it.
        /// </summary>
        internal const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmdHpveHVjcnpxbGphZHlpaWpkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjU3OTMwOTUsImV4cCI6MjA4MTM2OTA5NX0.HryjU3cmZlSGOjoSLcVoKIQiCspiqs7eN2Eemsf1LhY";

        /// <summary>
        /// Direct database connection string for Dapper/Npgsql.
        /// Uses the Supabase PostgreSQL database with SSL required.
        /// </summary>
        internal const string DatabaseConnectionString = 
            "Host=db.cftzoxucrzqljadyiijd.supabase.co;" +
            "Port=5432;" +
            "Database=postgres;" +
            "Username=postgres;" +
            "Password=3M1ly@2112$teelers;" +
            "SSL Mode=Require;" +
            "Trust Server Certificate=true;" +
            "Pooling=true;" +
            "Minimum Pool Size=1;" +
            "Maximum Pool Size=20;";

        /// <summary>
        /// Storage bucket name for avatars.
        /// </summary>
        internal const string AvatarBucket = "avatars";

        /// <summary>
        /// Maximum avatar file size in bytes (500KB).
        /// </summary>
        internal const int MaxAvatarSizeBytes = 512000;

        /// <summary>
        /// Avatar image dimensions after resize.
        /// </summary>
        internal const int AvatarSizePx = 256;
    }
}

