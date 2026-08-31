using System;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects UPDATE or DELETE statements in a migration's up body that have no WHERE clause.
/// </summary>
internal sealed class MissingWhereRule : ILintRule
{
    public static readonly MissingWhereRule Instance = new();

    public string Name => "ML101";
    public string Description => "Detects UPDATE or DELETE statements missing a WHERE clause.";
    public LintSeverity Severity => LintSeverity.Warning;

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
        // Using Singleline to allow matching across newlines.
        var matches = Regex.Matches(sqlOperation.Sql, @"\b(UPDATE|DELETE)\b.*?(?:;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var offendingStatementCount = 0;
        string? firstOffendingStatement = null;

        foreach (Match match in matches)
        {
            // Check if the statement contains a WHERE clause.
            if (!Regex.IsMatch(match.Value, @"\bWHERE\b", RegexOptions.IgnoreCase))
            {
                offendingStatementCount++;
                firstOffendingStatement ??= match.Value.Trim().Replace(Environment.NewLine, " ");
            }
        }

        if (firstOffendingStatement is null)
        {
            return null;
        }

        var snippet = firstOffendingStatement.Length > 120
            ? firstOffendingStatement[..120]
            : firstOffendingStatement;

        return new LintFinding(
            RuleName: Name,
            Severity: Severity,
            Message: $"Found {offendingStatementCount} UPDATE or DELETE statement(s) missing a WHERE clause. First offending statement: '{snippet}'.",
            File: sqlOperation.File,
            Line: sqlOperation.Line);
    }
}
