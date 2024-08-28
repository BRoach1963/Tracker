using System.Windows;
using Tracker.Common.Enums;

namespace Tracker.ViewModels.DialogViewModels
{
    public class SettingsViewModel : BaseDialogViewModel
    {

        #region Fields

        private bool _blackMode;
        private bool _goldMode;
        private bool _lightMode;

        #endregion

        #region Ctor

        public SettingsViewModel(Action? callback) : base(callback)
        {
            GetThemeSettings();
        }

        #endregion

        #region Public Properties

        public bool BlackMode
        {
            get => _blackMode;
            set
            {
                _blackMode = value;
                RaisePropertyChanged();
                if (_blackMode) SetTheme(ThemeEnum.Black);
            }
        }
        public bool GoldMode
        {
            get => _goldMode;
            set
            {
                _goldMode = value;
                RaisePropertyChanged();
                if (_goldMode) SetTheme(ThemeEnum.Gold);
            }
        }

        public bool LightMode
        {
            get => _goldMode;
            set
            {
                _lightMode = value;
                RaisePropertyChanged();
                if (_lightMode) SetTheme(ThemeEnum.Light);
            }
        }

        #endregion

        #region Private Methods

        private void SetTheme(ThemeEnum theme)
        {
            ResourceDictionary newTheme = new ResourceDictionary();
            switch (theme)
            {
                case ThemeEnum.Black:
                    newTheme.Source = new Uri("Resources/Themes/BlackTheme.xaml", UriKind.Relative);
                    break;
                case ThemeEnum.Gold:
                    newTheme.Source = new Uri("Resources/Themes/GoldTheme.xaml", UriKind.Relative);
                    break;
                case ThemeEnum.Light:
                    newTheme.Source = new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative);
                    break;

            }

            var themeDictionary = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                                     (d.Source.ToString().Contains("BlackTheme.xaml") ||
                                      d.Source.ToString().Contains("GoldTheme.xaml") ||
                                      d.Source.ToString().Contains("LightTheme.xaml")));

            if (themeDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(themeDictionary);
            }

            Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }

        private void GetThemeSettings()
        {
            var themeDictionary = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                                     d.Source.ToString().Contains("BlackTheme.xaml"));

            if (themeDictionary is not null)
            {
                _blackMode = true;
            }
            else
            {
                _goldMode = true;
            }
        }

        #endregion
    }
}
