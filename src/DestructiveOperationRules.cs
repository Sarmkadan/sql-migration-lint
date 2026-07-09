using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Contains lint rules for detecting destructive SQL operations that may cause data loss or compatibility issues.
/// </summary>
public static class DestructiveOperationRules
{
    /// <summary>
    /// Gets all destructive operation lint rules.
    /// </summary>
    public static IReadOnlyList<ILintRule> All { get; } = [
        DropTableRule.Instance,
        DropColumnRule.Instance,
        DropIndexRule.Instance,
        RenameColumnRule.Instance,
        RenameTableRule.Instance,
        DeleteDataRule.Instance,
        DropDataRule.Instance
    ];

    private sealed class DropTableRule : ILintRule
    {
        public static readonly DropTableRule Instance = new();

        public string Name => "drop-table";
        public string Description => "Detects DROP TABLE statements which permanently remove tables and their data.";
        public LintSeverity Severity => LintSeverity.Blocker;


        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"DROP\s+TABLE\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "DROP TABLE statement permanently removes tables and their data. Consider backing up or using a soft delete approach.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class DropColumnRule : ILintRule
    {
        public static readonly DropColumnRule Instance = new();

        public string Name => "drop-column";
        public string Description => "Detects DROP COLUMN statements which permanently remove columns and their data.";
        public LintSeverity Severity => LintSeverity.Blocker;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"DROP\s+COLUMN\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "DROP COLUMN statement permanently removes columns and their data. Consider backing up or using a soft delete approach.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class DropIndexRule : ILintRule
    {
        public static readonly DropIndexRule Instance = new();

        public string Name => "drop-index";
        public string Description => "Detects DROP INDEX statements which remove indexes but may impact performance.";
        public LintSeverity Severity => LintSeverity.Warning;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"DROP\s+INDEX\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "DROP INDEX statement removes an index which may impact query performance.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class RenameColumnRule : ILintRule
    {
        public static readonly RenameColumnRule Instance = new();

        public string Name => "rename-column";
        public string Description => "Detects sp_rename or ALTER TABLE RENAME COLUMN statements which may break compatibility in rolling deployments.";
        public LintSeverity Severity => LintSeverity.Danger;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"(sp_rename|ALTER\s+TABLE\s+[^;]+\s+RENAME\s+COLUMN)\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "Renaming a column may break compatibility in rolling deployments. Consider creating a new column and migrating data instead.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class RenameTableRule : ILintRule
    {
        public static readonly RenameTableRule Instance = new();

        public string Name => "rename-table";
        public string Description => "Detects sp_rename or ALTER TABLE RENAME TABLE statements which may break compatibility in rolling deployments.";
        public LintSeverity Severity => LintSeverity.Danger;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"(sp_rename|ALTER\s+TABLE\s+[^;]+\s+RENAME)\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "Renaming a table may break compatibility in rolling deployments. Consider creating a new table and migrating data instead.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class DeleteDataRule : ILintRule
    {
        public static readonly DeleteDataRule Instance = new();

        public string Name => "delete-data";
        public string Description => "Detects DELETE and TRUNCATE statements which permanently remove data.";
        public LintSeverity Severity => LintSeverity.Blocker;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"(DELETE\s+FROM|TRUNCATE(?:\s+TABLE)?)\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "DELETE/TRUNCATE statements permanently remove data. Consider using a soft delete approach or backup strategy.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }

    private sealed class DropDataRule : ILintRule
    {
        public static readonly DropDataRule Instance = new();

        public string Name => "drop-database-object";
        public string Description => "Detects DROP statements which permanently remove database objects.";
        public LintSeverity Severity => LintSeverity.Blocker;

        public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

        public LintFinding? Evaluate(MigrationOperation operation)
        {
            if (operation is not SqlOperation sqlOp)
            {
                return null;
            }

            var match = Regex.Match(sqlOp.Sql, @"DROP\s+(TABLE|DATABASE|SCHEMA)\s+[^;]+;", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: "DROP statement permanently removes database objects and their data. Consider backing up or using a soft delete approach.",
                    File: sqlOp.File,
                    Line: sqlOp.Line
                );
            }

            return null;
        }
    }
}