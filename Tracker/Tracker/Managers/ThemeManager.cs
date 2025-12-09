using System.Windows;
using System.Windows.Media;
using DeepEndControls.Theming;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages application theming using DeepEndControls themes.
    /// Provides runtime theme switching with resource dictionary updates.
    /// </summary>
    public class ThemeManager
    {
        #region Fields

        private static ThemeManager? _instance;
        private static readonly object SyncRoot = new();
        private DeepEndTheme _currentTheme = DeepEndTheme.Default;
        private ResourceDictionary? _currentThemeDictionary;

        #endregion

        #region Singleton Instance

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new ThemeManager();
                    }
                }
                return _instance;
            }
        }

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
        /// <param name="theme">The theme to apply on startup.</param>
        public void Initialize(DeepEndTheme theme = DeepEndTheme.Default)
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
        /// Applies the specified theme to the application.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public void ApplyTheme(DeepEndTheme theme)
        {
            if (Application.Current == null) return;

            // Ensure we're on the UI thread
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyTheme(theme));
                return;
            }

            var palette = ThemePalette.GetPalette(theme);
            var newThemeDictionary = CreateThemeDictionary(palette, theme);

            // Remove old theme dictionary if exists (including the one from App.xaml)
            if (_currentThemeDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
            }

            // Also remove any theme dictionaries loaded from XAML (DefaultTheme, LightTheme, etc.)
            var existingThemeDicts = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.ToString().Contains("Theme.xaml"))
                .ToList();
            
            foreach (var dict in existingThemeDicts)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dict);
            }

            // Add new theme dictionary - insert AFTER Styles.xaml so theme colors take precedence
            // Find the position after Styles.xaml, or add at the end if not found
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
                // Insert after Styles.xaml so our theme values take precedence
                Application.Current.Resources.MergedDictionaries.Insert(stylesIndex + 1, newThemeDictionary);
            }
            else
            {
                // Add at end if Styles.xaml not found
                Application.Current.Resources.MergedDictionaries.Add(newThemeDictionary);
            }

            _currentThemeDictionary = newThemeDictionary;
            _currentTheme = theme;

            // Apply to MainWindow for DeepEndControls (if available)
            if (Application.Current.MainWindow != null)
            {
                DeepEndThemeManager.SetTheme(Application.Current.MainWindow, theme);
            }

            ThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Gets the display name for a theme.
        /// </summary>
        public static string GetThemeDisplayName(DeepEndTheme theme) => theme switch
        {
            DeepEndTheme.Default => "Default (Black/Gold)",
            DeepEndTheme.Light => "Light",
            DeepEndTheme.Modern => "Modern",
            DeepEndTheme.Spicy => "Spicy",
            _ => theme.ToString()
        };

        /// <summary>
        /// Gets all available themes.
        /// </summary>
        public static IEnumerable<DeepEndTheme> GetAvailableThemes()
        {
            return Enum.GetValues<DeepEndTheme>();
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

