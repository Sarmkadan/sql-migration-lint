using System.Collections.Generic;

namespace SqlMigrationLint;

public abstract record MigrationOperation
{
    public string? File { get; init; }
    public int? Line { get; init; }
}

public sealed record SqlOperation : MigrationOperation
{
    public string Sql { get; init; } = string.Empty;
}

public sealed record AddColumnOperation : MigrationOperation
{
    public string TableName { get; init; } = string.Empty;
    public string ColumnName { get; init; } = string.Empty;
    public bool TableExists { get; init; }
    public bool IsNullable { get; init; }
    public object? DefaultValue { get; init; }
}

public sealed record AlterColumnOperation : MigrationOperation
{
    public string TableName { get; init; } = string.Empty;
    public string ColumnName { get; init; } = string.Empty;
    public string OldType { get; init; } = string.Empty;
    public string NewType { get; init; } = string.Empty;
}

public sealed record CreateIndexOperation : MigrationOperation
{
    public string TableName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string>? Options { get; init; }
}

public sealed record AddForeignKeyOperation : MigrationOperation
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<ForeignKeyColumn> Columns { get; init; } = Array.Empty<ForeignKeyColumn>();
}

public sealed record ForeignKeyColumn
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string>? Indexes { get; init; }
    public bool IsIndexed { get; init; }
}
