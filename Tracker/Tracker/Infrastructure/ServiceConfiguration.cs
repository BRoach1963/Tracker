using Microsoft.Extensions.DependencyInjection;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Factories;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
//using Tracker.Services.TeamHealth;
using Tracker.Help.ViewModels;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Infrastructure
{
    /// <summary>
    /// Configures dependency injection services for the application.
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configures all services and ViewModels for dependency injection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            // Register logging
            services.AddSingleton<ILogger>(_ => LoggingManager.GetComponentLogger("DI"));

            // Register singleton managers (already exist as singletons)
            services.AddSingleton(_ => TrackerDataManager.Instance);
            services.AddSingleton(_ => TrackerDbManager.Instance);
            services.AddSingleton(_ => UserSettingsManager.Instance);
            services.AddSingleton(_ => NotificationManager.Instance);
            services.AddSingleton(_ => ThemeManager.Instance);
            services.AddSingleton(_ => CalendarSyncManager.Instance);

            // Register services
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<IReminderService>(_ => ReminderService.Instance);
            //services.AddSingleton<ITeamHealthService, TeamHealthService>();
            services.AddSingleton<IMeasurableService, MeasurableService>();
            services.AddSingleton<IKpiCalculationService, KpiCalculationService>();
            services.AddSingleton<IOkrProgressService, OkrProgressService>();

            // Register ViewModel factory
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // Register ViewModels as transient (new instance each time)
            // Main ViewModels
            services.AddTransient<TrackerMainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<InsightPanelViewModel>();
            services.AddTransient<SearchViewModel>();
            services.AddTransient<OkrsViewModel>();
            services.AddTransient<QuickNotesViewModel>();
            services.AddTransient<HelpBotViewModel>();
            services.AddTransient<PulseSurveysViewModel>();
            services.AddTransient<PerformanceReviewsViewModel>();
            services.AddTransient<LogViewerViewModel>();

            // ViewModels without callbacks
            services.AddTransient<HelpViewModel>();
            services.AddTransient<AdminWindowViewModel>();

            // Dialog ViewModels - these need callbacks, so we use factory methods
            services.AddTransient<Func<Action?, SettingsViewModel>>(sp => callback => new SettingsViewModel(callback));
            services.AddTransient<Func<Action?, ReportsViewModel>>(sp => callback => new ReportsViewModel(callback));
            services.AddTransient<Func<Action?, SetupWizardViewModel>>(sp => callback => new SetupWizardViewModel(callback));
            services.AddTransient<Func<Action?, LoginDialogViewModel>>(sp => callback => new LoginDialogViewModel(callback));
            //services.AddTransient<Func<Action, TeamMember?, SendKudosViewModel>>(sp => (callback, member) => new SendKudosViewModel(callback, member));
            services.AddTransient<Func<Action, QuickMessageViewModel>>(sp => callback => new QuickMessageViewModel(callback));

            return services;
        }
    }
}
