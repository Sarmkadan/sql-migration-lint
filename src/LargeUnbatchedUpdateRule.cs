using System;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects UPDATE or DELETE statements in a migration's up body that have no WHERE clause
/// **and** are not batched (i.e., lack TOP or LIMIT). These are flagged as a blocker error
/// with a suggestion to batch the operation.
/// </summary>
internal sealed class LargeUnbatchedUpdateRule : ILintRule
{
    public static readonly LargeUnbatchedUpdateRule Instance = new();

    // Rule identifier – following the pattern used by other rules.
    public string Name => "ML102";

    public string Description => "Detects UPDATE or DELETE statements missing a WHERE clause and lacking batching (TOP/LIMIT).";

    // Use the blocker severity (the enum does not define an 'Error' member).
    public LintSeverity Severity => LintSeverity.Blocker;

    public bool AppliesTo(MigrationOperation operation)
    {
        return operation is SqlOperation;
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        if (operation is not SqlOperation sqlOperation)
        {
            return null;
        }

        // Find UPDATE or DELETE statements.
        // Singleline allows matching across newlines.
        var matches = Regex.Matches(
            sqlOperation.Sql,
            @"\b(UPDATE|DELETE)\b.*?;",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var statement = match.Value;

            // Check for a WHERE clause.
            bool hasWhere = Regex.IsMatch(statement, @"\bWHERE\b", RegexOptions.IgnoreCase);

            // Check for batching keywords (TOP for SQL Server, LIMIT for MySQL/Postgres).
            bool hasBatching = Regex.IsMatch(statement, @"\bTOP\s+\d+\b", RegexOptions.IgnoreCase) ||
                               Regex.IsMatch(statement, @"\bLIMIT\s+\d+\b", RegexOptions.IgnoreCase);

            // Flag only when both WHERE and batching are missing.
            if (!hasWhere && !hasBatching)
            {
                var cleanedStatement = statement.Trim().Replace(Environment.NewLine, " ");
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Statement '{cleanedStatement}' is missing a WHERE clause and is not batched (no TOP/LIMIT). Consider adding batching to limit the number of rows affected per operation.",
                    File: sqlOperation.File,
                    Line: sqlOperation.Line);
            }
        }

        return null;
    }
}
