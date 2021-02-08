using System.Collections.Generic;

namespace SqlMigrationLint;

/// <summary>
/// Represents a migration operation.
/// </summary>
public abstract record MigrationOperation
{
    /// <summary>
    /// Gets or initializes the file path associated with the migration operation.
    /// </summary>
    public string? File { get; init; }

    /// <summary>
    /// Gets or initializes the line number associated with the migration operation.
    /// </summary>
    public int? Line { get; init; }
}

/// <summary>
/// Represents a SQL operation.
/// </summary>
public sealed record SqlOperation : MigrationOperation
{
    /// <summary>
    /// Gets or initializes the SQL statement associated with the operation.
    /// </summary>
    public string Sql { get; init; } = string.Empty;
}

/// <summary>
/// Represents an add column operation.
/// </summary>
public sealed record AddColumnOperation : MigrationOperation
{
    /// <summary>
    /// Gets or initializes the name of the table to add the column to.
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the name of the column to add.
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes whether the table already exists.
    /// </summary>
    public bool TableExists { get; init; }

    /// <summary>
    /// Gets or initializes whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets or initializes the default value of the column.
    /// </summary>
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Represents an alter column operation.
/// </summary>
public sealed record AlterColumnOperation : MigrationOperation
{
    /// <summary>
    /// Gets or initializes the name of the table to alter the column in.
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the name of the column to alter.
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the old data type of the column.
    /// </summary>
    public string OldType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the new data type of the column.
    /// </summary>
    public string NewType { get; init; } = string.Empty;
}

/// <summary>
/// Represents a create index operation.
/// </summary>
public sealed record CreateIndexOperation : MigrationOperation
{
    /// <summary>
    /// Gets or initializes the name of the table to create the index on.
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the name of the index to create.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the columns to include in the index.
    /// </summary>
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or initializes any options for the index.
    /// </summary>
    public IReadOnlyList<string>? Options { get; init; }
}

/// <summary>
/// Represents an add foreign key operation.
/// </summary>
public sealed record AddForeignKeyOperation : MigrationOperation
{
    /// <summary>
    /// Gets or initializes the name of the foreign key to add.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the columns to include in the foreign key.
    /// </summary>
    public IReadOnlyList<ForeignKeyColumn> Columns { get; init; } = Array.Empty<ForeignKeyColumn>();
}

/// <summary>
/// Represents a foreign key column.
/// </summary>
public sealed record ForeignKeyColumn
{
    /// <summary>
    /// Gets or initializes the name of the foreign key column.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes any indexes associated with the foreign key column.
    /// </summary>
    public IReadOnlyList<string>? Indexes { get; init; }

    /// <summary>
    /// Gets or initializes whether the foreign key column is indexed.
    /// </summary>
    public bool IsIndexed { get; init; }
}
