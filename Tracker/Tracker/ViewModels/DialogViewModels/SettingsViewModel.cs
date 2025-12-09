using System.Collections.ObjectModel;
using System.Windows.Media;
using DeepEndControls.Theming;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    public class SettingsViewModel : BaseDialogViewModel
    {
        #region Fields

        private ThemeItem? _selectedTheme;

        #endregion

        #region Ctor

        public SettingsViewModel(Action? callback) : base(callback)
        {
            // Populate available themes
            AvailableThemes = new ObservableCollection<ThemeItem>();
            foreach (var theme in ThemeManager.GetAvailableThemes())
            {
                AvailableThemes.Add(new ThemeItem
                {
                    Theme = theme,
                    DisplayName = ThemeManager.GetThemeDisplayName(theme),
                    PreviewColor = GetThemePreviewColor(theme)
                });
            }

            // Set current selection
            _selectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == ThemeManager.Instance.CurrentTheme);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of available themes for the ComboBox.
        /// </summary>
        public ObservableCollection<ThemeItem> AvailableThemes { get; }

        /// <summary>
        /// The currently selected theme.
        /// </summary>
        public ThemeItem? SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    RaisePropertyChanged();
                    
                    if (_selectedTheme != null)
                    {
                        UserSettingsManager.Instance.Theme = _selectedTheme.Theme;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private static Brush GetThemePreviewColor(DeepEndTheme theme)
        {
            var palette = ThemePalette.GetPalette(theme);
            return palette.PrimaryBrush;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Represents a theme option for display in the UI.
        /// </summary>
        public class ThemeItem
        {
            public DeepEndTheme Theme { get; init; }
            public string DisplayName { get; init; } = string.Empty;
            public Brush PreviewColor { get; init; } = Brushes.Transparent;
        }

        #endregion
    }
}
