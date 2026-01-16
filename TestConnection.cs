using Npgsql;

var connectionString = "Host=db.cftzoxucrzqljadyiijd.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=$teelers4Ever;SSL Mode=Require;Trust Server Certificate=true;";

try {
    Console.WriteLine("Attempting to connect...");
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    Console.WriteLine("Connected successfully!");
    using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM team_members", conn);
    var count = cmd.ExecuteScalar();
    Console.WriteLine($"Team members count: {count}");
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
}
