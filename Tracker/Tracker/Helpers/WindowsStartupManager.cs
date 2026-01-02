using Microsoft.Win32;
using System;
using System.Diagnostics;
using Tracker.Logging;

namespace Tracker.Helpers
{
    /// <summary>
    /// Manages Windows startup registration for Tracker.
    /// Uses the Windows Registry to enable/disable launch on Windows startup.
    /// </summary>
    public static class WindowsStartupManager
    {
        private static readonly ILogger _logger = LoggingManager.GetComponentLogger("StartupManager");
        
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "Tracker";
        
        /// <summary>
        /// Gets whether Tracker is currently set to start with Windows.
        /// </summary>
        public static bool IsStartupEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                    return key?.GetValue(AppName) != null;
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Error checking startup status");
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Enables Tracker to start with Windows.
        /// Adds a registry entry that launches the app minimized to system tray.
        /// </summary>
        public static bool EnableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    _logger.Error("Could not open registry key for writing");
                    return false;
                }
                
                // Get the current executable path
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.Error("Could not determine executable path");
                    return false;
                }
                
                // Add --minimized argument so app starts in system tray
                var startupCommand = $"\"{exePath}\" --minimized";
                key.SetValue(AppName, startupCommand);
                
                _logger.Info("Startup enabled: {0}", startupCommand);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error enabling startup");
                return false;
            }
        }
        
        /// <summary>
        /// Disables Tracker from starting with Windows.
        /// Removes the registry entry.
        /// </summary>
        public static bool DisableStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    _logger.Error("Could not open registry key for writing");
                    return false;
                }
                
                // Only delete if it exists
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                    _logger.Info("Startup disabled");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error disabling startup");
                return false;
            }
        }
        
        /// <summary>
        /// Sets the startup state based on the boolean parameter.
        /// </summary>
        public static bool SetStartupEnabled(bool enabled)
        {
            return enabled ? EnableStartup() : DisableStartup();
        }
    }
}
