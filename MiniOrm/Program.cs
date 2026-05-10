using System;
using System.Linq;
using MiniOrm.Attributes;
using MiniOrm.Data;      // assuming EntityMetadata lives here
using Npgsql;

// Define a clean entity for testing
[Table("products")]
public class Product
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("price")]
    public decimal Price { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }

    // No attribute → ORM should ignore this property for INSERT/UPDATE/SELECT
    public string InternalNote { get; set; } = "";
}

public static class Program
{
    public static void Main()
    {
        // 1. Build metadata (reflection scan)
        var meta = EntityMetadata.BuildFrom<Product>();

        Console.WriteLine($"Table: {meta.TableName}");
        Console.WriteLine($"Primary Key: {meta.PrimaryKey.ColumnName} " +
                          $"(CLR: {meta.PrimaryKey.ClrType.Name}, " +
                          $"Nullable: {meta.PrimaryKey.IsNullable})");

        Console.WriteLine($"\nMapped columns ({meta.Columns.Count}):");
        foreach (var col in meta.Columns)
        {
            Console.WriteLine($"  {col.Property.Name} → \"{col.ColumnName}\" " +
                              $"(CLR: {col.ClrType.Name}, Nullable: {col.IsNullable})");
        }

        // All properties usable in a SELECT * query (includes mapped columns + primary key)
        Console.WriteLine($"\nAll properties for SELECT ({meta.AllProperties.Count}):");
        foreach (var prop in meta.AllProperties)
        {
            Console.WriteLine($"  {prop.ColumnName} ({prop.Property.Name})");
        }

        // 2. Optional: test PostgreSQL connection using environment variable
        TestPostgresConnection();
    }

    private static void TestPostgresConnection()
    {
        string? connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("\nSkipping DB connection test: MINIORM_CONN not set.");
            return;
        }

        Console.WriteLine($"\nTesting PostgreSQL connection...");
        using var conn = new NpgsqlConnection(connectionString);
        try
        {
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT version();", conn);
            var version = cmd.ExecuteScalar();
            Console.WriteLine($" Connected! PostgreSQL version: {version}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Connection failed: {ex.Message}");
        }
    }
}