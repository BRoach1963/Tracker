using BCrypt.Net;
using Npgsql;

var connectionString = "Host=localhost;Database=tracker_spike;Username=postgres;Password=$teelers4Ever";

var password = "$teelers4Ever";
var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");

// Update Brian's record
using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

using var cmd = conn.CreateCommand();
cmd.CommandText = $"UPDATE users SET password_hash = @hash WHERE email = 'brian@pricklycactussoftware.com'";
cmd.Parameters.AddWithValue("hash", hash);
var rows = await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"Updated {rows} row(s)");

// Verify it works
cmd.CommandText = "SELECT password_hash FROM users WHERE email = 'brian@pricklycactussoftware.com'";
cmd.Parameters.Clear();
using var reader = await cmd.ExecuteReaderAsync();
if (await reader.ReadAsync())
{
    var storedHash = reader.GetString(0);
    Console.WriteLine($"Verification: {BCrypt.Net.BCrypt.Verify(password, storedHash)}");
}

Console.WriteLine("\n✅ Brian can now log in with:");
Console.WriteLine("   Email: brian@pricklycactussoftware.com");
Console.WriteLine("   Password: $teelers4Ever");
