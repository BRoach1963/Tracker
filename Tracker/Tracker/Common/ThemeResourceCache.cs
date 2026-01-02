using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;

namespace Tracker.Common
{
    /// <summary>
    /// Caches theme resource lookups to improve performance.
    /// 
    /// Instead of repeatedly calling Application.Current.TryFindResource(),
    /// this cache stores the results to avoid repeated lookups.
    /// 
    /// Usage:
    /// <code>
    /// var brush = ThemeResourceCache.GetBrush("ForegroundBrush");
    /// var color = ThemeResourceCache.GetColor("AccentColor");
    /// </code>
    /// </summary>
    public static class ThemeResourceCache
    {
        #region Fields

        private static readonly ConcurrentDictionary<string, object?> _resourceCache = 
            new ConcurrentDictionary<string, object?>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a brush resource from the cache, or looks it up if not cached.
        /// </summary>
        public static Brush? GetBrush(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
                return null;

            var resource = GetResource(resourceKey);
            return resource as Brush;
        }

        /// <summary>
        /// Gets a color resource from the cache, or looks it up if not cached.
        /// </summary>
        public static Color? GetColor(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
                return null;

            var resource = GetResource(resourceKey);
            if (resource is Color color)
                return color;

            if (resource is SolidColorBrush brush)
                return brush.Color;

            return null;
        }

        /// <summary>
        /// Gets a generic resource from the cache, or looks it up if not cached.
        /// </summary>
        public static object? GetResource(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
                return null;

            // Try to get from cache first
            if (_resourceCache.TryGetValue(resourceKey, out var cachedValue))
                return cachedValue;

            // If not in cache, look it up
            if (Application.Current.TryFindResource(resourceKey) is object resource)
            {
                // Cache the result
                _resourceCache.TryAdd(resourceKey, resource);
                return resource;
            }

            // Cache null to avoid repeated lookups for missing resources
            _resourceCache.TryAdd(resourceKey, null);
            return null;
        }

        /// <summary>
        /// Clears the resource cache. Call this when the theme changes.
        /// </summary>
        public static void ClearCache()
        {
            _resourceCache.Clear();
        }

        /// <summary>
        /// Removes a specific resource from the cache.
        /// </summary>
        public static void RemoveResource(string resourceKey)
        {
            if (!string.IsNullOrEmpty(resourceKey))
            {
                _resourceCache.TryRemove(resourceKey, out _);
            }
        }

        #endregion
    }
}

