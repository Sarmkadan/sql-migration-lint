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
