using MiniOrm.Data;
using MiniOrm.Models;

var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
                       ?? throw new InvalidOperationException("MINIORM_CONN is not set.");

using var context = new AppDbContext(connectionString);

// ── Test 1: Insert with all values populated ─────────────────────────
var laptop = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    Discount = 50.00m,
    InStock = true,
    CreatedAt = DateTime.UtcNow
};

context.Products.Insert(laptop);
Console.WriteLine($"Inserted Product Id={laptop.Id}");
Console.WriteLine($"  Name:     {laptop.Name}");
Console.WriteLine($"  Price:    {laptop.Price}");
Console.WriteLine($"  Discount: {laptop.Discount}");
Console.WriteLine($"  InStock:  {laptop.InStock}");

// ── Test 2: Insert with nullable fields as null ──────────────────────
var mouse = new Product
{
    Name = "Mouse",
    Price = 29.99m,
    Discount = null, // ← should store NULL in DB
    InStock = true,
    CreatedAt = null // ← should store NULL in DB
};

context.Products.Insert(mouse);
Console.WriteLine($"\nInserted Product Id={mouse.Id}");
Console.WriteLine($"  Name:      {mouse.Name}");
Console.WriteLine($"  Discount:  {(mouse.Discount == null ? "NULL" : mouse.Discount)}");
Console.WriteLine($"  CreatedAt: {(mouse.CreatedAt == null ? "NULL" : mouse.CreatedAt)}");

// ── Test 3: Insert an Order ──────────────────────────────────────────
var order = new Order
{
    ProductId = laptop.Id,
    Quantity = 2,
    TotalPrice = 1999.98m,
    Note = null, // ← nullable string, should store NULL
    OrderedAt = DateTime.UtcNow
};

context.Orders.Insert(order);
Console.WriteLine($"\nInserted Order Id={order.Id}");
Console.WriteLine($"  ProductId:  {order.ProductId}");
Console.WriteLine($"  Quantity:   {order.Quantity}");
Console.WriteLine($"  Note:       {(order.Note == null ? "NULL" : order.Note)}");