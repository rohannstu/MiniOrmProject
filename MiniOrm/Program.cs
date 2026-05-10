using MiniOrm.Attributes;
using MiniOrm.Data;

namespace MiniOrmApp;

[Table("products")]
class Product
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("price")]
    public decimal Price { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }

    [Column("in_stock")]
    public bool InStock { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}

class Program
{
    static void Main()
    {
        var meta = EntityMetadata.BuildFrom<Product>();

        Console.WriteLine($"CREATE TABLE {meta.TableName} (");

        // Primary key first
        var pkType = TypeMapper.GetPostgresType(meta.PrimaryKey, isPrimaryKey: true);
        Console.WriteLine($"    {meta.PrimaryKey.ColumnName}    {pkType},");

        // Regular columns
        foreach (var col in meta.Columns)
        {
            var pgType = TypeMapper.GetPostgresType(col, isPrimaryKey: false);
            Console.WriteLine($"    {col.ColumnName}    {pgType},");
        }

        Console.WriteLine(");");
    }
}