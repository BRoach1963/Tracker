// Quick script to hash password and update Brian's user record
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

var connectionString = "Host=localhost;Database=tracker_spike;Username=postgres;Password=$teelers4Ever";

var password = "$teelers4Ever";
var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");

// Update Brian's record
using var conn = new Npgsql.NpgsqlConnection(connectionString);
await conn.OpenAsync();

using var cmd = conn.CreateCommand();
cmd.CommandText = $"UPDATE users SET password_hash = '{hash}' WHERE email = 'brian@pricklycactussoftware.com'";
var rows = await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"Updated {rows} row(s)");

// Verify it works
var verifyHash = "";
cmd.CommandText = "SELECT password_hash FROM users WHERE email = 'brian@pricklycactussoftware.com'";
using var reader = await cmd.ExecuteReaderAsync();
if (await reader.ReadAsync())
{
    verifyHash = reader.GetString(0);
}

Console.WriteLine($"Verification: {BCrypt.Net.BCrypt.Verify(password, verifyHash)}");
