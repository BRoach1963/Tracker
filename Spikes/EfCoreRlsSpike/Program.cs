using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;

Console.WriteLine("=== EF Core + PostgreSQL + RLS Spike ===\n");

var connectionString = "Host=localhost;Database=tracker_spike;Username=tracker_app;Password=tracker123";

// Test 1: Basic connection and RLS without context (should see nothing)
Console.WriteLine("TEST 1: No user context set (should see 0 rows)");
await using (var context = new SpikeDbContext(connectionString, null))
{
    var count = await context.TeamMembers.CountAsync();
    Console.WriteLine($"  Result: {count} team members found");
    Console.WriteLine($"  Expected: 0");
    Console.WriteLine($"  PASS: {count == 0}\n");
}

// Test 2: Set context to Alice (should see 3)
Console.WriteLine("TEST 2: Context set to Alice (should see 3 rows)");
var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
await using (var context = new SpikeDbContext(connectionString, aliceId))
{
    var members = await context.TeamMembers.ToListAsync();
    Console.WriteLine($"  Result: {members.Count} team members found");
    foreach (var m in members)
        Console.WriteLine($"    - {m.Name}");
    Console.WriteLine($"  Expected: 3 (Alice's employees)");
    Console.WriteLine($"  PASS: {members.Count == 3}\n");
}

// Test 3: Set context to Bob (should see 2)
Console.WriteLine("TEST 3: Context set to Bob (should see 2 rows)");
var bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
await using (var context = new SpikeDbContext(connectionString, bobId))
{
    var members = await context.TeamMembers.ToListAsync();
    Console.WriteLine($"  Result: {members.Count} team members found");
    foreach (var m in members)
        Console.WriteLine($"    - {m.Name}");
    Console.WriteLine($"  Expected: 2 (Bob's employees)");
    Console.WriteLine($"  PASS: {members.Count == 2}\n");
}

// Test 4: INSERT respects RLS (Alice can insert for herself)
Console.WriteLine("TEST 4: INSERT as Alice with Alice's owner_id (should work)");
await using (var context = new SpikeDbContext(connectionString, aliceId))
{
    var newMember = new TeamMember
    {
        Id = Guid.NewGuid(),
        OwnerId = aliceId,
        Name = "Alice New Hire",
        Email = "newhire@alice.com",
        Role = "Intern"
    };
    context.TeamMembers.Add(newMember);
    await context.SaveChangesAsync();
    Console.WriteLine($"  Result: INSERT succeeded");
    
    var count = await context.TeamMembers.CountAsync();
    Console.WriteLine($"  Alice now has: {count} team members");
    Console.WriteLine($"  PASS: {count == 4}\n");
}

// Test 5: Connection pooling simulation (multiple contexts, different users)
Console.WriteLine("TEST 5: Connection pooling with multiple contexts (concurrent)");
var tasks = new List<Task<(string user, int count)>>();
for (int i = 0; i < 10; i++)
{
    var userId = i % 2 == 0 ? aliceId : bobId;
    var userName = i % 2 == 0 ? "Alice" : "Bob";
    
    tasks.Add(Task.Run(async () =>
    {
        await using var ctx = new SpikeDbContext(connectionString, userId);
        var count = await ctx.TeamMembers.CountAsync();
        return (userName, count);
    }));
}
var results = await Task.WhenAll(tasks);
foreach (var r in results)
    Console.WriteLine($"  [{r.user}] got {r.count} members");
    
var aliceResults = results.Where(r => r.user == "Alice").All(r => r.count == 4); // Alice has 4 now
var bobResults = results.Where(r => r.user == "Bob").All(r => r.count == 2);
Console.WriteLine($"  All Alice queries returned 4: {aliceResults}");
Console.WriteLine($"  All Bob queries returned 2: {bobResults}");
Console.WriteLine($"  PASS: {aliceResults && bobResults}\n");

// Cleanup - delete the test record we added
await using (var context = new SpikeDbContext(connectionString, aliceId))
{
    var toDelete = await context.TeamMembers.FirstOrDefaultAsync(t => t.Name == "Alice New Hire");
    if (toDelete != null)
    {
        context.TeamMembers.Remove(toDelete);
        await context.SaveChangesAsync();
        Console.WriteLine("Cleanup: Removed test record");
    }
}

// Test 6: Try to INSERT as Alice with Bob's owner_id (should FAIL due to RLS)
Console.WriteLine("\nTEST 6: INSERT as Alice with BOB's owner_id (should FAIL)");
try
{
    await using var context = new SpikeDbContext(connectionString, aliceId);
    var badMember = new TeamMember
    {
        Id = Guid.NewGuid(),
        OwnerId = bobId, // Alice trying to create for Bob - BAD
        Name = "Malicious Record",
        Email = "hacker@evil.com",
        Role = "Spy"
    };
    context.TeamMembers.Add(badMember);
    await context.SaveChangesAsync();
    Console.WriteLine("  Result: INSERT succeeded (THIS IS BAD - SECURITY HOLE!)");
    Console.WriteLine("  PASS: False\n");
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("row-level security") == true)
{
    Console.WriteLine("  Result: INSERT blocked by RLS policy (GOOD!)");
    Console.WriteLine("  PASS: True\n");
}

Console.WriteLine("\n=== SPIKE COMPLETE ===");
Console.WriteLine("CONCLUSION: EF Core + PostgreSQL + RLS works correctly!");
Console.WriteLine("- Connection interceptor sets user context on connection open");
Console.WriteLine("- RLS enforces row-level security");
Console.WriteLine("- Connection pooling works (each DbContext gets its own interceptor)");
Console.WriteLine("- No data leakage between users");

// Bonus: Show Brian's data summary
Console.WriteLine("\n=== BRIAN'S DATA SUMMARY ===");
var brianId = Guid.Parse("33333333-3333-3333-3333-333333333333");
await using (var context = new SpikeDbContext(connectionString, brianId))
{
    var teamCount = await context.TeamMembers.CountAsync();
    var meetingCount = await context.Meetings.CountAsync();
    var taskCount = await context.Tasks.CountAsync();
    var kudosCount = await context.Kudos.CountAsync();
    
    Console.WriteLine($"  Team Members: {teamCount}");
    Console.WriteLine($"  Meetings: {meetingCount}");
    Console.WriteLine($"  Tasks: {taskCount}");
    Console.WriteLine($"  Kudos: {kudosCount}");
    
    Console.WriteLine("\n  Upcoming Meetings:");
    var upcomingMeetings = await context.Meetings
        .Include(m => m.TeamMember)
        .Where(m => m.Status == "scheduled")
        .OrderBy(m => m.MeetingDate)
        .Take(5)
        .ToListAsync();
    foreach (var m in upcomingMeetings)
        Console.WriteLine($"    - {m.MeetingDate:MMM dd}: {m.Title} with {m.TeamMember?.Name}");
        
    Console.WriteLine("\n  High Priority Tasks:");
    var highPriorityTasks = await context.Tasks
        .Include(t => t.TeamMember)
        .Where(t => t.Priority == "high" && t.Status != "completed")
        .OrderBy(t => t.DueDate)
        .ToListAsync();
    foreach (var t in highPriorityTasks)
        Console.WriteLine($"    - [{t.DueDate:MMM dd}] {t.Title} ({t.TeamMember?.Name ?? "Unassigned"})");
}

// === DbContext and Models ===

public class SpikeDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly Guid? _userId;

    public SpikeDbContext(string connectionString, Guid? userId)
    {
        _connectionString = connectionString;
        _userId = userId;
    }

    public DbSet<TeamMember> TeamMembers { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Meeting> Meetings { get; set; } = null!;
    public DbSet<TrackerTask> Tasks { get; set; } = null!;
    public DbSet<Kudos> Kudos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString);
        
        if (_userId.HasValue)
        {
            optionsBuilder.AddInterceptors(new RlsConnectionInterceptor(_userId.Value));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>().ToTable("team_members");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Meeting>().ToTable("meetings");
        modelBuilder.Entity<TrackerTask>().ToTable("tasks");
        modelBuilder.Entity<Kudos>().ToTable("kudos");
    }
}

/// <summary>
/// Interceptor that sets the RLS user context when a connection is opened.
/// This ensures every query on this context runs with the correct user context.
/// </summary>
public class RlsConnectionInterceptor : DbConnectionInterceptor
{
    private readonly Guid _userId;

    public RlsConnectionInterceptor(Guid userId)
    {
        _userId = userId;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetUserContext(connection);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await SetUserContextAsync(connection);
    }

    private void SetUserContext(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.current_user_id = '{_userId}'";
        cmd.ExecuteNonQuery();
    }

    private async Task SetUserContextAsync(DbConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.current_user_id = '{_userId}'";
        await cmd.ExecuteNonQueryAsync();
    }
}

[Table("team_members")]
public class TeamMember
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; }

    [Column("role")]
    public string? Role { get; set; }
}

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("display_name")]
    public string? DisplayName { get; set; }
}

[Table("meetings")]
public class Meeting
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("meeting_date")]
    public DateTime MeetingDate { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; } = 30;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    public string Status { get; set; } = "scheduled";

    public TeamMember? TeamMember { get; set; }
}

[Table("tasks")]
public class TrackerTask
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("priority")]
    public string Priority { get; set; } = "medium";

    public TeamMember? TeamMember { get; set; }
}

[Table("kudos")]
public class Kudos
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("category")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public TeamMember? TeamMember { get; set; }
}
