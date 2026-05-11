# MiniOrm

A simplified ORM framework built from scratch using C#, ADO.NET, and Npgsql.  
No Entity Framework. No Dapper. No ORM libraries. Just raw ADO.NET over PostgreSQL.

---

## Features

- Attribute-based entity mapping (`[Table]`, `[Column]`, `[PrimaryKey]`)
- Reflection-based metadata system with single-scan caching
- Generic `DbSet<T>` with full CRUD: `Insert`, `FindById`, `GetAll`, `Update`, `Delete`
- Full nullable support — `int?`, `decimal?`, `string?`, `DateTime?` map to SQL `NULL`
- Dynamic SQL generation — no hardcoded table or column names anywhere
- Parameterized queries only — no string concatenation, no SQL injection risk
- Migration CLI — `add`, `apply`, `list`, `rollback` with `-- up` / `-- down` SQL files
- Migration history tracked in `__migrations` table

---

## Project Structure

```
MiniOrmProject/
├── MiniOrm.sln
├── MiniOrm/                          ← ORM library + demo app
│   ├── Attributes/
│   │   ├── TableAttribute.cs         ← Maps class to DB table
│   │   ├── ColumnAttribute.cs        ← Maps property to DB column
│   │   └── PrimaryKeyAttribute.cs    ← Marks primary key property
│   ├── Models/
│   │   ├── Product.cs                ← Sample entity
│   │   └── Order.cs                  ← Sample entity
│   ├── Data/
│   │   ├── DbContext.cs              ← Connection lifecycle + DbSet initialization
│   │   ├── AppDbContext.cs           ← App-specific context with entity sets
│   │   ├── DbSet.cs                  ← Generic CRUD operations
│   │   ├── TypeMapper.cs             ← CLR type → PostgreSQL type translation
│   │   └── EntityMetadata.cs        ← Reflection-based metadata cache
│   └── Program.cs                    ← Full assignment demo
└── MiniOrm.Migrations/               ← Migration CLI
    ├── Commands/
    │   └── MigrationRunner.cs        ← add, apply, list, rollback
    ├── Migrations/                   ← Generated .sql migration files
    │   ├── ..._CreateProductsTable.sql
    │   └── ..._CreateOrdersTable.sql
    └── Program.cs                    ← CLI entry point
```

---

## Prerequisites

- [.NET 8 SDK or higher](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Windows)
- [DBeaver](https://dbeaver.io/) (optional, for visual inspection)

---

## PostgreSQL Setup (Docker)

Open PowerShell and run:

```powershell
docker run `
  --name miniorm-postgres `
  -e POSTGRES_USER=miniorm_user `
  -e POSTGRES_PASSWORD=miniorm_pass `
  -e POSTGRES_DB=miniorm_db `
  -p 5433:5432 `
  -d `
  postgres:16
```

Verify the container is running:

```powershell
docker ps
```

After a PC restart, start it again with:

```powershell
docker start miniorm-postgres
```

---

## DBeaver Connection

| Field    | Value        |
|----------|--------------|
| Host     | localhost    |
| Port     | 5433         |
| Database | miniorm_db   |
| Username | miniorm_user |
| Password | miniorm_pass |

Click **Test Connection** to verify before saving.

---

## Environment Variable Setup

The ORM reads the connection string from `MINIORM_CONN` — never hardcoded.

**Set permanently (survives reboots):**

```powershell
[System.Environment]::SetEnvironmentVariable(
  "MINIORM_CONN",
  "Host=localhost;Port=5433;Database=miniorm_db;Username=miniorm_user;Password=miniorm_pass",
  "User"
)
```

**Set for current terminal session only:**

```powershell
$env:MINIORM_CONN = "Host=localhost;Port=5433;Database=miniorm_db;Username=miniorm_user;Password=miniorm_pass"
```

**Verify:**

```powershell
echo $env:MINIORM_CONN
```

> Open a new terminal after setting permanently for it to take effect.

---

## Running Migrations

```powershell
cd MiniOrm.Migrations

# Generate a new migration file
dotnet run -- migrations add CreateProductsTable

# Apply all pending migrations
dotnet run -- migrations apply

# List applied and pending migrations
dotnet run -- migrations list

# Rollback the last applied migration
dotnet run -- migrations rollback
```

Migration files are generated in `MiniOrm.Migrations/Migrations/` as timestamped `.sql` files:

```
20240510120000_CreateProductsTable.sql
20240510120001_CreateOrdersTable.sql
```

Each file has two sections:

```sql
-- up
CREATE TABLE products (
    id         SERIAL PRIMARY KEY,
    name       TEXT NOT NULL,
    price      NUMERIC NOT NULL,
    discount   NUMERIC NULL,
    in_stock   BOOLEAN NOT NULL,
    created_at TIMESTAMP NULL
);

-- down
DROP TABLE products;
```

Applied migrations are tracked in a `__migrations` table created automatically on first `apply`.

---

## Running the Demo

```powershell
# 1. Apply migrations first
cd MiniOrm.Migrations
dotnet run -- migrations apply

# 2. Run the demo
cd ..\MiniOrm
dotnet run
```

The demo output covers every step:

```
── Step 1: Loading connection string ────────────
MINIORM_CONN loaded successfully.

── Step 2: Creating AppDbContext ────────────────
Connection state : Open
Products DbSet   : True
Orders DbSet     : True

── Step 3: Insert ───────────────────────────────
Inserted Product  Id=1  Name=Laptop  Price=999.99  Discount=50.00
Inserted Product  Id=2  Name=Mouse   Discount=NULL  CreatedAt=NULL
Inserted Product  Id=3  Name=Keyboard  InStock=False
Inserted Order    Id=1  ProductId=1  Quantity=2  Note=NULL

── Step 4: FindById ─────────────────────────────
FindById(1)    → Name=Laptop  Price=999.99  Discount=50.00  InStock=True
FindById(2)    → Name=Mouse   Discount=NULL  CreatedAt=NULL
FindById(99999)→ NULL (correct — no such row)

── Step 5: GetAll ───────────────────────────────
Products in table: 3
Orders in table: 1

── Step 6: Update ───────────────────────────────
Updated  Id=1  NewPrice=849.99  Discount=NULL
Verified Id=1  Price=849.99  Discount=NULL

── Step 7: Delete ───────────────────────────────
Deleted  Id=3  Name=Keyboard
FindById(3) after delete → NULL (correct)

── Step 8: Final State ──────────────────────────
Products remaining: 2
Orders remaining: 1
```

---

## How It Works

### Attribute Mapping

Entities are plain C# classes decorated with attributes:

```csharp
[Table("products")]
public class Product
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("price")]
    public decimal Price { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }   // nullable → NULL in DB

    public string InternalNote { get; set; } = "";  // no attribute → ignored
}
```

| Attribute | Placement | Effect |
|---|---|---|
| `[Table("x")]` | Class | Maps class to table `x` |
| `[Column("x")]` | Property | Maps property to column `x` |
| `[PrimaryKey("x")]` | Property | Marks primary key, maps to column `x` |

Properties without `[Column]` or `[PrimaryKey]` are silently ignored by the ORM.

### Reflection and Metadata Caching

When `DbSet<T>` is created, it calls `EntityMetadata.BuildFrom<T>()` once:

```
typeof(Product)
│
├── GetCustomAttribute → [Table("products")] → TableName = "products"
└── GetProperties()
    ├── Id       → [PrimaryKey("id")]   → PrimaryKey metadata
    ├── Name     → [Column("name")]     → Columns[0]
    ├── Price    → [Column("price")]    → Columns[1]
    ├── Discount → [Column("discount")] → Columns[2]  IsNullable=true
    └── InternalNote → (no attribute)  → IGNORED
```

The result is cached in `EntityMetadata`. All SQL generation reads from this cache — reflection runs exactly once per entity type.

### Dynamic SQL Generation

All SQL is built at runtime from metadata — no hardcoded names anywhere:

```csharp
var columnNames = _metadata.Columns.Select(c => c.ColumnName);
var paramNames  = _metadata.Columns.Select(c => $"@{c.ColumnName}");

// Produces:
// INSERT INTO products (name, price, discount) VALUES (@name, @price, @discount) RETURNING id
```

### Nullable Handling

Nullable value types are detected using `Nullable.GetUnderlyingType`:

```csharp
var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
// int?  → underlyingType = int,  IsNullable = true
// int   → underlyingType = null, IsNullable = false
```

Writing to DB — C# `null` becomes `DBNull.Value` (ADO.NET requirement):

```csharp
command.Parameters.AddWithValue("@discount", value ?? DBNull.Value);
```

Reading from DB — `DBNull.Value` becomes C# `null`:

```csharp
if (rawValue == DBNull.Value)
{
    if (prop.IsNullable) prop.Property.SetValue(entity, null);
    continue;
}
```

### Type Mapping

| C# Type | PostgreSQL Type |
|---|---|
| `int` (PK) | `SERIAL PRIMARY KEY` |
| `int` | `INTEGER NOT NULL` |
| `int?` | `INTEGER NULL` |
| `long` | `BIGINT NOT NULL` |
| `float` | `REAL NOT NULL` |
| `double` | `DOUBLE PRECISION NOT NULL` |
| `decimal` | `NUMERIC NOT NULL` |
| `decimal?` | `NUMERIC NULL` |
| `bool` | `BOOLEAN NOT NULL` |
| `bool?` | `BOOLEAN NULL` |
| `DateTime` | `TIMESTAMP NOT NULL` |
| `DateTime?` | `TIMESTAMP NULL` |
| `Guid` | `UUID NOT NULL` |
| `Guid?` | `UUID NULL` |
| `string` | `TEXT NOT NULL` |
| `string?` | `TEXT NULL` |

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# / .NET 8 or higher |
| Database driver | Npgsql (only NuGet package) |
| Database | PostgreSQL 16 (Docker) |
| Forbidden | Entity Framework, Dapper, any ORM library |
