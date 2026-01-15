using Microsoft.Extensions.DependencyInjection;
using Tracker.DataModels;
using Tracker.Factories;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Data;
using Tracker.Services.Data.Repositories;
using Tracker.Help.ViewModels;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Infrastructure
{
    /// <summary>
    /// Configures dependency injection services for the application.
    /// All data access uses Dapper repositories against Supabase PostgreSQL.
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
            // ===== DAPPER DATA ACCESS LAYER =====
            // These repositories use Dapper directly against Supabase PostgreSQL.
            // This is the ONLY place database operations happen - never directly in ViewModels or Services.
            services.AddSingleton<IDapperConnectionFactory, DapperConnectionFactory>();
            
            // Core entity repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
            services.AddScoped<IMeetingRepository, MeetingRepository>();
            services.AddScoped<IMeetingTemplateRepository, MeetingTemplateRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IGoalRepository, GoalRepository>();
            services.AddScoped<ITargetRepository, TargetRepository>();
            services.AddScoped<IMetricRepository, MetricRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IQuickNoteRepository, QuickNoteRepository>();
            services.AddScoped<IDevelopmentGoalRepository, DevelopmentGoalRepository>();
            services.AddScoped<IPulseSurveyRepository, PulseSurveyRepository>();
            services.AddScoped<IInsightRepository, InsightRepository>();
            services.AddScoped<IKudosRepository, KudosRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();
            services.AddScoped<ITaskCollectionRepository, TaskCollectionRepository>();
            // ALL 17 DAPPER REPOSITORIES REGISTERED

            // ===== BUSINESS LOGIC SERVICES LAYER =====
            // High-level services that wrap repositories
            // ViewModels inject these instead of repositories directly
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITeamMemberService, TeamMemberService>();
            services.AddScoped<IMeetingService, MeetingService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IGoalService, GoalService>();
            services.AddScoped<IMetricService, MetricService>();
            // TODO: Add remaining services (FeedbackService, ProjectService, etc.) as needed

            // Register logging
            services.AddSingleton<ILogger>(_ => LoggingManager.GetComponentLogger("DI"));

            // Register singleton managers (already exist as singletons)
            services.AddSingleton(_ => TrackerDataManager.Instance);
            services.AddSingleton(_ => UserSettingsManager.Instance);
            services.AddSingleton(_ => NotificationManager.Instance);
            services.AddSingleton(_ => ThemeManager.Instance);
            services.AddSingleton(_ => CalendarSyncManager.Instance);

            // Register services
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<IReminderService>(_ => ReminderService.Instance);

            // Register ViewModel factory
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // Register ViewModels as transient (new instance each time)
            // Main ViewModels
            services.AddTransient<TrackerMainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<InsightPanelViewModel>();
            services.AddTransient<SearchViewModel>();
            services.AddTransient<GoalsViewModel>();
            services.AddTransient<QuickNotesViewModel>();
            services.AddTransient<HelpBotViewModel>();
            services.AddTransient<PulseSurveysViewModel>();
            services.AddTransient<LogViewerViewModel>();

            // ViewModels without callbacks
            services.AddTransient<HelpViewModel>();
            services.AddTransient<AdminWindowViewModel>();

            // Dialog ViewModels - these need callbacks, so we use factory methods
            services.AddTransient<Func<Action?, SettingsViewModel>>(sp => callback => new SettingsViewModel(callback));
            services.AddTransient<Func<Action?, ReportsViewModel>>(sp => callback => new ReportsViewModel(callback));
            services.AddTransient<Func<Action?, SetupWizardViewModel>>(sp => callback => new SetupWizardViewModel(callback));
            services.AddTransient<Func<Action?, LoginDialogViewModel>>(sp => callback => new LoginDialogViewModel(callback));
            services.AddTransient<Func<Action, QuickMessageViewModel>>(sp => callback => new QuickMessageViewModel(callback));

            return services;
        }
    }
}
