using Microsoft.Extensions.DependencyInjection;
using Tracker.Database;
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

// Aliases for EF Core repositories that still need to be registered
// These are the OLD repositories - will be removed once all ViewModels migrate to services
using EfMeetingRepository = Tracker.Database.Repositories.MeetingRepository;
using EfMetricRepository = Tracker.Database.Repositories.MetricRepository;
using EfTrackerTaskRepository = Tracker.Database.Repositories.TrackerTaskRepository;
using EfTargetRepository = Tracker.Database.Repositories.TargetRepository;
using EfKudosRepository = Tracker.Database.Repositories.KudosRepository;
using EfGoalRepository = Tracker.Database.Repositories.GoalRepository;
using EfTeamMemberRepository = Tracker.Database.Repositories.TeamMemberRepository;
using EfDevelopmentGoalRepository = Tracker.Database.Repositories.DevelopmentGoalRepository;
using EfProjectRepository = Tracker.Database.Repositories.ProjectRepository;
using EfFeedbackRepository = Tracker.Database.Repositories.FeedbackRepository;
using EfReminderRepository = Tracker.Database.Repositories.ReminderRepository;
using EfReviewTemplateRepository = Tracker.Database.Repositories.ReviewTemplateRepository;
using EfReviewCycleRepository = Tracker.Database.Repositories.ReviewCycleRepository;
using EfPerformanceReviewRepository = Tracker.Database.Repositories.PerformanceReviewRepository;
using EfMeetingTemplateRepository = Tracker.Database.Repositories.MeetingTemplateRepository;
using EfQuickNoteRepository = Tracker.Database.Repositories.QuickNoteRepository;
using EfPulseSurveyRepository = Tracker.Database.Repositories.PulseSurveyRepository;

// EF Core Repository Interfaces (until migration complete)
using IEfMeetingRepository = Tracker.Database.Repositories.IMeetingRepository;
using IEfMetricRepository = Tracker.Database.Repositories.IMetricRepository;
using IEfTrackerTaskRepository = Tracker.Database.Repositories.ITrackerTaskRepository;
using IEfTargetRepository = Tracker.Database.Repositories.ITargetRepository;
using IEfKudosRepository = Tracker.Database.Repositories.IKudosRepository;
using IEfGoalRepository = Tracker.Database.Repositories.IGoalRepository;
using IEfTeamMemberRepository = Tracker.Database.Repositories.ITeamMemberRepository;
using IEfDevelopmentGoalRepository = Tracker.Database.Repositories.IDevelopmentGoalRepository;
using IEfProjectRepository = Tracker.Database.Repositories.IProjectRepository;
using IEfFeedbackRepository = Tracker.Database.Repositories.IFeedbackRepository;
using IEfReminderRepository = Tracker.Database.Repositories.IReminderRepository;
using IEfReviewTemplateRepository = Tracker.Database.Repositories.IReviewTemplateRepository;
using IEfReviewCycleRepository = Tracker.Database.Repositories.IReviewCycleRepository;
using IEfPerformanceReviewRepository = Tracker.Database.Repositories.IPerformanceReviewRepository;
using IEfMeetingTemplateRepository = Tracker.Database.Repositories.IMeetingTemplateRepository;
using IEfQuickNoteRepository = Tracker.Database.Repositories.IQuickNoteRepository;
using IEfPulseSurveyRepository = Tracker.Database.Repositories.IPulseSurveyRepository;

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
            services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
            services.AddScoped<IMeetingRepository, MeetingRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IGoalRepository, GoalRepository>();
            services.AddScoped<IMetricRepository, MetricRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IQuickNoteRepository, QuickNoteRepository>();
            services.AddScoped<IDevelopmentGoalRepository, DevelopmentGoalRepository>();
            services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
            services.AddScoped<IPulseSurveyRepository, PulseSurveyRepository>();
            // ALL 12 GOLD STANDARD REPOSITORIES REGISTERED - PHASE 2 COMPLETE

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
            services.AddSingleton<IMeasurableService, MeasurableService>();
            services.AddSingleton<IMetricCalculationService, MetricCalculationService>();
            services.AddSingleton<IGoalProgressService, GoalProgressService>();

            // ===== LEGACY EF CORE REPOSITORIES =====
            // These are the OLD repositories still needed by ViewModels not yet migrated.
            // TODO: Remove these once all ViewModels migrate to Dapper services.
            services.AddScoped<IEfMeetingRepository>(sp => 
                new EfMeetingRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfMetricRepository>(sp => 
                new EfMetricRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfTrackerTaskRepository>(sp => 
                new EfTrackerTaskRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfTargetRepository>(sp => 
                new EfTargetRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfKudosRepository>(sp => 
                new EfKudosRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfGoalRepository>(sp => 
                new EfGoalRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfTeamMemberRepository>(sp => 
                new EfTeamMemberRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfDevelopmentGoalRepository>(sp => 
                new EfDevelopmentGoalRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfProjectRepository>(sp => 
                new EfProjectRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfFeedbackRepository>(sp => 
                new EfFeedbackRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfReminderRepository>(sp => 
                new EfReminderRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfReviewTemplateRepository>(sp => 
                new EfReviewTemplateRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfReviewCycleRepository>(sp => 
                new EfReviewCycleRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfPerformanceReviewRepository>(sp => 
                new EfPerformanceReviewRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfMeetingTemplateRepository>(sp => 
                new EfMeetingTemplateRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfQuickNoteRepository>(sp => 
                new EfQuickNoteRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));
            services.AddScoped<IEfPulseSurveyRepository>(sp => 
                new EfPulseSurveyRepository(sp.GetRequiredService<TrackerDbContext>(), GetCurrentUserId(), GetContextFactory(sp)));

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
