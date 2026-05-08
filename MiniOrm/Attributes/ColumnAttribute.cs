namespace MiniOrm.Attributes;

/// <summary>
/// Maps a C# property to a database column.
/// Usage: [Column("product_name")]
/// 
/// Only properties decorated with [Column] or [PrimaryKey]
/// will be considered by the ORM. All others are ignored.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ColumnAttribute : Attribute
{
    public string Name { get; }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}