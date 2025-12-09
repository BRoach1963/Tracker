using System.Reflection;

namespace Tracker.Helpers
{
    public static class VersionHelper
    {
        public static Version GetAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        }

        public static string GetVersion()
        {
            var version = GetAppVersion();
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}

