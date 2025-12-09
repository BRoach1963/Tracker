using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeepEndControls.Theming;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Managers;
using Tracker.Views.Dialogs;

namespace Tracker.ViewModels.DialogViewModels
{
    public class SettingsViewModel : BaseDialogViewModel
    {
        #region Fields

        private ThemeItem? _selectedTheme;
        private ICommand? _changeDatabaseCommand;
        private ICommand? _clearDataCommand;
        private ICommand? _seedSampleDataCommand;
        private CalendarSettingsViewModel? _calendarSettings;

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
            
            // Initialize Calendar Settings ViewModel
            _calendarSettings = new CalendarSettingsViewModel(null);
        }

        #endregion

        #region Commands

        public ICommand ChangeDatabaseCommand => _changeDatabaseCommand ??= new TrackerCommand(ExecuteChangeDatabase);
        public ICommand ClearDataCommand => _clearDataCommand ??= new TrackerCommand(ExecuteClearData);
        public ICommand SeedSampleDataCommand => _seedSampleDataCommand ??= new TrackerCommand(ExecuteSeedSampleData);

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

        /// <summary>
        /// Gets the current database type display string.
        /// </summary>
        public string CurrentDatabaseType
        {
            get
            {
                var settings = UserSettingsManager.Instance.Settings.Database;
                return settings.Type == DatabaseType.SQLite ? "Local (SQLite)" : "SQL Server";
            }
        }

        /// <summary>
        /// Gets the current database location display string.
        /// </summary>
        public string CurrentDatabaseLocation
        {
            get
            {
                var settings = UserSettingsManager.Instance.Settings.Database;
                if (settings.Type == DatabaseType.SQLite)
                {
                    return DatabaseSettings.GetSqlitePath();
                }
                
                if (settings.UseOdbc)
                {
                    return $"ODBC: {settings.OdbcDsn}";
                }
                
                return $"{settings.Server}/{settings.Database}";
            }
        }

        /// <summary>
        /// Gets the Calendar Settings ViewModel for the Calendar tab.
        /// </summary>
        public CalendarSettingsViewModel CalendarSettings => _calendarSettings!;

        #endregion

        #region Private Methods

        private void ExecuteChangeDatabase(object? parameter)
        {
            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                "Changing your database connection will require restarting the application.\n\n" +
                "Your data in the current database will NOT be migrated to the new database.\n\n" +
                "Do you want to continue?",
                "Change Database Connection",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Mark setup as not completed so the wizard shows on next launch
            UserSettingsManager.Instance.Settings.Database.SetupCompleted = false;
            UserSettingsManager.Instance.SaveSettings();

            // Inform user to restart
            MessageBoxHelper.Show(
                "Please restart Tracker to configure your new database connection.",
                "Restart Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                owner);
        }

        private async void ExecuteClearData(object? parameter)
        {
            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                "⚠️ WARNING: This will permanently delete ALL data from your database!\n\n" +
                "This includes:\n" +
                "• All team members\n" +
                "• All 1:1 meetings\n" +
                "• All projects, tasks, OKRs, and KPIs\n\n" +
                "This action cannot be undone.\n\n" +
                "Are you sure you want to continue?",
                "Clear All Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Double confirm
            result = MessageBoxHelper.Show(
                "Are you ABSOLUTELY sure? All data will be permanently deleted.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDbManager.Instance!.ClearAllDataAsync();
            
            if (success)
            {
                // Publish a message to refresh all data in the main ViewModel
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.All,
                    RefreshData = true
                });

                NotificationManager.Instance.ShowSuccess("Data Cleared", "All data has been removed from the database.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to clear data. Check the logs for details.");
            }
        }

        private async void ExecuteSeedSampleData(object? parameter)
        {
            // Check if database already has data
            var hasExistingData = await TrackerDbManager.Instance!.HasDataAsync();
            
            string message;
            bool forceReseed = false;
            
            if (hasExistingData)
            {
                message = "⚠️ WARNING: Your database already contains data!\n\n" +
                         "This will:\n" +
                         "• DELETE all existing data\n" +
                         "• Add fresh sample data including:\n" +
                         "  - 7 team members (Steelers team)\n" +
                         "  - Sample 1:1 meetings\n" +
                         "  - Sample projects with OKRs and KPIs\n" +
                         "  - Sample tasks\n" +
                         "  - Linked items (Phase 1 features)\n\n" +
                         "This action cannot be undone.\n\n" +
                         "Do you want to continue?";
                forceReseed = true;
            }
            else
            {
                message = "This will add sample data to your database including:\n\n" +
                         "• 7 team members (Steelers team)\n" +
                         "• Sample 1:1 meetings\n" +
                         "• Sample projects with OKRs and KPIs\n" +
                         "• Sample tasks\n" +
                         "• Linked items (Phase 1 features)\n\n" +
                         "Do you want to continue?";
            }

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                message,
                forceReseed ? "Replace Data with Sample Data" : "Add Sample Data",
                MessageBoxButton.YesNo,
                forceReseed ? MessageBoxImage.Warning : MessageBoxImage.Question,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDbManager.Instance!.SeedSampleDataAsync(forceReseed);
            
            if (success)
            {
                // Publish a message to refresh all data in the main ViewModel
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.All,
                    RefreshData = true
                });

                NotificationManager.Instance.ShowSuccess(
                    "Sample Data Added", 
                    forceReseed 
                        ? "All data has been replaced with fresh sample data." 
                        : "Sample data has been added to the database.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to add sample data. Check the logs for details.");
            }
        }

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
