# MigrationOperation

The `MigrationOperation` type serves as a unified data transfer object representing a discrete schema change detected or defined within the `sql-migration-lint` pipeline. It encapsulates the metadata required to analyze, validate, and lint SQL migration scripts, capturing context such as source file location, target object identifiers (tables, columns, constraints), and specific transformation details like type changes or nullability shifts. This class acts as the primary input for linting rules, allowing analyzers to inspect the structural impact of a migration without parsing raw SQL strings directly.

## API

The following members expose the structural data of the migration operation. Note that due to the union-like nature of this type covering various operation kinds (e.g., AddColumn, AlterColumn, CreateIndex), specific properties will be populated depending on the operation type, while others may remain null or default.

### `File`
```csharp
public string? File
```
Gets the relative or absolute path to the SQL script file containing this operation. Returns `null` if the operation was generated programmatically or the source file is unknown.

### `Line`
```csharp
public int? Line
```
Gets the line number within the source `File` where this operation is defined. Returns `null` if the line number cannot be determined.

### `Sql`
```csharp
public string Sql
```
Gets the raw SQL statement associated with this operation. This property is always populated for operations derived directly from script parsing.

### `TableName`
```csharp
public string TableName
```
Gets the name of the table targeted by this operation. This property is relevant for column additions, alterations, and table-level constraints. Accessing this on an operation type that does not target a table may return an empty string or throw depending on internal validation state, but generally reflects the parsed target.

### `ColumnName`
```csharp
public string ColumnName
```
Gets the name of the specific column being added, altered, or referenced. Used primarily for single-column operations.

### `TableExists`
```csharp
public bool TableExists
```
Gets a boolean indicating whether the target table currently exists in the database schema at the time of analysis. This is used to validate operations against non-existent tables.

### `IsNullable`
```csharp
public bool IsNullable
```
Gets a value indicating whether the column involved in the operation allows null values. This is typically used in `AddColumn` or `AlterColumn` contexts.

### `DefaultValue`
```csharp
public object? DefaultValue
```
Gets the default value assigned to a column during creation or alteration. Returns `null` if no default value is specified. The runtime type depends on the SQL type (e.g., `string`, `int`, `DateTime`).

### `OldType`
```csharp
public string OldType
```
Gets the original data type of a column before an alteration. This is populated only for `AlterColumn` operations where the type is changing.

### `NewType`
```csharp
public string NewType
```
Gets the new data type of a column after an alteration or the type for a newly added column.

### `Name`
```csharp
public string Name
```
Gets the name of the database object being created or modified, such as a constraint name, index name, or foreign key name. The specific entity type is determined by the context of other populated properties.

### `Columns` (String List)
```csharp
public IReadOnlyList<string> Columns
```
Gets a read-only list of column names associated with the operation. This is commonly used for composite primary keys, unique constraints, or multi-column indexes.

### `Options`
```csharp
public IReadOnlyList<string>? Options
```
Gets a read-only list of string-based options or flags associated with the operation (e.g., `ON DELETE CASCADE`, `CLUSTERED`). Returns `null` if no options are specified.

### `Columns` (ForeignKeyColumn List)
```csharp
public IReadOnlyList<ForeignKeyColumn> Columns
```
Gets a read-only list of `ForeignKeyColumn` objects detailing the mapping between local and referenced columns in a foreign key constraint. This overload is specific to foreign key operations.

### `Indexes`
```csharp
public IReadOnlyList<string>? Indexes
```
Gets a read-only list of index names affected by or associated with this operation. Returns `null` if the operation does not involve indexes.

## Usage

### Example 1: Analyzing an Alter Column Operation
This example demonstrates how to inspect a `MigrationOperation` representing a data type change to ensure it is safe for production data.

```csharp
public void ValidateTypeChange(MigrationOperation operation)
{
    if (operation.OldType == null || operation.NewType == null)
    {
        return; // Not a type alteration
    }

    // Check if converting from a larger type to a smaller type potentially causing data loss
    if (operation.OldType.Contains("VARCHAR(255)") && operation.NewType.Contains("VARCHAR(50)"))
    {
        throw new MigrationValidationException(
            $"Potential data truncation detected in {operation.File}:{operation.Line}. " +
            $"Cannot shrink column '{operation.ColumnName}' on table '{operation.TableName}' " +
            $"from {operation.OldType} to {operation.NewType} without data verification."
        );
    }
}
```

### Example 2: Validating Foreign Key Constraints
This example iterates through the columns of a foreign key operation to ensure all referenced columns are explicitly listed.

```csharp
public void AuditForeignKeyColumns(MigrationOperation operation)
{
    // Identify FK operations by checking for the specific Columns collection type or context
    if (operation.Columns is IReadOnlyList<ForeignKeyColumn> fkColumns && fkColumns.Any())
    {
        Console.WriteLine($"Validating Foreign Key: {operation.Name} on {operation.TableName}");

        foreach (var fkCol in fkColumns)
        {
            if (string.IsNullOrWhiteSpace(fkCol.ReferencedColumn))
            {
                throw new MigrationValidationException(
                    $"Invalid FK definition at {operation.File}:{operation.Line}. " +
                    $"Column '{fkCol.Column}' lacks a referenced target."
                );
            }
        }

        if (operation.Options != null && operation.Options.Contains("NO CHECK"))
        {
            Console.WriteLine("Warning: Foreign key created with NO CHECK constraint.");
        }
    }
}
```

## Notes

*   **Mutually Exclusive Data**: `MigrationOperation` acts as a sum type. Properties such as `OldType`/`NewType` are only relevant for alterations, while `Columns` (string list) applies to indexes or constraints, and `Columns` (ForeignKeyColumn list) applies strictly to foreign keys. Consumers must check the context or operation type before accessing specific property sets to avoid logical errors, though the properties themselves will not throw simply by being accessed (they will return defaults like `null` or empty lists if irrelevant).
*   **Nullability**: Several properties (`File`, `Line`, `Options`, `Indexes`, `DefaultValue`) are nullable. Callers must perform null checks before dereferencing these members, particularly when operations are synthesized in memory rather than parsed from physical files.
*   **Thread Safety**: The `MigrationOperation` class is immutable regarding its public contract once instantiated; all collection properties expose `IReadOnlyList<T>`. Therefore, instances of this class are thread-safe for concurrent read operations. However, the underlying objects within the `ForeignKeyColumn` list should be treated as immutable as well to maintain overall safety.
*   **Duplicate Property Names**: The signature list includes multiple definitions for `TableName`, `ColumnName`, and `Name`. In the compiled type, these represent a single property each. The repetition in documentation sources indicates these fields are heavily utilized across different operation sub-types (e.g., `TableName` is critical for both `AddColumn` and `CreateIndex`).
