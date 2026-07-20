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
        var matches = Regex.Matches(sqlOperation.Sql, @"\b(UPDATE|DELETE)\b.*?;", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            // Check if the statement contains a WHERE clause.
            if (!Regex.IsMatch(match.Value, @"\bWHERE\b", RegexOptions.IgnoreCase))
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Statement '{match.Value.Trim().Replace(Environment.NewLine, " ")}' is missing a WHERE clause.",
                    File: sqlOperation.File,
                    Line: sqlOperation.Line);
            }
        }

        return null;
    }
}
