
namespace MiniOrm.Data;

/// <summary>
/// Translates a PropertyMetadata into a PostgreSQL column type string.
/// Used by the migration system when generating CREATE TABLE statements.
///
/// Rules:
///   - Primary key int         → SERIAL PRIMARY KEY
///   - Non-nullable value type → TYPE NOT NULL
///   - Nullable value type     → TYPE NULL
///   - string (non-nullable)   → TEXT NOT NULL
///   - string? (nullable)      → TEXT NULL
/// </summary>
public static class TypeMapper
{
    /// <summary>
    /// Returns the full PostgreSQL column definition for a property.
    /// e.g. "NUMERIC NOT NULL", "TEXT NULL", "SERIAL PRIMARY KEY"
    /// </summary>
    public static string GetPostgresType(PropertyMetadata property, bool isPrimaryKey = false)
    {
        // Primary key is always SERIAL PRIMARY KEY regardless of nullable flag
        if (isPrimaryKey)
            return "SERIAL PRIMARY KEY";

        // Map the CLR type to a base PostgreSQL type
        var baseType = GetBaseType(property.ClrType, property.ColumnName);

        // Append NULL / NOT NULL based on nullability
        var nullability = property.IsNullable ? "NULL" : "NOT NULL";

        return $"{baseType} {nullability}";
    }

    /// <summary>
    /// Maps a raw C# Type to its base PostgreSQL type keyword.
    /// Does not include nullability — that is appended by GetPostgresType.
    /// </summary>
    private static string GetBaseType(Type clrType, string columnName)
    {
        // Match against known C# types
        // Note: for nullable types (int?, decimal?) the ClrType is already
        // the unwrapped type (int, decimal) — PropertyMetadata handles that.
        if (clrType == typeof(int))      return "INTEGER";
        if (clrType == typeof(long))     return "BIGINT";
        if (clrType == typeof(float))    return "REAL";
        if (clrType == typeof(double))   return "DOUBLE PRECISION";
        if (clrType == typeof(decimal))  return "NUMERIC";
        if (clrType == typeof(bool))     return "BOOLEAN";
        if (clrType == typeof(DateTime)) return "TIMESTAMP";
        if (clrType == typeof(Guid))     return "UUID";
        if (clrType == typeof(string))   return "TEXT";

        // If we encounter a type we don't know, fail loudly
        // Silent failures here would generate invalid SQL
        throw new NotSupportedException(
            $"No PostgreSQL type mapping for CLR type '{clrType.Name}' " +
            $"on column '{columnName}'. " +
            $"Supported types: int, long, float, double, decimal, bool, DateTime, Guid, string.");
    }
}