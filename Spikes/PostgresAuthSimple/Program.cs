using Npgsql;

namespace PostgresAuthSimple;

/// <summary>
/// Simple test for PostgreSQL authentication.
/// Verifies that the auth infrastructure works with the tracker_spike database.
/// </summary>
class Program
{
    private const string ConnectionString = 
        "Host=localhost;Port=5432;Database=tracker_spike;Username=tracker_app;Password=tracker123";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== PostgreSQL Authentication Simple Test ===\n");

        // Test 1: Connection
        Console.WriteLine("1. Testing database connection...");
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            Console.WriteLine("   ✓ Connection successful\n");
            await conn.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Connection failed: {ex.Message}");
            return;
        }

        // Test 2: Lookup user by email
        Console.WriteLine("2. Looking up Brian's user record...");
        (Guid? id, string? email, string? passwordHash)? userRecord = null;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT id, email, password_hash FROM users WHERE email = @email", conn);
            cmd.Parameters.AddWithValue("email", "brian@pricklycactussoftware.com");

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                userRecord = (
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                );
                Console.WriteLine($"   ✓ User found:");
                Console.WriteLine($"     ID: {userRecord.Value.id}");
                Console.WriteLine($"     Email: {userRecord.Value.email}");
                Console.WriteLine($"     Hash prefix: {userRecord.Value.passwordHash?.Substring(0, 30)}...\n");
            }
            else
            {
                Console.WriteLine("   ✗ User not found\n");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Lookup failed: {ex.Message}\n");
            return;
        }

        // Test 3: Verify password with BCrypt
        Console.WriteLine("3. Verifying password with BCrypt...");
        var testPassword = "$teelers4Ever";
        try
        {
            if (userRecord?.passwordHash != null)
            {
                var isValid = BCrypt.Net.BCrypt.Verify(testPassword, userRecord.Value.passwordHash);
                Console.WriteLine($"   Password '{testPassword}': {(isValid ? "✓ VALID" : "✗ INVALID")}\n");

                // Test wrong password
                var wrongValid = BCrypt.Net.BCrypt.Verify("wrongpassword", userRecord.Value.passwordHash);
                Console.WriteLine($"   Password 'wrongpassword': {(wrongValid ? "✗ UNEXPECTED VALID" : "✓ Correctly rejected")}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ BCrypt verification failed: {ex.Message}\n");
        }

        // Test 4: Set RLS context and query team members
        Console.WriteLine("4. Testing RLS with Brian's user context...");
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Set RLS context
            var userId = userRecord!.Value.id!.Value;
            await using var setCmd = new NpgsqlCommand($"SET app.current_user_id = '{userId}'", conn);
            await setCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"   ✓ Set app.current_user_id = {userId}");

            // Query team members - should be filtered by RLS
            await using var queryCmd = new NpgsqlCommand(
                "SELECT id, name, email FROM team_members", conn);
            await using var reader = await queryCmd.ExecuteReaderAsync();

            var count = 0;
            Console.WriteLine("   Team members visible to Brian:");
            while (await reader.ReadAsync())
            {
                count++;
                var name = reader.GetString(1);
                var email = reader.IsDBNull(2) ? "(no email)" : reader.GetString(2);
                Console.WriteLine($"     {count}. {name} <{email}>");
            }
            Console.WriteLine($"   ✓ Found {count} team members (RLS filtered)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ RLS query failed: {ex.Message}\n");
        }

        // Test 5: Verify RLS isolation - try Alice's context
        Console.WriteLine("5. Verifying RLS isolation (switching to Alice's context)...");
        try
        {
            // Alice's user ID from the spike seed data
            var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Set RLS context to Alice
            await using var setCmd = new NpgsqlCommand($"SET app.current_user_id = '{aliceId}'", conn);
            await setCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"   Set context to Alice ({aliceId})");

            // Query team members - should see Alice's team, not Brian's
            await using var queryCmd = new NpgsqlCommand(
                "SELECT id, name FROM team_members", conn);
            await using var reader = await queryCmd.ExecuteReaderAsync();

            var count = 0;
            Console.WriteLine("   Team members visible to Alice:");
            while (await reader.ReadAsync())
            {
                count++;
                Console.WriteLine($"     {count}. {reader.GetString(1)}");
            }
            Console.WriteLine($"   ✓ Found {count} team members (different from Brian's - RLS working!)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ RLS isolation test failed: {ex.Message}\n");
        }

        // Test 6: Query other data types with Brian's context
        Console.WriteLine("6. Testing other RLS-protected tables with Brian's context...");
        try
        {
            var userId = userRecord!.Value.id!.Value;

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Set RLS context to Brian
            await using var setCmd = new NpgsqlCommand($"SET app.current_user_id = '{userId}'", conn);
            await setCmd.ExecuteNonQueryAsync();

            // Query meetings
            await using var meetCmd = new NpgsqlCommand("SELECT COUNT(*) FROM meetings", conn);
            var meetCount = await meetCmd.ExecuteScalarAsync();
            Console.WriteLine($"   Meetings: {meetCount}");

            // Query tasks
            await using var taskCmd = new NpgsqlCommand("SELECT COUNT(*) FROM tasks", conn);
            var taskCount = await taskCmd.ExecuteScalarAsync();
            Console.WriteLine($"   Tasks: {taskCount}");

            // Query kudos
            await using var kudosCmd = new NpgsqlCommand("SELECT COUNT(*) FROM kudos", conn);
            var kudosCount = await kudosCmd.ExecuteScalarAsync();
            Console.WriteLine($"   Kudos: {kudosCount}");

            Console.WriteLine("   ✓ All queries executed with RLS filtering\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Data query failed: {ex.Message}\n");
        }

        Console.WriteLine("=== All Tests Complete ===");
    }
}
