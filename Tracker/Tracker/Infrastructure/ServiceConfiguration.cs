using Microsoft.Extensions.DependencyInjection;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Factories;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Data;
using Tracker.Services.Data.Repositories;
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
            // ===== DAPPER DATA ACCESS LAYER =====
            // These are the NEW repositories using Dapper directly against Supabase.
            // This is the ONLY place database operations happen - never directly in ViewModels or Services.
            services.AddScoped<IDapperConnectionFactory, DapperConnectionFactory>();
            services.AddScoped<IUserRepository, UserRepository>();
            // TODO: Add remaining repositories as they are created

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
            //services.AddSingleton<ITeamHealthService, TeamHealthService>();
            services.AddSingleton<IMeasurableService, MeasurableService>();
            services.AddSingleton<IMetricCalculationService, MetricCalculationService>();
            services.AddSingleton<IGoalProgressService, GoalProgressService>();

            // Register repositories (scoped - one per request/operation)
            services.AddScoped<IMeetingRepository>(sp => 
                new MeetingRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IMetricRepository>(sp => 
                new MetricRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<ITrackerTaskRepository>(sp => 
                new TrackerTaskRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<ITargetRepository>(sp => 
                new TargetRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IKudosRepository>(sp => 
                new KudosRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IGoalRepository>(sp => 
                new GoalRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<ITeamMemberRepository>(sp => 
                new TeamMemberRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IDevelopmentGoalRepository>(sp => 
                new DevelopmentGoalRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IProjectRepository>(sp => 
                new ProjectRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IFeedbackRepository>(sp => 
                new FeedbackRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IReminderRepository>(sp => 
                new ReminderRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IReviewTemplateRepository>(sp => 
                new ReviewTemplateRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IReviewCycleRepository>(sp => 
                new ReviewCycleRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IPerformanceReviewRepository>(sp => 
                new PerformanceReviewRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IMeetingTemplateRepository>(sp => 
                new MeetingTemplateRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IQuickNoteRepository>(sp => 
                new QuickNoteRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IPulseSurveyRepository>(sp => 
                new PulseSurveyRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));

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

        private static Guid GetCurrentUserId()
        {
            // Prefer the scoped organization/user context when available
            var context = OrganizationContext.Current;
            return context.UserIdOrNull ?? Guid.Empty;
        }

        /// <summary>
        /// Creates a context factory for PostgreSQL support.
        /// </summary>
        private static Func<TrackerDbContext> GetContextFactory(IServiceProvider sp)
        {
            // Return a factory that creates new contexts if needed (for PostgreSQL scenarios)
            return () => sp.GetRequiredService<TrackerDbContext>();
        }
    }
}
