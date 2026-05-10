using MiniOrm.Attributes;

namespace MiniOrm.Models;

[Table("orders")]
public class Order
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("ordered_at")]
    public DateTime OrderedAt { get; set; }
}