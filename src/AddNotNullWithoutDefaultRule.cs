using System;

namespace SqlMigrationLint;

/// <summary>
/// Detects ADD COLUMN operations with NOT NULL constraint but without DEFAULT value,
/// which are unsafe on populated tables as they require a table rewrite.
/// </summary>
internal sealed class AddNotNullWithoutDefaultRule : ILintRule
{
    public static readonly AddNotNullWithoutDefaultRule Instance = new();

    public string Name => "add-not-null-without-default";
    public string Description => "Detects ADD COLUMN operations with NOT NULL constraint but without DEFAULT value, which cause table rewrites on populated tables.";
    public LintSeverity Severity => LintSeverity.Danger;

    public bool AppliesTo(MigrationOperation operation)
    {
        return operation is AddColumnOperation addColumn &&
               addColumn.IsNullable == false &&
               addColumn.DefaultValue is null &&
               addColumn.TableExists;
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        if (operation is not AddColumnOperation addColumn)
        {
            return null;
        }

        return new LintFinding(
            RuleName: Name,
            Severity: Severity,
            Message: $"Adding NOT NULL column '{addColumn.ColumnName}' without DEFAULT value to existing table '{addColumn.TableName}' causes table rewrite. Consider using a nullable column without default or add default in a separate migration.",
            File: addColumn?.File,
            Line: addColumn?.Line
        );
    }
}