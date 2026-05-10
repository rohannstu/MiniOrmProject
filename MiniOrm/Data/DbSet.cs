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

    // ══════════════════════════════════════════════════════════════
    // FIND BY ID
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches a single row by primary key.
    /// Returns null if no row is found.
    ///
    /// Generated SQL example:
    ///   SELECT id, name, price, discount, in_stock, created_at
    ///   FROM products
    ///   WHERE id = @id
    /// </summary>
    public T? FindById(int id)
    {
        // ── Step 1: Build SELECT column list from ALL properties (PK + columns) ──
        var selectColumns = string.Join(", ", _metadata.AllProperties.Select(p => p.ColumnName));

        // ── Step 2: Build the SQL ─────────────────────────────────────────────
        var sql = $"SELECT {selectColumns} " +
                  $"FROM {_metadata.TableName} " +
                  $"WHERE {_metadata.PrimaryKey.ColumnName} = @id";

        using var command = new NpgsqlCommand(sql, _connection);

        // ── Step 3: Bind the id parameter ────────────────────────────────────
        command.Parameters.AddWithValue("@id", id);

        // ── Step 4: Execute and read ──────────────────────────────────────────
        using var reader = command.ExecuteReader();

        // reader.Read() advances to the first row
        // Returns false if no rows matched — we return null
        if (!reader.Read())
            return null;

        // ── Step 5: Hydrate one entity from the current reader row ────────────
        return Hydrate(reader);
    }

   
    public List<T> GetAll() => throw new NotImplementedException("not implemented yet");

   
    public void Update(T entity) => throw new NotImplementedException("not implemented yet");

    public void Delete(int id) => throw new NotImplementedException("not implemented yet");

    // ══════════════════════════════════════════════════════════════
    // HYDRATE — private helper shared by FindById and GetAll
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts the current row of an NpgsqlDataReader into a populated T instance.
    ///
    /// Process:
    ///   1. new T()                          — blank entity
    ///   2. reader[columnName]               — raw object from DB
    ///   3. DBNull check                     — DB NULL → C# null
    ///   4. Convert.ChangeType               — object → correct CLR type
    ///   5. prop.SetValue(entity, value)     — write to property
    /// </summary>
    private T Hydrate(NpgsqlDataReader reader)
    {
        // Create a blank instance — requires the new() constraint on T
        var entity = new T();

        // Map every tracked property (PK + all columns)
        foreach (var prop in _metadata.AllProperties)
        {
            // Read raw value from the reader by column name
            var rawValue = reader[prop.ColumnName];

            // DBNull.Value is ADO.NET's representation of SQL NULL
            // We must convert it to C# null before setting on the property
            if (rawValue == DBNull.Value)
            {
                // Only set null if the property can accept it
                // Nullable value types (int?, decimal?) and reference types (string?) can
                // Non-nullable value types (int, decimal) cannot — leave them at default
                if (prop.IsNullable)
                    prop.Property.SetValue(entity, null);

                continue;
            }

            // Convert the raw database value to the exact CLR type the property expects
            // reader returns boxed objects — Convert.ChangeType unboxes to the right type
            // e.g. rawValue is boxed decimal 999.99 → Convert to decimal → set on Price
            var converted = Convert.ChangeType(rawValue, prop.ClrType);
            prop.Property.SetValue(entity, converted);
        }

        return entity;
    }
}