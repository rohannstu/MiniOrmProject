using Npgsql;

namespace MiniOrm.Data;

/// <summary>
/// Provides CRUD operations for a single entity type T.
/// One DbSet<T> exists per entity in your DbContext subclass.
/// 
/// Steps 7-11 will implement: Insert, FindById, GetAll, Update, Delete.
/// </summary>
public class DbSet<T> where T : class, new()
{
    // Shared connection from DbContext
    protected readonly NpgsqlConnection _connection;

    // Cached metadata — built once, reused for every operation
    protected readonly EntityMetadata _metadata;

    public DbSet(NpgsqlConnection connection)
    {
        _connection = connection;

        // Build and cache metadata for T right here at construction time
        // This is the one and only reflection scan for this entity type
        _metadata = EntityMetadata.BuildFrom<T>();
    }
}