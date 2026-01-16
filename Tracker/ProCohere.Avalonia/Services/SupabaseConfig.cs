namespace ProCohere.Avalonia.Services;

/// <summary>
/// Configuration for Supabase connection.
/// These values are embedded in the application binary and are safe for client-side use.
/// </summary>
internal static class SupabaseConfig
{
    /// <summary>
    /// Supabase project URL.
    /// </summary>
    internal const string ProjectUrl = "https://cftzoxucrzqljadyiijd.supabase.co";

    /// <summary>
    /// Supabase anon/public key (JWT format).
    /// </summary>
    internal const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmdHpveHVjcnpxbGphZHlpaWpkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjU3OTMwOTUsImV4cCI6MjA4MTM2OTA5NX0.HryjU3cmZlSGOjoSLcVoKIQiCspiqs7eN2Eemsf1LhY";
}
