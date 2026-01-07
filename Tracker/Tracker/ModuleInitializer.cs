using System;
using System.Runtime.CompilerServices;

namespace Tracker
{
    /// <summary>
    /// Module initializer that runs before any other code in the assembly.
    /// Used to configure global settings that must be set before types are loaded.
    /// </summary>
    internal static class ModuleInitializer
    {
        /// <summary>
        /// Called automatically by the runtime before any code in this assembly executes.
        /// </summary>
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Enable legacy timestamp behavior for Npgsql 6.0+
            // This MUST be set before any Npgsql types are loaded.
            // It allows DateTime to work with PostgreSQL timestamp with time zone columns
            // without requiring DateTimeOffset throughout the codebase.
            
            // Try both methods to ensure the switch is set
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            
            // Verify it was set
            bool isSet = AppContext.TryGetSwitch("Npgsql.EnableLegacyTimestampBehavior", out bool value);
            
            System.Diagnostics.Debug.WriteLine($"=== ModuleInitializer: Npgsql.EnableLegacyTimestampBehavior = {value} (found: {isSet}) ===");
        }
    }
}
