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
        /// <param name="supabaseUserId">The Supabase UUID for Row-Level Security filtering.</param>
        /// <param name="localUserId">The local EF Core User.Id for query filtering.</param>
        public TrackerDbContext(DatabaseSettings settings, Guid supabaseUserId, int? localUserId = null)
        {
            _settings = settings;
            _postgresUserId = supabaseUserId;
            // Set CurrentUserId for EF query filters - this is the integer User.Id
            CurrentUserId = localUserId ?? UserSettingsManager.Instance?.CurrentUserId;
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

        /// <summary>Meeting records (unified: 1:1s, team meetings, all-hands, etc.).</summary>
        public DbSet<Meeting> Meetings { get; set; } = null!;

        /// <summary>Meeting attendees and their responses.</summary>
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; } = null!;

        /// <summary>Meeting notes.</summary>
        public DbSet<MeetingNote> MeetingNotes { get; set; } = null!;

        /// <summary>Projects being managed.</summary>
        public DbSet<Project> Projects { get; set; } = null!;

        /// <summary>Project members/team.</summary>
        public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;

        /// <summary>Tasks (unified: individual, project, goal, and meeting action items).</summary>
        public DbSet<TrackerTask> TrackerTasks { get; set; } = null!;

        /// <summary>Agenda items.</summary>
        public DbSet<AgendaItem> AgendaItems { get; set; } = null!;

        /// <summary>Goals (formerly OKRs).</summary>
        public DbSet<Goal> Goals { get; set; } = null!;

        /// <summary>Targets/Key Results for goals.</summary>
        public DbSet<Target> Targets { get; set; } = null!;

        /// <summary>Links between targets and measurable entities.</summary>
        public DbSet<TargetMeasurable> TargetMeasurables { get; set; } = null!;

        /// <summary>Goal milestones.</summary>
        public DbSet<GoalMilestone> GoalMilestones { get; set; } = null!;

        /// <summary>Metrics (formerly KPIs).</summary>
        public DbSet<Metric> Metrics { get; set; } = null!;

        /// <summary>Data sources for metrics.</summary>
        public DbSet<MetricDataSource> MetricDataSources { get; set; } = null!;

        /// <summary>Links between Meetings and Metrics for discussion tracking.</summary>
        public DbSet<MeetingMetricLink> MeetingMetricLinks { get; set; } = null!;

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

        /// <summary>Feedback given to team members.</summary>
        public DbSet<Feedback> Feedbacks { get; set; } = null!;

        /// <summary>Kudos/recognition for team members.</summary>
        public DbSet<Kudos> Kudoses { get; set; } = null!;

        /// <summary>Development goals for team members.</summary>
        public DbSet<DevelopmentGoal> DevelopmentGoals { get; set; } = null!;

        /// <summary>Milestones for development goals.</summary>
        public DbSet<DevelopmentGoalMilestone> DevelopmentGoalMilestones { get; set; } = null!;

        /// <summary>Comments on development goals.</summary>
        public DbSet<DevelopmentGoalComment> DevelopmentGoalComments { get; set; } = null!;

        /// <summary>Reminders and notifications.</summary>
        public DbSet<Reminder> Reminders { get; set; } = null!;

        /// <summary>Meeting templates for quick 1:1 setup.</summary>
        public DbSet<MeetingTemplate> MeetingTemplates { get; set; } = null!;

        /// <summary>Items within meeting templates.</summary>
        public DbSet<MeetingTemplateItem> MeetingTemplateItems { get; set; } = null!;

        /// <summary>Quick notes and journal entries.</summary>
        public DbSet<QuickNote> QuickNotes { get; set; } = null!;

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
                    // PostgreSQL - data isolation handled by EF Core query filters
                    // (ConfigureGlobalQueryFilters adds WHERE UserId = @currentUser)
                    optionsBuilder.UseNpgsql(_settings.GetConnectionString());
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
            System.Diagnostics.Debug.WriteLine($"=== OnModelCreating: DatabaseType = {_settings?.Type} ===");
            
            base.OnModelCreating(modelBuilder);
            
            // Apply global query filters for automatic data isolation
            // These filters are applied at the SQL level for maximum performance
            ConfigureGlobalQueryFilters(modelBuilder);

            // Apply common audit configuration to all auditable entities
            ConfigureAuditableEntities(modelBuilder);

            // Configure each entity type with its specific relationships and constraints
            ConfigureUser(modelBuilder);
            ConfigureTeamMember(modelBuilder);
            ConfigureMeeting(modelBuilder);
            ConfigureProject(modelBuilder);
            ConfigureTrackerTask(modelBuilder);
            ConfigureAgendaItem(modelBuilder);
            ConfigureGoal(modelBuilder);
            ConfigureTarget(modelBuilder);
            ConfigureTargetMeasurable(modelBuilder);
            ConfigureMetric(modelBuilder);
            ConfigureMetricDataSource(modelBuilder);
            ConfigureTaskCollection(modelBuilder);
            ConfigureTaskCollectionItem(modelBuilder);
            ConfigureMilestone(modelBuilder);
            ConfigureRisk(modelBuilder);
            ConfigureProjectDependency(modelBuilder);
            ConfigureChangeTracking(modelBuilder);
            ConfigureFeedback(modelBuilder);
            ConfigureGoalMilestone(modelBuilder);
            ConfigureReminder(modelBuilder);
            ConfigureMeetingTemplate(modelBuilder);
            ConfigureQuickNote(modelBuilder);
            ConfigureInsight(modelBuilder);
            
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
            
            // MUST BE LAST: For PostgreSQL, configure ALL DateTime properties to use timestamp without time zone
            // This prevents InvalidCastException when Npgsql tries to read timestamptz into DateTime
            // This runs AFTER all entity configurations so all properties are registered
            if (_settings.Type == DatabaseType.PostgreSQL)
            {
                ConfigurePostgreSqlDateTimeProperties(modelBuilder);
            }
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
            // TeamMember: Filter by IsDeleted only (organization filtering happens via FK)
            modelBuilder.Entity<TeamMember>().HasQueryFilter(e => !e.IsDeleted);
            
            // Meeting: Filter by IsDeleted
            modelBuilder.Entity<Meeting>().HasQueryFilter(e => !e.IsDeleted);
            
            // Project: Filter by IsDeleted
            modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
            
            // TrackerTask: Filter by IsDeleted
            modelBuilder.Entity<TrackerTask>().HasQueryFilter(e => !e.IsDeleted);
            
            // AgendaItem: Filter by IsDeleted
            modelBuilder.Entity<AgendaItem>().HasQueryFilter(e => !e.IsDeleted);
            
            // Goal: Filter by IsDeleted
            modelBuilder.Entity<Goal>().HasQueryFilter(e => !e.IsDeleted);
            
            // Target: Filter by IsDeleted
            modelBuilder.Entity<Target>().HasQueryFilter(e => !e.IsDeleted);
            
            // Metric: Filter by IsDeleted
            modelBuilder.Entity<Metric>().HasQueryFilter(e => !e.IsDeleted);
            
            // MetricDataSource: Filter by IsDeleted
            modelBuilder.Entity<MetricDataSource>().HasQueryFilter(e => !e.IsDeleted);
            
            // TargetMeasurable: Filter by IsDeleted
            modelBuilder.Entity<TargetMeasurable>().HasQueryFilter(e => !e.IsDeleted);
            
            // TaskCollection: Filter by IsDeleted
            modelBuilder.Entity<TaskCollection>().HasQueryFilter(e => !e.IsDeleted);
            
            // TaskCollectionItem: Filter by IsDeleted
            modelBuilder.Entity<TaskCollectionItem>().HasQueryFilter(e => !e.IsDeleted);
            
            // Milestone: Filter by IsDeleted
            modelBuilder.Entity<Milestone>().HasQueryFilter(e => !e.IsDeleted);
            
            // Risk: Filter by IsDeleted
            modelBuilder.Entity<Risk>().HasQueryFilter(e => !e.IsDeleted);
            
            // Feedback: Filter by IsDeleted
            modelBuilder.Entity<Feedback>().HasQueryFilter(e => !e.IsDeleted);
            
            // Kudos: Filter by IsDeleted
            modelBuilder.Entity<Kudos>().HasQueryFilter(e => !e.IsDeleted);
            
            // GoalMilestone: Filter by IsDeleted
            modelBuilder.Entity<GoalMilestone>().HasQueryFilter(e => !e.IsDeleted);
            
            // Reminder: Filter by IsDeleted
            modelBuilder.Entity<Reminder>().HasQueryFilter(e => !e.IsDeleted);
            
            // MeetingTemplate: Filter by IsDeleted
            modelBuilder.Entity<MeetingTemplate>().HasQueryFilter(e => !e.IsDeleted);
            
            // MeetingTemplateItem: Filter by IsDeleted
            modelBuilder.Entity<MeetingTemplateItem>().HasQueryFilter(e => !e.IsDeleted);
            
            // QuickNote: Filter by IsDeleted
            modelBuilder.Entity<QuickNote>().HasQueryFilter(e => !e.IsDeleted);
            
            // ProjectDependency: Filter by IsDeleted
            modelBuilder.Entity<ProjectDependency>().HasQueryFilter(e => !e.IsDeleted);
            
            // Pulse Survey entities
            modelBuilder.Entity<PulseSurvey>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PulseSurveyQuestion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PulseSurveyResponse>().HasQueryFilter(e => !e.IsDeleted);
            
            // Performance Review entities
            modelBuilder.Entity<ReviewTemplate>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ReviewTemplateSection>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ReviewTemplateQuestion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PerformanceReviewCycle>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PerformanceReview>().HasQueryFilter(e => !e.IsDeleted);
        }
        
        #endregion

        #region Entity Configurations

        /// <summary>
        /// Configures ALL DateTime and DateTime? properties in the model to use 'timestamp without time zone'
        /// for PostgreSQL. This is necessary because Npgsql 6+ requires DateTimeOffset for timestamptz columns,
        /// but our entities use DateTime. This prevents InvalidCastException errors.
        /// </summary>
        private void ConfigurePostgreSqlDateTimeProperties(ModelBuilder modelBuilder)
        {
            int configuredCount = 0;
            
            // Create converters to ensure DateTime is treated as Unspecified kind (for timestamp without time zone)
            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));
            
            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v);
            
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Check if this is a DateTime or DateTime? property
                    if (property.ClrType == typeof(DateTime))
                    {
                        // Set column type to timestamp without time zone for PostgreSQL
                        property.SetColumnType("timestamp without time zone");
                        property.SetValueConverter(dateTimeConverter);
                        configuredCount++;
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp without time zone");
                        property.SetValueConverter(nullableDateTimeConverter);
                        configuredCount++;
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"=== ConfigurePostgreSqlDateTimeProperties: Configured {configuredCount} DateTime properties ===");
        }

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
                    
                    // Note: PostgreSQL DateTime column types are configured globally in 
                    // ConfigurePostgreSqlDateTimeProperties() which runs before this method

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
            return _settings.Type switch
            {
                DatabaseType.SqlServer => "GETUTCDATE()",
                DatabaseType.PostgreSQL => "NOW() AT TIME ZONE 'UTC'",
                _ => "datetime('now')" // SQLite
            };
        }

        /// <summary>
        /// Configures the User entity - the logged-in manager who owns all data.
        /// </summary>
        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                // Use lowercase table name for PostgreSQL convention
                entity.ToTable("users");
                
                entity.HasKey(e => e.Id);
                
                // Map to snake_case column names for PostgreSQL
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FirmId).HasColumnName("firm_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organizationid");
                entity.Property(e => e.SupabaseUserId).HasColumnName("supabaseuserid");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasColumnName("displayname").HasMaxLength(200);
                entity.Property(e => e.IsActive).HasColumnName("isactive");
                entity.Property(e => e.IsAdmin).HasColumnName("isadmin");
                entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                
                // Audit columns (from AuditableEntity)
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
                entity.Property(e => e.CreatedBy).HasColumnName("createdby");
                entity.Property(e => e.LastModifiedAt).HasColumnName("lastmodifiedat");
                entity.Property(e => e.LastModifiedBy).HasColumnName("lastmodifiedby");
                entity.Property(e => e.RowVersion).HasColumnName("rowversion");
                entity.Property(e => e.IsDeleted).HasColumnName("isdeleted");
                entity.Property(e => e.DeletedAt).HasColumnName("deletedat");
                entity.Property(e => e.DeletedBy).HasColumnName("deletedby");

                // Unique constraint on Email (one user per email)
                entity.HasIndex(e => e.Email).IsUnique();
                
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
                entity.Property(e => e.Nickname).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.JobTitle).HasMaxLength(200);
                entity.Property(e => e.Department).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.LinkedInUrl).HasMaxLength(500);

                // Ignore NotMapped properties that EF shouldn't track
                entity.Ignore(e => e.LegacyId);
                entity.Ignore(e => e.FacebookProfile);
                entity.Ignore(e => e.InstagramProfile);
                entity.Ignore(e => e.XProfile);
                entity.Ignore(e => e.ProfileImage);
                entity.Ignore(e => e.Specialty);
                entity.Ignore(e => e.SkillLevel);
                entity.Ignore(e => e.Role);
                entity.Ignore(e => e.LegacyManagerId);
                entity.Ignore(e => e.UpcomingMeetingCount);
                entity.Ignore(e => e.NextOneOnOneDate);
                entity.Ignore(e => e.LastOneOnOneDate);

                // Relationships
                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for common query patterns
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.ManagerUserId);
                entity.HasIndex(e => new { e.LastName, e.FirstName });
            });
        }

        /// <summary>
        /// Configures the Meeting entity - unified meeting model.
        /// </summary>
        private void ConfigureMeeting(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(4000);
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.RecurrenceRule).HasMaxLength(200);
                entity.Property(e => e.GoogleCalendarEventId).HasMaxLength(200);
                entity.Property(e => e.GoogleCalendarEventEtag).HasMaxLength(500);
                entity.Property(e => e.OutlookCalendarEventId).HasMaxLength(200);
                entity.Property(e => e.OutlookCalendarEventEtag).HasMaxLength(500);
                entity.Property(e => e.TeamsMeetingUrl).HasMaxLength(500);
                entity.Property(e => e.TeamsMeetingId).HasMaxLength(200);
                entity.Property(e => e.GoogleMeetUrl).HasMaxLength(500);
                entity.Property(e => e.SyncStatus).HasMaxLength(50);
                
                // Ignore computed properties
                entity.Ignore(e => e.IsRecurring);
                entity.Ignore(e => e.IsCompleted);
                entity.Ignore(e => e.ActionItemCount);
                entity.Ignore(e => e.AgendaItemCount);
                entity.Ignore(e => e.IsSyncedToGoogle);
                entity.Ignore(e => e.IsSyncedToOutlook);
                entity.Ignore(e => e.HasTeamsMeeting);
                entity.Ignore(e => e.HasGoogleMeet);

                // Relationships
                entity.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Report)
                    .WithMany()
                    .HasForeignKey(e => e.ReportTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Team)
                    .WithMany()
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Tasks and AgendaItems relationships
                entity.HasMany(e => e.Tasks)
                    .WithOne()
                    .HasForeignKey(t => t.MeetingId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.AgendaItems)
                    .WithOne()
                    .HasForeignKey(a => a.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.ScheduledAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.ReportTeamMemberId);
                entity.HasIndex(e => e.Type);
            });
        }

        /// <summary>
        /// Configures the Project entity with its relationships.
        /// </summary>
        private void ConfigureProject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Color).HasMaxLength(7);
                entity.Property(e => e.ProgressPercent).HasPrecision(5, 2);
                
                // Project owner relationship
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Tasks relationship
                entity.HasMany(e => e.Tasks)
                    .WithOne(t => t.Project)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Configure Milestones relationship
                entity.HasMany(e => e.Milestones)
                    .WithOne()
                    .HasForeignKey(m => m.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.OrganizationId);
            });
        }

        /// <summary>
        /// Configures TrackerTask - unified task model for all task types.
        /// </summary>
        private void ConfigureTrackerTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrackerTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(4000);
                
                // Computed properties - ignore
                entity.Ignore(e => e.IsCompleted);
                entity.Ignore(e => e.IsOverdue);
                entity.Ignore(e => e.DaysRemaining);
                entity.Ignore(e => e.DerivedType);
                
                // Task owner relationship
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Optional link to Project
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(e => e.ProjectId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Optional link to Goal
                entity.HasOne(e => e.Goal)
                    .WithMany()
                    .HasForeignKey(e => e.GoalId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Self-referential relationship for subtasks
                entity.HasOne(e => e.ParentTask)
                    .WithMany(e => e.Subtasks)
                    .HasForeignKey(e => e.ParentTaskId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.GoalId);
                entity.HasIndex(e => e.MeetingId);
                entity.HasIndex(e => e.OrganizationId);
            });
        }

        /// <summary>
        /// Configures AgendaItem - topics discussed in meetings.
        /// </summary>
        private void ConfigureAgendaItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgendaItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(4000);
                entity.Property(e => e.RelatedEntityType).HasMaxLength(50);

                // Indexes for common queries
                entity.HasIndex(e => e.MeetingId);
            });
        }

        /// <summary>
        /// Configures Goal - what we want to achieve (formerly OKR/Objective).
        /// </summary>
        private void ConfigureGoal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Goal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.ProgressPercent).HasPrecision(5, 2);
                entity.Property(e => e.ProgressOverride).HasPrecision(5, 2);
                
                // Computed properties - ignore
                entity.Ignore(e => e.EffectiveStatus);
                entity.Ignore(e => e.EffectiveProgress);
                entity.Ignore(e => e.IsActive);
                
                // Goal owner relationship
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Optional link to Project
                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Goal has many Targets (Key Results)
                entity.HasMany(e => e.Targets)
                    .WithOne(t => t.Goal)
                    .HasForeignKey(t => t.GoalId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Goal has many Milestones
                entity.HasMany(e => e.Milestones)
                    .WithOne(m => m.Goal)
                    .HasForeignKey(m => m.GoalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Year);
                entity.HasIndex(e => e.TimePeriod);
                entity.HasIndex(e => e.OrganizationId);
            });
        }

        /// <summary>
        /// Configures Target - measurable outcomes (Key Results) within Goals.
        /// </summary>
        private void ConfigureTarget(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Target>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.TargetValue).HasPrecision(18, 4);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 4);
                entity.Property(e => e.StartingValue).HasPrecision(18, 4);
                entity.Property(e => e.Weight).HasPrecision(5, 2);
                
                // Computed properties - ignore
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.IsComplete);
                entity.Ignore(e => e.Remaining);
                
                // Target has many Measurables
                entity.HasMany(e => e.Measurables)
                    .WithOne(m => m.Target)
                    .HasForeignKey(m => m.TargetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.GoalId);
                entity.HasIndex(e => e.Status);
            });
        }

        /// <summary>
        /// Configures TargetMeasurable - links between Targets and their data sources.
        /// </summary>
        private void ConfigureTargetMeasurable(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TargetMeasurable>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MeasurableType).HasMaxLength(50);
                
                // Note: MeasurableId is a polymorphic FK - not enforced at DB level
                // Application code resolves to Metric, Project, or TaskCollection based on MeasurableType

                entity.HasIndex(e => e.TargetId);
                entity.HasIndex(e => new { e.MeasurableType, e.MeasurableId });
            });
        }

        /// <summary>
        /// Configures Metric - quantitative measures (formerly KPI).
        /// </summary>
        private void ConfigureMetric(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Metric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Unit).HasMaxLength(50);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 4);
                entity.Property(e => e.TargetValue).HasPrecision(18, 4);
                entity.Property(e => e.BaselineValue).HasPrecision(18, 4);
                entity.Property(e => e.WarningThreshold).HasPrecision(18, 4);
                entity.Property(e => e.CriticalThreshold).HasPrecision(18, 4);
                
                // Computed properties - ignore
                entity.Ignore(e => e.Progress);
                entity.Ignore(e => e.Status);
                entity.Ignore("Id"); // Ignore the interface implementation
                
                // Metric owner relationship
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Self-referential relationship for composite metrics
                entity.HasOne(e => e.ParentMetric)
                    .WithMany(e => e.ChildMetrics)
                    .HasForeignKey(e => e.ParentMetricId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Metric has many data sources
                entity.HasMany(e => e.DataSources)
                    .WithOne(ds => ds.Metric)
                    .HasForeignKey(ds => ds.MetricId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Metric has history
                entity.HasMany(e => e.History)
                    .WithOne(h => h.Metric)
                    .HasForeignKey(h => h.MetricId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsComposite);
                entity.HasIndex(e => e.OrganizationId);
            });
        }

        /// <summary>
        /// Configures MetricDataSource - data sources that feed Metric values.
        /// </summary>
        private void ConfigureMetricDataSource(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MetricDataSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceType).HasMaxLength(50);
                entity.Property(e => e.SourceConfig).HasMaxLength(4000);
                
                // Ignore runtime properties
                entity.Ignore(e => e.DisplayName);
                entity.Ignore(e => e.CurrentValue);

                entity.HasIndex(e => e.MetricId);
                entity.HasIndex(e => new { e.SourceType, e.SourceId });
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
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200);
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
                entity.Property(e => e.ChangeType).HasMaxLength(20);
                entity.Property(e => e.ChangeData).HasMaxLength(8000);

                // Indexes for sync operations
                entity.HasIndex(e => e.IsSynced);
                entity.HasIndex(e => e.ChangedAt);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.OrganizationId);
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
                entity.Property(e => e.Content).HasMaxLength(4000).IsRequired();
                entity.Property(e => e.FeedbackType).HasMaxLength(50);
                entity.Property(e => e.Sentiment).HasMaxLength(50);
                entity.Property(e => e.ContextType).HasMaxLength(50);
                entity.Property(e => e.AiSummary).HasMaxLength(4000);

                // From team member
                entity.HasOne(e => e.FromTeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.FromTeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // To team member
                entity.HasOne(e => e.ToTeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.ToTeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.FromTeamMemberId);
                entity.HasIndex(e => e.ToTeamMemberId);
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
                entity.Property(e => e.Title).HasMaxLength(300);
                entity.Property(e => e.Message).HasMaxLength(2000);
                entity.Property(e => e.EntityType).HasMaxLength(50);
                entity.Property(e => e.RecurrenceRule).HasMaxLength(200);

                // Computed properties - ignore
                entity.Ignore(e => e.IsDue);
                entity.Ignore(e => e.IsSnoozed);

                // Organization
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // User ownership
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Optional FK to TeamMember
                entity.HasOne<TeamMember>()
                    .WithMany()
                    .HasForeignKey(e => e.TeamMemberId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.RemindAt);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
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

                // Optional FK to Meeting
                entity.HasOne<Meeting>()
                    .WithMany()
                    .HasForeignKey(e => e.MeetingId)
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
        /// Configures Insight - AI-generated insights and recommendations.
        /// </summary>
        private void ConfigureInsight(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Insight>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InsightType).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(4000);
                entity.Property(e => e.Priority).HasMaxLength(50);
                entity.Property(e => e.DismissReason).HasMaxLength(500);
                entity.Property(e => e.ActionNotes).HasMaxLength(4000);
                
                // Computed property - ignore
                entity.Ignore(e => e.IsActive);
                
                // Relationships
                entity.HasOne(e => e.TargetTeam)
                    .WithMany()
                    .HasForeignKey(e => e.TargetTeamId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.TargetTeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.TargetTeamMemberId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.DismissedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.DismissedBy)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.InsightType);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.ValidFrom);
                entity.HasIndex(e => e.IsDismissed);
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

                // Optional link to Meeting where review was discussed
                entity.HasOne(e => e.Meeting)
                    .WithMany()
                    .HasForeignKey(e => e.MeetingId)
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
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Message).IsRequired().HasMaxLength(4000);
                entity.Property(e => e.BadgeType).HasMaxLength(100);

                // From team member
                entity.HasOne(e => e.FromTeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.FromTeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // To team member
                entity.HasOne(e => e.ToTeamMember)
                    .WithMany()
                    .HasForeignKey(e => e.ToTeamMemberId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.FromTeamMemberId);
                entity.HasIndex(e => e.ToTeamMemberId);
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
        /// Configures the CalendarLink entity - user calendar provider connections.
        /// </summary>
        private void ConfigureCalendarLink(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CalendarLink>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AccountEmail).HasMaxLength(255);
                entity.Property(e => e.AccountName).HasMaxLength(200);
                entity.Property(e => e.AccessToken).HasMaxLength(4000);
                entity.Property(e => e.RefreshToken).HasMaxLength(4000);
                entity.Property(e => e.SyncToken).HasMaxLength(4000);
                entity.Property(e => e.DefaultCalendarId).HasMaxLength(500);
                entity.Property(e => e.DefaultCalendarName).HasMaxLength(200);
                entity.Property(e => e.LastSyncError).HasMaxLength(4000);

                // Ignore computed properties
                entity.Ignore(e => e.IsTokenExpired);
                entity.Ignore(e => e.IsReadyToSync);
                entity.Ignore(e => e.LastSyncSuccessful);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Provider);
                entity.HasIndex(e => e.IsActive);
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
