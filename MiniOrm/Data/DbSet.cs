using System.Text;
using Npgsql;

namespace MiniOrm.Data;

public class DbSet<T> where T : class, new()
{
    protected readonly NpgsqlConnection _connection;
    protected readonly EntityMetadata _metadata;

    public DbSet(NpgsqlConnection connection)
    {
        _connection = connection;
        _metadata = EntityMetadata.BuildFrom<T>();
    }

    // ══════════════════════════════════════════════════════════════
    // INSERT
    // ══════════════════════════════════════════════════════════════
    public void Insert(T entity)
    {
        var columnNames = _metadata.Columns.Select(c => c.ColumnName);
        var paramNames = _metadata.Columns.Select(c => $"@{c.ColumnName}");

        var sql = new StringBuilder();
        sql.Append($"INSERT INTO {_metadata.TableName} ");
        sql.Append($"({string.Join(", ", columnNames)}) ");
        sql.Append($"VALUES ({string.Join(", ", paramNames)}) ");
        sql.Append($"RETURNING {_metadata.PrimaryKey.ColumnName}");

        using var command = new NpgsqlCommand(sql.ToString(), _connection);

        foreach (var col in _metadata.Columns)
        {
            var value = col.Property.GetValue(entity);
            command.Parameters.AddWithValue($"@{col.ColumnName}", value ?? DBNull.Value);
        }

        var returnedId = command.ExecuteScalar();

        if (returnedId != null && returnedId != DBNull.Value)
            _metadata.PrimaryKey.Property.SetValue(entity, Convert.ToInt32(returnedId));
    }


    public T? FindById(int id)
    {
        var selectColumns = string.Join(", ", _metadata.AllProperties.Select(p => p.ColumnName));

        var sql = $"SELECT {selectColumns} " +
                  $"FROM {_metadata.TableName} " +
                  $"WHERE {_metadata.PrimaryKey.ColumnName} = @id";

        using var command = new NpgsqlCommand(sql, _connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Hydrate(reader);
    }


    /// <summary>
    /// Fetches every row from the table.
    /// Returns an empty list if no rows exist — never returns null.
    ///
    /// Generated SQL example:
    ///   SELECT id, name, price, discount, in_stock, created_at
    ///   FROM products
    /// </summary>
    public List<T> GetAll()
    {
        // ── Step 1: Build SELECT for all mapped columns ───────────────────
        var selectColumns = string.Join(", ", _metadata.AllProperties.Select(p => p.ColumnName));
        var sql = $"SELECT {selectColumns} FROM {_metadata.TableName}";

        using var command = new NpgsqlCommand(sql, _connection);
        using var reader = command.ExecuteReader();

        // ── Step 2: Hydrate every row into a T and collect ────────────────
        var results = new List<T>();

        // reader.Read() returns true while rows remain
        // Each call advances the cursor to the next row
        while (reader.Read())
            results.Add(Hydrate(reader));

        return results;
    }


    /// <summary>
    /// Updates every non-PK column for the row matching the entity's primary key.
    ///
    /// Generated SQL example:
    ///   UPDATE products
    ///   SET name = @name, price = @price, discount = @discount,
    ///       in_stock = @in_stock, created_at = @created_at
    ///   WHERE id = @id
    /// </summary>
    public void Update(T entity)
    {
        // ── Step 1: Build SET clause — all columns except primary key ─────
        // Each entry looks like:  name = @name
        var setClauses = _metadata.Columns.Select(c => $"{c.ColumnName} = @{c.ColumnName}");

        var sql = new StringBuilder();
        sql.Append($"UPDATE {_metadata.TableName} ");
        sql.Append($"SET {string.Join(", ", setClauses)} ");
        sql.Append($"WHERE {_metadata.PrimaryKey.ColumnName} = @{_metadata.PrimaryKey.ColumnName}");

        using var command = new NpgsqlCommand(sql.ToString(), _connection);

        // ── Step 2: Bind all column parameters ───────────────────────────
        foreach (var col in _metadata.Columns)
        {
            var value = col.Property.GetValue(entity);
            command.Parameters.AddWithValue($"@{col.ColumnName}", value ?? DBNull.Value);
        }

        // ── Step 3: Bind the WHERE id parameter ──────────────────────────
        // Read the primary key value from the entity instance
        var pkValue = _metadata.PrimaryKey.Property.GetValue(entity)
                      ?? throw new InvalidOperationException(
                          $"Cannot update entity of type '{typeof(T).Name}': primary key value is null.");

        command.Parameters.AddWithValue(
            $"@{_metadata.PrimaryKey.ColumnName}",
            pkValue);

        // ── Step 4: Execute ───────────────────────────────────────────────
        // ExecuteNonQuery returns the number of rows affected
        // We don't strictly need it but it's useful for verification
        var rowsAffected = command.ExecuteNonQuery();

        if (rowsAffected == 0)
            throw new InvalidOperationException(
                $"Update affected 0 rows. No {typeof(T).Name} found with " +
                $"{_metadata.PrimaryKey.ColumnName} = {pkValue}.");
    }


    /// <summary>
    /// Deletes the row with the given primary key value.
    ///
    /// Generated SQL example:
    ///   DELETE FROM products WHERE id = @id
    /// </summary>
    public void Delete(int id)
    {
        var sql = $"DELETE FROM {_metadata.TableName} " +
                  $"WHERE {_metadata.PrimaryKey.ColumnName} = @id";

        using var command = new NpgsqlCommand(sql, _connection);
        command.Parameters.AddWithValue("@id", id);

        var rowsAffected = command.ExecuteNonQuery();

        if (rowsAffected == 0)
            throw new InvalidOperationException(
                $"Delete affected 0 rows. No {typeof(T).Name} found with " +
                $"{_metadata.PrimaryKey.ColumnName} = {id}.");
    }


    private T Hydrate(NpgsqlDataReader reader)
    {
        var entity = new T();

        foreach (var prop in _metadata.AllProperties)
        {
            var rawValue = reader[prop.ColumnName];

            if (rawValue == DBNull.Value)
            {
                if (prop.IsNullable)
                    prop.Property.SetValue(entity, null);
                continue;
            }

            var converted = Convert.ChangeType(rawValue, prop.ClrType);
            prop.Property.SetValue(entity, converted);
        }

        return entity;
    }
}