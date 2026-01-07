// Quick test to diagnose PostgreSQL DateTime issue
// Run with: dotnet run TestPostgres.cs

using System;
using Npgsql;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== PostgreSQL DateTime Test ===");
        Console.WriteLine($"EnableLegacyTimestampBehavior: {AppContext.TryGetSwitch("Npgsql.EnableLegacyTimestampBehavior", out var val) && val}");
        
        var connStr = "Host=localhost;Port=5432;Database=tracker;Username=tracker_app;Password=tracker123";
        
        try
        {
            using var conn = new NpgsqlConnection(connStr);
            conn.Open();
            Console.WriteLine("Connected to PostgreSQL");
            
            // Check column types
            using var cmd1 = new NpgsqlCommand(@"
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_name = 'TeamMembers' AND column_name LIKE '%at%'
                ORDER BY column_name", conn);
            
            Console.WriteLine("\nTeamMembers timestamp columns:");
            using var reader1 = cmd1.ExecuteReader();
            while (reader1.Read())
            {
                Console.WriteLine($"  {reader1.GetString(0)}: {reader1.GetString(1)}");
            }
            reader1.Close();
            
            // Try to read actual data
            Console.WriteLine("\nReading TeamMembers data...");
            using var cmd2 = new NpgsqlCommand("SELECT \"Id\", \"FirstName\", \"CreatedAt\" FROM \"TeamMembers\" LIMIT 1", conn);
            using var reader2 = cmd2.ExecuteReader();
            
            if (reader2.Read())
            {
                var id = reader2.GetInt32(0);
                var name = reader2.GetString(1);
                Console.WriteLine($"  Id: {id}, FirstName: {name}");
                
                // This is likely where it fails
                try
                {
                    var createdAt = reader2.GetDateTime(2);
                    Console.WriteLine($"  CreatedAt (GetDateTime): {createdAt}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  GetDateTime FAILED: {ex.GetType().Name}: {ex.Message}");
                    
                    // Try getting as object
                    reader2.Close();
                    using var cmd3 = new NpgsqlCommand("SELECT \"CreatedAt\" FROM \"TeamMembers\" LIMIT 1", conn);
                    var objValue = cmd3.ExecuteScalar();
                    Console.WriteLine($"  Raw value type: {objValue?.GetType().FullName}");
                    Console.WriteLine($"  Raw value: {objValue}");
                }
            }
            else
            {
                Console.WriteLine("  No TeamMembers found in database!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}
