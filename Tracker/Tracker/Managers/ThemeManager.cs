using System.Windows;
using System.Windows.Media;
using DeepEndControls.Theming;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages application theming with support for runtime theme switching.
    /// 
    /// This manager integrates with DeepEndControls theming system and provides
    /// dynamic resource dictionary updates for seamless theme changes without restart.
    /// 
    /// Supported Themes:
    /// - Light: Clean white theme with gold accents - professional look (DEFAULT)
    /// - Dark: Dark slate theme with gold accents - reduced eye strain
    /// 
    /// Theme Resources Generated:
    /// - Core brushes: BackgroundBrush, ForegroundBrush, AccentBrush, etc.
    /// - Component-specific: ButtonBackgroundBrush, PopupBackgroundBrush, etc.
    /// - DataGrid-specific: DataGridCellBackgroundBrush, DataGridHeaderBackgroundBrush, etc.
    /// 
    /// Usage:
    /// <code>
    /// // Initialize on app startup
    /// ThemeManager.Instance.Initialize(savedTheme);
    /// 
    /// // Change theme at runtime
    /// ThemeManager.Instance.ApplyTheme(DeepEndTheme.Light);
    /// 
    /// // Listen for changes
    /// ThemeManager.Instance.ThemeChanged += (s, theme) => UpdateUI();
    /// </code>
    /// 
    /// XAML Usage:
    /// <code>
    /// &lt;Border Background="{DynamicResource BackgroundBrush}"&gt;
    ///     &lt;TextBlock Foreground="{DynamicResource TextBrush}"/&gt;
    /// &lt;/Border&gt;
    /// </code>
    /// 
    /// Note: Always use DynamicResource (not StaticResource) for theme brushes
    /// to enable live updates when the theme changes.
    /// </summary>
    public class ThemeManager
    {
        #region Fields

        private DeepEndTheme _currentTheme = DeepEndTheme.Light;
        private ResourceDictionary? _currentThemeDictionary;

        #endregion

        #region Singleton Instance

        private static readonly Lazy<ThemeManager> _lazyInstance = 
            new(() => new ThemeManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of ThemeManager.
        /// </summary>
        public static ThemeManager Instance => _lazyInstance.Value;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the currently active theme.
        /// </summary>
        public DeepEndTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Event fired when the theme changes.
        /// </summary>
        public event EventHandler<DeepEndTheme>? ThemeChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the theme manager with the specified theme.
        /// </summary>
        /// <param name="theme">The theme to apply on startup. Defaults to Light theme.</param>
        public void Initialize(DeepEndTheme theme = DeepEndTheme.Light)
        {
            // Normalize old theme values to Light or Dark (Tracker)
            theme = NormalizeTheme(theme);
            ApplyTheme(theme);
            
            // Subscribe to MainWindow changes to apply theme to new windows
            if (Application.Current != null)
            {
                Application.Current.Activated += OnApplicationActivated;
            }
        }

        private void OnApplicationActivated(object? sender, EventArgs e)
        {
            // Apply theme to MainWindow if it wasn't available during initial theme application
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (Application.Current?.MainWindow != null)
                {
                    var currentWindowTheme = DeepEndThemeManager.GetTheme(Application.Current.MainWindow);
                    if (currentWindowTheme != _currentTheme)
                    {
                        DeepEndThemeManager.SetTheme(Application.Current.MainWindow, _currentTheme);
                    }
                }
            });
        }

        /// <summary>
        /// Applies the specified theme to the application at runtime.
        /// 
        /// This method performs the following steps:
        /// 1. Gets the color palette for the requested theme
        /// 2. Creates a ResourceDictionary with all theme brushes
        /// 3. Removes any existing theme dictionaries
        /// 4. Inserts the new theme dictionary in the correct position
        /// 5. Notifies subscribers of the theme change
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <remarks>
        /// This method is thread-safe and will marshal to the UI thread if called
        /// from a background thread.
        /// </remarks>
        public void ApplyTheme(DeepEndTheme theme)
        {
            if (Application.Current == null) return;

            // Normalize to Light or Dark only
            theme = NormalizeTheme(theme);

            // Theme changes must happen on the UI thread since we're modifying
            // WPF resources. Marshal the call if we're on a background thread.
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyTheme(theme));
                return;
            }

            ResourceDictionary newThemeDictionary;
            
            // Load the appropriate theme XAML file
            if (theme == DeepEndTheme.Light)
            {
                newThemeDictionary = LoadLightTheme();
            }
            else
            {
                // Dark theme (Tracker) - use TrackerTheme.xaml
                newThemeDictionary = LoadDarkTheme();
            }

            // Remove the previously applied theme dictionary (if any)
            if (_currentThemeDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
            }

            // Also remove any theme dictionaries that were loaded from XAML files
            // (e.g., DefaultTheme.xaml, LightTheme.xaml loaded in App.xaml)
            var existingThemeDicts = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.ToString().Contains("Theme.xaml"))
                .ToList();
            
            foreach (var dict in existingThemeDicts)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dict);
            }

            // Insert the new theme dictionary AFTER Styles.xaml
            // This ensures theme brushes override any hardcoded values in styles
            // (ResourceDictionaries later in the MergedDictionaries collection take precedence)
            var stylesIndex = -1;
            for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
            {
                var dict = Application.Current.Resources.MergedDictionaries[i];
                if (dict.Source != null && dict.Source.ToString().Contains("Styles.xaml"))
                {
                    stylesIndex = i;
                    break;
                }
            }

            if (stylesIndex >= 0)
            {
                // Insert immediately after Styles.xaml for proper precedence
                Application.Current.Resources.MergedDictionaries.Insert(stylesIndex + 1, newThemeDictionary);
            }
            else
            {
                // Fallback: add at end if Styles.xaml not found
                Application.Current.Resources.MergedDictionaries.Add(newThemeDictionary);
            }

            // Track the current theme state
            _currentThemeDictionary = newThemeDictionary;
            _currentTheme = theme;

            // Also apply theme to DeepEndControls components on the MainWindow
            if (Application.Current.MainWindow != null)
            {
                DeepEndThemeManager.SetTheme(Application.Current.MainWindow, theme);
            }

            // Notify listeners that the theme has changed
            ThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Gets the display name for a theme.
        /// </summary>
        public static string GetThemeDisplayName(DeepEndTheme theme) => theme switch
        {
            DeepEndTheme.Light => "Light",
            DeepEndTheme.Dark => "Dark",
            _ => "Light" // Default to Light for any other theme
        };

        /// <summary>
        /// Gets available themes for Tracker app.
        /// Only Light and Dark are supported.
        /// </summary>
        public static IEnumerable<DeepEndTheme> GetAvailableThemes()
        {
            return new[]
            {
                DeepEndTheme.Light,   // Professional light theme with gold accents (DEFAULT)
                DeepEndTheme.Dark     // Dark slate theme with gold accents
            };
        }

        /// <summary>
        /// Normalizes any theme value to either Light or Dark.
        /// Used to handle legacy theme values from old settings.
        /// </summary>
        private static DeepEndTheme NormalizeTheme(DeepEndTheme theme)
        {
            // Map all themes to either Light or Dark
            return theme switch
            {
                DeepEndTheme.Light => DeepEndTheme.Light,
                DeepEndTheme.Dark => DeepEndTheme.Dark,
                _ => DeepEndTheme.Light // Everything else defaults to Light
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the Light theme from XAML.
        /// Uses White background with Gold (#C7A450) accents.
        /// </summary>
        private static ResourceDictionary LoadLightTheme()
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Resources/Themes/LightTheme.xaml", UriKind.Absolute)
            };
            
            // Freeze all brushes for performance
            foreach (var key in dictionary.Keys)
            {
                if (dictionary[key] is SolidColorBrush brush && !brush.IsFrozen)
                {
                    brush.Freeze();
                }
            }
            
            return dictionary;
        }

        /// <summary>
        /// Loads the Dark theme from XAML.
        /// Uses Dark Slate (#2E3843) background with Gold (#C7A450) accents.
        /// </summary>
        private static ResourceDictionary LoadDarkTheme()
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Resources/Themes/TrackerTheme.xaml", UriKind.Absolute)
            };
            
            // Freeze all brushes for performance
            foreach (var key in dictionary.Keys)
            {
                if (dictionary[key] is SolidColorBrush brush && !brush.IsFrozen)
                {
                    brush.Freeze();
                }
            }
            
            return dictionary;
        }

        #endregion
    }
}