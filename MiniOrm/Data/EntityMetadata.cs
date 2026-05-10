using System.Reflection;
using MiniOrm.Attributes;

namespace MiniOrm.Data;

/// <summary>
/// Holds mapping information for a single property-to-column pair.
/// One instance exists for every [Column] or [PrimaryKey] property on an entity.
/// </summary>
public class PropertyMetadata
{
    /// <summary>
    /// The C# PropertyInfo — used to get/set values on entity instances.
    /// e.g. typeof(Product).GetProperty("Name")
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// The database column name from the attribute.
    /// e.g. [Column("product_name")] → "product_name"
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// The underlying C# type of the property.
    /// For nullable types (int?, decimal?) this is the unwrapped type (int, decimal).
    /// For reference types (string) this is string.
    /// </summary>
    public Type ClrType { get; }

    /// <summary>
    /// True if the property is declared as nullable.
    /// int?  → true
    /// int   → true (because string is a reference type and can be null)
    /// string → false (NOT NULL by default unless declared string?)
    /// string? → true
    /// </summary>
    public bool IsNullable { get; }

    public PropertyMetadata(PropertyInfo property, string columnName)
    {
        Property   = property;
        ColumnName = columnName;

        // Detect nullable value types: int?, decimal?, Guid?, etc.
        // Nullable.GetUnderlyingType returns the inner type for Nullable<T>, or null if not nullable
        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);

        if (underlyingType != null)
        {
            // This is a nullable value type like int? (which is really Nullable<int>)
            ClrType    = underlyingType;   // store int, not Nullable<int>
            IsNullable = true;
        }
        else
        {
            // Either a non-nullable value type (int, decimal) or a reference type (string)
            ClrType = property.PropertyType;

            // For reference types, check the nullability context via custom attributes
            // This detects string? vs string in nullable-enabled projects
            var nullabilityCtx = new NullabilityInfoContext();
            var nullabilityInfo = nullabilityCtx.Create(property);
            IsNullable = nullabilityInfo.WriteState == NullabilityState.Nullable;
        }
    }
}

/// <summary>
/// Holds ALL mapping information for one entity class.
/// Built once via reflection, then reused for every SQL operation on that entity.
/// </summary>
public class EntityMetadata
{
    /// <summary>
    /// The PostgreSQL table name from [Table("...")] on the class.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Metadata for the [PrimaryKey] property.
    /// Used in WHERE clauses and excluded from INSERT column lists.
    /// </summary>
    public PropertyMetadata PrimaryKey { get; }

    /// <summary>
    /// Metadata for all [Column] properties (excludes the primary key).
    /// These are the columns used in INSERT, UPDATE, and SELECT.
    /// </summary>
    public IReadOnlyList<PropertyMetadata> Columns { get; }

    /// <summary>
    /// All mapped properties combined: PrimaryKey + Columns.
    /// Used when building SELECT column lists.
    /// </summary>
    public IReadOnlyList<PropertyMetadata> AllProperties { get; }

    public EntityMetadata(string tableName, PropertyMetadata primaryKey, List<PropertyMetadata> columns)
    {
        TableName  = tableName;
        PrimaryKey = primaryKey;
        Columns    = columns.AsReadOnly();

        // Combine PK + columns for SELECT queries
        var all = new List<PropertyMetadata> { primaryKey };
        all.AddRange(columns);
        AllProperties = all.AsReadOnly();
    }

    /// <summary>
    /// Factory method: scans a type via reflection and builds its EntityMetadata.
    /// This is the method that does the actual reflection work.
    /// Called once per entity type at DbContext startup.
    /// </summary>
    public static EntityMetadata BuildFrom<T>()
    {
        var type = typeof(T);

        // ── 1. Read [Table] from the class ──────────────────────────────────
        var tableAttr = (TableAttribute?)Attribute.GetCustomAttribute(type, typeof(TableAttribute))
            ?? throw new InvalidOperationException(
                $"Entity '{type.Name}' is missing the [Table] attribute.");

        // ── 2. Scan all public properties ────────────────────────────────────
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        PropertyMetadata? primaryKey = null;
        var columns = new List<PropertyMetadata>();

        foreach (var prop in properties)
        {
            var pkAttr  = (PrimaryKeyAttribute?)Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));
            var colAttr = (ColumnAttribute?)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));

            if (pkAttr != null)
            {
                // Found the primary key property
                if (primaryKey != null)
                    throw new InvalidOperationException(
                        $"Entity '{type.Name}' has more than one [PrimaryKey] attribute.");

                primaryKey = new PropertyMetadata(prop, pkAttr.Name);
            }
            else if (colAttr != null)
            {
                // Found a regular mapped column
                columns.Add(new PropertyMetadata(prop, colAttr.Name));
            }
            // Properties with neither attribute are silently ignored
        }

        if (primaryKey == null)
            throw new InvalidOperationException(
                $"Entity '{type.Name}' is missing a [PrimaryKey] attribute.");

        return new EntityMetadata(tableAttr.Name, primaryKey, columns);
    }
}