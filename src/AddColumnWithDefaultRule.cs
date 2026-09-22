namespace SqlMigrationLint;

/// <summary>
/// Detects ADD COLUMN operations with DEFAULT values on existing tables.
/// </summary>
public sealed class AddColumnWithDefaultRule : ILintRule
{
    public static readonly AddColumnWithDefaultRule Instance = new();

    public string Name => "add-column-with-default";
    public string Description => "Detects ADD COLUMN operations with DEFAULT values on existing tables that cause table rewrites.";
    public LintSeverity Severity => LintSeverity.Warning;

    public bool AppliesTo(MigrationOperation operation)
    {
        ValidateOperation(operation);

        return operation is AddColumnOperation { TableExists: true, DefaultValue: not null };
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        ValidateOperation(operation);

        if (operation is not AddColumnOperation addColumn)
        {
            return null;
        }

        return new LintFinding(
            RuleName: Name,
            Severity: Severity,
            Message: $"Adding column '{addColumn.ColumnName}' with default value to existing table '{addColumn.TableName}' causes table rewrite. Consider using a nullable column without default or add default in a separate migration.",
            File: addColumn.File,
            Line: addColumn.Line);
    }

    private static void ValidateOperation(MigrationOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (operation is SqlOperation sqlOperation && string.IsNullOrWhiteSpace(sqlOperation.Sql))
        {
            throw new ArgumentException("SQL text cannot be empty or whitespace.", nameof(operation));
        }
    }
}
