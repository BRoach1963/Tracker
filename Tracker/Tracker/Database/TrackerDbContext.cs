using System.IO;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.DataModels;

namespace Tracker.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for Tracker's database.
    /// Supports both SQLite (local) and SQL Server (remote) providers.
    /// </summary>
    public class TrackerDbContext : DbContext
    {
        private readonly DatabaseSettings _settings;

        public TrackerDbContext(DatabaseSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Constructor that uses default SQLite settings (for backwards compatibility).
        /// </summary>
        public TrackerDbContext() : this(new DatabaseSettings { Type = DatabaseType.SQLite })
        {
        }

        #region DbSets

        public DbSet<TeamMember> TeamMembers { get; set; } = null!;
        public DbSet<OneOnOne> OneOnOnes { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<IndividualTask> Tasks { get; set; } = null!;
        public DbSet<ActionItem> ActionItems { get; set; } = null!;
        public DbSet<FollowUpItem> FollowUpItems { get; set; } = null!;
        public DbSet<DiscussionPoint> DiscussionPoints { get; set; } = null!;
        public DbSet<Concern> Concerns { get; set; } = null!;
        public DbSet<ObjectiveKeyResult> ObjectiveKeyResults { get; set; } = null!;
        public DbSet<KeyPerformanceIndicator> KeyPerformanceIndicators { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;
        public DbSet<Risk> Risks { get; set; } = null!;
        public DbSet<ProjectDependency> ProjectDependencies { get; set; } = null!;
        public DbSet<ChangeTrackingEntry> ChangeTrackingEntries { get; set; } = null!;

        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_settings.Type)
            {
                case DatabaseType.SQLite:
                    var sqlitePath = DatabaseSettings.GetSqlitePath();
                    optionsBuilder.UseSqlite($"Data Source={sqlitePath}");
                    break;

                case DatabaseType.SqlServer:
                    optionsBuilder.UseSqlServer(_settings.GetConnectionString());
                    break;
            }

            // Enable sensitive data logging in debug mode
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure audit fields for all auditable entities
            ConfigureAuditableEntities(modelBuilder);

            // Configure each entity
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
        }

        #region Entity Configurations

        private void ConfigureAuditableEntities(ModelBuilder modelBuilder)
        {
            // Apply common audit field configuration to all entities that inherit from AuditableEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).Property<DateTime>("CreatedAt").HasDefaultValueSql(GetUtcDateFunction());
                    modelBuilder.Entity(entityType.ClrType).Property<DateTime>("LastModifiedAt").HasDefaultValueSql(GetUtcDateFunction());
                    modelBuilder.Entity(entityType.ClrType).Property<string>("CreatedBy").HasMaxLength(100);
                    modelBuilder.Entity(entityType.ClrType).Property<string>("LastModifiedBy").HasMaxLength(100);
                    modelBuilder.Entity(entityType.ClrType).Property<string>("DeletedBy").HasMaxLength(100);
                    
                    // Row version for concurrency - SQL Server uses ROWVERSION, SQLite uses manual
                    if (_settings.Type == DatabaseType.SqlServer)
                    {
                        modelBuilder.Entity(entityType.ClrType).Property<byte[]>("RowVersion").IsRowVersion();
                    }

                    // Index on IsDeleted for efficient soft-delete queries
                    modelBuilder.Entity(entityType.ClrType).HasIndex("IsDeleted");
                }
            }
        }

        private string GetUtcDateFunction()
        {
            return _settings.Type == DatabaseType.SqlServer ? "GETUTCDATE()" : "datetime('now')";
        }

        private void ConfigureTeamMember(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(e => e.Id);
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

                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.LastName, e.FirstName });
            });
        }

        private void ConfigureOneOnOne(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OneOnOne>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Agenda).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(4000);
                entity.Property(e => e.Feedback).HasMaxLength(4000);
                
                entity.Ignore(e => e.TeamMemberName);
                
                entity.HasOne(e => e.TeamMember)
                    .WithMany()
                    .HasForeignKey("TeamMemberId")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Date);
            });
        }

        private void ConfigureProject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Budget).HasPrecision(18, 2);
                
                entity.Ignore(e => e.KPIs);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasMany(e => e.TeamMembers)
                    .WithMany()
                    .UsingEntity(j => j.ToTable("ProjectTeamMembers"));

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
            });
        }

        private void ConfigureIndividualTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IndividualTask>(entity =>
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
                entity.HasIndex(e => e.IsCompleted);
            });
        }

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

        private void ConfigureDiscussionPoint(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscussionPoint>(entity =>
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

        private void ConfigureObjectiveKeyResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjectiveKeyResult>(entity =>
            {
                entity.HasKey(e => e.ObjectiveId);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                entity.Ignore(e => e.Status);
                entity.Ignore(e => e.CompletionPercentage);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasMany(e => e.KeyResults)
                    .WithOne()
                    .HasForeignKey(k => k.OkrId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.EndDate);
            });
        }

        private void ConfigureKeyPerformanceIndicator(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyPerformanceIndicator>(entity =>
            {
                entity.HasKey(e => e.KpiId);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                entity.Ignore(e => e.Status);
                
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey("OwnerId")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Name);
            });
        }

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

        private void ConfigureProjectDependency(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectDependency>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
            });
        }

        private void ConfigureChangeTracking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChangeTrackingEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EntityJson).HasMaxLength(8000);
                entity.Property(e => e.ChangedBy).HasMaxLength(100);
                entity.Property(e => e.SyncError).HasMaxLength(1000);

                entity.HasIndex(e => e.IsSynced);
                entity.HasIndex(e => e.ChangedAt);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures the database is created.
        /// </summary>
        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }

        /// <summary>
        /// Gets the database file path (SQLite only).
        /// </summary>
        public string? DatabasePath => _settings.Type == DatabaseType.SQLite 
            ? DatabaseSettings.GetSqlitePath() 
            : null;

        /// <summary>
        /// Gets the current database settings.
        /// </summary>
        public DatabaseSettings Settings => _settings;

        #endregion

        #region Audit Helpers

        /// <summary>
        /// Override SaveChanges to automatically update audit fields.
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically update audit fields.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

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
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = currentUser;
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = currentUser;
                        break;

                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedBy = currentUser;
                        break;

                    case EntityState.Deleted:
                        // Implement soft delete
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
