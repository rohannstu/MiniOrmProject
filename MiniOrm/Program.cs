using Npgsql;
using MiniOrm.Attributes;

namespace MiniOrm;

// Simulate what an entity will look like
[Table("products")]
class TestEntity
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("price")]
    public decimal Price { get; set; }

    // This property has NO attribute — the ORM will ignore it completely
    public string InternalNote { get; set; } = "";
}

class Program
{
    static void Main()
    {
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

        // Test database connection
        TestDatabaseConnection(connectionString);
        
        Console.WriteLine("\n--- Testing Entity Attributes ---");
        
        // Test attribute reflection
        TestEntityAttributes();
    }

    static void TestDatabaseConnection(string connectionString)
    {
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
    }

    static void TestEntityAttributes()
    {
        // Use reflection to read the attributes back — exactly what our ORM will do
        var type = typeof(TestEntity);

        // Read [Table] from the class
        var tableAttr = (TableAttribute?)Attribute.GetCustomAttribute(type, typeof(TableAttribute));
        Console.WriteLine($"Table name: {tableAttr?.Name}");

        // Read attributes from each property
        foreach (var prop in type.GetProperties())
        {
            var pkAttr = (PrimaryKeyAttribute?)Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));
            var colAttr = (ColumnAttribute?)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));

            if (pkAttr != null)
                Console.WriteLine($"  PrimaryKey property: {prop.Name} → column: {pkAttr.Name}");
            else if (colAttr != null)
                Console.WriteLine($"  Column property: {prop.Name} → column: {colAttr.Name}");
            else
                Console.WriteLine($"  IGNORED property: {prop.Name}");
        }
    }
}