namespace MiniOrm.Attributes;

/// <summary>
/// Marks a class as a database entity mapped to a specific table.
/// Usage: [Table("products")]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute  
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}