# sql-migration-lint

Lints EF Core migrations for dangerous operations before they hit production.

## MigrationFile

The `MigrationFile` class represents a parsed EF Core migration file. It provides access to the file's metadata, such as its file path, migration name, and contents. You can use it to inspect and lint migration files before applying them to your database.

## MigrationOperation

The `MigrationOperation` type is the base record for all migration operations detected by sql-migration-lint. It tracks the source file and line number of each operation and provides the foundation for specialized operation types like `AddColumnOperation`, `AlterColumnOperation`, `CreateIndexOperation`, and `AddForeignKeyOperation`. These derived types contain operation-specific properties that describe the actual database changes being made.

Example usage:

```csharp
var operation = new AddColumnOperation
{
    File = "0001_AddUserEmail.cs",
    Line = 42,
    TableName = "Users",
    ColumnName = "Email",
    TableExists = true,
    IsNullable = false,
    DefaultValue = null
};

if (operation.TableExists && !operation.IsNullable)
{
    Console.WriteLine($"Warning: Adding non-nullable column '{operation.ColumnName}' to existing table '{operation.TableName}' at {operation.File}:{operation.Line}");
}
```

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown, data flow, rule catalog (including which rules are registered by default), extension points, and known limitations.

## MigrationLinter

The `MigrationLinter` orchestrates linting of migration files using a collection of `ILintRule` implementations. It can be instantiated with a custom rule set via its public constructor or by using the static `CreateDefault` method, which registers all built‑in rules. After calling `Lint` you receive a `LintReport` that contains the findings, the number of migrations scanned, whether any blocker‑severity findings were reported, and the highest risk level observed. The linter also exposes shortcut properties (`Findings`, `MigrationsScanned`, `HasBlockers`, `MaxRisk`) through its `LintReport` property for quick access.

Example usage:

```csharp
using System;
using SqlMigrationLint;

class Program
{
    static void Main()
    {
        // Create a linter with the default rule set
        var linter = MigrationLinter.CreateDefault();

        // Run the linter against a project root folder
        var report = linter.Lint("/path/to/project");

        // Access report details
        Console.WriteLine($"Scanned {report.MigrationsScanned} migrations.");
        Console.WriteLine($"Has blockers: {report.HasBlockers}");
        Console.WriteLine($"Maximum risk level: {report.MaxRisk}");

        // Or use the shortcut properties on the linter
        Console.WriteLine($"Findings count: {linter.LintReport?.Findings.Count ?? 0}");
    }
}
```

## MigrationFileJsonConfig

`MigrationFileJsonConfig` defines the JSON serialization settings used for `MigrationFile` objects. It exposes the property names that are written, whether camel‑case naming and null‑value ignoring are applied, and provides helper methods to serialize/deserialize the configuration itself.

Example usage:

```csharp
using System;
using SqlMigrationLint;

class Program
{
    static void Main()
    {
        // Inspect the default configuration
        Console.WriteLine(string.Join(", ", MigrationFileJsonConfig.PropertyNames));
        Console.WriteLine($"Camel case: {MigrationFileJsonConfig.UseCamelCase}");
        Console.WriteLine($"Ignore nulls: {MigrationFileJsonConfig.IgnoreNullValues}");

        // Serialize the config to JSON (indented)
        string json = MigrationFileJsonConfig.ToJson(indented: true);
        Console.WriteLine(json);

        // Deserialize back
        var config = MigrationFileJsonConfig.FromJson(json);
        if (config != null)
        {
            Console.WriteLine("Deserialized successfully.");
        }

        // Try‑parse with error handling
        if (MigrationFileJsonConfig.TryFromJson(json, out var parsedConfig))
        {
            // Use parsedConfig here
            Console.WriteLine("TryFromJson succeeded.");
        }
    }
}
```
