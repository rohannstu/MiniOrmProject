using MiniOrm.Migrations.Commands;

var connectionString = Environment.GetEnvironmentVariable("MINIORM_CONN")
                       ?? throw new InvalidOperationException("MINIORM_CONN environment variable is not set.");

var runner = new MigrationRunner(connectionString);

// Expected usage:
//   dotnet run -- migrations add <Name>
//   dotnet run -- migrations apply
//   dotnet run -- migrations list
//   dotnet run -- migrations rollback

if (args.Length < 2 || args[0] != "migrations")
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- migrations add <Name>");
    Console.WriteLine("  dotnet run -- migrations apply");
    Console.WriteLine("  dotnet run -- migrations list");
    Console.WriteLine("  dotnet run -- migrations rollback");
    return;
}

var subcommand = args[1].ToLower();

switch (subcommand)
{
    case "add":
        if (args.Length < 3)
        {
            Console.WriteLine("ERROR: Missing migration name.");
            Console.WriteLine("Usage: dotnet run -- migrations add <Name>");
            return;
        }

        runner.Add(args[2]);
        break;

    case "apply":
        runner.Apply();
        break;

    case "list":
        runner.List();
        break;

    case "rollback":
        runner.Rollback();
        break;

    default:
        Console.WriteLine($"Unknown command: {subcommand}");
        Console.WriteLine("Valid commands: add, apply, list, rollback");
        break;
}