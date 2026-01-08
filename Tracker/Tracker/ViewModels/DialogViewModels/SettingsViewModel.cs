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
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.AI;
using Tracker.Views.Dialogs;

namespace Tracker.ViewModels.DialogViewModels
{
    public class SettingsViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("SettingsVM");
        
        private ThemeItem? _selectedTheme;
        private ICommand? _changeDatabaseCommand;
        private ICommand? _clearDataCommand;
        private ICommand? _seedSampleDataCommand;
        private ICommand? _resetSetupCommand;
        private ICommand? _resetSettingsCommand;
        private ICommand? _refreshAiKnowledgeCommand;
        private CalendarSettingsViewModel? _calendarSettings;
        private bool _isRefreshingAi;
        private string _aiRefreshStatus = string.Empty;
        
        // AI Provider fields
        private AIProviderType _selectedAIProvider;
        private int _creditsUsed;
        private int _monthlyCredits = 1000;
        private int _additionalCredits;

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
            
            // Initialize AI Provider selection
            _selectedAIProvider = ChatProviderFactory.Instance.SelectedProvider;
            
            // Populate available AI providers
            AvailableAIProviders = new ObservableCollection<AIProviderType>(
                ChatProviderFactory.Instance.AvailableProviders);
            
            // Load credit information (async fire-and-forget, UI will update)
            _ = LoadCreditInfoAsync();
        }

        #endregion

        #region Commands

        public ICommand ChangeDatabaseCommand => _changeDatabaseCommand ??= new TrackerCommand(ExecuteChangeDatabase);
        public ICommand ClearDataCommand => _clearDataCommand ??= new TrackerCommand(ExecuteClearData);
        public ICommand SeedSampleDataCommand => _seedSampleDataCommand ??= new TrackerCommand(ExecuteSeedSampleData);
        public ICommand ResetSetupCommand => _resetSetupCommand ??= new TrackerCommand(ExecuteResetSetup);
        public ICommand ResetSettingsCommand => _resetSettingsCommand ??= new TrackerCommand(ExecuteResetSettings);
        public ICommand RefreshAiKnowledgeCommand => _refreshAiKnowledgeCommand ??= new AsyncCommand(ExecuteRefreshAiKnowledgeAsync, _ => !_isRefreshingAi, nameof(RefreshAiKnowledgeCommand));

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of available themes for the ComboBox.
        /// </summary>
        public ObservableCollection<ThemeItem> AvailableThemes { get; }

        /// <summary>
        /// Gets the reminder settings for binding.
        /// </summary>
        public Classes.ReminderSettings ReminderSettings => UserSettingsManager.Instance.ReminderSettings;

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
                    // Show custom path if set, otherwise default path
                    if (!string.IsNullOrWhiteSpace(settings.CustomSqlitePath))
                    {
                        return settings.CustomSqlitePath;
                    }
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

        /// <summary>
        /// Gets the current logged-in user display name.
        /// </summary>
        public string CurrentUserDisplay => UserSettingsManager.Instance.CurrentUser;

        /// <summary>
        /// Whether the AI knowledge base is currently being refreshed.
        /// </summary>
        public bool IsRefreshingAi
        {
            get => _isRefreshingAi;
            set
            {
                _isRefreshingAi = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNotRefreshingAi));
            }
        }

        public bool IsNotRefreshingAi => !_isRefreshingAi;

        /// <summary>
        /// Status message for AI knowledge refresh operation.
        /// </summary>
        public string AiRefreshStatus
        {
            get => _aiRefreshStatus;
            set
            {
                _aiRefreshStatus = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasAiRefreshStatus));
            }
        }

        public bool HasAiRefreshStatus => !string.IsNullOrEmpty(_aiRefreshStatus);

        #region AI Provider Properties

        /// <summary>
        /// Collection of available AI providers for the ComboBox.
        /// </summary>
        public ObservableCollection<AIProviderType> AvailableAIProviders { get; }

        /// <summary>
        /// The currently selected AI provider.
        /// </summary>
        public AIProviderType SelectedAIProvider
        {
            get => _selectedAIProvider;
            set
            {
                if (_selectedAIProvider != value)
                {
                    _selectedAIProvider = value;
                    ChatProviderFactory.Instance.SelectedProvider = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(AIProviderDescription));
                    _logger.Info("AI Provider changed to: {0}", value);
                }
            }
        }

        /// <summary>
        /// Gets a description of the currently selected AI provider.
        /// </summary>
        public string AIProviderDescription => ChatProviderFactory.GetProviderDescription(_selectedAIProvider);

        /// <summary>
        /// Summary of credit usage for display.
        /// </summary>
        public string CreditUsageSummary
        {
            get
            {
                if (HasUnlimitedCredits)
                    return "Unlimited credits with your subscription";
                
                var remaining = MonthlyCredits + AdditionalCredits - _creditsUsed;
                return $"{remaining:N0} credits remaining this month";
            }
        }

        /// <summary>
        /// Percentage of credits used (0-100).
        /// </summary>
        public double CreditsUsedPercent
        {
            get
            {
                var total = MonthlyCredits + AdditionalCredits;
                if (total <= 0) return 0;
                return Math.Min(100, (_creditsUsed * 100.0) / total);
            }
        }

        /// <summary>
        /// Monthly credit allowance based on subscription tier.
        /// </summary>
        public int MonthlyCredits
        {
            get => _monthlyCredits;
            private set
            {
                _monthlyCredits = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CreditUsageSummary));
                RaisePropertyChanged(nameof(CreditsUsedPercent));
            }
        }

        /// <summary>
        /// Additional purchased credits.
        /// </summary>
        public int AdditionalCredits
        {
            get => _additionalCredits;
            private set
            {
                _additionalCredits = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CreditUsageSummary));
                RaisePropertyChanged(nameof(CreditsUsedPercent));
            }
        }

        /// <summary>
        /// Whether the user has unlimited credits (e.g., Enterprise tier).
        /// </summary>
        public bool HasUnlimitedCredits => false; // TODO: Check subscription tier

        /// <summary>
        /// Whether credits are exhausted.
        /// </summary>
        public bool IsCreditsExhausted => !HasUnlimitedCredits && _creditsUsed >= (MonthlyCredits + AdditionalCredits);

        #endregion

        #region Vector Storage Properties

        /// <summary>
        /// Gets a user-friendly name for the current vector storage provider.
        /// </summary>
        public string VectorStorageProviderName
        {
            get
            {
                var settings = TrackerDbManager.Instance.CurrentSettings;
                if (settings == null) return "Legacy (SQLite)";
                return Services.AI.VectorStoreFactory.GetProviderDisplayName(settings);
            }
        }

        /// <summary>
        /// Gets whether a legacy vector store exists that could be migrated.
        /// </summary>
        public bool HasLegacyVectorStore => Services.AI.VectorStoreMigrator.HasLegacyStore();

        /// <summary>
        /// Gets the size of the legacy vector store in a friendly format.
        /// </summary>
        public string LegacyVectorStoreSize
        {
            get
            {
                var bytes = Services.AI.VectorStoreMigrator.GetLegacyStoreSize();
                if (bytes == 0) return "None";
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
                return $"{bytes / (1024.0 * 1024):F1} MB";
            }
        }

        /// <summary>
        /// Gets whether the current provider supports native vector operations.
        /// </summary>
        public bool HasNativeVectorSupport
        {
            get
            {
                var settings = TrackerDbManager.Instance.CurrentSettings;
                if (settings == null) return false;
                return settings.GetVectorStorageProvider() == VectorStorageProvider.PostgreSQL;
            }
        }

        #endregion

        #endregion

        #region Private Methods

        private async Task LoadCreditInfoAsync()
        {
            try
            {
                // Load subscription/credit info from Supabase
                var subscription = Services.Backend.SupabaseService.Instance.CurrentSubscription;
                if (subscription != null)
                {
                    _creditsUsed = subscription.AiRequestsThisMonth;
                    // Credits vary by tier - for now hardcode, should come from subscription_plans
                    MonthlyCredits = subscription.Tier switch
                    {
                        Common.Enums.SubscriptionTier.Pro => 5000,
                        Common.Enums.SubscriptionTier.Internal => int.MaxValue, // Unlimited
                        _ => 1000 // Free tier
                    };
                }
                
                RaisePropertyChanged(nameof(CreditUsageSummary));
                RaisePropertyChanged(nameof(CreditsUsedPercent));
                RaisePropertyChanged(nameof(IsCreditsExhausted));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load credit info");
            }
            
            await Task.CompletedTask;
        }

        private void ExecuteChangeDatabase(object? parameter)
        {
            var owner = Win32UtilHelper.GetMainWindow();
            var result = MessageBoxHelper.Show(
                "Changing your database connection will require restarting the application.\n\n" +
                "If you have an existing database, you'll be asked if you want to copy it to the new location.\n\n" +
                "Do you want to continue?",
                "Change Database Connection",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Mark setup as not completed so the wizard shows on next launch
            UserSettingsManager.Instance.Settings.Database.SetupCompleted = false;
            UserSettingsManager.Instance.SaveSettings();

            // Ask if they want to restart now
            var restartResult = MessageBoxHelper.Show(
                "Settings saved. The database setup wizard will appear next time you log in.\n\n" +
                "Would you like to restart Tracker now?",
                "Restart Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                owner);

            if (restartResult == MessageBoxResult.Yes)
            {
                // Restart the application
                RestartApplication();
            }
        }

        /// <summary>
        /// Restarts the application.
        /// </summary>
        private void RestartApplication()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                var logger = Logging.LoggingManager.GetComponentLogger("SettingsVM");
                logger.Exception(ex, "Failed to restart application");
                var owner = Win32UtilHelper.GetMainWindow();
                MessageBoxHelper.Show(
                    "Unable to restart automatically. Please close and reopen Tracker manually.",
                    "Restart Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    owner);
            }
        }

        private async void ExecuteClearData(object? parameter)
        {
            var owner = Win32UtilHelper.GetMainWindow();
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

                // Also send through the new DataMessenger system for ViewModels using it
                DataMessenger.SendRefreshAll();

                NotificationManager.Instance.ShowSuccess("Data Cleared", "All data has been removed from the database.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to clear data. Check the logs for details.");
            }
        }

        private async void ExecuteSeedSampleData(object? parameter)
        {
            // Check if database already has data - use forceReseed=true if data exists
            var hasExistingData = await TrackerDbManager.Instance!.HasDataAsync();
            
            string message;
            bool forceReseed = hasExistingData; // Always use forceReseed if data exists
            
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

            // Get the Settings dialog window as owner, not MainWindow (which might cause issues)
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            var result = MessageBoxHelper.Show(
                message,
                forceReseed ? "Replace Data with Sample Data" : "Add Sample Data",
                MessageBoxButton.YesNo,
                forceReseed ? MessageBoxImage.Warning : MessageBoxImage.Question,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Re-check for data right before seeding (in case user cleared data between dialog and now)
            // Always use forceReseed=true if ANY data exists to ensure clean seeding
            var hasDataBeforeSeeding = await TrackerDbManager.Instance!.HasDataAsync();
            if (hasDataBeforeSeeding)
            {
                forceReseed = true; // Force reseed if data exists, regardless of original check
            }
            
            try
            {
                var success = await TrackerDbManager.Instance!.SeedSampleDataAsync(forceReseed);
                
                if (success)
                {
                    _logger.Info("Sample data seeded successfully. Sending refresh messages...");
                    
                    // Publish a message to refresh all data in the main ViewModel
                    Messenger.Publish(new PropertyChangedMessage
                    {
                        ChangedProperty = PropertyChangedEnum.All,
                        RefreshData = true
                    });

                    // Also send through the new DataMessenger system for ViewModels using it
                    _logger.Debug("Calling DataMessenger.SendRefreshAll()");
                    DataMessenger.SendRefreshAll();
                    _logger.Debug("DataMessenger.SendRefreshAll() completed");

                    NotificationManager.Instance.ShowSuccess(
                        "Sample Data Added", 
                        forceReseed 
                            ? "All data has been replaced with fresh sample data." 
                            : "Sample data has been added to the database.");
                }
                else
                {
                    // Check if it failed because data already exists
                    var hasDataAfterFailure = await TrackerDbManager.Instance!.HasDataAsync();
                    if (hasDataAfterFailure && !forceReseed)
                    {
                        NotificationManager.Instance.ShowWarning(
                            "Data Already Exists", 
                            "Your database already contains data. Use 'Replace Data with Sample Data' to clear and reseed.");
                    }
                    else
                    {
                        NotificationManager.Instance.ShowError("Error", "Failed to add sample data. The database may already contain data or there was an issue during seeding.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Show the actual error message to the user
                var errorMessage = ex.InnerException != null 
                    ? $"{ex.Message}\n\nDetails: {ex.InnerException.Message}"
                    : ex.Message;
                    
                NotificationManager.Instance.ShowError(
                    "Error Adding Sample Data", 
                    $"Failed to add sample data:\n\n{errorMessage}\n\nCheck the log file for more details:\n{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Tracker\\Logs\\Tracker.log");
            }
        }

        private static Brush GetThemePreviewColor(DeepEndTheme theme)
        {
            // Use our custom bronze gold for Tracker theme
            if (theme == DeepEndTheme.Tracker)
            {
                var brush = new SolidColorBrush(Color.FromRgb(0xC7, 0xA4, 0x4F));
                brush.Freeze();
                return brush;
            }
            
            var palette = ThemePalette.GetPalette(theme);
            return palette.PrimaryBrush;
        }

        private void ExecuteResetSetup(object? parameter)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            var result = MessageBoxHelper.Show(
                "Re-run Setup Wizard?\n\n" +
                "The application will restart and show the setup wizard.\n" +
                "Your existing data will NOT be deleted.\n\n" +
                "Do you want to continue?",
                "Re-run Setup Wizard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Reset setup completed flag
            UserSettingsManager.Instance.Settings.Database.SetupCompleted = false;
            UserSettingsManager.Instance.SaveSettings();

            // Inform user and offer to restart
            MessageBoxHelper.Show(
                "Please restart Tracker to run the setup wizard.",
                "Restart Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                owner);
        }

        private void ExecuteResetSettings(object? parameter)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            var result = MessageBoxHelper.Show(
                "⚠️ Reset All Settings?\n\n" +
                "This will:\n" +
                "• Reset all preferences to defaults\n" +
                "• Clear calendar connections\n" +
                "• Reset authentication settings\n" +
                "• Show setup wizard on next startup\n\n" +
                "Your DATABASE and DATA will NOT be affected.\n\n" +
                "The application will need to restart.\n\n" +
                "Do you want to continue?",
                "Reset All Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Delete settings file
                var settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tracker",
                    "TrackerSettings.json");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    System.IO.File.Delete(settingsPath);
                }

                MessageBoxHelper.Show(
                    "Settings have been reset.\n\nPlease restart Tracker.",
                    "Settings Reset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    owner);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.Show(
                    $"Failed to reset settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    owner);
            }
        }

        private async Task ExecuteRefreshAiKnowledgeAsync(object? parameter)
        {
            IsRefreshingAi = true;
            AiRefreshStatus = "Deleting old vector database...";

            try
            {
                // Delete the vector database file
                var vectorDbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tracker",
                    "vectors.db");

                if (System.IO.File.Exists(vectorDbPath))
                {
                    System.IO.File.Delete(vectorDbPath);
                }

                AiRefreshStatus = "Re-indexing documentation...";

                // Re-initialize the vector store and re-index all documents
                await Services.AI.VectorStore.Instance.InitializeAsync();
                await Services.AI.DocumentIndexer.Instance.ReindexAllAsync();

                AiRefreshStatus = "✅ AI knowledge base updated successfully!";
                NotificationManager.Instance.ShowSuccess(
                    "AI Knowledge Updated",
                    "The AI Help Bot's knowledge base has been refreshed with the latest documentation.");

                // Clear status after a few seconds
                await Task.Delay(3000);
                AiRefreshStatus = string.Empty;
            }
            catch (Exception ex)
            {
                AiRefreshStatus = $"❌ Error: {ex.Message}";
                NotificationManager.Instance.ShowError(
                    "Refresh Failed",
                    $"Failed to refresh AI knowledge base: {ex.Message}");
            }
            finally
            {
                IsRefreshingAi = false;
            }
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
