using System.Text;
using Npgsql;

namespace MiniOrm.Data;

/// <summary>
/// Generic CRUD operations for entity type T.
/// All SQL is built dynamically from EntityMetadata — no hardcoded table or column names.
/// All values are passed as NpgsqlParameters — no string concatenation, no SQL injection risk.
/// </summary>
public class DbSet<T> where T : class, new()
{
    protected readonly NpgsqlConnection _connection;
    protected readonly EntityMetadata _metadata;

    public DbSet(NpgsqlConnection connection)
    {
        _connection = connection;
        _metadata = EntityMetadata.BuildFrom<T>();
    }

    // Insert 

    /// <summary>
    /// Inserts a new entity row into the database.
    /// Excludes the primary key column (PostgreSQL generates it via SERIAL).
    /// Uses RETURNING id to get the generated key and sets it back on the entity.
    ///
    /// Generated SQL example:
    ///   INSERT INTO products (name, price, discount, in_stock, created_at)
    ///   VALUES (@name, @price, @discount, @in_stock, @created_at)
    ///   RETURNING id
    /// </summary>
    public void Insert(T entity)
    {
        // ── Step 1: Build column list and parameter list 
        // We use _metadata.Columns which already excludes the primary key
        var columnNames = _metadata.Columns.Select(c => c.ColumnName);
        var paramNames = _metadata.Columns.Select(c => $"@{c.ColumnName}");

        // ── Step 2: Assemble the SQL string 
        var sql = new StringBuilder();
        sql.Append($"INSERT INTO {_metadata.TableName} ");
        sql.Append($"({string.Join(", ", columnNames)}) ");
        sql.Append($"VALUES ({string.Join(", ", paramNames)}) ");
        sql.Append($"RETURNING {_metadata.PrimaryKey.ColumnName}");

        // ── Step 3: Create command and bind parameters 
        using var command = new NpgsqlCommand(sql.ToString(), _connection);

        foreach (var col in _metadata.Columns)
        {
            // Read the current value of this property from the entity instance
            var value = col.Property.GetValue(entity);

            // CRITICAL: ADO.NET requires DBNull.Value for SQL NULL
            // Passing C# null throws a runtime exception
            command.Parameters.AddWithValue($"@{col.ColumnName}", value ?? DBNull.Value);
        }

        // ── Step 4: Execute and capture the returned id 
        // ExecuteScalar returns the first column of the first row
        // RETURNING id means that's our generated primary key
        var returnedId = command.ExecuteScalar();

        // ── Step 5: Write the generated id back to the entity 
        // This is why after Insert(product), product.Id is populated
        if (returnedId != null && returnedId != DBNull.Value)
        {
            _metadata.PrimaryKey.Property.SetValue(entity, Convert.ToInt32(returnedId));
        }
    }

    public T? FindById(int id) => throw new NotImplementedException("not implemented yet");


    public List<T> GetAll() => throw new NotImplementedException("not implemented yet");

    public void Update(T entity) => throw new NotImplementedException("not implemented yet");

    public void Delete(int id) => throw new NotImplementedException("not implemented yet");
}