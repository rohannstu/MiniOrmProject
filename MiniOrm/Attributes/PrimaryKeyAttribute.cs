namespace MiniOrm.Attributes;


[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PrimaryKeyAttribute : Attribute
{
    public string Name { get; }

    public PrimaryKeyAttribute(string name)
    {
        Name = name;
    }
}