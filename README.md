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

## MigrationFileExtensions

`MigrationFileExtensions` provides a collection of extension methods for `MigrationFile` that simplify common operations on migration files, such as counting lines, extracting SQL statements, checking for body content, and analyzing file metadata. These methods help you quickly inspect migration files without manually parsing their structure.

Example usage:

```csharp
using System;
using System.IO;
using SqlMigrationLint;

class Program
{
    static void Main()
    {
        // Assume we have a migration file
        var migrationFile = new MigrationFile(
            filePath: Path.Combine("Migrations", "0001_CreateUserTable.cs"),
            name: "CreateUserTable",
            upBody: "migration.CreateTable(\n                name: \"Users\",
                columns: table => new
                {\n                    table.Column<Guid>(name: \"Id\");
                    table.Column<string>(name: \"Email\", nullable: false);
                });",
            downBody: "migration.DropTable(\"Users\");"
        );

        // Use extension methods to analyze the migration
        Console.WriteLine($"File: {migrationFile.GetFileNameWithoutExtension()}");
        Console.WriteLine($"Total lines: {migrationFile.GetLineCount()}");
        Console.WriteLine($"Up body lines: {migrationFile.GetUpBodyLineCount()}");
        Console.WriteLine($"Down body lines: {migrationFile.GetDownBodyLineCount()}");
        Console.WriteLine($"Has Up body: {migrationFile.HasUpBody()}");
        Console.WriteLine($"Has Down body: {migrationFile.HasDownBody()}");
        Console.WriteLine($"File size: {migrationFile.GetFileSize()} bytes");
        Console.WriteLine($"Total SQL statements: {migrationFile.GetTotalSqlStatementCount()}");
        
        // Get SQL statements
        Console.WriteLine("\nUp SQL statements:");
        foreach (var statement in migrationFile.GetUpSqlStatements())
        {
            Console.WriteLine($"  - {statement}");
        }
        
        Console.WriteLine("\nDown SQL statements:");
        foreach (var statement in migrationFile.GetDownSqlStatements())
        {
            Console.WriteLine($"  - {statement}");
        }
        
        // Count comments and keywords
        var sqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CREATE", "DROP", "ALTER", "INSERT", "UPDATE", "DELETE", "SELECT"
        };
        
        Console.WriteLine($"\nUp body comments: {migrationFile.GetUpBodyCommentCount()}");
        Console.WriteLine($"Down body comments: {migrationFile.GetDownBodyCommentCount()}");
        Console.WriteLine($"Up body keywords: {migrationFile.GetUpBodyKeywordCount(sqlKeywords)}");
        Console.WriteLine($"Down body keywords: {migrationFile.GetDownBodyKeywordCount(sqlKeywords)}");
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

## MigrationLinterExtensions

`MigrationLinterExtensions` provides a collection of extension methods for `MigrationLinter` and `LintReport` that simplify common reporting and analysis tasks. These methods allow you to filter findings by severity, group them by file or rule, calculate statistics, and quickly access summary information about migration linting results.

Example usage:

```csharp
using System;
using System.Linq;
using SqlMigrationLint;

class Program
{
    static void Main()
    {
        // Create a linter with the default rule set
        var linter = MigrationLinter.CreateDefault();
        string projectPath = "/path/to/your/project";

        // Get all blocker findings
        var blockers = linter.GetBlockerFindings(projectPath);
        Console.WriteLine($"Blocker findings: {blockers.Count()}");

        // Group findings by file
        var findingsByFile = linter.GroupFindingsByFile(projectPath);
        foreach (var fileGroup in findingsByFile)
        {
            Console.WriteLine($"\nFile: {fileGroup.Key}");
            Console.WriteLine($"  Total findings: {fileGroup.Value.Count}");
            Console.WriteLine($"  Warnings: {fileGroup.Value.Count(f => f.Severity == LintSeverity.Warning)}");
            Console.WriteLine($"  Dangers: {fileGroup.Value.Count(f => f.Severity == LintSeverity.Danger)}");
            Console.WriteLine($"  Blockers: {fileGroup.Value.Count(f => f.Severity == LintSeverity.Blocker)}");
        }

        // Get statistics
        Console.WriteLine($"\nTotal findings: {linter.GetTotalFindingsCount(projectPath)}");
        Console.WriteLine($"Findings percentage: {linter.GetFindingsPercentage(projectPath)}%");
        Console.WriteLine($"Max severity: {linter.GetMaxFindingSeverity(projectPath)}");

        // Get findings by severity
        var warnings = linter.GetFindingsBySeverity(projectPath, LintSeverity.Warning);
        Console.WriteLine($"\nWarning findings: {warnings.Count()}");

        // Get count by severity
        var countsBySeverity = linter.GetFindingsCountBySeverity(projectPath);
        foreach (var kvp in countsBySeverity)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        // Check if any findings of a specific severity exist
        bool hasBlockers = linter.HasFindingsOfSeverity(projectPath, LintSeverity.Blocker);
        Console.WriteLine($"\nHas blockers: {hasBlockers}");

        // Get rule names with findings
        var ruleNames = linter.GetRuleNamesWithFindings(projectPath);
        Console.WriteLine($"\nRules with findings: {string.Join(", ", ruleNames)}");
    }
}
```
