using System.IO;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.DataModels;

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

        #region Constructors

        /// <summary>
        /// Creates a new database context with the specified settings.
        /// </summary>
        /// <param name="settings">Database connection settings (SQLite or SQL Server).</param>
        public TrackerDbContext(DatabaseSettings settings)
        {
            _settings = settings;
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

        /// <summary>Team members/employees being tracked.</summary>
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;

        /// <summary>One-on-one meeting records.</summary>
        public DbSet<OneOnOne> OneOnOnes { get; set; } = null!;

        /// <summary>Projects being managed.</summary>
        public DbSet<Project> Projects { get; set; } = null!;

        /// <summary>Individual tasks assigned to team members.</summary>
        public DbSet<IndividualTask> Tasks { get; set; } = null!;

        /// <summary>Action items from meetings.</summary>
        public DbSet<ActionItem> ActionItems { get; set; } = null!;

        /// <summary>Follow-up items from meetings.</summary>
        public DbSet<FollowUpItem> FollowUpItems { get; set; } = null!;

        /// <summary>Discussion points raised in meetings.</summary>
        public DbSet<DiscussionPoint> DiscussionPoints { get; set; } = null!;

        /// <summary>Concerns raised by team members.</summary>
        public DbSet<Concern> Concerns { get; set; } = null!;

        /// <summary>Objectives and Key Results (OKRs).</summary>
        public DbSet<ObjectiveKeyResult> ObjectiveKeyResults { get; set; } = null!;

        /// <summary>Key Performance Indicators (KPIs) linked to OKRs.</summary>
        public DbSet<KeyPerformanceIndicator> KeyPerformanceIndicators { get; set; } = null!;

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
                    var sqlitePath = DatabaseSettings.GetSqlitePath();
                    optionsBuilder.UseSqlite($"Data Source={sqlitePath}");
                    break;

                case DatabaseType.SqlServer:
                    // SQL Server for multi-user/enterprise scenarios
                    optionsBuilder.UseSqlServer(_settings.GetConnectionString());
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

            // Apply common audit configuration to all auditable entities
            ConfigureAuditableEntities(modelBuilder);

            // Configure each entity type with its specific relationships and constraints
            ConfigureTeamMember(modelBuilder);
            ConfigureOneOnOne(modelBuilder);
            ConfigureProject(modelBuilder);
            ConfigureIndividualTask(modelBuilder);
            ConfigureActionItem(modelBuilder);
            ConfigureFollowUpItem(modelBuilder);
            ConfigureDiscussionPoint(modelBuilder);
            ConfigureConcern(modelBuilder);
            ConfigureObjectiveKeyResult(modelBuilder);
            ConfigureKeyPerformanceIndicator(modelBuilder);
            ConfigureMilestone(modelBuilder);
            ConfigureRisk(modelBuilder);
            ConfigureProjectDependency(modelBuilder);
            ConfigureChangeTracking(modelBuilder);
            ConfigureOneOnOneLinkedEntities(modelBuilder);
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

                // Indexes for common query patterns
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.LastName, e.FirstName });
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
                entity.Property(e => e.OutlookCalendarEventId).HasMaxLength(200);
                
                // TeamMemberName is computed from TeamMember navigation property
                entity.Ignore(e => e.TeamMemberName);
                
                // Relationship: Each 1:1 belongs to one TeamMember
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey("TeamMemberId")
                    .OnDelete(DeleteBehavior.SetNull); // Don't delete 1:1s if team member is deleted

                // Index for querying by date
                entity.HasIndex(e => e.Date);
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
                entity.Property(e => e.Budget).HasPrecision(18, 2); // Decimal precision for money
                
                // KPIs is a computed property derived from OKRs
                entity.Ignore(e => e.KPIs);
                
                // Project owner relationship
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Many-to-many: Projects have multiple team members
                // EF Core creates a join table automatically
                entity.HasMany(e => e.TeamMembers)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ProjectTeamMembers"));

                // Indexes for common queries
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
            });
        }

        /// <summary>
        /// Configures IndividualTask - standalone tasks assigned to team members.
        /// </summary>
        private void ConfigureIndividualTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IndividualTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                // These are computed properties from the ITask interface
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.OwnerName);
                entity.Ignore(e => e.Type);
                entity.Ignore(e => e.MeetingCount); // Non-persisted property
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);

                // Common query patterns: find tasks by due date or completion status
                entity.HasIndex(e => e.DueDate);
                entity.HasIndex(e => e.IsCompleted);
            });
        }

        /// <summary>
        /// Configures ActionItem - tasks created during 1:1 meetings.
        /// </summary>
        private void ConfigureActionItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.OwnerName);
                entity.Ignore(e => e.Type);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.DueDate);
            });
        }

        /// <summary>
        /// Configures FollowUpItem - follow-up tasks from meetings.
        /// </summary>
        private void ConfigureFollowUpItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FollowUpItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.OwnerName);
                entity.Ignore(e => e.Type);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.DueDate);
            });
        }

        /// <summary>
        /// Configures DiscussionPoint - topics discussed in meetings.
        /// </summary>
        private void ConfigureDiscussionPoint(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscussionPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Details).HasMaxLength(4000);
                
                // Discussion points can optionally be linked to action items
                entity.HasOne(e => e.LinkedActionItem)
                    .WithMany()
                    .HasForeignKey(e => e.ActionItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        /// <summary>
        /// Configures Concern - issues raised by team members.
        /// </summary>
        private void ConfigureConcern(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Concern>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Details).HasMaxLength(4000);
                
                entity.HasOne(e => e.LinkedActionItem)
                    .WithMany()
                    .HasForeignKey(e => e.ActionItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        /// <summary>
        /// Configures ObjectiveKeyResult - OKRs that contain multiple KPIs.
        /// </summary>
        private void ConfigureObjectiveKeyResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjectiveKeyResult>(entity =>
            {
                entity.HasKey(e => e.ObjectiveId);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // Status and completion percentage are computed from KPIs
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.CompletionPercentage);
                entity.Ignore(e => e.MeetingCount); // Non-persisted property
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);
                
                // One OKR has many KPIs (Key Results)
                // Cascade delete: deleting an OKR deletes its KPIs
                entity.HasMany(e => e.KeyResults)
                    .WithOne()
                    .HasForeignKey(k => k.OkrId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.EndDate);
            });
        }

        /// <summary>
        /// Configures KeyPerformanceIndicator - measurable metrics for OKRs.
        /// </summary>
        private void ConfigureKeyPerformanceIndicator(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyPerformanceIndicator>(entity =>
            {
                entity.HasKey(e => e.KpiId);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                // Status is computed from value vs target
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.MeetingCount); // Non-persisted property
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Name);
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

                entity.HasIndex(e => e.TargetDate);
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

                entity.HasIndex(e => e.RiskLevel);
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
            ? DatabaseSettings.GetSqlitePath() 
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
