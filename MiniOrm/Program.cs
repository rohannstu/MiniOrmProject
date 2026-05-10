using MiniOrm.Data;
using MiniOrm.Models;

// ══════════════════════════════════════════════════════════════════════
//  MiniOrm — Final Assignment Demo
//  Demonstrates: DbContext, DbSet<T>, CRUD, nullables, type mapping
// ══════════════════════════════════════════════════════════════════════

Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║         MiniOrm — Assignment Demo            ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.WriteLine();

// ── STEP 1: Read connection string from environment variable ──────────
Console.WriteLine("── Step 1: Loading connection string ────────────");
var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
                       ?? throw new InvalidOperationException(
                           "MINIORM_CONN environment variable is not set.\n" +
                           "Run: $env:MINIORM_CONN = \"Host=localhost;Port=5433;Database=miniorm_db;Username=miniorm_user;Password=miniorm_pass\"");

Console.WriteLine($"MINIORM_CONN loaded successfully.");
Console.WriteLine();

// ── STEP 2: Create DbContext ──────────────────────────────────────────
Console.WriteLine("── Step 2: Creating AppDbContext ────────────────");
using var context = new AppDbContext(connectionString);
Console.WriteLine($"Connection state : {context.GetConnection().State}");
Console.WriteLine($"Products DbSet   : {context.Products != null}");
Console.WriteLine($"Orders DbSet     : {context.Orders != null}");
Console.WriteLine();

// ── STEP 3: INSERT products ───────────────────────────────────────────
Console.WriteLine("── Step 3: Insert ───────────────────────────────");

// Product with all fields populated
var laptop = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    Discount = 50.00m, // non-null discount
    InStock = true,
    CreatedAt = DateTime.UtcNow
};
context.Products.Insert(laptop);
Console.WriteLine($"Inserted Product  Id={laptop.Id}  Name={laptop.Name}  " +
                  $"Price={laptop.Price}  Discount={laptop.Discount}");

// Product with nullable fields explicitly null
var mouse = new Product
{
    Name = "Mouse",
    Price = 29.99m,
    Discount = null, // nullable decimal → NULL in DB
    InStock = true,
    CreatedAt = null // nullable DateTime → NULL in DB
};
context.Products.Insert(mouse);
Console.WriteLine($"Inserted Product  Id={mouse.Id}  Name={mouse.Name}  " +
                  $"Discount=NULL  CreatedAt=NULL");

// Product to be deleted later
var keyboard = new Product
{
    Name = "Keyboard",
    Price = 49.99m,
    Discount = null,
    InStock = false,
    CreatedAt = DateTime.UtcNow
};
context.Products.Insert(keyboard);
Console.WriteLine($"Inserted Product  Id={keyboard.Id}  Name={keyboard.Name}  " +
                  $"InStock={keyboard.InStock}");

// Order linked to laptop
var order = new Order
{
    ProductId = laptop.Id,
    Quantity = 2,
    TotalPrice = 1999.98m,
    Note = null, // nullable string → NULL in DB
    OrderedAt = DateTime.UtcNow
};
context.Orders.Insert(order);
Console.WriteLine($"Inserted Order    Id={order.Id}  ProductId={order.ProductId}  " +
                  $"Quantity={order.Quantity}  Note=NULL");
Console.WriteLine();

// ── STEP 4: FIND BY ID ────────────────────────────────────────────────
Console.WriteLine("── Step 4: FindById ─────────────────────────────");

var foundLaptop = context.Products.FindById(laptop.Id);
Console.WriteLine($"FindById({laptop.Id})  → Name={foundLaptop?.Name}  " +
                  $"Price={foundLaptop?.Price}  " +
                  $"Discount={foundLaptop?.Discount}  " +
                  $"InStock={foundLaptop?.InStock}");

var foundMouse = context.Products.FindById(mouse.Id);
Console.WriteLine($"FindById({mouse.Id})  → Name={foundMouse?.Name}  " +
                  $"Discount={(foundMouse?.Discount == null ? "NULL" : foundMouse.Discount.ToString())}  " +
                  $"CreatedAt={(foundMouse?.CreatedAt == null ? "NULL" : foundMouse.CreatedAt.ToString())}");

var notFound = context.Products.FindById(99999);
Console.WriteLine($"FindById(99999) → {(notFound == null ? "NULL (correct — no such row)" : "ERROR")}");
Console.WriteLine();

// ── STEP 5: GET ALL ───────────────────────────────────────────────────
Console.WriteLine("── Step 5: GetAll ───────────────────────────────");

var allProducts = context.Products.GetAll();
Console.WriteLine($"Products in table: {allProducts.Count}");
foreach (var p in allProducts)
{
    Console.WriteLine($"  → Id={p.Id}  Name={p.Name,-10}  Price={p.Price,-8}  " +
                      $"Discount={(p.Discount == null ? "NULL" : p.Discount.ToString()),-8}  " +
                      $"InStock={p.InStock}");
}

var allOrders = context.Orders.GetAll();
Console.WriteLine($"Orders in table: {allOrders.Count}");
foreach (var o in allOrders)
{
    Console.WriteLine($"  → Id={o.Id}  ProductId={o.ProductId}  " +
                      $"Qty={o.Quantity}  Total={o.TotalPrice}  " +
                      $"Note={(o.Note == null ? "NULL" : o.Note)}");
}

Console.WriteLine();

// ── STEP 6: UPDATE ────────────────────────────────────────────────────
Console.WriteLine("── Step 6: Update ───────────────────────────────");

// Change price and clear the discount (set to null → DB NULL)
laptop.Price = 849.99m;
laptop.Discount = null;
context.Products.Update(laptop);
Console.WriteLine($"Updated  Id={laptop.Id}  NewPrice={laptop.Price}  Discount=NULL");

// Verify the update round-trips correctly from DB
var updatedLaptop = context.Products.FindById(laptop.Id);
Console.WriteLine($"Verified Id={updatedLaptop?.Id}  Price={updatedLaptop?.Price}  " +
                  $"Discount={(updatedLaptop?.Discount == null ? "NULL" : updatedLaptop.Discount.ToString())}");

// Update mouse — give it a discount now
mouse.Discount = 5.00m;
context.Products.Update(mouse);
Console.WriteLine($"Updated  Id={mouse.Id}  Discount={mouse.Discount} (was NULL)");

var updatedMouse = context.Products.FindById(mouse.Id);
Console.WriteLine($"Verified Id={updatedMouse?.Id}  Discount={updatedMouse?.Discount}");
Console.WriteLine();

// ── STEP 7: DELETE ────────────────────────────────────────────────────
Console.WriteLine("── Step 7: Delete ───────────────────────────────");

context.Products.Delete(keyboard.Id);
Console.WriteLine($"Deleted  Id={keyboard.Id}  Name={keyboard.Name}");

var afterDelete = context.Products.FindById(keyboard.Id);
Console.WriteLine($"FindById({keyboard.Id}) after delete → " +
                  $"{(afterDelete == null ? "NULL (correct)" : "ERROR — row still exists")}");
Console.WriteLine();

// ── STEP 8: FINAL STATE ───────────────────────────────────────────────
Console.WriteLine("── Step 8: Final State ──────────────────────────");

var finalProducts = context.Products.GetAll();
Console.WriteLine($"Products remaining: {finalProducts.Count}");
foreach (var p in finalProducts)
    Console.WriteLine($"  → Id={p.Id}  Name={p.Name,-10}  Price={p.Price}  " +
                      $"Discount={(p.Discount == null ? "NULL" : p.Discount.ToString())}");

var finalOrders = context.Orders.GetAll();
Console.WriteLine($"Orders remaining: {finalOrders.Count}");
foreach (var o in finalOrders)
    Console.WriteLine($"  → Id={o.Id}  ProductId={o.ProductId}  Total={o.TotalPrice}");

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║         All operations completed.            ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");