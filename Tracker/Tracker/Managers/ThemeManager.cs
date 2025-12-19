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
    /// - Default (Black/Gold): Dark theme with gold accents - classic CLEZ styling
    /// - Light: Clean white theme with blue accents - professional look
    /// - Modern: Contemporary indigo/purple theme - trendy appearance
    /// - Spicy: Bold red/coral on dark - high contrast, energetic
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
    /// ThemeManager.Instance.ApplyTheme(DeepEndTheme.Modern);
    /// 
    /// // Listen for changes
    /// ThemeManager.Instance.ThemeChanged += (s, theme) => UpdateUI();
    /// </code>
    /// 
    /// XAML Usage:
    /// <code>
    /// &lt;Border Background="{DynamicResource BackgroundBrush}"&gt;
    ///     &lt;TextBlock Foreground="{DynamicResource ForegroundBrush}"/&gt;
    /// &lt;/Border&gt;
    /// </code>
    /// 
    /// Note: Always use DynamicResource (not StaticResource) for theme brushes
    /// to enable live updates when the theme changes.
    /// </summary>
    public class ThemeManager
    {
        #region Fields

        private DeepEndTheme _currentTheme = DeepEndTheme.Default;
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
        /// <param name="theme">The theme to apply on startup. Defaults to Tracker theme.</param>
        public void Initialize(DeepEndTheme theme = DeepEndTheme.Tracker)
        {
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

            // Theme changes must happen on the UI thread since we're modifying
            // WPF resources. Marshal the call if we're on a background thread.
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyTheme(theme));
                return;
            }

            // Get the color palette from DeepEndControls for the selected theme
            var palette = ThemePalette.GetPalette(theme);
            
            // Create a new ResourceDictionary populated with all theme brushes
            var newThemeDictionary = CreateThemeDictionary(palette, theme);

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
            DeepEndTheme.Tracker => "Default",
            DeepEndTheme.Light => "Light",
            DeepEndTheme.Modern => "Modern", 
            DeepEndTheme.Spicy => "Bold",
            DeepEndTheme.Default => "Classic",
            _ => theme.ToString()
        };

        /// <summary>
        /// Gets available themes for Tracker app.
        /// Filters out themes intended for other applications (Eagles, Capehart, Phillies).
        /// </summary>
        public static IEnumerable<DeepEndTheme> GetAvailableThemes()
        {
            // Only expose themes relevant to Tracker
            // Eagles, Capehart, Phillies are for CLEZ/CDB
            return new[]
            {
                DeepEndTheme.Tracker,  // Default for Tracker - Gold on Black
                DeepEndTheme.Light,    // Professional blue on white
                DeepEndTheme.Modern,   // Contemporary indigo/purple
                DeepEndTheme.Spicy     // Bold red/coral
            };
        }

        #endregion

        #region Private Methods

        private static ResourceDictionary CreateThemeDictionary(ThemePalette palette, DeepEndTheme theme)
        {
            var dictionary = new ResourceDictionary();

            // Core colors (for backwards compatibility with existing styles)
            dictionary["BackgroundColor"] = ((SolidColorBrush)palette.BackgroundBrush).Color;
            dictionary["ForegroundColor"] = ((SolidColorBrush)palette.PrimaryBrush).Color;

            // Core brushes (matching existing theme structure)
            dictionary["BackgroundBrush"] = palette.BackgroundBrush;
            dictionary["ForegroundBrush"] = palette.PrimaryBrush;
            dictionary["WhiteBrush"] = palette.ForegroundBrush;
            dictionary["HintTextBrush"] = palette.HintBrush;
            dictionary["DisabledBrush"] = CreateFrozenBrush(Color.FromRgb(0x80, 0x80, 0x80));

            // Extended brushes for more control
            dictionary["PrimaryBrush"] = palette.PrimaryBrush;
            dictionary["AccentBrush"] = palette.AccentBrush;
            dictionary["BorderBrush"] = palette.BorderBrush;
            dictionary["ErrorBrush"] = palette.ErrorBrush;
            dictionary["TextBrush"] = palette.ForegroundBrush;

            // Popup/dropdown brushes
            dictionary["PopupBackgroundBrush"] = palette.PopupBackgroundBrush;
            dictionary["PopupBorderBrush"] = palette.PopupBorderBrush;

            // Button brushes
            dictionary["ButtonBackgroundBrush"] = palette.ButtonBackgroundBrush;
            dictionary["ButtonForegroundBrush"] = palette.ButtonForegroundBrush;

            // ToolTip brushes (use popup colors)
            dictionary["ToolTipBackground"] = palette.PopupBackgroundBrush;
            dictionary["ToolTipForeground"] = palette.ForegroundBrush;

            // Additional derived colors for UI elements
            var bgColor = ((SolidColorBrush)palette.BackgroundBrush).Color;
            var isDarkTheme = IsDarkColor(bgColor);
            
            // Selection and hover colors
            dictionary["SelectionBrush"] = CreateFrozenBrush(
                AdjustBrightness(((SolidColorBrush)palette.AccentBrush).Color, isDarkTheme ? 0.3 : -0.1));
            dictionary["HoverBrush"] = CreateFrozenBrush(
                AdjustBrightness(bgColor, isDarkTheme ? 0.15 : -0.05));

            // Surface colors (slightly different from background for cards, etc.)
            dictionary["SurfaceBrush"] = CreateFrozenBrush(
                AdjustBrightness(bgColor, isDarkTheme ? 0.05 : -0.02));
            dictionary["SurfaceAltBrush"] = CreateFrozenBrush(
                AdjustBrightness(bgColor, isDarkTheme ? 0.1 : -0.05));

            // DataGrid specific brushes - Dark themes keep dark cells, light themes get white cells
            var primaryColor = ((SolidColorBrush)palette.PrimaryBrush).Color;
            
            if (theme == DeepEndTheme.Default || theme == DeepEndTheme.Tracker || theme == DeepEndTheme.Spicy)
            {
                // Dark themes - dark cells with accent text
                dictionary["DataGridCellBackgroundBrush"] = palette.BackgroundBrush;
                dictionary["DataGridCellForegroundBrush"] = palette.PrimaryBrush;
                dictionary["DataGridCellBorderBrush"] = palette.PrimaryBrush;
                dictionary["DataGridHeaderBackgroundBrush"] = CreateFrozenBrush(AdjustBrightness(bgColor, 0.1));
                dictionary["DataGridHeaderForegroundBrush"] = palette.PrimaryBrush;
                dictionary["DataGridRowAlternateBrush"] = CreateFrozenBrush(AdjustBrightness(bgColor, 0.05));
            }
            else
            {
                // Light themes - white cells with colored text, colored headers with white text
                dictionary["DataGridCellBackgroundBrush"] = CreateFrozenBrush(Colors.White);
                dictionary["DataGridCellForegroundBrush"] = palette.PrimaryBrush;
                dictionary["DataGridCellBorderBrush"] = palette.PrimaryBrush;
                dictionary["DataGridHeaderBackgroundBrush"] = palette.PrimaryBrush;
                dictionary["DataGridHeaderForegroundBrush"] = CreateFrozenBrush(Colors.White);
                dictionary["DataGridRowAlternateBrush"] = CreateFrozenBrush(Color.FromRgb(0xF8, 0xF8, 0xF8));
            }

            return dictionary;
        }

        private static bool IsDarkColor(Color color)
        {
            // Calculate relative luminance
            var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance < 0.5;
        }

        private static Color AdjustBrightness(Color color, double factor)
        {
            int r, g, b;
            if (factor > 0)
            {
                // Lighten
                r = (int)Math.Min(255, color.R + (255 - color.R) * factor);
                g = (int)Math.Min(255, color.G + (255 - color.G) * factor);
                b = (int)Math.Min(255, color.B + (255 - color.B) * factor);
            }
            else
            {
                // Darken
                factor = Math.Abs(factor);
                r = (int)Math.Max(0, color.R * (1 - factor));
                g = (int)Math.Max(0, color.G * (1 - factor));
                b = (int)Math.Max(0, color.B * (1 - factor));
            }
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion
    }
}

