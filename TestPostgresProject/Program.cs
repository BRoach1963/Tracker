// EF Core test to diagnose the issue
using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// Minimal TeamMember entity
public class TeamMember
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? HireDate { get; set; }
}

public class TestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connStr = "Host=localhost;Port=5432;Database=tracker;Username=tracker_app;Password=tracker123";
        optionsBuilder.UseNpgsql(connStr);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("TeamMembers");
            entity.HasKey(e => e.Id);
            
            // Configure DateTime properties for PostgreSQL
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.HireDate).HasColumnType("timestamp without time zone");
        });
    }

    public DbSet<TeamMember> TeamMembers { get; set; } = null!;
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== EF Core PostgreSQL DateTime Test ===");
        Console.WriteLine($"EnableLegacyTimestampBehavior: {AppContext.TryGetSwitch("Npgsql.EnableLegacyTimestampBehavior", out var val) && val}");
        
        try
        {
            using var context = new TestDbContext();
            
            Console.WriteLine("\nQuerying TeamMembers...");
            var members = context.TeamMembers.Take(5).ToList();
            
            Console.WriteLine($"Found {members.Count} team members:");
            foreach (var m in members)
            {
                Console.WriteLine($"  Id={m.Id}, Name={m.FirstName} {m.LastName}, UserId={m.UserId}, CreatedAt={m.CreatedAt}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
        
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}
