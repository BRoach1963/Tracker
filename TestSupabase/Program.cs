using Npgsql;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var connectionString = @"Host=db.cftzoxucrzqljadyiijd.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=3M1ly@2112$teelers;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;";

try
{
    Console.WriteLine("Connecting to Supabase...");
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    Console.WriteLine("✓ Connected successfully!\n");

    // Test 1: Count team members
    using var cmd1 = new NpgsqlCommand("SELECT COUNT(*) FROM team_members", conn);
    var count = cmd1.ExecuteScalar();
    Console.WriteLine($"✓ team_members count: {count}");

    // Test 2: List tables
    using var cmd2 = new NpgsqlCommand(@"
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        ORDER BY table_name 
        LIMIT 10", conn);
    
    Console.WriteLine("\n✓ First 10 tables:");
    using var reader = cmd2.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"   - {reader.GetString(0)}");
    }

    Console.WriteLine("\n=== DATABASE CONNECTION WORKS! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ ERROR: {ex.GetType().Name}");
    Console.WriteLine($"  {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"  Inner: {ex.InnerException.Message}");
}
