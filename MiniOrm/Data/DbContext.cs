using System.Reflection;
using Npgsql;

namespace MiniOrm.Data;

/// <summary>
/// Central ORM coordinator.
/// 
/// Responsibilities:
///   1. Open and hold a single NpgsqlConnection for the lifetime of this context
///   2. Auto-discover all DbSet<T> properties on the subclass via reflection
///   3. Initialize each DbSet<T> with the shared connection
/// 
/// Usage:
///   class AppDbContext : DbContext
///   {
///       public DbSet<Product> Products { get; set; }
///       public DbSet<Order> Orders { get; set; }
///   }
/// 
///   using var ctx = new AppDbContext(connectionString);
///   ctx.Products.Insert(product);
/// </summary>
public abstract class DbContext : IDisposable
{
    // The single shared connection for this context instance.
    // All DbSet<T> operations use this same connection.
    private readonly NpgsqlConnection _connection;

    // Tracks whether Dispose has already been called
    private bool _disposed = false;

    protected DbContext(string connectionString)
    {
        // Create and open the connection immediately
        // One context = one connection, kept open for its lifetime
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();

        // Scan this instance's properties and initialize any DbSet<T> found
        InitializeDbSets();
    }

    /// <summary>
    /// Uses reflection to find all public DbSet<T> properties on the subclass
    /// and sets them to a new DbSet<T> instance wired to the shared connection.
    /// 
    /// This is why you don't need to manually new up each DbSet in your subclass.
    /// DbContext does it for you, just like EF Core does internally.
    /// </summary>
    private void InitializeDbSets()
    {
        // Get the actual runtime type (e.g. AppDbContext, not DbContext)
        var contextType = GetType();

        // Find all public instance properties on the subclass
        var properties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Check if this property's type is DbSet<T> (for any T)
            // IsGenericType: true for DbSet<Product>, DbSet<Order>, etc.
            // GetGenericTypeDefinition: strips the T → gives us DbSet<>
            if (!prop.PropertyType.IsGenericType)
                continue;

            if (prop.PropertyType.GetGenericTypeDefinition() != typeof(DbSet<>))
                continue;

            // Extract the T from DbSet<T>
            // e.g. DbSet<Product> → T = Product
            var entityType = prop.PropertyType.GetGenericArguments()[0];

            // Dynamically create: new DbSet<T>(_connection)
            // We can't write new DbSet<entityType>() because entityType is a runtime value
            // MakeGenericType + Activator.CreateInstance handles this
            var dbSetType    = typeof(DbSet<>).MakeGenericType(entityType);
            var dbSetInstance = Activator.CreateInstance(dbSetType, _connection);

            // Set the property on this context instance
            prop.SetValue(this, dbSetInstance);
        }
    }

    /// <summary>
    /// Exposes the raw connection for advanced use (e.g. migrations, raw queries).
    /// DbSet<T> uses this internally.
    /// </summary>
    public NpgsqlConnection GetConnection() => _connection;

    /// <summary>
    /// Closes and disposes the connection when the context goes out of scope.
    /// Always use DbContext in a 'using' block.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Close();
            _connection.Dispose();
            _disposed = true;
        }
    }
}