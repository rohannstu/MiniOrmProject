namespace MiniOrm.Attributes;

/// <summary>
/// Marks a property as the primary key of the table.
/// In PostgreSQL this maps to SERIAL PRIMARY KEY (auto-increment integer).
/// 
/// A property with [PrimaryKey] is automatically included in ORM operations.
/// It does NOT need an additional [Column] attribute.
/// 
/// Usage: [PrimaryKey("id")]
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PrimaryKeyAttribute : Attribute
{
    public string Name { get; }

    public PrimaryKeyAttribute(string name)
    {
        Name = name;
    }
}