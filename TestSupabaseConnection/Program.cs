using Npgsql;

// Supabase connection test
var connectionString = 
    "Host=db.cftzoxucrzqljadyiijd.supabase.co;" +
    "Port=5432;" +
    "Database=postgres;" +
    "Username=postgres;" +
    "Password=$teelers4Ever;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true;";

Console.WriteLine("Testing Supabase PostgreSQL connection...");
Console.WriteLine($"Host: db.cftzoxucrzqljadyiijd.supabase.co");

try
{
    using var conn = new NpgsqlConnection(connectionString);
    Console.WriteLine("Opening connection...");
    conn.Open();
    Console.WriteLine("✅ Connected successfully!\n");

    // Test 1: Simple query
    using var cmd1 = new NpgsqlCommand("SELECT version()", conn);
    var version = cmd1.ExecuteScalar();
    Console.WriteLine($"PostgreSQL Version: {version}\n");

    // Test 2: Check if team_members table exists
    using var cmd2 = new NpgsqlCommand("SELECT COUNT(*) FROM team_members", conn);
    var count = cmd2.ExecuteScalar();
    Console.WriteLine($"✅ team_members table exists, count: {count}");

    // Test 3: Check profiles table (Supabase auth)
    using var cmd3 = new NpgsqlCommand("SELECT COUNT(*) FROM profiles", conn);
    var profileCount = cmd3.ExecuteScalar();
    Console.WriteLine($"✅ profiles table exists, count: {profileCount}");

    Console.WriteLine("\n🎉 All tests passed! Database connection is working.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
