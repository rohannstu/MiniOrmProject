using MiniOrm.Data;

// Read connection string from environment
var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
    ?? throw new InvalidOperationException("MINIORM_CONN environment variable is not set.");

// Create the context — this opens the connection and initializes DbSets
using var context = new AppDbContext(connectionString);

Console.WriteLine("DbContext created successfully.");
Console.WriteLine($"Connection state: {context.GetConnection().State}");

// Verify DbSets were initialized by reflection
Console.WriteLine($"Products DbSet initialized: {context.Products != null}");
Console.WriteLine($"Orders DbSet initialized:   {context.Orders != null}");

Console.WriteLine("DbContext disposed cleanly.");