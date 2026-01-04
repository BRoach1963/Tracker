using System.IO;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for the Tracker application.
    /// 
    /// This context supports dual database providers:
    /// - SQLite for local/standalone deployments
    /// - SQL Server for networked/enterprise deployments
    /// 
    /// Key features:
    /// - Automatic audit field population (CreatedAt, ModifiedAt, etc.)
    /// - Soft delete support (IsDeleted flag instead of hard deletes)
    /// - Optimistic concurrency via RowVersion (SQL Server only)
    /// - Global query filters for UserId and IsDeleted (automatic data isolation)
    /// - Change tracking table for future offline sync capabilities
    /// 
    /// Usage:
    /// <code>
    /// var settings = new DatabaseSettings { Type = DatabaseType.SQLite };
    /// using var context = new TrackerDbContext(settings);
    /// context.EnsureCreated();
    /// </code>
    /// </summary>
    public class TrackerDbContext : DbContext
    {
        private readonly DatabaseSettings _settings;
        private readonly Guid? _postgresUserId;
        
        /// <summary>
        /// Gets or sets the current user ID for query filtering.
        /// When set, global query filters will automatically filter data by this user.
        /// Set to null to disable user filtering (for admin/seeding operations).
        /// Used for SQLite and SQL Server databases.
        /// </summary>
        public int? CurrentUserId { get; set; }

        /// <summary>
        /// Gets the PostgreSQL user ID (Guid) for RLS filtering.
        /// This is set via constructor for PostgreSQL databases.
        /// </summary>
        public Guid? PostgresUserId => _postgresUserId;

        #region Constructors

        /// <summary>
        /// Creates a new database context with the specified settings.
        /// </summary>
        /// <param name="settings">Database connection settings (SQLite, SQL Server, or PostgreSQL).</param>
        public TrackerDbContext(DatabaseSettings settings)
        {
            _settings = settings;
            // Initialize CurrentUserId from UserSettingsManager for automatic query filtering (SQLite/SQL Server)
            CurrentUserId = UserSettingsManager.Instance?.CurrentUserId;
        }

        /// <summary>
        /// Creates a new database context with PostgreSQL settings and user ID for RLS.
        /// </summary>
        /// <param name="settings">Database connection settings (should be PostgreSQL).</param>
        /// <param name="userId">The user ID to set for Row-Level Security filtering.</param>
        public TrackerDbContext(DatabaseSettings settings, Guid userId)
        {
            _settings = settings;
            _postgresUserId = userId;
        }

        /// <summary>
        /// Creates a new database context with default SQLite settings.
        /// Used primarily for backwards compatibility and design-time tooling.
        /// </summary>
        public TrackerDbContext() : this(new DatabaseSettings { Type = DatabaseType.SQLite })
        {
        }

        #endregion

        #region DbSets - Entity Tables

        /// <summary>Users/managers who own all data in the system.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Team members/employees being tracked.</summary>
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;

        /// <summary>One-on-one meeting records.</summary>
        public DbSet<OneOnOne> OneOnOnes { get; set; } = null!;

        /// <summary>Projects being managed.</summary>
        public DbSet<Project> Projects { get; set; } = null!;

        /// <summary>Individual tasks assigned to team members.</summary>
        public DbSet<IndividualTask> Tasks { get; set; } = null!;

        /// <summary>Tasks created from 1:1 meetings.</summary>
        public DbSet<MeetingTask> MeetingTasks { get; set; } = null!;

        /// <summary>Agenda items for 1:1 meetings (topics, concerns, questions, etc.).</summary>
        public DbSet<AgendaItem> AgendaItems { get; set; } = null!;

        /// <summary>Objectives and Key Results (OKRs).</summary>
        public DbSet<ObjectiveKeyResult> ObjectiveKeyResults { get; set; } = null!;

        /// <summary>Key Performance Indicators (KPIs) - standalone metrics.</summary>
        public DbSet<KeyPerformanceIndicator> KeyPerformanceIndicators { get; set; } = null!;

        /// <summary>Key Results that belong to OKRs.</summary>
        public DbSet<KeyResult> KeyResults { get; set; } = null!;

        /// <summary>Links between Key Results and their measurable sources (KPI, Project, TaskCollection).</summary>
        public DbSet<KeyResultMeasurable> KeyResultMeasurables { get; set; } = null!;

        /// <summary>Data sources that feed KPI values.</summary>
        public DbSet<KpiDataSource> KpiDataSources { get; set; } = null!;

        /// <summary>Collections of tasks treated as single measurable units.</summary>
        public DbSet<TaskCollection> TaskCollections { get; set; } = null!;

        /// <summary>Links between TaskCollections and their tasks.</summary>
        public DbSet<TaskCollectionItem> TaskCollectionItems { get; set; } = null!;

        /// <summary>Project milestones.</summary>
        public DbSet<Milestone> Milestones { get; set; } = null!;

        /// <summary>Project risks.</summary>
        public DbSet<Risk> Risks { get; set; } = null!;

        /// <summary>Dependencies between projects.</summary>
        public DbSet<ProjectDependency> ProjectDependencies { get; set; } = null!;

        /// <summary>
        /// Change tracking entries for offline sync.
        /// Records all inserts/updates/deletes for later synchronization.
        /// </summary>
        public DbSet<ChangeTrackingEntry> ChangeTrackingEntries { get; set; } = null!;

        /// <summary>
        /// Links between OneOnOne meetings and existing IndividualTasks that were discussed.
        /// </summary>
        public DbSet<OneOnOneLinkedTask> OneOnOneLinkedTasks { get; set; } = null!;

        /// <summary>
        /// Links between OneOnOne meetings and existing ObjectiveKeyResults that were discussed.
        /// </summary>
        public DbSet<OneOnOneLinkedOkr> OneOnOneLinkedOkrs { get; set; } = null!;

        /// <summary>
        /// Links between OneOnOne meetings and existing KeyPerformanceIndicators that were discussed.
        /// </summary>
        public DbSet<OneOnOneLinkedKpi> OneOnOneLinkedKpis { get; set; } = null!;

        /// <summary>Feedback given to team members.</summary>
        public DbSet<Feedback> Feedbacks { get; set; } = null!;

        /// <summary>Individual goals for team members.</summary>
        public DbSet<IndividualGoal> IndividualGoals { get; set; } = null!;

        /// <summary>Milestones for individual goals.</summary>
        public DbSet<GoalMilestone> GoalMilestones { get; set; } = null!;

        /// <summary>Reminders and notifications.</summary>
        public DbSet<Reminder> Reminders { get; set; } = null!;

        /// <summary>Meeting templates for quick 1:1 setup.</summary>
        public DbSet<MeetingTemplate> MeetingTemplates { get; set; } = null!;

        /// <summary>Items within meeting templates.</summary>
        public DbSet<MeetingTemplateItem> MeetingTemplateItems { get; set; } = null!;

        /// <summary>Quick notes and journal entries.</summary>
        public DbSet<QuickNote> QuickNotes { get; set; } = null!;

        /// <summary>Links from agenda items to other entities (Task, OKR, KPI, Project).</summary>
        public DbSet<LinkedItem> LinkedItems { get; set; } = null!;

        // Pulse Survey entities
        /// <summary>Pulse surveys for team engagement measurement.</summary>
        public DbSet<PulseSurvey> PulseSurveys { get; set; } = null!;

        /// <summary>Questions within pulse surveys.</summary>
        public DbSet<PulseSurveyQuestion> PulseSurveyQuestions { get; set; } = null!;

        /// <summary>Survey responses from team members.</summary>
        public DbSet<PulseSurveyResponse> PulseSurveyResponses { get; set; } = null!;

        /// <summary>Individual answers within survey responses.</summary>
        public DbSet<PulseSurveyAnswer> PulseSurveyAnswers { get; set; } = null!;

        // Performance Review entities
        /// <summary>Review templates defining the structure of performance reviews.</summary>
        public DbSet<ReviewTemplate> ReviewTemplates { get; set; } = null!;

        /// <summary>Sections within review templates.</summary>
        public DbSet<ReviewTemplateSection> ReviewTemplateSections { get; set; } = null!;

        /// <summary>Questions within review template sections.</summary>
        public DbSet<ReviewTemplateQuestion> ReviewTemplateQuestions { get; set; } = null!;

        /// <summary>Performance review cycles (e.g., Q1 2024 Reviews).</summary>
        public DbSet<PerformanceReviewCycle> PerformanceReviewCycles { get; set; } = null!;

        /// <summary>Individual performance reviews for team members.</summary>
        public DbSet<PerformanceReview> PerformanceReviews { get; set; } = null!;

        /// <summary>Sections within performance reviews.</summary>
        public DbSet<PerformanceReviewSection> PerformanceReviewSections { get; set; } = null!;

        /// <summary>Answers to review questions.</summary>
        public DbSet<PerformanceReviewAnswer> PerformanceReviewAnswers { get; set; } = null!;

        // Recognition & Kudos
        /// <summary>Kudos and recognition sent to team members.</summary>
        public DbSet<Kudos> Kudos { get; set; } = null!;

        // Predictive Analytics
        /// <summary>Point-in-time snapshots of progress for trajectory analysis.</summary>
        public DbSet<ProgressSnapshot> ProgressSnapshots { get; set; } = null!;

        // Calendar Integration
        /// <summary>Links between Tracker meetings and external calendar events.</summary>
        public DbSet<CalendarLink> CalendarLinks { get; set; } = null!;

        /// <summary>Sync tokens for calendar delta synchronization.</summary>
        public DbSet<CalendarSyncToken> CalendarSyncTokens { get; set; } = null!;

        #endregion

        #region Configuration

        /// <summary>
        /// Configures the database provider based on settings.
        /// Called automatically by EF Core during context initialization.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Select the appropriate provider based on configuration
            switch (_settings.Type)
            {
                case DatabaseType.SQLite:
                    // SQLite stores data in a local file - great for single-user scenarios
                    // Use custom path if specified, otherwise use default
                    var sqlitePath = !string.IsNullOrWhiteSpace(_settings.CustomSqlitePath) 
                        ? _settings.CustomSqlitePath 
                        : DatabaseSettings.GetSqlitePath();
                    
                    // Ensure directory exists for custom paths
                    var directory = Path.GetDirectoryName(sqlitePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Enable foreign keys via connection string (SQLite has them disabled by default)
                    optionsBuilder.UseSqlite($"Data Source={sqlitePath};Foreign Keys=True");
                    break;

                case DatabaseType.SqlServer:
                    // SQL Server for multi-user/enterprise scenarios
                    optionsBuilder.UseSqlServer(_settings.GetConnectionString());
                    break;

                case DatabaseType.PostgreSQL:
                    // PostgreSQL with Row-Level Security (RLS)
                    // The RLS interceptor sets app.current_user_id on connection open
                    optionsBuilder.UseNpgsql(_settings.GetConnectionString());
                    
                    // Add RLS interceptor if we have a current user ID
                    if (_postgresUserId.HasValue)
                    {
                        optionsBuilder.AddInterceptors(
                            new Interceptors.RlsConnectionInterceptor(_postgresUserId.Value));
                    }
                    break;
            }

            // Enable detailed logging in debug builds for troubleshooting
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }

        /// <summary>
        /// Configures entity relationships, constraints, and mappings.
        /// This is where we define how entities relate to each other and map to database tables.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Apply global query filters for automatic data isolation
            // These filters are applied at the SQL level for maximum performance
            ConfigureGlobalQueryFilters(modelBuilder);

            // Apply common audit configuration to all auditable entities
            ConfigureAuditableEntities(modelBuilder);

            // Configure each entity type with its specific relationships and constraints
            ConfigureUser(modelBuilder);
            ConfigureTeamMember(modelBuilder);
            ConfigureOneOnOne(modelBuilder);
            ConfigureProject(modelBuilder);
            ConfigureIndividualTask(modelBuilder);
            ConfigureMeetingTask(modelBuilder);
            ConfigureAgendaItem(modelBuilder);
            ConfigureObjectiveKeyResult(modelBuilder);
            ConfigureKeyResult(modelBuilder);
            ConfigureKeyResultMeasurable(modelBuilder);
            ConfigureKeyPerformanceIndicator(modelBuilder);
            ConfigureKpiDataSource(modelBuilder);
            ConfigureTaskCollection(modelBuilder);
            ConfigureTaskCollectionItem(modelBuilder);
            ConfigureMilestone(modelBuilder);
            ConfigureRisk(modelBuilder);
            ConfigureProjectDependency(modelBuilder);
            ConfigureChangeTracking(modelBuilder);
            ConfigureOneOnOneLinkedEntities(modelBuilder);
            ConfigureFeedback(modelBuilder);
            ConfigureIndividualGoal(modelBuilder);
            ConfigureGoalMilestone(modelBuilder);
            ConfigureReminder(modelBuilder);
            ConfigureMeetingTemplate(modelBuilder);
            ConfigureQuickNote(modelBuilder);
            ConfigureLinkedItem(modelBuilder);
            
            // Pulse Survey configuration
            ConfigurePulseSurvey(modelBuilder);
            ConfigurePulseSurveyQuestion(modelBuilder);
            ConfigurePulseSurveyResponse(modelBuilder);
            ConfigurePulseSurveyAnswer(modelBuilder);
            
            // Performance Review configuration
            ConfigureReviewTemplate(modelBuilder);
            ConfigureReviewTemplateSection(modelBuilder);
            ConfigureReviewTemplateQuestion(modelBuilder);
            ConfigurePerformanceReviewCycle(modelBuilder);
            ConfigurePerformanceReview(modelBuilder);
            ConfigurePerformanceReviewSection(modelBuilder);
            ConfigurePerformanceReviewAnswer(modelBuilder);
            
            // Recognition & Kudos configuration
            ConfigureKudos(modelBuilder);
            
            // Predictive Analytics configuration
            ConfigureProgressSnapshot(modelBuilder);
            
            // Calendar Integration configuration
            ConfigureCalendarLink(modelBuilder);
            ConfigureCalendarSyncToken(modelBuilder);
        }

        #endregion
        
        #region Global Query Filters
        
        /// <summary>
        /// Configures global query filters for automatic data isolation.
        /// These filters are applied at the SQL level for maximum performance.
        /// 
        /// Filters applied:
        /// - UserId: Only returns data belonging to CurrentUserId (when set)
        /// - IsDeleted: Automatically excludes soft-deleted records
        /// 
        /// To bypass filters, use .IgnoreQueryFilters() on the query.
        /// </summary>
        private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // TeamMember: Filter by UserId and IsDeleted
            modelBuilder.Entity<TeamMember>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // OneOnOne: Filter by UserId and IsDeleted
            modelBuilder.Entity<OneOnOne>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // Project: Filter by UserId and IsDeleted
            modelBuilder.Entity<Project>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // IndividualTask: Filter by UserId and IsDeleted
            modelBuilder.Entity<IndividualTask>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // MeetingTask: Filter by UserId and IsDeleted
            modelBuilder.Entity<MeetingTask>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // AgendaItem: Filter by UserId and IsDeleted
            modelBuilder.Entity<AgendaItem>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // ObjectiveKeyResult (OKR): Filter by UserId and IsDeleted
            modelBuilder.Entity<ObjectiveKeyResult>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // KeyResult: Filter by UserId and IsDeleted
            modelBuilder.Entity<KeyResult>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // KeyPerformanceIndicator (KPI): Filter by UserId and IsDeleted
            modelBuilder.Entity<KeyPerformanceIndicator>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // KeyResultMeasurable: Filter by UserId and IsDeleted
            modelBuilder.Entity<KeyResultMeasurable>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // KpiDataSource: Filter by IsDeleted only (no UserId on this entity)
            modelBuilder.Entity<KpiDataSource>().HasQueryFilter(e => !e.IsDeleted);
            
            // TaskCollection: Filter by UserId and IsDeleted
            modelBuilder.Entity<TaskCollection>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // TaskCollectionItem: Filter by UserId and IsDeleted
            modelBuilder.Entity<TaskCollectionItem>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // Milestone: Filter by UserId and IsDeleted
            modelBuilder.Entity<Milestone>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // Risk: Filter by UserId and IsDeleted
            modelBuilder.Entity<Risk>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // Feedback: Filter by UserId and IsDeleted
            modelBuilder.Entity<Feedback>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // IndividualGoal: Filter by UserId and IsDeleted
            modelBuilder.Entity<IndividualGoal>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // GoalMilestone: Filter by UserId and IsDeleted
            modelBuilder.Entity<GoalMilestone>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // Reminder: Filter by UserId and IsDeleted
            modelBuilder.Entity<Reminder>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // MeetingTemplate: Filter by UserId and IsDeleted
            modelBuilder.Entity<MeetingTemplate>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // MeetingTemplateItem: Filter by UserId and IsDeleted
            modelBuilder.Entity<MeetingTemplateItem>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // QuickNote: Filter by UserId and IsDeleted
            modelBuilder.Entity<QuickNote>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // LinkedItem: No filter (simple entity without audit fields, filtered via parent AgendaItem)
            
            // OneOnOneLinkedTask/Okr/Kpi: Filter by IsDeleted only (userId filtering via parent OneOnOne)
            modelBuilder.Entity<OneOnOneLinkedTask>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<OneOnOneLinkedOkr>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<OneOnOneLinkedKpi>().HasQueryFilter(e => !e.IsDeleted);
            
            // ProjectDependency: Filter by IsDeleted only (userId filtering via parent Project)
            modelBuilder.Entity<ProjectDependency>().HasQueryFilter(e => !e.IsDeleted);
            
            // Pulse Survey entities
            modelBuilder.Entity<PulseSurvey>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<PulseSurveyQuestion>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<PulseSurveyResponse>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // PulseSurveyAnswer: No filter (simple value object, filtered via parent Response cascade)
            
            // Performance Review entities
            modelBuilder.Entity<ReviewTemplate>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<ReviewTemplateSection>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<ReviewTemplateQuestion>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<PerformanceReviewCycle>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            modelBuilder.Entity<PerformanceReview>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
            
            // PerformanceReviewSection, PerformanceReviewAnswer: No filter (simple value objects, filtered via parent cascade)
            
            // Kudos: Filter by UserId and IsDeleted
            modelBuilder.Entity<Kudos>().HasQueryFilter(e => 
                !e.IsDeleted && (CurrentUserId == null || EF.Property<int>(e, "UserId") == CurrentUserId));
        }
        
        #endregion

        #region Entity Configurations

        /// <summary>
        /// Applies audit field configuration to all entities that inherit from AuditableEntity.
        /// This includes CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, and soft delete fields.
        /// </summary>
        private void ConfigureAuditableEntities(ModelBuilder modelBuilder)
        {
            // Find all entity types that inherit from AuditableEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // Configure default values for audit timestamps
                    // Uses database-specific functions for UTC time
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>("CreatedAt")
                        .HasDefaultValueSql(GetUtcDateFunction());
                    
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>("LastModifiedAt")
                        .HasDefaultValueSql(GetUtcDateFunction());

                    // Set max lengths for user name fields
                    modelBuilder.Entity(entityType.ClrType).Property<string>("CreatedBy").HasMaxLength(100);
                    modelBuilder.Entity(entityType.ClrType).Property<string>("LastModifiedBy").HasMaxLength(100);
                    modelBuilder.Entity(entityType.ClrType).Property<string>("DeletedBy").HasMaxLength(100);
                    
                    // Configure row version for optimistic concurrency (SQL Server only)
                    // This helps detect conflicts when multiple users edit the same record
                    if (_settings.Type == DatabaseType.SqlServer)
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property<byte[]>("RowVersion")
                            .IsRowVersion();
                    }

                    // Index on IsDeleted for efficient soft-delete queries
                    // Most queries will filter on IsDeleted = false
                    modelBuilder.Entity(entityType.ClrType).HasIndex("IsDeleted");
                }
            }
        }

        /// <summary>
        /// Gets the database-specific function for current UTC timestamp.
        /// </summary>
        private string GetUtcDateFunction()
        {
            return _settings.Type == DatabaseType.SqlServer 
                ? "GETUTCDATE()" 
                : "datetime('now')";
        }

        /// <summary>
        /// Configures the User entity - the logged-in manager who owns all data.
        /// </summary>
        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Set reasonable max lengths for string fields
                entity.Property(e => e.Username).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasMaxLength(200);

                // Unique constraint on Username (one user per username)
                entity.HasIndex(e => e.Username).IsUnique();
                
                // Index for querying active users
                entity.HasIndex(e => e.IsActive);
            });
        }

        /// <summary>
        /// Configures the TeamMember entity - the core entity representing employees.
        /// </summary>
        private void ConfigureTeamMember(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Set reasonable max lengths for string fields
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.NickName).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.CellPhone).HasMaxLength(20);
                entity.Property(e => e.JobTitle).HasMaxLength(100);
                entity.Property(e => e.LinkedInProfile).HasMaxLength(500);
                entity.Property(e => e.FacebookProfile).HasMaxLength(500);
                entity.Property(e => e.InstagramProfile).HasMaxLength(500);
                entity.Property(e => e.XProfile).HasMaxLength(500);

                // Relationship: Each TeamMember belongs to one User (the manager who owns them)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete team members if user is deleted

                // Indexes for common query patterns
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.LastName, e.FirstName });
                entity.HasIndex("UserId"); // Index for filtering by User
            });
        }

        /// <summary>
        /// Configures the OneOnOne entity - meeting records between managers and team members.
        /// </summary>
        private void ConfigureOneOnOne(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OneOnOne>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Agenda).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(4000);
                entity.Property(e => e.Feedback).HasMaxLength(4000);
                entity.Property(e => e.GoogleCalendarEventId).HasMaxLength(200);
                entity.Property(e => e.CalendarEventId).HasMaxLength(200);
                
                // TeamMemberName is computed from TeamMember navigation property
                entity.Ignore(e => e.TeamMemberName);
                
                // User ownership: Each 1:1 belongs to one User (the manager conducting the meeting)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Relationship: Each 1:1 is with one TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey("TeamMemberId")
                    .OnDelete(DeleteBehavior.SetNull); // Don't delete 1:1s if team member is deleted

                // Index for querying by date
                entity.HasIndex(e => e.Date);
                entity.HasIndex("UserId"); // Index for filtering by User
            });
        }

        /// <summary>
        /// Configures the junction entities that link OneOnOne meetings to existing Tasks, OKRs, and KPIs.
        /// </summary>
        private void ConfigureOneOnOneLinkedEntities(ModelBuilder modelBuilder)
        {
            // Configure OneOnOneLinkedTask
            modelBuilder.Entity<OneOnOneLinkedTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiscussionNotes).HasMaxLength(2000);

                // Many-to-one relationship: Many links can point to one OneOnOne
                entity.HasOne(e => e.OneOnOne)
                    .WithMany(o => o.LinkedTasks)
                    .HasForeignKey(e => e.OneOnOneId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete links when meeting is deleted

                // Many-to-one relationship: Many links can point to one Task
                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent deleting tasks that are linked

                // Unique constraint: A task can only be linked once per meeting
                entity.HasIndex(e => new { e.OneOnOneId, e.TaskId }).IsUnique();
            });

            // Configure OneOnOneLinkedOkr
            modelBuilder.Entity<OneOnOneLinkedOkr>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiscussionNotes).HasMaxLength(2000);

                entity.HasOne(e => e.OneOnOne)
                    .WithMany(o => o.LinkedOkrs)
                    .HasForeignKey(e => e.OneOnOneId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Okr)
                    .WithMany()
                    .HasForeignKey(e => e.OkrId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.OneOnOneId, e.OkrId }).IsUnique();
            });

            // Configure OneOnOneLinkedKpi
            modelBuilder.Entity<OneOnOneLinkedKpi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiscussionNotes).HasMaxLength(2000);

                entity.HasOne(e => e.OneOnOne)
                    .WithMany(o => o.LinkedKpis)
                    .HasForeignKey(e => e.OneOnOneId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Kpi)
                    .WithMany()
                    .HasForeignKey(e => e.KpiId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.OneOnOneId, e.KpiId }).IsUnique();
            });
        }

        /// <summary>
        /// Configures the Project entity with its complex relationships.
        /// </summary>
        private void ConfigureProject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Budget).HasPrecision(18, 2);
                
                // Interface and computed properties - ignore
                entity.Ignore(e => e.MeasurableId);
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.DisplayValue);
                entity.Ignore(e => e.MeasurableType);
                entity.Ignore(e => e.SourceId);
                entity.Ignore(e => e.SourceDisplayName);
                entity.Ignore(e => e.SourceType);
                entity.Ignore(e => e.TotalTasks);
                entity.Ignore(e => e.CompletedTasks);
                entity.Ignore(e => e.IncompleteTasks);
                entity.Ignore(e => e.IsOverdue);
                entity.Ignore(e => e.DaysRemaining);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Project owner relationship (TeamMember who manages the project)
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Many-to-many: Projects have multiple team members
                entity.HasMany(e => e.TeamMembers)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ProjectTeamMembers"));
                
                // Note: Tasks relationship is configured in ConfigureIndividualTask
                
                // Configure Milestones relationship
                entity.HasMany(e => e.Milestones)
                    .WithOne()
                    .HasForeignKey(m => m.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure Risks relationship
                entity.HasMany(e => e.Risks)
                    .WithOne()
                    .HasForeignKey(r => r.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure Dependencies relationship
                entity.HasMany(e => e.Dependencies)
                    .WithOne()
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures IndividualTask - tasks that can be standalone or belong to projects.
        /// </summary>
        private void ConfigureIndividualTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IndividualTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                // Computed properties - ignore
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.OwnerName);
                entity.Ignore(e => e.Type);
                entity.Ignore(e => e.MeetingCount);
                entity.Ignore(e => e.IsOverdue);
                entity.Ignore(e => e.DaysUntilDue);
                entity.Ignore(e => e.HasSubtasks);
                entity.Ignore(e => e.SubtaskProgress);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Task owner relationship (TeamMember who the task is assigned to)
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Optional link to Project
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(e => e.ProjectId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull); // Keep task if project is deleted
                
                // Self-referential relationship for subtasks
                entity.HasOne(e => e.ParentTask)
                    .WithMany(e => e.Subtasks)
                    .HasForeignKey(e => e.ParentTaskId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade); // Delete subtasks when parent is deleted

                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.IsCompleted);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.ParentTaskId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures MeetingTask - tasks that come out of 1:1 meetings.
        /// </summary>
        private void ConfigureMeetingTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MeetingTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.OwnerName);
                entity.Ignore(e => e.Type);
                
                // User ownership: Each MeetingTask belongs to one User (the manager's 1:1)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // MeetingTask belongs to a OneOnOne meeting
                entity.HasOne<OneOnOne>()
                    .WithMany(o => o.Tasks)
                    .HasForeignKey(e => e.OneOnOneId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // MeetingTask owner relationship (TeamMember who owns the task)
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.IsCompleted);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures AgendaItem - topics, concerns, questions discussed in 1:1 meetings.
        /// </summary>
        private void ConfigureAgendaItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgendaItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Resolution).HasMaxLength(2000);
                
                // Computed properties - ignore
                entity.Ignore(e => e.IsResolved);
                entity.Ignore(e => e.CategoryDisplay);
                entity.Ignore(e => e.HasLinkedItems);
                
                // User ownership: Each AgendaItem belongs to one User (the manager's 1:1)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // AgendaItem belongs to a OneOnOne meeting
                entity.HasOne<OneOnOne>()
                    .WithMany(o => o.AgendaItems)
                    .HasForeignKey(e => e.OneOnOneId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Optional link to a MeetingTask created from this item
                entity.HasOne<MeetingTask>()
                    .WithMany()
                    .HasForeignKey(e => e.LinkedTaskId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes for common queries
                entity.HasIndex(e => e.Category);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures ObjectiveKeyResult - OKRs that contain Key Results.
        /// </summary>
        private void ConfigureObjectiveKeyResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjectiveKeyResult>(entity =>
            {
                entity.HasKey(e => e.ObjectiveId);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // Computed properties - ignore
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.CompletionPercentage);
                entity.Ignore(e => e.MeetingCount);
                entity.Ignore(e => e.TimePeriodDisplay);
                entity.Ignore(e => e.KeyResultCount);
                entity.Ignore(e => e.HasKeyResults);
                entity.Ignore(e => e.LinkedKpiCount);
                entity.Ignore(e => e.LinkedProjectCount);
                entity.Ignore(e => e.LinkedTaskCollectionCount);
                entity.Ignore(e => e.IsActive);
                entity.Ignore(e => e.DaysRemaining);
                
                // User ownership: Each OKR belongs to one User (the manager who owns it)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // OKR owner relationship (TeamMember who owns the OKR)
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // OKR can optionally belong to a Project (backwards compatibility)
                // New design: OKRs connect to Projects through Key Results' Measurables
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // One OKR has many Key Results
                entity.HasMany(e => e.KeyResults)
                    .WithOne(kr => kr.Okr)
                    .HasForeignKey(kr => kr.OkrId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade); // Delete KRs when parent OKR is deleted

                entity.HasIndex(e => e.EndDate);
                entity.HasIndex(e => e.TimePeriod);
                entity.HasIndex(e => e.Year);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures KeyResult - measurable outcomes within OKRs.
        /// </summary>
        private void ConfigureKeyResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.TargetValue).HasPrecision(18, 4);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 4);
                entity.Property(e => e.StartingValue).HasPrecision(18, 4);
                entity.Property(e => e.Weight).HasPrecision(5, 2).HasDefaultValue(1.0m);
                
                // Computed properties - ignore
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.DisplayValue);
                entity.Ignore(e => e.HasMeasurables);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Key Result has many Measurables
                entity.HasMany(e => e.Measurables)
                    .WithOne(m => m.KeyResult)
                    .HasForeignKey(m => m.KeyResultId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OkrId);
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures KeyResultMeasurable - links between Key Results and their sources.
        /// </summary>
        private void ConfigureKeyResultMeasurable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyResultMeasurable>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Weight).HasPrecision(5, 2).HasDefaultValue(1.0m);
                
                // Computed properties - ignore (resolved at runtime)
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.CurrentProgress);
                entity.Ignore(e => e.CurrentDisplayValue);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Note: MeasurableId is a polymorphic FK - not enforced at DB level
                // Application code resolves to KPI, Project, or TaskCollection based on MeasurableType

                entity.HasIndex(e => e.KeyResultId);
                entity.HasIndex(e => new { e.MeasurableType, e.MeasurableId });
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures KeyPerformanceIndicator - standalone metrics that can feed Key Results.
        /// </summary>
        private void ConfigureKeyPerformanceIndicator(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyPerformanceIndicator>(entity =>
            {
                entity.HasKey(e => e.KpiId);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.Category).HasMaxLength(100);
                
                // Computed/interface properties - ignore
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.MeetingCount);
                entity.Ignore(e => e.PercentComplete);
                entity.Ignore(e => e.MeasurableId);
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.DisplayValue);
                entity.Ignore(e => e.MeasurableType);
                entity.Ignore(e => e.SourceId);
                entity.Ignore(e => e.SourceDisplayName);
                entity.Ignore(e => e.SourceType);
                entity.Ignore(e => e.HasDataSources);
                entity.Ignore(e => e.HasChildKpis);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // KPI owner relationship (TeamMember who owns the KPI)
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Self-referential relationship for composite KPIs
                entity.HasOne(e => e.ParentKpi)
                    .WithMany(e => e.ChildKpis)
                    .HasForeignKey(e => e.ParentKpiId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete through hierarchy
                
                // KPI has many data sources
                entity.HasMany(e => e.DataSources)
                    .WithOne(ds => ds.Kpi)
                    .HasForeignKey(ds => ds.KpiId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsComposite);
                entity.HasIndex(e => e.ParentKpiId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures KpiDataSource - data sources that feed KPI values.
        /// </summary>
        private void ConfigureKpiDataSource(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KpiDataSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Weight).HasPrecision(5, 2).HasDefaultValue(1.0m);
                entity.Property(e => e.QueryCriteria).HasMaxLength(2000);
                
                // Computed properties - ignore (resolved at runtime)
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.CurrentValue);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Note: SourceId is a polymorphic FK - not enforced at DB level
                // Application code resolves to Project, TaskCollection, or KPI based on SourceType

                entity.HasIndex(e => e.KpiId);
                entity.HasIndex(e => new { e.SourceType, e.SourceId });
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures TaskCollection - groups of tasks treated as single measurable units.
        /// </summary>
        private void ConfigureTaskCollection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskCollection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // Interface and computed properties - ignore
                entity.Ignore(e => e.MeasurableId);
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.DisplayValue);
                entity.Ignore(e => e.MeasurableType);
                entity.Ignore(e => e.SourceId);
                entity.Ignore(e => e.SourceDisplayName);
                entity.Ignore(e => e.SourceType);
                entity.Ignore(e => e.TotalTasks);
                entity.Ignore(e => e.CompletedTasks);
                entity.Ignore(e => e.IncompleteTasks);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Collection has many items
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Collection)
                    .HasForeignKey(i => i.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures TaskCollectionItem - links between TaskCollections and tasks.
        /// </summary>
        private void ConfigureTaskCollectionItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskCollectionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Link to task
                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade); // Remove from collection when task is deleted
                
                // Unique constraint: task can only be in a collection once
                entity.HasIndex(e => new { e.CollectionId, e.TaskId }).IsUnique();
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures Milestone - project checkpoints with target dates.
        /// </summary>
        private void ConfigureMilestone(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Milestone>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // User ownership: Each Milestone belongs to one User (via Project owner)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Note: Project FK is configured in ConfigureProject via HasMany(e => e.Milestones)
                
                entity.HasIndex(e => e.TargetDate);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures Risk - identified project risks with mitigation strategies.
        /// </summary>
        private void ConfigureRisk(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Risk>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.MitigationStrategy).HasMaxLength(4000);
                
                // User ownership: Each Risk belongs to one User (via Project owner)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Note: Project FK is configured in ConfigureProject via HasMany(e => e.Risks)
                
                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures ProjectDependency - relationships between projects.
        /// </summary>
        private void ConfigureProjectDependency(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectDependency>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // User ownership: Each ProjectDependency belongs to one User
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Note: Primary ProjectId FK is configured in ConfigureProject via HasMany(e => e.Dependencies)
                
                // FK to the dependent project
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.DependentProjectID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // FK to the required project
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.RequiredProjectID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures ChangeTrackingEntry - records changes for offline sync.
        /// This table captures all data modifications for later synchronization
        /// when the user reconnects to a SQL Server instance.
        /// </summary>
        private void ConfigureChangeTracking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChangeTrackingEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EntityJson).HasMaxLength(8000); // JSON snapshot of entity
                entity.Property(e => e.ChangedBy).HasMaxLength(100);
                entity.Property(e => e.SyncError).HasMaxLength(1000);

                // Indexes for sync operations
                entity.HasIndex(e => e.IsSynced); // Find unsynced changes
                entity.HasIndex(e => e.ChangedAt); // Order by time
                entity.HasIndex(e => new { e.EntityType, e.EntityId }); // Find changes to specific entity
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures the database and all tables are created.
        /// Call this on first run to initialize the database schema.
        /// </summary>
        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }

        /// <summary>
        /// Gets the SQLite database file path (null if using SQL Server).
        /// Useful for displaying connection info to users.
        /// </summary>
        public string? DatabasePath => _settings.Type == DatabaseType.SQLite 
            ? (!string.IsNullOrWhiteSpace(_settings.CustomSqlitePath) 
                ? _settings.CustomSqlitePath 
                : DatabaseSettings.GetSqlitePath())
            : null;

        /// <summary>
        /// Gets the current database settings.
        /// </summary>
        public DatabaseSettings Settings => _settings;

        #endregion

        #region Audit Field Auto-Population

        /// <summary>
        /// Saves changes and automatically updates audit fields.
        /// Audit fields (CreatedAt, ModifiedAt, etc.) are set automatically.
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Saves changes asynchronously and automatically updates audit fields.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Configures Feedback - feedback given to team members.
        /// </summary>
        private void ConfigureFeedback(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.Context).HasMaxLength(500);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Feedback belongs to a TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional link to OneOnOne
                entity.HasOne<OneOnOne>()
                    .WithMany()
                    .HasForeignKey(e => e.OneOnOneId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.TeamMemberId);
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.Type);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures IndividualGoal - personal goals for team members.
        /// </summary>
        private void ConfigureIndividualGoal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IndividualGoal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Notes).HasMaxLength(2000);

                // Computed properties - ignore
                entity.Ignore(e => e.IsOverdue);
                entity.Ignore(e => e.DaysRemaining);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Goal belongs to a TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Goal has many Milestones
                entity.HasMany(e => e.Milestones)
                    .WithOne()
                    .HasForeignKey(m => m.GoalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TeamMemberId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Category);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures GoalMilestone - milestones for individual goals.
        /// </summary>
        private void ConfigureGoalMilestone(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GoalMilestone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(500);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.GoalId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures Reminder - notifications and alerts.
        /// </summary>
        private void ConfigureReminder(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Message).HasMaxLength(1000);

                // Computed properties - ignore
                entity.Ignore(e => e.IsDue);
                entity.Ignore(e => e.IsSnoozed);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional FK to OneOnOne
                entity.HasOne<OneOnOne>()
                    .WithMany()
                    .HasForeignKey(e => e.OneOnOneId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional FK to TeamMember
                entity.HasOne<TeamMember>()
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional FK to IndividualTask
                entity.HasOne<IndividualTask>()
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional FK to IndividualGoal
                entity.HasOne<IndividualGoal>()
                    .WithMany()
                    .HasForeignKey(e => e.GoalId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DueDateTime);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures MeetingTemplate and MeetingTemplateItem - reusable meeting templates.
        /// </summary>
        private void ConfigureMeetingTemplate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MeetingTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Items relationship
                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey(i => i.MeetingTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex("UserId");
            });

            modelBuilder.Entity<MeetingTemplateItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(500).IsRequired();

                // User ownership (inherited from template, but track individually for filtering)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.MeetingTemplateId);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures QuickNote - quick notes and journal entries.
        /// </summary>
        private void ConfigureQuickNote(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QuickNote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Content).HasMaxLength(4000).IsRequired();
                entity.Property(e => e.Tags).HasMaxLength(500);

                // Polymorphic linking
                entity.Property(e => e.LinkedEntityType).HasDefaultValue(Common.Enums.NoteLinkedEntityType.None);
                entity.Property(e => e.LinkedEntityId);

                // Computed properties - ignore
                entity.Ignore(e => e.DisplayTitle);
                entity.Ignore(e => e.CategoryDisplay);
                entity.Ignore(e => e.LinkedEntityTypeDisplay);
                entity.Ignore(e => e.LinkedToDisplay);
                entity.Ignore(e => e.HasLinkedEntity);
                entity.Ignore(e => e.Preview);
                entity.Ignore(e => e.TagList);
                entity.Ignore(e => e.CreatedDisplay);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional FK to TeamMember (legacy + for navigation)
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Optional FK to Project (legacy)
                entity.HasOne<Project>()
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Optional FK to OneOnOne (legacy)
                entity.HasOne<OneOnOne>()
                    .WithMany()
                    .HasForeignKey(e => e.OneOnOneId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.LinkedEntityType);
                entity.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId });
                entity.HasIndex(e => e.IsPinned);
                entity.HasIndex(e => e.IsArchived);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures LinkedItem - links from agenda items to other entities (Tasks, OKRs, KPIs, Projects).
        /// </summary>
        private void ConfigureLinkedItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LinkedItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                
                // Computed property - ignore
                entity.Ignore(e => e.TypeDisplay);
                
                // Relationship: LinkedItem belongs to an AgendaItem
                entity.HasOne(e => e.AgendaItem)
                    .WithMany(a => a.LinkedItems)
                    .HasForeignKey(e => e.AgendaItemId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete linked items when agenda item is deleted
                
                // Indexes
                entity.HasIndex(e => e.AgendaItemId);
                entity.HasIndex(e => new { e.Type, e.ItemId }); // For finding links by entity type and ID
            });
        }

        #region Pulse Survey Entity Configurations

        /// <summary>
        /// Configures PulseSurvey - engagement pulse surveys.
        /// </summary>
        private void ConfigurePulseSurvey(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PulseSurvey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Survey has many Questions
                entity.HasMany(e => e.Questions)
                    .WithOne(q => q.PulseSurvey)
                    .HasForeignKey(q => q.PulseSurveyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Survey has many Responses
                entity.HasMany(e => e.Responses)
                    .WithOne(r => r.PulseSurvey)
                    .HasForeignKey(r => r.PulseSurveyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SentDate);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PulseSurveyQuestion - questions within pulse surveys.
        /// </summary>
        private void ConfigurePulseSurveyQuestion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PulseSurveyQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.RatingMinLabel).HasMaxLength(100);
                entity.Property(e => e.RatingMaxLabel).HasMaxLength(100);

                // User ownership (for global query filtering)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.PulseSurveyId);
                entity.HasIndex(e => e.QuestionType);
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PulseSurveyResponse - survey responses from team members.
        /// </summary>
        private void ConfigurePulseSurveyResponse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PulseSurveyResponse>(entity =>
            {
                entity.HasKey(e => e.Id);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Response optionally belongs to a TeamMember (can be anonymous)
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Response has many Answers
                entity.HasMany(e => e.Answers)
                    .WithOne(a => a.PulseSurveyResponse)
                    .HasForeignKey(a => a.PulseSurveyResponseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PulseSurveyId);
                entity.HasIndex(e => e.TeamMemberId);
                entity.HasIndex(e => e.SubmittedAt);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PulseSurveyAnswer - individual answers within survey responses.
        /// </summary>
        private void ConfigurePulseSurveyAnswer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PulseSurveyAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TextValue).HasMaxLength(2000);

                // Answer belongs to a Question
                entity.HasOne(e => e.PulseSurveyQuestion)
                    .WithMany()
                    .HasForeignKey(e => e.PulseSurveyQuestionId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict

                entity.HasIndex(e => e.PulseSurveyResponseId);
                entity.HasIndex(e => e.PulseSurveyQuestionId);
            });
        }

        #endregion

        #region Performance Review Entity Configurations

        /// <summary>
        /// Configures ReviewTemplate - templates defining review structure.
        /// </summary>
        private void ConfigureReviewTemplate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReviewTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Template has many Sections
                entity.HasMany(e => e.Sections)
                    .WithOne(s => s.ReviewTemplate)
                    .HasForeignKey(s => s.ReviewTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ReviewType);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures ReviewTemplateSection - sections within review templates.
        /// </summary>
        private void ConfigureReviewTemplateSection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReviewTemplateSection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Section has many Questions
                entity.HasMany(e => e.Questions)
                    .WithOne(q => q.ReviewTemplateSection)
                    .HasForeignKey(q => q.ReviewTemplateSectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ReviewTemplateId);
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures ReviewTemplateQuestion - questions within template sections.
        /// </summary>
        private void ConfigureReviewTemplateQuestion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReviewTemplateQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(500).IsRequired();
                entity.Property(e => e.RatingLabels).HasMaxLength(500);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReviewTemplateSectionId);
                entity.HasIndex(e => e.QuestionType);
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PerformanceReviewCycle - review cycles (e.g., Q1 2024).
        /// </summary>
        private void ConfigurePerformanceReviewCycle(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PerformanceReviewCycle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Cycle uses a Template
                entity.HasOne(e => e.ReviewTemplate)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewTemplateId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Cycle has many Reviews
                entity.HasMany(e => e.Reviews)
                    .WithOne(r => r.PerformanceReviewCycle)
                    .HasForeignKey(r => r.PerformanceReviewCycleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SelfReviewStartDate);
                entity.HasIndex(e => e.ManagerReviewDueDate);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PerformanceReview - individual reviews for team members.
        /// </summary>
        private void ConfigurePerformanceReview(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PerformanceReview>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ManagerSummary).HasMaxLength(4000);
                entity.Property(e => e.SelfAssessmentSummary).HasMaxLength(4000);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Review belongs to a TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional link to OneOnOne where review was discussed
                entity.HasOne(e => e.OneOnOne)
                    .WithMany()
                    .HasForeignKey(e => e.OneOnOneId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                // Review has many Sections
                entity.HasMany(e => e.Sections)
                    .WithOne(s => s.PerformanceReview)
                    .HasForeignKey(s => s.PerformanceReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PerformanceReviewCycleId);
                entity.HasIndex(e => e.TeamMemberId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex("UserId");
            });
        }

        /// <summary>
        /// Configures PerformanceReviewSection - sections within individual reviews.
        /// </summary>
        private void ConfigurePerformanceReviewSection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PerformanceReviewSection>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Reference to template section
                entity.HasOne(e => e.ReviewTemplateSection)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewTemplateSectionId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Section has many Answers
                entity.HasMany(e => e.Answers)
                    .WithOne(a => a.PerformanceReviewSection)
                    .HasForeignKey(a => a.PerformanceReviewSectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PerformanceReviewId);
                entity.HasIndex(e => e.ReviewTemplateSectionId);
            });
        }

        /// <summary>
        /// Configures PerformanceReviewAnswer - answers to review questions.
        /// </summary>
        private void ConfigurePerformanceReviewAnswer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PerformanceReviewAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TextValue).HasMaxLength(4000);

                // Reference to template question (for tracking)
                entity.HasOne(e => e.ReviewTemplateQuestion)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewTemplateQuestionId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.PerformanceReviewSectionId);
                entity.HasIndex(e => e.ReviewTemplateQuestionId);
            });
        }

        /// <summary>
        /// Configures Kudos - recognition sent to team members.
        /// </summary>
        private void ConfigureKudos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Kudos>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.DeliveryError).HasMaxLength(1000);
                
                // Store enums as strings for readability
                entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.DeliveryChannel).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.DeliveryStatus).HasConversion<string>().HasMaxLength(50);

                // Foreign key to TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for common queries
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TeamMemberId);
                entity.HasIndex(e => e.DeliveryStatus);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigureProgressSnapshot(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgressSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.EntityType)
                    .IsRequired()
                    .HasMaxLength(20);
                
                entity.Property(e => e.CurrentValue)
                    .HasPrecision(18, 4);
                
                entity.Property(e => e.TargetValue)
                    .HasPrecision(18, 4);
                
                entity.Property(e => e.Progress)
                    .HasPrecision(18, 4);

                // Unique constraint: one snapshot per entity per day
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.SnapshotDate, e.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_ProgressSnapshots_Entity_Date");

                // Index for efficient querying by entity
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.UserId })
                    .HasDatabaseName("IX_ProgressSnapshots_Entity");

                // Index for date-based queries
                entity.HasIndex(e => new { e.UserId, e.SnapshotDate })
                    .HasDatabaseName("IX_ProgressSnapshots_User_Date");
            });
        }

        /// <summary>
        /// Configures the CalendarLink entity - links Tracker meetings to external calendar events.
        /// </summary>
        private void ConfigureCalendarLink(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CalendarLink>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProviderId)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ExternalEventId)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.ETag)
                    .HasMaxLength(500);

                entity.Property(e => e.LastError)
                    .HasMaxLength(2000);

                // Store enum as string for readability
                entity.Property(e => e.LastSyncDirection)
                    .HasConversion<string>()
                    .HasMaxLength(10);

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Relationship: Each link belongs to one OneOnOne
                entity.HasOne(e => e.OneOnOne)
                    .WithMany()
                    .HasForeignKey(e => e.OneOnOneId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // User ownership via shadow property
                entity.Property<int>("UserId");
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint: one link per provider per meeting
                entity.HasIndex(e => new { e.OneOnOneId, e.ProviderId })
                    .IsUnique()
                    .HasDatabaseName("IX_CalendarLinks_Meeting_Provider");

                // Index for looking up by external event ID
                entity.HasIndex(e => new { e.ProviderId, e.ExternalEventId })
                    .HasDatabaseName("IX_CalendarLinks_Provider_ExternalId");

                // Index for user filtering
                entity.HasIndex("UserId")
                    .HasDatabaseName("IX_CalendarLinks_UserId");
            });
        }

        /// <summary>
        /// Configures the CalendarSyncToken entity - stores delta sync tokens per provider.
        /// </summary>
        private void ConfigureCalendarSyncToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CalendarSyncToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProviderId)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.SyncToken)
                    .IsRequired()
                    .HasMaxLength(2000);

                // User ownership via shadow property
                entity.Property<int>("UserId");
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: one token per provider per user
                entity.HasIndex(e => new { e.ProviderId })
                    .HasDatabaseName("IX_CalendarSyncTokens_Provider");

                // Combined unique index including UserId
                entity.HasIndex("UserId", "ProviderId")
                    .IsUnique()
                    .HasDatabaseName("IX_CalendarSyncTokens_User_Provider");
            });
        }

        #endregion

        /// <summary>
        /// Automatically populates audit fields based on entity state.
        /// - Added entities get CreatedAt/CreatedBy set
        /// - Modified entities get LastModifiedAt/LastModifiedBy updated
        /// - Deleted entities are converted to soft deletes
        /// </summary>
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<AuditableEntity>();
            var currentUser = Environment.UserName;
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // New record - set creation audit fields
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = currentUser;
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = currentUser;
                        break;

                    case EntityState.Modified:
                        // Existing record updated - set modification audit fields
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = currentUser;
                        break;

                    case EntityState.Deleted:
                        // Convert hard delete to soft delete
                        // This preserves data for audit trails and potential recovery
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.DeletedBy = currentUser;
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = currentUser;
                        break;
                }
            }
        }

        #endregion
    }
}
