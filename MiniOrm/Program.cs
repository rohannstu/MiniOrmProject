using MiniOrm.Data;
using MiniOrm.Models;

var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
                       ?? throw new InvalidOperationException("MINIORM_CONN is not set.");

using var context = new AppDbContext(connectionString);

// ── Insert two products ───────────────────────────────────────────────
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

var mouse = new Product
{
    Name = "Mouse",
    Price = 29.99m,
    Discount = null,
    InStock = true,
    CreatedAt = null
};
context.Products.Insert(mouse);
Console.WriteLine($"Inserted Product Id={mouse.Id}");

// ── FindById: existing record ─────────────────────────────────────────
var found = context.Products.FindById(laptop.Id);
Console.WriteLine($"\nFindById({laptop.Id}):");
Console.WriteLine($"  Name:     {found?.Name}");
Console.WriteLine($"  Price:    {found?.Price}");
Console.WriteLine($"  Discount: {found?.Discount}");
Console.WriteLine($"  InStock:  {found?.InStock}");

// ── FindById: nullable fields should come back as null ────────────────
var foundMouse = context.Products.FindById(mouse.Id);
Console.WriteLine($"\nFindById({mouse.Id}):");
Console.WriteLine($"  Name:      {foundMouse?.Name}");
Console.WriteLine($"  Discount:  {(foundMouse?.Discount == null ? "NULL" : foundMouse.Discount)}");
Console.WriteLine($"  CreatedAt: {(foundMouse?.CreatedAt == null ? "NULL" : foundMouse.CreatedAt)}");

// ── FindById: non-existent id should return null ──────────────────────
var missing = context.Products.FindById(99999);
Console.WriteLine($"\nFindById(99999): {(missing == null ? "NULL (correct)" : "ERROR — should be null")}");