namespace MiniOrm.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute : Attribute  
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        Name = name;
    }
}