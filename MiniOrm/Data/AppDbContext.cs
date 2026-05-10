using MiniOrm.Models;

namespace MiniOrm.Data;

/// <summary>
/// The application-specific DbContext.
/// Declares one DbSet<T> per entity.
/// DbContext base class auto-initializes these via reflection.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    public AppDbContext(string connectionString) : base(connectionString)
    {
    }
}