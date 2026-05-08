using Npgsql;

// Read connection string from environment variable
// This is the ONLY place we'll ever read the connection string
string? connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: MINIORM_CONN environment variable is not set.");
    Console.WriteLine("Run this in PowerShell:");
    Console.WriteLine(@"[System.Environment]::SetEnvironmentVariable(""MINIORM_CONN"", ""Host=localhost;Port=5432;Database=miniorm_db;Username=miniorm_user;Password=miniorm_pass"", ""User"")");
    return;
}

Console.WriteLine("Connection string loaded from environment variable.");
Console.WriteLine($"Connecting to: {connectionString}");

// Open a raw ADO.NET connection — this is exactly what our ORM will use internally
using var connection = new NpgsqlConnection(connectionString);

try
{
    connection.Open();
    Console.WriteLine("SUCCESS: Connected to PostgreSQL!");

    // Run a simple query to prove it works
    using var command = new NpgsqlCommand("SELECT version();", connection);
    var version = command.ExecuteScalar();
    Console.WriteLine($"PostgreSQL version: {version}");
}
catch (Exception ex)
{
    Console.WriteLine($"FAILED: {ex.Message}");
}