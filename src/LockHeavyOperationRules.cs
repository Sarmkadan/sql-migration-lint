namespace SqlMigrationLint;

internal static class LockHeavyOperationRules
{
    public static IReadOnlyList<ILintRule> All => [
        AddColumnWithDefaultRule.Instance,
        AlterColumnTypeChangeRule.Instance,
        DataLossAlterColumnRule.Instance,
        CreateIndexWithoutConcurrentRule.Instance,
        AddForeignKeyWithoutIndexRule.Instance,
        NullableFalseWithoutDefaultRule.Instance
    ];

    private sealed class AddColumnWithDefaultRule : ILintRule
    {
        public static readonly AddColumnWithDefaultRule Instance = new();

        public string Name => "add-column-with-default";
        public string Description => "Detects ADD COLUMN operations with DEFAULT values on existing tables that cause table rewrites.";
        public LintSeverity Severity => LintSeverity.Warning;

        public bool AppliesTo(MigrationOperation operation)
        {
            return operation is AddColumnOperation { TableExists: true } addColumn && addColumn.DefaultValue is not null;
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
                Message: $"Adding column '{addColumn.ColumnName}' with default value to existing table '{addColumn.TableName}' causes table rewrite. Consider using a nullable column without default or add default in a separate migration.",
                File: addColumn?.File,
                Line: addColumn?.Line
            );
        }
    }

    private sealed class AlterColumnTypeChangeRule : ILintRule
    {
        public static readonly AlterColumnTypeChangeRule Instance = new();

        public string Name => "alter-column-type-change";
        public string Description => "Detects ALTER COLUMN operations that change column types, which can be destructive.";
        public LintSeverity Severity => LintSeverity.Danger;

        public bool AppliesTo(MigrationOperation operation)
        {
            return operation is AlterColumnOperation;
        }

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not AlterColumnOperation alterColumn)
            {
                return null;
            }

            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"Changing column '{alterColumn.ColumnName}' type from '{alterColumn.OldType}' to '{alterColumn.NewType}' in table '{alterColumn.TableName}' can be destructive. Consider creating a new column and migrating data.",
                File: alterColumn?.File,
                Line: alterColumn?.Line
            );
        }
    }

    private sealed class CreateIndexWithoutConcurrentRule : ILintRule
    {
        public static readonly CreateIndexWithoutConcurrentRule Instance = new();

        public string Name => "create-index-without-concurrent";
        public string Description => "Detects CREATE INDEX operations without CONCURRENTLY or ONLINE options that lock tables.";
        public LintSeverity Severity => LintSeverity.Warning;

        public bool AppliesTo(MigrationOperation operation)
        {
            return operation is CreateIndexOperation;
        }

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not CreateIndexOperation createIndex)
            {
                return null;
            }

            // Check if the index creation uses CONCURRENTLY or ONLINE options
            var hasConcurrently = createIndex.Options?.Any(opt => opt.Contains("CONCURRENTLY", StringComparison.OrdinalIgnoreCase) == true) ?? false;
            var hasOnline = createIndex.Options?.Any(opt => opt.Contains("ONLINE", StringComparison.OrdinalIgnoreCase) == true) ?? false;
            var isConcurrent = hasConcurrently || hasOnline;

            if (!isConcurrent)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Creating index '{createIndex.Name}' on table '{createIndex.TableName}' without CONCURRENTLY/ONLINE option locks the table. Consider adding 'CONCURRENTLY' for PostgreSQL or 'ONLINE' for SQL Server.",
                    File: createIndex?.File,
                    Line: createIndex?.Line
                );
            }

            return null;
        }
    }

    private sealed class AddForeignKeyWithoutIndexRule : ILintRule
    {
        public static readonly AddForeignKeyWithoutIndexRule Instance = new();

        public string Name => "add-foreign-key-without-index";
        public string Description => "Detects ADD FOREIGN KEY operations without pre-existing indexes on the foreign key column.";
        public LintSeverity Severity => LintSeverity.Blocker;

        public bool AppliesTo(MigrationOperation operation)
        {
            return operation is AddForeignKeyOperation;
        }

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not AddForeignKeyOperation fk)
            {
                return null;
            }

            // Check if there's an index on the foreign key column
            var hasIndex = fk.Columns.Any(col =>
                col.Indexes?.Count > 0
                || col.IsIndexed == true);

            if (!hasIndex)
            {
                var columnNames = string.Join(", ", fk.Columns.Select(c => c.Name));
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Adding foreign key constraint '{fk.Name}' on column(s) [{columnNames}] without pre-existing index can cause performance issues. Create an index on the foreign key column before adding the constraint.",
                    File: fk?.File,
                    Line: fk?.Line
                );
            }

            return null;
        }
    }

    private sealed class NullableFalseWithoutDefaultRule : ILintRule
    {
        public static readonly NullableFalseWithoutDefaultRule Instance = new();

        public string Name => "nullable-false-without-default";
        public string Description => "Detects NOT NULL column additions without DEFAULT values that may cause issues in existing queries.";
        public LintSeverity Severity => LintSeverity.Blocker;

        public bool AppliesTo(MigrationOperation operation)
        {
            return operation is AddColumnOperation { IsNullable: false } addColumn && addColumn.DefaultValue is null;
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
                Message: $"Adding NOT NULL column '{addColumn.ColumnName}' without DEFAULT value can cause issues if existing rows need to be updated. Consider adding a DEFAULT value or making the column nullable initially.",
                File: addColumn?.File,
                Line: addColumn?.Line
            );
        }
    }
}
