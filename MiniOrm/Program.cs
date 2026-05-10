using MiniOrm.Data;
using MiniOrm.Models;

var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
                       ?? throw new InvalidOperationException("MINIORM_CONN is not set.");

using var context = new AppDbContext(connectionString);


// ── INSERT ────────────────────────────────────────────────────────────
var laptop = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    Discount = 50.00m,
    InStock = true,
    CreatedAt = DateTime.UtcNow
};
context.Products.Insert(laptop);
Console.WriteLine($"[INSERT] Product Id={laptop.Id}  Name={laptop.Name}  Price={laptop.Price}");

var mouse = new Product
{
    Name = "Mouse",
    Price = 29.99m,
    Discount = null,
    InStock = true,
    CreatedAt = null
};
context.Products.Insert(mouse);
Console.WriteLine($"[INSERT] Product Id={mouse.Id}  Name={mouse.Name}  Discount=NULL  CreatedAt=NULL");

var order = new Order
{
    ProductId = laptop.Id,
    Quantity = 2,
    TotalPrice = 1999.98m,
    Note = null,
    OrderedAt = DateTime.UtcNow
};
context.Orders.Insert(order);
Console.WriteLine($"[INSERT] Order Id={order.Id}  ProductId={order.ProductId}  Note=NULL");

// ── FIND BY ID ────────────────────────────────────────────────────────
Console.WriteLine();
var found = context.Products.FindById(laptop.Id);
Console.WriteLine($"[FIND]   Id={found?.Id}  Name={found?.Name}  " +
                  $"Discount={found?.Discount}  InStock={found?.InStock}");

var foundMouse = context.Products.FindById(mouse.Id);
Console.WriteLine($"[FIND]   Id={foundMouse?.Id}  Name={foundMouse?.Name}  " +
                  $"Discount={(foundMouse?.Discount == null ? "NULL" : foundMouse.Discount)}  " +
                  $"CreatedAt={(foundMouse?.CreatedAt == null ? "NULL" : foundMouse.CreatedAt)}");

var missing = context.Products.FindById(99999);
Console.WriteLine($"[FIND]   Id=99999  Result={(missing == null ? "NULL (correct)" : "ERROR")}");

// ── GET ALL ───────────────────────────────────────────────────────────
Console.WriteLine();
var allProducts = context.Products.GetAll();
Console.WriteLine($"[GETALL] Products in table: {allProducts.Count}");
foreach (var p in allProducts)
    Console.WriteLine($"         → Id={p.Id}  Name={p.Name}  Price={p.Price}");

// ── UPDATE ────────────────────────────────────────────────────────────
Console.WriteLine();
laptop.Price = 899.99m;
laptop.Discount = null; // clear the discount → should become NULL in DB
context.Products.Update(laptop);
Console.WriteLine($"[UPDATE] Id={laptop.Id}  NewPrice={laptop.Price}  Discount=NULL");

var afterUpdate = context.Products.FindById(laptop.Id);
Console.WriteLine($"[VERIFY] Id={afterUpdate?.Id}  Price={afterUpdate?.Price}  " +
                  $"Discount={(afterUpdate?.Discount == null ? "NULL" : afterUpdate.Discount)}");

// ── DELETE ────────────────────────────────────────────────────────────
Console.WriteLine();
context.Products.Delete(mouse.Id);
Console.WriteLine($"[DELETE] Product Id={mouse.Id} deleted");

var afterDelete = context.Products.FindById(mouse.Id);
Console.WriteLine($"[VERIFY] FindById({mouse.Id}) after delete: " +
                  $"{(afterDelete == null ? "NULL (correct)" : "ERROR — still exists")}");

var remainingProducts = context.Products.GetAll();
Console.WriteLine($"[GETALL] Products remaining: {remainingProducts.Count}");

